using System.Windows.Media;
using System.Text.Json.Serialization;
using Color = System.Windows.Media.Color;

namespace FractalExplorerWPF.Models;

// Numeric values follow the canonical flam3 variation indices and are persisted in save files.
public enum FlameVariation
{
    Linear = 0,
    Sinusoidal = 1,
    Spherical = 2,
    Swirl = 3,
    Horseshoe = 4,
    Polar = 5,
    Heart = 7,
    Disc = 8,
    Spiral = 9,
    Julia = 13,
    Fisheye = 16,
    Bubble = 28
}

public sealed class FlameTransform
{
    public double Weight { get; set; } = 1;
    public double A { get; set; }
    public double B { get; set; }
    public double C { get; set; }
    public double D { get; set; }
    public double E { get; set; }
    public double F { get; set; }
    public FlameVariation Variation { get; set; }
    public Color Color { get; set; } = Colors.White;
    public FlameTransform Clone() => (FlameTransform)MemberwiseClone();
}

public sealed class FlameState
{
    public string SaveName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public double CenterX { get; set; }
    public double CenterY { get; set; }
    public double Scale { get; set; } = 4;
    public int Samples { get; set; } = 1_000_000;
    public int IterationsPerSample { get; set; } = 20;
    public int WarmupIterations { get; set; } = 20;
    public double Exposure { get; set; } = 1.35;
    public double Gamma { get; set; } = 2.2;
    public List<FlameTransform> Transforms { get; set; } = [];
    [JsonPropertyName("Iterations")] public int LegacyIterations { set { if (value > 0) IterationsPerSample = value; } }
    [JsonPropertyName("Warmup")] public int LegacyWarmup { set { if (value >= 0) WarmupIterations = value; } }

    public FlameState Clone(string? saveName = null) => new()
    {
        SaveName = saveName ?? SaveName, Timestamp = Timestamp, CenterX = CenterX, CenterY = CenterY,
        Scale = Scale, Samples = Samples, IterationsPerSample = IterationsPerSample,
        WarmupIterations = WarmupIterations, Exposure = Exposure, Gamma = Gamma,
        Transforms = Transforms.Select(t => t.Clone()).ToList()
    };

    public static List<FlameTransform> CreateDefaults() =>
    [
        new() { Weight=1, A=.5, C=-.3, E=.5, Variation=FlameVariation.Linear, Color=Colors.Orange },
        new() { Weight=1, A=.5, C=.3, E=.5, Variation=FlameVariation.Sinusoidal, Color=Colors.DeepSkyBlue },
        new() { Weight=.6, A=.5, E=.5, F=.4, Variation=FlameVariation.Spherical, Color=Colors.Lime }
    ];
}
