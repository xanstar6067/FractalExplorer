using System.Diagnostics;
using perturbation_theory.Models;

namespace perturbation_theory.Core.Rendering;

public readonly record struct PixelSample(int Iterations, double Smooth, bool Escaped,
    int Rebases = 0, bool UsedFallback = false);
public sealed record RenderedTile(int X, int Y, int Width, int Height, byte[] Pixels);
public sealed record RenderStatistics(TimeSpan Elapsed, TimeSpan ReferenceTime, int ReferenceIterations,
    long Rebases, long FallbackPixels, long Pixels);

/// <summary>
/// Mandelbrot z²+c. Reference Z starts at zero; each pixel follows
/// dz' = 2*Z*dz + dz² + dc. The reference and pixel indices are independent.
/// Rebasing: https://mathr.co.uk/web/deep-zoom.html#rebasing
/// Rendering, smooth escape and BGRA output are adapted from the main WPF renderer.
/// </summary>
public sealed class PerturbationRenderer
{
    private readonly record struct DoublePoint(double Re, double Im);
    private readonly record struct DecimalPoint(decimal Re, decimal Im);
    private readonly MandelbrotSettings _settings;
    private readonly DoublePoint[] _orbit;
    private readonly DecimalPoint[]? _decimalOrbit;
    private readonly int _referenceLength;
    private readonly TimeSpan _referenceTime;
    private readonly double _radiusSquared;

    public PerturbationRenderer(MandelbrotSettings settings, CancellationToken token = default)
    {
        settings.Validate();
        token.ThrowIfCancellationRequested();
        _settings = settings;
        _radiusSquared = (double)(settings.EscapeRadius * settings.EscapeRadius);
        var clock = Stopwatch.StartNew();
        _orbit = new DoublePoint[settings.Iterations + 1];
        if (settings.Precision == PrecisionMode.Decimal)
            _decimalOrbit = new DecimalPoint[settings.Iterations + 1];

        // Stop at escape; rebasing lets other pixels continue beyond a short reference.
        // Never square an already escaped reference (important for decimal overflow).
        if (settings.Precision == PrecisionMode.Double)
        {
            double re = 0, im = 0;
            double cr = (double)settings.CenterX, ci = (double)settings.CenterY;
            for (int n = 1; n <= settings.Iterations; n++)
            {
                if ((n & 255) == 0) token.ThrowIfCancellationRequested();
                (re, im) = (re * re - im * im + cr, 2 * re * im + ci);
                _orbit[n] = new DoublePoint(re, im);
                _referenceLength = n;
                if (re * re + im * im > _radiusSquared) break;
            }
        }
        else
        {
            decimal re = 0, im = 0;
            decimal radiusSquared = settings.EscapeRadius * settings.EscapeRadius;
            for (int n = 1; n <= settings.Iterations; n++)
            {
                if ((n & 255) == 0) token.ThrowIfCancellationRequested();
                (re, im) = (re * re - im * im + settings.CenterX, 2m * re * im + settings.CenterY);
                _orbit[n] = new DoublePoint((double)re, (double)im);
                if (_decimalOrbit is not null) _decimalOrbit[n] = new DecimalPoint(re, im);
                _referenceLength = n;
                if (re * re + im * im > radiusSquared) break;
            }
        }
        token.ThrowIfCancellationRequested();
        _referenceTime = clock.Elapsed;
    }

    public int ReferenceIterations => _referenceLength;

    /// <summary>
    /// Offsets are formed BEFORE converting to double. Adding them to a double center
    /// first would erase adjacent pixels at deep zoom and defeat perturbation entirely.
    /// </summary>
    public PixelSample EvaluateOffset(decimal deltaReal, decimal deltaImaginary, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        decimal pixelReal = _settings.CenterX + deltaReal;
        decimal pixelImaginary = _settings.CenterY + deltaImaginary;
        // The real interval is exactly bounded, including the repelling endpoint -2.
        // Do not let tiny reference rounding errors turn this endpoint into an escape.
        if (pixelImaginary == 0 && pixelReal is >= -2m and <= 0.25m) return Interior();
        double cr = (double)pixelReal;
        double ci = (double)pixelImaginary;
        if (IsSafelyInside(cr, ci)) return Interior();
        return _settings.Precision == PrecisionMode.Decimal
            ? IterateDecimal(deltaReal, deltaImaginary, token)
            : IterateDouble(deltaReal, deltaImaginary, token);
    }

