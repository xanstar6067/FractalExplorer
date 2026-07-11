using System.Windows.Media;
using FractalExplorerWPF.Models;
using Color = System.Windows.Media.Color;

namespace FractalExplorerWPF.Core.Rendering;

public static class PhoenixRenderer
{
    private const decimal BaseScale = 4m;

    public static void Render(PhoenixState state, byte[] pixels, int width, int height, int stride,
        int threadCount, CancellationToken token, Action<int>? progress = null)
    {
        double centerX = (double)state.CenterX;
        double centerY = (double)state.CenterY;
        double scale = (double)(BaseScale / Math.Max(0.000000000001m, state.Zoom));
        double thresholdSquared = (double)(state.Threshold * state.Threshold);
        double p = (double)state.C1Real;
        double q = (double)state.C1Imaginary;
        long completed = 0;
        Parallel.For(0, height, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, threadCount), CancellationToken = token }, y =>
        {
            int row = y * stride;
            double imaginary = centerY + (height / 2.0 - y) * scale / width;
            for (int x = 0; x < width; x++)
            {
                if ((x & 63) == 0) token.ThrowIfCancellationRequested();
                double real = centerX + (x - width / 2.0) * scale / width;
                int iterations = Iterate(real, imaginary, p, q, state.Iterations, thresholdSquared, out double finalMagnitudeSquared);
                double smooth = Smooth(iterations, state.Iterations, finalMagnitudeSquared);
                Color color = ResolveColor(state, iterations, smooth);
                int offset = row + x * 4;
                pixels[offset] = color.B; pixels[offset + 1] = color.G; pixels[offset + 2] = color.R; pixels[offset + 3] = color.A;
            }
            int rows = (int)Interlocked.Increment(ref completed);
            if (rows == height || rows % Math.Max(1, height / 100) == 0) progress?.Invoke(rows * 100 / height);
        });
    }

    public static void RenderSlice(byte[] pixels, int width, int height, int stride, PhoenixSliceRange range,
        bool pSlice, decimal fixedParameter, int iterations, decimal threshold, int threadCount,
        CancellationToken token, Action<int>? progress = null)
    {
        double fixedValue = (double)fixedParameter;
        double thresholdSquared = (double)(threshold * threshold);
        long completed = 0;
        Parallel.For(0, height, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, threadCount), CancellationToken = token }, y =>
        {
            double z0Imaginary = range.MaxY - y * (range.MaxY - range.MinY) / height;
            int row = y * stride;
            for (int x = 0; x < width; x++)
            {
                double axis = range.MinX + x * (range.MaxX - range.MinX) / width;
                double p = pSlice ? axis : fixedValue;
                double q = pSlice ? fixedValue : axis;
                int count = Iterate(0, z0Imaginary, p, q, iterations, thresholdSquared, out _);
                byte gray = count >= iterations ? (byte)0 : (byte)Math.Clamp((int)(255 * (1 - Math.Log(count + 1) / Math.Log(iterations + 1))), 0, 255);
                int offset = row + x * 4;
                pixels[offset] = gray; pixels[offset + 1] = gray; pixels[offset + 2] = gray; pixels[offset + 3] = 255;
            }
            int rows = (int)Interlocked.Increment(ref completed);
            if (rows == height || rows % Math.Max(1, height / 100) == 0) progress?.Invoke(rows * 100 / height);
        });
    }

    private static int Iterate(double zr, double zi, double p, double q, int maximum, double thresholdSquared, out double magnitudeSquared)
    {
        double previousReal = 0, previousImaginary = 0;
        int iteration = 0;
        magnitudeSquared = zr * zr + zi * zi;
        while (iteration < maximum && magnitudeSquared <= thresholdSquared)
        {
            double nextReal = zr * zr - zi * zi + p + q * previousReal;
            double nextImaginary = 2 * zr * zi + q * previousImaginary;
            previousReal = zr; previousImaginary = zi; zr = nextReal; zi = nextImaginary;
            magnitudeSquared = zr * zr + zi * zi;
            iteration++;
        }
        return iteration;
    }

    private static double Smooth(int iteration, int maximum, double magnitudeSquared)
    {
        if (iteration >= maximum || !double.IsFinite(magnitudeSquared) || magnitudeSquared <= 1) return iteration;
        double log = Math.Log(magnitudeSquared);
        if (log <= 0) return iteration;
        double nu = Math.Log(log / (2 * Math.Log(2))) / Math.Log(2);
        return double.IsFinite(nu) ? iteration + 1 - nu : iteration;
    }

    private static Color ResolveColor(PhoenixState state, int iteration, double smooth)
    {
        MandelbrotPalette palette = state.Palette;
        if (iteration >= state.Iterations) return palette.InteriorColor;
        double period = palette.AlignWithRenderIterations ? state.Iterations : Math.Max(1, palette.ColorPeriod);
        double value = state.UseSmoothColoring ? Math.Max(0, smooth) : iteration;
        if (palette.Name == "Стандартный серый")
        {
            double normalized = Math.Log(value + 1) / Math.Log(state.Iterations + 1);
            byte gray = (byte)Math.Clamp((int)(255 * (1 - normalized)), 0, 255);
            return ApplyGamma(Color.FromRgb(gray, gray, gray), palette.Gamma);
        }
        double normalizedValue = state.UseSmoothColoring ? (value % period + period) % period / period : Math.Min(value, period) / period;
        return SamplePalette(palette, normalizedValue);
    }

    private static Color SamplePalette(MandelbrotPalette palette, double normalized)
    {
        if (palette.Colors.Count == 0) return Colors.White;
        if (palette.Colors.Count == 1) return ApplyGamma(palette.Colors[0], palette.Gamma);
        normalized = Math.Clamp(normalized, 0, 1);
        Color result;
        if (!palette.IsGradient)
            result = palette.Colors[Math.Min((int)(normalized * palette.Colors.Count), palette.Colors.Count - 1)];
        else
        {
            double position = normalized * (palette.Colors.Count - 1);
            int left = Math.Min((int)position, palette.Colors.Count - 1);
            if (left == palette.Colors.Count - 1) result = palette.Colors[left];
            else
            {
                Color a = palette.Colors[left], b = palette.Colors[left + 1];
                double amount = position - left;
                result = Color.FromArgb(Lerp(a.A, b.A, amount), Lerp(a.R, b.R, amount), Lerp(a.G, b.G, amount), Lerp(a.B, b.B, amount));
            }
        }
        return ApplyGamma(result, palette.Gamma);
    }

    private static Color ApplyGamma(Color color, double gamma)
    {
        double correction = 1 / Math.Max(0.01, gamma);
        return Color.FromArgb(color.A, (byte)(255 * Math.Pow(color.R / 255d, correction)),
            (byte)(255 * Math.Pow(color.G / 255d, correction)), (byte)(255 * Math.Pow(color.B / 255d, correction)));
    }

    private static byte Lerp(byte start, byte end, double amount) => (byte)Math.Round(start + (end - start) * amount);
}
