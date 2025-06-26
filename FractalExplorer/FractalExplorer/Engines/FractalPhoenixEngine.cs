using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using FractalExplorer.Resources; // Для ComplexDecimal и TileInfo

namespace FractalExplorer.Engines
{
    public class PhoenixEngine
    {
        public int MaxIterations { get; set; } = 100;
        public decimal ThresholdSquared { get; set; } = 4.0m; // 2.0*2.0

        public ComplexDecimal C1 { get; set; } // P = C1.Real, Q = C1.Imaginary
        public ComplexDecimal C2 { get; set; } // Пока не используется в основной формуле

        public Func<int, int, int, Color> Palette { get; set; }
        public int MaxColorIterations { get; set; } = 1000; // Для нормализации цвета в палитре

        public decimal CenterX { get; set; } = 0.0m;
        public decimal CenterY { get; set; } = 0.0m;
        public decimal Scale { get; set; } = 4.0m; // Базовый масштаб для Феникса

        /// <summary>
        /// Вычисляет количество итераций для фрактала Феникса.
        /// z_next = z_current^2 + P + Q * z_prev
        /// </summary>
        /// <param name="z_current_start">Начальное значение z_current.</param>
        /// <param name="z_prev_start">Начальное значение z_prev.</param>
        /// <param name="c1_param">Параметр C1 (P = c1_param.Real, Q = c1_param.Imaginary).</param>
        /// <param name="c2_param_unused">Параметр C2 (пока не используется).</param>
        /// <returns>Количество итераций.</returns>
        public int CalculateIterations(ComplexDecimal z_current_start, ComplexDecimal z_prev_start, ComplexDecimal c1_param, ComplexDecimal c2_param_unused)
        {
            int iter = 0;
            ComplexDecimal z_current = z_current_start;
            ComplexDecimal z_prev = z_prev_start;

            decimal p_const = c1_param.Real;
            decimal q_const = c1_param.Imaginary;

            while (iter < MaxIterations && z_current.MagnitudeSquared <= ThresholdSquared)
            {
                try
                {
                    // ОШИБКА ИСПРАВЛЕНА: Эти вычисления могут вызвать OverflowException
                    decimal x_next = z_current.Real * z_current.Real - z_current.Imaginary * z_current.Imaginary + p_const + q_const * z_prev.Real;
                    decimal y_next = 2 * z_current.Real * z_current.Imaginary + q_const * z_prev.Imaginary;

                    z_prev = z_current;
                    z_current = new ComplexDecimal(x_next, y_next);
                    iter++;
                }
                catch (OverflowException)
                {
                    // Если произошло арифметическое переполнение, это означает, что точка
                    // гарантированно ушла в бесконечность. Мы прерываем цикл.
                    iter = MaxIterations; // Устанавливаем iter в максимум, чтобы цвет был корректным для "убежавшей" точки.
                    break;
                }
            }
            return iter;
        }

        /// <summary>
        /// Определяет количество итераций для точки на экране.
        /// </summary>
        /// <param name="re_pixel">Действительная часть точки на экране.</param>
        /// <param name="im_pixel">Мнимая часть точки на экране.</param>
        /// <returns>Количество итераций.</returns>
        protected int GetIterationsForPoint(decimal re_pixel, decimal im_pixel)
        {
            ComplexDecimal z_current_start = new ComplexDecimal(re_pixel, im_pixel);
            ComplexDecimal z_prev_start = ComplexDecimal.Zero; // z_prev(0) = 0
            return CalculateIterations(z_current_start, z_prev_start, C1, C2);
        }

        /// <summary>
        /// Отрисовывает одну плитку (тайл) в ее собственный байтовый массив.
        /// </summary>
        public byte[] RenderSingleTile(TileInfo tile, int canvasWidth, int canvasHeight, out int bytesPerPixel)
        {
            bytesPerPixel = 4; // Используем 4 байта на пиксель для поддержки формата 32bppArgb
            byte[] buffer = new byte[tile.Bounds.Width * tile.Bounds.Height * bytesPerPixel];

            decimal halfWidthPixels = canvasWidth / 2.0m;
            decimal halfHeightPixels = canvasHeight / 2.0m;
            decimal unitsPerPixel = Scale / canvasWidth;

            for (int y = 0; y < tile.Bounds.Height; y++)
            {
                int canvasY = tile.Bounds.Y + y;
                if (canvasY >= canvasHeight) continue;

                for (int x = 0; x < tile.Bounds.Width; x++)
                {
                    int canvasX = tile.Bounds.X + x;
                    if (canvasX >= canvasWidth) continue;

                    decimal re = CenterX + (canvasX - halfWidthPixels) * unitsPerPixel;
                    decimal im = CenterY - (canvasY - halfHeightPixels) * unitsPerPixel; // Y инвертирован

                    int iter = GetIterationsForPoint(re, im);
                    Color pixelColor = Palette(iter, MaxIterations, MaxColorIterations);

                    int bufferIndex = (y * tile.Bounds.Width + x) * bytesPerPixel;
                    buffer[bufferIndex] = pixelColor.B;
                    buffer[bufferIndex + 1] = pixelColor.G;
                    buffer[bufferIndex + 2] = pixelColor.R;
                    buffer[bufferIndex + 3] = 255; // Alpha
                }
            }
            return buffer;
        }

        /// <summary>
        /// Рендерит фрактал в новый объект Bitmap, используя параллельные вычисления.
        /// </summary>
        public Bitmap RenderToBitmap(int renderWidth, int renderHeight, int numThreads, Action<int> reportProgressCallback)
        {
            if (renderWidth <= 0 || renderHeight <= 0)
            {
                return new Bitmap(1, 1);
            }

            Bitmap bmp = new Bitmap(renderWidth, renderHeight, PixelFormat.Format24bppRgb);
            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, renderWidth, renderHeight), ImageLockMode.WriteOnly, bmp.PixelFormat);
            int stride = bmpData.Stride;
            nint scan0 = bmpData.Scan0; // Используем nint для Scan0
            byte[] buffer = new byte[Math.Abs(stride) * renderHeight];

            ParallelOptions po = new ParallelOptions { MaxDegreeOfParallelism = numThreads };
            long done = 0;

            decimal halfWidthPixels = renderWidth / 2.0m;
            decimal halfHeightPixels = renderHeight / 2.0m;
            decimal unitsPerPixel = Scale / renderWidth;

            Parallel.For(0, renderHeight, po, y =>
            {
                int rowOffset = y * stride;
                for (int x = 0; x < renderWidth; x++)
                {
                    decimal re = CenterX + (x - halfWidthPixels) * unitsPerPixel;
                    decimal im = CenterY - (y - halfHeightPixels) * unitsPerPixel; // Y инвертирован

                    int iterVal = GetIterationsForPoint(re, im);
                    Color pixelColor = Palette(iterVal, MaxIterations, MaxColorIterations);

                    int index = rowOffset + x * 3; // 3 байта на пиксель (24bpp)
                    if (index + 2 < buffer.Length)
                    {
                        buffer[index] = pixelColor.B;
                        buffer[index + 1] = pixelColor.G;
                        buffer[index + 2] = pixelColor.R;
                    }
                }

                long currentDone = Interlocked.Increment(ref done);
                if (renderHeight > 0)
                {
                    reportProgressCallback((int)(100.0 * currentDone / renderHeight));
                }
            });

            Marshal.Copy(buffer, 0, scan0, buffer.Length);
            bmp.UnlockBits(bmpData);

            return bmp;
        }
    }
}