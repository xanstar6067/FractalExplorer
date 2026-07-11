using System.IO;
using System.Text.Json;
using System.Windows.Media;
using FractalExplorerWPF.Models;
using Color = System.Windows.Media.Color;

namespace FractalExplorerWPF.Infrastructure;

public sealed class NewtonPaletteManager
{
    private const string FileName = "newton_palettes.json";

    public List<NewtonColorPalette> Palettes { get; } =
    [
        BuiltIn("Оттенки серого", [Colors.White, Colors.LightGray, Colors.DarkGray], true),
        BuiltIn("Классика", [], false),
        BuiltIn("Классика — градиент", [], true),
        BuiltIn("Чёрно-белый", [Colors.White], false),
        BuiltIn("Пастель", [Rgb(255,182,193), Rgb(173,216,230), Rgb(189,252,201), Rgb(253,253,150)], false, Rgb(40,40,40)),
        BuiltIn("Контраст", [Colors.Red, Colors.Yellow, Colors.Blue], false),
        BuiltIn("Огонь", [Rgb(200,0,0), Rgb(255,100,0), Rgb(255,255,100)], true),
        BuiltIn("Психоделика", [Rgb(10,0,20), Colors.Magenta, Colors.Cyan], true),
        BuiltIn("Огонь и лёд", [Rgb(255,100,0), Rgb(0,100,255), Rgb(255,200,0), Rgb(0,200,255)], true)
    ];

    public NewtonColorPalette ActivePalette { get; set; }

    public NewtonPaletteManager()
    {
        try { LoadCustomPalettes(); } catch { }
        ActivePalette = Palettes[1];
    }

    public void SaveCustomPalettes()
    {
        string path = Path.Combine(AppPaths.EnsureSavesDirectory(), FileName);
        File.WriteAllText(path, JsonSerializer.Serialize(
            Palettes.Where(palette => !palette.IsBuiltIn), JsonOptionsFactory.Create()));
    }

    public static List<Color> AdjustColors(NewtonColorPalette palette, int requiredCount)
    {
        if (requiredCount <= 0) return [];
        if (palette.RootColors.Count == 0) return GenerateHarmonicColors(requiredCount);
        if (palette.RootColors.Count == requiredCount) return [.. palette.RootColors];
        if (palette.RootColors.Count > requiredCount) return palette.RootColors.Take(requiredCount).ToList();

        var colors = new List<Color>(palette.RootColors);
        List<Color> harmonic = GenerateHarmonicColors(requiredCount);
        while (colors.Count < requiredCount) colors.Add(harmonic[colors.Count]);
        return colors;
    }

    public static List<Color> GenerateHarmonicColors(int count)
    {
        var colors = new List<Color>();
        for (int index = 0; index < count; index++)
            colors.Add(FromHsl(360d * index / count, 0.85, 0.6));
        return colors;
    }

    private void LoadCustomPalettes()
    {
        string path = Path.Combine(AppPaths.SavesDirectory, FileName);
        if (!File.Exists(path)) return;
        List<NewtonColorPalette>? custom = JsonSerializer.Deserialize<List<NewtonColorPalette>>(
            File.ReadAllText(path), JsonOptionsFactory.Create());
        if (custom is not null) Palettes.AddRange(custom.Where(palette => !palette.IsBuiltIn));
    }

    private static NewtonColorPalette BuiltIn(string name, List<Color> colors, bool gradient, Color? background = null) => new()
    {
        Name = name,
        RootColors = colors,
        BackgroundColor = background ?? Colors.Black,
        IsGradient = gradient,
        IsBuiltIn = true
    };

    private static Color Rgb(byte red, byte green, byte blue) => Color.FromRgb(red, green, blue);

    private static Color FromHsl(double hue, double saturation, double lightness)
    {
        double chroma = (1 - Math.Abs(2 * lightness - 1)) * saturation;
        double sector = hue / 60;
        double x = chroma * (1 - Math.Abs(sector % 2 - 1));
        (double r, double g, double b) = sector switch
        {
            < 1 => (chroma, x, 0d), < 2 => (x, chroma, 0d), < 3 => (0d, chroma, x),
            < 4 => (0d, x, chroma), < 5 => (x, 0d, chroma), _ => (chroma, 0d, x)
        };
        double m = lightness - chroma / 2;
        return Color.FromRgb((byte)Math.Round((r + m) * 255), (byte)Math.Round((g + m) * 255), (byte)Math.Round((b + m) * 255));
    }
}
