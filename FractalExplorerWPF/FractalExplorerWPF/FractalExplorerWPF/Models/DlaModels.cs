using System.Windows.Media;
using Color = System.Windows.Media.Color;

namespace FractalExplorerWPF.Models;

public enum DlaSeedMode
{
    Center,
    BottomLine,
    BottomPoint
}

public enum DlaColoringMode
{
    GrowthOrder,
    BranchDepth,
    DistanceFromSeed
}

public sealed class DlaPreset
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required DlaState State { get; init; }
    public override string ToString() => Name;
}

public sealed class DlaState
{
    public string SaveName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? PresetId { get; set; }
    public int ParticleCount { get; set; } = 7_500;
    public int GridSize { get; set; } = 701;
    public int MaxStepsPerWalker { get; set; } = 6_000;
    public int RandomSeed { get; set; } = 12345;
    public double Stickiness { get; set; } = 1;
    public double DriftX { get; set; }
    public double DriftY { get; set; }
    public double CenterX { get; set; }
    public double CenterY { get; set; }
    public double ViewWidth { get; set; } = 2.2;
    public double ParticleRadius { get; set; } = 1.15;
    public DlaSeedMode SeedMode { get; set; } = DlaSeedMode.Center;
    public DlaColoringMode ColoringMode { get; set; } = DlaColoringMode.GrowthOrder;
    public Color StartColor { get; set; } = Color.FromRgb(125, 211, 252);
    public Color EndColor { get; set; } = Color.FromRgb(99, 102, 241);
    public Color BackgroundColor { get; set; } = Color.FromRgb(3, 7, 18);

    public DlaState Clone(string? name = null) => new()
    {
        SaveName = name ?? SaveName,
        Timestamp = Timestamp,
        PresetId = PresetId,
        ParticleCount = ParticleCount,
        GridSize = GridSize,
        MaxStepsPerWalker = MaxStepsPerWalker,
        RandomSeed = RandomSeed,
        Stickiness = Stickiness,
        DriftX = DriftX,
        DriftY = DriftY,
        CenterX = CenterX,
        CenterY = CenterY,
        ViewWidth = ViewWidth,
        ParticleRadius = ParticleRadius,
        SeedMode = SeedMode,
        ColoringMode = ColoringMode,
        StartColor = StartColor,
        EndColor = EndColor,
        BackgroundColor = BackgroundColor
    };
}

public static class DlaPresets
{
    public static IReadOnlyList<DlaPreset> All { get; } =
    [
        new()
        {
            Id = "frost",
            Name = "Морозный кристалл — из центра",
            State = new DlaState
            {
                PresetId = "frost", ParticleCount = 7_500, GridSize = 701,
                SeedMode = DlaSeedMode.Center, Stickiness = 1, ViewWidth = 2.2,
                StartColor = Color.FromRgb(224, 242, 254), EndColor = Color.FromRgb(56, 189, 248)
            }
        },
        new()
        {
            Id = "coral",
            Name = "Коралл — рост от линии",
            State = new DlaState
            {
                PresetId = "coral", ParticleCount = 10_000, GridSize = 701,
                SeedMode = DlaSeedMode.BottomLine, Stickiness = 0.72, DriftY = 0.16,
                CenterY = -0.05, ViewWidth = 2.15,
                StartColor = Color.FromRgb(251, 146, 60), EndColor = Color.FromRgb(244, 63, 94)
            }
        },
        new()
        {
            Id = "lightning",
            Name = "Молния — рост к точке",
            State = new DlaState
            {
                PresetId = "lightning", ParticleCount = 6_000, GridSize = 701,
                SeedMode = DlaSeedMode.BottomPoint, Stickiness = 0.82, DriftY = 0.32,
                CenterY = -0.05, ViewWidth = 2.15,
                StartColor = Color.FromRgb(254, 249, 195), EndColor = Color.FromRgb(168, 85, 247)
            }
        }
    ];
}
