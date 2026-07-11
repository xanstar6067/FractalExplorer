namespace FractalExplorerWPF.Models;

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
    public decimal C1Imaginary { get; set; } = -0.5m;
    public decimal C2Real { get; set; }
    public decimal C2Imaginary { get; set; }
    public bool UseSmoothColoring { get; set; } = true;
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
