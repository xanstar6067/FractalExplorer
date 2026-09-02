using System.Diagnostics;
using System.Numerics;
using perturbation_theory.Core.Rendering;
using perturbation_theory.Models;

// No test packages or WPF dependency. The oracle uses independent 192-bit fixed-point
// direct iteration, not the production fallback or perturbation equations.
int failed = 0;
Run("Overview: all three modes vs 192-bit direct iteration", () =>
{
    var settings = new MandelbrotSettings { Iterations = 300 };
    CompareGrid(settings, 41, 31, 3.8m, 0.0001);
});
Run("Escaping reference: rebasing continues to the pixel's iteration limit", () =>
{
    var settings = new MandelbrotSettings { CenterX = 1.2m, CenterY = 0.1m, Iterations = 500 };
    CompareGrid(settings, 23, 17, 5m, 0.001);
    var renderer = new PerturbationRenderer(settings);
    var pixel = renderer.EvaluateOffset(-2.96m, -0.099m); // c = -1.76 + 0.001i, inside the period-three bulb
    Check(renderer.ReferenceIterations < settings.Iterations, "Reference must escape early.");
    Check(!pixel.Escaped && pixel.Iterations == settings.Iterations && pixel.Rebases > 0,
        "Pixel must continue after the short reference ends.");
    Check(!renderer.EvaluateOffset(-3.2m, -0.1m).Escaped, "The exact real endpoint c=-2 must remain bounded.");
});
Run("Deep zoom 1e12 vs 192-bit direct iteration", () =>
{
    CompareGrid(new MandelbrotSettings
    {
        CenterX = -0.743643887037151m, CenterY = 0.131825904205330m,
        Zoom = 1e12m, Iterations = 5000
    }, 11, 7, 3e-12m, 0.002, decimalModesOnly: true);
});
Run("Sub-double offsets at 1e18 remain distinct", () =>
{
    var settings = new MandelbrotSettings
    {
        CenterX = -0.7436438870371587047521915061m,
        CenterY = 0.1318259042053119704931320563m,
        Zoom = 1e18m, Iterations = 12000
    };
    Check((double)settings.CenterX == (double)(settings.CenterX + 1e-20m), "Test must be below double coordinate precision.");
    CompareGrid(settings, 9, 7, 3e-18m, 0.02, decimalModesOnly: true, requireVariation: true);
});
Run("Final-iteration escape and both coloring modes", () =>
{
    foreach (PrecisionMode precision in Enum.GetValues<PrecisionMode>())
    {
        var settings = new MandelbrotSettings { CenterX = 1m, Iterations = 3, Precision = precision };
        var sample = new PerturbationRenderer(settings).EvaluateOffset(0, 0);
        Check(sample.Escaped && sample.Iterations == 3, "c=1 escapes at the final allowed iteration.");
        Check(double.IsFinite(sample.Smooth), "Smooth count must be finite.");
        foreach (ColoringMode mode in Enum.GetValues<ColoringMode>())
        {
            var sampler = new PaletteSampler(settings with { Coloring = mode, Palette = BuiltInPalette.All[8] });
            Check(sampler.Sample(sample) != new Rgb(0, 0, 0), "Escaped pixel must not be painted as interior.");
            Check(sampler.Sample(new PixelSample(3, 3, false)) == new Rgb(0, 0, 0), "Interior must be black.");
        }
    }
});
Run("Severe cancellation takes the explicit direct fallback", () =>
{
    foreach (PrecisionMode mode in Enum.GetValues<PrecisionMode>())
    {
        var settings = new MandelbrotSettings { CenterX = 0.2m, CenterY = 0.6m, Precision = mode };
        var renderer = new PerturbationRenderer(settings);
        // A period-three center approaches z=0 after its third step, causing cancellation
        // against this unrelated reference orbit. It is outside the analytic shortcuts.
        const decimal real = -0.1225611668766536199752455518m;
        const decimal imaginary = 0.7448617666197442365931704286m;
        PixelSample sample = renderer.EvaluateOffset(real - settings.CenterX, imaginary - settings.CenterY);
        Check(sample.UsedFallback, $"{mode}: cancellation guard was not exercised.");
        Check(!sample.Escaped, $"{mode}: the period-three center must remain bounded.");
    }
});
Run("Odd-sized tiles, alpha and deterministic parallel output", () =>
{
    var settings = new MandelbrotSettings { Iterations = 200, Palette = BuiltInPalette.All[2] };
    byte[] single = Frame(settings with { Threads = 1 }, 101, 77);
    byte[] parallel = Frame(settings with { Threads = 0 }, 101, 77);
    Check(single.SequenceEqual(parallel), "Thread count changed the image.");
    for (int i = 3; i < single.Length; i += 4) Check(single[i] == 255, "Tile gap or bad alpha.");
});
Run("Cancellation before reference construction and during rendering", () =>
{
    using var cancelled = new CancellationTokenSource();
    cancelled.Cancel();
    Expect<OperationCanceledException>(() => new PerturbationRenderer(new(), cancelled.Token));
    var renderer = new PerturbationRenderer(new());
    Expect<OperationCanceledException>(() => renderer.EvaluateOffset(0, 0, cancelled.Token));
    using var during = new CancellationTokenSource();
    Expect<OperationCanceledException>(() => renderer.Render(100, 80, during.Token, _ => during.Cancel()));
});
Run("Parameter and decimal resolution limits", () =>
{
    Expect<ArgumentException>(() => new MandelbrotSettings { Zoom = 0 }.Validate());
    Expect<ArgumentException>(() => new MandelbrotSettings { EscapeRadius = 1 }.Validate());
    Expect<ArgumentException>(() => new MandelbrotSettings { Iterations = 0 }.Validate());
    // The frame is rejected only once the pixel step underflows decimal (< 1e-28); the
    // softer "center + step == center" precision wall is intentionally left open now.
    Expect<ArgumentException>(() => new MandelbrotSettings { Zoom = 1e26m }.ValidateSurface(8000, 100));
    new MandelbrotSettings { Zoom = 1e24m }.ValidateSurface(1920, 1080);
    new MandelbrotSettings { Zoom = 1e18m }.ValidateSurface(1920, 1080);
});

