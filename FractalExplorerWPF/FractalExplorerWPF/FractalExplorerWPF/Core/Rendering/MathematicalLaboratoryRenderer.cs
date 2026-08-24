using System.Numerics;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FractalExplorerWPF.Models;
using Color = System.Windows.Media.Color;

namespace FractalExplorerWPF.Core.Rendering;

public static class MathematicalLaboratoryRenderer
{
    public static async Task<BitmapSource> RenderBitmapAsync(
        MathematicalLaboratoryState state,
        int width,
        int height,
        CancellationToken token,
        IProgress<int>? progress = null)
    {
        width = Math.Clamp(width, 64, 8_192);
        height = Math.Clamp(height, 64, 8_192);
        MathematicalLaboratoryState snapshot = state.Clone();
        progress?.Report(2);
        byte[] pixels = await Task.Run(() => RenderPixels(snapshot, width, height, token, progress), token);
        token.ThrowIfCancellationRequested();
        BitmapSource bitmap = BitmapSource.Create(
            width, height, 96, 96, PixelFormats.Bgra32, null, pixels, width * 4);
        bitmap.Freeze();
        progress?.Report(100);
        return bitmap;
    }

    private static byte[] RenderPixels(
        MathematicalLaboratoryState state,
        int width,
        int height,
        CancellationToken token,
        IProgress<int>? progress)
    {
        var canvas = new RasterCanvas(width, height, state);
        canvas.Clear(state.BackgroundColor);
        progress?.Report(8);
        switch (state.Kind)
        {
            case MathematicalLaboratoryKind.ModularArithmetic:
                RenderModular(canvas, state, token);
                break;
            case MathematicalLaboratoryKind.PascalModulo:
                RenderPascal(canvas, state, token, progress);
                break;
            case MathematicalLaboratoryKind.RationalNumbers:
                RenderRationals(canvas, state, token);
                break;
            case MathematicalLaboratoryKind.PrimeGeometry:
                RenderPrimes(canvas, state, token, progress);
                break;
            case MathematicalLaboratoryKind.Phyllotaxis:
                RenderPhyllotaxis(canvas, state, token);
                break;
            case MathematicalLaboratoryKind.CircleInversion:
                RenderInversion(canvas, state, token);
                break;
            case MathematicalLaboratoryKind.AperiodicTilings:
                RenderTilings(canvas, state, token);
                break;
            case MathematicalLaboratoryKind.HyperbolicGeometry:
                RenderHyperbolic(canvas, state, token);
                break;
            case MathematicalLaboratoryKind.FourierEpicycles:
                RenderFourier(canvas, state, token, progress);
                break;
            case MathematicalLaboratoryKind.ChladniWaveInterference:
                RenderChladniWaves(canvas, state, token, progress);
                break;
        }
        progress?.Report(98);
        return canvas.Pixels;
    }

    private static void RenderModular(RasterCanvas canvas, MathematicalLaboratoryState state, CancellationToken token)
    {
        int modulus = Math.Clamp(state.PrimaryValue, 10, 2_000);
        int a = state.SecondaryValue;
        int value = state.TertiaryValue;
        double radius = 0.9;
        if (state.ShowGuides)
            canvas.Circle(0, 0, radius, 1.2, Mix(state.PrimaryColor, state.BackgroundColor, 0.55), false);

        for (int x = 0; x < modulus; x++)
        {
            if ((x & 127) == 0) token.ThrowIfCancellationRequested();
            int target = state.Mode switch
            {
                0 => Mod((long)a * x, modulus),
                1 => Mod((long)a * x + value, modulus),
                2 => Mod((long)x * x + value, modulus),
                3 => PowMod(x, Math.Clamp(Math.Abs(value), 2, 31), modulus),
                4 => x % 2 == 0 ? x / 2 : Mod(3L * x + 1, modulus),
                _ => Mod((long)a * x, modulus)
            };
            double angle1 = -Math.PI / 2 + Math.Tau * x / modulus;
            double angle2 = -Math.PI / 2 + Math.Tau * target / modulus;
            Color color = Palette(state, (double)x / modulus);
            canvas.Line(radius * Math.Cos(angle1), radius * Math.Sin(angle1),
                radius * Math.Cos(angle2), radius * Math.Sin(angle2),
                Math.Clamp(state.Parameter, 0.25, 8), color, modulus > 700 ? 0.42 : 0.72);
        }

        if (modulus <= 500)
        {
            double pointRadius = Math.Clamp(4.5 / Math.Sqrt(modulus), 0.004, 0.018);
            for (int x = 0; x < modulus; x++)
            {
                double angle = -Math.PI / 2 + Math.Tau * x / modulus;
                canvas.Circle(radius * Math.Cos(angle), radius * Math.Sin(angle), pointRadius,
                    1, state.AccentColor, true);
            }
        }
    }

    private static void RenderPascal(
        RasterCanvas canvas,
        MathematicalLaboratoryState state,
        CancellationToken token,
        IProgress<int>? progress)
    {
        int rows = Math.Clamp(state.PrimaryValue, 8, 2_000);
        int modulus = Math.Clamp(state.SecondaryValue, 2, 256);
        int cellSize = Math.Clamp(state.TertiaryValue, 1, 8);
        int[] previous = new int[rows + 1];
        int[] current = new int[rows + 1];
        previous[0] = 1;
        int rowStride = Math.Max(1, rows / Math.Max(64, canvas.Height * 2));

        for (int row = 0; row < rows; row++)
        {
            if ((row & 31) == 0)
            {
                token.ThrowIfCancellationRequested();
                progress?.Report(8 + row * 86 / rows);
            }
            if (row % rowStride == 0 || row == rows - 1)
            {
                double y = -0.92 + 1.84 * row / Math.Max(1, rows - 1d);
                for (int column = 0; column <= row; column++)
                {
                    int residue = previous[column];
                    if (state.Mode == 1 && residue != 0) continue;
                    double x = row == 0 ? 0 : 0.92 * (2d * column - row) / rows;
                    Color color = state.Mode switch
                    {
                        1 => state.AccentColor,
                        _ => residue == 0
                            ? Mix(state.BackgroundColor, state.PrimaryColor, 0.18)
                            : Palette(state, (double)residue / modulus)
                    };
                    (double sx, double sy) = canvas.Map(x, y);
                    int size = Math.Max(1, (int)Math.Round(cellSize * Math.Max(0.55, state.Zoom)));
                    canvas.PixelSquare((int)Math.Round(sx), (int)Math.Round(sy), size, color,
                        Math.Clamp(state.Parameter, 0.2, 2.5) / 2.5);
                }
            }

            Array.Clear(current);
            current[0] = current[row + 1] = 1 % modulus;
            for (int column = 1; column <= row; column++)
                current[column] = (previous[column - 1] + previous[column]) % modulus;
            (previous, current) = (current, previous);
        }

        if (state.Mode == 2 && state.ShowGuides)
        {
            for (long power = modulus; power < rows; power *= modulus)
            {
                double y = -0.92 + 1.84 * power / rows;
                canvas.Line(-0.94, y, 0.94, y, 1, state.AccentColor, 0.35);
                if (power > rows / Math.Max(2, modulus)) break;
            }
        }
    }

