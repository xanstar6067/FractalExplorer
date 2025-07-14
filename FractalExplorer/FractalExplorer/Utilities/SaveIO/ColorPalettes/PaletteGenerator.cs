using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FractalExplorer.Utilities.SaveIO.ColorPalettes
{
    /// <summary>
    /// Статический класс-фабрика для создания функций окрашивания (палитр)
    /// на основе предоставленных объектов Palette.
    /// </summary>
    public static class PaletteGenerator
    {
        /// <summary>
        /// Создает функцию для НЕПРЕРЫВНОГО (сглаженного) окрашивания.
        /// </summary>
        public static Func<double, Color> CreateSmooth(Palette palette, int effectiveMaxColorIterations, int maxRenderIterations)
        {
            double gamma = palette.Gamma;
            var colors = new List<Color>(palette.Colors);
            int colorCount = colors.Count;

            if (palette.Name == "Стандартный серый")
            {
                return (smoothIter) =>
                {
                    if (smoothIter >= maxRenderIterations) return Color.Black;
                    if (smoothIter < 0) smoothIter = 0;
                    double logMax = Math.Log(maxRenderIterations + 1);
                    if (logMax <= 0) return Color.Black;
                    double tLog = Math.Log(smoothIter + 1) / logMax;
                    int gray_level = (int)(255.0 * (1.0 - tLog));
                    gray_level = Math.Max(0, Math.Min(255, gray_level));
                    Color baseColor = Color.FromArgb(gray_level, gray_level, gray_level);
                    return ColorCorrection.ApplyGamma(baseColor, gamma);
                };
            }

            if (effectiveMaxColorIterations <= 0 || colorCount == 0)
            {
                return (smoothIter) => Color.Black;
            }
            if (colorCount == 1)
            {
                return (smoothIter) => (smoothIter >= maxRenderIterations) ? Color.Black : ColorCorrection.ApplyGamma(colors[0], gamma);
            }

            return (smoothIter) =>
            {
                if (smoothIter >= maxRenderIterations) return Color.Black;
                if (smoothIter < 0) smoothIter = 0;

                double cyclicIter = smoothIter % effectiveMaxColorIterations;
                double t = cyclicIter / (double)effectiveMaxColorIterations;
                t = Math.Max(0.0, Math.Min(1.0, t));

                double scaledT = t * (colorCount - 1);
                int index1 = (int)Math.Floor(scaledT);
                int index2 = Math.Min(index1 + 1, colorCount - 1);
                double localT = scaledT - index1;

                Color baseColor = LerpColor(colors[index1], colors[index2], localT);
                return ColorCorrection.ApplyGamma(baseColor, gamma);
            };
        }

        /// <summary>
        /// Создает функцию для ДИСКРЕТНОГО окрашивания.
        /// </summary>
        public static Func<int, int, int, Color> CreateDiscrete(Palette palette)
        {
            double gamma = palette.Gamma;
            var colors = new List<Color>(palette.Colors);
            bool isGradient = palette.IsGradient;
            int colorCount = colors.Count;

            if (palette.Name == "Стандартный серый")
            {
                return (iter, maxIter, maxColorIter) =>
                {
                    if (iter >= maxIter) return Color.Black;
                    double logMax = Math.Log(maxColorIter + 1);
                    if (logMax <= 0) return Color.Black;
                    double tLog = Math.Log(Math.Min(iter, maxColorIter) + 1) / logMax;
                    int cVal = (int)(255.0 * (1 - tLog));
                    return ColorCorrection.ApplyGamma(Color.FromArgb(cVal, cVal, cVal), gamma);
                };
            }

            if (colorCount == 0) return (i, m, mc) => Color.Black;
            if (colorCount == 1) return (iter, max, clrMax) => ColorCorrection.ApplyGamma((iter >= max) ? Color.Black : colors[0], gamma);

            return (iter, maxIter, maxColorIter) =>
            {
                if (iter >= maxIter) return Color.Black;
                double normalizedIter = maxColorIter > 0 ? (double)Math.Min(iter, maxColorIter) / maxColorIter : 0;
                Color baseColor;
                if (isGradient)
                {
                    double scaledT = normalizedIter * (colorCount - 1);
                    int index1 = (int)Math.Floor(scaledT);
                    int index2 = Math.Min(index1 + 1, colorCount - 1);
                    double localT = scaledT - index1;
                    baseColor = LerpColor(colors[index1], colors[index2], localT);
                }
                else
                {
                    int colorIndex = (int)(normalizedIter * colorCount);
                    if (colorIndex >= colorCount) colorIndex = colorCount - 1;
                    baseColor = colors[colorIndex];
                }
                return ColorCorrection.ApplyGamma(baseColor, gamma);
            };
        }

        /// <summary>
        /// Выполняет линейную интерполяцию между двумя цветами.
        /// </summary>
        private static Color LerpColor(Color a, Color b, double t)
        {
            t = Math.Max(0, Math.Min(1, t));
            return Color.FromArgb(
                (int)(a.A + (b.A - a.A) * t),
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t));
        }
    }

    
}
