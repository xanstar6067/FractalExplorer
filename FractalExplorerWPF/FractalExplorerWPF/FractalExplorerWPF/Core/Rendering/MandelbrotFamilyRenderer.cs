using System.Numerics;
using System.Windows.Media;
using FractalExplorer.Utilities;
using FractalExplorerWPF.Models;
using Color = System.Windows.Media.Color;

namespace FractalExplorerWPF.Core.Rendering;

public static class MandelbrotFamilyRenderer
{
    private readonly record struct PixelMetrics(int Iterations, double Smooth, double OrbitTrap, double Stripe);

    public static byte[]? RenderTile(
        MandelbrotState state,
        int canvasWidth,
        int canvasHeight,
        MandelbrotRenderTile tile,
        CancellationToken token)
    {
        int stride = checked(tile.Width * 4);
        var buffer = new byte[checked(stride * tile.Height)];
        decimal viewWidth = 3m / Math.Max(state.Zoom, 0.000000000000001m);
        decimal viewHeight = viewWidth * canvasHeight / canvasWidth;

        for (int localY = 0; localY < tile.Height; localY++)
        {
            if (token.IsCancellationRequested) return null;
            int y = tile.Y + localY;
            decimal im = state.CenterY + (0.5m - (decimal)y / canvasHeight) * viewHeight;
            int row = localY * stride;
            for (int localX = 0; localX < tile.Width; localX++)
            {
                int x = tile.X + localX;
                decimal re = state.CenterX + ((decimal)x / canvasWidth - 0.5m) * viewWidth;
                PixelMetrics metrics = IterateAt(state, re, im);
                double histogramValue = state.ColoringMode == MandelbrotColoringMode.Histogram
                    ? Math.Clamp((state.HistogramInputUseSmooth ? metrics.Smooth : metrics.Iterations) /
                                 Math.Max(1, state.Iterations), 0, 1)
                    : 0;
                Color color = ResolveColor(state, metrics, histogramValue);
                int offset = row + localX * 4;
                buffer[offset] = color.B;
                buffer[offset + 1] = color.G;
                buffer[offset + 2] = color.R;
                buffer[offset + 3] = 255;
            }
        }
        return buffer;
    }

    public static void Render(
        MandelbrotState state,
        byte[] buffer,
        int width,
        int height,
        int stride,
        CancellationToken token,
        Action<int>? reportProgress = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 1);
        if (buffer.Length < stride * height) throw new ArgumentException("Буфер изображения слишком мал.", nameof(buffer));

        int threads = state.Threads <= 0 ? Environment.ProcessorCount : state.Threads;
        var options = new ParallelOptions
        {
            CancellationToken = token,
            MaxDegreeOfParallelism = Math.Clamp(threads, 1, Environment.ProcessorCount)
        };
        decimal viewWidth = 3m / Math.Max(state.Zoom, 0.000000000000001m);
        decimal viewHeight = viewWidth * height / width;
        int completedRows = 0;

        if (state.ColoringMode == MandelbrotColoringMode.Histogram)
        {
            RenderHistogram(state, buffer, width, height, stride, viewWidth, viewHeight,
                options, ref completedRows, reportProgress);
            return;
        }