Console.WriteLine(failed == 0 ? "All numerical and rendering checks passed." : $"FAILED: {failed} checks.");
if (failed == 0 && args.Contains("--benchmark")) Benchmark();
return failed == 0 ? 0 : 1;

void Run(string name, Action action)
{
    var clock = Stopwatch.StartNew();
    try { action(); Console.WriteLine($"PASS {name} ({clock.Elapsed.TotalSeconds:F2}s)"); }
    catch (Exception ex) { failed++; Console.WriteLine($"FAIL {name}: {ex.Message}"); }
}

static void CompareGrid(MandelbrotSettings settings, int width, int height, decimal viewWidth, double tolerance,
    bool decimalModesOnly = false, bool requireVariation = false)
{
    PrecisionMode[] modes = decimalModesOnly ? [PrecisionMode.DecimalReference, PrecisionMode.Decimal] : Enum.GetValues<PrecisionMode>();
    var renderers = modes.Select(mode => new PerturbationRenderer(settings with { Precision = mode })).ToArray();
    var counts = new HashSet<int>();
    var clock = Stopwatch.StartNew();
    for (int y = 0; y < height; y++)
    for (int x = 0; x < width; x++)
    {
        decimal dx = ((decimal)x / width - 0.5m) * viewWidth;
        decimal dy = (0.5m - (decimal)y / height) * viewWidth * height / width;
        PixelSample expected = Oracle(settings.CenterX + dx, settings.CenterY + dy, settings.Iterations, settings.EscapeRadius);
        counts.Add(expected.Iterations);
        for (int i = 0; i < modes.Length; i++)
        {
            PixelSample actual = renderers[i].EvaluateOffset(dx, dy);
            Check(actual.Escaped == expected.Escaped && actual.Iterations == expected.Iterations,
                $"{modes[i]} at ({x},{y}): expected {expected.Iterations}/{expected.Escaped}, got {actual.Iterations}/{actual.Escaped}.");
            Check(Math.Abs(actual.Smooth - expected.Smooth) < tolerance,
                $"{modes[i]} at ({x},{y}): smooth error {Math.Abs(actual.Smooth - expected.Smooth):G5}.");
        }
    }
    if (requireVariation) Check(counts.Count > 1, "Deep view must contain distinct escape counts.");
    Console.WriteLine($"  {width * height} points × {modes.Length} modes, {counts.Count} distinct escape counts, {clock.Elapsed.TotalSeconds:F2}s");
}