    private static void RenderRationals(RasterCanvas canvas, MathematicalLaboratoryState state, CancellationToken token)
    {
        int depth = Math.Clamp(state.PrimaryValue, 1, 13);
        int denominator = Math.Clamp(state.SecondaryValue, 2, 300);
        int limit = Math.Clamp(state.TertiaryValue, 20, 30_000);
        switch (state.Mode)
        {
            case 0:
                RenderRationalTree(canvas, depth, limit, sternBrocot: true, state, token);
                break;
            case 1:
                RenderRationalTree(canvas, depth, limit, sternBrocot: false, state, token);
                break;
            case 2:
            case 3:
                RenderFarey(canvas, denominator, limit, state.Mode == 3, state, token);
                break;
            default:
                RenderContinuedFraction(canvas, state.Parameter, denominator, state);
                break;
        }
    }

    private static void RenderRationalTree(
        RasterCanvas canvas, int maxDepth, int limit, bool sternBrocot,
        MathematicalLaboratoryState state, CancellationToken token)
    {
        var nodes = new List<RationalNode>();
        if (sternBrocot)
        {
            var queue = new Queue<(long lp, long lq, long rp, long rq, int depth, int position, int parent)>();
            queue.Enqueue((0, 1, 1, 0, 0, 0, -1));
            while (queue.Count > 0 && nodes.Count < limit)
            {
                var item = queue.Dequeue();
                long p = item.lp + item.rp;
                long q = item.lq + item.rq;
                int index = nodes.Count;
                double slots = Math.Pow(2, item.depth);
                double x = -0.94 + 1.88 * (item.position + 0.5) / slots;
                double y = -0.85 + 1.7 * item.depth / Math.Max(1, maxDepth);
                nodes.Add(new RationalNode(p, q, x, y, item.parent, item.depth));
                if (item.depth >= maxDepth) continue;
                queue.Enqueue((item.lp, item.lq, p, q, item.depth + 1, item.position * 2, index));
                queue.Enqueue((p, q, item.rp, item.rq, item.depth + 1, item.position * 2 + 1, index));
            }
        }
        else
        {
            var queue = new Queue<(long p, long q, int depth, int position, int parent)>();
            queue.Enqueue((1, 1, 0, 0, -1));
            while (queue.Count > 0 && nodes.Count < limit)
            {
                var item = queue.Dequeue();
                int index = nodes.Count;
                double slots = Math.Pow(2, item.depth);
                double x = -0.94 + 1.88 * (item.position + 0.5) / slots;
                double y = -0.85 + 1.7 * item.depth / Math.Max(1, maxDepth);
                nodes.Add(new RationalNode(item.p, item.q, x, y, item.parent, item.depth));
                if (item.depth >= maxDepth) continue;
                queue.Enqueue((item.p, item.p + item.q, item.depth + 1, item.position * 2, index));
                queue.Enqueue((item.p + item.q, item.q, item.depth + 1, item.position * 2 + 1, index));
            }
        }

        for (int i = 0; i < nodes.Count; i++)
        {
            if ((i & 255) == 0) token.ThrowIfCancellationRequested();
            RationalNode node = nodes[i];
            if (node.Parent >= 0)
            {
                RationalNode parent = nodes[node.Parent];
                canvas.Line(parent.X, parent.Y, node.X, node.Y, 1,
                    Palette(state, (double)node.Depth / Math.Max(1, maxDepth)), 0.6);
            }
        }
        foreach (RationalNode node in nodes)
        {
            double radius = Math.Clamp(0.026 / Math.Sqrt(node.Depth + 1), 0.0035, 0.02);
            canvas.Circle(node.X, node.Y, radius, 1,
                Palette(state, (double)node.Depth / Math.Max(1, maxDepth)), state.Filled);
        }
    }

    private static void RenderFarey(
        RasterCanvas canvas, int maxDenominator, int limit, bool fordCircles,
        MathematicalLaboratoryState state, CancellationToken token)
    {
        var fractions = new List<(int p, int q, double value)> { (0, 1, 0), (1, 1, 1) };
        for (int q = 2; q <= maxDenominator && fractions.Count < limit; q++)
        {
            for (int p = 1; p < q && fractions.Count < limit; p++)
                if (GreatestCommonDivisor(p, q) == 1) fractions.Add((p, q, (double)p / q));
            if ((q & 15) == 0) token.ThrowIfCancellationRequested();
        }
        fractions.Sort((left, right) => left.value.CompareTo(right.value));
        canvas.Line(-0.94, 0.72, 0.94, 0.72, 1.4, state.PrimaryColor, 0.7);

        foreach ((int p, int q, double value) fraction in fractions)
        {
            double x = -0.94 + 1.88 * fraction.value;
            if (fordCircles)
            {
                double radius = Math.Min(0.34, 0.7 / (fraction.q * fraction.q));
                canvas.Circle(x, 0.72 - radius, radius, 1,
                    Palette(state, (double)(fraction.q % 17) / 17), state.Filled);
            }
            else
            {
                double height = 0.7 / fraction.q;
                canvas.Line(x, 0.72, x, 0.72 - height, 1.2,
                    Palette(state, (double)(fraction.q % 13) / 13), 0.85);
                canvas.Circle(x, 0.72 - height, 0.004 + 0.012 / Math.Sqrt(fraction.q),
                    1, state.AccentColor, true);
            }
        }
    }

    private static void RenderContinuedFraction(
        RasterCanvas canvas, double target, int maxTerms, MathematicalLaboratoryState state)
    {
        target = double.IsFinite(target) ? target : Math.Sqrt(2);
        maxTerms = Math.Clamp(maxTerms, 2, 32);
        var convergents = new List<(long p, long q, double error)>();
        double x = target;
        long p0 = 0, p1 = 1, q0 = 1, q1 = 0;
        for (int i = 0; i < maxTerms; i++)
        {
            long a = (long)Math.Floor(x);
            long p;
            long q;
            try
            {
                p = checked(a * p1 + p0);
                q = checked(a * q1 + q0);
            }
            catch (OverflowException)
            {
                break;
            }
            convergents.Add((p, q, Math.Abs(target - (double)p / q)));
            p0 = p1; p1 = p; q0 = q1; q1 = q;
            double remainder = x - a;
            if (remainder < 1e-13 || q > 10_000_000) break;
            x = 1 / remainder;
        }
        if (convergents.Count == 0) return;
        double maxError = Math.Max(1e-12, convergents[0].error);
        for (int i = 0; i < convergents.Count; i++)
        {
            double px = -0.88 + 1.76 * i / Math.Max(1, convergents.Count - 1d);
            double py = 0.78 - 1.56 * Math.Clamp(-Math.Log10(Math.Max(1e-15, convergents[i].error)) / 15, 0, 1);
            if (i > 0)
            {
                double previousX = -0.88 + 1.76 * (i - 1) / Math.Max(1, convergents.Count - 1d);
                double previousY = 0.78 - 1.56 * Math.Clamp(
                    -Math.Log10(Math.Max(1e-15, convergents[i - 1].error)) / 15, 0, 1);
                canvas.Line(previousX, previousY, px, py, 2, Palette(state, (double)i / convergents.Count), 0.9);
            }
            canvas.Circle(px, py, 0.018, 1, Palette(state, (double)i / convergents.Count), true);
        }
        if (state.ShowGuides)
            for (int i = 0; i <= 5; i++)
            {
                double y = 0.78 - 1.56 * i / 5d;
                canvas.Line(-0.9, y, 0.9, y, 1, state.PrimaryColor, 0.16);
            }
    }

