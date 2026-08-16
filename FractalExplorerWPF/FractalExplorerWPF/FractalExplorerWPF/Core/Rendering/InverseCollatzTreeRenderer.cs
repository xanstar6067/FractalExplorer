using System.Diagnostics;
using System.Numerics;
using System.Windows.Media;
using FractalExplorerWPF.Models;
using Color = System.Windows.Media.Color;

namespace FractalExplorerWPF.Core.Rendering;

public sealed class InverseCollatzNode(BigInteger value, int depth, int parentIndex)
{
    public BigInteger Value { get; } = value;
    public int Depth { get; } = depth;
    public int ParentIndex { get; } = parentIndex;
    public int FirstChildIndex { get; set; } = -1;
    public int SecondChildIndex { get; set; } = -1;
}

public sealed record InverseCollatzTree(
    IReadOnlyList<InverseCollatzNode> Nodes,
    int MaximumDepth,
    bool Truncated,
    TimeSpan BuildDuration);

public readonly record struct InverseCollatzPoint(double X, double Y);

public sealed record InverseCollatzRenderResult(
    byte[] Pixels,
    int DrawnNodes,
    TimeSpan LayoutDuration,
    TimeSpan DrawDuration);

public static class InverseCollatzTreeRenderer
{
    public static InverseCollatzTree BuildTree(int maximumDepth, int maximumNodes,
        CancellationToken token, Action<int>? progress = null)
    {
        maximumDepth = Math.Clamp(maximumDepth, 1, 500);
        maximumNodes = Math.Clamp(maximumNodes, 10, 1_000_000);
        var watch = Stopwatch.StartNew();
        var nodes = new List<InverseCollatzNode>(Math.Min(maximumNodes, 100_000))
        {
            new(BigInteger.One, 0, -1)
        };
        var visited = new HashSet<BigInteger> { BigInteger.One };
        bool truncated = false;
        int reportedDepth = -1;

        for (int index = 0; index < nodes.Count; index++)
        {
            if ((index & 1023) == 0 && token.IsCancellationRequested)
                throw new OperationCanceledException(token);
            InverseCollatzNode node = nodes[index];
            if (node.Depth != reportedDepth)
            {
                reportedDepth = node.Depth;
                progress?.Invoke(node.Depth * 35 / maximumDepth);
            }
            if (node.Depth >= maximumDepth) continue;

            AddChild(node.Value << 1, index, node, nodes, visited, maximumNodes, ref truncated);
            if (truncated) break;

            // n is an odd predecessor of m exactly when n=(m-1)/3 is a
            // positive odd integer, equivalently m ≡ 4 (mod 6).
            if (node.Value % 6 == 4)
            {
                BigInteger oddPredecessor = (node.Value - 1) / 3;
                AddChild(oddPredecessor, index, node, nodes, visited, maximumNodes, ref truncated);
                if (truncated) break;
            }
        }

        watch.Stop();
        progress?.Invoke(35);
        int reachedDepth = nodes.Count == 0 ? 0 : nodes[^1].Depth;
        return new InverseCollatzTree(nodes, reachedDepth, truncated, watch.Elapsed);
    }

    private static void AddChild(BigInteger value, int parentIndex, InverseCollatzNode parent,
        List<InverseCollatzNode> nodes, HashSet<BigInteger> visited, int maximumNodes, ref bool truncated)
    {
        if (value <= 0 || !visited.Add(value)) return;
        if (nodes.Count >= maximumNodes)
        {
            visited.Remove(value);
            truncated = true;
            return;
        }

        int childIndex = nodes.Count;
        nodes.Add(new InverseCollatzNode(value, parent.Depth + 1, parentIndex));
        if (parent.FirstChildIndex < 0) parent.FirstChildIndex = childIndex;
        else parent.SecondChildIndex = childIndex;
    }

