using System.Globalization;
using FractalExplorer.Utilities;

namespace FractalExplorerWPF.Studio.Dsl;

public readonly record struct StudioComplexDouble(double Real, double Imaginary)
{
    public double NormSquared => Real * Real + Imaginary * Imaginary;

    public static StudioComplexDouble operator +(StudioComplexDouble left, StudioComplexDouble right) =>
        new(left.Real + right.Real, left.Imaginary + right.Imaginary);

    public static StudioComplexDouble operator -(StudioComplexDouble left, StudioComplexDouble right) =>
        new(left.Real - right.Real, left.Imaginary - right.Imaginary);

    public static StudioComplexDouble operator -(StudioComplexDouble value) => new(-value.Real, -value.Imaginary);

    public static StudioComplexDouble operator *(StudioComplexDouble left, StudioComplexDouble right) =>
        new(left.Real * right.Real - left.Imaginary * right.Imaginary,
            left.Real * right.Imaginary + left.Imaginary * right.Real);

    public static StudioComplexDouble operator /(StudioComplexDouble left, StudioComplexDouble right)
    {
        double denominator = right.NormSquared;
        if (denominator == 0)
            throw new DivideByZeroException("Деление на нулевое комплексное число.");
        return new StudioComplexDouble(
            (left.Real * right.Real + left.Imaginary * right.Imaginary) / denominator,
            (left.Imaginary * right.Real - left.Real * right.Imaginary) / denominator);
    }

    public static StudioComplexDouble Pow(StudioComplexDouble value, double power)
    {
        if (value.NormSquared == 0)
            return power == 0 ? new StudioComplexDouble(1, 0) : new StudioComplexDouble(0, 0);
        double magnitude = Math.Pow(Math.Sqrt(value.NormSquared), power);
        double angle = Math.Atan2(value.Imaginary, value.Real) * power;
        return new StudioComplexDouble(magnitude * Math.Cos(angle), magnitude * Math.Sin(angle));
    }
}

public readonly record struct StudioComplexDecimal(decimal Real, decimal Imaginary)
{
    public decimal NormSquared => Real * Real + Imaginary * Imaginary;

    public static StudioComplexDecimal operator +(StudioComplexDecimal left, StudioComplexDecimal right) =>
        new(left.Real + right.Real, left.Imaginary + right.Imaginary);

    public static StudioComplexDecimal operator -(StudioComplexDecimal left, StudioComplexDecimal right) =>
        new(left.Real - right.Real, left.Imaginary - right.Imaginary);

    public static StudioComplexDecimal operator -(StudioComplexDecimal value) => new(-value.Real, -value.Imaginary);

    public static StudioComplexDecimal operator *(StudioComplexDecimal left, StudioComplexDecimal right) =>
        new(left.Real * right.Real - left.Imaginary * right.Imaginary,
            left.Real * right.Imaginary + left.Imaginary * right.Real);

    public static StudioComplexDecimal operator /(StudioComplexDecimal left, StudioComplexDecimal right)
    {
        decimal denominator = right.NormSquared;
        if (denominator == 0)
            throw new DivideByZeroException("Деление на нулевое комплексное число.");
        return new StudioComplexDecimal(
            (left.Real * right.Real + left.Imaginary * right.Imaginary) / denominator,
            (left.Imaginary * right.Real - left.Real * right.Imaginary) / denominator);
    }

    public static StudioComplexDecimal Pow(StudioComplexDecimal value, decimal power)
    {
        if (value.NormSquared == 0)
            return power == 0 ? new StudioComplexDecimal(1, 0) : new StudioComplexDecimal(0, 0);
        decimal magnitude = DecimalMath.Pow(DecimalMath.Sqrt(value.NormSquared), power);
        decimal angle = DecimalMath.Atan2(value.Imaginary, value.Real) * power;
        return new StudioComplexDecimal(
            magnitude * DecimalMath.Cos(angle),
            magnitude * DecimalMath.Sin(angle));
    }
}

public readonly record struct StudioOrbitSample(
    int Iterations,
    bool Escaped,
    double ZReal,
    double ZImaginary,
    double NormSquared,
    double SmoothIteration,
    bool IsValid)
{
    public static StudioOrbitSample FromDouble(
        int iterations,
        bool escaped,
        StudioComplexDouble value)
    {
        double norm = value.NormSquared;
        return new StudioOrbitSample(
            iterations,
            escaped,
            value.Real,
            value.Imaginary,
            norm,
            ComputeSmooth(iterations, escaped, norm),
            double.IsFinite(value.Real) && double.IsFinite(value.Imaginary));
    }

    public static StudioOrbitSample FromDecimal(
        int iterations,
        bool escaped,
        StudioComplexDecimal value)
    {
        double real = (double)value.Real;
        double imaginary = (double)value.Imaginary;
        double norm = (double)value.NormSquared;
        return new StudioOrbitSample(
            iterations,
            escaped,
            real,
            imaginary,
            norm,
            ComputeSmooth(iterations, escaped, norm),
            true);
    }

    public static StudioOrbitSample Invalid(int iterations) =>
        new(iterations, false, 0, 0, 0, iterations, false);

    private static double ComputeSmooth(int iterations, bool escaped, double normSquared)
    {
        if (!escaped || normSquared <= 1 || !double.IsFinite(normSquared))
            return iterations;
        double logMagnitude = Math.Log(normSquared) / 2;
        double correction = Math.Log(Math.Max(logMagnitude, 1e-300)) / Math.Log(2);
        double result = iterations + 1 - correction;
        return double.IsFinite(result) ? result : iterations;
    }
}

public sealed class StudioDoubleParameterSet
{
    public required double[] Reals { get; init; }
    public required int[] Integers { get; init; }
    public required StudioComplexDouble[] Complexes { get; init; }
    public required bool[] Booleans { get; init; }
}

public sealed class StudioDecimalParameterSet
{
    public required decimal[] Reals { get; init; }
    public required int[] Integers { get; init; }
    public required StudioComplexDecimal[] Complexes { get; init; }
    public required bool[] Booleans { get; init; }
}

public delegate StudioOrbitSample StudioDoubleKernel(
    double pixelReal,
    double pixelImaginary,
    double[] realParameters,
    int[] integerParameters,
    StudioComplexDouble[] complexParameters,
    bool[] booleanParameters);

public delegate StudioOrbitSample StudioDecimalKernel(
    decimal pixelReal,
    decimal pixelImaginary,
    decimal[] realParameters,
    int[] integerParameters,
    StudioComplexDecimal[] complexParameters,
    bool[] booleanParameters);

internal static class StudioFormulaValueParser
{
    public static int Integer(string text, string name) =>
        int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
            ? value
            : throw new FormatException($"Параметр «{name}» должен быть целым числом.");

    public static double Double(string text, string name) =>
        double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double value) &&
        double.IsFinite(value)
            ? value
            : throw new FormatException($"Параметр «{name}» должен быть конечным числом.");

    public static decimal Decimal(string text, string name) =>
        decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal value)
            ? value
            : throw new FormatException($"Параметр «{name}» должен быть decimal-числом.");

    public static bool Boolean(string text, string name) =>
        bool.TryParse(text, out bool value)
            ? value
            : throw new FormatException($"Параметр «{name}» должен иметь значение true или false.");
}
