using System.Collections.Concurrent;
using System.Numerics;

namespace FractalExplorerWPF.Core.NewtonMath;

/// <summary>
/// Трансцендентные функции над <see cref="BigFloat"/>: π, экспонента, синус с косинусом и
/// их гиперболические напарники.
///
/// Набор появился ради глубокого зума Коллатца. Его формула
/// <c>z ← a + b·z + (c + d·z)·cos(πz)</c> трансцендентна, поэтому пертурбация ей не подходит:
/// орбита считается напрямую, и каждый шаг требует sin/cos/ch/sh с полной рабочей точностью.
/// Мандельбротовскому «второму двигателю» этот файл не нужен и им не используется.
///
/// Общие принципы всех методов:
/// <list type="bullet">
/// <item>внутри открывается <see cref="BigFloat.PrecisionScope"/> с запасом
/// <see cref="GuardBits"/> (плюс запас под усиление ошибки при возведениях в квадрат), а
/// результат округляется до точности вызывающего уже за её пределами;</item>
/// <item>аргумент приводится делением на 2^k — операция точная (см.
/// <see cref="BigFloat.ScaleByPowerOfTwo"/>), — затем ряд Тейлора, затем возврат формулами
/// удвоения. Глубина приведения <see cref="SeriesSplit"/> ≈ √P уравновешивает число членов
/// ряда (≈P/2m) и число удвоений (≈m);</item>
/// <item>ряд обрывается по <see cref="BigFloat.BinaryExponent"/> очередного члена, без
/// вычитаний и без лишних выделений памяти. Для малого аргумента это даёт ранний выход:
/// у самой вещественной оси, где живёт вся структура Коллатца, ch/sh стоят почти нуля.</item>
/// </list>
/// </summary>
public static class BigFloatMath
{
    /// <summary>Запас точности, с которым считается любая промежуточная величина.</summary>
    private const int GuardBits = 32;

    // π зависит только от точности, поэтому считается один раз на каждое встреченное
    // значение WorkingPrecisionBits и переиспользуется всеми потоками рендера.
    private static readonly ConcurrentDictionary<int, BigFloat> PiCache = new();

    private static readonly BigFloat Half = BigFloat.ScaleByPowerOfTwo(BigFloat.One, -1);
    private static readonly BigFloat Quarter = BigFloat.ScaleByPowerOfTwo(BigFloat.One, -2);

    /// <summary>π с текущей рабочей точностью.</summary>
    public static BigFloat Pi => PiCache.GetOrAdd(BigFloat.WorkingPrecisionBits, ComputePi);

    /// <summary>
    /// π по формуле Мэчина: π = 16·arctg(1/5) − 4·arctg(1/239). Оба арктангенса считаются
    /// в целых числах с фиксированной запятой, поэтому деления идут нацело и промежуточных
    /// округлений нет вовсе.
    /// </summary>
    private static BigFloat ComputePi(int bits)
    {
        using var precision = new BigFloat.PrecisionScope(bits);
        int scaleBits = bits + 64;
        BigInteger value = 16 * ArctanReciprocal(5, scaleBits) - 4 * ArctanReciprocal(239, scaleBits);
        return BigFloat.FromScaled(value, -scaleBits);
    }

    /// <summary>arctg(1/inverse), умноженный на 2^scaleBits: ряд Σ (−1)^k / ((2k+1)·x^(2k+1)).</summary>
    private static BigInteger ArctanReciprocal(int inverse, int scaleBits)
    {
        BigInteger power = (BigInteger.One << scaleBits) / inverse;
        BigInteger sum = power;
        BigInteger squared = (BigInteger)inverse * inverse;
        for (int term = 1; ; term++)
        {
            power /= squared;
            if (power.IsZero) return sum;
            BigInteger value = power / (2 * term + 1);
            if ((term & 1) == 1) sum -= value;
            else sum += value;
        }
    }

    /// <summary>
    /// Глубина приведения аргумента: столько младших двоичных порядков «срезается» перед
    /// рядом Тейлора. Оптимум по суммарному числу умножений — около √P.
    /// </summary>
    private static int SeriesSplit(int precisionBits) =>
        System.Math.Clamp((int)System.Math.Sqrt(precisionBits), 8, 32);

