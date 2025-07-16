using FractalExplorer.Resources;
using FractalExplorer.Utilities;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Numerics;

namespace FractalExplorer.Engines
{
    /// <summary>
    /// Абстрактный базовый класс для движков рендеринга фракталов семейства Мандельброта.
    /// Инкапсулирует общую логику рендеринга и управления параметрами.
    /// Поддерживает АДАПТИВНУЮ ТОЧНОСТЬ: автоматически переключается между double и decimal
    /// в зависимости от уровня масштабирования для оптимального баланса скорости и качества.
    /// </summary>
    public abstract class FractalMandelbrotFamilyEngine
    {
        #region Constants

        /// <summary>
        /// Порог масштабирования для переключения на высокоточные вычисления (decimal).
        /// Рассчитан как начальный масштаб (4.0) / желаемый зум (20000x).
        /// При значениях Scale МЕНЬШЕ этого порога будет использоваться decimal.
        /// </summary>
        private const decimal SCALE_THRESHOLD_FOR_DECIMAL = 4.0m / 2000000000.0m;

        #endregion

        #region Properties

        /// <summary>
        /// Максимальное количество итераций для вычисления фрактала.
        /// </summary>
        public int MaxIterations { get; set; }

        /// <summary>
        /// Квадрат порога, используемый для определения, вышла ли точка за пределы множества.
        /// </summary>
        public decimal ThresholdSquared { get; set; }

        /// <summary>
        /// Константа 'C' для фракталов семейства Жюлиа.
        /// </summary>
        public ComplexDecimal C { get; set; }

        /// <summary>
        /// Координата X центра видимой области фрактала.
        /// </summary>
        public decimal CenterX { get; set; }

        /// <summary>
        /// Координата Y центра видимой области фрактала.
        /// </summary>
        public decimal CenterY { get; set; }

        /// <summary>
        /// Текущий масштаб рендеринга (ширина комплексной плоскости).
        /// </summary>
        public decimal Scale { get; set; }

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

        #region Core Calculation Logic (Abstract Methods for Both Precisions)

        // --- DECIMAL (ВЫСОКАЯ ТОЧНОСТЬ) ---

        /// <summary>
        /// Главный метод вычисления (ВЫСОКАЯ ТОЧНОСТЬ).
        /// </summary>
        public abstract int CalculateIterations(ref ComplexDecimal z, ComplexDecimal c);

        /// <summary>
        /// Определяет параметры для расчета в режиме ВЫСОКОЙ ТОЧНОСТИ.
        /// </summary>
        protected abstract void GetCalculationParameters(decimal re, decimal im, out ComplexDecimal initialZ, out ComplexDecimal constantC);

        // --- DOUBLE (СТАНДАРТНАЯ ТОЧНОСТЬ) ---

        /// <summary>
        /// Главный метод вычисления (СТАНДАРТНАЯ ТОЧНОСТЬ).
        /// </summary>
        public abstract int CalculateIterationsDouble(ref ComplexDouble z, ComplexDouble c);

        /// <summary>
        /// Определяет параметры для расчета в режиме СТАНДАРТНОЙ ТОЧНОСТИ.
        /// </summary>
        protected abstract void GetCalculationParametersDouble(double re, double im, out ComplexDouble initialZ, out ComplexDouble constantC);

        #endregion

        #region Smoothing Logic

        /// <summary>
        /// Вычисляет "сглаженное" значение итерации для decimal.
        /// </summary>
        private double CalculateSmoothValue(int iter, ComplexDecimal finalZ)
        {
            if (iter >= MaxIterations) return iter;
            double log_zn_sq = Math.Log((double)finalZ.MagnitudeSquared);
            double nu = Math.Log(log_zn_sq / (2 * Math.Log(2))) / Math.Log(2);
            return iter + 1 - nu;
        }

        /// <summary>
        /// Вычисляет "сглаженное" значение итерации для double.
        /// </summary>
        private double CalculateSmoothValueDouble(int iter, ComplexDouble finalZ)
        {
            if (iter >= MaxIterations) return iter;
            double log_zn_sq = Math.Log(finalZ.MagnitudeSquared);
            double nu = Math.Log(log_zn_sq / (2 * Math.Log(2))) / Math.Log(2);
            return iter + 1 - nu;
        }

