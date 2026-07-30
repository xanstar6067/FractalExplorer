using System.Collections.Concurrent;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using FractalExplorerWPF.Studio.Dsl;
using FractalExplorerWPF.Studio.Models;

namespace FractalExplorerWPF.Studio.Rendering;

public static class StudioOrbitRenderer
{
    private static readonly ConcurrentDictionary<string, Lazy<StudioCompiledFormula>> FormulaCache =
        new(StringComparer.Ordinal);

    public static StudioCompiledFormula Compile(string source)
    {
        string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source)));
        return FormulaCache.GetOrAdd(
            hash,
            _ => new Lazy<StudioCompiledFormula>(
                () => StudioFormulaCompiler.Compile(source),
                LazyThreadSafetyMode.ExecutionAndPublication)).Value;
    }

    public static Task<StudioLayerFrame> RenderAsync(
        StudioLayerSnapshot layer,
        int width,
        int height,
        int ssaa,
        int threadCount,
        CancellationToken token,
        IProgress<StudioRenderProgress>? progress = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 1);
        ssaa = Math.Clamp(ssaa, 1, 4);
        int workers = threadCount <= 0
            ? Environment.ProcessorCount
            : Math.Clamp(threadCount, 1, Environment.ProcessorCount);
        return Task.Run(() =>
        {
            StudioCompiledFormula formula = Compile(layer.FormulaSource);
            var frame = new StudioLayerFrame(width, height);
            IReadOnlyList<StudioTile> tiles = StudioTilePlanner.Create(width, height);
            var options = new ParallelOptions
            {
                CancellationToken = token,
                MaxDegreeOfParallelism = workers
            };
            int completed = 0;

            if (layer.PrecisionMode == StudioPrecisionMode.Decimal)
            {
                StudioDecimalParameterSet parameters = formula.CreateDecimalParameters(layer.Parameters);
                int maxIterations = formula.GetIntegerParameter(parameters, "maxIterations");
                Parallel.ForEach(tiles, options, tile =>
                {
                    RenderDecimalTile(layer, formula.DecimalKernel, parameters, maxIterations,
                        frame, tile, ssaa, options.CancellationToken);
                    int done = Interlocked.Increment(ref completed);
                    progress?.Report(new StudioRenderProgress(frame, tile, done, tiles.Count));
                });
            }
            else
            {
                StudioDoubleParameterSet parameters = formula.CreateDoubleParameters(layer.Parameters);
                int maxIterations = formula.GetIntegerParameter(parameters, "maxIterations");
                Parallel.ForEach(tiles, options, tile =>
                {
                    RenderDoubleTile(layer, formula.DoubleKernel, parameters, maxIterations,
                        frame, tile, ssaa, options.CancellationToken);
                    int done = Interlocked.Increment(ref completed);
                    progress?.Report(new StudioRenderProgress(frame, tile, done, tiles.Count));
                });
            }

            return frame;
        }, token);
    }

    private static void RenderDoubleTile(
        StudioLayerSnapshot layer,
        StudioDoubleKernel kernel,
        StudioDoubleParameterSet parameters,
        int maxIterations,
        StudioLayerFrame frame,
        StudioTile tile,
        int ssaa,
        CancellationToken token)
    {
        double viewWidth = 3d / Math.Max((double)layer.Zoom, 1e-28);
        double viewHeight = viewWidth * frame.Height / frame.Width;
        double centerX = (double)layer.CenterX;
        double centerY = (double)layer.CenterY;
        double invSamples = 1d / (ssaa * ssaa);

        for (int y = tile.Y; y < tile.Y + tile.Height; y++)
        {
            token.ThrowIfCancellationRequested();
            for (int x = tile.X; x < tile.X + tile.Width; x++)
            {
                Vector4 accumulated = Vector4.Zero;
                for (int sy = 0; sy < ssaa; sy++)
                for (int sx = 0; sx < ssaa; sx++)
                {
                    double sampleX = x + (sx + 0.5) / ssaa;
                    double sampleY = y + (sy + 0.5) / ssaa;
                    double real = centerX + (sampleX / frame.Width - 0.5) * viewWidth;
                    double imaginary = centerY + (0.5 - sampleY / frame.Height) * viewHeight;
                    StudioOrbitSample sample;
                    try
                    {
                        sample = kernel(real, imaginary, parameters.Reals, parameters.Integers,
                            parameters.Complexes, parameters.Booleans);
                    }
                    catch (ArithmeticException)
                    {
                        sample = StudioOrbitSample.Invalid(0);
                    }
                    accumulated += StudioColoring.Colorize(
                        sample,
                        maxIterations,
                        layer.PaletteFrequency,
                        layer.PalettePhase);
                }
                frame[x, y] = accumulated * (float)invSamples;
            }
        }
    }

    private static void RenderDecimalTile(
        StudioLayerSnapshot layer,
        StudioDecimalKernel kernel,
        StudioDecimalParameterSet parameters,
        int maxIterations,
        StudioLayerFrame frame,
        StudioTile tile,
        int ssaa,
        CancellationToken token)
    {
        decimal viewWidth = 3m / Math.Max(layer.Zoom, 0.0000000000000000000000000001m);
        decimal viewHeight = viewWidth * frame.Height / frame.Width;
        decimal invSamples = 1m / (ssaa * ssaa);

        for (int y = tile.Y; y < tile.Y + tile.Height; y++)
        {
            token.ThrowIfCancellationRequested();
            for (int x = tile.X; x < tile.X + tile.Width; x++)
            {
                Vector4 accumulated = Vector4.Zero;
                for (int sy = 0; sy < ssaa; sy++)
                for (int sx = 0; sx < ssaa; sx++)
                {
                    decimal sampleX = x + ((decimal)sx + 0.5m) / ssaa;
                    decimal sampleY = y + ((decimal)sy + 0.5m) / ssaa;
                    decimal real = layer.CenterX + (sampleX / frame.Width - 0.5m) * viewWidth;
                    decimal imaginary = layer.CenterY + (0.5m - sampleY / frame.Height) * viewHeight;
                    StudioOrbitSample sample;
                    try
                    {
                        sample = kernel(real, imaginary, parameters.Reals, parameters.Integers,
                            parameters.Complexes, parameters.Booleans);
                    }
                    catch (ArithmeticException)
                    {
                        sample = StudioOrbitSample.Invalid(0);
                    }
                    accumulated += StudioColoring.Colorize(
                        sample,
                        maxIterations,
                        layer.PaletteFrequency,
                        layer.PalettePhase);
                }
                frame[x, y] = accumulated * (float)invSamples;
            }
        }
    }
}

