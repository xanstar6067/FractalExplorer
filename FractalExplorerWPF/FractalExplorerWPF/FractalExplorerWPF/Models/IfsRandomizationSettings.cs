namespace FractalExplorerWPF.Models;

public enum IfsTransformFamily
{
    Similarity,
    Anisotropic,
    Shear,
    Reflection,
    Stem
}

public enum IfsPlacementMode
{
    Free,
    Radial,
    Bilateral
}

public enum IfsProbabilityMode
{
    AreaWeighted,
    Uniform,
    Random
}

public sealed class IfsRandomizationSettings
{
    public const int MinimumAllowedTransforms = 1;
    public const int MaximumAllowedTransforms = 25;

    public int MinimumTransforms { get; set; } = 3;
    public int MaximumTransforms { get; set; } = 6;
    public IfsPlacementMode PlacementMode { get; set; } = IfsPlacementMode.Radial;
    public IfsProbabilityMode ProbabilityMode { get; set; } = IfsProbabilityMode.AreaWeighted;
    public List<IfsTransformFamily> Families { get; set; } = [.. Enum.GetValues<IfsTransformFamily>()];

    public IfsRandomizationSettings Normalize()
    {
        MinimumTransforms = Math.Clamp(MinimumTransforms, MinimumAllowedTransforms, MaximumAllowedTransforms);
        MaximumTransforms = Math.Clamp(MaximumTransforms, MinimumAllowedTransforms, MaximumAllowedTransforms);
        if (MinimumTransforms > MaximumTransforms)
            (MinimumTransforms, MaximumTransforms) = (MaximumTransforms, MinimumTransforms);

        if (!Enum.IsDefined(PlacementMode)) PlacementMode = IfsPlacementMode.Radial;
        if (!Enum.IsDefined(ProbabilityMode)) ProbabilityMode = IfsProbabilityMode.AreaWeighted;
        Families = [.. (Families ?? [])
            .Where(family => Enum.IsDefined(family))
            .Distinct()
            .OrderBy(family => (int)family)];
        return this;
    }

    public IfsRandomizationSettings Clone() => new()
    {
        MinimumTransforms = MinimumTransforms,
        MaximumTransforms = MaximumTransforms,
        PlacementMode = PlacementMode,
        ProbabilityMode = ProbabilityMode,
        Families = [.. (Families ?? [])]
    };
}
