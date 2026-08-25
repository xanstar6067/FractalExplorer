namespace FractalExplorerWPF.Models;

public enum PhoenixPlaneMode
{
    Julia,
    ParameterC1
}

public enum PhoenixVariant
{
    Classic,
    Tricorn,
    BurningShip,
    Celtic,
    Buffalo
}

public enum PhoenixColoringMode
{
    Discrete,
    Smooth,
    OrbitTrap,
    StripeAverage,
    TriangleInequalityAverage,
    FinalArgument,
    Period
}

public enum PhoenixOrbitTrapMode
{
    Axes,
    Circle,
    Point
}

public enum PhoenixParameterPlane
{
    C1,
    C2
}

public sealed class PhoenixState
{
    public string SaveName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string FractalType { get; set; } = "Phoenix";
    public decimal CenterX { get; set; }
    public decimal CenterY { get; set; }
    public decimal Zoom { get; set; } = 1;
    public decimal Threshold { get; set; } = 4;
    public int Iterations { get; set; } = 100;
    public decimal C1Real { get; set; } = 0.56m;
    public decimal C1Imaginary { get; set; }
    public decimal C2Real { get; set; } = -0.5m;
    public decimal C2Imaginary { get; set; }
    public PhoenixPlaneMode PlaneMode { get; set; } = PhoenixPlaneMode.Julia;
    public PhoenixVariant Variant { get; set; } = PhoenixVariant.Classic;
    public int PrimaryPower { get; set; } = 2;
    public int SecondaryPower { get; set; }
    public decimal InitialZReal { get; set; }
    public decimal InitialZImaginary { get; set; }
    public decimal InitialPreviousReal { get; set; }
    public decimal InitialPreviousImaginary { get; set; }
    public PhoenixColoringMode ColoringMode { get; set; } = PhoenixColoringMode.Smooth;
    public PhoenixOrbitTrapMode OrbitTrapMode { get; set; } = PhoenixOrbitTrapMode.Axes;
    public double OrbitTrapRadius { get; set; } = 0.5;
    public double OrbitTrapStrength { get; set; } = 1.5;
    public double StripeFrequency { get; set; } = 3;
    public double StripeStrength { get; set; } = 0.65;
    public double CycleTolerance { get; set; } = 1e-7;
    public int MaximumDetectedPeriod { get; set; } = 32;
    public MandelbrotPalette Palette { get; set; } = new();
}

public sealed class PhoenixSliceRange
{
    public double MinX { get; set; } = -2;
    public double MaxX { get; set; } = 2;
    public double MinY { get; set; } = -2;
    public double MaxY { get; set; } = 2;

    public void Reset() => (MinX, MaxX, MinY, MaxY) = (-2, 2, -2, 2);
}