public static class StudioColoring
{
    public static Vector4 Colorize(
        StudioOrbitSample sample,
        int maxIterations,
        double frequency,
        double phase)
    {
        if (!sample.IsValid)
            return new Vector4(2.5f, 0.02f, 1.5f, 1);
        if (!sample.Escaped || sample.Iterations >= maxIterations)
            return new Vector4(0.003f, 0.006f, 0.012f, 1);

        double cycle = sample.SmoothIteration * 0.018 * frequency + phase;
        double hue = cycle - Math.Floor(cycle);
        double value = 0.55 + 0.45 * Math.Sin((cycle + 0.15) * Math.PI);
        Vector3 srgb = HsvToRgb((float)hue, 0.82f, (float)Math.Clamp(value, 0.08, 1.2));
        Vector3 linear = new(
            SrgbToLinear(srgb.X),
            SrgbToLinear(srgb.Y),
            SrgbToLinear(srgb.Z));
        return new Vector4(linear * 1.35f, 1);
    }

    private static Vector3 HsvToRgb(float hue, float saturation, float value)
    {
        float h = (hue - MathF.Floor(hue)) * 6;
        int sector = (int)MathF.Floor(h);
        float fraction = h - sector;
        float p = value * (1 - saturation);
        float q = value * (1 - saturation * fraction);
        float t = value * (1 - saturation * (1 - fraction));
        return sector switch
        {
            0 => new Vector3(value, t, p),
            1 => new Vector3(q, value, p),
            2 => new Vector3(p, value, t),
            3 => new Vector3(p, q, value),
            4 => new Vector3(t, p, value),
            _ => new Vector3(value, p, q)
        };
    }

    private static float SrgbToLinear(float value) =>
        value <= 0.04045f
            ? value / 12.92f
            : MathF.Pow((value + 0.055f) / 1.055f, 2.4f);
}