    private PixelSample IterateDouble(decimal dcReal, decimal dcImaginary, CancellationToken token)
    {
        double cr = (double)dcReal, ci = (double)dcImaginary;
        double dr = 0, di = 0;
        int referenceIndex = 0, rebases = 0;
        for (int n = 1; n <= _settings.Iterations; n++)
        {
            if ((n & 127) == 0) token.ThrowIfCancellationRequested();
            DoublePoint z = _orbit[referenceIndex];
            (dr, di) = (2 * (z.Re * dr - z.Im * di) + dr * dr - di * di + cr,
                2 * (z.Re * di + z.Im * dr + dr * di) + ci);
            DoublePoint next = _orbit[++referenceIndex];
            double wr = next.Re + dr, wi = next.Im + di;
            double magnitudeSquared = wr * wr + wi * wi;
            double referenceSquared = next.Re * next.Re + next.Im * next.Im;

            // Severe cancellation can lose significant digits before rebasing repairs
            // the representation. Recompute only this pixel, and count it explicitly.
            if (!double.IsFinite(magnitudeSquared) || magnitudeSquared < 1e-12 * referenceSquared)
                return DirectFallback(dcReal, dcImaginary, rebases, token);
            if (magnitudeSquared > _radiusSquared) return Escaped(n, magnitudeSquared, rebases);

            if (magnitudeSquared < dr * dr + di * di || referenceIndex == _referenceLength)
            {
                dr = wr;
                di = wi;
                referenceIndex = 0;
                rebases++;
            }
        }
        return Interior(rebases);
    }

    private PixelSample IterateDecimal(decimal cr, decimal ci, CancellationToken token)
    {
        DecimalPoint[] orbit = _decimalOrbit!;
        // decimal has a fixed minimum quantum of 1e-28. Store dz = scale*d instead:
        // d' = 2*Z*d + scale*d² + dc/scale. This keeps tiny deep-zoom deltas from
        // losing significant digits on every multiplication. Multiply scale*d first
        // in the quadratic term to avoid overflowing the large scaled d².
        const decimal scale = 0.000000000001m;
        decimal scaledCr = cr / scale, scaledCi = ci / scale;
        decimal dr = 0, di = 0;
        decimal radiusSquared = _settings.EscapeRadius * _settings.EscapeRadius;
        int referenceIndex = 0, rebases = 0;
        for (int n = 1; n <= _settings.Iterations; n++)
        {
            if ((n & 127) == 0) token.ThrowIfCancellationRequested();
            DecimalPoint z = orbit[referenceIndex];
            decimal sr = scale * dr, si = scale * di;
            (dr, di) = (2m * (z.Re * dr - z.Im * di) + sr * dr - si * di + scaledCr,
                2m * (z.Re * di + z.Im * dr + sr * di) + scaledCi);
            DecimalPoint next = orbit[++referenceIndex];
            sr = scale * dr;
            si = scale * di;
            decimal wr = next.Re + sr, wi = next.Im + si;
            decimal magnitudeSquared = wr * wr + wi * wi;
            // Use double only for the tiny cancellation ratio to avoid decimal
            // underflow when squaring a very small number. Orbit iteration stays decimal.
            double magnitude = (double)wr * (double)wr + (double)wi * (double)wi;
            double reference = (double)next.Re * (double)next.Re + (double)next.Im * (double)next.Im;
            if (magnitude < 1e-24 * reference)
                return DirectFallback(cr, ci, rebases, token);
            if (magnitudeSquared > radiusSquared) return Escaped(n, (double)magnitudeSquared, rebases);
            if (magnitudeSquared < sr * sr + si * si || referenceIndex == _referenceLength)
            {
                dr = wr / scale;
                di = wi / scale;
                referenceIndex = 0;
                rebases++;
            }
        }
        return Interior(rebases);
    }

    private PixelSample DirectFallback(decimal deltaReal, decimal deltaImaginary, int rebases, CancellationToken token)
    {
        // This is a numerical safety path, never the normal frame renderer.
        if (_settings.Precision == PrecisionMode.Double)
        {
            double cr = (double)_settings.CenterX + (double)deltaReal;
            double ci = (double)_settings.CenterY + (double)deltaImaginary;
            double re = 0, im = 0;
            for (int n = 1; n <= _settings.Iterations; n++)
            {
                if ((n & 127) == 0) token.ThrowIfCancellationRequested();
                (re, im) = (re * re - im * im + cr, 2 * re * im + ci);
                double norm = re * re + im * im;
                if (norm > _radiusSquared) return Escaped(n, norm, rebases) with { UsedFallback = true };
            }
        }
        else
        {
            decimal cr = _settings.CenterX + deltaReal, ci = _settings.CenterY + deltaImaginary;
            decimal re = 0, im = 0;
            decimal radiusSquared = _settings.EscapeRadius * _settings.EscapeRadius;
            for (int n = 1; n <= _settings.Iterations; n++)
            {
                if ((n & 127) == 0) token.ThrowIfCancellationRequested();
                (re, im) = (re * re - im * im + cr, 2m * re * im + ci);
                decimal norm = re * re + im * im;
                if (norm > radiusSquared) return Escaped(n, (double)norm, rebases) with { UsedFallback = true };
            }
        }
        return Interior(rebases) with { UsedFallback = true };
    }