    private static void RenderPrimes(
        RasterCanvas canvas,
        MathematicalLaboratoryState state,
        CancellationToken token,
        IProgress<int>? progress)
    {
        int radius = Math.Clamp(state.PrimaryValue, 8, 260);
        int colorModulus = Math.Clamp(state.SecondaryValue, 2, 64);
        int side = radius * 2 + 1;
        int maximum = state.Mode switch
        {
            0 => side * side + Math.Abs(state.TertiaryValue) + 10,
            1 or 2 => side * side + 10,
            3 => 2 * radius * radius + 10,
            _ => 3 * radius * radius + 10
        };
        bool[] primes = BuildPrimeSieve(Math.Max(32, maximum), token);
        double pointRadius = Math.Clamp(state.Parameter / Math.Min(canvas.Width, canvas.Height), 0.0012, 0.018);

        if (state.Mode == 0)
        {
            int x = 0, y = 0, dx = 1, dy = 0, segmentLength = 1, segmentProgress = 0, turns = 0;
            int count = side * side;
            var polynomialValues = new HashSet<int>();
            if (state.ShowGuides)
            {
                for (int n = -radius; n <= radius; n++)
                {
                    long value = (long)n * n + n + state.TertiaryValue;
                    if (value is >= 0 and <= int.MaxValue) polynomialValues.Add((int)value);
                }
            }
            for (int n = 1; n <= count; n++)
            {
                if (n < primes.Length && primes[n])
                {
                    Color color = polynomialValues.Contains(n) ? state.AccentColor : Palette(state, (double)(n % colorModulus) / colorModulus);
                    canvas.Circle(0.88 * x / radius, 0.88 * y / radius, pointRadius, 1, color, true);
                }
                x += dx; y += dy; segmentProgress++;
                if (segmentProgress == segmentLength)
                {
                    segmentProgress = 0; (dx, dy) = (-dy, dx); turns++;
                    if ((turns & 1) == 0) segmentLength++;
                }
                if ((n & 2047) == 0)
                {
                    token.ThrowIfCancellationRequested();
                    progress?.Report(12 + n * 80 / count);
                }
            }
            return;
        }

        if (state.Mode is 1 or 2)
        {
            int count = side * side;
            for (int n = 2; n <= count; n++)
            {
                if (!primes[n]) continue;
                double radial = 0.9 * Math.Sqrt((double)n / count);
                double angle = state.Mode == 1
                    ? Math.Tau * Math.Sqrt(n)
                    : Math.Tau * n / 6 + Math.Sin(n * Math.PI / 3) * 0.16;
                canvas.Circle(radial * Math.Cos(angle), radial * Math.Sin(angle), pointRadius,
                    1, Palette(state, (double)(n % colorModulus) / colorModulus), true);
                if ((n & 2047) == 0) token.ThrowIfCancellationRequested();
            }
            return;
        }

        for (int a = -radius; a <= radius; a++)
        {
            for (int b = -radius; b <= radius; b++)
            {
                if (a == 0 && b == 0) continue;
                long norm = state.Mode == 3
                    ? (long)a * a + (long)b * b
                    : (long)a * a - (long)a * b + (long)b * b;
                bool isPrime = state.Mode == 3
                    ? a == 0 || b == 0
                        ? IsPrime(Math.Abs(a == 0 ? b : a)) && Math.Abs(a == 0 ? b : a) % 4 == 3
                        : norm < primes.Length && primes[(int)norm]
                    : norm < primes.Length && primes[(int)norm];
                if (!isPrime) continue;
                double x = 0.88 * a / radius;
                double y = state.Mode == 4
                    ? 0.88 * (b - a * 0.5) / radius
                    : 0.88 * b / radius;
                canvas.Circle(x, y, pointRadius, 1,
                    Palette(state, (double)(norm % colorModulus) / colorModulus), true);
            }
            if ((a & 15) == 0) token.ThrowIfCancellationRequested();
        }
        if (state.ShowGuides)
        {
            canvas.Line(-0.92, 0, 0.92, 0, 1, state.PrimaryColor, 0.2);
            canvas.Line(0, -0.92, 0, 0.92, 1, state.PrimaryColor, 0.2);
        }
    }

    private static void RenderPhyllotaxis(RasterCanvas canvas, MathematicalLaboratoryState state, CancellationToken token)
    {
        int count = Math.Clamp(state.PrimaryValue, 50, 50_000);
        double angle = state.Mode switch
        {
            0 => Math.Tau * (1 - 1 / ((1 + Math.Sqrt(5)) / 2)),
            1 => Math.PI - 3,
            2 => Math.Tau * (Math.Sqrt(2) - 1),
            3 => Math.Tau * state.SecondaryValue / Math.Max(1d, state.TertiaryValue),
            _ => state.Parameter * Math.PI / 180
        };
        double dot = Math.Clamp(0.025 / Math.Pow(count / 500d, 0.28), 0.0015, 0.018);
        for (int n = count - 1; n >= 0; n--)
        {
            if ((n & 1023) == 0) token.ThrowIfCancellationRequested();
            double radius = 0.92 * Math.Sqrt((n + 0.5) / count);
            double theta = n * angle;
            double family = 0.5 + 0.5 * Math.Sin(theta * 0.13 + radius * 28);
            canvas.Circle(radius * Math.Cos(theta), radius * Math.Sin(theta), dot,
                1, Palette(state, state.ShowGuides ? family : (double)n / count), true);
        }
    }

    private static void RenderInversion(RasterCanvas canvas, MathematicalLaboratoryState state, CancellationToken token)
    {
        double inversionRadius = Math.Clamp(state.Parameter, 0.05, 2);
        double cx = state.AnchorX, cy = state.AnchorY;
        canvas.Circle(cx, cy, inversionRadius, 1.5, state.AccentColor, false);
        canvas.Circle(cx, cy, 0.012, 1, state.AccentColor, true);

        if (state.Mode == 1)
        {
            int count = Math.Clamp(state.PrimaryValue, 3, 500);
            int symmetry = Math.Clamp(state.TertiaryValue, 2, 24);
            for (int i = 0; i < count; i++)
            {
                if ((i & 63) == 0) token.ThrowIfCancellationRequested();
                double angle = Math.Tau * i / symmetry;
                double ring = 0.22 + 0.55 * ((i / symmetry) % Math.Max(1, count / symmetry)) / Math.Max(1d, count / symmetry);
                double radius = 0.025 + 0.045 * (0.5 + 0.5 * Math.Sin(i * 1.73));
                double ox = ring * Math.Cos(angle), oy = ring * Math.Sin(angle);
                canvas.Circle(ox, oy, radius, 1, state.PrimaryColor, state.Filled);
                if (TryInvertCircle(ox, oy, radius, cx, cy, inversionRadius, out double ix, out double iy, out double ir))
                    canvas.Circle(ix, iy, ir, 1, Palette(state, (double)i / count), state.Filled);
            }
            return;
        }

        List<LaboratoryPoint> points = state.InputPoints.Count > 0
            ? state.InputPoints
            : CreateInversionSeed(Math.Clamp(state.PrimaryValue, 3, 2_000), Math.Clamp(state.TertiaryValue, 2, 24));
        if (state.Mode == 3)
        {
            Complex a = new(1, 0.18), b = new(-0.18, 0.24), c = new(0.22, -0.17), d = Complex.One;
            for (int i = 0; i < points.Count; i++)
            {
                Complex z = new(points[i].X, points[i].Y);
                Complex denominator = c * z + d;
                if (denominator.Magnitude < 1e-8) continue;
                Complex mapped = (a * z + b) / denominator;
                canvas.Line(z.Real, z.Imaginary, mapped.Real, mapped.Imaginary, 1,
                    Palette(state, (double)i / points.Count), 0.5);
                canvas.Circle(mapped.Real, mapped.Imaginary, 0.009, 1,
                    Palette(state, (double)i / points.Count), true);
            }
            return;
        }

        int repetitions = state.Mode == 2 ? Math.Clamp(state.SecondaryValue, 1, 12) : 1;
        int symmetryCount = Math.Clamp(state.TertiaryValue, 2, 24);
        for (int i = 0; i < points.Count; i++)
        {
            if ((i & 127) == 0) token.ThrowIfCancellationRequested();
            LaboratoryPoint current = points[i];
            canvas.Circle(current.X, current.Y, 0.007, 1, state.PrimaryColor, true);
            for (int iteration = 0; iteration < repetitions; iteration++)
            {
                double localCx = cx, localCy = cy;
                if (state.Mode == 2 && iteration > 0)
                {
                    double angle = Math.Tau * iteration / symmetryCount;
                    localCx += 0.42 * Math.Cos(angle);
                    localCy += 0.42 * Math.Sin(angle);
                }
                if (!TryInvertPoint(current.X, current.Y, localCx, localCy, inversionRadius,
                        out double ix, out double iy)) break;
                canvas.Line(current.X, current.Y, ix, iy, 1,
                    Palette(state, (double)(i + iteration) / (points.Count + repetitions)), 0.4);
                current = new LaboratoryPoint(ix, iy);
                canvas.Circle(ix, iy, 0.009, 1,
                    Palette(state, (double)i / points.Count), true);
            }
        }
    }

