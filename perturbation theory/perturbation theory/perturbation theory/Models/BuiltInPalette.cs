namespace perturbation_theory.Models;

public readonly record struct Rgb(byte R, byte G, byte B);

// Basic presets copied from the main WPF MandelbrotPaletteManager.
// Plain RGB data keeps the numerical renderer independent of WPF and palette storage.
public sealed record BuiltInPalette(string Name, Rgb[] Colors, int Period, double Gamma = 1,
    bool Grayscale = false, bool Gradient = true)
{
    private static readonly Rgb Black = new(0, 0, 0);
    private static readonly Rgb White = new(255, 255, 255);

    public static IReadOnlyList<BuiltInPalette> All { get; } =
    [
        new("Стандартный серый", [Black, White], 800, Grayscale: true),
        new("Ультрафиолет", [Black, new(148, 0, 211), new(238, 130, 238), White], 1000, 1.2),
        new("Огонь", [Black, new(139, 0, 0), new(255, 0, 0), new(255, 165, 0), new(255, 255, 0), White], 400, 0.9),
        new("Лёд", [Black, new(0, 0, 139), new(0, 0, 255), new(0, 255, 255), White], 500, 1.2),
        new("Огонь и лед", [Black, new(0, 0, 139), new(0, 255, 255), White, new(255, 255, 0), new(255, 0, 0), new(139, 0, 0)], 700),
        new("Психоделика", [new(255, 0, 0), new(255, 255, 0), new(0, 255, 0), new(0, 255, 255), new(0, 0, 255), new(255, 0, 255)], 6, Gradient: false),
        new("Черно-белый", [Black, White], 500),
        new("Зеленый", [Black, new(0, 128, 0), new(0, 204, 0), new(0, 234, 0), new(60, 255, 60), new(145, 255, 145), new(213, 255, 213), White], 120),
        new("Сепия", [new(20, 10, 0), new(255, 240, 192)], 500)
    ];

    public override string ToString() => Name;
}
