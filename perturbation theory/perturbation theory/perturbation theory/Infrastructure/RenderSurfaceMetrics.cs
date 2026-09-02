using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace perturbation_theory.Infrastructure;

/// <summary>
/// Measures the drawable child of a framed host and converts its WPF device-independent
/// size to the exact physical-pixel size required for a bitmap shown on that surface.
/// </summary>
public readonly record struct RenderSurfaceMetrics(
    FrameworkElement Surface,
    double LogicalWidth,
    double LogicalHeight,
    int PixelWidth,
    int PixelHeight,
    DpiScale Dpi)
{
    public static RenderSurfaceMetrics Measure(FrameworkElement host)
    {
        FrameworkElement surface = host is Border { Child: FrameworkElement child } ? child : host;
        double width = surface.ActualWidth;
        double height = surface.ActualHeight;

        if ((width <= 0 || height <= 0) && host is Border border)
        {
            width = Math.Max(0, host.ActualWidth - border.BorderThickness.Left - border.BorderThickness.Right
                - border.Padding.Left - border.Padding.Right);
            height = Math.Max(0, host.ActualHeight - border.BorderThickness.Top - border.BorderThickness.Bottom
                - border.Padding.Top - border.Padding.Bottom);
        }

        width = Math.Max(1, width);
        height = Math.Max(1, height);
        DpiScale dpi = VisualTreeHelper.GetDpi(surface);
        return new RenderSurfaceMetrics(
            surface,
            width,
            height,
            Math.Max(1, (int)Math.Round(width * dpi.DpiScaleX)),
            Math.Max(1, (int)Math.Round(height * dpi.DpiScaleY)),
            dpi);
    }
}
