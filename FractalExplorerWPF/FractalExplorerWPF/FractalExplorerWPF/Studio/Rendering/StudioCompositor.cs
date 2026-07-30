using System.Numerics;
using FractalExplorerWPF.Studio.Models;

namespace FractalExplorerWPF.Studio.Rendering;

public static class StudioCompositor
{
    public static byte[] ComposeTile(
        IReadOnlyList<(StudioLayerSnapshot Layer, StudioLayerFrame Frame)> layers,
        StudioTile tile,
        double exposure = 1)
    {
        int stride = checked(tile.Width * 4);
        var output = new byte[checked(stride * tile.Height)];
        for (int localY = 0; localY < tile.Height; localY++)
        for (int localX = 0; localX < tile.Width; localX++)
        {
            int x = tile.X + localX;
            int y = tile.Y + localY;
            Vector4 composite = Vector4.Zero;
            foreach ((StudioLayerSnapshot layer, StudioLayerFrame frame) in layers)
            {
                if (!layer.IsVisible || frame.Width <= x || frame.Height <= y)
                    continue;
                Vector4 source = frame[x, y];
                source.W *= (float)layer.Opacity;
                source.X *= (float)layer.Opacity;
                source.Y *= (float)layer.Opacity;
                source.Z *= (float)layer.Opacity;
                composite = Composite(composite, source, layer.BlendMode);
            }

            int offset = localY * stride + localX * 4;
            Vector3 color = composite.W > 0
                ? new Vector3(composite.X, composite.Y, composite.Z) / composite.W
                : Vector3.Zero;
            color *= (float)Math.Pow(2, exposure - 1);
            color = Aces(color);
            output[offset] = ToByte(LinearToSrgb(color.Z));
            output[offset + 1] = ToByte(LinearToSrgb(color.Y));
            output[offset + 2] = ToByte(LinearToSrgb(color.X));
            output[offset + 3] = ToByte(Math.Clamp(composite.W, 0, 1));
        }
        return output;
    }

    private static Vector4 Composite(Vector4 destination, Vector4 source, StudioBlendMode mode)
    {
        float sourceAlpha = Math.Clamp(source.W, 0, 1);
        float destinationAlpha = Math.Clamp(destination.W, 0, 1);
        if (sourceAlpha <= 0)
            return destination;
        Vector3 sourceColor = sourceAlpha > 0
            ? new Vector3(source.X, source.Y, source.Z) / sourceAlpha
            : Vector3.Zero;
        Vector3 destinationColor = destinationAlpha > 0
            ? new Vector3(destination.X, destination.Y, destination.Z) / destinationAlpha
            : Vector3.Zero;
        Vector3 blended = Blend(destinationColor, sourceColor, mode);
        Vector3 result =
            (1 - sourceAlpha) * new Vector3(destination.X, destination.Y, destination.Z) +
            (1 - destinationAlpha) * new Vector3(source.X, source.Y, source.Z) +
            sourceAlpha * destinationAlpha * blended;
        float alpha = sourceAlpha + destinationAlpha * (1 - sourceAlpha);
        return new Vector4(result, alpha);
    }

    private static Vector3 Blend(Vector3 destination, Vector3 source, StudioBlendMode mode) => mode switch
    {
        StudioBlendMode.Normal => source,
        StudioBlendMode.Add => destination + source,
        StudioBlendMode.Subtract => Vector3.Max(Vector3.Zero, destination - source),
        StudioBlendMode.Multiply => destination * source,
        StudioBlendMode.Screen => Vector3.One - (Vector3.One - destination) * (Vector3.One - source),
        StudioBlendMode.Overlay => PerChannel(destination, source, Overlay),
        StudioBlendMode.SoftLight => PerChannel(destination, source, SoftLight),
        StudioBlendMode.HardLight => PerChannel(source, destination, Overlay),
        StudioBlendMode.Darken => Vector3.Min(destination, source),
        StudioBlendMode.Lighten => Vector3.Max(destination, source),
        StudioBlendMode.ColorDodge => PerChannel(destination, source,
            (d, s) => s >= 1 ? 1 : d / Math.Max(1e-6f, 1 - s)),
        StudioBlendMode.ColorBurn => PerChannel(destination, source,
            (d, s) => s <= 0 ? 0 : 1 - (1 - d) / Math.Max(1e-6f, s)),
        StudioBlendMode.Difference => Vector3.Abs(destination - source),
        StudioBlendMode.Exclusion => destination + source - 2 * destination * source,
        StudioBlendMode.Hue => SetLuminosity(SetSaturation(source, Saturation(destination)), Luminosity(destination)),
        StudioBlendMode.Saturation =>
            SetLuminosity(SetSaturation(destination, Saturation(source)), Luminosity(destination)),
        StudioBlendMode.Color => SetLuminosity(source, Luminosity(destination)),
        StudioBlendMode.Luminosity => SetLuminosity(destination, Luminosity(source)),
        _ => source
    };

