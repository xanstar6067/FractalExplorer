using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FractalExplorer.Utilities
{
    /// <summary>
    /// Предоставляет статические методы для цветокоррекции.
    /// </summary>
    public static class ColorCorrection
    {
        /// <summary>
        /// Применяет гамма-коррекцию к цвету.
        /// </summary>
        /// <param name="originalColor">Исходный цвет.</param>
        /// <param name="gamma">Значение гаммы. > 1.0 делает изображение темнее, < 1.0 - светлее.</param>
        /// <returns>Цвет с примененной гамма-коррекцией.</returns>
        public static Color ApplyGamma(Color originalColor, double gamma)
        {
            // Если гамма равна 1.0, коррекция не требуется, возвращаем исходный цвет.
            if (gamma == 1.0)
            {
                return originalColor;
            }

            // Гамма-коррекция применяется только к каналам R, G, B. Альфа-канал остается без изменений.
            double gammaCorrection = 1.0 / gamma;

            byte r = (byte)(255 * Math.Pow(originalColor.R / 255.0, gammaCorrection));
            byte g = (byte)(255 * Math.Pow(originalColor.G / 255.0, gammaCorrection));
            byte b = (byte)(255 * Math.Pow(originalColor.B / 255.0, gammaCorrection));

            return Color.FromArgb(originalColor.A, r, g, b);
        }
    }
}