    private static void RenderTilings(RasterCanvas canvas, MathematicalLaboratoryState state, CancellationToken token)
    {
        int depth = Math.Clamp(state.PrimaryValue, 1, 9);
        switch (state.Mode)
        {
            case 0:
                RenderRadialTriangles(canvas, state, depth, Math.Clamp(state.SecondaryValue, 5, 20), golden: true, token);
                break;
            case 1:
                RenderAmmannBeenker(canvas, state, depth, token);
                break;
            case 2:
                RenderChair(canvas, state, depth, -0.9, -0.9, 1.8, 0, token);
                break;
            case 3:
                RenderRadialTriangles(canvas, state, depth, 5, golden: false, token);
                break;
            case 4:
                RenderSphinx(canvas, state, depth, new(-0.9, 0.68), new(0.9, 0.68), new(0, -0.88), 0, token);
                break;
            default:
                RenderFibonacciTriangles(canvas, state, depth);
                break;
        }
    }

    private static void RenderRadialTriangles(
        RasterCanvas canvas, MathematicalLaboratoryState state, int depth, int sectors, bool golden,
        CancellationToken token)
    {
        for (int sector = 0; sector < sectors; sector++)
        {
            double a0 = Math.Tau * sector / sectors - Math.PI / 2;
            double a1 = Math.Tau * (sector + 1) / sectors - Math.PI / 2;
            var stack = new Stack<(LaboratoryPoint a, LaboratoryPoint b, LaboratoryPoint c, int depth, int branch)>();
            stack.Push((new(0, 0), new(0.94 * Math.Cos(a0), 0.94 * Math.Sin(a0)),
                new(0.94 * Math.Cos(a1), 0.94 * Math.Sin(a1)), depth, sector));
            while (stack.Count > 0)
            {
                var tile = stack.Pop();
                if (tile.depth == 0)
                {
                    DrawTile(canvas, [tile.a, tile.b, tile.c], state,
                        (tile.branch * 0.137 + sector / (double)sectors) % 1);
                    continue;
                }
                double ratio = golden ? 1 / ((1 + Math.Sqrt(5)) / 2) : 0.5;
                LaboratoryPoint split = Lerp(tile.b, tile.c, ratio);
                stack.Push((tile.a, tile.b, split, tile.depth - 1, tile.branch * 2));
                stack.Push((tile.a, split, tile.c, tile.depth - 1, tile.branch * 2 + 1));
                if ((stack.Count & 2047) == 0) token.ThrowIfCancellationRequested();
            }
        }
    }

    private static void RenderAmmannBeenker(
        RasterCanvas canvas, MathematicalLaboratoryState state, int depth, CancellationToken token)
    {
        int rings = Math.Clamp(depth * 3, 3, 24);
        for (int ring = rings; ring >= 1; ring--)
        {
            double inner = 0.9 * (ring - 1d) / rings;
            double outer = 0.9 * ring / rings;
            int sectors = 8 * Math.Max(1, ring);
            for (int i = 0; i < sectors; i++)
            {
                double a0 = Math.Tau * i / sectors;
                double a1 = Math.Tau * (i + 1) / sectors;
                LaboratoryPoint[] tile =
                [
                    new(inner * Math.Cos(a0), inner * Math.Sin(a0)),
                    new(outer * Math.Cos(a0), outer * Math.Sin(a0)),
                    new(outer * Math.Cos(a1), outer * Math.Sin(a1)),
                    new(inner * Math.Cos(a1), inner * Math.Sin(a1))
                ];
                DrawTile(canvas, tile, state, (ring + i % 8) / (double)(rings + 8));
            }
            token.ThrowIfCancellationRequested();
        }
    }

    private static void RenderChair(
        RasterCanvas canvas, MathematicalLaboratoryState state, int depth,
        double x, double y, double size, int rotation, CancellationToken token)
    {
        if (depth <= 0 || size < 0.008)
        {
            double third = size / 3;
            for (int row = 0; row < 3; row++)
                for (int column = 0; column < 3; column++)
                {
                    int missingX = rotation is 0 or 3 ? 2 : 0;
                    int missingY = rotation is 0 or 1 ? 0 : 2;
                    if (column == missingX && row == missingY) continue;
                    LaboratoryPoint[] square =
                    [
                        new(x + column * third, y + row * third),
                        new(x + (column + 1) * third, y + row * third),
                        new(x + (column + 1) * third, y + (row + 1) * third),
                        new(x + column * third, y + (row + 1) * third)
                    ];
                    DrawTile(canvas, square, state, (rotation + row + column) / 7d);
                }
            return;
        }
        double half = size / 2;
        RenderChair(canvas, state, depth - 1, x, y, half, (rotation + 1) % 4, token);
        RenderChair(canvas, state, depth - 1, x + half, y, half, rotation, token);
        RenderChair(canvas, state, depth - 1, x, y + half, half, rotation, token);
        token.ThrowIfCancellationRequested();
    }

    private static void RenderSphinx(
        RasterCanvas canvas, MathematicalLaboratoryState state, int depth,
        LaboratoryPoint a, LaboratoryPoint b, LaboratoryPoint c, int branch, CancellationToken token)
    {
        if (depth <= 0)
        {
            DrawTile(canvas, [a, b, c], state, branch * 0.173 % 1);
            return;
        }
        LaboratoryPoint ab = Lerp(a, b, 0.5), bc = Lerp(b, c, 0.5), ca = Lerp(c, a, 0.5);
        RenderSphinx(canvas, state, depth - 1, a, ab, ca, branch * 4, token);
        RenderSphinx(canvas, state, depth - 1, ab, b, bc, branch * 4 + 1, token);
        RenderSphinx(canvas, state, depth - 1, ca, bc, c, branch * 4 + 2, token);
        if ((branch & 255) == 0) token.ThrowIfCancellationRequested();
    }