    private static float Overlay(float destination, float source) =>
        destination <= 0.5f
            ? 2 * destination * source
            : 1 - 2 * (1 - destination) * (1 - source);

    private static float SoftLight(float destination, float source)
    {
        if (source <= 0.5f)
            return destination - (1 - 2 * source) * destination * (1 - destination);
        float d = destination <= 0.25f
            ? ((16 * destination - 12) * destination + 4) * destination
            : MathF.Sqrt(Math.Max(0, destination));
        return destination + (2 * source - 1) * (d - destination);
    }

    private static Vector3 PerChannel(
        Vector3 destination,
        Vector3 source,
        Func<float, float, float> operation) =>
        new(operation(destination.X, source.X),
            operation(destination.Y, source.Y),
            operation(destination.Z, source.Z));

    private static float Luminosity(Vector3 color) => 0.3f * color.X + 0.59f * color.Y + 0.11f * color.Z;
    private static float Saturation(Vector3 color) =>
        Math.Max(color.X, Math.Max(color.Y, color.Z)) - Math.Min(color.X, Math.Min(color.Y, color.Z));

    private static Vector3 SetLuminosity(Vector3 color, float luminosity)
    {
        float delta = luminosity - Luminosity(color);
        return ClipColor(color + new Vector3(delta));
    }

    private static Vector3 SetSaturation(Vector3 color, float saturation)
    {
        float[] values = [color.X, color.Y, color.Z];
        int min = Array.IndexOf(values, values.Min());
        int max = Array.IndexOf(values, values.Max());
        int middle = 3 - min - max;
        if (values[max] > values[min])
        {
            values[middle] = (values[middle] - values[min]) * saturation /
                             (values[max] - values[min]);
            values[max] = saturation;
        }
        else
        {
            values[middle] = values[max] = 0;
        }
        values[min] = 0;
        return new Vector3(values[0], values[1], values[2]);
    }

    private static Vector3 ClipColor(Vector3 color)
    {
        float luminosity = Luminosity(color);
        float minimum = Math.Min(color.X, Math.Min(color.Y, color.Z));
        float maximum = Math.Max(color.X, Math.Max(color.Y, color.Z));
        if (minimum < 0)
            color = new Vector3(luminosity) +
                    (color - new Vector3(luminosity)) * luminosity / Math.Max(1e-6f, luminosity - minimum);
        if (maximum > 1)
            color = new Vector3(luminosity) +
                    (color - new Vector3(luminosity)) * (1 - luminosity) /
                    Math.Max(1e-6f, maximum - luminosity);
        return color;
    }

    private static Vector3 Aces(Vector3 value)
    {
        value = Vector3.Max(Vector3.Zero, value);
        return Vector3.Clamp(
            value * (2.51f * value + new Vector3(0.03f)) /
            (value * (2.43f * value + new Vector3(0.59f)) + new Vector3(0.14f)),
            Vector3.Zero,
            Vector3.One);
    }

    private static float LinearToSrgb(float value) =>
        value <= 0.0031308f
            ? value * 12.92f
            : 1.055f * MathF.Pow(value, 1f / 2.4f) - 0.055f;

    private static byte ToByte(float value) =>
        (byte)Math.Clamp((int)Math.Round(Math.Clamp(value, 0, 1) * 255), 0, 255);
}
