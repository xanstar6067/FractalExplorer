using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Image = System.Windows.Controls.Image;

namespace FractalExplorerWPF.Infrastructure;

/// <summary>Copies the displayed image layers; never invokes a fractal renderer.</summary>
public static class SavePreviewCapture
{
    public static BitmapSource? Capture(FrameworkElement layer, Brush? background,
        int maxWidth, int maxHeight, params Image[] images)
    {
        layer.Dispatcher.VerifyAccess();
        if (!images.Any(image => image.Source is not null && image.Visibility == Visibility.Visible) ||
            layer.ActualWidth <= 0 || layer.ActualHeight <= 0)
            return null;

        double scale = Math.Min(1, Math.Min(maxWidth / layer.ActualWidth, maxHeight / layer.ActualHeight));
        int width = Math.Max(1, (int)Math.Round(layer.ActualWidth * scale));
        int height = Math.Max(1, (int)Math.Round(layer.ActualHeight * scale));
        var bounds = new Rect(0, 0, width, height);
        var drawing = new DrawingVisual();
        RenderOptions.SetBitmapScalingMode(drawing, BitmapScalingMode.HighQuality);
        using (DrawingContext context = drawing.RenderOpen())
        {
            context.DrawRectangle(background ?? Brushes.Black, null, bounds);
            var brush = new VisualBrush(layer)
            {
                ViewboxUnits = BrushMappingMode.Absolute,
                Viewbox = new Rect(0, 0, layer.ActualWidth, layer.ActualHeight),
                Stretch = Stretch.Fill
            };
            context.DrawRectangle(brush, null, bounds);
        }
        var snapshot = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        snapshot.Render(drawing);
        snapshot.Freeze();
        return snapshot;
    }
}