    /// <summary>
    /// Экспонента. Аргумент делится на 2^k до модуля не больше 2^−√P, дальше прямой ряд и k
    /// возведений в квадрат. Каждое возведение удваивает относительную погрешность, поэтому
    /// в рабочую точность добавляется ровно k бит.
    /// </summary>
    public static BigFloat Exp(BigFloat value)
    {
        if (value.IsZero) return BigFloat.One;

        int precision = BigFloat.WorkingPrecisionBits;
        int reduction = System.Math.Max(0, value.BinaryExponent + SeriesSplit(precision));
        BigFloat result;
        using (var scope = new BigFloat.PrecisionScope(precision + GuardBits + reduction))
        {
            BigFloat reduced = BigFloat.ScaleByPowerOfTwo(value, -reduction);
            int cutoff = -(precision + GuardBits);
            BigFloat term = BigFloat.One;
            result = BigFloat.One;
            for (long n = 1; ; n++)
            {
                term = term * reduced / n;
                if (term.IsZero || term.BinaryExponent < cutoff) break;
                result += term;
            }
            for (int i = 0; i < reduction; i++) result *= result;
        }
        return BigFloat.FromScaled(result.Mantissa, result.Exponent);
    }

    /// <summary>
    /// Синус и косинус от π·<paramref name="value"/> — именно в таком виде их требует
    /// формула Коллатца. Приведение по периоду делается над самим аргументом (вычитание
    /// ближайшего чётного целого — точная операция), а не делением на приближённое 2π,
    /// поэтому значащие цифры не теряются даже когда аргумент много больше единицы.
    /// </summary>
    public static void SinCosPi(BigFloat value, out BigFloat sin, out BigFloat cos)
    {
        int precision = BigFloat.WorkingPrecisionBits;
        BigFloat resultSin, resultCos;
        using (var scope = new BigFloat.PrecisionScope(precision + GuardBits + 16))
        {
            // Период 2: sin/cos(πx) зависят только от x mod 2.
            BigFloat reduced = value - BigFloat.ScaleByPowerOfTwo(
                Round(BigFloat.ScaleByPowerOfTwo(value, -1)), 1);

            // Полупериод: сдвиг на 1 меняет знак и синуса, и косинуса.
            bool negate = false;
            if (reduced > Half) { reduced -= BigFloat.One; negate = true; }
            else if (reduced < -Half) { reduced += BigFloat.One; negate = true; }

            // Четверть: при |x| > 1/4 берём дополнение до 1/2 и меняем синус с косинусом
            // местами. После этого угол не превосходит π/4, и на удвоениях косинус не
            // проходит близко к нулю — cos(2u) = 1 − 2sin²u считается без потери старших цифр.
            int sign = reduced.Sign;
            BigFloat turns = BigFloat.Abs(reduced);
            bool swap = turns > Quarter;
            if (swap) turns = Half - turns;

            BigFloat angle = Pi * turns;
            int doublings = System.Math.Max(0, angle.BinaryExponent + SeriesSplit(precision));
            SinCosSeries(BigFloat.ScaleByPowerOfTwo(angle, -doublings), precision + GuardBits,
                out BigFloat sine, out BigFloat cosine);
            for (int i = 0; i < doublings; i++)
            {
                BigFloat doubledSine = BigFloat.ScaleByPowerOfTwo(sine * cosine, 1);
                cosine = BigFloat.One - BigFloat.ScaleByPowerOfTwo(sine * sine, 1);
                sine = doubledSine;
            }

            if (swap) (sine, cosine) = (cosine, sine);
            if (sign < 0) sine = -sine;
            if (negate) { sine = -sine; cosine = -cosine; }
            resultSin = sine;
            resultCos = cosine;
        }
        sin = BigFloat.FromScaled(resultSin.Mantissa, resultSin.Exponent);
        cos = BigFloat.FromScaled(resultCos.Mantissa, resultCos.Exponent);
    }

    /// <summary>Синус и косинус в радианах. Приведение по периоду делает <see cref="SinCosPi"/>.</summary>
    public static void SinCos(BigFloat angle, out BigFloat sin, out BigFloat cos) =>
        SinCosPi(angle / Pi, out sin, out cos);

