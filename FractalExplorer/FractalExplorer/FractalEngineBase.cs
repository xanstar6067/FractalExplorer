using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace FractalDraving
{
    /// <summary>
    /// Абстрактный базовый класс для движков рендеринга фракталов.
    /// Инкапсулирует общую логику рендеринга и управления параметрами.
    /// </summary>
    public abstract class FractalEngineBase
    {
        // Параметры фрактала
        public int MaxIterations { get; set; }
        public decimal ThresholdSquared { get; set; }
        public ComplexDecimal C { get; set; } // Для множеств Жюлиа

        // Параметры вида
        public decimal CenterX { get; set; }
        public decimal CenterY { get; set; }
        public decimal Scale { get; set; } // Масштаб = BaseScale / Zoom

        // Параметры палитры
        public Func<int, int, int, Color> Palette { get; set; }
        public int MaxColorIterations { get; set; } = 1000;

        /// <summary>
        /// Главный метод вычисления. Для данной точки z0 и константы c,
        /// возвращает количество итераций до выхода за порог.
        /// </summary>
        /// <param name="z">Начальная точка итерации.</param>
        /// <param name="c">Константа для итерации.</param>
        /// <returns>Количество итераций.</returns>
        public abstract int CalculateIterations(ComplexDecimal z, ComplexDecimal c);

        /// <summary>
        /// Отрисовывает одну плитку (тайл) в предоставленный BitmapData.
        /// </summary>
        /// <param name="bmpData">Данные битмапа, куда будет производиться запись.</param>
        /// <param name="tile">Информация о плитке для отрисовки.</param>
        /// <param name="canvasWidth">Общая ширина холста.</param>
        /// <param name="canvasHeight">Общая высота холста.</param>
        public void RenderTile(BitmapData bmpData, TileInfo tile, int canvasWidth, int canvasHeight)
        {
            int stride = bmpData.Stride;
            IntPtr scan0 = bmpData.Scan0;
            int bytesPerPixel = Image.GetPixelFormatSize(bmpData.PixelFormat) / 8;
            byte[] buffer = new byte[tile.Bounds.Width * tile.Bounds.Height * bytesPerPixel];

            decimal half_width = canvasWidth / 2.0m;
            decimal half_height = canvasHeight / 2.0m;

            for (int y = 0; y < tile.Bounds.Height; y++)
            {
                int canvasY = tile.Bounds.Y + y;
                for (int x = 0; x < tile.Bounds.Width; x++)
                {
                    int canvasX = tile.Bounds.X + x;

                    // Преобразуем пиксельные координаты в комплексные
                    decimal re = CenterX + (canvasX - half_width) * Scale / canvasWidth;
                    decimal im = CenterY + (canvasY - half_height) * Scale / canvasHeight;

                    // Вызываем специфичный для фрактала метод расчета
                    int iter = GetIterationsForPoint(re, im);

                    // Получаем цвет на основе итераций
                    Color pixelColor = Palette(iter, MaxIterations, MaxColorIterations);

                    // Записываем цвет в локальный буфер тайла
                    int bufferIndex = (y * tile.Bounds.Width + x) * bytesPerPixel;
                    buffer[bufferIndex] = pixelColor.B;
                    buffer[bufferIndex + 1] = pixelColor.G;
                    buffer[bufferIndex + 2] = pixelColor.R;
                }
            }

            // Копируем локальный буфер тайла в глобальный буфер битмапа
            for (int y = 0; y < tile.Bounds.Height; y++)
            {
                IntPtr destPtr = IntPtr.Add(scan0, (tile.Bounds.Y + y) * stride + tile.Bounds.X * bytesPerPixel);
                int srcOffset = y * tile.Bounds.Width * bytesPerPixel;
                Marshal.Copy(buffer, srcOffset, destPtr, tile.Bounds.Width * bytesPerPixel);
            }
        }

        /// <summary>
        /// Определяет, какой метод расчета итераций использовать в зависимости от типа фрактала.
        /// </summary>
        protected abstract int GetIterationsForPoint(decimal re, decimal im);
    }

    #region Concrete Engines

    public class MandelbrotEngine : FractalEngineBase
    {
        protected override int GetIterationsForPoint(decimal re, decimal im)
        {
            var z0 = new ComplexDecimal(re, im);
            return CalculateIterations(ComplexDecimal.Zero, z0);
        }

        public override int CalculateIterations(ComplexDecimal z, ComplexDecimal c)
        {
            int iter = 0;
            while (iter < MaxIterations && z.MagnitudeSquared <= ThresholdSquared)
            {
                z = z * z + c;
                iter++;
            }
            return iter;
        }
    }

    public class JuliaEngine : FractalEngineBase
    {
        protected override int GetIterationsForPoint(decimal re, decimal im)
        {
            var z0 = new ComplexDecimal(re, im);
            return CalculateIterations(z0, C); // Для Жюлиа `c` - константа, а `z0` - точка на экране
        }

        public override int CalculateIterations(ComplexDecimal z, ComplexDecimal c)
        {
            int iter = 0;
            while (iter < MaxIterations && z.MagnitudeSquared <= ThresholdSquared)
            {
                z = z * z + c;
                iter++;
            }
            return iter;
        }
    }

    public class MandelbrotBurningShipEngine : FractalEngineBase
    {
        protected override int GetIterationsForPoint(decimal re, decimal im)
        {
            var z0 = new ComplexDecimal(re, im);
            return CalculateIterations(ComplexDecimal.Zero, z0);
        }

        public override int CalculateIterations(ComplexDecimal z, ComplexDecimal c)
        {
            int iter = 0;
            while (iter < MaxIterations && z.MagnitudeSquared <= ThresholdSquared)
            {
                z = new ComplexDecimal(Math.Abs(z.Real), Math.Abs(z.Imaginary));
                z = z * z + c;
                iter++;
            }
            return iter;
        }
    }

    public class JuliaBurningShipEngine : FractalEngineBase
    {
        protected override int GetIterationsForPoint(decimal re, decimal im)
        {
            var z0 = new ComplexDecimal(re, im);
            return CalculateIterations(z0, C);
        }

        public override int CalculateIterations(ComplexDecimal z, ComplexDecimal c)
        {
            int iter = 0;
            while (iter < MaxIterations && z.MagnitudeSquared <= ThresholdSquared)
            {
                z = new ComplexDecimal(Math.Abs(z.Real), Math.Abs(z.Imaginary));
                z = z * z + c;
                iter++;
            }
            return iter;
        }
    }

    #endregion
}