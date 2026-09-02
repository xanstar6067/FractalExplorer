using perturbation_theory.Models;

namespace perturbation_theory.Core.Rendering;

// Adapted from MandelbrotFamilyRenderer: logarithmic gray, gradient interpolation,
// and the same gamma lookup. Only smooth and discrete escape coloring remain.
public sealed class PaletteSampler
{
    private readonly MandelbrotSettings _settings;
    private readonly byte[] _gamma = new byte[256];

    public PaletteSampler(MandelbrotSettings settings)
    {
        _settings = settings;
        for (int i = 0; i < _gamma.Length; i++)
            _gamma[i] = (byte)(255 * Math.Pow(i / 255.0, 1 / settings.Palette.Gamma));
    }

    public Rgb Sample(PixelSample sample)
    {
        if (!sample.Escaped) return new Rgb(0, 0, 0);
        BuiltInPalette palette = _settings.Palette;
        double iteration = _settings.Coloring == ColoringMode.Smooth ? sample.Smooth : sample.Iterations;
        iteration = Math.Max(0, iteration);
        if (palette.Grayscale)
        {
            double divisor = _settings.Coloring == ColoringMode.Smooth ? _settings.Iterations : _settings.ColorPeriod;
            double normalized = Math.Clamp(Math.Log(iteration + 1) / Math.Log(divisor + 1), 0, 1);
            byte gray = _gamma[(byte)(255 * (1 - normalized))];
            return new Rgb(gray, gray, gray);
        }

        // Repeat in both modes, so deep views do not saturate at the last palette color.
        double t = (iteration % _settings.ColorPeriod) / _settings.ColorPeriod;
        Rgb color;
        if (!palette.Gradient)
            color = palette.Colors[Math.Min((int)(t * palette.Colors.Length), palette.Colors.Length - 1)];
        else
        {
            double position = t * (palette.Colors.Length - 1);
            int left = (int)position;
            Rgb a = palette.Colors[left];
            Rgb b = palette.Colors[Math.Min(left + 1, palette.Colors.Length - 1)];
            double fraction = position - left;
            color = new Rgb(Lerp(a.R, b.R, fraction), Lerp(a.G, b.G, fraction), Lerp(a.B, b.B, fraction));
        }
        return new Rgb(_gamma[color.R], _gamma[color.G], _gamma[color.B]);
    }

    private static byte Lerp(byte a, byte b, double t) => (byte)Math.Round(a + (b - a) * t);
}