    private static void RenderFibonacciTriangles(RasterCanvas canvas, MathematicalLaboratoryState state, int depth)
    {
        var fibonacci = new List<int> { 1, 1 };
        for (int i = 2; i < depth + 5; i++) fibonacci.Add(fibonacci[^1] + fibonacci[^2]);
        int rows = Math.Clamp(fibonacci[^1], 5, 144);
        for (int row = 0; row < rows; row++)
        {
            int columns = rows - row;
            double y0 = -0.86 + 1.72 * row / rows;
            double y1 = -0.86 + 1.72 * (row + 1) / rows;
            for (int column = 0; column < columns; column++)
            {
                double x0 = -0.88 + 1.76 * (column + row * 0.5) / rows;
                double x1 = -0.88 + 1.76 * (column + 1 + row * 0.5) / rows;
                double xm = (x0 + x1) / 2;
                DrawTile(canvas, [new(x0, y0), new(x1, y0), new(xm, y1)], state,
                    ((column + fibonacci[row % fibonacci.Count]) % 13) / 13d);
            }
        }
    }

    private static void DrawTile(
        RasterCanvas canvas, IReadOnlyList<LaboratoryPoint> points,
        MathematicalLaboratoryState state, double palettePosition)
    {
        Color color = Palette(state, palettePosition);
        if (state.Filled) canvas.Polygon(points, color, true, 0.76);
        canvas.Polygon(points, state.Filled ? Mix(color, state.BackgroundColor, 0.3) : color,
            false, Math.Clamp(state.Parameter, 0.25, 6));
    }

    private static void RenderHyperbolic(RasterCanvas canvas, MathematicalLaboratoryState state, CancellationToken token)
    {
        canvas.Circle(0, 0, 0.94, 2, state.AccentColor, false);
        if (state.Mode == 4)
        {
            int count = Math.Clamp(state.SecondaryValue * state.TertiaryValue, 8, 120);
            for (int i = 0; i < count; i++)
            {
                double midpoint = Math.Tau * i / count + state.Rotation;
                double delta = 0.18 + 1.18 * ((i * 37) % count) / count;
                DrawPoincareGeodesic(canvas, midpoint, delta, Palette(state, (double)i / count));
            }
            return;
        }

        int depth = Math.Clamp(state.PrimaryValue, 1, 9);
        (int p, int q) = state.Mode switch
        {
            0 => (3, 7),
            1 => (4, 5),
            2 => (6, 4),
            _ => (Math.Clamp(state.SecondaryValue, 3, 12), Math.Clamp(state.TertiaryValue, 3, 12))
        };
        double curvature = Math.Clamp(state.Parameter, 0.15, 1.5);
        var previousRing = new List<LaboratoryPoint> { new(0, 0) };
        DrawRegularPolygon(canvas, 0, 0, 0.16, p, 0, Palette(state, 0), state.Filled);

        for (int layer = 1; layer <= depth; layer++)
        {
            int count = Math.Min(3_000, p * (int)Math.Pow(Math.Max(2, q - 2), layer - 1));
            double radius = 0.94 * Math.Tanh(curvature * layer / Math.Max(1, depth) * 2.1) / Math.Tanh(curvature * 2.1);
            double tileRadius = Math.Clamp(0.38 * (1 - radius * radius) / Math.Sqrt(layer + 1), 0.006, 0.16);
            var ring = new List<LaboratoryPoint>(count);
            for (int i = 0; i < count; i++)
            {
                double angle = Math.Tau * (i + (layer & 1) * 0.5) / count;
                var point = new LaboratoryPoint(radius * Math.Cos(angle), radius * Math.Sin(angle));
                ring.Add(point);
                LaboratoryPoint parent = previousRing[Math.Min(previousRing.Count - 1,
                    (int)((long)i * previousRing.Count / count))];
                canvas.Line(parent.X, parent.Y, point.X, point.Y, 0.75,
                    Palette(state, (double)layer / depth), 0.34);
                if (i > 0)
                    canvas.Line(ring[i - 1].X, ring[i - 1].Y, point.X, point.Y, 0.7,
                        Palette(state, (double)layer / depth), 0.28);
                DrawRegularPolygon(canvas, point.X, point.Y, tileRadius, p, angle,
                    Palette(state, (layer + (double)i / count) / (depth + 1)), state.Filled);
            }
            if (ring.Count > 2)
                canvas.Line(ring[^1].X, ring[^1].Y, ring[0].X, ring[0].Y, 0.7,
                    Palette(state, (double)layer / depth), 0.28);
            previousRing = ring;
            token.ThrowIfCancellationRequested();
        }
    }

    private static void DrawPoincareGeodesic(RasterCanvas canvas, double midpoint, double delta, Color color)
    {
        delta = Math.Clamp(delta, 0.08, Math.PI / 2 - 0.04);
        double centerDistance = 0.94 / Math.Cos(delta);
        double radius = 0.94 * Math.Tan(delta);
        double cx = centerDistance * Math.Cos(midpoint);
        double cy = centerDistance * Math.Sin(midpoint);
        LaboratoryPoint? previous = null;
        for (int i = 0; i <= 360; i++)
        {
            double angle = Math.Tau * i / 360;
            var point = new LaboratoryPoint(cx + radius * Math.Cos(angle), cy + radius * Math.Sin(angle));
            bool inside = point.X * point.X + point.Y * point.Y <= 0.94 * 0.94 + 1e-5;
            if (inside && previous is { } last)
                canvas.Line(last.X, last.Y, point.X, point.Y, 1.1, color, 0.74);
            previous = inside ? point : null;
        }
    }

    private static void DrawRegularPolygon(
        RasterCanvas canvas, double x, double y, double radius, int sides, double angle,
        Color color, bool filled)
    {
        var points = new LaboratoryPoint[sides];
        for (int i = 0; i < sides; i++)
        {
            double theta = angle + Math.Tau * i / sides;
            points[i] = new LaboratoryPoint(x + radius * Math.Cos(theta), y + radius * Math.Sin(theta));
        }
        if (filled) canvas.Polygon(points, color, true, 0.42);
        canvas.Polygon(points, color, false, 0.8);
    }

