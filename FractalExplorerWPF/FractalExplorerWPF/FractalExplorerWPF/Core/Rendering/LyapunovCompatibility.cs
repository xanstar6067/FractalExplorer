using DrawingColor = System.Drawing.Color;

namespace FractalExplorer.Utilities.SaveIO.ColorPalettes
{
    public sealed class LyapunovColorPalette
    {
        public string Name { get; set; } = "Классическая Ляпунова";
        public FractalExplorer.Utilities.Coloring.LyapunovColoringMode Mode { get; set; }
        public List<DrawingColor> Colors { get; set; } = [];
        public double ExponentRange { get; set; } = 2;
        public double ZeroBandWidth { get; set; } = .05;
    }

    public static class LyapunovPaletteManager
    {
        public static LyapunovColorPalette CreateDefaultBuiltInPalette() => new()
        {
            Mode = FractalExplorer.Utilities.Coloring.LyapunovColoringMode.LegacyBuiltIn,
            Colors = [DrawingColor.FromArgb(20, 30, 80), DrawingColor.FromArgb(90, 200, 255), DrawingColor.FromArgb(120, 140, 70), DrawingColor.FromArgb(190, 100, 45), DrawingColor.FromArgb(255, 50, 30)]
        };
    }
}

namespace FractalExplorer.Utilities.Coloring
{
    using FractalExplorer.Utilities.SaveIO.ColorPalettes;

    public enum LyapunovColoringMode { Diverging, Absolute, ZeroBandHighlight, HistogramEqualized, LegacyBuiltIn }

    public sealed class LyapunovColoringContext
    {
        public double MinExponent { get; init; }
        public double MaxExponent { get; init; }
        public double[] Cdf { get; init; } = [];
        public double MapByHistogram(double value)
        {
            if (Cdf.Length == 0 || !double.IsFinite(value) || MaxExponent <= MinExponent) return 0;
            int index = (int)Math.Round(Math.Clamp((value - MinExponent) / (MaxExponent - MinExponent), 0, 1) * (Cdf.Length - 1));
            return Cdf[Math.Clamp(index, 0, Cdf.Length - 1)];
        }
    }

    public static class LyapunovColoring
    {
        public static DrawingColor MapExponent(double exponent, LyapunovColorPalette palette, LyapunovColoringContext? context = null)
        {
            if (!double.IsFinite(exponent)) return DrawingColor.Transparent;
            if (palette.Mode == LyapunovColoringMode.LegacyBuiltIn) return Legacy(exponent);
            List<DrawingColor> colors = EnsureColors(palette.Colors);
            double range = Math.Max(1e-9, Math.Abs(palette.ExponentRange));
            if (palette.Mode == LyapunovColoringMode.Diverging) return MapDiverging(exponent, colors, range);
            if (palette.Mode == LyapunovColoringMode.ZeroBandHighlight) return MapZeroBand(exponent, colors, range, palette.ZeroBandWidth);
            double t = palette.Mode switch
            {
                LyapunovColoringMode.Absolute => Math.Clamp(Math.Abs(exponent) / range, 0, 1),
                LyapunovColoringMode.HistogramEqualized => context?.MapByHistogram(exponent) ?? Math.Clamp((exponent + range) / (2 * range), 0, 1),
                _ => Math.Clamp((exponent + range) / (2 * range), 0, 1)
            };
            return Interpolate(colors, t);
        }

        private static DrawingColor MapDiverging(double exponent, List<DrawingColor> colors, double range)
        {
            int mid=colors.Count/2;
            return exponent<0?Interpolate(colors.Take(mid+1).ToList(),Math.Clamp(-exponent/range,0,1)):Interpolate(colors.Skip(mid).ToList(),Math.Clamp(exponent/range,0,1));
        }

        private static DrawingColor MapZeroBand(double exponent,List<DrawingColor> colors,double range,double band)
        {
            int mid=colors.Count/2;band=Math.Max(1e-9,Math.Abs(band));if(Math.Abs(exponent)<=band)return colors[mid];
            return exponent<0?Interpolate(colors.Take(mid+1).ToList(),Math.Clamp((Math.Abs(exponent)-band)/Math.Max(1e-9,range-band),0,1)):Interpolate(colors.Skip(mid).ToList(),Math.Clamp((exponent-band)/Math.Max(1e-9,range-band),0,1));
        }

        private static DrawingColor Legacy(double e)
        {
            if (e < 0)
            {
                double t = Math.Clamp(-e / 2, 0, 1);
                return DrawingColor.FromArgb((int)(20 + 70 * t), (int)(30 + 170 * t), (int)(80 + 175 * t));
            }
            double p = Math.Clamp(e / 2, 0, 1);
            return DrawingColor.FromArgb((int)(120 + 135 * p), (int)(50 + 90 * (1 - p)), (int)(30 + 40 * (1 - p)));
        }

        private static List<DrawingColor> EnsureColors(List<DrawingColor>? colors)
        {
            if (colors is not { Count: > 1 }) return [DrawingColor.Black, DrawingColor.Gray, DrawingColor.White];
            var result = colors.ToList();
            if (result.Count % 2 == 0) result.Insert(result.Count / 2, result[result.Count / 2]);
            return result;
        }
        private static DrawingColor Interpolate(List<DrawingColor> colors, double t)
        {
            t = Math.Clamp(t, 0, 1); double scaled = t * (colors.Count - 1); int a = (int)scaled; int b = Math.Min(colors.Count - 1, a + 1); double f = scaled - a;
            return DrawingColor.FromArgb(
                (int)Math.Round(colors[a].A + (colors[b].A - colors[a].A) * f),
                (int)Math.Round(colors[a].R + (colors[b].R - colors[a].R) * f),
                (int)Math.Round(colors[a].G + (colors[b].G - colors[a].G) * f),
                (int)Math.Round(colors[a].B + (colors[b].B - colors[a].B) * f));
        }
    }
}
