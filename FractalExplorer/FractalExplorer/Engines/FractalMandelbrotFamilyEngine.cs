using FractalExplorer.Resources;
using FractalExplorer.Utilities;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace FractalExplorer.Engines
{
    /// <summary>
    /// Абстрактный базовый класс для движков рендеринга фракталов семейства Мандельброта.
    /// Инкапсулирует общую логику, управление параметрами и поддерживает адаптивную точность вычислений,
    /// автоматически переключаясь между <see cref="double"/> и <see cref="decimal"/> в зависимости от масштаба.
    /// </summary>
    public abstract class FractalMandelbrotFamilyEngine
    {
        #region Constants

        /// <summary>
        /// Порог масштабирования для переключения на высокоточные вычисления (<see langword="decimal"/>).
        /// При значениях <see cref="Scale"/> МЕНЬШЕ этого порога будет использоваться <see langword="decimal"/>.
        /// </summary>
        private const decimal SCALE_THRESHOLD_FOR_DECIMAL = 4.0m / 2000000000.0m;

        #endregion

        #region Properties

        /// <summary>
        /// Получает или задает максимальное количество итераций для вычисления фрактала.
        /// </summary>
        public int MaxIterations { get; set; }

        /// <summary>
        /// Получает или задает квадрат порога (bailout value), используемый для определения, вышла ли точка за пределы множества.
        /// </summary>
        public virtual decimal ThresholdSquared { get; set; }

        /// <summary>
        /// Получает или задает комплексную константу 'C' для фракталов семейства Жюлиа.
        /// </summary>
        public ComplexDecimal C { get; set; }

        /// <summary>
        /// Получает или задает координату X центра видимой области фрактала.
        /// </summary>
        public decimal CenterX { get; set; }

        /// <summary>
        /// Получает или задает координату Y центра видимой области фрактала.
        /// </summary>
        public decimal CenterY { get; set; }

        /// <summary>
        /// Получает или задает текущий масштаб рендеринга (ширина комплексной плоскости, отображаемая на экране).
        /// </summary>
        public decimal Scale { get; set; }

        /// <summary>
        /// Получает или задает флаг, указывающий, нужно ли использовать непрерывное (сглаженное) окрашивание.
        /// </summary>
        public bool UseSmoothColoring { get; set; } = false;

        /// <summary>
        /// Получает или задает функцию палитры для дискретного окрашивания.
        /// Принимает (текущая итерация, макс. итераций, макс. итераций для цвета) и возвращает цвет.
        /// </summary>
        public Func<int, int, int, Color> Palette { get; set; }

        /// <summary>
        /// Получает или задает функцию палитры для непрерывного (сглаженного) окрашивания.
        /// Принимает дробное значение итерации и возвращает цвет.
        /// </summary>
        public Func<double, Color> SmoothPalette { get; set; }

        /// <summary>
        /// Получает или задает максимальное количество итераций для нормализации цвета в палитре (для дискретного режима).
        /// </summary>
        public int MaxColorIterations { get; set; } = 1000;

        #endregion

        #region Core Calculation Logic (Abstract Methods)

        /// <summary>
        /// Копирует специфичные для конкретного движка параметры из исходного экземпляра.
        /// </summary>
        /// <param name="source">Исходный движок, из которого копируются параметры.</param>
        public abstract void CopySpecificParametersFrom(FractalMandelbrotFamilyEngine source);

        /// <summary>
        /// Вычисляет количество итераций для точки с использованием высокой точности (<see cref="ComplexDecimal"/>).
        /// </summary>
        /// <param name="z">Начальное комплексное число (передается по ссылке и изменяется в процессе).</param>
        /// <param name="c">Комплексная константа.</param>
        /// <returns>Количество итераций до выхода за пределы порога или <see cref="MaxIterations"/>.</returns>
        public abstract int CalculateIterations(ref ComplexDecimal z, ComplexDecimal c);

        /// <summary>
        /// Определяет начальные параметры для расчета точки с использованием высокой точности (<see cref="ComplexDecimal"/>).
        /// </summary>
        /// <param name="re">Действительная часть координаты точки.</param>
        /// <param name="im">Мнимая часть координаты точки.</param>
        /// <param name="initialZ">Выходной параметр: начальное значение Z.</param>
        /// <param name="constantC">Выходной параметр: константа C.</param>
        protected abstract void GetCalculationParameters(decimal re, decimal im, out ComplexDecimal initialZ, out ComplexDecimal constantC);

        /// <summary>
        /// Вычисляет количество итераций для точки с использованием стандартной точности (<see cref="ComplexDouble"/>).
        /// </summary>
        /// <param name="z">Начальное комплексное число (передается по ссылке и изменяется в процессе).</param>
        /// <param name="c">Комплексная константа.</param>
        /// <returns>Количество итераций до выхода за пределы порога или <see cref="MaxIterations"/>.</returns>
        public abstract int CalculateIterationsDouble(ref ComplexDouble z, ComplexDouble c);

        /// <summary>
        /// Определяет начальные параметры для расчета точки с использованием стандартной точности (<see cref="ComplexDouble"/>).
        /// </summary>
        /// <param name="re">Действительная часть координаты точки.</param>
        /// <param name="im">Мнимая часть координаты точки.</param>
        /// <param name="initialZ">Выходной параметр: начальное значение Z.</param>
        /// <param name="constantC">Выходной параметр: константа C.</param>
        protected abstract void GetCalculationParametersDouble(double re, double im, out ComplexDouble initialZ, out ComplexDouble constantC);

        #endregion

        #region Private Smoothing Logic

        /// <summary>
        /// Вычисляет "сглаженное" значение итерации для высокой точности (<see cref="ComplexDecimal"/>).
        /// </summary>
        /// <param name="iter">Количество итераций, после которых точка покинула множество.</param>
        /// <param name="finalZ">Конечное значение Z после итераций.</param>
        /// <returns>Дробное значение итерации для плавного окрашивания.</returns>
        private double CalculateSmoothValue(int iter, ComplexDecimal finalZ)
        {
            if (iter >= MaxIterations) return iter;
            double log_zn_sq = Math.Log((double)finalZ.MagnitudeSquared);
            double nu = Math.Log(log_zn_sq / (2 * Math.Log(2))) / Math.Log(2);
            return iter + 1 - nu;
        }

        /// <summary>
        /// Вычисляет "сглаженное" значение итерации для стандартной точности (<see cref="ComplexDouble"/>).
        /// </summary>
        /// <param name="iter">Количество итераций, после которых точка покинула множество.</param>
        /// <param name="finalZ">Конечное значение Z после итераций.</param>
        /// <returns>Дробное значение итерации для плавного окрашивания.</returns>
        private double CalculateSmoothValueDouble(int iter, ComplexDouble finalZ)
        {
            if (iter >= MaxIterations) return iter;
            double log_zn_sq = Math.Log(finalZ.MagnitudeSquared);
            double nu = Math.Log(log_zn_sq / (2 * Math.Log(2))) / Math.Log(2);
            return iter + 1 - nu;
        }

        #endregion

        #region Public Rendering Methods

        /// <summary>
        /// Отрисовывает одну плитку (тайл), автоматически выбирая точность вычислений на основе текущего масштаба.
        /// </summary>
        /// <param name="tile">Информация о плитке для рендеринга.</param>
        /// <param name="canvasWidth">Общая ширина холста.</param>
        /// <param name="canvasHeight">Общая высота холста.</param>
        /// <param name="bytesPerPixel">Выходной параметр: количество байт на пиксель (BGRA).</param>
        /// <returns>Массив байт с пиксельными данными плитки в формате BGRA.</returns>
        public byte[] RenderSingleTile(TileInfo tile, int canvasWidth, int canvasHeight, out int bytesPerPixel)
        {
            bytesPerPixel = 4;
            byte[] buffer = new byte[tile.Bounds.Width * tile.Bounds.Height * bytesPerPixel];
            if (canvasWidth <= 0 || canvasHeight <= 0) return buffer;

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
        /// Отрисовывает одну плитку с использованием суперсэмплинга (SSAA), автоматически выбирая точность вычислений.
        /// </summary>
        /// <param name="tile">Информация о плитке для рендеринга.</param>
        /// <param name="canvasWidth">Общая ширина холста.</param>
        /// <param name="canvasHeight">Общая высота холста.</param>
        /// <param name="supersamplingFactor">Фактор суперсэмплинга (например, 2 для 2x2 SSAA).</param>
        /// <param name="bytesPerPixel">Выходной параметр: количество байт на пиксель (BGRA).</param>
        /// <returns>Массив байт с пиксельными данными плитки в формате BGRA.</returns>
        public byte[] RenderSingleTileSSAA(TileInfo tile, int canvasWidth, int canvasHeight, int supersamplingFactor, out int bytesPerPixel)
        {
            bytesPerPixel = 4;
            if (supersamplingFactor <= 1)
            {
                return RenderSingleTile(tile, canvasWidth, canvasHeight, out bytesPerPixel);
            }

            byte[] finalTileBuffer = new byte[tile.Bounds.Width * tile.Bounds.Height * bytesPerPixel];
            if (canvasWidth <= 0 || canvasHeight <= 0) return finalTileBuffer;

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
        /// Рендерит фрактал в новый объект <see cref="Bitmap"/>, автоматически выбирая точность вычислений.
        /// </summary>
        /// <param name="renderWidth">Ширина итогового изображения.</param>
        /// <param name="renderHeight">Высота итогового изображения.</param>
        /// <param name="numThreads">Количество потоков для параллельного рендеринга.</param>
        /// <param name="reportProgressCallback">Обратный вызов для сообщения о прогрессе (от 0 до 100).</param>
        /// <param name="cancellationToken">Токен для отмены операции.</param>
        /// <returns>Объект <see cref="Bitmap"/> с изображением фрактала.</returns>
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
                if (Scale < SCALE_THRESHOLD_FOR_DECIMAL) // Высокая точность (decimal)
                {
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
                else // Стандартная точность (double)
                {
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
                // Позволяет выйти из метода без ошибок, если рендеринг был отменен.
            }
            finally
            {
                Marshal.Copy(buffer, 0, bmpData.Scan0, buffer.Length);
                bmp.UnlockBits(bmpData);
            }

            return bmp;
        }

        /// <summary>
        /// Асинхронно рендерит фрактал в новый <see cref="Bitmap"/> с использованием суперсэмплинга (SSAA).
        /// Этот метод инкапсулирует логику рендеринга в высоком разрешении и последующего уменьшения изображения.
        /// </summary>
        /// <param name="finalWidth">Итоговая ширина изображения.</param>
        /// <param name="finalHeight">Итоговая высота изображения.</param>
        /// <param name="numThreads">Количество потоков для параллельного рендеринга.</param>
        /// <param name="reportProgressCallback">Обратный вызов для сообщения о прогрессе (от 0 до 100).</param>
        /// <param name="supersamplingFactor">Фактор суперсэмплинга (например, 2 для 2x2 SSAA).</param>
        /// <param name="cancellationToken">Токен для отмены операции.</param>
        /// <returns>Задача, результатом которой является <see cref="Bitmap"/> с сглаженным изображением фрактала.</returns>
        public Task<Bitmap> RenderToBitmapSSAA(int finalWidth, int finalHeight, int numThreads, Action<int> reportProgressCallback, int supersamplingFactor, CancellationToken cancellationToken = default)
        {
            if (supersamplingFactor <= 1)
            {
                return Task.Run(() => RenderToBitmap(finalWidth, finalHeight, numThreads, reportProgressCallback, cancellationToken), cancellationToken);
            }

            return Task.Run(() =>
            {
                // Рендеринг с повышенным разрешением
                int highResWidth = finalWidth * supersamplingFactor;
                int highResHeight = finalHeight * supersamplingFactor;
                Action<int> highResProgressCallback = p => reportProgressCallback((int)(p * 0.98));
                Bitmap highResBitmap = RenderToBitmap(highResWidth, highResHeight, numThreads, highResProgressCallback, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                reportProgressCallback(98);

                // Уменьшение изображения до целевого размера
                Bitmap finalBitmap = new Bitmap(highResBitmap, finalWidth, finalHeight);
                highResBitmap.Dispose(); // Немедленное освобождение памяти
                reportProgressCallback(100);

                return finalBitmap;
            }, cancellationToken);
        }

        #endregion

        #region Private Rendering Helpers

        /// <summary>
        /// Вспомогательный метод для рендеринга тайла с высокой точностью (<see cref="decimal"/>).
        /// </summary>
        /// <param name="buffer">Буфер для записи пиксельных данных.</param>
        /// <param name="tile">Информация о плитке.</param>
        /// <param name="canvasWidth">Ширина холста.</param>
        /// <param name="canvasHeight">Высота холста.</param>
        /// <param name="bytesPerPixel">Количество байт на пиксель.</param>
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

                    GetCalculationParameters(re, im, out ComplexDecimal z, out ComplexDecimal c);
                    int iter = CalculateIterations(ref z, c);

                    Color pixelColor = UseSmoothColoring && SmoothPalette != null
                        ? SmoothPalette(CalculateSmoothValue(iter, z))
                        : Palette(iter, MaxIterations, MaxColorIterations);

                    int bufferIndex = (y * tile.Bounds.Width + x) * bytesPerPixel;
                    buffer[bufferIndex] = pixelColor.B;
                    buffer[bufferIndex + 1] = pixelColor.G;
                    buffer[bufferIndex + 2] = pixelColor.R;
                    buffer[bufferIndex + 3] = 255;
                }
            }
        }

        /// <summary>
        /// Вспомогательный метод для рендеринга тайла со стандартной точностью (<see cref="double"/>).
        /// </summary>
        /// <param name="buffer">Буфер для записи пиксельных данных.</param>
        /// <param name="tile">Информация о плитке.</param>
        /// <param name="canvasWidth">Ширина холста.</param>
        /// <param name="canvasHeight">Высота холста.</param>
        /// <param name="bytesPerPixel">Количество байт на пиксель.</param>
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

                    GetCalculationParametersDouble(re, im, out ComplexDouble z, out ComplexDouble c);
                    int iter = CalculateIterationsDouble(ref z, c);

                    Color pixelColor = UseSmoothColoring && SmoothPalette != null
                        ? SmoothPalette(CalculateSmoothValueDouble(iter, z))
                        : Palette(iter, MaxIterations, MaxColorIterations);

                    int bufferIndex = (y * tile.Bounds.Width + x) * bytesPerPixel;
                    buffer[bufferIndex] = pixelColor.B;
                    buffer[bufferIndex + 1] = pixelColor.G;
                    buffer[bufferIndex + 2] = pixelColor.R;
                    buffer[bufferIndex + 3] = 255;
                }
            }
        }

        /// <summary>
        /// Вспомогательный метод для SSAA рендеринга тайла с высокой точностью (<see cref="decimal"/>).
        /// </summary>
        /// <param name="finalTileBuffer">Буфер для итогового изображения тайла.</param>
        /// <param name="tile">Информация о плитке.</param>
        /// <param name="canvasWidth">Ширина холста.</param>
        /// <param name="canvasHeight">Высота холста.</param>
        /// <param name="supersamplingFactor">Фактор суперсэмплинга.</param>
        /// <param name="bytesPerPixel">Количество байт на пиксель.</param>
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

            // Усреднение цветов субпикселей для получения итогового цвета пикселя
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

        /// <summary>
        /// Вспомогательный метод для SSAA рендеринга тайла со стандартной точностью (<see cref="double"/>).
        /// </summary>
        /// <param name="finalTileBuffer">Буфер для итогового изображения тайла.</param>
        /// <param name="tile">Информация о плитке.</param>
        /// <param name="canvasWidth">Ширина холста.</param>
        /// <param name="canvasHeight">Высота холста.</param>
        /// <param name="supersamplingFactor">Фактор суперсэмплинга.</param>
        /// <param name="bytesPerPixel">Количество байт на пиксель.</param>
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

            // Усреднение цветов субпикселей для получения итогового цвета пикселя
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
    /// Реализует движок для рендеринга фрактала "Буффало".
    /// Итерационная формула: z -> (|Re(z)| + i|Im(z)|)² + c.
    /// </summary>
    public class BuffaloEngine : FractalMandelbrotFamilyEngine
    {
        /// <inheritdoc />
        public override void CopySpecificParametersFrom(FractalMandelbrotFamilyEngine source)
        {
            // Для этого движка нет специфичных параметров.
        }

        /// <inheritdoc />
        protected override void GetCalculationParameters(decimal re, decimal im, out ComplexDecimal initialZ, out ComplexDecimal constantC)
        {
            initialZ = ComplexDecimal.Zero;
            constantC = new ComplexDecimal(re, im);
        }

        /// <inheritdoc />
        public override int CalculateIterations(ref ComplexDecimal z, ComplexDecimal c)
        {
            int iter = 0;
            while (iter < MaxIterations && z.MagnitudeSquared <= ThresholdSquared)
            {
                // Применяем модуль к каждой компоненте перед возведением в квадрат
                z = new ComplexDecimal(Math.Abs(z.Real), Math.Abs(z.Imaginary));
                z = z * z + c;
                iter++;
            }
            return iter;
        }

        /// <inheritdoc />
        protected override void GetCalculationParametersDouble(double re, double im, out ComplexDouble initialZ, out ComplexDouble constantC)
        {
            initialZ = ComplexDouble.Zero;
            constantC = new ComplexDouble(re, im);
        }

        /// <inheritdoc />
        public override int CalculateIterationsDouble(ref ComplexDouble z, ComplexDouble c)
        {
            int iter = 0;
            double thresholdSq = (double)ThresholdSquared;
            while (iter < MaxIterations && z.MagnitudeSquared <= thresholdSq)
            {
                // Применяем модуль к каждой компоненте перед возведением в квадрат
                z = new ComplexDouble(Math.Abs(z.Real), Math.Abs(z.Imaginary));
                z = z * z + c;
                iter++;
            }
            return iter;
        }
    }

    /// <summary>
    /// Реализует движок для рендеринга фрактала Симоноброт (пользовательская версия).
    /// Итерационная формула: z -> (z^p * |z|^p) + c.
    /// </summary>
    public class SimonobrotEngine : FractalMandelbrotFamilyEngine
    {
        /// <summary>
        /// Получает или задает степень 'p', в которую возводится z и |z|.
        /// </summary>
        public decimal Power { get; set; } = 2m;

        /// <summary>
        /// Получает или задает флаг, определяющий зеркальное отражение фрактала относительно вертикальной оси.
        /// <see langword="false"/>: обычное отображение;
        /// <see langword="true"/>: зеркальное отражение (инверсия по горизонтали).
        /// </summary>
        public bool UseInversion { get; set; } = false;

        /// <inheritdoc />
        public override void CopySpecificParametersFrom(FractalMandelbrotFamilyEngine source)
        {
            if (source is SimonobrotEngine sourceEngine)
            {
                this.Power = sourceEngine.Power;
                this.UseInversion = sourceEngine.UseInversion;
            }
        }

        /// <inheritdoc />
        protected override void GetCalculationParameters(decimal re, decimal im, out ComplexDecimal initialZ, out ComplexDecimal constantC)
        {
            initialZ = ComplexDecimal.Zero;

            // Применяем зеркальное отражение относительно вертикальной оси
            if (UseInversion)
            {
                constantC = new ComplexDecimal(-re, im);
            }
            else
            {
                constantC = new ComplexDecimal(re, im);
            }
        }

        /// <inheritdoc />
        public override int CalculateIterations(ref ComplexDecimal z, ComplexDecimal c)
        {
            int iter = 0;
            decimal p = Power;

            while (iter < MaxIterations && z.MagnitudeSquared <= ThresholdSquared)
            {
                // Обрабатываем сингулярность z=0 в первую очередь.
                if (z.MagnitudeSquared == 0)
                {
                    // Для первой итерации z_next всегда равно c, чтобы избежать 0^(-p).
                    z = c;
                }
                else
                {
                    // Теперь, когда z != 0, можно безопасно выполнять основные расчеты.
                    ComplexDecimal zPower = PowerComplex(z, p);

                    // Используем стандартную формулу Simonobrot: (z^p * |z|^p) + c
                    decimal magnitude = (decimal)z.Magnitude;
                    decimal magnitudePower = (decimal)Math.Pow((double)magnitude, (double)p);
                    z = new ComplexDecimal(zPower.Real * magnitudePower + c.Real,
                                         zPower.Imaginary * magnitudePower + c.Imaginary);
                }

                iter++;
            }

            return iter;
        }

        /// <inheritdoc />
        protected override void GetCalculationParametersDouble(double re, double im, out ComplexDouble initialZ, out ComplexDouble constantC)
        {
            initialZ = ComplexDouble.Zero;

            // Применяем зеркальное отражение относительно вертикальной оси
            if (UseInversion)
            {
                constantC = new ComplexDouble(-re, im);
            }
            else
            {
                constantC = new ComplexDouble(re, im);
            }
        }

        /// <inheritdoc />
        public override int CalculateIterationsDouble(ref ComplexDouble z, ComplexDouble c)
        {
            int iter = 0;
            double thresholdSq = (double)ThresholdSquared;
            double p = (double)Power;

            while (iter < MaxIterations && z.MagnitudeSquared <= thresholdSq)
            {
                // Обрабатываем сингулярность z=0 в первую очередь.
                if (z.MagnitudeSquared == 0)
                {
                    // Для первой итерации z_next всегда равно c, чтобы избежать 0^(-p).
                    z = c;
                }
                else
                {
                    // Теперь, когда z != 0, можно безопасно выполнять основные расчеты.
                    ComplexDouble zPower = PowerComplexDouble(z, p);

                    // Используем стандартную формулу Simonobrot: (z^p * |z|^p) + c
                    double magnitude = z.Magnitude;
                    double magnitudePower = Math.Pow(magnitude, p);
                    z = new ComplexDouble(zPower.Real * magnitudePower + c.Real,
                                        zPower.Imaginary * magnitudePower + c.Imaginary);
                }

                iter++;
            }

            return iter;
        }

        /// <summary>
        /// Возводит комплексное число в степень (<see langword="decimal"/> версия).
        /// </summary>
        /// <param name="z">Комплексное число.</param>
        /// <param name="power">Степень.</param>
        /// <returns>Результат возведения в степень.</returns>
        private ComplexDecimal PowerComplex(ComplexDecimal z, decimal power)
        {
            // Проверка на z=0 была вынесена выше, поэтому здесь она не нужна.
            double r = z.Magnitude;
            double theta = Math.Atan2((double)z.Imaginary, (double)z.Real);
            double p = (double)power;

            double newR = Math.Pow(r, p);
            double newTheta = theta * p;

            return ComplexDecimal.FromPolarCoordinates(newR, newTheta);
        }

        /// <summary>
        /// Возводит комплексное число в степень (<see langword="double"/> версия).
        /// </summary>
        /// <param name="z">Комплексное число.</param>
        /// <param name="power">Степень.</param>
        /// <returns>Результат возведения в степень.</returns>
        private ComplexDouble PowerComplexDouble(ComplexDouble z, double power)
        {
            // Проверка на z=0 была вынесена выше, поэтому здесь она не нужна.
            // Используем стандартную быструю реализацию.
            System.Numerics.Complex result = System.Numerics.Complex.Pow(new System.Numerics.Complex(z.Real, z.Imaginary), power);
            return new ComplexDouble(result.Real, result.Imaginary);
        }
    }

    /// <summary>
    /// Реализует движок для рендеринга классического множества Мандельброта (z = z^2 + c).
    /// </summary>
    public class MandelbrotEngine : FractalMandelbrotFamilyEngine
    {
        /// <inheritdoc />
        public override void CopySpecificParametersFrom(FractalMandelbrotFamilyEngine source)
        {
            // Для этого движка нет специфичных параметров для копирования.
        }

        /// <inheritdoc />
        protected override void GetCalculationParameters(decimal re, decimal im, out ComplexDecimal initialZ, out ComplexDecimal constantC)
        {
            initialZ = ComplexDecimal.Zero;
            constantC = new ComplexDecimal(re, im);
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        protected override void GetCalculationParametersDouble(double re, double im, out ComplexDouble initialZ, out ComplexDouble constantC)
        {
            initialZ = ComplexDouble.Zero;
            constantC = new ComplexDouble(re, im);
        }

        /// <inheritdoc />
        public override int CalculateIterationsDouble(ref ComplexDouble z, ComplexDouble c)
        {
            int iter = 0;
            double thresholdSq = (double)ThresholdSquared;
            while (iter < MaxIterations && z.MagnitudeSquared <= thresholdSq)
            {
                z = z * z + c;
                iter++;
            }
            return iter;
        }
    }

    /// <summary>
    /// Реализует движок для рендеринга множества Жюлиа (z = z^2 + c), где 'c' - константа.
    /// </summary>
    public class JuliaEngine : FractalMandelbrotFamilyEngine
    {
        /// <inheritdoc />
        public override void CopySpecificParametersFrom(FractalMandelbrotFamilyEngine source)
        {
            // Для этого движка нет специфичных параметров для копирования.
        }

        /// <inheritdoc />
        protected override void GetCalculationParameters(decimal re, decimal im, out ComplexDecimal initialZ, out ComplexDecimal constantC)
        {
            initialZ = new ComplexDecimal(re, im);
            constantC = C; // Используется заданная константа C
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        protected override void GetCalculationParametersDouble(double re, double im, out ComplexDouble initialZ, out ComplexDouble constantC)
        {
            initialZ = new ComplexDouble(re, im);
            constantC = new ComplexDouble((double)C.Real, (double)C.Imaginary);
        }

        /// <inheritdoc />
        public override int CalculateIterationsDouble(ref ComplexDouble z, ComplexDouble c)
        {
            int iter = 0;
            double thresholdSq = (double)ThresholdSquared;
            while (iter < MaxIterations && z.MagnitudeSquared <= thresholdSq)
            {
                z = z * z + c;
                iter++;
            }
            return iter;
        }
    }

    /// <summary>
    /// Реализует движок для рендеринга фрактала "Пылающий Корабль" (Mandelbrot-like).
    /// Итерационная формула: z' = |Re(z)| - i*|Im(z)|, z_next = (z')^2 + c.
    /// </summary>
    public class MandelbrotBurningShipEngine : FractalMandelbrotFamilyEngine
    {
        /// <inheritdoc />
        public override void CopySpecificParametersFrom(FractalMandelbrotFamilyEngine source)
        {
            // Для этого движка нет специфичных параметров для копирования.
        }

        /// <inheritdoc />
        protected override void GetCalculationParameters(decimal re, decimal im, out ComplexDecimal initialZ, out ComplexDecimal constantC)
        {
            initialZ = ComplexDecimal.Zero;
            constantC = new ComplexDecimal(re, im);
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        protected override void GetCalculationParametersDouble(double re, double im, out ComplexDouble initialZ, out ComplexDouble constantC)
        {
            initialZ = ComplexDouble.Zero;
            constantC = new ComplexDouble(re, im);
        }

        /// <inheritdoc />
        public override int CalculateIterationsDouble(ref ComplexDouble z, ComplexDouble c)
        {
            int iter = 0;
            double thresholdSq = (double)ThresholdSquared;
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
    /// Реализует движок для рендеринга фрактала "Пылающий Корабль" (Julia-like).
    /// Итерационная формула: z' = |Re(z)| - i*|Im(z)|, z_next = (z')^2 + c.
    /// </summary>
    public class JuliaBurningShipEngine : FractalMandelbrotFamilyEngine
    {
        /// <inheritdoc />
        public override void CopySpecificParametersFrom(FractalMandelbrotFamilyEngine source)
        {
            // Для этого движка нет специфичных параметров для копирования.
        }

        /// <inheritdoc />
        protected override void GetCalculationParameters(decimal re, decimal im, out ComplexDecimal initialZ, out ComplexDecimal constantC)
        {
            initialZ = new ComplexDecimal(re, im);
            constantC = C;
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        protected override void GetCalculationParametersDouble(double re, double im, out ComplexDouble initialZ, out ComplexDouble constantC)
        {
            initialZ = new ComplexDouble(re, im);
            constantC = new ComplexDouble((double)C.Real, (double)C.Imaginary);
        }

        /// <inheritdoc />
        public override int CalculateIterationsDouble(ref ComplexDouble z, ComplexDouble c)
        {
            int iter = 0;
            double thresholdSq = (double)ThresholdSquared;
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
    /// Реализует движок для рендеринга Обобщенного множества Мандельброта (z -> z^p + c).
    /// </summary>
    public class GeneralizedMandelbrotEngine : FractalMandelbrotFamilyEngine
    {
        /// <summary>
        /// Получает или задает степень 'p', в которую возводится z.
        /// </summary>
        public decimal Power { get; set; } = 3m;

        /// <inheritdoc />
        public override void CopySpecificParametersFrom(FractalMandelbrotFamilyEngine source)
        {
            if (source is GeneralizedMandelbrotEngine sourceEngine)
            {
                this.Power = sourceEngine.Power;
            }
        }

        /// <inheritdoc />
        protected override void GetCalculationParameters(decimal re, decimal im, out ComplexDecimal initialZ, out ComplexDecimal constantC)
        {
            initialZ = ComplexDecimal.Zero;
            constantC = new ComplexDecimal(re, im);
        }

        /// <inheritdoc />
        public override int CalculateIterations(ref ComplexDecimal z, ComplexDecimal c)
        {
            int iter = 0;
            while (iter < MaxIterations && z.MagnitudeSquared <= ThresholdSquared)
            {
                z = ComplexDecimalPow(z, Power) + c;
                iter++;
            }
            return iter;
        }

        /// <inheritdoc />
        protected override void GetCalculationParametersDouble(double re, double im, out ComplexDouble initialZ, out ComplexDouble constantC)
        {
            initialZ = ComplexDouble.Zero;
            constantC = new ComplexDouble(re, im);
        }

        /// <inheritdoc />
        public override int CalculateIterationsDouble(ref ComplexDouble z, ComplexDouble c)
        {
            int iter = 0;
            double thresholdSq = (double)ThresholdSquared;
            double power_d = (double)Power;

            // Преобразуем в стандартный System.Numerics.Complex для использования быстрой функции Pow
            Complex z_numerics = new Complex(z.Real, z.Imaginary);
            Complex c_numerics = new Complex(c.Real, c.Imaginary);

            while (iter < MaxIterations && z_numerics.Magnitude * z_numerics.Magnitude <= thresholdSq)
            {
                z_numerics = Complex.Pow(z_numerics, power_d) + c_numerics;
                iter++;
            }

            // Обновляем исходную переменную z, переданную по ссылке
            z = new ComplexDouble(z_numerics.Real, z_numerics.Imaginary);
            return iter;
        }

        /// <summary>
        /// Возводит комплексное число высокой точности в указанную степень.
        /// </summary>
        /// <param name="z">Комплексное число для возведения в степень.</param>
        /// <param name="power">Степень.</param>
        /// <returns>Результат <paramref name="z"/> в степени <paramref name="power"/>.</returns>
        private ComplexDecimal ComplexDecimalPow(ComplexDecimal z, decimal power)
        {
            if (z == ComplexDecimal.Zero) return ComplexDecimal.Zero;

            double r = z.Magnitude;
            double theta = Math.Atan2((double)z.Imaginary, (double)z.Real);
            double p = (double)power;

            double newR = Math.Pow(r, p);
            double newTheta = theta * p;

            return ComplexDecimal.FromPolarCoordinates(newR, newTheta);
        }
    }

    #endregion
}