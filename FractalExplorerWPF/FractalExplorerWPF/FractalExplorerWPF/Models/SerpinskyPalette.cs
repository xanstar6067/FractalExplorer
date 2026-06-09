using System.Windows.Media;
using Color = System.Windows.Media.Color;

namespace FractalExplorerWPF.Models;

public sealed class SerpinskyPalette
{
    public string Name { get; set; } = string.Empty;
    public Color FractalColor { get; set; } = Colors.Black;
    public Color BackgroundColor { get; set; } = Colors.White;
    public bool IsBuiltIn { get; set; }

    public override string ToString() => IsBuiltIn ? $"{Name} [Встроенная]" : Name;

    public SerpinskyPalette Clone(string name) =>
        new()
        {
            Name = name,
            FractalColor = FractalColor,
            BackgroundColor = BackgroundColor
        };
}