    private static void RenderChladniWaves(
        RasterCanvas canvas,
        MathematicalLaboratoryState state,
        CancellationToken token,
        IProgress<int>? progress)
    {
        int mode = Math.Clamp(state.Mode, 0, 5);
        int first = Math.Clamp(state.PrimaryValue, 0, 64);
        int second = Math.Clamp(state.SecondaryValue, 1, 128);
        int contourBands = Math.Clamp(state.TertiaryValue, 1, 360);
        double phase = Math.Tau * (state.Phase - Math.Floor(state.Phase));
        double threshold = Math.Clamp(state.Parameter, 0.005, 0.35);
        List<WaveSource> sources = mode >= 3
            ? CreateWaveSources(mode, first, state.TertiaryValue, state.Parameter)
            : [];
        double waveNumber = 5.2 + second * 1.55;
        int completedRows = 0;
        int reportStep = Math.Max(1, canvas.Height / 24);
        var options = new ParallelOptions
        {
            CancellationToken = token,
            MaxDegreeOfParallelism = Environment.ProcessorCount
        };

        Parallel.For(0, canvas.Height, options, y =>
        {
            for (int x = 0; x < canvas.Width; x++)
            {
                (double worldX, double worldY) = canvas.Unmap(x + 0.5, y + 0.5);
                bool inside;
                double value;
                double effectiveThreshold = threshold;
                if (mode <= 1)
                {
                    const double halfSize = 0.92;
                    inside = Math.Abs(worldX) <= halfSize && Math.Abs(worldY) <= halfSize;
                    if (!inside) continue;
                    double u = (worldX / halfSize + 1) * 0.5;
                    double v = (worldY / halfSize + 1) * 0.5;
                    int m = Math.Max(1, first);
                    int n = Math.Max(1, second);
                    double direct = Math.Sin(m * Math.PI * u) * Math.Sin(n * Math.PI * v);
                    double exchanged = Math.Sin(n * Math.PI * u) * Math.Sin(m * Math.PI * v);
                    double phaseAmplitude = 0.72 + 0.28 * Math.Cos(phase);
                    value = (mode == 0 ? direct + exchanged : direct - exchanged) *
                            phaseAmplitude * 0.72;
                }
                else if (mode == 2)
                {
                    double radius = Math.Sqrt(worldX * worldX + worldY * worldY) / 0.92;
                    inside = radius <= 1;
                    if (!inside) continue;
                    int angularOrder = first;
                    int radialOrder = Math.Max(1, second);
                    double alpha = (radialOrder + angularOrder * 0.5 - 0.25) * Math.PI;
                    double angle = Math.Atan2(worldY, worldX);
                    double phaseAmplitude = 0.72 + 0.28 * Math.Cos(phase);
                    value = BesselJ(angularOrder, alpha * radius) *
                            Math.Cos(angularOrder * angle) * phaseAmplitude * 2.35;
                }
                else
                {
                    inside = true;
                    double sum = 0;
                    foreach (WaveSource source in sources)
                    {
                        double dx = worldX - source.X;
                        double dy = worldY - source.Y;
                        double distance = Math.Sqrt(dx * dx + dy * dy);
                        sum += Math.Cos(waveNumber * distance - phase + source.Phase) /
                               Math.Sqrt(0.14 + distance);
                    }
                    value = sum / Math.Sqrt(Math.Max(1, sources.Count));
                    effectiveThreshold = Math.Clamp(0.028 + state.Parameter * 0.045, 0.015, 0.16);
                }

                Color color = WaveFieldColor(state, value, effectiveThreshold, contourBands);
                canvas.SetPixel(x, y, color);
            }

            int done = Interlocked.Increment(ref completedRows);
            if (done % reportStep == 0 || done == canvas.Height)
                progress?.Report(8 + done * 86 / canvas.Height);
        });

        if (!state.ShowGuides) return;
        Color guide = Mix(state.AccentColor, state.PrimaryColor, 0.35);
        if (mode <= 1)
        {
            LaboratoryPoint[] plate = [new(-0.92, -0.92), new(0.92, -0.92), new(0.92, 0.92), new(-0.92, 0.92)];
            canvas.Polygon(plate, guide, false, 1.4);
        }
        else if (mode == 2)
        {
            canvas.Circle(0, 0, 0.92, 1.5, guide, false);
        }
        else
        {
            foreach (WaveSource source in sources)
            {
                canvas.Circle(source.X, source.Y, 0.018, 1, state.AccentColor, true);
                canvas.Circle(source.X, source.Y, 0.034, 1, guide, false);
            }
            if (mode == 5)
                canvas.Line(-0.72, -0.92, -0.72, 0.92, 1, guide, 0.45);
        }
    }

    private static List<WaveSource> CreateWaveSources(
        int mode, int primary, int phaseStepDegrees, double parameter)
    {
        var sources = new List<WaveSource>();
        double phaseStep = phaseStepDegrees * Math.PI / 180;
        if (mode == 3)
        {
            double spacing = Math.Clamp(0.24 + parameter * 0.5, 0.2, 0.82);
            sources.Add(new WaveSource(-spacing / 2, 0, 0));
            sources.Add(new WaveSource(spacing / 2, 0, phaseStep));
            return sources;
        }

        if (mode == 4)
        {
            int count = Math.Clamp(primary, 2, 64);
            double radius = Math.Clamp(0.3 + parameter * 0.32, 0.22, 0.72);
            for (int i = 0; i < count; i++)
            {
                double angle = Math.Tau * i / count;
                sources.Add(new WaveSource(radius * Math.Cos(angle), radius * Math.Sin(angle), i * phaseStep));
            }
            return sources;
        }

        int slitCount = Math.Clamp(primary, 2, 32);
        double separation = Math.Clamp(0.075 + parameter * 0.22, 0.06, 0.24);
        double offset = (slitCount - 1) * separation / 2;
        for (int i = 0; i < slitCount; i++)
            sources.Add(new WaveSource(-0.72, i * separation - offset, i * phaseStep));
        return sources;
    }

    private static Color WaveFieldColor(
        MathematicalLaboratoryState state, double value, double threshold, int contourBands)
    {
        double absolute = Math.Abs(value);
        double signed = 0.5 + 0.5 * Math.Tanh(value * 1.35);
        double nodeStrength = Math.Exp(-Math.Pow(absolute / Math.Max(1e-6, threshold), 2));
        if (!state.Filled)
            return Mix(state.BackgroundColor, state.AccentColor, nodeStrength * 0.96);

        double amplitude = Math.Clamp(absolute, 0, 1);
        double contour = 0.82 + 0.18 * Math.Cos(Math.Tau * contourBands * absolute);
        Color wave = Palette(state, signed);
        Color color = Mix(state.BackgroundColor, wave, (0.25 + amplitude * 0.7) * contour);
        return Mix(color, state.AccentColor, nodeStrength * 0.88);
    }

    private static double BesselJ(int order, double value)
    {
        value = Math.Abs(value);
        if (value < 1e-12) return order == 0 ? 1 : 0;
        if (value > 20)
            return Math.Sqrt(2 / (Math.PI * value)) *
                   Math.Cos(value - order * Math.PI / 2 - Math.PI / 4);

        double half = value / 2;
        double term = 1;
        for (int i = 1; i <= order; i++) term *= half / i;
        double sum = term;
        for (int index = 1; index < 80; index++)
        {
            term *= -half * half / (index * (index + order));
            sum += term;
            if (Math.Abs(term) < Math.Abs(sum) * 1e-14 + 1e-16) break;
        }
        return sum;
    }

