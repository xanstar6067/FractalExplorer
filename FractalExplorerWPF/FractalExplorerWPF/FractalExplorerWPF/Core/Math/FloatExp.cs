namespace FractalExplorerWPF.Core.NewtonMath;

/// <summary>
/// Число с плавающей запятой и расширенным диапазоном экспоненты:
/// значение = <see cref="Mantissa"/> · 2^<see cref="Exponent"/>, где мантисса —
/// обычный <see cref="double"/>, нормализованный в диапазон [1; 2) (со знаком), а
/// экспонента — 32-битное целое. Даёт ту же ~52-битную относительную точность, что и
/// <see cref="double"/>, но без потолка ±1e308 и без потери значимости в денормалах.
///
/// Нужен «второму двигателю» глубокого зума Мандельброта: отклонение δ на пиксель на
/// зуме за ~1e72 перестаёт помещаться в обычный <see cref="double"/> (δ² уходит в
/// денормалы и в ноль). Набор операций минимален — сложение, вычитание, умножение и
/// конвертации, которых достаточно для рекуррентности δ' = 2·Z·δ + δ² + δc.
///
/// Опорная орбита при этом остаётся в <see cref="double"/>: её значения ограничены
/// радиусом бейлаута и в расширенном диапазоне не нуждаются.
/// </summary>
public readonly struct FloatExp
{
    /// <summary>Нормализованная мантисса: 0 либо |Mantissa| ∈ [1; 2).</summary>
    public readonly double Mantissa;

    /// <summary>Двоичная экспонента. Для нулевого значения — 0.</summary>
    public readonly int Exponent;

    // Разность экспонент, за которой меньшее слагаемое уже не влияет на 52-битную
    // мантиссу большего. 120 — с запасом больше 53 и не заходит в денормалы double.
    private const int NegligibleShift = 120;

    private FloatExp(double mantissa, int exponent)
    {
        Mantissa = mantissa;
        Exponent = exponent;
    }

    public static FloatExp Zero => default;

    public bool IsZero => Mantissa == 0.0;

    /// <summary>Нормализует произвольные mantissa·2^exponent в канонический вид.</summary>
    private static FloatExp Normalize(double mantissa, int exponent)
    {
        if (mantissa == 0.0) return default;
        if (!double.IsFinite(mantissa)) return new FloatExp(mantissa, 0);
        int shift = System.Math.ILogB(mantissa);
        return new FloatExp(System.Math.ScaleB(mantissa, -shift), exponent + shift);
    }

    public static FloatExp FromDouble(double value)
    {
        if (value == 0.0 || !double.IsFinite(value)) return value == 0.0 ? default : new FloatExp(value, 0);
        int shift = System.Math.ILogB(value);
        return new FloatExp(System.Math.ScaleB(value, -shift), shift);
    }

    /// <summary>
    /// Ближайший <see cref="double"/>. Слишком малое по модулю значение обращается в 0,
    /// слишком большое — в ±∞; и то и другое корректно для сравнений в пиксельном цикле
    /// (пренебрежимо мало / убежало за радиус).
    /// </summary>
    public double ToDouble() => IsZero ? 0.0 : System.Math.ScaleB(Mantissa, Exponent);

    public static FloatExp operator -(FloatExp value) =>
        value.IsZero ? default : new FloatExp(-value.Mantissa, value.Exponent);

    public static FloatExp operator +(FloatExp left, FloatExp right)
    {
        if (left.IsZero) return right;
        if (right.IsZero) return left;

        int difference = left.Exponent - right.Exponent;
        if (difference > NegligibleShift) return left;
        if (difference < -NegligibleShift) return right;

        return difference >= 0
            ? Normalize(left.Mantissa + System.Math.ScaleB(right.Mantissa, -difference), left.Exponent)
            : Normalize(System.Math.ScaleB(left.Mantissa, difference) + right.Mantissa, right.Exponent);
    }

    public static FloatExp operator -(FloatExp left, FloatExp right) => left + (-right);

    public static FloatExp operator *(FloatExp left, FloatExp right)
    {
        if (left.IsZero || right.IsZero) return default;
        // Мантиссы в [1; 2) ⇒ произведение в [1; 4): одной нормализации достаточно.
        return Normalize(left.Mantissa * right.Mantissa, left.Exponent + right.Exponent);
    }

    public static FloatExp operator *(FloatExp left, double right) => left * FromDouble(right);
    public static FloatExp operator *(double left, FloatExp right) => FromDouble(left) * right;

    /// <summary>|re|² + |im|² как <see cref="FloatExp"/> — без промежуточного переполнения диапазона.</summary>
    public static FloatExp MagnitudeSquared(FloatExp re, FloatExp im) => re * re + im * im;
}
