using System.Text.Json.Serialization;
using System.Windows.Media;
using Color = System.Windows.Media.Color;
using MediaColors = System.Windows.Media.Colors;

namespace FractalExplorerWPF.Models;

public enum MandelbrotVariant
{
    Mandelbrot,
    BurningShip,
    Tricorn,
    Buffalo,
    Celtic,
    Simonobrot,
    Generalized,
    Julia,
    JuliaBurningShip
}

public enum MandelbrotColoringMode
{
    Discrete,
    Smooth,
    Histogram,
    OrbitTrap,
    StripeAverage,
    SmoothEscapePolynomial,
    DistanceEstimation
}

public enum MandelbrotPaletteWrapMode
{
    Repeat,
    Clamp,
    Mirror
}

public sealed record MandelbrotVariantDefinition(
    MandelbrotVariant Variant,
    string DisplayName,
    string Identifier,
    decimal InitialCenterX,
    decimal InitialCenterY,
    decimal InitialZoom,
    bool HasPower = false,
    bool HasInversion = false,
    decimal DefaultPower = 2.0m,
    bool HasJuliaConstant = false,
    decimal DefaultJuliaReal = 0m,
    decimal DefaultJuliaImaginary = 0m)
{
    public static MandelbrotVariantDefinition For(MandelbrotVariant variant) => variant switch
    {
        MandelbrotVariant.Mandelbrot => new(variant, "Множество Мандельброта", "Mandelbrot", -0.5m, 0, 0.75m),
        MandelbrotVariant.BurningShip => new(variant, "Множество «Горящий корабль»", "MandelbrotBurningShip", 0, 0.5m, 0.75m),
        MandelbrotVariant.Tricorn => new(variant, "Трикорн (Mandelbar)", "Tricorn", 0, 0, 0.75m),
        MandelbrotVariant.Buffalo => new(variant, "Фрактал Буффало", "Buffalo", 0, 0, 0.75m),
        MandelbrotVariant.Celtic => new(variant, "Кельтский Мандельброт", "CelticMandelbrot", 0, 0, 0.75m),
        MandelbrotVariant.Simonobrot => new(variant, "Симоноброт", "Simonobrot", 0, 0, 0.75m, true, true, 2),
        MandelbrotVariant.Generalized => new(variant, "Обобщённый Мандельброт", "GeneralizedMandelbrot", 0, 0, 0.75m, true, false, 3),
        MandelbrotVariant.Julia => new(variant, "Классическое множество Жюлиа", "Julia", 0, 0, 0.75m,
            HasJuliaConstant: true, DefaultJuliaReal: -0.800m, DefaultJuliaImaginary: 0.156m),
        MandelbrotVariant.JuliaBurningShip => new(variant, "Горящий Корабль (Жюлиа)", "JuliaBurningShip", 0, 0, 0.75m,
            HasJuliaConstant: true, DefaultJuliaReal: -1.7551867961883m, DefaultJuliaImaginary: 0.01068m),
        _ => throw new ArgumentOutOfRangeException(nameof(variant))
    };
}

public sealed class MandelbrotPalette
{
    public string Name { get; set; } = "Новая палитра";
    public List<Color> Colors { get; set; } = [MediaColors.Black, MediaColors.White];
    public Color InteriorColor { get; set; } = MediaColors.Black;
    public bool IsGradient { get; set; } = true;
    public bool IsBuiltIn { get; set; }
    public double Gamma { get; set; } = 1.0;
    public int ColorPeriod { get; set; } = 500;
    public bool AlignWithRenderIterations { get; set; }

    public MandelbrotPalette Clone(string name) => new()
    {
        Name = name,
        Colors = [.. Colors],
        InteriorColor = InteriorColor,
        IsGradient = IsGradient,
        Gamma = Gamma,
        ColorPeriod = ColorPeriod,
        AlignWithRenderIterations = AlignWithRenderIterations
    };

    public override string ToString() => Name;
}

public sealed class MandelbrotState
{
    public string SaveName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public MandelbrotVariant Variant { get; set; }
    public decimal CenterX { get; set; }
    public decimal CenterY { get; set; }
    public decimal Zoom { get; set; } = 1;
    public int Iterations { get; set; } = 500;
    public decimal Threshold { get; set; } = 2;
    [JsonIgnore]
    public int Threads { get; set; }
    public MandelbrotColoringMode ColoringMode { get; set; } = MandelbrotColoringMode.Smooth;
    public string PaletteName { get; set; } = string.Empty;
    public MandelbrotPalette Palette { get; set; } = new();
    public decimal Power { get; set; } = 2;
    public bool UseInversion { get; set; }
    public decimal JuliaCReal { get; set; }
    public decimal JuliaCImaginary { get; set; }
    public double HistogramContrast { get; set; } = 1;
    public bool HistogramEnabledEqualization { get; set; } = true;
    public bool HistogramInputUseSmooth { get; set; } = true;
    public double SmoothBlendPower { get; set; } = 1;
    public double SmoothIterationOffset { get; set; }
    public double PalettePhaseOffset { get; set; }
    public double PaletteScale { get; set; } = 1;
    public MandelbrotPaletteWrapMode PaletteWrapMode { get; set; }
    public bool UseCustomInteriorColor { get; set; }
    public Color InteriorColor { get; set; } = MediaColors.Black;
    public double OrbitTrapStrength { get; set; } = 1;
    public double OrbitTrapBias { get; set; }
    public double StripeFrequency { get; set; } = 3;
    public double StripeStrength { get; set; } = 0.5;
    public double StripeBias { get; set; }
    public double PolynomialA { get; set; } = 9;
    public double PolynomialB { get; set; } = 15;
    public double PolynomialC { get; set; } = 8.5;
    public double PolynomialGamma { get; set; } = 1;
    public double PolynomialBlend { get; set; } = 1;
    public double PolynomialBias { get; set; }
    public double DistanceReliefStrength { get; set; } = 1.35;
    public double DistanceLightAzimuth { get; set; } = 135;
    public double DistanceLightElevation { get; set; } = 45;
    public double DistanceAmbient { get; set; } = 0.28;
    public double DistanceDiffuse { get; set; } = 0.9;
    public double DistanceSpecular { get; set; } = 0.3;
    public double DistanceShininess { get; set; } = 32;
    public bool DistanceContoursEnabled { get; set; } = true;
    public double DistanceContourSpacing { get; set; } = 12;
    public double DistanceContourStrength { get; set; } = 0.45;
}
