using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using FractalExplorer.Resources; // Для ComplexDecimal и TileInfo

namespace FractalExplorer.Engines
{
    /// <summary>
    /// Представляет движок для рендеринга фрактала Феникс.
    /// Инкапсулирует логику вычислений и отрисовки.
    /// </summary>
    public class PhoenixEngine
    {
        /// <summary>
        /// Максимальное количество итераций для каждой точки.
        /// </summary>
        public int MaxIterations { get; set; } = 100;

        /// <summary>
        /// Квадрат порога, используемый для определения, вышла ли точка за пределы множества.
        /// </summary>
        public decimal ThresholdSquared { get; set; } = 4.0m;

        /// <summary>
        /// Комплексный параметр C1. Его действительная часть (P) и мнимая (Q) используются в формуле.
        /// </summary>
        public ComplexDecimal C1 { get; set; }

        /// <summary>
        /// Комплексный параметр C2. Зарезервирован для будущих вариаций формулы.
        /// </summary>
        public ComplexDecimal C2 { get; set; }

        /// <summary>
        /// Функция палитры, преобразующая количество итераций в цвет.
        /// </summary>
        public Func<int, int, int, Color> Palette { get; set; }

        /// <summary>
        /// Максимальное количество итераций для нормализации цвета в палитре.
        /// </summary>
        public int MaxColorIterations { get; set; } = 1000;

        /// <summary>
        /// Координата X центра видимой области фрактала.
        /// </summary>
        public decimal CenterX { get; set; } = 0.0m;

        /// <summary>
        /// Координата Y центра видимой области фрактала.
        /// </summary>
        public decimal CenterY { get; set; } = 0.0m;

        /// <summary>
        /// Масштаб рендеринга. Определяет ширину видимой области в комплексных координатах.
        /// </summary>
        public decimal Scale { get; set; } = 4.0m;

        /// <summary>
        /// Вычисляет количество итераций для фрактала Феникса по формуле z_next = z_current^2 + P + Q * z_prev.
        /// </summary>
        /// <param name="z_current_start">Начальное значение z_current.</param>
        /// <param name="z_prev_start">Начальное значение z_prev.</param>
        /// <param name="c1_param">Параметр C1 (P = c1_param.Real, Q = c1_param.Imaginary).</param>
        /// <param name="c2_param_unused">Параметр C2 (не используется).</param>
        /// <returns>Количество итераций до выхода за порог.</returns>
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
                    decimal x_next = z_current.Real * z_current.Real - z_current.Imaginary * z_current.Imaginary + p_const + q_const * z_prev.Real;
                    decimal y_next = 2 * z_current.Real * z_current.Imaginary + q_const * z_prev.Imaginary;

                    z_prev = z_current;
                    z_current = new ComplexDecimal(x_next, y_next);
                    iter++;
                }
                catch (OverflowException)
                {
                    iter = MaxIterations;
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
            ComplexDecimal z_prev_start = ComplexDecimal.Zero;
            return CalculateIterations(z_current_start, z_prev_start, C1, C2);
        }

        /// <summary>
        /// Отрисовывает одну плитку (тайл) в ее собственный байтовый массив.
        /// </summary>
        /// <param name="tile">Информация о плитке для рендеринга.</param>
        /// <param name="canvasWidth">Общая ширина холста.</param>
        /// <param name="canvasHeight">Общая высота холста.</param>
        /// <param name="bytesPerPixel">Выходной параметр: количество байтов на пиксель.</param>
        /// <returns>Массив байт с данными пикселей плитки.</returns>
        public byte[] RenderSingleTile(TileInfo tile, int canvasWidth, int canvasHeight, out int bytesPerPixel)
        {
            bytesPerPixel = 4;
            byte[] buffer = new byte[tile.Bounds.Width * tile.Bounds.Height * bytesPerPixel];

            // ИСПРАВЛЕНИЕ: Защита от нулевых размеров прямо в движке.
            if (canvasWidth <= 0 || canvasHeight <= 0)
            {
                return buffer; // Возвращаем пустой (черный) буфер.
            }

            decimal halfWidthPixels = canvasWidth / 2.0m;
            decimal halfHeightPixels = canvasHeight / 2.0m;
            decimal unitsPerPixel = Scale / canvasWidth; // Эта строка теперь в безопасности.

            for (int y = 0; y < tile.Bounds.Height; y++)
            {
                int canvasY = tile.Bounds.Y + y;
                if (canvasY >= canvasHeight) continue;

                for (int x = 0; x < tile.Bounds.Width; x++)
                {
                    int canvasX = tile.Bounds.X + x;
                    if (canvasX >= canvasWidth) continue;

                    decimal re = CenterX + (canvasX - halfWidthPixels) * unitsPerPixel;
                    decimal im = CenterY - (canvasY - halfHeightPixels) * unitsPerPixel;

                    int iter = GetIterationsForPoint(re, im);
                    Color pixelColor = Palette(iter, MaxIterations, MaxColorIterations);

                    int bufferIndex = (y * tile.Bounds.Width + x) * bytesPerPixel;
                    buffer[bufferIndex] = pixelColor.B;
                    buffer[bufferIndex + 1] = pixelColor.G;
                    buffer[bufferIndex + 2] = pixelColor.R;
                    buffer[bufferIndex + 3] = 255;
                }
            }
            return buffer;
        }

        /// <summary>
        /// Рендерит фрактал в новый объект Bitmap, используя параллельные вычисления.
        /// </summary>
        /// <param name="renderWidth">Ширина генерируемого изображения.</param>
        /// <param name="renderHeight">Высота генерируемого изображения.</param>
        /// <param name="numThreads">Количество потоков для рендеринга.</param>
        /// <param name="reportProgressCallback">Callback-функция для отчета о прогрессе (0-100).</param>
        /// <returns>Объект Bitmap с отрисованным фракталом.</returns>
        public Bitmap RenderToBitmap(int renderWidth, int renderHeight, int numThreads, Action<int> reportProgressCallback)
        {
            // ИСПРАВЛЕНИЕ: Защита от нулевых размеров.
            if (renderWidth <= 0 || renderHeight <= 0)
            {
                return new Bitmap(1, 1);
            }

            Bitmap bmp = new Bitmap(renderWidth, renderHeight, PixelFormat.Format24bppRgb);
            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, renderWidth, renderHeight), ImageLockMode.WriteOnly, bmp.PixelFormat);
            byte[] buffer = new byte[Math.Abs(bmpData.Stride) * renderHeight];

            ParallelOptions po = new ParallelOptions { MaxDegreeOfParallelism = numThreads };
            long done = 0;

            decimal halfWidthPixels = renderWidth / 2.0m;
            decimal halfHeightPixels = renderHeight / 2.0m;
            decimal unitsPerPixel = Scale / renderWidth; // Эта строка теперь в безопасности.

            Parallel.For(0, renderHeight, po, y =>
            {
                int rowOffset = y * bmpData.Stride;
                for (int x = 0; x < renderWidth; x++)
                {
                    decimal re = CenterX + (x - halfWidthPixels) * unitsPerPixel;
                    decimal im = CenterY - (y - halfHeightPixels) * unitsPerPixel;

                    int iterVal = GetIterationsForPoint(re, im);
                    Color pixelColor = Palette(iterVal, MaxIterations, MaxColorIterations);

                    int index = rowOffset + x * 3;
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

            Marshal.Copy(buffer, 0, bmpData.Scan0, buffer.Length);
            bmp.UnlockBits(bmpData);

            return bmp;
        }
    }
}