    /// <summary>
    /// Ряды sin и cos для уже приведённого малого угла. Порог обрыва у каждого свой:
    /// синус меряется относительно самого угла, косинус — относительно единицы, иначе
    /// синусный ряд считал бы лишние члены.
    /// </summary>
    private static void SinCosSeries(BigFloat angle, int significantBits, out BigFloat sin, out BigFloat cos)
    {
        BigFloat squared = angle * angle;

        int sinCutoff = angle.BinaryExponent - significantBits;
        BigFloat term = angle;
        sin = angle;
        for (long n = 1; ; n++)
        {
            term = -(term * squared) / (2 * n * (2 * n + 1));
            if (term.IsZero || term.BinaryExponent < sinCutoff) break;
            sin += term;
        }

        term = BigFloat.One;
        cos = BigFloat.One;
        for (long n = 1; ; n++)
        {
            term = -(term * squared) / ((2 * n - 1) * (2 * n));
            if (term.IsZero || term.BinaryExponent < -significantBits) break;
            cos += term;
        }
    }

    /// <summary>
    /// Гиперболические синус и косинус — той же схемой, что и тригонометрические: аргумент
    /// делится на 2^k, дальше ряды и k удвоений (sh(2u) = 2·sh·ch, ch(2u) = 1 + 2·sh²; оба
    /// слагаемых положительны, вычитания близких величин нет). Через экспоненту было бы
    /// дороже: понадобилось бы ещё и полноразрядное деление на неё.
    ///
    /// У самой вещественной оси, где живёт вся структура Коллатца, аргумент π·Im z ничтожен,
    /// приведение не нужно вовсе, и ряд обрывается на первом-втором члене.
    /// </summary>
    public static void SinhCosh(BigFloat value, out BigFloat sinh, out BigFloat cosh)
    {
        if (value.IsZero) { sinh = BigFloat.Zero; cosh = BigFloat.One; return; }

        int precision = BigFloat.WorkingPrecisionBits;
        int doublings = System.Math.Max(0, value.BinaryExponent + SeriesSplit(precision));
        BigFloat resultSinh, resultCosh;
        using (var scope = new BigFloat.PrecisionScope(precision + GuardBits + doublings))
        {
            BigFloat reduced = BigFloat.ScaleByPowerOfTwo(value, -doublings);
            int significantBits = precision + GuardBits;
            BigFloat squared = reduced * reduced;

            int sinhCutoff = reduced.BinaryExponent - significantBits;
            BigFloat term = reduced;
            resultSinh = reduced;
            for (long n = 1; ; n++)
            {
                term = term * squared / (2 * n * (2 * n + 1));
                if (term.IsZero || term.BinaryExponent < sinhCutoff) break;
                resultSinh += term;
            }

            term = BigFloat.One;
            resultCosh = BigFloat.One;
            for (long n = 1; ; n++)
            {
                term = term * squared / ((2 * n - 1) * (2 * n));
                if (term.IsZero || term.BinaryExponent < -significantBits) break;
                resultCosh += term;
            }

            for (int index = 0; index < doublings; index++)
            {
                BigFloat doubledSinh = BigFloat.ScaleByPowerOfTwo(resultSinh * resultCosh, 1);
                resultCosh = BigFloat.One + BigFloat.ScaleByPowerOfTwo(resultSinh * resultSinh, 1);
                resultSinh = doubledSinh;
            }
        }
        sinh = BigFloat.FromScaled(resultSinh.Mantissa, resultSinh.Exponent);
        cosh = BigFloat.FromScaled(resultCosh.Mantissa, resultCosh.Exponent);
    }

    /// <summary>
    /// Ближайшее целое; половина округляется от нуля. Точка ровно на середине для рендера
    /// недостижима, поэтому выбор правила здесь ни на что не влияет.
    /// </summary>
    public static BigFloat Round(BigFloat value)
    {
        if (value.IsZero || value.Exponent >= 0) return value;

        // |value| < 1 обрабатывается отдельно: у такого значения экспонента может быть
        // сколь угодно маленькой, и сдвиг на −Exponent построил бы гигантское число.
        if (value.BinaryExponent <= 0)
        {
            if (BigFloat.Abs(value) < Half) return BigFloat.Zero;
            return value.Sign < 0 ? -BigFloat.One : BigFloat.One;
        }

        int shift = -value.Exponent;
        BigInteger magnitude = BigInteger.Abs(value.Mantissa);
        magnitude = (magnitude + (BigInteger.One << (shift - 1))) >> shift;
        return BigFloat.FromScaled(value.Sign < 0 ? -magnitude : magnitude, 0);
    }
}
