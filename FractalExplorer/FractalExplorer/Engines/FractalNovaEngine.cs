using FractalExplorer.Resources;
using FractalExplorer.Utilities;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Numerics; // Для Complex.Pow в double-версии
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace FractalExplorer.Engines
{
    /// <summary>
    /// Представляет движок для рендеринга фрактала Nova.
    /// Инкапсулирует логику вычислений, поддерживает адаптивную точность (double/decimal),
    /// сглаженное окрашивание и SSAA.
    /// </summary>
    public class FractalNovaEngine
    {
        #region Constants
        /// <summary>
        /// Порог масштабирования для переключения на высокоточные вычисления (decimal).
        /// При значениях Scale МЕНЬШЕ этого порога будет использоваться decimal.
        /// </summary>
        private const decimal SCALE_THRESHOLD_FOR_DECIMAL = 4.0m / 2000000000.0m;
        #endregion

        #region Properties
        // --- Стандартные параметры рендеринга ---
        public int MaxIterations { get; set; } = 100;
        public decimal ThresholdSquared { get; set; } = 100.0m; // Для Nova порог должен быть значительно выше
        public decimal CenterX { get; set; } = 0.0m;
        public decimal CenterY { get; set; } = 0.0m;
        public decimal Scale { get; set; } = 4.0m;
        public bool UseSmoothColoring { get; set; } = false;

        // --- Уникальные параметры Nova ---
        /// <summary>
        /// Комплексная степень 'p' в формуле.
        /// </summary>
        public ComplexDecimal P { get; set; } = new ComplexDecimal(3.0m, 0.0m);
        /// <summary>
        /// Начальное значение Z₀ для итераций.
        /// </summary>
        public ComplexDecimal Z0 { get; set; } = new ComplexDecimal(1.0m, 0.0m);
        /// <summary>
        /// Параметр релаксации 'm'.
        /// </summary>
        public decimal M { get; set; } = 1.0m;

        // --- Параметры палитры ---
        public Func<int, int, int, Color> Palette { get; set; }
        public Func<double, Color> SmoothPalette { get; set; }
        public int MaxColorIterations { get; set; } = 1000;
        #endregion

        #region Core Calculation Logic

        /// <summary>
        /// Вычисляет количество итераций для фрактала Nova с использованием высокой точности (decimal).
        /// Формула: z_next = z - m * (z^p - 1) / (p * z^(p-1)) + c
        /// </summary>
        private int CalculateIterations(ref ComplexDecimal z, ComplexDecimal c, ComplexDecimal p, decimal m)
        {
            int iter = 0;
            while (iter < MaxIterations && z.MagnitudeSquared <= ThresholdSquared)
            {
                try
                {
                    // Проверка на деление на ноль или нестабильность
                    if (z.MagnitudeSquared < 1e-28m)
                    {
                        break; // z слишком близко к нулю, производная будет нестабильна
                    }

                    ComplexDecimal z_pow_p = ComplexDecimal.Pow(z, p);
                    ComplexDecimal z_pow_p_minus_1 = ComplexDecimal.Pow(z, new ComplexDecimal(p.Real - 1, p.Imaginary));

                    ComplexDecimal numerator = m * (z_pow_p - 1);
                    ComplexDecimal denominator = p * z_pow_p_minus_1;

                    if (denominator.MagnitudeSquared < 1e-28m)
                    {
                        break; // Избегаем деления на ноль
                    }

                    z = z - numerator / denominator + c;
                    iter++;
                }
                catch (OverflowException)
                {
                    iter = MaxIterations; // Считаем, что точка ушла в бесконечность
                    break;
                }
            }
            return iter;
        }

        /// <summary>
        /// Вычисляет количество итераций для фрактала Nova с использованием стандартной точности (double).
        /// </summary>
        private int CalculateIterationsDouble(ref ComplexDouble z, ComplexDouble c, ComplexDouble p, double m)
        {
            int iter = 0;
            double thresholdSq = (double)this.ThresholdSquared;
            var one = new System.Numerics.Complex(1, 0);

            while (iter < MaxIterations && z.MagnitudeSquared <= thresholdSq)
            {
                // Проверка на деление на ноль или нестабильность
                if (z.MagnitudeSquared < 1e-12)
                {
                    break;
                }

                var z_numerics = new System.Numerics.Complex(z.Real, z.Imaginary);
                var p_numerics = new System.Numerics.Complex(p.Real, p.Imaginary);

                var z_pow_p = System.Numerics.Complex.Pow(z_numerics, p_numerics);
                var z_pow_p_minus_1 = System.Numerics.Complex.Pow(z_numerics, p_numerics - one);

                var numerator = m * (z_pow_p - one);
                var denominator = p_numerics * z_pow_p_minus_1;

                if (denominator.Magnitude < 1e-12)
                {
                    break;
                }

                var result_numerics = z_numerics - numerator / denominator + new System.Numerics.Complex(c.Real, c.Imaginary);
                z = new ComplexDouble(result_numerics.Real, result_numerics.Imaginary);
                iter++;
            }
            return iter;
        }

        /// <summary>
        /// Вычисляет "сглаженное" значение итерации (decimal).
        /// Формула адаптирована для произвольной степени p.
        /// </summary>
        private double CalculateSmoothValue(int iter, ComplexDecimal finalZ, ComplexDecimal p)
        {
            if (iter >= MaxIterations) return iter;

            double log_zn_sq = Math.Log((double)finalZ.MagnitudeSquared);
            // Используем вещественную часть P как основу логарифма
            double log_p = Math.Log(Math.Abs((double)p.Real));
            if (log_p == 0) log_p = Math.Log(2); // Запасной вариант, если Re(p)=1

            double nu = Math.Log(log_zn_sq / (2 * Math.Log(2))) / log_p;
            return iter + 1 - nu;
        }

        /// <summary>
        /// Вычисляет "сглаженное" значение итерации (double).
        /// </summary>
        private double CalculateSmoothValueDouble(int iter, ComplexDouble finalZ, ComplexDouble p)
        {
            if (iter >= MaxIterations || double.IsInfinity(finalZ.MagnitudeSquared) || double.IsNaN(finalZ.MagnitudeSquared))
            {
                return iter;
            }

            double log_zn_sq = Math.Log(finalZ.MagnitudeSquared);
            if (log_zn_sq <= 0) return iter;

            double log_p = Math.Log(Math.Abs(p.Real));
            if (log_p == 0) log_p = Math.Log(2);

            double nu = Math.Log(log_zn_sq / (2 * Math.Log(2))) / log_p;
            return iter + 1 - nu;
        }

        #endregion

        #region Rendering Methods

        /// <summary>
        /// Отрисовывает одну плитку (тайл), автоматически выбирая точность вычислений.
        /// </summary>
        public byte[] RenderSingleTile(TileInfo tile, int canvasWidth, int canvasHeight, out int bytesPerPixel)
        {
            bytesPerPixel = 4; // BGRA
            byte[] buffer = new byte[tile.Bounds.Width * tile.Bounds.Height * bytesPerPixel];
            if (canvasWidth <= 0 || canvasHeight <= 0) return buffer;

            if (Scale < SCALE_THRESHOLD_FOR_DECIMAL)
            {
                // --- ВЫСОКАЯ ТОЧНОСТЬ (DECIMAL) ---
                decimal halfWidthPixels = canvasWidth / 2.0m;
                decimal halfHeightPixels = canvasHeight / 2.0m;
                decimal unitsPerPixel = Scale / canvasWidth;

                for (int y = 0; y < tile.Bounds.Height; y++)
                {
                    for (int x = 0; x < tile.Bounds.Width; x++)
                    {
                        decimal re = CenterX + (tile.Bounds.X + x - halfWidthPixels) * unitsPerPixel;
                        decimal im = CenterY - (tile.Bounds.Y + y - halfHeightPixels) * unitsPerPixel;

                        ComplexDecimal c = new ComplexDecimal(re, im);
                        ComplexDecimal z = this.Z0; // Z всегда начинается с Z0
                        int iter = CalculateIterations(ref z, c, this.P, this.M);

                        Color pixelColor = UseSmoothColoring && SmoothPalette != null
                            ? SmoothPalette(CalculateSmoothValue(iter, z, this.P))
                            : Palette(iter, MaxIterations, MaxColorIterations);

                        int bufferIndex = (y * tile.Bounds.Width + x) * bytesPerPixel;
                        buffer[bufferIndex] = pixelColor.B;
                        buffer[bufferIndex + 1] = pixelColor.G;
                        buffer[bufferIndex + 2] = pixelColor.R;
                        buffer[bufferIndex + 3] = 255;
                    }
                }
            }
            else
            {
                // --- СТАНДАРТНАЯ ТОЧНОСТЬ (DOUBLE) ---
                double centerX_d = (double)CenterX;
                double centerY_d = (double)CenterY;
                double unitsPerPixel_d = (double)Scale / canvasWidth;
                double halfWidthPixels_d = canvasWidth / 2.0;
                double halfHeightPixels_d = canvasHeight / 2.0;
                ComplexDouble p_d = new ComplexDouble((double)P.Real, (double)P.Imaginary);
                ComplexDouble z0_d = new ComplexDouble((double)Z0.Real, (double)Z0.Imaginary);
                double m_d = (double)M;

                for (int y = 0; y < tile.Bounds.Height; y++)
                {
                    for (int x = 0; x < tile.Bounds.Width; x++)
                    {
                        double re = centerX_d + (tile.Bounds.X + x - halfWidthPixels_d) * unitsPerPixel_d;
                        double im = centerY_d - (tile.Bounds.Y + y - halfHeightPixels_d) * unitsPerPixel_d;

                        ComplexDouble c = new ComplexDouble(re, im);
                        ComplexDouble z = z0_d; // Z всегда начинается с Z0
                        int iter = CalculateIterationsDouble(ref z, c, p_d, m_d);

                        Color pixelColor = UseSmoothColoring && SmoothPalette != null
                            ? SmoothPalette(CalculateSmoothValueDouble(iter, z, p_d))
                            : Palette(iter, MaxIterations, MaxColorIterations);

                        int bufferIndex = (y * tile.Bounds.Width + x) * bytesPerPixel;
                        buffer[bufferIndex] = pixelColor.B;
                        buffer[bufferIndex + 1] = pixelColor.G;
                        buffer[bufferIndex + 2] = pixelColor.R;
                        buffer[bufferIndex + 3] = 255;
                    }
                }
            }
            return buffer;
        }

        /// <summary>
        /// Рендерит фрактал в новый объект Bitmap, используя многопоточность.
        /// </summary>
        public Bitmap RenderToBitmap(int renderWidth, int renderHeight, int numThreads, Action<int> reportProgressCallback, CancellationToken cancellationToken = default)
        {
            if (renderWidth <= 0 || renderHeight <= 0) return new Bitmap(1, 1);

            Bitmap bmp = new Bitmap(renderWidth, renderHeight, PixelFormat.Format24bppRgb);
            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, renderWidth, renderHeight), ImageLockMode.WriteOnly, bmp.PixelFormat);
            byte[] buffer = new byte[Math.Abs(bmpData.Stride) * renderHeight];
            ParallelOptions po = new ParallelOptions { MaxDegreeOfParallelism = numThreads, CancellationToken = cancellationToken };
            long done = 0;

            try
            {
                if (Scale < SCALE_THRESHOLD_FOR_DECIMAL)
                {
                    // --- ВЫСОКАЯ ТОЧНОСТЬ (DECIMAL) ---
                    decimal halfWidthPixels = renderWidth / 2.0m;
                    decimal halfHeightPixels = renderHeight / 2.0m;
                    decimal unitsPerPixel = Scale / renderWidth;

                    Parallel.For(0, renderHeight, po, y =>
                    {
                        int rowOffset = y * bmpData.Stride;
                        for (int x = 0; x < renderWidth; x++)
                        {
                            decimal re = CenterX + (x - halfWidthPixels) * unitsPerPixel;
                            decimal im = CenterY - (y - halfHeightPixels) * unitsPerPixel;

                            ComplexDecimal c = new ComplexDecimal(re, im);
                            ComplexDecimal z = this.Z0;
                            int iter = CalculateIterations(ref z, c, this.P, this.M);

                            Color pixelColor = UseSmoothColoring && SmoothPalette != null
                                ? SmoothPalette(CalculateSmoothValue(iter, z, this.P))
                                : Palette(iter, MaxIterations, MaxColorIterations);

                            int index = rowOffset + x * 3;
                            buffer[index] = pixelColor.B; buffer[index + 1] = pixelColor.G; buffer[index + 2] = pixelColor.R;
                        }
                        long currentDone = Interlocked.Increment(ref done);
                        if (renderHeight > 0) reportProgressCallback((int)(100.0 * currentDone / renderHeight));
                    });
                }
                else
                {
                    // --- СТАНДАРТНАЯ ТОЧНОСТЬ (DOUBLE) ---
                    double centerX_d = (double)CenterX;
                    double centerY_d = (double)CenterY;
                    double unitsPerPixel_d = (double)Scale / renderWidth;
                    double halfWidthPixels_d = renderWidth / 2.0;
                    double halfHeightPixels_d = renderHeight / 2.0;
                    ComplexDouble p_d = new ComplexDouble((double)P.Real, (double)P.Imaginary);
                    ComplexDouble z0_d = new ComplexDouble((double)Z0.Real, (double)Z0.Imaginary);
                    double m_d = (double)M;

                    Parallel.For(0, renderHeight, po, y =>
                    {
                        int rowOffset = y * bmpData.Stride;
                        for (int x = 0; x < renderWidth; x++)
                        {
                            double re = centerX_d + (x - halfWidthPixels_d) * unitsPerPixel_d;
                            double im = centerY_d - (y - halfHeightPixels_d) * unitsPerPixel_d;

                            ComplexDouble c = new ComplexDouble(re, im);
                            ComplexDouble z = z0_d;
                            int iter = CalculateIterationsDouble(ref z, c, p_d, m_d);

                            Color pixelColor = UseSmoothColoring && SmoothPalette != null
                                ? SmoothPalette(CalculateSmoothValueDouble(iter, z, p_d))
                                : Palette(iter, MaxIterations, MaxColorIterations);

                            int index = rowOffset + x * 3;
                            buffer[index] = pixelColor.B; buffer[index + 1] = pixelColor.G; buffer[index + 2] = pixelColor.R;
                        }
                        long currentDone = Interlocked.Increment(ref done);
                        if (renderHeight > 0) reportProgressCallback((int)(100.0 * currentDone / renderHeight));
                    });
                }
            }
            catch (OperationCanceledException) { /* Игнорируем */ }
            finally
            {
                Marshal.Copy(buffer, 0, bmpData.Scan0, buffer.Length);
                bmp.UnlockBits(bmpData);
            }
            return bmp;
        }

        /// <summary>
        /// Асинхронно рендерит фрактал в Bitmap с использованием суперсэмплинга (SSAA).
        /// </summary>
        public Task<Bitmap> RenderToBitmapSSAA(int finalWidth, int finalHeight, int numThreads, Action<int> reportProgressCallback, int ssaaFactor, CancellationToken cancellationToken = default)
        {
            if (ssaaFactor <= 1)
            {
                return Task.Run(() => RenderToBitmap(finalWidth, finalHeight, numThreads, reportProgressCallback, cancellationToken), cancellationToken);
            }

            return Task.Run(() =>
            {
                int highResWidth = finalWidth * ssaaFactor;
                int highResHeight = finalHeight * ssaaFactor;

                // Создаем копию движка для рендеринга в высоком разрешении, чтобы не менять параметры основного
                var highResEngine = new FractalNovaEngine
                {
                    MaxIterations = this.MaxIterations,
                    ThresholdSquared = this.ThresholdSquared,
                    CenterX = this.CenterX,
                    CenterY = this.CenterY,
                    Scale = this.Scale,
                    P = this.P,
                    Z0 = this.Z0,
                    M = this.M,
                    UseSmoothColoring = this.UseSmoothColoring,
                    Palette = this.Palette,
                    SmoothPalette = this.SmoothPalette,
                    MaxColorIterations = this.MaxColorIterations
                };

                Action<int> highResProgress = p => reportProgressCallback((int)(p * 0.95));
                Bitmap highResBmp = highResEngine.RenderToBitmap(highResWidth, highResHeight, numThreads, highResProgress, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                reportProgressCallback(95);

                // Уменьшаем изображение до финального размера
                Bitmap finalBmp = new Bitmap(finalWidth, finalHeight, PixelFormat.Format24bppRgb);
                using (var g = Graphics.FromImage(finalBmp))
                {
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.DrawImage(highResBmp, 0, 0, finalWidth, finalHeight);
                }
                highResBmp.Dispose();
                reportProgressCallback(100);
                return finalBmp;

            }, cancellationToken);
        }
        #endregion
    }
}
