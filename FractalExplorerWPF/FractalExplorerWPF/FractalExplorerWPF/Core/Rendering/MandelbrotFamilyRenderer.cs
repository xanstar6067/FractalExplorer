using System.Numerics;
using System.Windows.Media;
using FractalExplorer.Utilities;
using FractalExplorerWPF.Models;
using Color = System.Windows.Media.Color;

namespace FractalExplorerWPF.Core.Rendering;

public static class MandelbrotFamilyRenderer
{
    private readonly record struct PixelMetrics(int Iterations, double Smooth, double OrbitTrap, double Stripe);

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
            decimal im = state.CenterY + (0.5m - ((decimal)y + 0.5m) / height) * viewHeight;
            for (int x = 0; x < width; x++)
            {
                decimal re = state.CenterX + (((decimal)x + 0.5m) / width - 0.5m) * viewWidth;
                PixelMetrics metrics = IterateAt(state, re, im);
                Color color = ResolveColor(state, metrics, 0);
                int offset = row + x * 4;
                buffer[offset] = color.B;
                buffer[offset + 1] = color.G;
                buffer[offset + 2] = color.R;
                buffer[offset + 3] = color.A;
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
            decimal im = state.CenterY + (0.5m - ((decimal)y + 0.5m) / height) * viewHeight;
            int row = y * width;
            for (int x = 0; x < width; x++)
            {
                decimal re = state.CenterX + (((decimal)x + 0.5m) / width - 0.5m) * viewWidth;
                PixelMetrics value = IterateAt(state, re, im);
                metrics[row + x] = value;
                if (value.Iterations < state.Iterations) localBins[value.Iterations]++;
            }
            lock (histogramLock)
            {
                for (int i = 0; i < bins.Length; i++) bins[i] += localBins[i];
            }
            int done = Interlocked.Increment(ref scanRows);
            progress?.Invoke(done * 65 / height);
        });

        long total = bins.Take(state.Iterations).Sum(value => (long)value);
        var cdf = new double[bins.Length];
        long cumulative = 0;
        for (int i = 0; i < state.Iterations; i++)
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
                double normalized = value.Iterations >= state.Iterations ? 0 : cdf[value.Iterations];
                Color color = ResolveColor(state, value, normalized);
                int offset = outputRow + x * 4;
                buffer[offset] = color.B;
                buffer[offset + 1] = color.G;
                buffer[offset + 2] = color.R;
                buffer[offset + 3] = color.A;
            }
            int done = Interlocked.Increment(ref coloredRows);
            progress?.Invoke(65 + done * 35 / height);
        });
    }

    private static PixelMetrics IterateAt(MandelbrotState state, decimal re, decimal im) =>
        state.Zoom > 2_000_000_000m
            ? IterateDecimal(state, re, im)
            : Iterate(state, (double)re, (double)im);

    private static PixelMetrics Iterate(MandelbrotState state, double re, double im)
    {
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
            IterateOnce(state, ref zr, ref zi, cr, ci);
            iterations++;
            minTrap = Math.Min(minTrap, Math.Min(Math.Abs(zr), Math.Abs(zi)));
            stripe += 0.5 + 0.5 * Math.Sin(state.StripeFrequency * Math.Atan2(zi, zr));
        }

        double magnitudeSquared = zr * zr + zi * zi;
        double smooth = iterations;
        if (iterations < state.Iterations && magnitudeSquared > 1)
        {
            double logZn = Math.Log(magnitudeSquared) / 2;
            double power = state.Variant is MandelbrotVariant.Generalized or MandelbrotVariant.Simonobrot
                ? Math.Max(1.0001, (double)state.Power)
                : 2;
            double nu = Math.Log(Math.Max(logZn, 1e-300) / Math.Log(power)) / Math.Log(power);
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
            minTrap = Math.Min(minTrap, Math.Min(Math.Abs(zr), Math.Abs(zi)));
            stripe += 0.5 + 0.5 * Math.Sin(state.StripeFrequency * Math.Atan2((double)zi, (double)zr));
        }

        decimal magnitudeSquared = zr * zr + zi * zi;
        double smooth = iterations;
        if (iterations < state.Iterations && magnitudeSquared > 1)
        {
            double magnitudeAsDouble = (double)magnitudeSquared;
            double power = state.Variant is MandelbrotVariant.Generalized or MandelbrotVariant.Simonobrot
                ? Math.Max(1.0001, (double)state.Power)
                : 2;
            double logZn = Math.Log(magnitudeAsDouble) / 2;
            double nu = Math.Log(Math.Max(logZn, 1e-300) / Math.Log(power)) / Math.Log(power);
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

    private static Color ResolveColor(MandelbrotState state, PixelMetrics value, double histogramValue)
    {
        MandelbrotPalette palette = state.Palette;
        if (value.Iterations >= state.Iterations) return palette.InteriorColor;

        if (palette.Name == "Стандартный серый" &&
            state.ColoringMode is MandelbrotColoringMode.Discrete or MandelbrotColoringMode.Smooth)
        {
            double source = state.ColoringMode == MandelbrotColoringMode.Discrete
                ? Math.Min(value.Iterations, palette.ColorPeriod)
                : Math.Max(0, value.Smooth);
            double maximum = state.ColoringMode == MandelbrotColoringMode.Discrete
                ? palette.ColorPeriod
                : state.Iterations;
            double mapped = Math.Log(source + 1) / Math.Log(Math.Max(1, maximum) + 1);
            byte gray = (byte)Math.Round(255 * Math.Pow(Math.Clamp(1 - mapped, 0, 1), 1 / Math.Max(0.01, palette.Gamma)));
            return Color.FromRgb(gray, gray, gray);
        }

        double normalized = state.ColoringMode switch
        {
            MandelbrotColoringMode.Discrete => (double)(value.Iterations % Math.Max(1, palette.ColorPeriod)) / Math.Max(1, palette.ColorPeriod),
            MandelbrotColoringMode.Smooth => PositiveModulo(value.Smooth, palette.ColorPeriod) / Math.Max(1, palette.ColorPeriod),
            MandelbrotColoringMode.Histogram => Math.Pow(Math.Clamp(histogramValue, 0, 1), 1 / Math.Max(0.01, state.HistogramContrast)),
            MandelbrotColoringMode.OrbitTrap => Math.Clamp(1 / (1 + value.OrbitTrap) * state.OrbitTrapStrength + state.OrbitTrapBias, 0, 1),
            MandelbrotColoringMode.StripeAverage => Math.Clamp(
                Math.Clamp(value.Smooth / Math.Max(1, state.Iterations), 0, 1) * (1 - Math.Clamp(state.StripeStrength, 0, 1)) +
                Math.Clamp(value.Stripe + state.StripeBias, 0, 1) * Math.Clamp(state.StripeStrength, 0, 1), 0, 1),
            MandelbrotColoringMode.SmoothEscapePolynomial => PolynomialMap(state, value.Smooth / Math.Max(1, state.Iterations)),
            _ => 0
        };
        return SamplePalette(palette, normalized);
    }

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
        if (palette.Colors.Count == 1) return palette.Colors[0];
        normalized = Math.Clamp(normalized, 0, 1);
        if (palette.Name == "Стандартный серый") normalized = 1 - normalized;
        normalized = Math.Pow(normalized, 1 / Math.Max(0.01, palette.Gamma));
        double position = normalized * (palette.Colors.Count - 1);
        int left = Math.Min((int)position, palette.Colors.Count - 1);
        if (!palette.IsGradient || left == palette.Colors.Count - 1) return palette.Colors[left];
        Color a = palette.Colors[left];
        Color b = palette.Colors[left + 1];
        double t = position - left;
        return Color.FromArgb(
            Lerp(a.A, b.A, t), Lerp(a.R, b.R, t), Lerp(a.G, b.G, t), Lerp(a.B, b.B, t));
    }

    private static byte Lerp(byte a, byte b, double t) => (byte)Math.Round(a + (b - a) * t);
    private static double PositiveModulo(double value, double period) => (value % period + period) % period;
}
