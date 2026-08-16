using System.Windows.Media;
using FractalExplorerWPF.Models;

namespace FractalExplorerWPF.Infrastructure;

public sealed class InverseCollatzPaletteManager : MandelbrotPaletteManager
{
    private const string FileName = "custom_palettes_inverse_collatz.json";

    public InverseCollatzPaletteManager() : base(FileName)
    {
        ActivePalette = new MandelbrotPalette
        {
            Name = "Глубина Коллатца",
            Colors = [Colors.DarkBlue, Colors.Cyan, Colors.Yellow, Colors.Red],
            IsBuiltIn = true,
            IsGradient = true,
            Gamma = 1
        };
        Palettes.Insert(0, ActivePalette);
    }
}
