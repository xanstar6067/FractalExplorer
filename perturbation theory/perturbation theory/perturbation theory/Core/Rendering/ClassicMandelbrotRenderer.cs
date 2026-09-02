using perturbation_theory.Models;

namespace perturbation_theory.Core.Rendering;

/// <summary>
/// Direct Mandelbrot iteration extracted from the main WPF MandelbrotFamilyRenderer:
/// IterateAt, Iterate/IterateDecimal, SquareAdd and IsInsideMandelbrot.
/// Retains the original automatic precision threshold, interior tests, operation order,
/// escape-limit convention and smooth count. Only the other fractal/coloring variants
/// have been removed; cancellation polling has been added for responsive switching.
/// Raster scheduling and palettes are shared with perturbation for in-window comparison.
/// </summary>
public sealed class ClassicMandelbrotRenderer
{
    public const decimal DecimalIterationZoomThreshold = 1_500_000_000m;
    private readonly MandelbrotSettings _settings;

    public ClassicMandelbrotRenderer(MandelbrotSettings settings)
    {
        settings.Validate();
        _settings = settings;
    }

    public static bool UsesDecimal(decimal zoom) => zoom > DecimalIterationZoomThreshold;

    public PixelSample EvaluateOffset(decimal deltaReal, decimal deltaImaginary, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        decimal re = _settings.CenterX + deltaReal;
        decimal im = _settings.CenterY + deltaImaginary;
        return UsesDecimal(_settings.Zoom)
            ? IterateDecimal(re, im, token)
            : Iterate((double)re, (double)im, token);
    }

    private PixelSample Iterate(double cr, double ci, CancellationToken token)
    {
        if (IsInsideMandelbrot(cr, ci)) return Interior();
        double zr = 0, zi = 0;
        double thresholdSquared = (double)(_settings.EscapeRadius * _settings.EscapeRadius);
        int iterations = 0;
        while (iterations < _settings.Iterations && zr * zr + zi * zi <= thresholdSquared)
        {
            if ((iterations & 127) == 0) token.ThrowIfCancellationRequested();
            double real = zr * zr - zi * zi + cr;
            zi = 2 * zr * zi + ci;
            zr = real;
            iterations++;
        }
        return Metrics(iterations, zr * zr + zi * zi);
    }

    private PixelSample IterateDecimal(decimal cr, decimal ci, CancellationToken token)
    {
        if (IsInsideMandelbrot(cr, ci)) return Interior();
        decimal zr = 0, zi = 0;
        decimal thresholdSquared = _settings.EscapeRadius * _settings.EscapeRadius;
        int iterations = 0;
        while (iterations < _settings.Iterations && zr * zr + zi * zi <= thresholdSquared)
        {
            if ((iterations & 127) == 0) token.ThrowIfCancellationRequested();
            try
            {
                decimal real = zr * zr - zi * zi + cr;
                zi = 2 * zr * zi + ci;
                zr = real;
            }
            catch (OverflowException)
            {
                zr = _settings.EscapeRadius + 1;
                zi = 0;
            }
            iterations++;
        }
        return Metrics(iterations, (double)(zr * zr + zi * zi));
    }

    private PixelSample Metrics(int iterations, double magnitudeSquared)
    {
        double smooth = iterations;
        if (iterations < _settings.Iterations && magnitudeSquared > 1)
        {
            double logZn = Math.Log(magnitudeSquared) / 2;
            const double smoothingPower = 2;
            double nu = Math.Log(Math.Max(logZn, 1e-300) / Math.Log(smoothingPower)) /
                        Math.Log(smoothingPower);
            if (double.IsFinite(nu)) smooth = iterations + 1 - nu;
        }
        // Preserve the original renderer's convention at the iteration limit.
        return new PixelSample(iterations, smooth, iterations < _settings.Iterations);
    }

    private PixelSample Interior() => new(_settings.Iterations, _settings.Iterations, false);

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

    public RenderStatistics Render(int width, int height, CancellationToken token,
        Action<RenderedTile> publishTile, Action<int>? progress = null) =>
        MandelbrotFrameRenderer.Render(_settings, EvaluateOffset, TimeSpan.Zero, 0,
            width, height, token, publishTile, progress);
}
