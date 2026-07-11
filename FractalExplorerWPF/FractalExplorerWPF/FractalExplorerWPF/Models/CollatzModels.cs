namespace FractalExplorerWPF.Models;

public enum CollatzVariation
{
    Standard,
    SineVariation,
    GeneralizedP
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
    public bool UseSmoothColoring { get; set; } = true;
    public MandelbrotPalette Palette { get; set; } = new();
}
