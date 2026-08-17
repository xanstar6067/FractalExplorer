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
    public string Formula { get; init; } = "z";
    public double CenterX { get; init; }
    public double CenterY { get; init; }
    public double Zoom { get; init; } = 1;
    public DomainColoringMode ColoringMode { get; init; }
    public double HueCycles { get; init; } = 1;
    public double MagnitudeExposure { get; init; } = 1;
    public double RingDensity { get; init; } = 1;
    public int PhaseSectors { get; init; } = 12;
    public double ContourStrength { get; init; } = 0.55;
    public double Saturation { get; init; } = 0.9;
    public bool ShowAxes { get; init; }
    public Color InvalidColor { get; init; } = Colors.White;
}