    public static InverseCollatzPoint[] CalculateLayout(InverseCollatzTree tree,
        InverseCollatzLayout layout, CancellationToken token)
    {
        IReadOnlyList<InverseCollatzNode> nodes = tree.Nodes;
        int count = nodes.Count;
        var weights = new double[count];
        var left = new double[count];
        var right = new double[count];
        var result = new InverseCollatzPoint[count];

        for (int index = count - 1; index >= 0; index--)
        {
            if ((index & 4095) == 0 && token.IsCancellationRequested)
                throw new OperationCanceledException(token);
            InverseCollatzNode node = nodes[index];
            double weight = 0;
            if (node.FirstChildIndex >= 0) weight += weights[node.FirstChildIndex];
            if (node.SecondChildIndex >= 0) weight += weights[node.SecondChildIndex];
            weights[index] = weight > 0 ? weight : 1;
        }

        left[0] = 0;
        right[0] = 1;
        int depthDivisor = Math.Max(1, tree.MaximumDepth);
        for (int index = 0; index < count; index++)
        {
            if ((index & 4095) == 0 && token.IsCancellationRequested)
                throw new OperationCanceledException(token);
            InverseCollatzNode node = nodes[index];
            double center = (left[index] + right[index]) * 0.5;
            double depth = node.Depth / (double)depthDivisor;
            if (layout == InverseCollatzLayout.Radial)
            {
                double angle = center * 2 * Math.PI - Math.PI / 2;
                result[index] = new InverseCollatzPoint(Math.Cos(angle) * depth, Math.Sin(angle) * depth);
            }
            else
            {
                result[index] = new InverseCollatzPoint(center * 2 - 1, depth * 2 - 1);
            }

            int first = node.FirstChildIndex;
            int second = node.SecondChildIndex;
            if (first < 0) continue;
            if (second < 0)
            {
                left[first] = left[index];
                right[first] = right[index];
                continue;
            }

            double split = left[index] + (right[index] - left[index]) *
                weights[first] / (weights[first] + weights[second]);
            left[first] = left[index];
            right[first] = split;
            left[second] = split;
            right[second] = right[index];
        }
        return result;
    }

