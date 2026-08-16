using System.Windows.Media;
using Color = System.Windows.Media.Color;

namespace FractalExplorerWPF.Models;

public enum CollatzVariation
{
    Standard = 0,
    SineVariation = 1,

    // Value 2 used to be named GeneralizedP. Keep its numeric value so existing
    // saves continue to render with the formula with which they were created.
    ParityBranchVariation = 2,
    GeneralizedP = 3,
    GeneralizedPQ = 4
}

public enum CollatzColoringMode
{
    EscapeTime = 0,
    FinalArgument = 1,
    FinalMagnitude = 2,
    CycleBasins = 3,
    IntegerTrap = 4,
    RealAxisTrap = 5,
    OrbitDensity = 6,
    PeriodDetection = 7
}

public enum CollatzInteriorFillMode
{
    ByColoringMode = 0,
    Auto = 1,
    Black = 2,
    White = 3,
    Custom = 4
}

public sealed class CollatzState
{
    public string SaveName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string FractalType { get; set; } = "Collatz";
    public decimal CenterX { get; set; }
    public decimal CenterY { get; set; }
    public decimal Zoom { get; set; } = 1;
    public decimal Threshold { get; set; } = 100;
    public int Iterations { get; set; } = 150;
    public CollatzVariation Variation { get; set; }
    public decimal PParameter { get; set; } = 3;
    public decimal QRealParameter { get; set; }
    public decimal QImaginaryParameter { get; set; }
    public CollatzColoringMode ColoringMode { get; set; }
    public bool UseSmoothColoring { get; set; } = true;
    public double ArgumentCycles { get; set; } = 1;
    public double MagnitudeScale { get; set; } = 1;
    public double TrapScale { get; set; } = 4;
    public double CycleTolerance { get; set; } = 1e-6;
    public int MaximumDetectedPeriod { get; set; } = 32;
    public double OrbitDensityExposure { get; set; } = 1;
    public int OrbitDensitySampleStep { get; set; } = 2;
    public bool OrbitDensityEscapedOnly { get; set; } = true;
    public CollatzInteriorFillMode InteriorFillMode { get; set; }
    public Color CustomInteriorColor { get; set; } = Colors.Black;
    public MandelbrotPalette Palette { get; set; } = new();
}
