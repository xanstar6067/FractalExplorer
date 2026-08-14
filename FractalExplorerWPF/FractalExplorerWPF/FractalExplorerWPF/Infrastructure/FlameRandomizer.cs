using FractalExplorerWPF.Models;
using Color = System.Windows.Media.Color;

namespace FractalExplorerWPF.Infrastructure;

public static class FlameRandomizer
{
    public static List<FlameTransform> Create(
        FlameRandomizationSettings settings,
        Random? random = null)
    {
        ArgumentNullException.ThrowIfNull(settings);
        FlameRandomizationSettings normalized = settings.Clone().Normalize();
        if (normalized.Variations.Count == 0)
            throw new InvalidOperationException("Выберите хотя бы одну вариацию для случайной генерации.");

        random ??= Random.Shared;
        int count = random.Next(normalized.MinimumTransforms, normalized.MaximumTransforms + 1);
        double[] weights = new double[count];
        double totalWeight = 0;
        for (int i = 0; i < count; i++)
            totalWeight += weights[i] = .25 + random.NextDouble() * random.NextDouble() * 2.5;

        double baseHue = random.NextDouble() * 360;
        var result = new List<FlameTransform>(count);
        for (int i = 0; i < count; i++)
        {
            FlameVariation variation = normalized.Variations[random.Next(normalized.Variations.Count)];
            result.Add(CreateTransform(
                random,
                variation,
                weights[i] / totalWeight,
                baseHue + i * (360d / count) + Range(random, -28, 28)));
        }

        return result;
    }

    private static FlameTransform CreateTransform(
        Random random,
        FlameVariation variation,
        double weight,
        double hue)
    {
        double angle = Range(random, -Math.PI, Math.PI);
        double scaleX = Range(random, .22, .82);
        double scaleY = Range(random, .22, .82);
        double shear = Range(random, -.28, .28);
        double cosine = Math.Cos(angle);
        double sine = Math.Sin(angle);
        double translationRadius = variation is FlameVariation.Spherical or FlameVariation.Spiral
            ? Range(random, .04, .48)
            : Range(random, .12, .92);
        double translationAngle = Range(random, -Math.PI, Math.PI);

        return new FlameTransform
        {
            Weight = weight,
            A = cosine * scaleX + sine * shear,
            B = -sine * scaleY,
            C = Math.Cos(translationAngle) * translationRadius,
            D = sine * scaleX,
            E = cosine * scaleY + cosine * shear,
            F = Math.Sin(translationAngle) * translationRadius,
            Variation = variation,
            Color = Hsv(hue, Range(random, .62, .95), Range(random, .72, 1))
        };
    }

    private static double Range(Random random, double minimum, double maximum) =>
        minimum + random.NextDouble() * (maximum - minimum);

    private static Color Hsv(double hue, double saturation, double value)
    {
        hue = ((hue % 360) + 360) % 360;
        double chroma = value * saturation;
        double secondary = chroma * (1 - Math.Abs(hue / 60 % 2 - 1));
        double match = value - chroma;
        (double red, double green, double blue) = hue switch
        {
            < 60 => (chroma, secondary, 0d),
            < 120 => (secondary, chroma, 0d),
            < 180 => (0d, chroma, secondary),
            < 240 => (0d, secondary, chroma),
            < 300 => (secondary, 0d, chroma),
            _ => (chroma, 0d, secondary)
        };
        return Color.FromRgb(
            (byte)Math.Round((red + match) * 255),
            (byte)Math.Round((green + match) * 255),
            (byte)Math.Round((blue + match) * 255));
    }
}
