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
        public void RenderTile(byte[] buffer, int stride, int bytesPerPixel, TileInfo tile, int canvasWidth, int canvasHeight)
        {
            decimal half_width_pixels = canvasWidth / 2.0m;
            decimal half_height_pixels = canvasHeight / 2.0m;
            decimal units_per_pixel = this.Scale / canvasWidth; // Единый коэффициент

            for (int y = 0; y < tile.Bounds.Height; y++)
            {
                int canvasY = tile.Bounds.Y + y;
                if (canvasY >= canvasHeight) continue;

                for (int x = 0; x < tile.Bounds.Width; x++)
                {
                    int canvasX = tile.Bounds.X + x;
                    if (canvasX >= canvasWidth) continue;

                    decimal re = this.CenterX + (canvasX - half_width_pixels) * units_per_pixel;
                    decimal im = this.CenterY - (canvasY - half_height_pixels) * units_per_pixel; // Тот же units_per_pixel

                    int iter = GetIterationsForPoint(re, im);
                    Color pixelColor = Palette(iter, MaxIterations, MaxColorIterations);

                    int bufferIndex = canvasY * stride + canvasX * bytesPerPixel;
                    if (bufferIndex + bytesPerPixel - 1 < buffer.Length)
                    {
                        buffer[bufferIndex] = pixelColor.B;
                        buffer[bufferIndex + 1] = pixelColor.G;
                        buffer[bufferIndex + 2] = pixelColor.R;
                    }
                }
            }
        }

        /// <summary>
        /// Отрисовывает ОДНУ плитку в ее собственный байтовый массив.
        /// </summary>
        /// <returns>Массив байт с данными пикселей плитки.</returns>
        public byte[] RenderSingleTile(TileInfo tile, int canvasWidth, int canvasHeight, out int bytesPerPixel)
        {
            // Определяем формат и выделяем память под буфер плитки
            bytesPerPixel = 3; // Для 24bppRgb
            byte[] buffer = new byte[tile.Bounds.Width * tile.Bounds.Height * bytesPerPixel];

            decimal half_width_pixels = canvasWidth / 2.0m;
            decimal half_height_pixels = canvasHeight / 2.0m;
            decimal units_per_pixel = this.Scale / canvasWidth;

            for (int y = 0; y < tile.Bounds.Height; y++)
            {
                int canvasY = tile.Bounds.Y + y;
                // Пропускаем пиксели, которые выходят за пределы холста (на случай неровных размеров)
                if (canvasY >= canvasHeight) continue;

                for (int x = 0; x < tile.Bounds.Width; x++)
                {
                    int canvasX = tile.Bounds.X + x;
                    if (canvasX >= canvasWidth) continue;

                    // Преобразуем пиксельные координаты в комплексные
                    decimal re = this.CenterX + (canvasX - half_width_pixels) * units_per_pixel;
                    decimal im = this.CenterY - (canvasY - half_height_pixels) * units_per_pixel;

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
            return buffer;
        }


        // --- Изменения для RenderToBitmap ---
        public Bitmap RenderToBitmap(int renderWidth, int renderHeight, int numThreads, Action<int> reportProgressCallback)
        {
            if (renderWidth <= 0 || renderHeight <= 0) return new Bitmap(1, 1);

            Bitmap bmp = new Bitmap(renderWidth, renderHeight, PixelFormat.Format24bppRgb);
            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, renderWidth, renderHeight), ImageLockMode.WriteOnly, bmp.PixelFormat);
            int stride = bmpData.Stride;
            IntPtr scan0 = bmpData.Scan0;
            byte[] buffer = new byte[Math.Abs(stride) * renderHeight];

            ParallelOptions po = new ParallelOptions { MaxDegreeOfParallelism = numThreads };
            long done = 0;

            decimal half_width_pixels = renderWidth / 2.0m;
            decimal half_height_pixels = renderHeight / 2.0m;
            decimal units_per_pixel = this.Scale / renderWidth; // Единый коэффициент

            Parallel.For(0, renderHeight, po, y =>
            {
                int rowOffset = y * stride;
                for (int x = 0; x < renderWidth; x++)
                {
                    decimal re = this.CenterX + (x - half_width_pixels) * units_per_pixel;
                    decimal im = this.CenterY - (y - half_height_pixels) * units_per_pixel; // Тот же units_per_pixel

                    int iter_val = GetIterationsForPoint(re, im);
                    Color pixelColor = Palette(iter_val, MaxIterations, MaxColorIterations);

                    int index = rowOffset + x * 3; // Предполагаем 3 байта на пиксель (24bpp)
                    if (index + 2 < buffer.Length) // Проверка границ
                    {
                        buffer[index] = pixelColor.B;
                        buffer[index + 1] = pixelColor.G;
                        buffer[index + 2] = pixelColor.R;
                    }
                }

                long currentDone = System.Threading.Interlocked.Increment(ref done);
                if (renderHeight > 0)
                {
                    reportProgressCallback((int)(100.0 * currentDone / renderHeight));
                }
            });

            Marshal.Copy(buffer, 0, scan0, buffer.Length);
            bmp.UnlockBits(bmpData);

            return bmp;
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