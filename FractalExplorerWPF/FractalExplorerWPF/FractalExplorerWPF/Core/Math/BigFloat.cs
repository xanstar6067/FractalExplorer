using System.Globalization;
using System.Numerics;
using System.Text;

namespace FractalExplorerWPF.Core.NewtonMath;

/// <summary>
/// Компактное число с плавающей запятой произвольной (фиксированной) точности:
/// значение = <see cref="Mantissa"/> · 2^<see cref="Exponent"/>. Мантисса — знаковый
/// <see cref="BigInteger"/>, после каждой операции округляется до
/// <see cref="PrecisionBits"/> значащих бит.
///
/// Тип нужен только «второму двигателю» глубокого зума Мандельброта: он хранит
/// центр области и опорную точку с точностью, недостижимой для <see cref="decimal"/>
/// (≈28 десятичных цифр). Набор операций намеренно минимален — сложение, вычитание,
/// умножение, квадратный корень, сравнение и конвертации, которых достаточно для навигации
/// и построения опорной орбиты.
/// </summary>
public readonly struct BigFloat : IComparable<BigFloat>, IEquatable<BigFloat>
{
    /// <summary>
    /// Нижняя граница и значение по умолчанию рабочей точности мантиссы. 384 бита ≈ 115
    /// десятичных цифр — с запасом перекрывает зум до предела <see cref="decimal"/>-зума
    /// (~1e28) и достаётся глубокому зуму примерно до 1e90. Глубже
    /// <see cref="WorkingPrecisionBits"/> поднимается адаптивно; ниже этого порога — никогда.
    /// </summary>
    public const int MinimumPrecisionBits = 384;

    /// <summary>
    /// Жёсткий нижний предел, ниже которого <see cref="WorkingPrecisionBits"/> не опускается
    /// даже явным заданием. Значение по умолчанию (когда точность не задана) по-прежнему
    /// <see cref="MinimumPrecisionBits"/>; область <see cref="PrecisionScope"/> может опустить
    /// точность ниже 384 бит — это нужно ступеням, которым 115 десятичных цифр избыточны и
    /// стоят лишнего времени (прямая итерация Коллатца на умеренной глубине).
    /// </summary>
    public const int AbsoluteMinimumPrecisionBits = 96;

    [ThreadStatic] private static int _workingPrecisionBits;

    /// <summary>
    /// Рабочая точность мантиссы для текущего потока: все операции округляют результат до
    /// этого числа значащих бит. По умолчанию (и как нижняя граница) равна
    /// <see cref="MinimumPrecisionBits"/>. Поднимается на время построения опорной орбиты
    /// сверхглубокого зума через <see cref="PrecisionScope"/> и восстанавливается после.
    /// </summary>
    public static int WorkingPrecisionBits
    {
        get => _workingPrecisionBits < AbsoluteMinimumPrecisionBits
            ? MinimumPrecisionBits
            : _workingPrecisionBits;
        set => _workingPrecisionBits = value < AbsoluteMinimumPrecisionBits
            ? AbsoluteMinimumPrecisionBits
            : value;
    }

    /// <summary>
    /// Временно повышает <see cref="WorkingPrecisionBits"/> текущего потока и возвращает
    /// прежнее значение при <see cref="Dispose"/>. Область точности не вкладывается сама в
    /// себя рекурсивно — рассчитана на один охватывающий вызов построения опорной орбиты.
    /// </summary>
    public readonly ref struct PrecisionScope
    {
        private readonly int _previous;

        public PrecisionScope(int bits)
        {
            _previous = _workingPrecisionBits;
            WorkingPrecisionBits = bits;
        }

        public void Dispose() => _workingPrecisionBits = _previous;
    }

    /// <summary>Минимум десятичных цифр после запятой при round-trip сериализации.</summary>
    private const int SerializationFractionDigits = 130;

    // log10(2): перевод «значащих бит мантиссы» в «десятичные цифры».
    private const double Log10Of2 = 0.30102999566398120;

    public BigInteger Mantissa { get; }
    public int Exponent { get; }

    private BigFloat(BigInteger mantissa, int exponent)
    {
        if (mantissa.IsZero)
        {
            Mantissa = BigInteger.Zero;
            Exponent = 0;
            return;
        }

        int precisionBits = WorkingPrecisionBits;
        int bitLength = (int)mantissa.GetBitLength();
        if (bitLength > precisionBits)
        {
            int shift = bitLength - precisionBits;
            int sign = mantissa.Sign;
            BigInteger magnitude = BigInteger.Abs(mantissa);
            // Округление к ближайшему (half-up по модулю).
            magnitude = (magnitude + (BigInteger.One << (shift - 1))) >> shift;
            mantissa = sign < 0 ? -magnitude : magnitude;
            exponent += shift;
        }

        // Убираем младшие нулевые биты — держит мантиссу компактной и канонизирует значение.
        int trailing = (int)BigInteger.TrailingZeroCount(mantissa);
        if (trailing > 0)
        {
            mantissa >>= trailing;
            exponent += trailing;
        }

        Mantissa = mantissa;
        Exponent = exponent;
    }

    public static BigFloat Zero => default;

    /// <summary>Единица. Значение не зависит от рабочей точности.</summary>
    public static BigFloat One { get; } = FromInt(1);

    public bool IsZero => Mantissa.IsZero;
    public int Sign => Mantissa.Sign;

    /// <summary>
    /// Двоичный порядок величины: для ненулевого значения |x| ∈ [2^(BinaryExponent−1),
    /// 2^BinaryExponent). По нему алгоритмы трансцендентных функций выбирают глубину
    /// приведения аргумента и решают, что очередной член ряда уже пренебрежимо мал —
    /// без вычитания и без выделения памяти под промежуточный результат.
    /// </summary>
    public int BinaryExponent => Mantissa.IsZero
        ? int.MinValue
        : (int)BigInteger.Abs(Mantissa).GetBitLength() + Exponent;

    public static BigFloat Abs(BigFloat value) => value.Sign < 0 ? -value : value;

    private static BigFloat FromRawRounded(BigInteger mantissa, int exponent) => new(mantissa, exponent);

    public static BigFloat FromInt(long value) => new(value, 0);

    public static BigFloat FromDouble(double value)
    {
        if (value == 0 || !double.IsFinite(value)) return Zero;

        long bits = BitConverter.DoubleToInt64Bits(value);
        bool negative = bits < 0;
        int exponentField = (int)((bits >> 52) & 0x7FF);
        long fraction = bits & 0xF_FFFF_FFFF_FFFF;

        BigInteger mantissa;
        int exponent;
        if (exponentField == 0)
        {
            // Субнормальное число.
            mantissa = fraction;
            exponent = -1022 - 52;
        }
        else
        {
            mantissa = fraction | (1L << 52);
            exponent = exponentField - 1023 - 52;
        }

        if (negative) mantissa = -mantissa;
        return new BigFloat(mantissa, exponent);
    }

    public static BigFloat FromDecimal(decimal value)
    {
        if (value == 0m) return Zero;

        int[] parts = decimal.GetBits(value);
        int scale = (parts[3] >> 16) & 0xFF;
        bool negative = (parts[3] & int.MinValue) != 0;

        BigInteger magnitude = (uint)parts[2];
        magnitude = (magnitude << 32) | (uint)parts[1];
        magnitude = (magnitude << 32) | (uint)parts[0];

        BigInteger numerator = negative ? -magnitude : magnitude;
        if (scale == 0) return new BigFloat(numerator, 0);
        return FromRatio(numerator, BigInteger.Pow(10, scale));
    }

    /// <summary>Разбор десятичной строки (инвариантная культура), в т.ч. в научной нотации.</summary>
    public static BigFloat Parse(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return Zero;
        text = text.Trim();

        bool negative = false;
        int index = 0;
        if (text[0] is '+' or '-')
        {
            negative = text[0] == '-';
            index = 1;
        }

        var digits = new StringBuilder();
        int fractionDigits = 0;
        bool seenPoint = false;
        int exponentPart = 0;

        for (; index < text.Length; index++)
        {
            char c = text[index];
            if (c is >= '0' and <= '9')
            {
                digits.Append(c);
                if (seenPoint) fractionDigits++;
            }
            else if (c == '.' && !seenPoint)
            {
                seenPoint = true;
            }
            else if (c is 'e' or 'E')
            {
                exponentPart = int.Parse(text[(index + 1)..], NumberStyles.Integer, CultureInfo.InvariantCulture);
                break;
            }
            else
            {
                throw new FormatException($"Недопустимый символ '{c}' в числе «{text}».");
            }
        }

        BigInteger mantissa = digits.Length == 0 ? BigInteger.Zero : BigInteger.Parse(digits.ToString(), CultureInfo.InvariantCulture);
        if (negative) mantissa = -mantissa;
        if (mantissa.IsZero) return Zero;

        int decimalExponent = exponentPart - fractionDigits;
        if (decimalExponent >= 0)
            return new BigFloat(mantissa * BigInteger.Pow(10, decimalExponent), 0);
        return FromRatio(mantissa, BigInteger.Pow(10, -decimalExponent));
    }

    /// <summary>Округлённое значение num / den с рабочей точностью.</summary>
    private static BigFloat FromRatio(BigInteger numerator, BigInteger denominator)
    {
        if (numerator.IsZero) return Zero;
        int sign = numerator.Sign * denominator.Sign;
        BigInteger absNumerator = BigInteger.Abs(numerator);
        BigInteger absDenominator = BigInteger.Abs(denominator);

        // Масштабируем так, чтобы получить хотя бы WorkingPrecisionBits + 2 значащих бита.
        int numeratorBits = (int)absNumerator.GetBitLength();
        int denominatorBits = (int)absDenominator.GetBitLength();
        int shift = WorkingPrecisionBits + 2 - (numeratorBits - denominatorBits);
        if (shift < 0) shift = 0;

        BigInteger scaled = (absNumerator << shift);
        BigInteger quotient = BigInteger.DivRem(scaled, absDenominator, out BigInteger remainder);
        // Округление к ближайшему по остатку.
        if ((remainder << 1) >= absDenominator) quotient += 1;
        if (sign < 0) quotient = -quotient;
        return new BigFloat(quotient, -shift);
    }

    public static BigFloat operator -(BigFloat value) => FromRawRounded(-value.Mantissa, value.Exponent);

    public static BigFloat operator +(BigFloat left, BigFloat right)
    {
        if (left.IsZero) return right;
        if (right.IsZero) return left;

        if (left.Exponent == right.Exponent)
            return new BigFloat(left.Mantissa + right.Mantissa, left.Exponent);

        if (left.Exponent > right.Exponent)
        {
            BigInteger aligned = left.Mantissa << (left.Exponent - right.Exponent);
            return new BigFloat(aligned + right.Mantissa, right.Exponent);
        }
        else
        {
            BigInteger aligned = right.Mantissa << (right.Exponent - left.Exponent);
            return new BigFloat(left.Mantissa + aligned, left.Exponent);
        }
    }

    public static BigFloat operator -(BigFloat left, BigFloat right) => left + (-right);

    /// <summary>
    /// Нижняя граница экспоненты: всё, что мельче 2^-1048576 (≈1e-315653), для задач движка
    /// неотличимо от нуля и схлопывается в <see cref="Zero"/>. Защита нужна не «на всякий
    /// случай»: экспонента хранится в <see cref="int"/>, а орбита, сходящаяся к
    /// сверхпритягивающей точке (центр ровно в ядре минимандельброта, Жюлиа при c=0 внутри
    /// круга), на каждом шаге z ← z² удваивает модуль экспоненты — int переполняется примерно
    /// за 31 шаг, и без порога число «переворачивается» в огромное, а орбита ломается.
    /// Со схлопыванием в ноль поведение остаётся математически верным: z→0 ⇒ z²+c → c, то
    /// есть орбита правильно садится на цикл, содержащий ноль.
    /// </summary>
    private const int MinimumExponent = -(1 << 20);

    /// <summary>
    /// Верхняя граница экспоненты, симметричная <see cref="MinimumExponent"/>. Значение
    /// 2^1048576 (≈1e315652) для задач движка уже «бесконечность»: любая орбита с таким
    /// модулем давно вышла за радиус выхода. Насыщение здесь нужно ровно затем же, зачем
    /// схлопывание внизу — экспонента хранится в <see cref="int"/>, и без ограничения
    /// произведение двух огромных чисел «перевернулось» бы в маленькое.
    /// </summary>
    private const int MaximumExponent = 1 << 20;

    /// <summary>Значение mantissa·2^exponent с округлением до рабочей точности и с
    /// ограничением экспоненты сверху и снизу.</summary>
    private static BigFloat Scaled(BigInteger mantissa, long exponent)
    {
        if (mantissa.IsZero) return Zero;
        if (exponent < MinimumExponent) return Zero;
        if (exponent > MaximumExponent) exponent = MaximumExponent;
        return new BigFloat(mantissa, (int)exponent);
    }

    /// <summary>Значение mantissa·2^exponent, округлённое до рабочей точности.</summary>
    public static BigFloat FromScaled(BigInteger mantissa, int exponent) => Scaled(mantissa, exponent);

    /// <summary>
    /// Умножение на 2^shift. Меняется только экспонента, поэтому операция точная —
    /// на ней держится приведение аргумента у <see cref="BigFloatMath"/>.
    /// </summary>
    public static BigFloat ScaleByPowerOfTwo(BigFloat value, int shift) =>
        value.IsZero ? Zero : Scaled(value.Mantissa, (long)value.Exponent + shift);

    public static BigFloat operator *(BigFloat left, BigFloat right)
    {
        if (left.IsZero || right.IsZero) return Zero;
        return Scaled(left.Mantissa * right.Mantissa, (long)left.Exponent + right.Exponent);
    }

    public static BigFloat operator *(BigFloat left, long right) =>
        left.IsZero || right == 0 ? Zero : Scaled(left.Mantissa * right, left.Exponent);

    /// <summary>
    /// Деление с рабочей точностью. Считается как отношение мантисс через
    /// <see cref="FromRatio"/> с последующим сложением экспонент: вычитания близких величин
    /// здесь нет, поэтому погрешность результата — одно округление.
    /// </summary>
    public static BigFloat operator /(BigFloat left, BigFloat right)
    {
        if (right.IsZero) throw new DivideByZeroException("Деление BigFloat на ноль.");
        if (left.IsZero) return Zero;
        BigFloat ratio = FromRatio(left.Mantissa, right.Mantissa);
        return Scaled(ratio.Mantissa, (long)ratio.Exponent + left.Exponent - right.Exponent);
    }

    /// <summary>Деление на небольшое целое — знаменатели членов ряда Тейлора.</summary>
    public static BigFloat operator /(BigFloat left, long right)
    {
        if (right == 0) throw new DivideByZeroException("Деление BigFloat на ноль.");
        if (left.IsZero) return Zero;
        BigFloat ratio = FromRatio(left.Mantissa, right);
        return Scaled(ratio.Mantissa, (long)ratio.Exponent + left.Exponent);
    }

    /// <summary>
    /// Квадратный корень с рабочей точностью. Нужен нечётной степени Симоноброта:
    /// <c>|z|ᵖ = M^(p/2) = Mᵠ·√M</c> при <c>p = 2q+1</c>, где <c>M = |z|²</c>.
    /// Отрицательный аргумент — ошибка вызывающего (в движке под корнем всегда сумма
    /// квадратов).
    /// </summary>
    public static BigFloat Sqrt(BigFloat value)
    {
        if (value.IsZero) return Zero;
        if (value.Sign < 0)
            throw new ArgumentOutOfRangeException(nameof(value), "Квадратный корень из отрицательного числа.");

        // √(m·2^e) = √(m·2^s) · 2^((e−s)/2). Сдвиг s подбирается так, чтобы (e−s) было
        // чётным, а у целочисленного корня оказалось на 8 бит больше рабочей точности:
        // погрешность «floor» целого корня — не более 1 младшего бита, то есть на восемь
        // разрядов ниже позиции округления, которое дальше делает конструктор.
        int targetBits = 2 * (WorkingPrecisionBits + 8);
        int shift = targetBits - (int)value.Mantissa.GetBitLength();
        if (shift < 0) shift = 0;
        if ((((long)value.Exponent - shift) & 1L) != 0) shift++;

        BigInteger root = IntegerSquareRoot(value.Mantissa << shift);
        return new BigFloat(root, (value.Exponent - shift) / 2);
    }

    /// <summary>
    /// ⌊√value⌋ методом Ньютона. Начальное приближение берётся из старших бит через
    /// <see cref="System.Math.Sqrt"/> (сразу ~50 верных бит), поэтому даже для
    /// восьмисотбитного аргумента хватает трёх-четырёх уточнений вместо трёх десятков.
    /// Приближение заведомо не меньше искомого корня — на этом держится монотонность
    /// итерации и её остановка ровно на ⌊√value⌋.
    /// </summary>
    private static BigInteger IntegerSquareRoot(BigInteger value)
    {
        if (value.Sign <= 0) return BigInteger.Zero;

        int bitLength = (int)value.GetBitLength();
        int headShift = bitLength > 53 ? (bitLength - 53) & ~1 : 0;
        double head = (double)(value >> headShift);
        // Слагаемые с запасом закрывают и отброшенный хвост, и округление double.
        BigInteger guess =
            ((BigInteger)System.Math.Ceiling(System.Math.Sqrt(head + 4.0)) + 2) << (headShift / 2);

        while (true)
        {
            BigInteger next = (guess + value / guess) >> 1;
            if (next >= guess) return guess;
            guess = next;
        }
    }

    public double ToDouble()
    {
        if (IsZero) return 0;
        int bitLength = (int)Mantissa.GetBitLength();
        if (bitLength <= 53)
            return (double)Mantissa * System.Math.ScaleB(1.0, Exponent);

        int shift = bitLength - 53;
        BigInteger reduced = Mantissa >> shift;
        return (double)reduced * System.Math.ScaleB(1.0, Exponent + shift);
    }

    /// <summary>Ближайшее <see cref="decimal"/>; при выходе за диапазон — насыщение к границе.</summary>
    public decimal ToDecimalClamped()
    {
        try
        {
            return decimal.Parse(ToInvariantString(29), NumberStyles.Float, CultureInfo.InvariantCulture);
        }
        catch (OverflowException)
        {
            return Sign < 0 ? decimal.MinValue : decimal.MaxValue;
        }
    }

    /// <summary>
    /// Round-trip строка: число дробных цифр берётся по фактической длине мантиссы этого
    /// значения (а не по <see cref="WorkingPrecisionBits"/> вызывающего потока), поэтому
    /// сверхглубокий центр сериализуется без потери точности.
    /// </summary>
    public string ToInvariantString()
    {
        int significantDigits = (int)System.Math.Ceiling(Mantissa.GetBitLength() * Log10Of2) + 8;
        return ToInvariantString(System.Math.Max(SerializationFractionDigits, significantDigits));
    }

    public string ToInvariantString(int maxFractionDigits)
    {
        if (IsZero) return "0";

        bool negative = Mantissa.Sign < 0;
        BigInteger magnitude = BigInteger.Abs(Mantissa);

        if (Exponent >= 0)
        {
            BigInteger integer = magnitude << Exponent;
            return negative ? "-" + integer.ToString(CultureInfo.InvariantCulture)
                            : integer.ToString(CultureInfo.InvariantCulture);
        }

        int fractionBits = -Exponent;
        BigInteger denominator = BigInteger.One << fractionBits;
        BigInteger integerPart = BigInteger.DivRem(magnitude, denominator, out BigInteger remainder);

        var fraction = new StringBuilder();
        int produced = 0;
        while (!remainder.IsZero && produced < maxFractionDigits)
        {
            remainder *= 10;
            BigInteger digit = BigInteger.DivRem(remainder, denominator, out remainder);
            fraction.Append((char)('0' + (int)digit));
            produced++;
        }

        // Округление последней выводимой цифры по остатку.
        if (!remainder.IsZero && (remainder << 1) >= denominator)
            RoundUpDecimalString(fraction, ref integerPart);

        while (fraction.Length > 0 && fraction[^1] == '0') fraction.Length--;

        var builder = new StringBuilder();
        if (negative) builder.Append('-');
        builder.Append(integerPart.ToString(CultureInfo.InvariantCulture));
        if (fraction.Length > 0) builder.Append('.').Append(fraction);
        return builder.ToString();
    }

    private static void RoundUpDecimalString(StringBuilder fraction, ref BigInteger integerPart)
    {
        int position = fraction.Length - 1;
        while (position >= 0)
        {
            if (fraction[position] == '9')
            {
                fraction[position] = '0';
                position--;
            }
            else
            {
                fraction[position]++;
                return;
            }
        }

        integerPart += 1;
    }

    public int CompareTo(BigFloat other)
    {
        BigFloat difference = this - other;
        return difference.Mantissa.Sign;
    }

    public static bool operator <(BigFloat left, BigFloat right) => left.CompareTo(right) < 0;
    public static bool operator >(BigFloat left, BigFloat right) => left.CompareTo(right) > 0;
    public static bool operator <=(BigFloat left, BigFloat right) => left.CompareTo(right) <= 0;
    public static bool operator >=(BigFloat left, BigFloat right) => left.CompareTo(right) >= 0;
    public static bool operator ==(BigFloat left, BigFloat right) => left.Equals(right);
    public static bool operator !=(BigFloat left, BigFloat right) => !left.Equals(right);

    public bool Equals(BigFloat other) => Mantissa == other.Mantissa && Exponent == other.Exponent;
    public override bool Equals(object? obj) => obj is BigFloat other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Mantissa, Exponent);
    public override string ToString() => ToInvariantString(40);
}
