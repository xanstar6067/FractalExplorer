namespace FractalExplorerWPF.Infrastructure;

public sealed class NovaPaletteManager : MandelbrotPaletteManager
{
    public const int BuiltInColorPeriod = 7;
    private const string FileName = "custom_palettes_nova.json";

    public NovaPaletteManager() : base(FileName, BuiltInColorPeriod)
    {
    }
}
