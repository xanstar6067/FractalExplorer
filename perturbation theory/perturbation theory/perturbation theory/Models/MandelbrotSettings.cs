namespace perturbation_theory.Models;

public enum PrecisionMode
{
    Double,
    DecimalReference,
    Decimal
}

public enum ColoringMode { Discrete, Smooth }

public sealed record MandelbrotSettings
{
    public decimal CenterX { get; init; } = -0.5m;
    public decimal CenterY { get; init; }
    public decimal Zoom { get; init; } = 0.75m;
    public int Iterations { get; init; } = 500;
    public decimal EscapeRadius { get; init; } = 2m;
    public int Threads { get; init; }
    public PrecisionMode Precision { get; init; } = PrecisionMode.DecimalReference;
    public ColoringMode Coloring { get; init; } = ColoringMode.Smooth;
    public BuiltInPalette Palette { get; init; } = BuiltInPalette.All[0];
    public int ColorPeriod { get; init; } = 800;

    // decimal is fixed precision, not arbitrary precision. Keep guard digits for navigation.
    public const decimal MinZoom = 0.01m;
    public const decimal MaxZoom = 1_000_000_000_000_000_000_000_000m;
    public const decimal MinPixelStep = 0.000000000000000000000000001m;

    public void Validate()
    {
        if (CenterX is < -1000m or > 1000m || CenterY is < -1000m or > 1000m)
            throw new ArgumentException("Координаты центра должны быть от −1000 до 1000.");
        if (Zoom < MinZoom || Zoom > MaxZoom)
            throw new ArgumentException("Приближение должно быть от 0.01 до 1e24.");
        if (Iterations is < 1 or > 1_000_000)
            throw new ArgumentException("Число итераций должно быть от 1 до 1000000.");
        if (EscapeRadius is < 2m or > 1000m)
            throw new ArgumentException("Порог выхода должен быть от 2 до 1000.");
        if (Threads < 0 || Threads > Environment.ProcessorCount)
            throw new ArgumentException("Недопустимое число потоков.");
        if (ColorPeriod is < 1 or > 1_000_000)
            throw new ArgumentException("Период палитры должен быть от 1 до 1000000.");
        if (!Enum.IsDefined(Precision) || !Enum.IsDefined(Coloring) || Palette is null)
            throw new ArgumentException("Не выбран режим рендера или палитра.");
    }

    public decimal PixelStep(int width)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
        return (3m / Zoom) / width;
    }

    public void ValidateSurface(int width, int height)
    {
        Validate();
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 1);
        if ((long)width * height > 64_000_000)
            throw new ArgumentException("Полотно слишком велико (максимум 64 млн пикселей).");
        decimal step = PixelStep(width);
        if (step < MinPixelStep || CenterX + step == CenterX || CenterY + step == CenterY)
            throw new ArgumentException("Достигнут предел координат decimal для этого размера полотна. Уменьшите приближение.");
    }
}
