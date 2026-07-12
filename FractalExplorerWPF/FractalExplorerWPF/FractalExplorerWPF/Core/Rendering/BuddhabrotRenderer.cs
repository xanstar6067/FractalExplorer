using System.Collections.Concurrent;
using System.Windows.Media;
using FractalExplorerWPF.Infrastructure;
using FractalExplorerWPF.Models;
using Color = System.Windows.Media.Color;

namespace FractalExplorerWPF.Core.Rendering;

public sealed class BuddhabrotRenderer(BuddhabrotState state, int width, int height, int threads)
{
    private readonly int[] _density = new int[checked(width * height)];
    private readonly ConcurrentBag<List<(double Re, double Im)>> _pool = new();
    public int ProcessedSamples { get; private set; }

    public void Accumulate(int count, CancellationToken token)
    {
        int start = ProcessedSamples, end = Math.Min(state.SampleCount, start + Math.Max(1, count));
        double minRe = (double)state.SampleMinRe, maxRe = (double)state.SampleMaxRe;
        double minIm = (double)state.SampleMinIm, maxIm = (double)state.SampleMaxIm;
        double viewScale = 4d / Math.Max(0.0000001, (double)state.Zoom), viewScaleY = viewScale * height / width;
        Parallel.For(start, end, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, threads), CancellationToken = token },
            () => Rent(state.MaxIterations), (sample, _, orbit) =>
            {
                orbit.Clear(); ulong random = Mix((ulong)sample + 0x9E3779B97F4A7C15UL);
                double cr = minRe + Next(ref random) * (maxRe - minRe), ci = minIm + Next(ref random) * (maxIm - minIm);
                double zr = 0, zi = 0; bool escaped = false;
                for (int i = 0; i < state.MaxIterations; i++)
                {
                    if ((i & 63) == 0) token.ThrowIfCancellationRequested();
                    (zr, zi) = (zr * zr - zi * zi + cr, 2 * zr * zi + ci);
                    if (i >= 2) orbit.Add((zr, zi));
                    if (zr * zr + zi * zi > 16) { escaped = true; break; }
                }
                bool take = state.RenderMode == BuddhabrotRenderMode.AntiBuddhabrot ? !escaped : escaped;
                if (take)
                {
                    bool mirror = state.RenderMode == BuddhabrotRenderMode.SymmetricBuddhabrot;
                    foreach ((double re, double im) in orbit)
                    {
                        int x = (int)(((im - (double)state.CenterY) / viewScale + .5) * width);
                        int y = (int)(((re - (double)state.CenterX) / viewScaleY + .5) * height);
                        if ((uint)x < (uint)width && (uint)y < (uint)height) Interlocked.Increment(ref _density[y * width + x]);
                        if (mirror)
                        {
                            int mx = (int)(((-im - (double)state.CenterY) / viewScale + .5) * width);
                            if ((uint)mx < (uint)width && (uint)y < (uint)height) Interlocked.Increment(ref _density[y * width + mx]);
                        }
                    }
                }
                return orbit;
            }, Return);
        ProcessedSamples = end;
    }

    public byte[] CreateFrame()
    {
        byte[] pixels = new byte[checked(width * height * 4)]; int max = _density.Max();
        if (max <= 0) return pixels;
        double denominator = Math.Log(1 + max);
        for (int i = 0; i < _density.Length; i++)
        {
            double normalized = _density[i] <= 0 ? 0 : Math.Log(1 + _density[i]) / denominator;
            Color color = BuddhabrotPaletteManager.Evaluate(state.Palette, normalized, state.MaxIterations);
            int o = i * 4; pixels[o] = color.B; pixels[o + 1] = color.G; pixels[o + 2] = color.R; pixels[o + 3] = color.A;
        }
        return pixels;
    }

    private List<(double, double)> Rent(int capacity) { if (!_pool.TryTake(out var list)) return new List<(double, double)>(capacity); if (list.Capacity < capacity) list.Capacity = capacity; return list; }
    private void Return(List<(double, double)> list) { list.Clear(); _pool.Add(list); }
    private static ulong Mix(ulong value) { value += 0x9E3779B97F4A7C15UL; value = (value ^ value >> 30) * 0xBF58476D1CE4E5B9UL; value = (value ^ value >> 27) * 0x94D049BB133111EBUL; return value ^ value >> 31; }
    private static double Next(ref ulong state) { state = Mix(state); return (state >> 11) * (1d / (1UL << 53)); }
}
