using System.Windows.Media;
using FractalExplorer.Engines;
using Color = System.Windows.Media.Color;

namespace FractalExplorerWPF.Models;

public sealed class SerpinskySaveState
{
    public string SaveName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string FractalType { get; set; } = "Serpinsky";
    public SerpinskyRenderMode RenderMode { get; set; }
    public int Iterations { get; set; }
    public double Zoom { get; set; } = 1.0;
    public double CenterX { get; set; }
    public double CenterY { get; set; }
    public Color FractalColor { get; set; } = Colors.Black;
    public Color BackgroundColor { get; set; } = Colors.White;
}