        #endregion

        #region Rendering Methods with Adaptive Precision

        /// <summary>
        /// Отрисовывает одну плитку, автоматически выбирая точность вычислений.
        /// </summary>
        public byte[] RenderSingleTile(TileInfo tile, int canvasWidth, int canvasHeight, out int bytesPerPixel)
        {
            bytesPerPixel = 4;
            byte[] buffer = new byte[tile.Bounds.Width * tile.Bounds.Height * bytesPerPixel];
            if (canvasWidth <= 0 || canvasHeight <= 0) return buffer;

            // --- ЛОГИКА ВЫБОРА ТОЧНОСТИ ---
            if (Scale < SCALE_THRESHOLD_FOR_DECIMAL)
            {
                RenderTileDecimal(buffer, tile, canvasWidth, canvasHeight, bytesPerPixel);
            }
            else
            {
                RenderTileDouble(buffer, tile, canvasWidth, canvasHeight, bytesPerPixel);
            }

            return buffer;
        }

        /// <summary>
        /// Отрисовывает одну плитку с использованием суперсэмплинга (SSAA), автоматически выбирая точность.
        /// </summary>
        public byte[] RenderSingleTileSSAA(TileInfo tile, int canvasWidth, int canvasHeight, int supersamplingFactor, out int bytesPerPixel)
        {
            bytesPerPixel = 4;
            if (supersamplingFactor <= 1)
            {
                return RenderSingleTile(tile, canvasWidth, canvasHeight, out bytesPerPixel);
            }

            byte[] finalTileBuffer = new byte[tile.Bounds.Width * tile.Bounds.Height * bytesPerPixel];
            if (canvasWidth <= 0 || canvasHeight <= 0) return finalTileBuffer;

            // --- ЛОГИКА ВЫБОРА ТОЧНОСТИ ---
            if (Scale < SCALE_THRESHOLD_FOR_DECIMAL)
            {
                RenderTileSSAA_Decimal(finalTileBuffer, tile, canvasWidth, canvasHeight, supersamplingFactor, bytesPerPixel);
            }
            else
            {
                RenderTileSSAA_Double(finalTileBuffer, tile, canvasWidth, canvasHeight, supersamplingFactor, bytesPerPixel);
            }

            return finalTileBuffer;
        }