    private static void RenderFourier(
        RasterCanvas canvas,
        MathematicalLaboratoryState state,
        CancellationToken token,
        IProgress<int>? progress)
    {
        int samples = Math.Clamp(state.SecondaryValue, 32, 2_048);
        Complex[] source = ResampleContour(state.InputPoints, samples);
        List<FourierCoefficient> coefficients = CalculateFourier(source, token, progress);
        if (state.Mode == 2)
        {
            List<FourierCoefficient> spectrum = coefficients
                .OrderBy(coefficient => Math.Abs(coefficient.Frequency))
                .Take(Math.Min(state.PrimaryValue * 2 + 1, coefficients.Count)).ToList();
            double maximum = Math.Max(1e-9, spectrum.Max(item => item.Value.Magnitude));
            for (int i = 0; i < spectrum.Count; i++)
            {
                double x = -0.92 + 1.84 * (i + 0.5) / spectrum.Count;
                double barHeight = 1.55 * Math.Sqrt(spectrum[i].Value.Magnitude / maximum);
                canvas.Line(x, 0.78, x, 0.78 - barHeight, Math.Max(1, canvas.Width / (double)spectrum.Count * 0.55),
                    Palette(state, (double)i / spectrum.Count), 0.88);
            }
            return;
        }

        int harmonicCount = Math.Clamp(state.PrimaryValue, 1, Math.Min(250, coefficients.Count));
        List<FourierCoefficient> selected = state.Mode == 1
            ? coefficients.OrderByDescending(item => item.Value.Magnitude).Take(harmonicCount).ToList()
            : coefficients.OrderBy(item => Math.Abs(item.Frequency) * 2 + (item.Frequency < 0 ? 1 : 0))
                .Take(harmonicCount).ToList();
        int tracePoints = Math.Clamp(state.TertiaryValue, 50, 4_000);
        LaboratoryPoint? previous = null;
        for (int i = 0; i <= tracePoints; i++)
        {
            if ((i & 127) == 0) token.ThrowIfCancellationRequested();
            double t = (double)i / tracePoints;
            Complex value = Reconstruct(selected, t);
            var point = new LaboratoryPoint(value.Real, value.Imaginary);
            if (previous is { } last)
                canvas.Line(last.X, last.Y, point.X, point.Y, 1.4,
                    Palette(state, t), 0.88);
            previous = point;
        }

        double phase = state.Phase - Math.Floor(state.Phase);
        Complex center = Complex.Zero;
        for (int i = 0; i < selected.Count; i++)
        {
            FourierCoefficient coefficient = selected[i];
            Complex next = center + coefficient.Value * Complex.Exp(Complex.ImaginaryOne * Math.Tau * coefficient.Frequency * phase);
            double radius = coefficient.Value.Magnitude;
            if (state.ShowGuides && radius > 0.002)
                canvas.Circle(center.Real, center.Imaginary, radius, 0.8,
                    Palette(state, (double)i / selected.Count), false);
            canvas.Line(center.Real, center.Imaginary, next.Real, next.Imaginary, 1,
                Palette(state, (double)i / selected.Count), 0.85);
            center = next;
        }
        canvas.Circle(center.Real, center.Imaginary, 0.014, 1, state.AccentColor, true);
    }

    private static Complex[] ResampleContour(IReadOnlyList<LaboratoryPoint> points, int samples)
    {
        LaboratoryPoint[] contour = points.Count >= 4
            ? points.ToArray()
            : Enumerable.Range(0, 360).Select(index =>
            {
                double t = Math.Tau * index / 360;
                double radius = 0.48 + 0.16 * Math.Cos(5 * t) + 0.08 * Math.Sin(3 * t);
                return new LaboratoryPoint(radius * Math.Cos(t), radius * Math.Sin(t));
            }).ToArray();
        var result = new Complex[samples];
        for (int i = 0; i < samples; i++)
        {
            double position = (double)i / samples * contour.Length;
            int left = (int)Math.Floor(position) % contour.Length;
            int right = (left + 1) % contour.Length;
            double amount = position - Math.Floor(position);
            LaboratoryPoint point = Lerp(contour[left], contour[right], amount);
            result[i] = new Complex(point.X, point.Y);
        }
        return result;
    }

    private static List<FourierCoefficient> CalculateFourier(
        Complex[] samples, CancellationToken token, IProgress<int>? progress)
    {
        int count = samples.Length;
        var result = new List<FourierCoefficient>(count);
        for (int frequency = -count / 2; frequency < (count + 1) / 2; frequency++)
        {
            Complex sum = Complex.Zero;
            for (int n = 0; n < count; n++)
                sum += samples[n] * Complex.Exp(-Complex.ImaginaryOne * Math.Tau * frequency * n / count);
            result.Add(new FourierCoefficient(frequency, sum / count));
            if ((frequency & 15) == 0)
            {
                token.ThrowIfCancellationRequested();
                progress?.Report(12 + (frequency + count / 2) * 54 / count);
            }
        }
        return result;
    }

    private static Complex Reconstruct(IEnumerable<FourierCoefficient> coefficients, double time)
    {
        Complex result = Complex.Zero;
        foreach (FourierCoefficient coefficient in coefficients)
            result += coefficient.Value * Complex.Exp(Complex.ImaginaryOne * Math.Tau * coefficient.Frequency * time);
        return result;
    }

    private static List<LaboratoryPoint> CreateInversionSeed(int count, int symmetry)
    {
        var result = new List<LaboratoryPoint>(count);
        for (int i = 0; i < count; i++)
        {
            double angle = Math.Tau * i / symmetry;
            double radial = 0.18 + 0.68 * (i / symmetry + 1d) / (count / symmetry + 1d);
            radial += 0.04 * Math.Sin(i * 2.399963229728653);
            result.Add(new LaboratoryPoint(radial * Math.Cos(angle), radial * Math.Sin(angle)));
        }
        return result;
    }

    private static bool TryInvertPoint(
        double x, double y, double centerX, double centerY, double radius,
        out double resultX, out double resultY)
    {
        double dx = x - centerX, dy = y - centerY;
        double squared = dx * dx + dy * dy;
        if (squared < 1e-10)
        {
            resultX = resultY = 0;
            return false;
        }
        double factor = radius * radius / squared;
        resultX = centerX + dx * factor;
        resultY = centerY + dy * factor;
        return double.IsFinite(resultX) && double.IsFinite(resultY);
    }

    private static bool TryInvertCircle(
        double x, double y, double circleRadius,
        double centerX, double centerY, double inversionRadius,
        out double resultX, out double resultY, out double resultRadius)
    {
        double dx = x - centerX, dy = y - centerY;
        double denominator = dx * dx + dy * dy - circleRadius * circleRadius;
        if (Math.Abs(denominator) < 1e-8)
        {
            resultX = resultY = resultRadius = 0;
            return false;
        }
        double factor = inversionRadius * inversionRadius / denominator;
        resultX = centerX + dx * factor;
        resultY = centerY + dy * factor;
        resultRadius = Math.Abs(factor * circleRadius);
        return resultRadius < 20 && double.IsFinite(resultX) && double.IsFinite(resultY);
    }

    private static bool[] BuildPrimeSieve(int maximum, CancellationToken token)
    {
        var prime = Enumerable.Repeat(true, maximum + 1).ToArray();
        prime[0] = prime[1] = false;
        for (int value = 2; value * value <= maximum; value++)
        {
            if (!prime[value]) continue;
            for (int composite = value * value; composite <= maximum; composite += value)
                prime[composite] = false;
            if ((value & 31) == 0) token.ThrowIfCancellationRequested();
        }
        return prime;
    }

    private static bool IsPrime(int value)
    {
        value = Math.Abs(value);
        if (value < 2) return false;
        if (value % 2 == 0) return value == 2;
        for (int divisor = 3; divisor * divisor <= value; divisor += 2)
            if (value % divisor == 0) return false;
        return true;
    }

    private static int GreatestCommonDivisor(int a, int b)
    {
        while (b != 0) (a, b) = (b, a % b);
        return Math.Abs(a);
    }

    private static int Mod(long value, int modulus) => (int)((value % modulus + modulus) % modulus);

    private static int PowMod(int value, int exponent, int modulus)
    {
        long result = 1, factor = Mod(value, modulus);
        while (exponent > 0)
        {
            if ((exponent & 1) != 0) result = result * factor % modulus;
            factor = factor * factor % modulus;
            exponent >>= 1;
        }
        return (int)result;
    }

    private static LaboratoryPoint Lerp(LaboratoryPoint from, LaboratoryPoint to, double amount) =>
        new(from.X + (to.X - from.X) * amount, from.Y + (to.Y - from.Y) * amount);

    private static Color Palette(MathematicalLaboratoryState state, double amount)
    {
        amount -= Math.Floor(amount);
        return amount < 0.5
            ? Mix(state.PrimaryColor, state.AccentColor, amount * 2)
            : Mix(state.AccentColor, state.SecondaryColor, amount * 2 - 1);
    }

