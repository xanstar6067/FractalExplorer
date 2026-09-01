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
    private const int MaximumSupportedPeriod = 64;

    private readonly record struct OrbitMetrics(
        int Iterations,
        double Smooth,
        Complex Final,
        double IntegerTrap,
        double RealAxisTrap,
        int DetectedPeriod,
        double CycleKey,
        bool IsInterior);

    public static byte[]? RenderTile(CollatzState state, int canvasWidth, int canvasHeight,
        MandelbrotRenderTile tile, CancellationToken token)
    {
        if (state.ColoringMode == CollatzColoringMode.OrbitDensity)
            throw new InvalidOperationException("Orbit Density requires a full-frame render.");

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
        int effectiveThreads = Math.Clamp(threadCount, 1, Environment.ProcessorCount);
        if (state.ColoringMode == CollatzColoringMode.OrbitDensity)
        {
            RenderOrbitDensity(state, pixels, width, height, stride, effectiveThreads, scale, token, progress);
            return;
        }

        long completed = 0;

        Parallel.For(0, height, new ParallelOptions
        {
            MaxDegreeOfParallelism = effectiveThreads
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
        OrbitMetrics metrics;
        if (scale < DecimalScaleThreshold)
        {
            var z = new ComplexDecimal(real, imaginary);
            var q = new ComplexDecimal(state.QRealParameter, state.QImaginaryParameter);
            metrics = IterateDecimal(z, state, q);
        }
        else
        {
            var z = new Complex((double)real, (double)imaginary);
            var q = new Complex((double)state.QRealParameter, (double)state.QImaginaryParameter);
            metrics = Iterate(z, state, q);
        }
        return ResolveColor(state, metrics);
    }

    private static OrbitMetrics Iterate(Complex z, CollatzState state, Complex q)
    {
        int maximum = state.Iterations;
        double thresholdSquared = (double)(state.Threshold * state.Threshold);
        bool trackIntegerTrap = state.ColoringMode == CollatzColoringMode.IntegerTrap;
        bool trackRealAxisTrap = state.ColoringMode == CollatzColoringMode.RealAxisTrap;
        bool detectCycles = state.ColoringMode is CollatzColoringMode.CycleBasins or
            CollatzColoringMode.PeriodDetection;
        Span<Complex> history = detectCycles
            ? stackalloc Complex[MaximumSupportedPeriod + 1]
            : Span<Complex>.Empty;
        int historyCount = 0;
        int candidatePeriod = 0;
        int candidateHits = 0;
        int detectedPeriod = 0;
        double cycleKey = 0;
        int maximumPeriod = Math.Clamp(state.MaximumDetectedPeriod, 1, MaximumSupportedPeriod);
        double tolerance = Math.Clamp(state.CycleTolerance, 1e-12, 0.1);
        double integerTrap = trackIntegerTrap ? DistanceToInteger(z) : double.PositiveInfinity;
        double realAxisTrap = trackRealAxisTrap ? Math.Abs(z.Imaginary) : double.PositiveInfinity;
        int iteration = 0;

        if (detectCycles)
        {
            history[0] = z;
            historyCount = 1;
        }

        while (iteration < maximum)
        {
            double magnitudeSquared = z.Real * z.Real + z.Imaginary * z.Imaginary;
            if (!double.IsFinite(magnitudeSquared) || magnitudeSquared > thresholdSquared || Math.Abs(z.Imaginary * Math.PI) > 700) break;

            z = ApplyFormula(z, state.Variation, (double)state.PParameter, q);
            iteration++;
            if (double.IsFinite(z.Real) && double.IsFinite(z.Imaginary))
            {
                if (trackIntegerTrap) integerTrap = Math.Min(integerTrap, DistanceToInteger(z));
                if (trackRealAxisTrap) realAxisTrap = Math.Min(realAxisTrap, Math.Abs(z.Imaginary));
            }

            if (!detectCycles) continue;
            int matchedPeriod = FindPeriod(z, history, historyCount, maximumPeriod, tolerance);
            if (matchedPeriod == candidatePeriod && matchedPeriod > 0)
                candidateHits++;
            else
            {
                candidatePeriod = matchedPeriod;
                candidateHits = matchedPeriod > 0 ? 1 : 0;
            }

            if (candidateHits >= 2)
            {
                detectedPeriod = candidatePeriod;
                cycleKey = CalculateCycleKey(z, history, historyCount, detectedPeriod);
                break;
            }

            history[historyCount % history.Length] = z;
            historyCount++;
        }

        bool isInterior = detectedPeriod > 0 || iteration >= maximum &&
            IsWithinEscapeRadius(z, thresholdSquared);
        return new OrbitMetrics(iteration, Smooth(iteration, maximum, z), z,
            integerTrap, realAxisTrap, detectedPeriod, cycleKey, isInterior);
    }

    private static OrbitMetrics IterateDecimal(ComplexDecimal z, CollatzState state, ComplexDecimal q)
    {
        int maximum = state.Iterations;
        decimal threshold = state.Threshold;
        bool trackIntegerTrap = state.ColoringMode == CollatzColoringMode.IntegerTrap;
        bool trackRealAxisTrap = state.ColoringMode == CollatzColoringMode.RealAxisTrap;
        bool detectCycles = state.ColoringMode is CollatzColoringMode.CycleBasins or
            CollatzColoringMode.PeriodDetection;
        Span<ComplexDecimal> history = detectCycles
            ? stackalloc ComplexDecimal[MaximumSupportedPeriod + 1]
            : Span<ComplexDecimal>.Empty;
        int historyCount = 0;
        int candidatePeriod = 0;
        int candidateHits = 0;
        int detectedPeriod = 0;
        double cycleKey = 0;
        int maximumPeriod = Math.Clamp(state.MaximumDetectedPeriod, 1, MaximumSupportedPeriod);
        decimal tolerance = (decimal)Math.Clamp(state.CycleTolerance, 1e-12, 0.1);
        double integerTrap = trackIntegerTrap ? DistanceToInteger(z) : double.PositiveInfinity;
        double realAxisTrap = trackRealAxisTrap ? (double)Math.Abs(z.Imaginary) : double.PositiveInfinity;
        int iteration = 0;

        if (detectCycles)
        {
            history[0] = z;
            historyCount = 1;
        }

        while (iteration < maximum && IsWithinEscapeRadius(z, threshold))
        {
            if (Math.Abs(z.Imaginary * (decimal)Math.PI) > 60m) break;
            try
            {
                z = ApplyFormula(z, state.Variation, state.PParameter, q);
                iteration++;
                if (trackIntegerTrap) integerTrap = Math.Min(integerTrap, DistanceToInteger(z));
                if (trackRealAxisTrap)
                    realAxisTrap = Math.Min(realAxisTrap, (double)Math.Abs(z.Imaginary));

                if (!detectCycles) continue;
                int matchedPeriod = FindPeriod(z, history, historyCount, maximumPeriod, tolerance);
                if (matchedPeriod == candidatePeriod && matchedPeriod > 0)
                    candidateHits++;
                else
                {
                    candidatePeriod = matchedPeriod;
                    candidateHits = matchedPeriod > 0 ? 1 : 0;
                }

                if (candidateHits >= 2)
                {
                    detectedPeriod = candidatePeriod;
                    cycleKey = CalculateCycleKey(z, history, historyCount, detectedPeriod);
                    break;
                }

                history[historyCount % history.Length] = z;
                historyCount++;
            }
            catch (OverflowException)
            {
                break;
            }
        }

        bool isInterior = detectedPeriod > 0 || iteration >= maximum &&
            IsWithinEscapeRadius(z, threshold);
        return new OrbitMetrics(iteration, Smooth(iteration, maximum, z),
            new Complex((double)z.Real, (double)z.Imaginary), integerTrap, realAxisTrap,
            detectedPeriod, cycleKey, isInterior);
    }

    private static int FindPeriod(Complex z, Span<Complex> history, int historyCount,
        int maximumPeriod, double tolerance)
    {
        if (!double.IsFinite(z.Real) || !double.IsFinite(z.Imaginary)) return 0;
        int available = Math.Min(maximumPeriod, historyCount);
        double limit = tolerance * Math.Max(1, Complex.Abs(z));
        for (int period = 1; period <= available; period++)
        {
            Complex previous = history[(historyCount - period) % history.Length];
            if (Complex.Abs(z - previous) <= limit) return period;
        }
        return 0;
    }

    private static int FindPeriod(ComplexDecimal z, Span<ComplexDecimal> history, int historyCount,
        int maximumPeriod, decimal tolerance)
    {
        int available = Math.Min(maximumPeriod, historyCount);
        decimal limit = tolerance * Math.Max(1m, Math.Max(Math.Abs(z.Real), Math.Abs(z.Imaginary)));
        for (int period = 1; period <= available; period++)
        {
            ComplexDecimal previous = history[(historyCount - period) % history.Length];
            if (Math.Max(Math.Abs(z.Real - previous.Real), Math.Abs(z.Imaginary - previous.Imaginary)) <= limit)
                return period;
        }
        return 0;
    }

    private static double CalculateCycleKey(Complex z, Span<Complex> history, int historyCount, int period)
    {
        Complex centroid = z;
        for (int offset = 1; offset < period; offset++)
            centroid += history[(historyCount - offset) % history.Length];
        return CalculateCycleKey(centroid / period, period);
    }

    private static double CalculateCycleKey(ComplexDecimal z, Span<ComplexDecimal> history,
        int historyCount, int period)
    {
        ComplexDecimal centroid = z;
        for (int offset = 1; offset < period; offset++)
            centroid += history[(historyCount - offset) % history.Length];
        centroid /= period;
        return CalculateCycleKey(new Complex((double)centroid.Real, (double)centroid.Imaginary), period);
    }

    private static double CalculateCycleKey(Complex centroid, int period)
    {
        double angle = (Math.Atan2(centroid.Imaginary, centroid.Real) + Math.PI) / (2 * Math.PI);
        double magnitude = Math.Log(1 + Complex.Abs(centroid));
        return PositiveModulo(angle + magnitude * 0.3819660112501051 + period * 0.6180339887498949, 1);
    }

    private static double DistanceToInteger(Complex z)
    {
        if (!double.IsFinite(z.Real) || !double.IsFinite(z.Imaginary)) return double.PositiveInfinity;
        double realDistance = z.Real - Math.Round(z.Real);
        return Complex.Abs(new Complex(realDistance, z.Imaginary));
    }

    private static double DistanceToInteger(ComplexDecimal z)
    {
        decimal realDistance = z.Real - Math.Round(z.Real);
        return Complex.Abs(new Complex((double)realDistance, (double)z.Imaginary));
    }

    private static void RenderOrbitDensity(CollatzState state, byte[] pixels, int width, int height,
        int stride, int threadCount, decimal scale, CancellationToken token, Action<int>? progress)
    {
        var density = new long[checked(width * height)];
        int sampleStep = Math.Clamp(state.OrbitDensitySampleStep, 1, 8);
        int sourceRows = (height + sampleStep - 1) / sampleStep;
        int completedRows = 0;
        int sampleWeight = sampleStep * sampleStep;
        var options = new ParallelOptions { MaxDegreeOfParallelism = threadCount };
        object mergeLock = new();

        // На поток — буфер орбиты (как и раньше) и собственный буфер плотности. Прежде
        // на каждую точку каждой орбиты вызывался Interlocked.Add по общему массиву;
        // теперь общий массив трогается один раз на поток. Сумма одних и тех же целых
        // приращений от порядка не зависит — поле плотности идентично.
        Parallel.For(0, sourceRows, options,
            () => new OrbitDensityScratch(new int[Math.Max(1, state.Iterations)], new long[density.Length]),
            (sourceRow, loopState, scratch) =>
            {
                int[] path = scratch.Path;
                long[] localDensity = scratch.Density;
                if (token.IsCancellationRequested) { loopState.Stop(); return scratch; }
                int y = sourceRow * sampleStep;
                for (int x = 0; x < width; x += sampleStep)
                {
                    if (token.IsCancellationRequested) { loopState.Stop(); return scratch; }
                    bool escaped;
                    int pathLength = scale < DecimalScaleThreshold
                        ? TraceDensityOrbitDecimal(state, x, y, width, height, scale, path, token,
                            out escaped)
                        : TraceDensityOrbit(state, x, y, width, height, (double)scale, path, token,
                            out escaped);
                    if (state.OrbitDensityEscapedOnly && !escaped) continue;
                    for (int index = 0; index < pathLength; index++)
                        localDensity[path[index]] += sampleWeight;
                }
                int done = Interlocked.Increment(ref completedRows);
                if (done == sourceRows || done % Math.Max(1, sourceRows / 100) == 0)
                    progress?.Invoke(done * 75 / sourceRows);
                return scratch;
            },
            scratch =>
            {
                lock (mergeLock)
                {
                    long[] shared = density, local = scratch.Density;
                    for (int i = 0; i < shared.Length; i++) shared[i] += local[i];
                }
            });

        if (token.IsCancellationRequested) return;
        long maximumDensity = 0;
        for (int index = 0; index < density.Length; index++)
        {
            if ((index & 65535) == 0 && token.IsCancellationRequested) return;
            maximumDensity = Math.Max(maximumDensity, density[index]);
        }

        double logMaximum = Math.Log(1 + maximumDensity);
        double exposure = Math.Clamp(state.OrbitDensityExposure, 0.1, 10);
        Color emptyColor = state.InteriorFillMode == CollatzInteriorFillMode.ByColoringMode
            ? SamplePalette(state.Palette, 0)
            : ResolveInteriorFillColor(state);
        int coloredRows = 0;
        Parallel.For(0, height, options, (y, loopState) =>
        {
            if (token.IsCancellationRequested) { loopState.Stop(); return; }
            int outputRow = y * stride;
            int densityRow = y * width;
            for (int x = 0; x < width; x++)
            {
                double normalized = logMaximum <= 0
                    ? 0
                    : Math.Pow(Math.Log(1 + density[densityRow + x]) / logMaximum, 1 / exposure);
                Color color = density[densityRow + x] == 0
                    ? emptyColor
                    : SamplePalette(state.Palette, normalized);
                int offset = outputRow + x * 4;
                pixels[offset] = color.B;
                pixels[offset + 1] = color.G;
                pixels[offset + 2] = color.R;
                pixels[offset + 3] = color.A;
            }
            int done = Interlocked.Increment(ref coloredRows);
            if (done == height || done % Math.Max(1, height / 100) == 0)
                progress?.Invoke(75 + done * 25 / height);
        });
    }

    private static int TraceDensityOrbit(CollatzState state, int x, int y, int width, int height,
        double scale, int[] path, CancellationToken token, out bool escaped)
    {
        var z = new Complex((double)state.CenterX + (x - width / 2d) * scale / width,
            (double)state.CenterY - (y - height / 2d) * scale / width);
        var q = new Complex((double)state.QRealParameter, (double)state.QImaginaryParameter);
        double thresholdSquared = (double)(state.Threshold * state.Threshold);
        int iteration = 0;
        int pathLength = 0;
        while (iteration < state.Iterations)
        {
            if ((iteration & 63) == 0 && token.IsCancellationRequested) break;
            double magnitudeSquared = z.Real * z.Real + z.Imaginary * z.Imaginary;
            if (!double.IsFinite(magnitudeSquared) || magnitudeSquared > thresholdSquared ||
                Math.Abs(z.Imaginary * Math.PI) > 700) break;
            z = ApplyFormula(z, state.Variation, (double)state.PParameter, q);
            iteration++;
            if (TryMapToPixel(z, (double)state.CenterX, (double)state.CenterY, scale,
                    width, height, out int pixelIndex))
                path[pathLength++] = pixelIndex;
        }
        escaped = iteration < state.Iterations && !token.IsCancellationRequested;
        return pathLength;
    }

    private static int TraceDensityOrbitDecimal(CollatzState state, int x, int y, int width, int height,
        decimal scale, int[] path, CancellationToken token, out bool escaped)
    {
        var z = new ComplexDecimal(state.CenterX + (x - width / 2m) * scale / width,
            state.CenterY - (y - height / 2m) * scale / width);
        var q = new ComplexDecimal(state.QRealParameter, state.QImaginaryParameter);
        int iteration = 0;
        int pathLength = 0;
        while (iteration < state.Iterations && IsWithinEscapeRadius(z, state.Threshold))
        {
            if ((iteration & 63) == 0 && token.IsCancellationRequested) break;
            if (Math.Abs(z.Imaginary * (decimal)Math.PI) > 60m) break;
            try
            {
                z = ApplyFormula(z, state.Variation, state.PParameter, q);
                iteration++;
                if (TryMapToPixel(z, state.CenterX, state.CenterY, scale,
                        width, height, out int pixelIndex))
                    path[pathLength++] = pixelIndex;
            }
            catch (OverflowException)
            {
                break;
            }
        }
        escaped = iteration < state.Iterations && !token.IsCancellationRequested;
        return pathLength;
    }

    private static bool TryMapToPixel(Complex z, double centerX, double centerY, double scale,
        int width, int height, out int pixelIndex)
    {
        double canvasX = (z.Real - centerX) * width / scale + width / 2d;
        double canvasY = height / 2d - (z.Imaginary - centerY) * width / scale;
        if (!double.IsFinite(canvasX) || !double.IsFinite(canvasY) ||
            canvasX < 0 || canvasX >= width || canvasY < 0 || canvasY >= height)
        {
            pixelIndex = 0;
            return false;
        }
        pixelIndex = (int)canvasY * width + (int)canvasX;
        return true;
    }

    private static bool TryMapToPixel(ComplexDecimal z, decimal centerX, decimal centerY, decimal scale,
        int width, int height, out int pixelIndex)
    {
        decimal canvasX = (z.Real - centerX) * width / scale + width / 2m;
        decimal canvasY = height / 2m - (z.Imaginary - centerY) * width / scale;
        if (canvasX < 0 || canvasX >= width || canvasY < 0 || canvasY >= height)
        {
            pixelIndex = 0;
            return false;
        }
        pixelIndex = (int)canvasY * width + (int)canvasX;
        return true;
    }

    internal static Complex ApplyFormula(Complex z, CollatzVariation variation, double p, Complex q)
    {
        Complex argument = Math.PI * z;
        return variation switch
        {
            CollatzVariation.SineVariation =>
                0.25 * (2 + 7 * z - (2 + 5 * z) * Complex.Sin(argument)),
            CollatzVariation.ParityBranchVariation =>
                0.5 * ((p - 1) * z + 1 - ((p - 1) * z - 1) * Complex.Cos(argument)),
            CollatzVariation.GeneralizedP => GeneralizedCollatz(z, p, Complex.Cos(argument)),
            CollatzVariation.GeneralizedPQ =>
                GeneralizedCollatz(z, p, Complex.Cos(argument)) + q * Complex.Sin(argument),
            _ => 0.25 * (2 + 7 * z - (2 + 5 * z) * Complex.Cos(argument))
        };
    }

    internal static ComplexDecimal ApplyFormula(ComplexDecimal z, CollatzVariation variation, decimal p,
        ComplexDecimal q)
    {
        ComplexDecimal argument = z * (decimal)Math.PI;
        var two = new ComplexDecimal(2, 0);
        return variation switch
        {
            CollatzVariation.SineVariation =>
                (two + z * 7 - (two + z * 5) * ComplexSin(argument)) / 4m,
            CollatzVariation.ParityBranchVariation =>
                ((p - 1) * z + 1 - ((p - 1) * z - 1) * ComplexCos(argument)) * 0.5m,
            CollatzVariation.GeneralizedP => GeneralizedCollatz(z, p, ComplexCos(argument)),
            CollatzVariation.GeneralizedPQ =>
                GeneralizedCollatz(z, p, ComplexCos(argument)) + q * ComplexSin(argument),
            _ => (two + z * 7 - (two + z * 5) * ComplexCos(argument)) / 4m
        };
    }

    private static Complex GeneralizedCollatz(Complex z, double p, Complex cosine) =>
        (2 + (2 * p + 1) * z - (2 + (2 * p - 1) * z) * cosine) / 4;

    private static ComplexDecimal GeneralizedCollatz(ComplexDecimal z, decimal p, ComplexDecimal cosine)
    {
        var two = new ComplexDecimal(2, 0);
        return (two + (2 * p + 1) * z - (two + (2 * p - 1) * z) * cosine) / 4m;
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

    private static bool IsWithinEscapeRadius(Complex z, double thresholdSquared)
    {
        double magnitudeSquared = z.Real * z.Real + z.Imaginary * z.Imaginary;
        return double.IsFinite(magnitudeSquared) && magnitudeSquared <= thresholdSquared &&
               Math.Abs(z.Imaginary * Math.PI) <= 700;
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

    private static Color ResolveColor(CollatzState state, OrbitMetrics metrics)
    {
        MandelbrotPalette palette = state.Palette;
        if (metrics.IsInterior && metrics.DetectedPeriod == 0 &&
            state.InteriorFillMode != CollatzInteriorFillMode.ByColoringMode)
            return ResolveInteriorFillColor(state);

        return state.ColoringMode switch
        {
            CollatzColoringMode.FinalArgument => SamplePalette(palette,
                MapFinalArgument(metrics.Final, state.ArgumentCycles)),
            CollatzColoringMode.FinalMagnitude => SamplePalette(palette,
                MapFinalMagnitude(metrics.Final, state.MagnitudeScale)),
            CollatzColoringMode.CycleBasins => metrics.DetectedPeriod > 0
                ? SamplePalette(palette, metrics.CycleKey)
                : palette.InteriorColor,
            CollatzColoringMode.IntegerTrap => SamplePalette(palette,
                MapTrap(metrics.IntegerTrap, state.TrapScale)),
            CollatzColoringMode.RealAxisTrap => SamplePalette(palette,
                MapTrap(metrics.RealAxisTrap, state.TrapScale)),
            CollatzColoringMode.PeriodDetection => metrics.DetectedPeriod > 0
                ? SamplePalette(palette, PositiveModulo(metrics.DetectedPeriod * 0.6180339887498949, 1))
                : palette.InteriorColor,
            _ => ResolveEscapeColor(state, metrics.Iterations, metrics.Smooth)
        };
    }

    private static Color ResolveInteriorFillColor(CollatzState state) => state.InteriorFillMode switch
    {
        CollatzInteriorFillMode.Auto => SamplePalette(state.Palette, 0),
        CollatzInteriorFillMode.Black => Colors.Black,
        CollatzInteriorFillMode.White => Colors.White,
        CollatzInteriorFillMode.Custom => state.CustomInteriorColor,
        _ => state.Palette.InteriorColor
    };

    private static double MapFinalArgument(Complex value, double cycles)
    {
        if (!double.IsFinite(value.Real) || !double.IsFinite(value.Imaginary)) return 0;
        return PositiveModulo((Math.Atan2(value.Imaginary, value.Real) / (2 * Math.PI) + 0.5) *
                              Math.Clamp(cycles, 0.1, 20), 1);
    }

    private static double MapFinalMagnitude(Complex value, double scale)
    {
        double magnitude = Complex.Abs(value);
        if (!double.IsFinite(magnitude)) return 1;
        return 1 - Math.Exp(-Math.Clamp(scale, 0.01, 20) * Math.Log(1 + magnitude));
    }

    private static double MapTrap(double distance, double scale)
    {
        if (!double.IsFinite(distance)) return 0;
        return Math.Exp(-Math.Clamp(scale, 0.01, 100) * Math.Max(0, distance));
    }

    private static Color ResolveEscapeColor(CollatzState state, int iteration, double smooth)
    {
        MandelbrotPalette palette = state.Palette;
        if (iteration >= state.Iterations) return palette.InteriorColor;
        double period = palette.AlignWithRenderIterations ? state.Iterations : Math.Max(1, palette.ColorPeriod);
        double value = state.UseSmoothColoring ? smooth : iteration;
        if (palette.UsesAlgorithmicGrayscale)
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
        if (!double.IsFinite(normalized)) normalized = 0;
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

    // Таблица гаммы на 256 значений, кэш на поток (гамма постоянна в пределах рендера).
    // Бит-в-бит совпадает с прямым Math.Pow.
    [ThreadStatic] private static double _gammaLutKey;
    [ThreadStatic] private static byte[]? _gammaLut;

    private static Color ApplyGamma(Color color, double gamma)
    {
        byte[]? lut = _gammaLut;
        if (lut is null || _gammaLutKey != gamma)
        {
            lut = new byte[256];
            double correction = 1 / Math.Max(0.01, gamma);
            for (int value = 0; value < 256; value++)
                lut[value] = (byte)(255 * Math.Pow(value / 255d, correction));
            _gammaLut = lut;
            _gammaLutKey = gamma;
        }
        return Color.FromArgb(color.A, lut[color.R], lut[color.G], lut[color.B]);
    }

    private static byte Lerp(byte start, byte end, double amount) =>
        (byte)Math.Round(start + (end - start) * amount);

    private static double PositiveModulo(double value, double period) =>
        (value % period + period) % period;

    // Пер-поточные буферы для режима Orbit Density: путь орбиты и локальная плотность.
    private sealed class OrbitDensityScratch(int[] path, long[] density)
    {
        public readonly int[] Path = path;
        public readonly long[] Density = density;
    }
}
