namespace FractalExplorerWPF.Models;

public sealed class FlameRandomizationSettings
{
    public const int MinimumAllowedTransforms = 1;
    public const int MaximumAllowedTransforms = 10;

    public int MinimumTransforms { get; set; } = 3;
    public int MaximumTransforms { get; set; } = 6;
    public List<FlameVariation> Variations { get; set; } = [.. Enum.GetValues<FlameVariation>()];

    public FlameRandomizationSettings Normalize()
    {
        MinimumTransforms = Math.Clamp(MinimumTransforms, MinimumAllowedTransforms, MaximumAllowedTransforms);
        MaximumTransforms = Math.Clamp(MaximumTransforms, MinimumAllowedTransforms, MaximumAllowedTransforms);
        if (MinimumTransforms > MaximumTransforms)
            (MinimumTransforms, MaximumTransforms) = (MaximumTransforms, MinimumTransforms);

        Variations = [.. (Variations ?? [])
            .Where(variation => Enum.IsDefined(variation))
            .Distinct()
            .OrderBy(variation => (int)variation)];
        return this;
    }

    public FlameRandomizationSettings Clone() => new()
    {
        MinimumTransforms = MinimumTransforms,
        MaximumTransforms = MaximumTransforms,
        Variations = [.. (Variations ?? [])]
    };
}
