namespace FractalExplorerWPF.Models;

public sealed record FractalCatalogItem(
    string Family,
    string DisplayName,
    string Description,
    string PreviewResourcePath,
    string? LaunchKey = null);
