using System.Windows.Media;
using Color = System.Windows.Media.Color;

namespace FractalExplorerWPF.Models;

public enum ApollonianColoringMode
{
    Depth,
    Curvature,
    ParentCircle
}

public enum ApollonianDrawMode
{
    Filled,
    Outline
}

public sealed class ApollonianState
{
    public string SaveName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public int MaxDepth { get; set; } = 11;
    public int MaxCircles { get; set; } = 25_000;
    public double MinimumRadius { get; set; } = 0.00001;
    public double CenterX { get; set; }
    public double CenterY { get; set; }
    public double ViewWidth { get; set; } = 2.2;
    public double LineWidth { get; set; } = 1.25;
    public ApollonianColoringMode ColoringMode { get; set; } = ApollonianColoringMode.Depth;
    public ApollonianDrawMode DrawMode { get; set; } = ApollonianDrawMode.Filled;
    public Color StartColor { get; set; } = Color.FromRgb(34, 211, 238);
    public Color EndColor { get; set; } = Color.FromRgb(244, 63, 94);
    public Color BackgroundColor { get; set; } = Color.FromRgb(8, 15, 30);

    public ApollonianState Clone(string? name = null) => new()
    {
        SaveName = name ?? SaveName,
        Timestamp = Timestamp,
        MaxDepth = MaxDepth,
        MaxCircles = MaxCircles,
        MinimumRadius = MinimumRadius,
        CenterX = CenterX,
        CenterY = CenterY,
        ViewWidth = ViewWidth,
        LineWidth = LineWidth,
        ColoringMode = ColoringMode,
        DrawMode = DrawMode,
        StartColor = StartColor,
        EndColor = EndColor,
        BackgroundColor = BackgroundColor
    };
}
