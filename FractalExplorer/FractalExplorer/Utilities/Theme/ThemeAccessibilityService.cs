using System.Drawing;

namespace FractalExplorer.Utilities.Theme
{
    public static class ThemeAccessibilityService
    {
        public const double NonTextUiContrastRatio = 3.0d;
        public const double InteractiveStateMinDifferenceContrastRatio = 1.35d;

        public static Color Mix(Color source, Color target, float targetWeight)
        {
            float clampedWeight = Math.Clamp(targetWeight, 0f, 1f);
            float sourceWeight = 1f - clampedWeight;
            return Color.FromArgb(
                (int)Math.Round(source.R * sourceWeight + target.R * clampedWeight),
                (int)Math.Round(source.G * sourceWeight + target.G * clampedWeight),
                (int)Math.Round(source.B * sourceWeight + target.B * clampedWeight));
        }

        public static double Contrast(Color first, Color second)
        {
            double firstL = CalculateRelativeLuminance(first);
            double secondL = CalculateRelativeLuminance(second);
            double lighter = Math.Max(firstL, secondL);
            double darker = Math.Min(firstL, secondL);
            return (lighter + 0.05d) / (darker + 0.05d);
        }

        public static Color EnsureMinimumContrast(Color accent, Color background, double minContrast)
        {
            double contrast = Contrast(accent, background);
            if (contrast >= minContrast) return accent;

            Color target = CalculateRelativeLuminance(background) < 0.5d ? Color.White : Color.Black;
            Color adjusted = accent;
            for (int i = 0; i < 8 && contrast < minContrast; i++)
            {
                adjusted = Mix(adjusted, target, 0.55f);
                contrast = Contrast(adjusted, background);
            }

            return adjusted;
        }

        public static (Color Normal, Color Hover) GetInteractiveStateColors(ThemeDefinition theme, Color background)
        {
            Color hover = EnsureMinimumContrast(theme.AccentPrimary, background, NonTextUiContrastRatio);
            Color normal = EnsureMinimumContrast(Mix(hover, background, 0.35f), background, NonTextUiContrastRatio);

            if (Contrast(normal, hover) < InteractiveStateMinDifferenceContrastRatio)
            {
                normal = EnsureMinimumContrast(Mix(normal, background, 0.45f), background, NonTextUiContrastRatio);
            }

            return (normal, hover);
        }

        private static double CalculateRelativeLuminance(Color color)
        {
            static double Convert(byte channel)
            {
                double normalized = channel / 255d;
                return normalized <= 0.03928d ? normalized / 12.92d : Math.Pow((normalized + 0.055d) / 1.055d, 2.4d);
            }

            return (0.2126d * Convert(color.R)) + (0.7152d * Convert(color.G)) + (0.0722d * Convert(color.B));
        }
    }
}
