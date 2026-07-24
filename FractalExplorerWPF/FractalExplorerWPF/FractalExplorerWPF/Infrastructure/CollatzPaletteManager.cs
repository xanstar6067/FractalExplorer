namespace FractalExplorerWPF.Infrastructure;

public sealed class CollatzPaletteManager : MandelbrotPaletteManager
{
    public const int BuiltInColorPeriod = 25;
    private const string FileName = "custom_palettes_collatz.json";

    public CollatzPaletteManager() : base(FileName, BuiltInColorPeriod)
    {
    }
}
