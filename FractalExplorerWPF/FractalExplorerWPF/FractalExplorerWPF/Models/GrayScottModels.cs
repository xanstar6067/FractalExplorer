using System.Windows.Media;
using Color = System.Windows.Media.Color;
using MediaColors = System.Windows.Media.Colors;

namespace FractalExplorerWPF.Models;

public enum GrayScottSeedMode
{
    CenterSquare,
    RandomSpots,
    Ring,
    Noise
}

public enum GrayScottFieldMode
{
    V,
    U,
    Difference
}

public sealed class GrayScottPalette
{
    public string Name { get; set; } = "Новая палитра";
    public List<Color> Colors { get; set; } = [MediaColors.Black, MediaColors.White];
    public bool IsGradient { get; set; } = true;
    public bool IsBuiltIn { get; set; }
    public double Gamma { get; set; } = 1;

    public GrayScottPalette Clone(string? name = null) => new()
    {
        Name = name ?? Name,
        Colors = [.. Colors],
        IsGradient = IsGradient,
        Gamma = Gamma
    };

    public override string ToString() => Name;
}

public sealed class GrayScottPreset
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required GrayScottState State { get; init; }
    public override string ToString() => Name;
}

public sealed class GrayScottState
{
    public string SaveName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? PresetId { get; set; }
    public double DiffusionU { get; set; } = 0.16;
    public double DiffusionV { get; set; } = 0.08;
    public double Feed { get; set; } = 0.0545;
    public double Kill { get; set; } = 0.062;
    public double DeltaTime { get; set; } = 1;
    public int GridSize { get; set; } = 256;
    public int StepsPerFrame { get; set; } = 4;
    public int TargetFps { get; set; } = 30;
    public int RandomSeed { get; set; } = 1729;
    public GrayScottSeedMode SeedMode { get; set; } = GrayScottSeedMode.Noise;
    public int SeedCount { get; set; } = 18;
    public int SeedRadius { get; set; } = 6;
    public int BrushRadius { get; set; } = 8;
    public GrayScottFieldMode FieldMode { get; set; } = GrayScottFieldMode.V;
    public double RangeMinimum { get; set; }
    public double RangeMaximum { get; set; } = 0.5;
    public bool ReversePalette { get; set; }
    public GrayScottPalette Palette { get; set; } = GrayScottPalettes.Coral.Clone();

    public GrayScottState Clone(string? name = null) => new()
    {
        SaveName = name ?? SaveName,
        Timestamp = Timestamp,
        PresetId = PresetId,
        DiffusionU = DiffusionU,
        DiffusionV = DiffusionV,
        Feed = Feed,
        Kill = Kill,
        DeltaTime = DeltaTime,
        GridSize = GridSize,
        StepsPerFrame = StepsPerFrame,
        TargetFps = TargetFps,
        RandomSeed = RandomSeed,
        SeedMode = SeedMode,
        SeedCount = SeedCount,
        SeedRadius = SeedRadius,
        BrushRadius = BrushRadius,
        FieldMode = FieldMode,
        RangeMinimum = RangeMinimum,
        RangeMaximum = RangeMaximum,
        ReversePalette = ReversePalette,
        Palette = Palette.Clone()
    };
}

public static class GrayScottPalettes
{
    public static GrayScottPalette Coral => new()
    {
        Name = "Коралловый риф",
        Colors =
        [
            Color.FromRgb(2, 6, 23), Color.FromRgb(8, 47, 73),
            Color.FromRgb(8, 145, 178), Color.FromRgb(103, 232, 249),
            Color.FromRgb(255, 247, 237), Color.FromRgb(251, 113, 133)
        ]
    };
}

public static class GrayScottPresets
{
    public static IReadOnlyList<GrayScottPreset> All { get; } =
    [
        Preset("coral", "Коралл — ветвящиеся лабиринты", 0.060, 0.062, GrayScottSeedMode.Noise, 18, 6, steps: 4),
        Preset("worms", "Черви — движущиеся нити", 0.078, 0.061, GrayScottSeedMode.RandomSpots, 60, 4, steps: 4),
        Preset("mitosis", "Митоз — делящиеся пятна", 0.0367, 0.0649, GrayScottSeedMode.RandomSpots, 28, 7, steps: 4),
        Preset("solitons", "Солитоны — устойчивые импульсы", 0.030, 0.062, GrayScottSeedMode.CenterSquare, 1, 12, steps: 4),
        Preset("waves", "Волны — кольцевой фронт", 0.014, 0.054, GrayScottSeedMode.Ring, 1, 8, steps: 5),
        Preset("chaos", "Хаос — взаимодействующие домены", 0.026, 0.051, GrayScottSeedMode.Noise, 1, 5, steps: 3)
    ];

    private static GrayScottPreset Preset(
        string id,
        string name,
        double feed,
        double kill,
        GrayScottSeedMode seedMode,
        int seedCount,
        int seedRadius,
        int steps = 8) => new()
    {
        Id = id,
        Name = name,
        State = new GrayScottState
        {
            PresetId = id,
            Feed = feed,
            Kill = kill,
            SeedMode = seedMode,
            SeedCount = seedCount,
            SeedRadius = seedRadius,
            StepsPerFrame = steps
        }
    };
}