    private static Color Mix(Color from, Color to, double amount)
    {
        amount = Math.Clamp(amount, 0, 1);
        return Color.FromRgb(
            (byte)Math.Round(from.R + (to.R - from.R) * amount),
            (byte)Math.Round(from.G + (to.G - from.G) * amount),
            (byte)Math.Round(from.B + (to.B - from.B) * amount));
    }

    private readonly record struct RationalNode(long P, long Q, double X, double Y, int Parent, int Depth);
    private readonly record struct FourierCoefficient(int Frequency, Complex Value);
    private readonly record struct WaveSource(double X, double Y, double Phase);

    private sealed class RasterCanvas
    {
        private readonly MathematicalLaboratoryState _state;
        private readonly double _scale;

        public RasterCanvas(int width, int height, MathematicalLaboratoryState state)
        {
            Width = width;
            Height = height;
            _state = state;
            _scale = Math.Min(width, height) * 0.5;
            Pixels = new byte[checked(width * height * 4)];
        }

        public int Width { get; }
        public int Height { get; }
        public byte[] Pixels { get; }

        public void Clear(Color color) => RasterDrawing.Fill(Pixels, color);

        public (double x, double y) Map(double x, double y)
        {
            double dx = (x - _state.ViewCenterX) * _state.Zoom;
            double dy = (y - _state.ViewCenterY) * _state.Zoom;
            double radians = _state.Rotation * Math.PI / 180;
            double cosine = Math.Cos(radians), sine = Math.Sin(radians);
            double rx = dx * cosine - dy * sine;
            double ry = dx * sine + dy * cosine;
            return (Width / 2d + rx * _scale, Height / 2d - ry * _scale);
        }

        public (double x, double y) Unmap(double x, double y)
        {
            double rotatedX = (x - Width / 2d) / _scale;
            double rotatedY = -(y - Height / 2d) / _scale;
            double radians = _state.Rotation * Math.PI / 180;
            double cosine = Math.Cos(radians), sine = Math.Sin(radians);
            double dx = rotatedX * cosine + rotatedY * sine;
            double dy = -rotatedX * sine + rotatedY * cosine;
            return (_state.ViewCenterX + dx / _state.Zoom,
                _state.ViewCenterY + dy / _state.Zoom);
        }

        public void SetPixel(int x, int y, Color color)
        {
            if ((uint)x >= (uint)Width || (uint)y >= (uint)Height) return;
            int offset = (y * Width + x) * 4;
            Pixels[offset] = color.B;
            Pixels[offset + 1] = color.G;
            Pixels[offset + 2] = color.R;
            Pixels[offset + 3] = 255;
        }

        public void Line(
            double x1, double y1, double x2, double y2,
            double thickness, Color color, double opacity = 1)
        {
            (double sx1, double sy1) = Map(x1, y1);
            (double sx2, double sy2) = Map(x2, y2);
            double dx = sx2 - sx1, dy = sy2 - sy1;
            double length = Math.Sqrt(dx * dx + dy * dy);
            if (!double.IsFinite(length) || length > 50_000) return;
            int steps = Math.Max(1, (int)Math.Ceiling(length / 0.75));
            double radius = Math.Max(0.5, thickness / 2);
            for (int step = 0; step <= steps; step++)
            {
                double amount = (double)step / steps;
                Stamp(sx1 + dx * amount, sy1 + dy * amount, radius, color, opacity);
            }
        }

        public void Circle(
            double x, double y, double radius, double thickness,
            Color color, bool filled)
        {
            (double sx, double sy) = Map(x, y);
            double pixelRadius = Math.Abs(radius * _state.Zoom * _scale);
            if (pixelRadius > Math.Max(Width, Height) * 8 || pixelRadius < 0.2) return;
            RasterDrawing.DrawCircle(Pixels, Width, Height, sx, sy, pixelRadius,
                Math.Max(0.5, thickness), color, filled);
        }

        public void Polygon(
            IReadOnlyList<LaboratoryPoint> points, Color color,
            bool filled, double value)
        {
            if (points.Count < 3) return;
            var mapped = points.Select(point => Map(point.X, point.Y)).ToArray();
            if (filled) FillPolygon(mapped, color, value);
            else
                for (int i = 0; i < points.Count; i++)
                    Line(points[i].X, points[i].Y,
                        points[(i + 1) % points.Count].X, points[(i + 1) % points.Count].Y,
                        value, color);
        }

        public void PixelSquare(int x, int y, int size, Color color, double opacity)
        {
            int half = Math.Max(0, size / 2);
            for (int py = y - half; py <= y + half; py++)
                for (int px = x - half; px <= x + half; px++)
                    Blend(px, py, color, opacity);
        }

        private void FillPolygon((double x, double y)[] points, Color color, double opacity)
        {
            int minimumY = Math.Max(0, (int)Math.Floor(points.Min(point => point.y)));
            int maximumY = Math.Min(Height - 1, (int)Math.Ceiling(points.Max(point => point.y)));
            var intersections = new List<double>(points.Length);
            for (int y = minimumY; y <= maximumY; y++)
            {
                intersections.Clear();
                double scan = y + 0.5;
                for (int i = 0; i < points.Length; i++)
                {
                    (double x, double y) first = points[i];
                    (double x, double y) second = points[(i + 1) % points.Length];
                    if (first.y > second.y) (first, second) = (second, first);
                    if (scan < first.y || scan >= second.y || Math.Abs(second.y - first.y) < 1e-12) continue;
                    intersections.Add(first.x + (scan - first.y) * (second.x - first.x) / (second.y - first.y));
                }
                intersections.Sort();
                for (int i = 0; i + 1 < intersections.Count; i += 2)
                {
                    int left = Math.Max(0, (int)Math.Ceiling(intersections[i]));
                    int right = Math.Min(Width - 1, (int)Math.Floor(intersections[i + 1]));
                    for (int x = left; x <= right; x++) Blend(x, y, color, opacity);
                }
            }
        }

        private void Stamp(double centerX, double centerY, double radius, Color color, double opacity)
        {
            int left = Math.Max(0, (int)Math.Floor(centerX - radius - 1));
            int right = Math.Min(Width - 1, (int)Math.Ceiling(centerX + radius + 1));
            int top = Math.Max(0, (int)Math.Floor(centerY - radius - 1));
            int bottom = Math.Min(Height - 1, (int)Math.Ceiling(centerY + radius + 1));
            for (int y = top; y <= bottom; y++)
                for (int x = left; x <= right; x++)
                {
                    double dx = x + 0.5 - centerX, dy = y + 0.5 - centerY;
                    double coverage = Math.Clamp(radius + 0.75 - Math.Sqrt(dx * dx + dy * dy), 0, 1) * opacity;
                    if (coverage > 0) Blend(x, y, color, coverage);
                }
        }

        private void Blend(int x, int y, Color color, double opacity)
        {
            if ((uint)x >= (uint)Width || (uint)y >= (uint)Height || opacity <= 0) return;
            opacity = Math.Clamp(opacity, 0, 1);
            int offset = (y * Width + x) * 4;
            double inverse = 1 - opacity;
            Pixels[offset] = (byte)Math.Round(Pixels[offset] * inverse + color.B * opacity);
            Pixels[offset + 1] = (byte)Math.Round(Pixels[offset + 1] * inverse + color.G * opacity);
            Pixels[offset + 2] = (byte)Math.Round(Pixels[offset + 2] * inverse + color.R * opacity);
            Pixels[offset + 3] = 255;
        }
    }
}