static PixelSample Oracle(decimal cr, decimal ci, int limit, decimal radius)
{
    const int bits = 192;
    BigInteger real = Fixed(cr), imaginary = Fixed(ci);
    BigInteger re = 0, im = 0, radiusSquared = Fixed(radius * radius);
    for (int n = 1; n <= limit; n++)
    {
        (re, im) = (((re * re - im * im) >> bits) + real, ((2 * re * im) >> bits) + imaginary);
        BigInteger norm = (re * re + im * im) >> bits;
        if (norm > radiusSquared)
        {
            double magnitude = (double)norm / Math.Pow(2, bits);
            double smooth = n + 1 - Math.Log2(Math.Log2(Math.Sqrt(magnitude)));
            return new PixelSample(n, smooth, true);
        }
    }
    return new PixelSample(limit, limit, false);

    static BigInteger Fixed(decimal value)
    {
        int[] data = decimal.GetBits(value);
        BigInteger numerator = (uint)data[0] + ((BigInteger)(uint)data[1] << 32) + ((BigInteger)(uint)data[2] << 64);
        int scale = (data[3] >> 16) & 0xff;
        if (data[3] < 0) numerator = -numerator;
        return (numerator << bits) / BigInteger.Pow(10, scale);
    }
}

static byte[] Frame(MandelbrotSettings settings, int width, int height)
{
    var pixels = new byte[width * height * 4];
    int progress = 0;
    RenderStatistics stats = new PerturbationRenderer(settings).Render(width, height, CancellationToken.None, tile =>
    {
        for (int y = 0; y < tile.Height; y++)
            Buffer.BlockCopy(tile.Pixels, y * tile.Width * 4, pixels, ((tile.Y + y) * width + tile.X) * 4, tile.Width * 4);
    }, p => Interlocked.Exchange(ref progress, p));
    Check(stats.Pixels == width * height, "Pixel count mismatch.");
    Check(progress == 100, "Missing final progress.");
    return pixels;
}

static void Check(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

static void Expect<T>(Action action) where T : Exception
{
    try { action(); }
    catch (T) { return; }
    throw new InvalidOperationException($"Expected {typeof(T).Name}.");
}

static void Benchmark()
{
    const int width = 128, height = 80;
    var settings = new MandelbrotSettings
    {
        CenterX = -0.743643887037151m, CenterY = 0.131825904205330m,
        Zoom = 1e12m, Iterations = 4000
    };
    Console.WriteLine($"Benchmark: {width}x{height}, zoom 1e12, 4000 iterations, {Environment.ProcessorCount} logical CPUs (Release recommended).");
    foreach (PrecisionMode mode in Enum.GetValues<PrecisionMode>())
    {
        var clock = Stopwatch.StartNew();
        var renderer = new PerturbationRenderer(settings with { Precision = mode });
        RenderStatistics stats = renderer.Render(width, height, CancellationToken.None, _ => { });
        Console.WriteLine($"  Perturbation {mode,-16}: {clock.Elapsed.TotalMilliseconds:F1} ms; fallbacks {stats.FallbackPixels}/{stats.Pixels}");
    }
    var baseline = Stopwatch.StartNew();
    var iterations = new int[width * height];
    Parallel.For(0, height, y =>
    {
        decimal ci = settings.CenterY + (0.5m - (decimal)y / height) * (3m / settings.Zoom) * height / width;
        for (int x = 0; x < width; x++)
        {
            decimal cr = settings.CenterX + ((decimal)x / width - 0.5m) * (3m / settings.Zoom);
            decimal re = 0, im = 0;
            int n = 0;
            while (n < settings.Iterations && re * re + im * im <= 4m)
            {
                (re, im) = (re * re - im * im + cr, 2m * re * im + ci);
                n++;
            }
            iterations[y * width + x] = n;
        }
    });
    Console.WriteLine($"  Direct decimal (no coloring): {baseline.Elapsed.TotalMilliseconds:F1} ms; checksum {iterations.Sum()}");
}