        /// <summary>
        /// Рендерит фрактал в новый объект Bitmap, автоматически выбирая точность.
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
                // --- ЛОГИКА ВЫБОРА ТОЧНОСТИ ---
                if (Scale < SCALE_THRESHOLD_FOR_DECIMAL)
                {
                    // ВЫСОКАЯ ТОЧНОСТЬ (DECIMAL)
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

                            GetCalculationParameters(re, im, out ComplexDecimal z, out ComplexDecimal c);
                            int iter = CalculateIterations(ref z, c);

                            Color pixelColor = UseSmoothColoring && SmoothPalette != null
                                ? SmoothPalette(CalculateSmoothValue(iter, z))
                                : Palette(iter, MaxIterations, MaxColorIterations);

                            int index = rowOffset + x * 3;
                            if (index + 2 < buffer.Length)
                            {
                                buffer[index] = pixelColor.B;
                                buffer[index + 1] = pixelColor.G;
                                buffer[index + 2] = pixelColor.R;
                            }
                        }
                        long currentDone = Interlocked.Increment(ref done);
                        if (renderHeight > 0) reportProgressCallback((int)(100.0 * currentDone / renderHeight));
                    });
                }
                else
                {
                    // СТАНДАРТНАЯ ТОЧНОСТЬ (DOUBLE)
                    double centerX_d = (double)CenterX;
                    double centerY_d = (double)CenterY;
                    double unitsPerPixel_d = (double)Scale / renderWidth;
                    double halfWidthPixels_d = renderWidth / 2.0;
                    double halfHeightPixels_d = renderHeight / 2.0;

                    Parallel.For(0, renderHeight, po, y =>
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        int rowOffset = y * bmpData.Stride;
                        for (int x = 0; x < renderWidth; x++)
                        {
                            double re = centerX_d + (x - halfWidthPixels_d) * unitsPerPixel_d;
                            double im = centerY_d - (y - halfHeightPixels_d) * unitsPerPixel_d;

                            GetCalculationParametersDouble(re, im, out ComplexDouble z, out ComplexDouble c);
                            int iter = CalculateIterationsDouble(ref z, c);

                            Color pixelColor = UseSmoothColoring && SmoothPalette != null
                                ? SmoothPalette(CalculateSmoothValueDouble(iter, z))
                                : Palette(iter, MaxIterations, MaxColorIterations);

                            int index = rowOffset + x * 3;
                            if (index + 2 < buffer.Length)
                            {
                                buffer[index] = pixelColor.B;
                                buffer[index + 1] = pixelColor.G;
                                buffer[index + 2] = pixelColor.R;
                            }
                        }
                        long currentDone = Interlocked.Increment(ref done);
                        if (renderHeight > 0) reportProgressCallback((int)(100.0 * currentDone / renderHeight));
                    });
                }
            }
            catch (OperationCanceledException)
            {
                // Позволяет выйти из метода без ошибок, если рендеринг был отменен
            }
            finally
            {
                Marshal.Copy(buffer, 0, bmpData.Scan0, buffer.Length);
                bmp.UnlockBits(bmpData);
            }

            return bmp;
        }

        /// <summary>
        /// Рендерит фрактал в новый объект Bitmap с использованием суперсэмплинга (SSAA).
        /// Этот метод инкапсулирует всю логику SSAA: рендеринг в высоком разрешении и последующее уменьшение.
        /// </summary>
        public Task<Bitmap> RenderToBitmapSSAA(int finalWidth, int finalHeight, int numThreads, Action<int> reportProgressCallback, int supersamplingFactor, CancellationToken cancellationToken = default)
        {
            // Если SSAA не требуется, просто вызываем обычный рендер.
            if (supersamplingFactor <= 1)
            {
                return Task.Run(() => RenderToBitmap(finalWidth, finalHeight, numThreads, reportProgressCallback, cancellationToken), cancellationToken);
            }

            // Асинхронно выполняем весь процесс SSAA
            return Task.Run(() =>
            {
                // 1. Рассчитываем разрешение для рендеринга с учетом SSAA.
                int highResWidth = finalWidth * supersamplingFactor;
                int highResHeight = finalHeight * supersamplingFactor;

                // Обертка для прогресс-бара, чтобы он занимал 98% времени.
                Action<int> highResProgressCallback = p => reportProgressCallback((int)(p * 0.98));

                // 2. Рендерим ОГРОМНОЕ изображение. Используем уже существующий метод.
                Bitmap highResBitmap = RenderToBitmap(highResWidth, highResHeight, numThreads, highResProgressCallback, cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();

                reportProgressCallback(98); // Обновляем прогресс до 98%

                // 3. Уменьшаем огромное изображение до целевого размера.
                Bitmap finalBitmap = new Bitmap(highResBitmap, finalWidth, finalHeight);

                // 4. Немедленно освобождаем память от огромного битмапа.
                highResBitmap.Dispose();

                reportProgressCallback(100); // Готово!

                return finalBitmap;
            }, cancellationToken);
        }

        #endregion

        #region Private Rendering Helpers

        // Вспомогательный метод для рендеринга с ВЫСОКОЙ точностью (decimal)
        private void RenderTileDecimal(byte[] buffer, TileInfo tile, int canvasWidth, int canvasHeight, int bytesPerPixel)
        {
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
                    GetCalculationParameters(re, im, out ComplexDecimal z, out ComplexDecimal c);

                    int iter = CalculateIterations(ref z, c);

                    if (UseSmoothColoring && SmoothPalette != null)
                    {
                        double smoothIter = CalculateSmoothValue(iter, z);
                        pixelColor = SmoothPalette(smoothIter);
                    }
                    else
                    {
                        pixelColor = Palette(iter, MaxIterations, MaxColorIterations);
                    }

                    int bufferIndex = (y * tile.Bounds.Width + x) * bytesPerPixel;
                    buffer[bufferIndex] = pixelColor.B;
                    buffer[bufferIndex + 1] = pixelColor.G;
                    buffer[bufferIndex + 2] = pixelColor.R;
                    buffer[bufferIndex + 3] = 255;
                }
            }
        }

        // Вспомогательный метод для рендеринга со СТАНДАРТНОЙ точностью (double)
        private void RenderTileDouble(byte[] buffer, TileInfo tile, int canvasWidth, int canvasHeight, int bytesPerPixel)
        {
            double centerX_d = (double)CenterX;
            double centerY_d = (double)CenterY;
            double scale_d = (double)Scale;

            double halfWidthPixels = canvasWidth / 2.0;
            double halfHeightPixels = canvasHeight / 2.0;
            double unitsPerPixel = scale_d / canvasWidth;

            for (int y = 0; y < tile.Bounds.Height; y++)
            {
                int canvasY = tile.Bounds.Y + y;
                if (canvasY >= canvasHeight) continue;

                for (int x = 0; x < tile.Bounds.Width; x++)
                {
                    int canvasX = tile.Bounds.X + x;
                    if (canvasX >= canvasWidth) continue;

                    double re = centerX_d + (canvasX - halfWidthPixels) * unitsPerPixel;
                    double im = centerY_d - (canvasY - halfHeightPixels) * unitsPerPixel;

                    Color pixelColor;
                    GetCalculationParametersDouble(re, im, out ComplexDouble z, out ComplexDouble c);

                    int iter = CalculateIterationsDouble(ref z, c);

                    if (UseSmoothColoring && SmoothPalette != null)
                    {
                        double smoothIter = CalculateSmoothValueDouble(iter, z);
                        pixelColor = SmoothPalette(smoothIter);
                    }
                    else
                    {
                        pixelColor = Palette(iter, MaxIterations, MaxColorIterations);
                    }

                    int bufferIndex = (y * tile.Bounds.Width + x) * bytesPerPixel;
                    buffer[bufferIndex] = pixelColor.B;
                    buffer[bufferIndex + 1] = pixelColor.G;
                    buffer[bufferIndex + 2] = pixelColor.R;
                    buffer[bufferIndex + 3] = 255;
                }
            }
        }

        // Вспомогательный метод для SSAA рендеринга с ВЫСОКОЙ точностью (decimal)
        private void RenderTileSSAA_Decimal(byte[] finalTileBuffer, TileInfo tile, int canvasWidth, int canvasHeight, int supersamplingFactor, int bytesPerPixel)
        {
            int highResTileWidth = tile.Bounds.Width * supersamplingFactor;
            int highResTileHeight = tile.Bounds.Height * supersamplingFactor;
            Color[,] highResColorBuffer = new Color[highResTileWidth, highResTileHeight];

            long highResCanvasWidth = (long)canvasWidth * supersamplingFactor;
            decimal unitsPerSubPixel = Scale / highResCanvasWidth;
            decimal highResHalfWidthPixels = highResCanvasWidth / 2.0m;
            decimal highResHalfHeightPixels = (long)canvasHeight * supersamplingFactor / 2.0m;

            Parallel.For(0, highResTileHeight, y =>
            {
                for (int x = 0; x < highResTileWidth; x++)
                {
                    long globalHighResX = (long)tile.Bounds.X * supersamplingFactor + x;
                    long globalHighResY = (long)tile.Bounds.Y * supersamplingFactor + y;

                    decimal re = CenterX + (globalHighResX - highResHalfWidthPixels) * unitsPerSubPixel;
                    decimal im = CenterY - (globalHighResY - highResHalfHeightPixels) * unitsPerSubPixel;

                    GetCalculationParameters(re, im, out ComplexDecimal z, out ComplexDecimal c);
                    int iter = CalculateIterations(ref z, c);

                    highResColorBuffer[x, y] = UseSmoothColoring && SmoothPalette != null
                        ? SmoothPalette(CalculateSmoothValue(iter, z))
                        : Palette(iter, MaxIterations, MaxColorIterations);
                }
            });

            // Усреднение цветов
            int sampleCount = supersamplingFactor * supersamplingFactor;
            for (int finalY = 0; finalY < tile.Bounds.Height; finalY++)
            {
                for (int finalX = 0; finalX < tile.Bounds.Width; finalX++)
                {
                    long totalR = 0, totalG = 0, totalB = 0;
                    int startSubX = finalX * supersamplingFactor;
                    int startSubY = finalY * supersamplingFactor;
                    for (int subY = 0; subY < supersamplingFactor; subY++)
                    {
                        for (int subX = 0; subX < supersamplingFactor; subX++)
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
        }

        // Вспомогательный метод для SSAA рендеринга со СТАНДАРТНОЙ точностью (double)
        private void RenderTileSSAA_Double(byte[] finalTileBuffer, TileInfo tile, int canvasWidth, int canvasHeight, int supersamplingFactor, int bytesPerPixel)
        {
            int highResTileWidth = tile.Bounds.Width * supersamplingFactor;
            int highResTileHeight = tile.Bounds.Height * supersamplingFactor;
            Color[,] highResColorBuffer = new Color[highResTileWidth, highResTileHeight];

            long highResCanvasWidth = (long)canvasWidth * supersamplingFactor;
            double unitsPerSubPixel = (double)Scale / highResCanvasWidth;
            double highResHalfWidthPixels = highResCanvasWidth / 2.0;
            double highResHalfHeightPixels = (long)canvasHeight * supersamplingFactor / 2.0;

            double centerX_d = (double)CenterX;
            double centerY_d = (double)CenterY;

            Parallel.For(0, highResTileHeight, y =>
            {
                for (int x = 0; x < highResTileWidth; x++)
                {
                    long globalHighResX = (long)tile.Bounds.X * supersamplingFactor + x;
                    long globalHighResY = (long)tile.Bounds.Y * supersamplingFactor + y;

                    double re = centerX_d + (globalHighResX - highResHalfWidthPixels) * unitsPerSubPixel;
                    double im = centerY_d - (globalHighResY - highResHalfHeightPixels) * unitsPerSubPixel;

                    GetCalculationParametersDouble(re, im, out ComplexDouble z, out ComplexDouble c);
                    int iter = CalculateIterationsDouble(ref z, c);

                    highResColorBuffer[x, y] = UseSmoothColoring && SmoothPalette != null
                        ? SmoothPalette(CalculateSmoothValueDouble(iter, z))
                        : Palette(iter, MaxIterations, MaxColorIterations);
                }
            });

            // Усреднение цветов
            int sampleCount = supersamplingFactor * supersamplingFactor;
            for (int finalY = 0; finalY < tile.Bounds.Height; finalY++)
            {
                for (int finalX = 0; finalX < tile.Bounds.Width; finalX++)
                {
                    long totalR = 0, totalG = 0, totalB = 0;
                    int startSubX = finalX * supersamplingFactor;
                    int startSubY = finalY * supersamplingFactor;
                    for (int subY = 0; subY < supersamplingFactor; subY++)
                    {
                        for (int subX = 0; subX < supersamplingFactor; subX++)
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
        }
        #endregion
    }

    #region Concrete Engines Implementations

    /// <summary>
    /// Движок для множества Мандельброта с адаптивной точностью.
    /// </summary>
    public class MandelbrotEngine : FractalMandelbrotFamilyEngine
    {
        // Decimal path
        protected override void GetCalculationParameters(decimal re, decimal im, out ComplexDecimal initialZ, out ComplexDecimal constantC)
        {
            initialZ = ComplexDecimal.Zero;
            constantC = new ComplexDecimal(re, im);
        }
        public override int CalculateIterations(ref ComplexDecimal z, ComplexDecimal c)
        {
            int iter = 0;
            while (iter < MaxIterations && z.MagnitudeSquared <= ThresholdSquared)
            {
                z = z * z + c;
                iter++;
            }
            return iter;
        }

        // Double path
        protected override void GetCalculationParametersDouble(double re, double im, out ComplexDouble initialZ, out ComplexDouble constantC)
        {
            initialZ = ComplexDouble.Zero;
            constantC = new ComplexDouble(re, im);
        }
        public override int CalculateIterationsDouble(ref ComplexDouble z, ComplexDouble c)
        {
            int iter = 0;
            double thresholdSq = (double)this.ThresholdSquared;
            while (iter < MaxIterations && z.MagnitudeSquared <= thresholdSq)
            {
                z = z * z + c;
                iter++;
            }
            return iter;
        }
    }

    /// <summary>
    /// Движок для множества Жюлиа с адаптивной точностью.
    /// </summary>
    public class JuliaEngine : FractalMandelbrotFamilyEngine
    {
        // Decimal path
        protected override void GetCalculationParameters(decimal re, decimal im, out ComplexDecimal initialZ, out ComplexDecimal constantC)
        {
            initialZ = new ComplexDecimal(re, im);
            constantC = this.C;
        }
        public override int CalculateIterations(ref ComplexDecimal z, ComplexDecimal c)
        {
            int iter = 0;
            while (iter < MaxIterations && z.MagnitudeSquared <= ThresholdSquared)
            {
                z = z * z + c;
                iter++;
            }
            return iter;
        }

        // Double path
        protected override void GetCalculationParametersDouble(double re, double im, out ComplexDouble initialZ, out ComplexDouble constantC)
        {
            initialZ = new ComplexDouble(re, im);
            constantC = new ComplexDouble((double)this.C.Real, (double)this.C.Imaginary);
        }
        public override int CalculateIterationsDouble(ref ComplexDouble z, ComplexDouble c)
        {
            int iter = 0;
            double thresholdSq = (double)this.ThresholdSquared;
            while (iter < MaxIterations && z.MagnitudeSquared <= thresholdSq)
            {
                z = z * z + c;
                iter++;
            }
            return iter;
        }
    }

    /// <summary>
    /// Движок для "Пылающего Корабля" Мандельброта с адаптивной точностью.
    /// </summary>
    public class MandelbrotBurningShipEngine : FractalMandelbrotFamilyEngine
    {
        // Decimal path
        protected override void GetCalculationParameters(decimal re, decimal im, out ComplexDecimal initialZ, out ComplexDecimal constantC)
        {
            initialZ = ComplexDecimal.Zero;
            constantC = new ComplexDecimal(re, im);
        }
        public override int CalculateIterations(ref ComplexDecimal z, ComplexDecimal c)
        {
            int iter = 0;
            while (iter < MaxIterations && z.MagnitudeSquared <= ThresholdSquared)
            {
                z = new ComplexDecimal(Math.Abs(z.Real), -Math.Abs(z.Imaginary)); // Note: Math.Abs for decimal is slow
                z = z * z + c;
                iter++;
            }
            return iter;
        }

        // Double path
        protected override void GetCalculationParametersDouble(double re, double im, out ComplexDouble initialZ, out ComplexDouble constantC)
        {
            initialZ = ComplexDouble.Zero;
            constantC = new ComplexDouble(re, im);
        }
        public override int CalculateIterationsDouble(ref ComplexDouble z, ComplexDouble c)
        {
            int iter = 0;
            double thresholdSq = (double)this.ThresholdSquared;
            while (iter < MaxIterations && z.MagnitudeSquared <= thresholdSq)
            {
                z = new ComplexDouble(Math.Abs(z.Real), -Math.Abs(z.Imaginary));
                z = z * z + c;
                iter++;
            }
            return iter;
        }
    }

    /// <summary>
    /// Движок для "Пылающего Корабля" Жюлиа с адаптивной точностью.
    /// </summary>
    public class JuliaBurningShipEngine : FractalMandelbrotFamilyEngine
    {
        // Decimal path
        protected override void GetCalculationParameters(decimal re, decimal im, out ComplexDecimal initialZ, out ComplexDecimal constantC)
        {
            initialZ = new ComplexDecimal(re, im);
            constantC = this.C;
        }
        public override int CalculateIterations(ref ComplexDecimal z, ComplexDecimal c)
        {
            int iter = 0;
            while (iter < MaxIterations && z.MagnitudeSquared <= ThresholdSquared)
            {
                z = new ComplexDecimal(Math.Abs(z.Real), -Math.Abs(z.Imaginary));
                z = z * z + c;
                iter++;
            }
            return iter;
        }

        // Double path
        protected override void GetCalculationParametersDouble(double re, double im, out ComplexDouble initialZ, out ComplexDouble constantC)
        {
            initialZ = new ComplexDouble(re, im);
            constantC = new ComplexDouble((double)this.C.Real, (double)this.C.Imaginary);
        }
        public override int CalculateIterationsDouble(ref ComplexDouble z, ComplexDouble c)
        {
            int iter = 0;
            double thresholdSq = (double)this.ThresholdSquared;
            while (iter < MaxIterations && z.MagnitudeSquared <= thresholdSq)
            {
                z = new ComplexDouble(Math.Abs(z.Real), -Math.Abs(z.Imaginary));
                z = z * z + c;
                iter++;
            }
            return iter;
        }
    }

    /// <summary>
    /// Движок для Обобщенного множества Мандельброта (z -> z^p + c) с адаптивной точностью.
    /// </summary>
    public class GeneralizedMandelbrotEngine : FractalMandelbrotFamilyEngine
    {
        /// <summary>
        /// Степень 'p', в которую возводится z.
        /// </summary>
        public decimal Power { get; set; } = 3m;

        // Вспомогательный метод для возведения в степень для ComplexDecimal
           private ComplexDecimal ComplexDecimalPow(ComplexDecimal z, decimal power)
        {
            // ИСПРАВЛЕНИЕ: Используем оператор '==' для сравнения с ComplexDecimal.Zero
            if (z == ComplexDecimal.Zero) return ComplexDecimal.Zero;

            double r = z.Magnitude; // У вас уже есть свойство Magnitude, которое возвращает double
            double theta = Math.Atan2((double)z.Imaginary, (double)z.Real);
            double p = (double)power;

            double newR = Math.Pow(r, p);
            double newTheta = theta * p;

            // Используем ваш статический метод для создания из полярных координат
            return ComplexDecimal.FromPolarCoordinates(newR, newTheta);
        }

        // Decimal path
        protected override void GetCalculationParameters(decimal re, decimal im, out ComplexDecimal initialZ, out ComplexDecimal constantC)
        {
            initialZ = ComplexDecimal.Zero;
            constantC = new ComplexDecimal(re, im);
        }
        public override int CalculateIterations(ref ComplexDecimal z, ComplexDecimal c)
        {
            int iter = 0;
            while (iter < MaxIterations && z.MagnitudeSquared <= ThresholdSquared)
            {
                z = ComplexDecimalPow(z, this.Power) + c;
                iter++;
            }
            return iter;
        }

        // Double path
        protected override void GetCalculationParametersDouble(double re, double im, out ComplexDouble initialZ, out ComplexDouble constantC)
        {
            initialZ = ComplexDouble.Zero;
            constantC = new ComplexDouble(re, im);
        }
        public override int CalculateIterationsDouble(ref ComplexDouble z, ComplexDouble c)
        {
            int iter = 0;
            double thresholdSq = (double)this.ThresholdSquared;
            double power_d = (double)this.Power;

            // ИСПРАВЛЕНИЕ: Преобразуем ваши типы в стандартный System.Numerics.Complex
            Complex z_numerics = new Complex(z.Real, z.Imaginary);
            Complex c_numerics = new Complex(c.Real, c.Imaginary);

            while (iter < MaxIterations && z_numerics.Magnitude * z_numerics.Magnitude <= thresholdSq)
            {
                // Выполняем вычисления с использованием стандартного типа
                z_numerics = Complex.Pow(z_numerics, power_d) + c_numerics;
                iter++;
            }
            // Обновляем исходную переменную z (которая передана по ссылке 'ref')
            z = new ComplexDouble(z_numerics.Real, z_numerics.Imaginary);
            return iter;
        }
    }

    #endregion
}