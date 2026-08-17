using System.Windows.Media;
using Color = System.Windows.Media.Color;

namespace FractalExplorerWPF.Core.Rendering;

internal static class RasterDrawing
{
    public static void Fill(byte[] pixels, Color color)
    {
        Parallel.For(0, pixels.Length / 4, index =>
        {
            int offset = index * 4;
            pixels[offset] = color.B;
            pixels[offset + 1] = color.G;
            pixels[offset + 2] = color.R;
            pixels[offset + 3] = 255;
        });
    }

    public static Color Lerp(Color from, Color to, double amount)
    {
        amount = Math.Clamp(amount, 0, 1);
        return Color.FromRgb(
            (byte)Math.Round(from.R + (to.R - from.R) * amount),
            (byte)Math.Round(from.G + (to.G - from.G) * amount),
            (byte)Math.Round(from.B + (to.B - from.B) * amount));
    }

    public static void DrawCircle(
        byte[] pixels,
        int width,
        int height,
        double centerX,
        double centerY,
        double radius,
        double lineWidth,
        Color color,
        bool filled)
    {
        if (!double.IsFinite(centerX) || !double.IsFinite(centerY) ||
            !double.IsFinite(radius) || radius <= 0)
            return;

        double halfLine = Math.Max(0.5, lineWidth / 2);
        double reach = radius + (filled ? 1 : halfLine + 1);
        int left = Math.Max(0, (int)Math.Floor(centerX - reach));
        int right = Math.Min(width - 1, (int)Math.Ceiling(centerX + reach));
        int top = Math.Max(0, (int)Math.Floor(centerY - reach));
        int bottom = Math.Min(height - 1, (int)Math.Ceiling(centerY + reach));
        if (left > right || top > bottom) return;

        for (int y = top; y <= bottom; y++)
        {
            double dy = y + 0.5 - centerY;
            for (int x = left; x <= right; x++)
            {
                double dx = x + 0.5 - centerX;
                double distance = Math.Sqrt(dx * dx + dy * dy);
                double coverage = filled
                    ? Math.Clamp(radius + 0.5 - distance, 0, 1)
                    : Math.Clamp(halfLine + 0.5 - Math.Abs(distance - radius), 0, 1);
                if (coverage <= 0) continue;

                int offset = (y * width + x) * 4;
                double inverse = 1 - coverage;
                pixels[offset] = (byte)Math.Round(color.B * coverage + pixels[offset] * inverse);
                pixels[offset + 1] = (byte)Math.Round(color.G * coverage + pixels[offset + 1] * inverse);
                pixels[offset + 2] = (byte)Math.Round(color.R * coverage + pixels[offset + 2] * inverse);
                pixels[offset + 3] = 255;
            }
        }
    }
}
