using System.Diagnostics;
using perturbation_theory.Models;

namespace perturbation_theory.Core.Rendering;

public readonly record struct PixelSample(int Iterations, double Smooth, bool Escaped,
    int Rebases = 0, bool UsedFallback = false);
public sealed record RenderedTile(int X, int Y, int Width, int Height, byte[] Pixels);
public sealed record RenderStatistics(TimeSpan Elapsed, TimeSpan ReferenceTime, int ReferenceIterations,
    long Rebases, long FallbackPixels, long Pixels);

// Identical pixel grid, scheduling, coloring and timing for both engines.
public static class MandelbrotFrameRenderer
{
    public static RenderStatistics Render(MandelbrotSettings settings,
        Func<decimal, decimal, CancellationToken, PixelSample> evaluateOffset,
        TimeSpan referenceTime, int referenceLength, int width, int height, CancellationToken token,
        Action<RenderedTile> publishTile, Action<int>? progress = null)
    {
        settings.ValidateSurface(width, height);
        token.ThrowIfCancellationRequested();
        var clock = Stopwatch.StartNew();
        var sampler = new PaletteSampler(settings);
        decimal viewWidth = 3m / settings.Zoom;
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
            MaxDegreeOfParallelism = settings.Threads == 0 ? Environment.ProcessorCount : settings.Threads
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
                    PixelSample sample = evaluateOffset(realOffsets[tile.X + x], imaginaryOffsets[tile.Y + y], token);
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
        return new RenderStatistics(clock.Elapsed + referenceTime, referenceTime, referenceLength,
            rebases, fallbackPixels, (long)width * height);
    }
}
