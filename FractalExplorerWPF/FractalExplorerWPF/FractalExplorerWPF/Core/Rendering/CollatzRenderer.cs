using System.Numerics;
using System.Windows.Media;
using FractalExplorer.Utilities;
using FractalExplorerWPF.Models;
using Color = System.Windows.Media.Color;

namespace FractalExplorerWPF.Core.Rendering;

public static class CollatzRenderer
{
    private const decimal BaseScale = 4m;
    private const decimal DecimalScaleThreshold = 4m / 2_000_000_000m;

    public static byte[]? RenderTile(CollatzState state, int canvasWidth, int canvasHeight,
        MandelbrotRenderTile tile, CancellationToken token)
    {
        byte[] pixels = new byte[checked(tile.Width * tile.Height * 4)];
        decimal scale = BaseScale / Math.Max(0.000000000000001m, state.Zoom);

        for (int localY = 0; localY < tile.Height; localY++)
        {
            if (token.IsCancellationRequested) return null;
            int canvasY = tile.Y + localY;
            decimal imaginary = state.CenterY - (canvasY - canvasHeight / 2m) * scale / canvasWidth;
            for (int localX = 0; localX < tile.Width; localX++)
            {
                if ((localX & 31) == 0 && token.IsCancellationRequested) return null;
                int canvasX = tile.X + localX;
                decimal real = state.CenterX + (canvasX - canvasWidth / 2m) * scale / canvasWidth;
                Color color = CalculateColor(state, real, imaginary, scale);
                int offset = (localY * tile.Width + localX) * 4;
                pixels[offset] = color.B;
                pixels[offset + 1] = color.G;
                pixels[offset + 2] = color.R;
                pixels[offset + 3] = color.A;
            }
        }
        return pixels;
    }

    public static void Render(CollatzState state, byte[] pixels, int width, int height, int stride,
        int threadCount, CancellationToken token, Action<int>? progress = null)
    {
        decimal scale = BaseScale / Math.Max(0.000000000000001m, state.Zoom);
        long completed = 0;

        Parallel.For(0, height, new ParallelOptions
        {
            MaxDegreeOfParallelism = Math.Max(1, threadCount)
        }, (y, loopState) =>
        {
            if (token.IsCancellationRequested) { loopState.Stop(); return; }
            int row = y * stride;
            decimal imaginary = state.CenterY - (y - height / 2m) * scale / width;
            for (int x = 0; x < width; x++)
            {
                if ((x & 63) == 0 && token.IsCancellationRequested) { loopState.Stop(); return; }
                decimal real = state.CenterX + (x - width / 2m) * scale / width;
                Color color = CalculateColor(state, real, imaginary, scale);
                int offset = row + x * 4;
                pixels[offset] = color.B;
                pixels[offset + 1] = color.G;
                pixels[offset + 2] = color.R;
                pixels[offset + 3] = color.A;
            }
            int rows = (int)Interlocked.Increment(ref completed);
            if (rows == height || rows % Math.Max(1, height / 100) == 0) progress?.Invoke(rows * 100 / height);
        });
    }

    private static Color CalculateColor(CollatzState state, decimal real, decimal imaginary, decimal scale)
    {
        int iteration;
        double smooth;
        if (scale < DecimalScaleThreshold)
        {
            var z = new ComplexDecimal(real, imaginary);
            iteration = IterateDecimal(ref z, state.Variation, state.PParameter, state.Iterations,
                state.Threshold);
            smooth = Smooth(iteration, state.Iterations, z);
        }
        else
        {
            var z = new Complex((double)real, (double)imaginary);
            iteration = Iterate(ref z, state.Variation, (double)state.PParameter, state.Iterations,
                (double)(state.Threshold * state.Threshold));
            smooth = Smooth(iteration, state.Iterations, z);
        }
        return ResolveColor(state, iteration, smooth);
    }

    private static int Iterate(ref Complex z, CollatzVariation variation, double p, int maximum, double thresholdSquared)
    {
        int iteration = 0;
        while (iteration < maximum)
        {
            double magnitudeSquared = z.Real * z.Real + z.Imaginary * z.Imaginary;
            if (!double.IsFinite(magnitudeSquared) || magnitudeSquared > thresholdSquared || Math.Abs(z.Imaginary * Math.PI) > 700) break;

            z = variation switch
            {
                CollatzVariation.SineVariation => 0.25 * (2 + 7 * z - (2 + 5 * z) * Complex.Sin(Math.PI * z)),
                CollatzVariation.GeneralizedP => 0.5 * ((p - 1) * z + 1 - ((p - 1) * z - 1) * Complex.Cos(Math.PI * z)),
                _ => 0.25 * (2 + 7 * z - (2 + 5 * z) * Complex.Cos(Math.PI * z))
            };
            iteration++;
        }
        return iteration;
    }

    private static int IterateDecimal(ref ComplexDecimal z, CollatzVariation variation, decimal p, int maximum,
        decimal threshold)
    {
        int iteration = 0;
        while (iteration < maximum && IsWithinEscapeRadius(z, threshold))
        {
            if (Math.Abs(z.Imaginary * (decimal)Math.PI) > 60m) break;
            try
            {
                ComplexDecimal argument = z * (decimal)Math.PI;
                z = variation switch
                {
                    CollatzVariation.SineVariation => 0.25m *
                        (new ComplexDecimal(2, 0) + z * 7 - (new ComplexDecimal(2, 0) + z * 5) * ComplexSin(argument)),
                    CollatzVariation.GeneralizedP => 0.5m *
                        ((p - 1) * z + 1 - ((p - 1) * z - 1) * ComplexCos(argument)),
                    _ => (new ComplexDecimal(2, 0) + z * 7 -
                          (new ComplexDecimal(2, 0) + z * 5) * ComplexCos(argument)) / 4m
                };
                iteration++;
            }
            catch (OverflowException)
            {
                break;
            }
        }
        return iteration;
    }

