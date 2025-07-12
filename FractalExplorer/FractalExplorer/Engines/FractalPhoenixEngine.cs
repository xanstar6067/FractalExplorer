using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using FractalExplorer.Resources;

namespace FractalExplorer.Engines
{
    /// <summary>
    /// Представляет движок для рендеринга фрактала Феникс.
    /// Инкапсулирует логику вычислений и отрисовки.
    /// Поддерживает как дискретное, так и непрерывное (сглаженное) окрашивание, а также SSAA.
    /// </summary>
    public class PhoenixEngine
    {
        #region Properties
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
        /// Флаг, указывающий, нужно ли использовать непрерывное (сглаженное) окрашивание.
        /// </summary>
        public bool UseSmoothColoring { get; set; } = false;

        /// <summary>
        /// Функция палитры для ДИСКРЕТНОГО окрашивания.
        /// Принимает (текущая итерация, макс. итераций, макс. итераций для цвета) и возвращает цвет.
        /// </summary>
        public Func<int, int, int, Color> Palette { get; set; }

        /// <summary>
        /// Функция палитры для НЕПРЕРЫВНОГО (сглаженного) окрашивания.
        /// Принимает (дробное значение итерации) и возвращает цвет.
        /// </summary>
        public Func<double, Color> SmoothPalette { get; set; }

        /// <summary>
        /// Максимальное количество итераций для нормализации цвета в палитре (для дискретного режима).
        /// </summary>
        public int MaxColorIterations { get; set; } = 1000;
        #endregion