    public static InverseCollatzRenderResult Render(InverseCollatzTree tree, InverseCollatzState state,
        int width, int height, int visibleDepth, CancellationToken token, Action<int>? progress = null,
        InverseCollatzPoint[]? precomputedLayout = null, double rasterScale = 1)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 1);
        var layoutWatch = Stopwatch.StartNew();
        InverseCollatzPoint[] points = precomputedLayout ?? CalculateLayout(tree, state.Layout, token);
        layoutWatch.Stop();
        progress?.Invoke(45);

        var drawWatch = Stopwatch.StartNew();
        byte[] pixels = CreateBackground(width, height, state.BackgroundColor, token);
        visibleDepth = Math.Clamp(visibleDepth, 0, tree.MaximumDepth);
        bool[] visible = BuildVisibility(tree, state, visibleDepth);
        int drawnNodes = 0;

        for (int index = 1; index < tree.Nodes.Count; index++)
        {
            if ((index & 2047) == 0)
            {
                if (token.IsCancellationRequested) throw new OperationCanceledException(token);
                progress?.Invoke(45 + index * 25 / Math.Max(1, tree.Nodes.Count));
            }
            if (!visible[index]) continue;
            int parent = FindVisibleParent(tree, visible, index, state.FilterBehavior);
            if (parent < 0) continue;
            (double x1, double y1) = ToScreen(points[parent], state, width, height);
            (double x2, double y2) = ToScreen(points[index], state, width, height);
            bool matches = MatchesFilter(tree.Nodes[index].Value, state);
            double opacity = state.Modulus > 0 && state.Residue >= 0 &&
                             state.FilterBehavior == InverseCollatzFilterBehavior.Highlight && !matches
                ? 0.08 : 0.42;
            Color color = DepthColor(state.Palette, tree.Nodes[index].Depth, tree.MaximumDepth);
            DrawLine(pixels, width, height, x1, y1, x2, y2,
                Math.Clamp(state.LineThickness, 0.2, 8) * Math.Sqrt(Math.Max(0.05, state.Zoom)),
                color, opacity, rasterScale);
        }

        for (int index = 0; index < tree.Nodes.Count; index++)
        {
            if ((index & 2047) == 0)
            {
                if (token.IsCancellationRequested) throw new OperationCanceledException(token);
                progress?.Invoke(70 + index * 30 / Math.Max(1, tree.Nodes.Count));
            }
            if (!visible[index]) continue;
            InverseCollatzNode node = tree.Nodes[index];
            bool matches = MatchesFilter(node.Value, state);
            double opacity = state.Modulus > 0 && state.Residue >= 0 &&
                             state.FilterBehavior == InverseCollatzFilterBehavior.Highlight && !matches
                ? 0.16 : 1;
            double radius = Math.Clamp(state.NodeRadius, 0.4, 20) *
                            Math.Sqrt(Math.Max(0.05, state.Zoom)) * Math.Max(0.1, rasterScale);
            if (matches && state.Modulus > 0 && state.Residue >= 0) radius *= 1.35;
            Color color = index == 0
                ? ContrastColor(state.BackgroundColor)
                : DepthColor(state.Palette, node.Depth, tree.MaximumDepth);
            (double x, double y) = ToScreen(points[index], state, width, height);
            DrawCircle(pixels, width, height, x, y, index == 0 ? radius * 1.8 : radius, color, opacity);
            drawnNodes++;
        }

        drawWatch.Stop();
        progress?.Invoke(100);
        return new InverseCollatzRenderResult(pixels, drawnNodes, layoutWatch.Elapsed, drawWatch.Elapsed);
    }

    private static bool[] BuildVisibility(InverseCollatzTree tree, InverseCollatzState state, int visibleDepth)
    {
        var visible = new bool[tree.Nodes.Count];
        for (int index = 0; index < tree.Nodes.Count; index++)
        {
            InverseCollatzNode node = tree.Nodes[index];
            if (node.Depth > visibleDepth) continue;
            visible[index] = index == 0 || state.FilterBehavior == InverseCollatzFilterBehavior.Highlight ||
                             MatchesFilter(node.Value, state);
        }
        return visible;
    }

    private static int FindVisibleParent(InverseCollatzTree tree, bool[] visible, int index,
        InverseCollatzFilterBehavior behavior)
    {
        int parent = tree.Nodes[index].ParentIndex;
        if (behavior != InverseCollatzFilterBehavior.OnlyMatching) return parent;
        while (parent >= 0 && !visible[parent]) parent = tree.Nodes[parent].ParentIndex;
        return parent;
    }

    private static bool MatchesFilter(BigInteger value, InverseCollatzState state)
    {
        if (state.Modulus <= 0 || state.Residue < 0) return true;
        return (int)(value % state.Modulus) == state.Residue;
    }

    private static (double X, double Y) ToScreen(InverseCollatzPoint point, InverseCollatzState state,
        int width, int height)
    {
        double zoom = Math.Clamp(state.Zoom, 0.05, 1000);
        if (state.Layout == InverseCollatzLayout.Radial)
        {
            double scale = Math.Min(width, height) * 0.46 * zoom;
            return (width / 2d + (point.X - state.CenterX) * scale,
                height / 2d + (point.Y - state.CenterY) * scale);
        }
        return (width / 2d + (point.X - state.CenterX) * width * 0.46 * zoom,
            height / 2d + (point.Y - state.CenterY) * height * 0.46 * zoom);
    }

    private static byte[] CreateBackground(int width, int height, Color color, CancellationToken token)
    {
        var pixels = new byte[checked(width * height * 4)];
        Parallel.For(0, height, (y, loopState) =>
        {
            if (token.IsCancellationRequested) { loopState.Stop(); return; }
            int offset = y * width * 4;
            for (int x = 0; x < width; x++)
            {
                pixels[offset++] = color.B;
                pixels[offset++] = color.G;
                pixels[offset++] = color.R;
                pixels[offset++] = color.A;
            }
        });
        return pixels;
    }

    private static void DrawLine(byte[] pixels, int width, int height, double x1, double y1,
        double x2, double y2, double thickness, Color color, double opacity, double rasterScale)
    {
        if (!ClipLine(ref x1, ref y1, ref x2, ref y2, width, height)) return;
        double dx = x2 - x1;
        double dy = y2 - y1;
        int steps = Math.Max(1, (int)Math.Ceiling(Math.Max(Math.Abs(dx), Math.Abs(dy))));
        double radius = Math.Max(0.45, thickness * Math.Max(0.1, rasterScale) * 0.5);
        for (int step = 0; step <= steps; step++)
        {
            double t = step / (double)steps;
            if (radius <= 0.7)
                BlendPixel(pixels, width, height, (int)Math.Round(x1 + dx * t),
                    (int)Math.Round(y1 + dy * t), color, opacity);
            else
                DrawCircle(pixels, width, height, x1 + dx * t, y1 + dy * t, radius, color, opacity);
        }
    }

    private static bool ClipLine(ref double x1, ref double y1, ref double x2, ref double y2,
        int width, int height)
    {
        double dx = x2 - x1;
        double dy = y2 - y1;
        double t0 = 0;
        double t1 = 1;
        if (!Clip(-dx, x1, ref t0, ref t1) || !Clip(dx, width - 1 - x1, ref t0, ref t1) ||
            !Clip(-dy, y1, ref t0, ref t1) || !Clip(dy, height - 1 - y1, ref t0, ref t1)) return false;
        double originalX = x1;
        double originalY = y1;
        if (t1 < 1) { x2 = originalX + t1 * dx; y2 = originalY + t1 * dy; }
        if (t0 > 0) { x1 = originalX + t0 * dx; y1 = originalY + t0 * dy; }
        return true;
    }

    private static bool Clip(double p, double q, ref double t0, ref double t1)
    {
        if (Math.Abs(p) < 1e-12) return q >= 0;
        double r = q / p;
        if (p < 0)
        {
            if (r > t1) return false;
            if (r > t0) t0 = r;
        }
        else
        {
            if (r < t0) return false;
            if (r < t1) t1 = r;
        }
        return true;
    }

    private static void DrawCircle(byte[] pixels, int width, int height, double centerX,
        double centerY, double radius, Color color, double opacity)
    {
        int left = Math.Max(0, (int)Math.Floor(centerX - radius - 1));
        int right = Math.Min(width - 1, (int)Math.Ceiling(centerX + radius + 1));
        int top = Math.Max(0, (int)Math.Floor(centerY - radius - 1));
        int bottom = Math.Min(height - 1, (int)Math.Ceiling(centerY + radius + 1));
        if (left > right || top > bottom) return;
        double outer = radius + 0.75;
        double inner = Math.Max(0, radius - 0.75);
        for (int y = top; y <= bottom; y++)
        {
            for (int x = left; x <= right; x++)
            {
                double distance = Math.Sqrt((x + 0.5 - centerX) * (x + 0.5 - centerX) +
                                            (y + 0.5 - centerY) * (y + 0.5 - centerY));
                if (distance > outer) continue;
                double coverage = distance <= inner ? 1 : (outer - distance) / (outer - inner);
                BlendPixel(pixels, width, height, x, y, color, opacity * coverage);
            }
        }
    }

    private static void BlendPixel(byte[] pixels, int width, int height, int x, int y,
        Color color, double opacity)
    {
        if ((uint)x >= (uint)width || (uint)y >= (uint)height || opacity <= 0) return;
        int offset = (y * width + x) * 4;
        double alpha = Math.Clamp(opacity * color.A / 255d, 0, 1);
        pixels[offset] = Blend(pixels[offset], color.B, alpha);
        pixels[offset + 1] = Blend(pixels[offset + 1], color.G, alpha);
        pixels[offset + 2] = Blend(pixels[offset + 2], color.R, alpha);
        pixels[offset + 3] = 255;
    }

    private static byte Blend(byte background, byte foreground, double alpha) =>
        (byte)Math.Clamp((int)Math.Round(background + (foreground - background) * alpha), 0, 255);

    private static Color DepthColor(InverseCollatzPalette palette, int depth, int maximumDepth)
    {
        double normalized;
        if (palette.Mapping == InverseCollatzPaletteMapping.RepeatByLevel)
        {
            int levels = Math.Clamp(palette.LevelsPerCycle, 2, 500);
            normalized = depth % levels / (double)(levels - 1);
        }
        else
        {
            normalized = depth / (double)Math.Max(1, maximumDepth);
        }
        if (palette.Reverse) normalized = 1 - normalized;
        return SamplePalette(palette, normalized);
    }

    private static Color SamplePalette(InverseCollatzPalette palette, double normalized)
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

    private static Color ContrastColor(Color background)
    {
        double luminance = (0.2126 * background.R + 0.7152 * background.G + 0.0722 * background.B) / 255;
        return luminance > 0.55 ? Colors.Black : Colors.White;
    }
}
