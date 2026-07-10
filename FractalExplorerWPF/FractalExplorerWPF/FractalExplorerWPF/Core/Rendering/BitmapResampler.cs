using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FractalExplorerWPF.Core.Rendering;

public static class BitmapResampler
{
    public static BitmapSource ResizeBicubic(BitmapSource source, int width, int height)
    {
        if (source.PixelWidth == width && source.PixelHeight == height) return source;
        var visual = new DrawingVisual();
        RenderOptions.SetBitmapScalingMode(visual, BitmapScalingMode.HighQuality);
        using (DrawingContext context = visual.RenderOpen())
            context.DrawImage(source, new System.Windows.Rect(0, 0, width, height));
        var result = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        result.Render(visual);
        result.Freeze();
        return result;
    }

    public static BitmapSource ResizeLanczos3(
        BitmapSource source,
        int width,
        int height,
        CancellationToken token,
        Action<int>? reportProgress = null)
    {
        if (source.PixelWidth == width && source.PixelHeight == height) return source;

        BitmapSource bgraSource = source.Format == PixelFormats.Bgra32
            ? source
            : new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
        int sourceWidth = bgraSource.PixelWidth;
        int sourceHeight = bgraSource.PixelHeight;
        int sourceStride = checked(sourceWidth * 4);
        byte[] sourcePixels = new byte[checked(sourceStride * sourceHeight)];
        bgraSource.CopyPixels(sourcePixels, sourceStride, 0);

        Contribution[][] horizontal = BuildContributions(sourceWidth, width);
        Contribution[][] vertical = BuildContributions(sourceHeight, height);
        byte[] output = new byte[checked(width * height * 4)];
        var rowCache = new Dictionary<int, byte[]>();

        for (int targetY = 0; targetY < height; targetY++)
        {
            token.ThrowIfCancellationRequested();
            Contribution[] yContributions = vertical[targetY];
            foreach (Contribution contribution in yContributions)
            {
                if (!rowCache.ContainsKey(contribution.Index))
                    rowCache[contribution.Index] = ResizeRow(
                        sourcePixels, sourceStride, contribution.Index, width, horizontal);
            }

            int outputOffset = targetY * width * 4;
            for (int targetX = 0; targetX < width; targetX++)
            {
                double b = 0, g = 0, r = 0, a = 0;
                int pixelOffset = targetX * 4;
                foreach (Contribution contribution in yContributions)
                {
                    byte[] row = rowCache[contribution.Index];
                    double weight = contribution.Weight;
                    b += row[pixelOffset] * weight;
                    g += row[pixelOffset + 1] * weight;
                    r += row[pixelOffset + 2] * weight;
                    a += row[pixelOffset + 3] * weight;
                }
                output[outputOffset + pixelOffset] = ToByte(b);
                output[outputOffset + pixelOffset + 1] = ToByte(g);
                output[outputOffset + pixelOffset + 2] = ToByte(r);
                output[outputOffset + pixelOffset + 3] = ToByte(a);
            }

            int nextMinimum = targetY + 1 < height
                ? vertical[targetY + 1].Min(item => item.Index)
                : sourceHeight;
            foreach (int staleRow in rowCache.Keys.Where(index => index < nextMinimum).ToArray())
                rowCache.Remove(staleRow);
            reportProgress?.Invoke(95 + Math.Min(4, (targetY + 1) * 5 / height));
        }

        BitmapSource result = BitmapSource.Create(width, height, 96, 96,
            PixelFormats.Bgra32, null, output, width * 4);
        result.Freeze();
        return result;
    }

    private static byte[] ResizeRow(
        byte[] source,
        int sourceStride,
        int sourceY,
        int targetWidth,
        Contribution[][] contributions)
    {
        var result = new byte[targetWidth * 4];
        int sourceRowOffset = sourceY * sourceStride;
        for (int targetX = 0; targetX < targetWidth; targetX++)
        {
            double b = 0, g = 0, r = 0, a = 0;
            foreach (Contribution contribution in contributions[targetX])
            {
                int offset = sourceRowOffset + contribution.Index * 4;
                double weight = contribution.Weight;
                b += source[offset] * weight;
                g += source[offset + 1] * weight;
                r += source[offset + 2] * weight;
                a += source[offset + 3] * weight;
            }
            int targetOffset = targetX * 4;
            result[targetOffset] = ToByte(b);
            result[targetOffset + 1] = ToByte(g);
            result[targetOffset + 2] = ToByte(r);
            result[targetOffset + 3] = ToByte(a);
        }
        return result;
    }

    private static Contribution[][] BuildContributions(int sourceSize, int targetSize)
    {
        double scale = (double)targetSize / sourceSize;
        double filterScale = Math.Min(1, scale);
        double support = 3 / filterScale;
        var result = new Contribution[targetSize][];

        for (int target = 0; target < targetSize; target++)
        {
            double center = (target + 0.5) / scale - 0.5;
            int left = (int)Math.Ceiling(center - support);
            int right = (int)Math.Floor(center + support);
            var weights = new Dictionary<int, double>();
            double sum = 0;
            for (int source = left; source <= right; source++)
            {
                int clamped = Math.Clamp(source, 0, sourceSize - 1);
                double weight = Lanczos((center - source) * filterScale) * filterScale;
                if (Math.Abs(weight) < 1e-12) continue;
                weights[clamped] = weights.GetValueOrDefault(clamped) + weight;
                sum += weight;
            }
            if (Math.Abs(sum) < 1e-12)
            {
                result[target] = [new Contribution(Math.Clamp((int)Math.Round(center), 0, sourceSize - 1), 1)];
                continue;
            }
            result[target] = weights.Select(pair => new Contribution(pair.Key, pair.Value / sum)).ToArray();
        }
        return result;
    }

    private static double Lanczos(double value)
    {
        value = Math.Abs(value);
        if (value < 1e-12) return 1;
        if (value >= 3) return 0;
        double piValue = Math.PI * value;
        return Math.Sin(piValue) / piValue * Math.Sin(piValue / 3) / (piValue / 3);
    }

    private static byte ToByte(double value) => (byte)Math.Clamp((int)Math.Round(value), 0, 255);

    private readonly record struct Contribution(int Index, double Weight);
}
