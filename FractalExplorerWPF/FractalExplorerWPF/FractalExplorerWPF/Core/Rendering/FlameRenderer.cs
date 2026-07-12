using System.Collections.Concurrent;
using System.Windows.Media;
using FractalExplorerWPF.Models;
using Color = System.Windows.Media.Color;

namespace FractalExplorerWPF.Core.Rendering;

public sealed class FlameRenderer
{
    private readonly FlameState _state;
    private readonly int _width, _height, _threads;
    private readonly double[] _hit, _red, _green, _blue;
    private readonly List<FlameTransform> _transforms;
    private readonly double[] _weights;
    public int ProcessedSamples { get; private set; }

    public FlameRenderer(FlameState state, int width, int height, int threads)
    {
        _state = state.Clone(); _width = width; _height = height; _threads = Math.Max(1, threads);
        _hit = new double[checked(width * height)]; _red = new double[_hit.Length]; _green = new double[_hit.Length]; _blue = new double[_hit.Length];
        _transforms = _state.Transforms.Where(t => t.Weight > 0).Select(t => t.Clone()).ToList();
        double sum = _transforms.Sum(t => t.Weight), cumulative = 0;
        _weights = new double[_transforms.Count];
        for (int i = 0; i < _transforms.Count; i++) _weights[i] = cumulative += _transforms[i].Weight / Math.Max(sum, 1e-12);
        if (_weights.Length > 0) _weights[^1] = 1;
    }

    public void Accumulate(int count, CancellationToken token)
    {
        if (_transforms.Count == 0) { ProcessedSamples = _state.Samples; return; }
        int start = ProcessedSamples, end = Math.Min(_state.Samples, start + Math.Max(1, count));
        double worldWidth = Math.Max(1e-9, Math.Abs(_state.Scale));
        double worldHeight = worldWidth * _height / (double)_width;
        double left = _state.CenterX - worldWidth / 2, top = _state.CenterY + worldHeight / 2;
        var locals = new ConcurrentBag<Dictionary<int, Pixel>>();
        Parallel.For(start, end, new ParallelOptions { MaxDegreeOfParallelism = _threads, CancellationToken = token },
            () => new Dictionary<int, Pixel>(4096),
            (sample, _, local) =>
            {
                ulong seed = Mix((ulong)(sample + 1) * 0x9E3779B97F4A7C15UL);
                double x = Signed(ref seed), y = Signed(ref seed), cr = .5, cg = .5, cb = .5;
                int total = Math.Max(1, _state.WarmupIterations + _state.IterationsPerSample);
                for (int i = 0; i < total; i++)
                {
                    if ((i & 63) == 0) token.ThrowIfCancellationRequested();
                    FlameTransform transform = Select(Unit(ref seed));
                    Apply(transform, ref x, ref y);
                    cr = (cr + transform.Color.R / 255d) * .5; cg = (cg + transform.Color.G / 255d) * .5; cb = (cb + transform.Color.B / 255d) * .5;
                    if (i < _state.WarmupIterations || !double.IsFinite(x) || !double.IsFinite(y)) continue;
                    int px = (int)((x - left) / worldWidth * _width), py = (int)((top - y) / worldHeight * _height);
                    if ((uint)px >= (uint)_width || (uint)py >= (uint)_height) continue;
                    int index = py * _width + px; local.TryGetValue(index, out Pixel p);
                    p.Hit++; p.R += cr; p.G += cg; p.B += cb; local[index] = p;
                }
                return local;
            }, local => locals.Add(local));
        foreach (Dictionary<int, Pixel> local in locals)
            foreach ((int i, Pixel p) in local) { _hit[i] += p.Hit; _red[i] += p.R; _green[i] += p.G; _blue[i] += p.B; }
        ProcessedSamples = end;
    }

    public byte[] CreateCoverageFrame()
    {
        byte[] pixels = new byte[_hit.Length * 4]; double max = _hit.Max(); if (max <= 0) return pixels;
        double denominator = Math.Log(1 + max);
        for (int i = 0; i < _hit.Length; i++)
        {
            if (_hit[i] <= 0) continue; double t = Math.Log(1 + _hit[i]) / denominator;
            Color c = Heat(t); int p = i * 4; pixels[p] = c.B; pixels[p + 1] = c.G; pixels[p + 2] = c.R; pixels[p + 3] = 255;
        }
        return pixels;
    }

    public byte[] CreateFinalFrame()
    {
        byte[] pixels = new byte[_hit.Length * 4]; double max = _hit.Max(), denominator = Math.Log(1 + max);
        double exposure = Math.Max(.0001, _state.Exposure), invGamma = 1 / Math.Max(.1, _state.Gamma);
        for (int i = 0; i < _hit.Length; i++)
        {
            int p = i * 4; pixels[p + 3] = 255; if (_hit[i] <= 0 || max <= 0) continue;
            double mapped = Math.Log(1 + _hit[i]) / denominator;
            pixels[p + 2] = Channel(_red[i] / _hit[i], mapped, exposure, invGamma);
            pixels[p + 1] = Channel(_green[i] / _hit[i], mapped, exposure, invGamma);
            pixels[p] = Channel(_blue[i] / _hit[i], mapped, exposure, invGamma);
        }
        return pixels;
    }

    private FlameTransform Select(double value) { int i = Array.BinarySearch(_weights, value); if (i < 0) i = ~i; return _transforms[Math.Min(i, _transforms.Count - 1)]; }
    private static void Apply(FlameTransform t, ref double x, ref double y)
    {
        double ax = t.A * x + t.B * y + t.C, ay = t.D * x + t.E * y + t.F;
        if (t.Variation == FlameVariation.Sinusoidal) { x = Math.Sin(ax); y = Math.Sin(ay); }
        else if (t.Variation == FlameVariation.Spherical) { double r2 = ax * ax + ay * ay; if (r2 < 1e-12) x = y = 0; else { x = ax / r2; y = ay / r2; } }
        else { x = ax; y = ay; }
    }
    private static byte Channel(double average, double density, double exposure, double invGamma) => (byte)Math.Clamp(Math.Round(Math.Pow(1 - Math.Exp(-average * density * exposure), invGamma) * 255), 0, 255);
    private static Color Heat(double t) { t = Math.Clamp(t, 0, 1); if (t < .33) { double k = t / .33; return Color.FromRgb((byte)(k * 90), (byte)(k * 255), 255); } if (t < .66) { double k = (t - .33) / .33; return Color.FromRgb((byte)(90 + k * 165), 255, (byte)(255 - k * 255)); } double q = (t - .66) / .34; return Color.FromRgb(255, (byte)(255 - q * 255), (byte)(q * 120)); }
    private static ulong Mix(ulong v) { v += 0x9E3779B97F4A7C15UL; v = (v ^ v >> 30) * 0xBF58476D1CE4E5B9UL; v = (v ^ v >> 27) * 0x94D049BB133111EBUL; return v ^ v >> 31; }
    private static double Unit(ref ulong state) { state = Mix(state); return (state >> 11) * (1d / (1UL << 53)); }
    private static double Signed(ref ulong state) => Unit(ref state) * 2 - 1;
    private struct Pixel { public double Hit, R, G, B; }
}
