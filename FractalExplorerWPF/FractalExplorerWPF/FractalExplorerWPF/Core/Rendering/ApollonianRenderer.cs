using System.Numerics;
using FractalExplorerWPF.Models;

namespace FractalExplorerWPF.Core.Rendering;

public sealed class ApollonianRenderer
{
    private readonly ApollonianState _state;
    private readonly int _width;
    private readonly int _height;
    private readonly byte[] _pixels;
    private readonly List<GasketCircle> _circles = [];
    private readonly Queue<DescartesNode> _pending = new();
    private readonly HashSet<CircleKey> _known = [];
    private int _plottedCircles;

    public int CircleCount => _circles.Count;
    public int CurrentDepth { get; private set; }
    public bool Complete => _circles.Count >= _state.MaxCircles || _pending.Count == 0;

    public ApollonianRenderer(ApollonianState state, int width, int height)
    {
        _state = state.Clone();
        _width = width;
        _height = height;
        _pixels = new byte[checked(width * height * 4)];
        RasterDrawing.Fill(_pixels, _state.BackgroundColor);
        InitializeSymmetricConfiguration();
    }

    public void Advance(int maximumNewCircles, CancellationToken token)
    {
        int target = Math.Min(_state.MaxCircles, _circles.Count + Math.Max(1, maximumNewCircles));
        while (_pending.Count > 0 && _circles.Count < target && !token.IsCancellationRequested)
        {
            DescartesNode node = _pending.Dequeue();
            if (node.Depth >= _state.MaxDepth) continue;

            // A dequeued Descartes quadruple must be processed atomically. Stopping halfway
            // through its four alternatives would silently discard entire recursive branches.
            for (int replacedIndex = 0; replacedIndex < 4 && _circles.Count < _state.MaxCircles; replacedIndex++)
            {
                if (node.LastReplacedIndex == replacedIndex) continue;
                GasketCircle candidate = ReplaceCircle(node.Circles, replacedIndex,
                    node.Depth + 1, node.Depth == 0 ? replacedIndex : node.RootBranch);

                if (!IsUsable(candidate) || !_known.Add(Key(candidate))) continue;
                _circles.Add(candidate);
                CurrentDepth = Math.Max(CurrentDepth, candidate.Depth);

                var next = (GasketCircle[])node.Circles.Clone();
                next[replacedIndex] = candidate;
                _pending.Enqueue(new DescartesNode(next, replacedIndex, candidate.Depth, candidate.RootBranch));
            }
        }

        RasterizePending(token);
    }

    public byte[] CreateFrame() => (byte[])_pixels.Clone();

    private void InitializeSymmetricConfiguration()
    {
        double sqrtThree = Math.Sqrt(3);
        double radius = sqrtThree / (2 + sqrtThree);
        double distance = 1 - radius;
        double innerBend = 1 / radius;

        var initial = new GasketCircle[4];
        initial[0] = new GasketCircle(0, 0, -1, 0, 0);
        for (int index = 0; index < 3; index++)
        {
            double angle = -Math.PI / 2 + index * Math.PI * 2 / 3;
            initial[index + 1] = new GasketCircle(
                distance * Math.Cos(angle), distance * Math.Sin(angle), innerBend, 0, index + 1);
        }

        foreach (GasketCircle circle in initial)
        {
            _circles.Add(circle);
            _known.Add(Key(circle));
        }
        _pending.Enqueue(new DescartesNode(initial, -1, 0, 0));
    }

    private void RasterizePending(CancellationToken token)
    {
        double worldHeight = _state.ViewWidth * _height / _width;
        double pixelsPerWorldUnit = _width / _state.ViewWidth;
        for (; _plottedCircles < _circles.Count; _plottedCircles++)
        {
            if ((_plottedCircles & 255) == 0 && token.IsCancellationRequested) return;
            GasketCircle circle = _circles[_plottedCircles];
            double radiusPixels = circle.Radius * pixelsPerWorldUnit;
            if (radiusPixels < 0.22) continue;

            double x = (circle.X - (_state.CenterX - _state.ViewWidth / 2)) * pixelsPerWorldUnit;
            double y = ((_state.CenterY + worldHeight / 2) - circle.Y) * pixelsPerWorldUnit;
            if (x + radiusPixels < 0 || x - radiusPixels >= _width ||
                y + radiusPixels < 0 || y - radiusPixels >= _height)
                continue;

            bool filled = _state.DrawMode == ApollonianDrawMode.Filled && circle.Bend > 0;
            RasterDrawing.DrawCircle(_pixels, _width, _height, x, y, radiusPixels,
                _state.LineWidth, ColorFor(circle), filled);
        }
    }

    private System.Windows.Media.Color ColorFor(GasketCircle circle)
    {
        double amount = _state.ColoringMode switch
        {
            ApollonianColoringMode.Curvature => CurvatureAmount(circle),
            ApollonianColoringMode.ParentCircle => Math.Clamp(circle.RootBranch / 3d, 0, 1),
            _ => circle.Depth / (double)Math.Max(1, _state.MaxDepth)
        };
        return RasterDrawing.Lerp(_state.StartColor, _state.EndColor, amount);
    }

    private double CurvatureAmount(GasketCircle circle)
    {
        double maximumBend = 1 / Math.Max(1e-12, _state.MinimumRadius);
        double denominator = Math.Log(Math.Max(2, maximumBend));
        return denominator <= 0 ? 0 : Math.Log(Math.Max(1, Math.Abs(circle.Bend))) / denominator;
    }

    private bool IsUsable(GasketCircle circle) =>
        double.IsFinite(circle.X) && double.IsFinite(circle.Y) && double.IsFinite(circle.Bend) &&
        circle.Bend > 0 && circle.Radius >= _state.MinimumRadius &&
        Math.Abs(circle.X) <= 1.0000001 && Math.Abs(circle.Y) <= 1.0000001;

    private static GasketCircle ReplaceCircle(
        IReadOnlyList<GasketCircle> circles, int replacedIndex, int depth, int rootBranch)
    {
        double otherBends = 0;
        Complex otherBendCenters = Complex.Zero;
        for (int index = 0; index < 4; index++)
        {
            if (index == replacedIndex) continue;
            GasketCircle circle = circles[index];
            otherBends += circle.Bend;
            otherBendCenters += circle.Bend * new Complex(circle.X, circle.Y);
        }

        GasketCircle replaced = circles[replacedIndex];
        double bend = 2 * otherBends - replaced.Bend;
        Complex bendCenter = 2 * otherBendCenters -
                             replaced.Bend * new Complex(replaced.X, replaced.Y);
        Complex center = bendCenter / bend;
        return new GasketCircle(center.Real, center.Imaginary, bend, depth, rootBranch);
    }

    private static CircleKey Key(GasketCircle circle)
    {
        const double quantization = 10_000_000_000d;
        return new CircleKey(
            (long)Math.Round(circle.X * quantization),
            (long)Math.Round(circle.Y * quantization),
            (long)Math.Round(circle.Radius * quantization));
    }

    private readonly record struct GasketCircle(double X, double Y, double Bend, int Depth, int RootBranch)
    {
        public double Radius => 1 / Math.Abs(Bend);
    }

    private readonly record struct CircleKey(long X, long Y, long Radius);
    private sealed record DescartesNode(GasketCircle[] Circles, int LastReplacedIndex, int Depth, int RootBranch);
}