        Parallel.For(0, height, options, y =>
        {
            int row = y * stride;
            decimal im = state.CenterY + (0.5m - (decimal)y / height) * viewHeight;
            for (int x = 0; x < width; x++)
            {
                decimal re = state.CenterX + ((decimal)x / width - 0.5m) * viewWidth;
                PixelMetrics metrics = IterateAt(state, re, im);
                Color color = ResolveColor(state, metrics, 0);
                int offset = row + x * 4;
                buffer[offset] = color.B;
                buffer[offset + 1] = color.G;
                buffer[offset + 2] = color.R;
                buffer[offset + 3] = 255;
            }
            int done = Interlocked.Increment(ref completedRows);
            reportProgress?.Invoke(done * 100 / height);
        });
    }

    private static void RenderHistogram(
        MandelbrotState state, byte[] buffer, int width, int height, int stride,
        decimal viewWidth, decimal viewHeight, ParallelOptions options,
        ref int completedRows, Action<int>? progress)
    {
        completedRows = 0;
        int scanRows = 0;
        var metrics = new PixelMetrics[checked(width * height)];
        var bins = new int[state.Iterations + 1];
        object histogramLock = new();

        Parallel.For(0, height, options, y =>
        {
            var localBins = new int[bins.Length];
            decimal im = state.CenterY + (0.5m - (decimal)y / height) * viewHeight;
            int row = y * width;
            for (int x = 0; x < width; x++)
            {
                decimal re = state.CenterX + ((decimal)x / width - 0.5m) * viewWidth;
                PixelMetrics value = IterateAt(state, re, im);
                metrics[row + x] = value;
                int bin = state.HistogramInputUseSmooth
                    ? Math.Clamp((int)Math.Floor(value.Smooth), 0, state.Iterations)
                    : Math.Clamp(value.Iterations, 0, state.Iterations);
                localBins[bin]++;
            }
            lock (histogramLock)
            {
                for (int i = 0; i < bins.Length; i++) bins[i] += localBins[i];
            }
            int done = Interlocked.Increment(ref scanRows);
            progress?.Invoke(done * 65 / height);
        });

        long total = (long)width * height;
        var cdf = new double[bins.Length];
        long cumulative = 0;
        for (int i = 0; i <= state.Iterations; i++)
        {
            cumulative += bins[i];
            cdf[i] = total == 0 ? 0 : (double)cumulative / total;
        }

        int coloredRows = 0;
        Parallel.For(0, height, options, y =>
        {
            int metricRow = y * width;
            int outputRow = y * stride;
            for (int x = 0; x < width; x++)
            {
                PixelMetrics value = metrics[metricRow + x];
                int bin = state.HistogramInputUseSmooth
                    ? Math.Clamp((int)Math.Floor(value.Smooth), 0, state.Iterations)
                    : Math.Clamp(value.Iterations, 0, state.Iterations);
                double normalized = value.Iterations >= state.Iterations
                    ? 0
                    : state.HistogramEnabledEqualization
                        ? cdf[bin]
                        : bin / (double)Math.Max(1, state.Iterations);
                Color color = ResolveColor(state, value, normalized);
                int offset = outputRow + x * 4;
                buffer[offset] = color.B;
                buffer[offset + 1] = color.G;
                buffer[offset + 2] = color.R;
                buffer[offset + 3] = 255;
            }
            int done = Interlocked.Increment(ref coloredRows);
            progress?.Invoke(65 + done * 35 / height);
        });
    }

    private static PixelMetrics IterateAt(MandelbrotState state, decimal re, decimal im) =>
        state.Zoom > 1_500_000_000m
            ? IterateDecimal(state, re, im)
            : Iterate(state, (double)re, (double)im);

    private static PixelMetrics Iterate(MandelbrotState state, double re, double im)
    {
        if (state.Variant == MandelbrotVariant.Mandelbrot && IsInsideMandelbrot(re, im))
            return new PixelMetrics(state.Iterations, state.Iterations, 0, 0);
        double cr = state.UseInversion && state.Variant == MandelbrotVariant.Simonobrot ? -re : re;
        double ci = im;
        double zr = 0;
        double zi = 0;
        double thresholdSquared = (double)(state.Threshold * state.Threshold);
        double minTrap = double.MaxValue;
        double stripe = 0;
        int iterations = 0;

        while (iterations < state.Iterations && zr * zr + zi * zi <= thresholdSquared)
        {
            minTrap = Math.Min(minTrap, Math.Min(Math.Abs(zr), Math.Abs(zi)));
            stripe += 0.5 + 0.5 * Math.Sin(state.StripeFrequency * Math.Atan2(zi, zr));
            IterateOnce(state, ref zr, ref zi, cr, ci);
            iterations++;
        }

        double magnitudeSquared = zr * zr + zi * zi;
        double smooth = iterations;
        if (iterations < state.Iterations && magnitudeSquared > 1)
        {
            double logZn = Math.Log(magnitudeSquared) / 2;
            const double smoothingPower = 2;
            double nu = Math.Log(Math.Max(logZn, 1e-300) / Math.Log(smoothingPower)) /
                        Math.Log(smoothingPower);
            if (double.IsFinite(nu)) smooth = iterations + 1 - nu;
        }

        return new PixelMetrics(
            iterations,
            smooth,
            minTrap == double.MaxValue ? 0 : minTrap,
            iterations == 0 ? 0 : stripe / iterations);
    }

    private static PixelMetrics IterateDecimal(MandelbrotState state, decimal re, decimal im)
    {
        if (state.Variant == MandelbrotVariant.Mandelbrot && IsInsideMandelbrot(re, im))
            return new PixelMetrics(state.Iterations, state.Iterations, 0, 0);
        decimal cr = state.UseInversion && state.Variant == MandelbrotVariant.Simonobrot ? -re : re;
        decimal ci = im;
        decimal zr = 0;
        decimal zi = 0;
        decimal thresholdSquared = state.Threshold * state.Threshold;
        decimal minTrap = decimal.MaxValue;
        double stripe = 0;
        int iterations = 0;

        while (iterations < state.Iterations && zr * zr + zi * zi <= thresholdSquared)
        {
            minTrap = Math.Min(minTrap, Math.Min(Math.Abs(zr), Math.Abs(zi)));
            stripe += 0.5 + 0.5 * Math.Sin(state.StripeFrequency * Math.Atan2((double)zi, (double)zr));
            try
            {
                IterateOnceDecimal(state, ref zr, ref zi, cr, ci);
            }
            catch (OverflowException)
            {
                zr = state.Threshold + 1;
                zi = 0;
            }
            iterations++;
        }

        decimal magnitudeSquared = zr * zr + zi * zi;
        double smooth = iterations;
        if (iterations < state.Iterations && magnitudeSquared > 1)
        {
            double magnitudeAsDouble = (double)magnitudeSquared;
            double logZn = Math.Log(magnitudeAsDouble) / 2;
            const double smoothingPower = 2;
            double nu = Math.Log(Math.Max(logZn, 1e-300) / Math.Log(smoothingPower)) /
                        Math.Log(smoothingPower);
            if (double.IsFinite(nu)) smooth = iterations + 1 - nu;
        }

        return new PixelMetrics(iterations, smooth,
            minTrap == decimal.MaxValue ? 0 : (double)minTrap,
            iterations == 0 ? 0 : stripe / iterations);
    }

    private static void IterateOnceDecimal(
        MandelbrotState state, ref decimal zr, ref decimal zi, decimal cr, decimal ci)
    {
        switch (state.Variant)
        {
            case MandelbrotVariant.Mandelbrot:
                SquareAddDecimal(ref zr, ref zi, cr, ci);
                break;
            case MandelbrotVariant.BurningShip:
                zr = Math.Abs(zr);
                zi = -Math.Abs(zi);
                SquareAddDecimal(ref zr, ref zi, cr, ci);
                break;
            case MandelbrotVariant.Tricorn:
                zi = -zi;
                SquareAddDecimal(ref zr, ref zi, cr, ci);
                break;
            case MandelbrotVariant.Buffalo:
                zr = Math.Abs(zr);
                zi = Math.Abs(zi);
                SquareAddDecimal(ref zr, ref zi, cr, ci);
                break;
            case MandelbrotVariant.Celtic:
            {
                decimal real = Math.Abs(zr * zr - zi * zi) + cr;
                zi = 2 * zr * zi + ci;
                zr = real;
                break;
            }
            case MandelbrotVariant.Simonobrot:
            {
                decimal magnitudeSquared = zr * zr + zi * zi;
                if (magnitudeSquared == 0)
                {
                    zr = cr;
                    zi = ci;
                    break;
                }
                ComplexDecimal powered = ComplexDecimal.Pow(
                    new ComplexDecimal(zr, zi), new ComplexDecimal(state.Power, 0));
                decimal magnitudePower = DecimalMath.Pow(DecimalMath.Sqrt(magnitudeSquared), state.Power);
                zr = powered.Real * magnitudePower + cr;
                zi = powered.Imaginary * magnitudePower + ci;
                break;
            }
            case MandelbrotVariant.Generalized:
            {
                ComplexDecimal powered = ComplexDecimal.Pow(
                    new ComplexDecimal(zr, zi), new ComplexDecimal(state.Power, 0));
                zr = powered.Real + cr;
                zi = powered.Imaginary + ci;
                break;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private static void SquareAddDecimal(ref decimal zr, ref decimal zi, decimal cr, decimal ci)
    {
        decimal real = zr * zr - zi * zi + cr;
        zi = 2 * zr * zi + ci;
        zr = real;
    }

    private static void IterateOnce(MandelbrotState state, ref double zr, ref double zi, double cr, double ci)
    {
        switch (state.Variant)
        {
            case MandelbrotVariant.Mandelbrot:
                SquareAdd(ref zr, ref zi, cr, ci);
                break;
            case MandelbrotVariant.BurningShip:
                zr = Math.Abs(zr);
                zi = -Math.Abs(zi);
                SquareAdd(ref zr, ref zi, cr, ci);
                break;
            case MandelbrotVariant.Tricorn:
                zi = -zi;
                SquareAdd(ref zr, ref zi, cr, ci);
                break;
            case MandelbrotVariant.Buffalo:
                zr = Math.Abs(zr);
                zi = Math.Abs(zi);
                SquareAdd(ref zr, ref zi, cr, ci);
                break;
            case MandelbrotVariant.Celtic:
            {
                double real = Math.Abs(zr * zr - zi * zi) + cr;
                zi = 2 * zr * zi + ci;
                zr = real;
                break;
            }
            case MandelbrotVariant.Simonobrot:
            {
                double magnitudeSquared = zr * zr + zi * zi;
                if (magnitudeSquared == 0)
                {
                    zr = cr;
                    zi = ci;
                    break;
                }
                Complex powered = Complex.Pow(new Complex(zr, zi), (double)state.Power);
                double magnitudePower = Math.Pow(Math.Sqrt(magnitudeSquared), (double)state.Power);
                zr = powered.Real * magnitudePower + cr;
                zi = powered.Imaginary * magnitudePower + ci;
                break;
            }
            case MandelbrotVariant.Generalized:
            {
                Complex powered = Complex.Pow(new Complex(zr, zi), (double)state.Power);
                zr = powered.Real + cr;
                zi = powered.Imaginary + ci;
                break;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private static void SquareAdd(ref double zr, ref double zi, double cr, double ci)
    {
        double real = zr * zr - zi * zi + cr;
        zi = 2 * zr * zi + ci;
        zr = real;
    }

    private static bool IsInsideMandelbrot(double x, double y)
    {
        double shiftedX = x - 0.25;
        double ySquared = y * y;
        double q = shiftedX * shiftedX + ySquared;
        if (q * (q + shiftedX) <= 0.25 * ySquared) return true;
        double bulbX = x + 1;
        return bulbX * bulbX + ySquared <= 0.0625;
    }

    private static bool IsInsideMandelbrot(decimal x, decimal y)
    {
        decimal shiftedX = x - 0.25m;
        decimal ySquared = y * y;
        decimal q = shiftedX * shiftedX + ySquared;
        if (q * (q + shiftedX) <= 0.25m * ySquared) return true;
        decimal bulbX = x + 1;
        return bulbX * bulbX + ySquared <= 0.0625m;
    }

    private static Color ResolveColor(MandelbrotState state, PixelMetrics value, double histogramValue)
    {
        MandelbrotPalette palette = state.Palette;
        if (value.Iterations >= state.Iterations) return ResolveInteriorColor(state);
        double colorPeriod = palette.AlignWithRenderIterations
            ? Math.Max(1, state.Iterations)
            : Math.Max(1, palette.ColorPeriod);

        if (state.ColoringMode == MandelbrotColoringMode.Discrete)
            return SampleDiscretePalette(state, value.Iterations, colorPeriod);
        if (state.ColoringMode == MandelbrotColoringMode.Smooth)
            return SampleSmoothPalette(state, value.Smooth, colorPeriod);

        double normalized = state.ColoringMode switch
        {
            MandelbrotColoringMode.Histogram => Math.Pow(Math.Clamp(histogramValue, 0, 1), 1 / Math.Max(0.01, state.HistogramContrast)),
            MandelbrotColoringMode.OrbitTrap => Math.Clamp(1 / (1 + value.OrbitTrap) * state.OrbitTrapStrength + state.OrbitTrapBias, 0, 1),
            MandelbrotColoringMode.StripeAverage => Math.Clamp(
                Math.Clamp(value.Smooth / Math.Max(1, state.Iterations), 0, 1) * (1 - Math.Clamp(state.StripeStrength, 0, 1)) +
                Math.Clamp(value.Stripe + state.StripeBias, 0, 1) * Math.Clamp(state.StripeStrength, 0, 1), 0, 1),
            MandelbrotColoringMode.SmoothEscapePolynomial => PolynomialMap(state, value.Smooth / Math.Max(1, state.Iterations)),
            _ => 0
        };

        bool useSmoothPalette = state.ColoringMode != MandelbrotColoringMode.Histogram ||
                                state.HistogramInputUseSmooth;
        return useSmoothPalette
            ? SampleSmoothPalette(state, normalized * colorPeriod, colorPeriod)
            : SampleDiscretePalette(state, (int)Math.Round(normalized * colorPeriod), colorPeriod);
    }

    private static Color SampleDiscretePalette(MandelbrotState state, int iteration, double colorPeriod)
    {
        MandelbrotPalette palette = state.Palette;
        if (palette.Name == "Стандартный серый")
        {
            double grayNormalized = Math.Log(Math.Min(iteration, colorPeriod) + 1) /
                                    Math.Log(colorPeriod + 1);
            grayNormalized = TransformPaletteIndex(grayNormalized, state);
            byte gray = (byte)Math.Clamp((int)(255 * (1 - grayNormalized)), 0, 255);
            return ApplyGamma(Color.FromRgb(gray, gray, gray), palette.Gamma);
        }
        double normalized = Math.Min(iteration, colorPeriod) / colorPeriod;
        normalized = TransformPaletteIndex(normalized, state);
        return SamplePalette(palette, normalized);
    }

    private static Color SampleSmoothPalette(MandelbrotState state, double smoothIteration, double colorPeriod)
    {
        MandelbrotPalette palette = state.Palette;
        smoothIteration += state.SmoothIterationOffset;
        if (smoothIteration >= state.Iterations) return ResolveInteriorColor(state);
        smoothIteration = Math.Max(0, smoothIteration);

        double normalized;
        if (palette.Name == "Стандартный серый")
        {
            normalized = Math.Log(smoothIteration + 1) / Math.Log(Math.Max(1, state.Iterations) + 1);
            normalized = Math.Pow(Math.Clamp(normalized, 0, 1), Math.Max(0.01, state.SmoothBlendPower));
            normalized = TransformPaletteIndex(normalized, state);
            byte gray = (byte)Math.Clamp((int)(255 * (1 - normalized)), 0, 255);
            return ApplyGamma(Color.FromRgb(gray, gray, gray), palette.Gamma);
        }

        normalized = PositiveModulo(smoothIteration, colorPeriod) / colorPeriod;
        normalized = Math.Pow(normalized, Math.Max(0.01, state.SmoothBlendPower));
        normalized = TransformPaletteIndex(normalized, state);
        return SamplePalette(palette, normalized);
    }

    private static Color ResolveInteriorColor(MandelbrotState state) =>
        state.UseCustomInteriorColor ? state.InteriorColor : state.Palette.InteriorColor;

    private static double PolynomialMap(MandelbrotState state, double t)
    {
        t = Math.Clamp(t, 0, 1);
        double inverse = 1 - t;
        double polynomial = state.PolynomialA * inverse * t * t * t +
                            state.PolynomialB * inverse * inverse * t * t +
                            state.PolynomialC * inverse * inverse * inverse * t;
        polynomial = Math.Clamp(polynomial, 0, 1);
        double blended = t * (1 - Math.Clamp(state.PolynomialBlend, 0, 1)) +
                         polynomial * Math.Clamp(state.PolynomialBlend, 0, 1);
        return Math.Pow(Math.Clamp(blended + state.PolynomialBias, 0, 1), Math.Max(0.01, state.PolynomialGamma));
    }

    private static Color SamplePalette(MandelbrotPalette palette, double normalized)
    {
        if (palette.Colors.Count == 0) return Colors.White;
        if (palette.Colors.Count == 1) return ApplyGamma(palette.Colors[0], palette.Gamma);
        normalized = Math.Clamp(normalized, 0, 1);
        Color result;
        if (!palette.IsGradient)
        {
            int index = Math.Min((int)(normalized * palette.Colors.Count), palette.Colors.Count - 1);
            result = palette.Colors[index];
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
                double t = position - left;
                result = Color.FromArgb(
                    Lerp(a.A, b.A, t), Lerp(a.R, b.R, t), Lerp(a.G, b.G, t), Lerp(a.B, b.B, t));
            }
        }
        return ApplyGamma(result, palette.Gamma);
    }

    private static Color ApplyGamma(Color color, double gamma)
    {
        double correction = 1 / Math.Max(0.01, gamma);
        return Color.FromArgb(color.A,
            (byte)(255 * Math.Pow(color.R / 255.0, correction)),
            (byte)(255 * Math.Pow(color.G / 255.0, correction)),
            (byte)(255 * Math.Pow(color.B / 255.0, correction)));
    }

    private static byte Lerp(byte a, byte b, double t) => (byte)Math.Round(a + (b - a) * t);
    private static double PositiveModulo(double value, double period) => (value % period + period) % period;

    private static double TransformPaletteIndex(double value, MandelbrotState state)
    {
        double scale = Math.Abs(state.PaletteScale) < 1e-9 ? 1 : state.PaletteScale;
        double transformed = value * scale + state.PalettePhaseOffset;
        return state.PaletteWrapMode switch
        {
            MandelbrotPaletteWrapMode.Clamp => Math.Clamp(transformed, 0, 1),
            MandelbrotPaletteWrapMode.Mirror => Mirror01(transformed),
            _ => transformed - Math.Floor(transformed)
        };
    }

    private static double Mirror01(double value)
    {
        double period = value % 2;
        if (period < 0) period += 2;
        return period <= 1 ? period : 2 - period;
    }
}