    private PixelSample Interior(int rebases = 0) => new(_settings.Iterations, _settings.Iterations, false, rebases);

    private static PixelSample Escaped(int n, double magnitudeSquared, int rebases)
    {
        // Same normalized escape count as the source WPF Mandelbrot renderer.
        double logZn = Math.Log(magnitudeSquared) / 2;
        double nu = Math.Log(logZn / Math.Log(2)) / Math.Log(2);
        return new PixelSample(n, double.IsFinite(nu) ? n + 1 - nu : n, true, rebases);
    }

    private static bool IsSafelyInside(double re, double im)
    {
        double y2 = im * im;
        double x = re - 0.25;
        double q = x * x + y2;
        // A conservative margin prevents rounded deep boundary points being declared interior.
        return q * (q + x) < 0.25 * y2 - 1e-14 || (re + 1) * (re + 1) + y2 < 0.0625 - 1e-14;
    }

    public RenderStatistics Render(int width, int height, CancellationToken token,
        Action<RenderedTile> publishTile, Action<int>? progress = null)
    {
        _settings.ValidateSurface(width, height);
        token.ThrowIfCancellationRequested();
        var clock = Stopwatch.StartNew();
        var sampler = new PaletteSampler(_settings);
        decimal viewWidth = 3m / _settings.Zoom;
        decimal viewHeight = viewWidth * height / width;
        var realOffsets = new decimal[width];
        var imaginaryOffsets = new decimal[height];
        for (int x = 0; x < width; x++) realOffsets[x] = ((decimal)x / width - 0.5m) * viewWidth;
        for (int y = 0; y < height; y++) imaginaryOffsets[y] = (0.5m - (decimal)y / height) * viewHeight;

        const int tileSize = 48;
        var tiles = new List<(int X, int Y)>();
        for (int y = 0; y < height; y += tileSize)
            for (int x = 0; x < width; x += tileSize) tiles.Add((x, y));
        // Keep the source window's progressive center-first rendering, without grid overlays.
        tiles.Sort((a, b) => Distance(a).CompareTo(Distance(b)));
        double Distance((int X, int Y) tile) => Math.Pow(tile.X + tileSize / 2.0 - width / 2.0, 2)
            + Math.Pow(tile.Y + tileSize / 2.0 - height / 2.0, 2);

        int completed = 0;
        object progressLock = new();
        long rebases = 0, fallbackPixels = 0;
        var options = new ParallelOptions
        {
            CancellationToken = token,
            MaxDegreeOfParallelism = _settings.Threads == 0 ? Environment.ProcessorCount : _settings.Threads
        };
        Parallel.ForEach(tiles, options, tile =>
        {
            int tw = Math.Min(tileSize, width - tile.X), th = Math.Min(tileSize, height - tile.Y);
            var pixels = new byte[tw * th * 4];
            long tileRebases = 0, tileFallbacks = 0;
            for (int y = 0; y < th; y++)
            {
                token.ThrowIfCancellationRequested();
                for (int x = 0; x < tw; x++)
                {
                    PixelSample sample = EvaluateOffset(realOffsets[tile.X + x], imaginaryOffsets[tile.Y + y], token);
                    Rgb color = sampler.Sample(sample);
                    int offset = (y * tw + x) * 4;
                    pixels[offset] = color.B;
                    pixels[offset + 1] = color.G;
                    pixels[offset + 2] = color.R;
                    pixels[offset + 3] = 255;
                    tileRebases += sample.Rebases;
                    if (sample.UsedFallback) tileFallbacks++;
                }
            }
            token.ThrowIfCancellationRequested();
            publishTile(new RenderedTile(tile.X, tile.Y, tw, th, pixels));
            Interlocked.Add(ref rebases, tileRebases);
            Interlocked.Add(ref fallbackPixels, tileFallbacks);
            // Serialize reports: a delayed worker must not publish 99% after 100%.
            lock (progressLock)
            {
                completed++;
                progress?.Invoke(completed * 100 / tiles.Count);
            }
        });
        return new RenderStatistics(clock.Elapsed + _referenceTime, _referenceTime, _referenceLength,
            rebases, fallbackPixels, (long)width * height);
    }
}
