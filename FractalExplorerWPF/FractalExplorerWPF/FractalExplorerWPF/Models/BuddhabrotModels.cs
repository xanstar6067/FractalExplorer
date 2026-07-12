using System.Windows.Media;
using Color = System.Windows.Media.Color;

namespace FractalExplorerWPF.Models;

public enum BuddhabrotRenderMode { Buddhabrot, AntiBuddhabrot, SymmetricBuddhabrot }
public enum BuddhabrotColoringMode { Logarithmic, Sqrt, Linear }

public sealed class BuddhabrotColorPalette
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Новая палитра";
    public List<Color> Colors { get; set; } = [];
    public bool IsGradient { get; set; } = true;
    public int MaxColorIterations { get; set; } = 500;
    public bool AlignWithRenderIterations { get; set; }
    public double Gamma { get; set; } = 1;
    public BuddhabrotColoringMode ColoringMode { get; set; } = BuddhabrotColoringMode.Logarithmic;
    public bool IsBuiltIn { get; set; }
    public BuddhabrotColorPalette Clone(string name, bool builtIn = false) => new()
    {
        Id = Guid.NewGuid(), Name = name, Colors = [.. Colors], IsGradient = IsGradient,
        MaxColorIterations = MaxColorIterations, AlignWithRenderIterations = AlignWithRenderIterations,
        Gamma = Gamma, ColoringMode = ColoringMode, IsBuiltIn = builtIn
    };
}

public sealed class BuddhabrotState
{
    public string SaveName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public decimal CenterX { get; set; }
    public decimal CenterY { get; set; }
    public decimal Zoom { get; set; } = 1;
    public int MaxIterations { get; set; } = 500;
    public int SampleCount { get; set; } = 250_000;
    public BuddhabrotRenderMode RenderMode { get; set; }
    public decimal SampleMinRe { get; set; } = -2;
    public decimal SampleMaxRe { get; set; } = 1;
    public decimal SampleMinIm { get; set; } = -1.5m;
    public decimal SampleMaxIm { get; set; } = 1.5m;
    public BuddhabrotColorPalette Palette { get; set; } = new();
}
