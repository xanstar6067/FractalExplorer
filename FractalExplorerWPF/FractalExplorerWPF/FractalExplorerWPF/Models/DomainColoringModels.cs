using System.Windows.Media;
using Color = System.Windows.Media.Color;

namespace FractalExplorerWPF.Models;

public enum DomainColoringMode
{
    SmoothMagnitude,
    LogarithmicRings,
    PhaseContours,
    PolarGrid,
    ArgumentOnly
}

public sealed class DomainColoringState
{
    public string SaveName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string FractalType { get; set; } = "DomainColoring";
    public string Formula { get; set; } = "z";
    public double CenterX { get; set; }
    public double CenterY { get; set; }
    public double Zoom { get; set; } = 1;
    public DomainColoringMode ColoringMode { get; set; }
    public double HueCycles { get; set; } = 1;
    public double MagnitudeExposure { get; set; } = 1;
    public double RingDensity { get; set; } = 1;
    public int PhaseSectors { get; set; } = 12;
    public double ContourStrength { get; set; } = 0.55;
    public double Saturation { get; set; } = 0.9;
    public bool ShowAxes { get; set; }
    public Color InvalidColor { get; set; } = Colors.White;
}
