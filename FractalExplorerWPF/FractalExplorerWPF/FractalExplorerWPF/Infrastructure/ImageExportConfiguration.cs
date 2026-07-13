using System.Windows.Media.Imaging;

namespace FractalExplorerWPF.Infrastructure;

public enum ImageExportFormat
{
    Png,
    Jpeg,
    Bmp
}

public enum ImageExportProcessingMode
{
    Ssaa,
    Bicubic,
    Lanczos
}

public readonly record struct ImageExportRenderRequest(int Width, int Height, int SsaaFactor);

public sealed class ImageExportConfiguration
{
    public required string FileNamePrefix { get; init; }
    public string WindowTitle { get; init; } = "Менеджер сохранения изображений";
    public int InitialWidth { get; init; } = 1920;
    public int InitialHeight { get; init; } = 1080;
    public int MaxSsaaFactor { get; init; } = 4;
    public bool HasNativeSsaa { get; init; } = true;
    public required Func<ImageExportRenderRequest, CancellationToken, IProgress<int>, Task<BitmapSource>> RenderAsync { get; init; }
}
