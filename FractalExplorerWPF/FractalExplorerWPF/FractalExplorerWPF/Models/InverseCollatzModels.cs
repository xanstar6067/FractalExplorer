using System.Windows.Media;
using Color = System.Windows.Media.Color;

namespace FractalExplorerWPF.Models;

public enum InverseCollatzLayout
{
    Radial,
    Layered
}

public enum InverseCollatzFilterBehavior
{
    Highlight,
    OnlyMatching
}

public sealed class InverseCollatzState
{
    public string SaveName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public int Depth { get; set; } = 28;
    public int VisibleDepth { get; set; } = 28;
    public int MaxNodes { get; set; } = 100_000;
    public InverseCollatzLayout Layout { get; set; }
    public int Modulus { get; set; }
    public int Residue { get; set; } = -1;
    public InverseCollatzFilterBehavior FilterBehavior { get; set; }
    public double NodeRadius { get; set; } = 2.2;
    public double LineThickness { get; set; } = 0.8;
    public int AnimationIntervalMs { get; set; } = 140;
    public double CenterX { get; set; }
    public double CenterY { get; set; }
    public double Zoom { get; set; } = 1;
    public Color BackgroundColor { get; set; } = Colors.Black;
    public MandelbrotPalette Palette { get; set; } = new();
}