        #region Core Calculation Logic
        /// <summary>
        /// Вычисляет количество итераций для фрактала Феникса.
        /// z_next = z_current^2 + P + Q * z_prev
        /// </summary>
        /// <param name="z_current">Начальное значение z_current. По ссылке, чтобы вернуть финальное значение.</param>
        /// <param name="z_prev">Начальное значение z_prev.</param>
        /// <returns>Количество итераций до выхода за порог.</returns>
        public int CalculateIterations(ref ComplexDecimal z_current, ComplexDecimal z_prev)
        {
            int iter = 0;
            decimal p_const = C1.Real;
            decimal q_const = C1.Imaginary;

            // Параметр C2 (c2_param) здесь не используется, но оставлен для возможного расширения.

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
                    // Если произошло переполнение, считаем, что точка ушла в бесконечность
                    iter = MaxIterations;
                    break;
                }
            }
            return iter;
        }

        // --- НОВЫЙ ПЕРЕГРУЖЕННЫЙ МЕТОД ДЛЯ ПОТОКОБЕЗОПАСНЫХ ВЫЗОВОВ ---
        /// <summary>
        /// Потокобезопасная версия для вычисления итераций, принимающая C1 как параметр.
        /// </summary>
        /// <returns>Количество итераций до выхода за порог.</returns>
        public int CalculateIterations(ref ComplexDecimal z_current, ComplexDecimal z_prev, ComplexDecimal c1_param)
        {
            int iter = 0;
            decimal p_const = c1_param.Real; // Использует параметр, а не свойство
            decimal q_const = c1_param.Imaginary; // Использует параметр, а не свойство

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
        /// Вычисляет "сглаженное" значение итерации.
        /// </summary>
        /// <param name="iter">Целочисленное количество итераций.</param>
        /// <param name="finalZ">Комплексное число в момент выхода за порог.</param>
        /// <returns>Дробное (сглаженное) значение итерации.</returns>
        private double CalculateSmoothValue(int iter, ComplexDecimal finalZ)
        {
            if (iter >= MaxIterations)
            {
                return iter; // Точка внутри множества.
            }

            // ВАЖНО: Эта формула математически корректна для z=z^2+c.
            // Для фрактала Феникс она является лишь аппроксимацией, но дает визуально "гладкий" результат.
            double log_zn_sq = Math.Log((double)finalZ.MagnitudeSquared);
            double nu = Math.Log(log_zn_sq / (2 * Math.Log(2))) / Math.Log(2);

            return iter + 1 - nu;
        }
        #endregion

        #region Rendering Methods

        /// <summary>
        /// Отрисовывает одну плитку в ее собственный, отдельный байтовый массив.
        /// Поддерживает как дискретное, так и сглаженное окрашивание.
        /// </summary>
        public byte[] RenderSingleTile(TileInfo tile, int canvasWidth, int canvasHeight, out int bytesPerPixel)
        {
            bytesPerPixel = 4; // BGRA
            byte[] buffer = new byte[tile.Bounds.Width * tile.Bounds.Height * bytesPerPixel];

            if (canvasWidth <= 0 || canvasHeight <= 0) return buffer;

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
                    decimal im = CenterY - (canvasY - halfHeightPixels) * unitsPerPixel;

                    Color pixelColor;
                    ComplexDecimal z_current = new ComplexDecimal(re, im);
                    ComplexDecimal z_prev = ComplexDecimal.Zero; // C2 используется как z_prev

                    if (UseSmoothColoring && SmoothPalette != null)
                    {
                        int iter = CalculateIterations(ref z_current, z_prev);
                        double smoothIter = CalculateSmoothValue(iter, z_current);
                        pixelColor = SmoothPalette(smoothIter);
                    }
                    else
                    {
                        int iter = CalculateIterations(ref z_current, z_prev);
                        pixelColor = Palette(iter, MaxIterations, MaxColorIterations);
                    }

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
        /// Отрисовывает одну плитку с использованием суперсэмплинга (SSAA).
        /// </summary>
        public byte[] RenderSingleTileSSAA(TileInfo tile, int canvasWidth, int canvasHeight, int ssaaFactor, out int bytesPerPixel)
        {
            bytesPerPixel = 4;
            if (ssaaFactor <= 1)
            {
                return RenderSingleTile(tile, canvasWidth, canvasHeight, out bytesPerPixel);
            }

            byte[] finalTileBuffer = new byte[tile.Bounds.Width * tile.Bounds.Height * bytesPerPixel];
            if (canvasWidth <= 0 || canvasHeight <= 0) return finalTileBuffer;

            int highResTileWidth = tile.Bounds.Width * ssaaFactor;
            int highResTileHeight = tile.Bounds.Height * ssaaFactor;
            Color[,] highResColorBuffer = new Color[highResTileWidth, highResTileHeight];

            long highResCanvasWidth = (long)canvasWidth * ssaaFactor;
            decimal unitsPerSubPixel = Scale / highResCanvasWidth;
            decimal highResHalfWidthPixels = highResCanvasWidth / 2.0m;
            decimal highResHalfHeightPixels = (long)canvasHeight * ssaaFactor / 2.0m;

            Parallel.For(0, highResTileHeight, y =>
            {
                for (int x = 0; x < highResTileWidth; x++)
                {
                    long globalHighResX = (long)tile.Bounds.X * ssaaFactor + x;
                    long globalHighResY = (long)tile.Bounds.Y * ssaaFactor + y;

                    decimal re = CenterX + (globalHighResX - highResHalfWidthPixels) * unitsPerSubPixel;
                    decimal im = CenterY - (globalHighResY - highResHalfHeightPixels) * unitsPerSubPixel;

                    ComplexDecimal z_current = new ComplexDecimal(re, im);
                    ComplexDecimal z_prev = ComplexDecimal.Zero;

                    if (UseSmoothColoring && SmoothPalette != null)
                    {
                        int iter = CalculateIterations(ref z_current, z_prev);
                        double smoothIter = CalculateSmoothValue(iter, z_current);
                        highResColorBuffer[x, y] = SmoothPalette(smoothIter);
                    }
                    else
                    {
                        int iter = CalculateIterations(ref z_current, z_prev);
                        highResColorBuffer[x, y] = Palette(iter, MaxIterations, MaxColorIterations);
                    }
                }
            });

            int sampleCount = ssaaFactor * ssaaFactor;
            for (int finalY = 0; finalY < tile.Bounds.Height; finalY++)
            {
                for (int finalX = 0; finalX < tile.Bounds.Width; finalX++)
                {
                    long totalR = 0, totalG = 0, totalB = 0;
                    int startSubX = finalX * ssaaFactor;
                    int startSubY = finalY * ssaaFactor;
                    for (int subY = 0; subY < ssaaFactor; subY++)
                    {
                        for (int subX = 0; subX < ssaaFactor; subX++)
                        {
                            Color pixelColor = highResColorBuffer[startSubX + subX, startSubY + subY];
                            totalR += pixelColor.R;
                            totalG += pixelColor.G;
                            totalB += pixelColor.B;
                        }
                    }
                    int bufferIndex = (finalY * tile.Bounds.Width + finalX) * bytesPerPixel;
                    finalTileBuffer[bufferIndex] = (byte)(totalB / sampleCount);
                    finalTileBuffer[bufferIndex + 1] = (byte)(totalG / sampleCount);
                    finalTileBuffer[bufferIndex + 2] = (byte)(totalR / sampleCount);
                    finalTileBuffer[bufferIndex + 3] = 255;
                }
            }
            return finalTileBuffer;
        }

        /// <summary>
        /// Рендерит фрактал в новый объект Bitmap (с поддержкой SSAA через другой метод).
        /// </summary>
        public Bitmap RenderToBitmap(int renderWidth, int renderHeight, int numThreads, Action<int> reportProgressCallback, CancellationToken cancellationToken = default)
        {
            if (renderWidth <= 0 || renderHeight <= 0) return new Bitmap(1, 1);

            Bitmap bmp = new Bitmap(renderWidth, renderHeight, PixelFormat.Format24bppRgb);
            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, renderWidth, renderHeight), ImageLockMode.WriteOnly, bmp.PixelFormat);
            byte[] buffer = new byte[Math.Abs(bmpData.Stride) * renderHeight];

            ParallelOptions po = new ParallelOptions { MaxDegreeOfParallelism = numThreads, CancellationToken = cancellationToken };
            long done = 0;
            decimal halfWidthPixels = renderWidth / 2.0m;
            decimal halfHeightPixels = renderHeight / 2.0m;
            decimal unitsPerPixel = Scale / renderWidth;

            Parallel.For(0, renderHeight, po, y =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                int rowOffset = y * bmpData.Stride;
                for (int x = 0; x < renderWidth; x++)
                {
                    decimal re = CenterX + (x - halfWidthPixels) * unitsPerPixel;
                    decimal im = CenterY - (y - halfHeightPixels) * unitsPerPixel;

                    Color pixelColor;
                    ComplexDecimal z_current = new ComplexDecimal(re, im);
                    ComplexDecimal z_prev = ComplexDecimal.Zero;

                    if (UseSmoothColoring && SmoothPalette != null)
                    {
                        int iter = CalculateIterations(ref z_current, z_prev);
                        double smoothIter = CalculateSmoothValue(iter, z_current);
                        pixelColor = SmoothPalette(smoothIter);
                    }
                    else
                    {
                        int iter = CalculateIterations(ref z_current, z_prev);
                        pixelColor = Palette(iter, MaxIterations, MaxColorIterations);
                    }

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

        /// <summary>
        /// Рендерит фрактал в новый объект Bitmap с использованием суперсэмплинга (SSAA).
        /// </summary>
        public async Task<Bitmap> RenderToBitmapSSAA(int finalWidth, int finalHeight, int numThreads, Action<int> reportProgressCallback, int ssaaFactor, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                if (finalWidth <= 0 || finalHeight <= 0) return new Bitmap(1, 1);
                if (ssaaFactor <= 1)
                {
                    return RenderToBitmap(finalWidth, finalHeight, numThreads, reportProgressCallback, cancellationToken);
                }

                int highResWidth = finalWidth * ssaaFactor;
                int highResHeight = finalHeight * ssaaFactor;

                // Рендерим временный битмап в высоком разрешении
                var highResEngine = new PhoenixEngine
                {
                    MaxIterations = this.MaxIterations,
                    ThresholdSquared = this.ThresholdSquared,
                    C1 = this.C1,
                    C2 = this.C2,
                    CenterX = this.CenterX,
                    CenterY = this.CenterY,
                    Scale = this.Scale,
                    UseSmoothColoring = this.UseSmoothColoring,
                    Palette = this.Palette,
                    SmoothPalette = this.SmoothPalette,
                    MaxColorIterations = this.MaxColorIterations
                };

                Action<int> highResProgress = p => reportProgressCallback(p / 2);
                Bitmap highResBmp = highResEngine.RenderToBitmap(highResWidth, highResHeight, numThreads, highResProgress, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                // Уменьшаем его до финального размера
                Action<int> downsampleProgress = p => reportProgressCallback(50 + p / 2);
                Bitmap finalBmp = new Bitmap(finalWidth, finalHeight, PixelFormat.Format24bppRgb);
                using (var g = Graphics.FromImage(finalBmp))
                {
                    // Имитируем отчет о прогрессе для этой быстрой операции
                    downsampleProgress(10);
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.DrawImage(highResBmp, 0, 0, finalWidth, finalHeight);
                    downsampleProgress(90);
                }
                highResBmp.Dispose();
                downsampleProgress(100);
                return finalBmp;

            }, cancellationToken);
        }
        #endregion
    }
}