    private static bool IsWithinEscapeRadius(ComplexDecimal z, decimal threshold)
    {
        // Check the components first. An escaped value can still fit in decimal while
        // squaring it for MagnitudeSquared cannot (the source of the deep-zoom crash).
        if (z.Real < -threshold || z.Real > threshold ||
            z.Imaginary < -threshold || z.Imaginary > threshold)
            return false;

        return z.Real * z.Real + z.Imaginary * z.Imaginary <= threshold * threshold;
    }

    private static ComplexDecimal ComplexCos(ComplexDecimal z)
    {
        double real = (double)z.Real;
        double imaginary = (double)z.Imaginary;
        return new ComplexDecimal((decimal)(Math.Cos(real) * Math.Cosh(imaginary)),
            (decimal)(-Math.Sin(real) * Math.Sinh(imaginary)));
    }

    private static ComplexDecimal ComplexSin(ComplexDecimal z)
    {
        double real = (double)z.Real;
        double imaginary = (double)z.Imaginary;
        return new ComplexDecimal((decimal)(Math.Sin(real) * Math.Cosh(imaginary)),
            (decimal)(Math.Cos(real) * Math.Sinh(imaginary)));
    }

    private static double Smooth(int iteration, int maximum, Complex z)
    {
        if (iteration >= maximum) return iteration;
        double magnitudeSquared = z.Real * z.Real + z.Imaginary * z.Imaginary;
        if (!double.IsFinite(magnitudeSquared) || magnitudeSquared <= 1) return iteration;
        double log = Math.Log(magnitudeSquared);
        if (!double.IsFinite(log) || log <= 0) return iteration;
        double nu = Math.Log(log / (2 * Math.Log(2))) / Math.Log(2);
        return double.IsFinite(nu) ? Math.Max(0, iteration + 1 - nu) : iteration;
    }

    private static double Smooth(int iteration, int maximum, ComplexDecimal z)
    {
        if (iteration >= maximum) return iteration;
        double real = (double)z.Real;
        double imaginary = (double)z.Imaginary;
        double magnitudeSquared = real * real + imaginary * imaginary;
        if (!double.IsFinite(magnitudeSquared) || magnitudeSquared <= 1) return iteration;
        double log = Math.Log(magnitudeSquared);
        if (!double.IsFinite(log) || log <= 0) return iteration;
        double nu = Math.Log(log / (2 * Math.Log(2))) / Math.Log(2);
        return double.IsFinite(nu) ? Math.Max(0, iteration + 1 - nu) : iteration;
    }

    private static Color ResolveColor(CollatzState state, int iteration, double smooth)
    {
        MandelbrotPalette palette = state.Palette;
        if (iteration >= state.Iterations) return palette.InteriorColor;
        double period = palette.AlignWithRenderIterations ? state.Iterations : Math.Max(1, palette.ColorPeriod);
        double value = state.UseSmoothColoring ? smooth : iteration;
        if (palette.Name == "Стандартный серый")
        {
            double logarithmic = Math.Log(Math.Max(0, value) + 1) / Math.Log(state.Iterations + 1);
            byte gray = (byte)Math.Clamp((int)Math.Round(255 * (1 - logarithmic)), 0, 255);
            return ApplyGamma(Color.FromRgb(gray, gray, gray), palette.Gamma);
        }
        double normalized = (value % period + period) % period / period;
        return SamplePalette(palette, normalized);
    }

    private static Color SamplePalette(MandelbrotPalette palette, double normalized)
    {
        if (palette.Colors.Count == 0) return Colors.White;
        if (palette.Colors.Count == 1) return ApplyGamma(palette.Colors[0], palette.Gamma);
        normalized = Math.Clamp(normalized, 0, 1);
        Color result;
        if (!palette.IsGradient)
        {
            result = palette.Colors[Math.Min((int)(normalized * palette.Colors.Count), palette.Colors.Count - 1)];
        }
        else
        {
            double position = normalized * (palette.Colors.Count - 1);
            int left = Math.Min((int)position, palette.Colors.Count - 1);
            if (left == palette.Colors.Count - 1) result = palette.Colors[left];
            else
            {
                Color a = palette.Colors[left];
                Color b = palette.Colors[left + 1];
                double amount = position - left;
                result = Color.FromArgb(Lerp(a.A, b.A, amount), Lerp(a.R, b.R, amount),
                    Lerp(a.G, b.G, amount), Lerp(a.B, b.B, amount));
            }
        }
        return ApplyGamma(result, palette.Gamma);
    }

    private static Color ApplyGamma(Color color, double gamma)
    {
        double correction = 1 / Math.Max(0.01, gamma);
        return Color.FromArgb(color.A,
            (byte)(255 * Math.Pow(color.R / 255d, correction)),
            (byte)(255 * Math.Pow(color.G / 255d, correction)),
            (byte)(255 * Math.Pow(color.B / 255d, correction)));
    }

    private static byte Lerp(byte start, byte end, double amount) =>
        (byte)Math.Round(start + (end - start) * amount);
}
