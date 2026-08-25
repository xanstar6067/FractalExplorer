using System.Numerics;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FractalExplorerWPF.Models;
using Color = System.Windows.Media.Color;

namespace FractalExplorerWPF.Core.Rendering;

/// <summary>
/// Rendering for the newer, computation-heavy mathematical laboratories.  It is
/// intentionally kept separate from MathematicalLaboratoryRenderer so the shared
/// window can grow without making its original renderer a single giant switch.
/// </summary>
public static class AdvancedMathematicalLaboratoryRenderer
{
    public static bool Supports(MathematicalLaboratoryKind kind) => kind is
        MathematicalLaboratoryKind.VoronoiLloyd or
        MathematicalLaboratoryKind.RecamanSequence or
        MathematicalLaboratoryKind.KnotStudio or
        MathematicalLaboratoryKind.StochasticMotion or
        MathematicalLaboratoryKind.KleinianSchottky;

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
        var surface = new RasterSurface(width, height, state);
        surface.Clear(state.BackgroundColor);
        progress?.Report(6);
        switch (state.Kind)
        {
            case MathematicalLaboratoryKind.VoronoiLloyd:
                RenderVoronoi(surface, state, token, progress);
                break;
            case MathematicalLaboratoryKind.RecamanSequence:
                RenderRecaman(surface, state, token, progress);
                break;
            case MathematicalLaboratoryKind.KnotStudio:
                RenderKnots(surface, state, token, progress);
                break;
            case MathematicalLaboratoryKind.StochasticMotion:
                RenderStochasticMotion(surface, state, token, progress);
                break;
            case MathematicalLaboratoryKind.KleinianSchottky:
                RenderKleinianSchottky(surface, state, token, progress);
                break;
        }
        progress?.Report(98);
        return surface.Pixels;
    }

    #region Voronoi and Lloyd relaxation

    private static void RenderVoronoi(
        RasterSurface surface,
        MathematicalLaboratoryState state,
        CancellationToken token,
        IProgress<int>? progress)
    {
        List<VoronoiSite> sites = CreateVoronoiSites(state);
        if (sites.Count == 0) return;

        int relaxationIterations = state.Mode is 1 or 4 ? Math.Clamp(state.SecondaryValue, 0, 80) : 0;
        if (state.Animate)
            relaxationIterations = (int)Math.Round(relaxationIterations * Math.Clamp(state.Phase, 0, 1));
        RelaxSites(sites, relaxationIterations, state.Mode, token, progress);

        long area = (long)surface.Width * surface.Height;
        int stride = area switch
        {
            > 32_000_000 => 4,
            > 8_000_000 => 2,
            _ => 1
        };
        int gridWidth = (surface.Width + stride - 1) / stride;
        int gridHeight = (surface.Height + stride - 1) / stride;
        var labels = new ushort[checked(gridWidth * gridHeight)];

        for (int gy = 0; gy < gridHeight; gy++)
        {
            if ((gy & 15) == 0)
            {
                token.ThrowIfCancellationRequested();
                progress?.Report(22 + gy * 54 / Math.Max(1, gridHeight));
            }
            int py = Math.Min(surface.Height - 1, gy * stride + stride / 2);
            for (int gx = 0; gx < gridWidth; gx++)
            {
                int px = Math.Min(surface.Width - 1, gx * stride + stride / 2);
                (double x, double y) = surface.Unmap(px, py);
                int nearest = FindNearestSite(sites, x, y, state.Mode);
                labels[gy * gridWidth + gx] = (ushort)nearest;
                Color cellColor = state.Filled
                    ? Mix(state.BackgroundColor, sites[nearest].Color, 0.58)
                    : Mix(state.BackgroundColor, sites[nearest].Color, 0.08);
                surface.FillScreenRect(gx * stride, gy * stride, stride, stride, cellColor);
            }
        }

        var neighbours = state.Mode == 4 ? new HashSet<long>() : null;
        Color boundary = Mix(state.PrimaryColor, Color.FromRgb(245, 248, 255), 0.58);
        double thickness = Math.Clamp(state.Parameter, 0.25, 8);
        for (int gy = 0; gy < gridHeight; gy++)
        {
            if ((gy & 31) == 0) token.ThrowIfCancellationRequested();
            for (int gx = 0; gx < gridWidth; gx++)
            {
                int index = gy * gridWidth + gx;
                int current = labels[index];
                if (gx > 0 && labels[index - 1] != current)
                {
                    AddNeighbour(neighbours, current, labels[index - 1]);
                    surface.ScreenLine(gx * stride, gy * stride, gx * stride, (gy + 1) * stride,
                        thickness, boundary, 0.92);
                }
                if (gy > 0 && labels[index - gridWidth] != current)
                {
                    AddNeighbour(neighbours, current, labels[index - gridWidth]);
                    surface.ScreenLine(gx * stride, gy * stride, (gx + 1) * stride, gy * stride,
                        thickness, boundary, 0.92);
                }
            }
        }

        if (neighbours is not null && state.ShowGuides)
        {
            foreach (long pair in neighbours)
            {
                int first = (int)(pair >> 32);
                int second = (int)pair;
                surface.Line(sites[first].X, sites[first].Y, sites[second].X, sites[second].Y,
                    Math.Max(0.7, thickness * 0.7), state.AccentColor, 0.44);
            }
        }

        if (state.ShowGuides)
        {
            double radius = Math.Clamp(0.011 / Math.Sqrt(Math.Max(0.2, state.Zoom)), 0.003, 0.018);
            foreach (VoronoiSite site in sites)
            {
                surface.Circle(site.X, site.Y, radius * 1.8, 1, state.BackgroundColor, true, 0.82);
                surface.Circle(site.X, site.Y, radius, 1, state.AccentColor, true, 1);
            }
        }
    }

    private static List<VoronoiSite> CreateVoronoiSites(MathematicalLaboratoryState state)
    {
        int count = Math.Clamp(state.PrimaryValue, 3, 256);
        var random = new Random(state.TertiaryValue);
        var sites = new List<VoronoiSite>(count);
        if (state.InputPoints.Count > 0)
        {
            foreach (LaboratoryPoint point in state.InputPoints.Take(256))
                sites.Add(new VoronoiSite(point.X, point.Y, random.NextDouble() * 0.16 - 0.08,
                    Palette(state, random.NextDouble())));
            return sites;
        }

        double shiftX = random.NextDouble();
        double shiftY = random.NextDouble();
        for (int i = 0; i < count; i++)
        {
            double x = -0.94 + 1.88 * ((Halton(i + 1, 2) + shiftX) % 1);
            double y = -0.94 + 1.88 * ((Halton(i + 1, 3) + shiftY) % 1);
            x += (random.NextDouble() - 0.5) * 0.035;
            y += (random.NextDouble() - 0.5) * 0.035;
            sites.Add(new VoronoiSite(x, y, random.NextDouble() * 0.16 - 0.08,
                Palette(state, (i * 0.61803398875 + random.NextDouble() * 0.08) % 1)));
        }
        return sites;
    }

    private static void RelaxSites(
        List<VoronoiSite> sites,
        int iterations,
        int mode,
        CancellationToken token,
        IProgress<int>? progress)
    {
        const int resolution = 150;
        var sumX = new double[sites.Count];
        var sumY = new double[sites.Count];
        var counts = new int[sites.Count];
        for (int iteration = 0; iteration < iterations; iteration++)
        {
            token.ThrowIfCancellationRequested();
            Array.Clear(sumX); Array.Clear(sumY); Array.Clear(counts);
            for (int yIndex = 0; yIndex < resolution; yIndex++)
            {
                double y = -0.98 + 1.96 * (yIndex + 0.5) / resolution;
                for (int xIndex = 0; xIndex < resolution; xIndex++)
                {
                    double x = -0.98 + 1.96 * (xIndex + 0.5) / resolution;
                    int nearest = FindNearestSite(sites, x, y, mode);
                    sumX[nearest] += x;
                    sumY[nearest] += y;
                    counts[nearest]++;
                }
            }
            for (int i = 0; i < sites.Count; i++)
                if (counts[i] > 0)
                    sites[i] = sites[i] with
                    {
                        X = sumX[i] / counts[i],
                        Y = sumY[i] / counts[i]
                    };
            progress?.Report(8 + (iteration + 1) * 12 / Math.Max(1, iterations));
        }
    }

    private static int FindNearestSite(IReadOnlyList<VoronoiSite> sites, double x, double y, int mode)
    {
        int nearest = 0;
        double best = double.PositiveInfinity;
        for (int i = 0; i < sites.Count; i++)
        {
            double dx = x - sites[i].X;
            double dy = y - sites[i].Y;
            double distance = mode switch
            {
                2 => dx * dx + dy * dy - sites[i].Weight,
                3 => Math.Abs(dx) + Math.Abs(dy),
                _ => dx * dx + dy * dy
            };
            if (distance >= best) continue;
            best = distance;
            nearest = i;
        }
        return nearest;
    }

    private static void AddNeighbour(HashSet<long>? neighbours, int left, int right)
    {
        if (neighbours is null || left == right) return;
        int first = Math.Min(left, right);
        int second = Math.Max(left, right);
        neighbours.Add(((long)first << 32) | (uint)second);
    }

    #endregion

    #region Recaman sequence

    private static void RenderRecaman(
        RasterSurface surface,
        MathematicalLaboratoryState state,
        CancellationToken token,
        IProgress<int>? progress)
    {
        int requested = Math.Clamp(state.PrimaryValue, 10, 20_000);
        int count = state.Animate
            ? Math.Clamp((int)Math.Round(requested * Math.Max(0.015, state.Phase)), 2, requested)
            : requested;
        long current = Math.Max(0, state.SecondaryValue);
        var used = new HashSet<long> { current };
        var values = new long[count];
        values[0] = current;
        for (int n = 1; n < count; n++)
        {
            if ((n & 1023) == 0)
            {
                token.ThrowIfCancellationRequested();
                progress?.Report(8 + n * 30 / count);
            }
            long backwards = current - n;
            current = backwards >= 0 && !used.Contains(backwards) ? backwards : current + n;
            used.Add(current);
            values[n] = current;
        }

        long minimum = values.Min();
        long maximum = values.Max();
        double range = Math.Max(1, maximum - minimum);
        double X(long value) => -0.92 + 1.84 * (value - minimum) / range;
        double thickness = Math.Clamp(state.Parameter, 0.25, 10);
        int period = Math.Clamp(state.TertiaryValue, 1, 2_000);

        switch (Math.Clamp(state.Mode, 0, 3))
        {
            case 0:
            case 1:
                if (state.ShowGuides)
                    surface.Line(-0.96, 0, 0.96, 0, 1, Mix(state.PrimaryColor, state.BackgroundColor, 0.58), 0.8);
                for (int i = 1; i < count; i++)
                {
                    if ((i & 255) == 0) token.ThrowIfCancellationRequested();
                    double x1 = X(values[i - 1]);
                    double x2 = X(values[i]);
                    double center = (x1 + x2) * 0.5;
                    double radius = Math.Abs(x2 - x1) * 0.5;
                    double sign = state.Mode == 1 || (i & 1) == 0 ? 1 : -1;
                    int segments = Math.Clamp((int)(radius * surface.Scale * Math.PI / 3), 8, 180);
                    Color color = Palette(state, (double)(i % period) / period);
                    double previousX = x1, previousY = 0;
                    for (int segment = 1; segment <= segments; segment++)
                    {
                        double angle = Math.PI * segment / segments;
                        double x = center - (x2 >= x1 ? 1 : -1) * radius * Math.Cos(angle);
                        double y = sign * radius * Math.Sin(angle);
                        surface.Line(previousX, previousY, x, y, thickness, color, 0.78);
                        previousX = x; previousY = y;
                    }
                }
                break;
            case 2:
                if (state.ShowGuides)
                    surface.Circle(0, 0, 0.88, 1, Mix(state.PrimaryColor, state.BackgroundColor, 0.6), false, 0.9);
                for (int i = 1; i < count; i++)
                {
                    if ((i & 511) == 0) token.ThrowIfCancellationRequested();
                    double a1 = Math.Tau * (values[i - 1] - minimum) / range - Math.PI / 2;
                    double a2 = Math.Tau * (values[i] - minimum) / range - Math.PI / 2;
                    surface.Line(0.88 * Math.Cos(a1), 0.88 * Math.Sin(a1),
                        0.88 * Math.Cos(a2), 0.88 * Math.Sin(a2), thickness,
                        Palette(state, (double)(i % period) / period), 0.32);
                }
                break;
            default:
                var points = new LaboratoryPoint[count];
                double px = 0, py = 0, maxRadius = 1;
                points[0] = new LaboratoryPoint(0, 0);
                for (int i = 1; i < count; i++)
                {
                    double angle = values[i] * 2.399963229728653;
                    double step = 0.3 + Math.Sqrt(i);
                    px += Math.Cos(angle) * step;
                    py += Math.Sin(angle) * step;
                    points[i] = new LaboratoryPoint(px, py);
                    maxRadius = Math.Max(maxRadius, Math.Max(Math.Abs(px), Math.Abs(py)));
                }
                for (int i = 1; i < count; i++)
                    surface.Line(points[i - 1].X * 0.9 / maxRadius, points[i - 1].Y * 0.9 / maxRadius,
                        points[i].X * 0.9 / maxRadius, points[i].Y * 0.9 / maxRadius, thickness,
                        Palette(state, (double)(i % period) / period), 0.72);
                break;
        }
        progress?.Report(94);
    }

    #endregion

    #region Knots and braids

    private static void RenderKnots(
        RasterSurface surface,
        MathematicalLaboratoryState state,
        CancellationToken token,
        IProgress<int>? progress)
    {
        int p = Math.Clamp(state.PrimaryValue, 1, 32);
        int q = Math.Clamp(state.SecondaryValue, 1, 64);
        int samples = Math.Clamp(state.TertiaryValue, 200, 30_000);
        samples = Math.Min(samples, Math.Max(800, surface.Width * 5));
        double spatialRotation = state.Phase * Math.Tau;
        var segments = new List<KnotSegment>(samples * Math.Min(p, 8));

        if (state.Mode == 2)
        {
            int strands = Math.Clamp(p, 2, 16);
            int perStrand = Math.Max(160, samples / strands);
            for (int strand = 0; strand < strands; strand++)
            {
                Point3 previous = ProjectKnotPoint(CreateBraidPoint(0, strand, strands, q), spatialRotation);
                for (int i = 1; i <= perStrand; i++)
                {
                    double t = Math.Tau * i / perStrand;
                    Point3 point = ProjectKnotPoint(CreateBraidPoint(t, strand, strands, q), spatialRotation);
                    segments.Add(new KnotSegment(previous, point, (double)i / perStrand + (double)strand / strands));
                    previous = point;
                }
            }
        }
        else
        {
            Point3 previous = ProjectKnotPoint(CreateKnotPoint(0, p, q, state.Mode), spatialRotation);
            for (int i = 1; i <= samples; i++)
            {
                if ((i & 1023) == 0)
                {
                    token.ThrowIfCancellationRequested();
                    progress?.Report(8 + i * 45 / samples);
                }
                double t = Math.Tau * i / samples;
                Point3 point = ProjectKnotPoint(CreateKnotPoint(t, p, q, state.Mode), spatialRotation);
                segments.Add(new KnotSegment(previous, point, (double)i / samples));
                previous = point;
            }
        }

        if (state.ShowGuides)
        {
            surface.Circle(0, 0, 0.68, 1, Mix(state.PrimaryColor, state.BackgroundColor, 0.68), false, 0.7);
            surface.Line(-0.84, 0, 0.84, 0, 1, Mix(state.PrimaryColor, state.BackgroundColor, 0.72), 0.55);
            surface.Line(0, -0.84, 0, 0.84, 1, Mix(state.SecondaryColor, state.BackgroundColor, 0.72), 0.55);
        }

        segments.Sort((left, right) => left.Depth.CompareTo(right.Depth));
        double baseThickness = state.Filled ? Math.Clamp(state.Parameter, 0.4, 20) : 1.25;
        for (int i = 0; i < segments.Count; i++)
        {
            if ((i & 2047) == 0) token.ThrowIfCancellationRequested();
            KnotSegment segment = segments[i];
            double depthAmount = Math.Clamp((segment.Depth + 1.4) / 2.8, 0, 1);
            double thickness = baseThickness * (0.72 + depthAmount * 0.48);
            if (state.Filled)
                surface.Line(segment.From.X, segment.From.Y, segment.To.X, segment.To.Y,
                    thickness + 3.2, state.BackgroundColor, 0.88);
            surface.Line(segment.From.X, segment.From.Y, segment.To.X, segment.To.Y,
                thickness, Mix(Palette(state, segment.Parameter), Color.FromRgb(255, 255, 255), depthAmount * 0.28),
                0.94);
        }
        progress?.Report(95);
    }

    private static Point3 CreateKnotPoint(double t, int p, int q, int mode)
    {
        return mode switch
        {
            0 => new Point3(
                (0.58 + 0.26 * Math.Cos(q * t)) * Math.Cos(p * t),
                (0.58 + 0.26 * Math.Cos(q * t)) * Math.Sin(p * t),
                0.26 * Math.Sin(q * t)),
            1 => new Point3(
                0.76 * Math.Sin(p * t + Math.PI / 5),
                0.76 * Math.Sin(q * t),
                0.6 * Math.Sin((p + q + 1) * t + Math.PI / 3)),
            _ => new Point3(
                0.75 * Math.Sin(p * t),
                0.75 * Math.Sin(q * t + Math.PI / 4),
                0.58 * Math.Sin((2 * p + q) * t + Math.PI / 7))
        };
    }

    private static Point3 CreateBraidPoint(double t, int strand, int strands, int turns)
    {
        double phase = Math.Tau * strand / strands;
        double cross = turns * t + phase;
        double radius = 0.56 + 0.17 * Math.Cos(cross);
        return new Point3(radius * Math.Cos(t), radius * Math.Sin(t), 0.22 * Math.Sin(cross));
    }

    private static Point3 ProjectKnotPoint(Point3 point, double rotation)
    {
        double cosY = Math.Cos(rotation), sinY = Math.Sin(rotation);
        double x1 = point.X * cosY + point.Z * sinY;
        double z1 = -point.X * sinY + point.Z * cosY;
        const double tilt = -0.46;
        double cosX = Math.Cos(tilt), sinX = Math.Sin(tilt);
        double y2 = point.Y * cosX - z1 * sinX;
        double z2 = point.Y * sinX + z1 * cosX;
        double perspective = 1 / Math.Max(0.55, 1.25 - z2 * 0.22);
        return new Point3(x1 * perspective, y2 * perspective, z2);
    }

    #endregion

    #region Brownian motion and Levy flights

    private static void RenderStochasticMotion(
        RasterSurface surface,
        MathematicalLaboratoryState state,
        CancellationToken token,
        IProgress<int>? progress)
    {
        int requestedSteps = Math.Clamp(state.PrimaryValue, 20, 200_000);
        int steps = state.Animate
            ? Math.Clamp((int)Math.Round(requestedSteps * Math.Max(0.01, state.Phase)), 2, requestedSteps)
            : requestedSteps;
        int paths = Math.Clamp(state.SecondaryValue, 1, 1_000);
        int mode = Math.Clamp(state.Mode, 0, 4);
        const int pointBudget = 1_800_000;
        if (mode != 3 && (long)steps * paths > pointBudget)
            paths = Math.Max(1, pointBudget / steps);
        double parameter = Math.Clamp(state.Parameter, 0.2, 2);
        var allPaths = new List<List<LaboratoryPoint>>(mode == 3 ? 0 : paths);
        var endpoints = new List<LaboratoryPoint>(paths);
        double startX = state.AnchorX;
        double startY = state.AnchorY;

        for (int pathIndex = 0; pathIndex < paths; pathIndex++)
        {
            if ((pathIndex & 7) == 0)
            {
                token.ThrowIfCancellationRequested();
                progress?.Report(8 + pathIndex * 62 / Math.Max(1, paths));
            }
            var random = new Random(unchecked(state.TertiaryValue + pathIndex * 1_000_003));
            double x = startX, y = startY;
            if (mode == 3)
            {
                double elapsedFraction = Math.Sqrt((double)steps / requestedSteps);
                x += NextGaussian(random) * 0.72 * parameter * elapsedFraction;
                y += NextGaussian(random) * 0.72 * parameter * elapsedFraction;
                endpoints.Add(new LaboratoryPoint(x, y));
                continue;
            }
            double brownianScale = 0.72 * parameter / Math.Sqrt(requestedSteps);
            double levyScale = 0.68 / Math.Pow(requestedSteps, 1 / Math.Max(0.35, parameter));
            double direction = random.NextDouble() * Math.Tau;
            List<LaboratoryPoint>? points = mode == 3 ? null : new List<LaboratoryPoint>(steps + 1)
            {
                new(startX, startY)
            };
            for (int step = 1; step <= steps; step++)
            {
                if ((step & 8191) == 0) token.ThrowIfCancellationRequested();
                switch (mode)
                {
                    case 1:
                        double levyAngle = random.NextDouble() * Math.Tau;
                        double length = levyScale * Math.Pow(Math.Max(1e-12, random.NextDouble()), -1 / parameter);
                        length = Math.Min(length, 0.42);
                        x += Math.Cos(levyAngle) * length;
                        y += Math.Sin(levyAngle) * length;
                        break;
                    case 4:
                        double inertia = Math.Clamp(parameter / 2, 0.1, 0.985);
                        direction += NextGaussian(random) * (1 - inertia) * 2.8;
                        x += Math.Cos(direction) * 0.82 / Math.Sqrt(requestedSteps);
                        y += Math.Sin(direction) * 0.82 / Math.Sqrt(requestedSteps);
                        break;
                    default:
                        x += NextGaussian(random) * brownianScale;
                        y += NextGaussian(random) * brownianScale;
                        break;
                }
                points?.Add(new LaboratoryPoint(x, y));
            }

            if (mode == 2 && points is not null)
            {
                double endDx = points[^1].X - startX;
                double endDy = points[^1].Y - startY;
                for (int i = 1; i < points.Count; i++)
                {
                    double amount = (double)i / (points.Count - 1);
                    points[i] = new LaboratoryPoint(points[i].X - endDx * amount, points[i].Y - endDy * amount);
                }
                x = points[^1].X; y = points[^1].Y;
            }
            if (points is not null) allPaths.Add(points);
            endpoints.Add(new LaboratoryPoint(x, y));
        }

        if (state.ShowGuides)
        {
            surface.Circle(startX, startY, 0.025, 1.5, state.AccentColor, false, 1);
            surface.Circle(startX, startY, 0.01, 1, state.AccentColor, true, 1);
            surface.Circle(startX, startY, 0.72 * (mode == 1 ? 1 : parameter), 1,
                Mix(state.PrimaryColor, state.BackgroundColor, 0.68), false, 0.55);
        }

        if (mode == 3)
        {
            for (int i = 0; i < endpoints.Count; i++)
            {
                Color color = Palette(state, (double)i / Math.Max(1, endpoints.Count));
                surface.Circle(endpoints[i].X, endpoints[i].Y, 0.008, 1, color, true, 0.34);
            }
        }
        else
        {
            for (int pathIndex = 0; pathIndex < allPaths.Count; pathIndex++)
            {
                List<LaboratoryPoint> points = allPaths[pathIndex];
                Color color = Palette(state, (double)pathIndex / Math.Max(1, allPaths.Count));
                double opacity = Math.Clamp(1.1 / Math.Sqrt(allPaths.Count), 0.16, 0.86);
                for (int i = 1; i < points.Count; i++)
                    surface.Line(points[i - 1].X, points[i - 1].Y, points[i].X, points[i].Y,
                        mode == 1 ? 1.35 : 1.05, color, opacity);
            }
        }
        progress?.Report(95);
    }

    #endregion

    #region Kleinian and Schottky groups

    private static void RenderKleinianSchottky(
        RasterSurface surface,
        MathematicalLaboratoryState state,
        CancellationToken token,
        IProgress<int>? progress)
    {
        int points = Math.Clamp(state.PrimaryValue, 2_000, 2_000_000);
        int burnIn = Math.Clamp(state.SecondaryValue, 1, 200);
        double deformation = Math.Clamp(state.Parameter, 0.05, 3);
        int mode = Math.Clamp(state.Mode, 0, 4);
        var random = new Random(state.TertiaryValue);
        Complex rotation = Complex.FromPolarCoordinates(1, state.Phase * Math.Tau);

        if (mode == 2)
            RenderTwoParabolicOrbit(surface, state, points, burnIn, deformation, random, rotation, token, progress);
        else
            RenderCircleGroupOrbit(surface, state, points, burnIn, deformation, mode, random, rotation, token, progress);
        progress?.Report(95);
    }

    private static void RenderCircleGroupOrbit(
        RasterSurface surface,
        MathematicalLaboratoryState state,
        int points,
        int burnIn,
        double deformation,
        int mode,
        Random random,
        Complex rotation,
        CancellationToken token,
        IProgress<int>? progress)
    {
        CircleGenerator[] generators;
        if (mode == 3)
        {
            double smallRadius = 0.34;
            generators =
            [
                new CircleGenerator(new Complex(0, 0), 0.92, new Complex(0, 0), 0.92, 0),
                new CircleGenerator(Complex.FromPolarCoordinates(0.43, -Math.PI / 2), smallRadius,
                    Complex.FromPolarCoordinates(0.43, -Math.PI / 2), smallRadius, 0),
                new CircleGenerator(Complex.FromPolarCoordinates(0.43, Math.PI / 6), smallRadius,
                    Complex.FromPolarCoordinates(0.43, Math.PI / 6), smallRadius, 0),
                new CircleGenerator(Complex.FromPolarCoordinates(0.43, 5 * Math.PI / 6), smallRadius,
                    Complex.FromPolarCoordinates(0.43, 5 * Math.PI / 6), smallRadius, 0)
            ];
        }
        else
        {
            double radius = Math.Clamp(0.255 + deformation * 0.115, 0.22, 0.42);
            double distance = Math.Clamp(0.72 - deformation * 0.055, 0.48, 0.7);
            double twist = mode == 1 ? deformation * 0.42 : 0;
            Complex left = new(-distance, 0), right = new(distance, 0);
            Complex bottom = new(0, -distance), top = new(0, distance);
            generators =
            [
                new CircleGenerator(right, radius, left, radius, twist),
                new CircleGenerator(left, radius, right, radius, -twist),
                new CircleGenerator(top, radius, bottom, radius, twist),
                new CircleGenerator(bottom, radius, top, radius, -twist)
            ];
        }

        Complex z = mode == 3 ? new Complex(0.1, 0.13) : Complex.Zero;
        int previous = -1;
        int total = checked(points + burnIn);
        for (int iteration = 0; iteration < total; iteration++)
        {
            if ((iteration & 8191) == 0)
            {
                token.ThrowIfCancellationRequested();
                progress?.Report(8 + iteration * 80 / Math.Max(1, total));
            }
            int branch;
            do branch = random.Next(generators.Length);
            while (mode != 3 && previous >= 0 && branch == (previous ^ 1));

            CircleGenerator generator = generators[branch];
            z = mode == 3 ? generator.Invert(z) : generator.Contract(z, branch);
            previous = branch;
            if (!IsFinite(z) || z.Magnitude > 1e8)
            {
                z = new Complex(random.NextDouble() * 0.1, random.NextDouble() * 0.1);
                previous = -1;
                continue;
            }
            if (iteration < burnIn) continue;
            Complex plotted = z * rotation;
            Color color = Palette(state, (branch + iteration * 0.000013) / generators.Length);
            surface.Plot(plotted.Real, plotted.Imaginary, color,
                state.Filled ? 0.075 : 0.3, state.Filled ? 1.35 : 0.7);
        }

        if (state.ShowGuides || mode == 4)
        {
            foreach (CircleGenerator generator in generators)
            {
                Complex center = generator.Target * rotation;
                surface.Circle(center.Real, center.Imaginary, generator.TargetRadius, 1.2,
                    state.AccentColor, false, 0.72);
            }
        }
    }

    private static void RenderTwoParabolicOrbit(
        RasterSurface surface,
        MathematicalLaboratoryState state,
        int points,
        int burnIn,
        double deformation,
        Random random,
        Complex rotation,
        CancellationToken token,
        IProgress<int>? progress)
    {
        Complex mu = new(-0.18 + deformation * 0.08, 0.45 + deformation * 0.82);
        Complex z = new(0.031, 0.071);
        int previous = -1;
        int total = points + burnIn;
        for (int iteration = 0; iteration < total; iteration++)
        {
            if ((iteration & 8191) == 0)
            {
                token.ThrowIfCancellationRequested();
                progress?.Report(8 + iteration * 80 / Math.Max(1, total));
            }
            int branch;
            do branch = random.Next(4);
            while (previous >= 0 && branch == (previous ^ 1));
            z = branch switch
            {
                0 => z + Complex.One,
                1 => z - Complex.One,
                2 => z / (mu * z + Complex.One),
                _ => z / (-mu * z + Complex.One)
            };
            previous = branch;
            if (!IsFinite(z) || z.Magnitude > 1e7)
            {
                z = new Complex(0.031, 0.071);
                previous = -1;
                continue;
            }
            if (iteration < burnIn) continue;
            Complex normalized = 0.56 * z / (1 + 0.16 * z.Magnitude) * rotation;
            surface.Plot(normalized.Real, normalized.Imaginary,
                Palette(state, branch / 4d + iteration * 0.000019),
                state.Filled ? 0.07 : 0.3, state.Filled ? 1.4 : 0.7);
        }

        if (state.ShowGuides)
        {
            surface.Line(-0.9, 0, 0.9, 0, 1, Mix(state.PrimaryColor, state.BackgroundColor, 0.62), 0.7);
            surface.Circle(0, 0, 0.56 / Math.Max(0.2, mu.Magnitude), 1,
                state.AccentColor, false, 0.5);
        }
    }

    #endregion

    private static double Halton(int index, int radix)
    {
        double result = 0;
        double fraction = 1d / radix;
        while (index > 0)
        {
            result += fraction * (index % radix);
            index /= radix;
            fraction /= radix;
        }
        return result;
    }

    private static double NextGaussian(Random random)
    {
        double u1 = Math.Max(1e-12, random.NextDouble());
        double u2 = random.NextDouble();
        return Math.Sqrt(-2 * Math.Log(u1)) * Math.Cos(Math.Tau * u2);
    }

    private static bool IsFinite(Complex value) =>
        double.IsFinite(value.Real) && double.IsFinite(value.Imaginary);

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

    private readonly record struct VoronoiSite(double X, double Y, double Weight, Color Color);
    private readonly record struct Point3(double X, double Y, double Z);
    private readonly record struct KnotSegment(Point3 From, Point3 To, double Parameter)
    {
        public double Depth => (From.Z + To.Z) * 0.5;
    }

    private readonly record struct CircleGenerator(
        Complex Target,
        double TargetRadius,
        Complex Source,
        double SourceRadius,
        double Twist)
    {
        public Complex Contract(Complex value, int branch)
        {
            Complex normalized = value / 1.15;
            Complex bend = Complex.FromPolarCoordinates(0.29, branch * Math.Tau / 4 + Twist * 0.35);
            Complex denominator = Complex.One + Complex.Conjugate(bend) * normalized;
            if (denominator.Magnitude < 1e-12) denominator = new Complex(1e-12, 0);
            Complex diskAutomorphism = (normalized + bend) / denominator;
            return Target + TargetRadius * 0.94 * Complex.FromPolarCoordinates(1, Twist) * diskAutomorphism;
        }

        public Complex Invert(Complex value)
        {
            Complex denominator = Complex.Conjugate(value - Target);
            if (denominator.Magnitude < 1e-12) denominator = new Complex(1e-12, 0);
            return Target + TargetRadius * TargetRadius / denominator;
        }
    }

    private sealed class RasterSurface
    {
        private readonly MathematicalLaboratoryState _state;

        public RasterSurface(int width, int height, MathematicalLaboratoryState state)
        {
            Width = width;
            Height = height;
            _state = state;
            Scale = Math.Min(width, height) * 0.5;
            Pixels = new byte[checked(width * height * 4)];
        }

        public int Width { get; }
        public int Height { get; }
        public double Scale { get; }
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
            return (Width / 2d + rx * Scale, Height / 2d - ry * Scale);
        }

        public (double x, double y) Unmap(double x, double y)
        {
            double rotatedX = (x - Width / 2d) / Scale;
            double rotatedY = -(y - Height / 2d) / Scale;
            double radians = _state.Rotation * Math.PI / 180;
            double cosine = Math.Cos(radians), sine = Math.Sin(radians);
            double dx = rotatedX * cosine + rotatedY * sine;
            double dy = -rotatedX * sine + rotatedY * cosine;
            return (_state.ViewCenterX + dx / _state.Zoom, _state.ViewCenterY + dy / _state.Zoom);
        }

        public void FillScreenRect(int left, int top, int width, int height, Color color)
        {
            int right = Math.Min(Width, left + width);
            int bottom = Math.Min(Height, top + height);
            left = Math.Max(0, left); top = Math.Max(0, top);
            for (int y = top; y < bottom; y++)
            {
                int offset = (y * Width + left) * 4;
                for (int x = left; x < right; x++, offset += 4)
                {
                    Pixels[offset] = color.B;
                    Pixels[offset + 1] = color.G;
                    Pixels[offset + 2] = color.R;
                    Pixels[offset + 3] = 255;
                }
            }
        }

        public void Plot(double x, double y, Color color, double opacity, double radius)
        {
            (double sx, double sy) = Map(x, y);
            Stamp(sx, sy, radius, color, opacity);
        }

        public void Line(
            double x1, double y1, double x2, double y2,
            double thickness, Color color, double opacity = 1)
        {
            (double sx1, double sy1) = Map(x1, y1);
            (double sx2, double sy2) = Map(x2, y2);
            ScreenLine(sx1, sy1, sx2, sy2, thickness, color, opacity);
        }

        public void ScreenLine(
            double x1, double y1, double x2, double y2,
            double thickness, Color color, double opacity = 1)
        {
            double dx = x2 - x1, dy = y2 - y1;
            double length = Math.Sqrt(dx * dx + dy * dy);
            if (!double.IsFinite(length) || length > 100_000) return;
            int steps = Math.Max(1, (int)Math.Ceiling(length / 0.8));
            double radius = Math.Max(0.45, thickness / 2);
            for (int step = 0; step <= steps; step++)
            {
                double amount = (double)step / steps;
                Stamp(x1 + dx * amount, y1 + dy * amount, radius, color, opacity);
            }
        }

        public void Circle(
            double x, double y, double radius, double thickness,
            Color color, bool filled, double opacity)
        {
            (double sx, double sy) = Map(x, y);
            double pixelRadius = Math.Abs(radius * _state.Zoom * Scale);
            if (pixelRadius < 0.2 || pixelRadius > Math.Max(Width, Height) * 8) return;
            if (opacity >= 0.999)
            {
                RasterDrawing.DrawCircle(Pixels, Width, Height, sx, sy, pixelRadius,
                    Math.Max(0.5, thickness), color, filled);
                return;
            }
            if (filled)
            {
                Stamp(sx, sy, pixelRadius, color, opacity);
                return;
            }
            int segments = Math.Clamp((int)(pixelRadius * Math.Tau / 2), 24, 720);
            double previousX = sx + pixelRadius, previousY = sy;
            for (int i = 1; i <= segments; i++)
            {
                double angle = Math.Tau * i / segments;
                double nextX = sx + pixelRadius * Math.Cos(angle);
                double nextY = sy + pixelRadius * Math.Sin(angle);
                ScreenLine(previousX, previousY, nextX, nextY, thickness, color, opacity);
                previousX = nextX; previousY = nextY;
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
