using System.Numerics;

namespace FractalExplorerWPF.Core.NewtonMath;

/// <summary>
/// Комплексное число с компонентами <see cref="BigFloat"/> — ровно тот минимум операций,
/// который нужен прямой итерации Коллатца на глубоком зуме: сложение, вычитание, умножение
/// (на комплексное, на вещественное и на небольшое целое), деление на небольшое целое и
/// комплексные sin/cos от π·z.
///
/// Аналог <see cref="FractalExplorer.Utilities.ComplexDecimal"/> для ступени, где decimal
/// уже не хватает.
/// </summary>
public readonly struct ComplexBigFloat : IEquatable<ComplexBigFloat>
{
    public BigFloat Real { get; }
    public BigFloat Imaginary { get; }

    public ComplexBigFloat(BigFloat real, BigFloat imaginary)
    {
        Real = real;
        Imaginary = imaginary;
    }

    public static ComplexBigFloat Zero => default;

    public static ComplexBigFloat FromDouble(double real, double imaginary) =>
        new(BigFloat.FromDouble(real), BigFloat.FromDouble(imaginary));

    public static ComplexBigFloat FromDecimal(decimal real, decimal imaginary) =>
        new(BigFloat.FromDecimal(real), BigFloat.FromDecimal(imaginary));

    public BigFloat MagnitudeSquared => Real * Real + Imaginary * Imaginary;

    public Complex ToComplex() => new(Real.ToDouble(), Imaginary.ToDouble());

    /// <summary>Умножение обеих компонент на 2^shift — точная операция.</summary>
    public static ComplexBigFloat ScaleByPowerOfTwo(ComplexBigFloat value, int shift) =>
        new(BigFloat.ScaleByPowerOfTwo(value.Real, shift),
            BigFloat.ScaleByPowerOfTwo(value.Imaginary, shift));

    public bool Equals(ComplexBigFloat other) =>
        Real.Equals(other.Real) && Imaginary.Equals(other.Imaginary);

    public override bool Equals(object? obj) => obj is ComplexBigFloat other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Real, Imaginary);

    public static ComplexBigFloat operator -(ComplexBigFloat value) =>
        new(-value.Real, -value.Imaginary);

    public static ComplexBigFloat operator +(ComplexBigFloat left, ComplexBigFloat right) =>
        new(left.Real + right.Real, left.Imaginary + right.Imaginary);

    public static ComplexBigFloat operator -(ComplexBigFloat left, ComplexBigFloat right) =>
        new(left.Real - right.Real, left.Imaginary - right.Imaginary);

    public static ComplexBigFloat operator *(ComplexBigFloat left, ComplexBigFloat right) =>
        new(left.Real * right.Real - left.Imaginary * right.Imaginary,
            left.Real * right.Imaginary + left.Imaginary * right.Real);

    public static ComplexBigFloat operator *(ComplexBigFloat left, BigFloat right) =>
        new(left.Real * right, left.Imaginary * right);

    public static ComplexBigFloat operator *(BigFloat left, ComplexBigFloat right) => right * left;

    public static ComplexBigFloat operator *(ComplexBigFloat left, long right) =>
        new(left.Real * right, left.Imaginary * right);

    public static ComplexBigFloat operator /(ComplexBigFloat left, long right) =>
        new(left.Real / right, left.Imaginary / right);

    public static ComplexBigFloat operator +(ComplexBigFloat left, long right) =>
        new(left.Real + BigFloat.FromInt(right), left.Imaginary);

    public static ComplexBigFloat operator -(ComplexBigFloat left, long right) =>
        new(left.Real - BigFloat.FromInt(right), left.Imaginary);

    public static ComplexBigFloat operator +(long left, ComplexBigFloat right) => right + left;

    public static ComplexBigFloat operator -(long left, ComplexBigFloat right) =>
        new(BigFloat.FromInt(left) - right.Real, -right.Imaginary);

    /// <summary>
    /// sin(πz) и cos(πz) за один проход: вещественная часть даёт sin/cos, мнимая — sh/ch,
    /// и обе пары переиспользуются обеими функциями. Формулы Коллатца просят то одну из них,
    /// то обе, а дорогая часть у них общая.
    /// </summary>
    public static void SinCosPi(ComplexBigFloat value, out ComplexBigFloat sin, out ComplexBigFloat cos)
    {
        BigFloatMath.SinCosPi(value.Real, out BigFloat sine, out BigFloat cosine);
        BigFloatMath.SinhCosh(BigFloatMath.Pi * value.Imaginary,
            out BigFloat hyperbolicSine, out BigFloat hyperbolicCosine);
        sin = new ComplexBigFloat(sine * hyperbolicCosine, cosine * hyperbolicSine);
        cos = new ComplexBigFloat(cosine * hyperbolicCosine, -(sine * hyperbolicSine));
    }

    /// <summary>
    /// cos(πz), когда синус вызывающему не нужен. Вещественные sin/cos и ch/sh всё равно
    /// нужны оба, а вот собирать из них вторую комплексную величину незачем.
    /// </summary>
    public static ComplexBigFloat CosPi(ComplexBigFloat value)
    {
        BigFloatMath.SinCosPi(value.Real, out BigFloat sine, out BigFloat cosine);
        BigFloatMath.SinhCosh(BigFloatMath.Pi * value.Imaginary,
            out BigFloat hyperbolicSine, out BigFloat hyperbolicCosine);
        return new ComplexBigFloat(cosine * hyperbolicCosine, -(sine * hyperbolicSine));
    }

    /// <summary>sin(πz), когда косинус вызывающему не нужен.</summary>
    public static ComplexBigFloat SinPi(ComplexBigFloat value)
    {
        BigFloatMath.SinCosPi(value.Real, out BigFloat sine, out BigFloat cosine);
        BigFloatMath.SinhCosh(BigFloatMath.Pi * value.Imaginary,
            out BigFloat hyperbolicSine, out BigFloat hyperbolicCosine);
        return new ComplexBigFloat(sine * hyperbolicCosine, cosine * hyperbolicSine);
    }

    public override string ToString() =>
        $"{Real.ToInvariantString(30)} + {Imaginary.ToInvariantString(30)}i";
}
