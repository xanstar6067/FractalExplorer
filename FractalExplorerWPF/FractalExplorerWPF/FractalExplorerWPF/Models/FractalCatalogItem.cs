namespace FractalExplorerWPF.Models;

public sealed record FractalCatalogItem(
    IReadOnlyList<string> CategoryPath,
    string DisplayName,
    string Description,
    string PreviewResourcePath,
    string? LaunchKey = null);
