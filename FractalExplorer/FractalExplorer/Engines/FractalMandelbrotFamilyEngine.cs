using FractalExplorer.Resources;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace FractalExplorer.Engines
{
    /// <summary>
    /// Абстрактный базовый класс для движков рендеринга фракталов семейства Мандельброта.
    /// Инкапсулирует общую логику рендеринга и управления параметрами.
    /// </summary>
    public abstract class FractalMandelbrotFamilyEngine
    {
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
        /// Функция палитры, используемая для преобразования количества итераций в цвет.
        /// Принимает (текущая итерация, макс. итераций, макс. итераций для цвета) и возвращает цвет.
        /// </summary>
        public Func<int, int, int, Color> Palette { get; set; }

        /// <summary>
        /// Максимальное количество итераций для нормализации цвета в палитре.
        /// </summary>
        public int MaxColorIterations { get; set; } = 1000;

        /// <summary>
        /// Главный абстрактный метод вычисления. Для данной точки z0 и константы c,
        /// возвращает количество итераций до выхода за порог.
        /// </summary>
        /// <param name="z">Начальная точка итерации.</param>
        /// <param name="c">Константа для итерации.</param>
        /// <returns>Количество итераций.</returns>
        public abstract int CalculateIterations(ComplexDecimal z, ComplexDecimal c);

        /// <summary>
        /// Отрисовывает одну плитку (тайл) в предоставленный общий буфер пикселей.
        /// </summary>
        /// <param name="buffer">Общий буфер байтов всего изображения, в который будут записаны пиксельные данные.</param>
        /// <param name="stride">Длина строки в байтах (количество байтов на одну строку изображения).</param>
        /// <param name="bytesPerPixel">Количество байтов на один пиксель.</param>
        /// <param name="tile">Информация о плитке для отрисовки.</param>
        /// <param name="canvasWidth">Общая ширина холста.</param>
        /// <param name="canvasHeight">Общая высота холста.</param>
        public void RenderTile(byte[] buffer, int stride, int bytesPerPixel, TileInfo tile, int canvasWidth, int canvasHeight)
        {
            // Защита от деления на ноль.
            if (canvasWidth <= 0 || canvasHeight <= 0) return;

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

                    int iter = GetIterationsForPoint(re, im);
                    Color pixelColor = Palette(iter, MaxIterations, MaxColorIterations);

                    int bufferIndex = canvasY * stride + canvasX * bytesPerPixel;
                    if (bufferIndex + bytesPerPixel - 1 < buffer.Length)
                    {
                        buffer[bufferIndex] = pixelColor.B;
                        buffer[bufferIndex + 1] = pixelColor.G;
                        buffer[bufferIndex + 2] = pixelColor.R;
                        if (bytesPerPixel == 4) buffer[bufferIndex + 3] = pixelColor.A;
                    }
                }
            }
        }

        /// <summary>
        /// Отрисовывает одну плитку в ее собственный, отдельный байтовый массив.
        /// </summary>
        /// <param name="tile">Информация о плитке для отрисовки.</param>
        /// <param name="canvasWidth">Общая ширина холста.</param>
        /// <param name="canvasHeight">Общая высота холста.</param>
        /// <param name="bytesPerPixel">Выходной параметр, возвращающий количество байтов на пиксель (всегда 4).</param>
        /// <returns>Массив байт с данными пикселей плитки.</returns>
        public byte[] RenderSingleTile(TileInfo tile, int canvasWidth, int canvasHeight, out int bytesPerPixel)
        {
            bytesPerPixel = 4; // Используем 4 байта на пиксель (BGRA)
            byte[] buffer = new byte[tile.Bounds.Width * tile.Bounds.Height * bytesPerPixel];

            // Защита от деления на ноль.
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

        #region Supersampling (SSAA) Implementation

        /// <summary>
        /// Отрисовывает одну плитку с использованием суперсэмплинга (SSAA) для сглаживания.
        /// </summary>
        /// <param name="tile">Информация о плитке.</param>
        /// <param name="canvasWidth">Общая финальная ширина холста.</param>
        /// <param name="canvasHeight">Общая финальная высота холста.</param>
        /// <param name="supersamplingFactor">Фактор суперсэмплинга (например, 2 для 2x2 SSAA).</param>
        /// <param name="bytesPerPixel">Выходной параметр, возвращающий количество байтов на пиксель (всегда 4).</param>
        /// <returns>Массив байт с усредненными данными пикселей для плитки.</returns>
        public byte[] RenderSingleTileSSAA(TileInfo tile, int canvasWidth, int canvasHeight, int supersamplingFactor, out int bytesPerPixel)
        {
            bytesPerPixel = 4;
            if (supersamplingFactor <= 1)
            {
                return RenderSingleTile(tile, canvasWidth, canvasHeight, out bytesPerPixel);
            }

            byte[] finalTileBuffer = new byte[tile.Bounds.Width * tile.Bounds.Height * bytesPerPixel];

            // Защита от деления на ноль.
            if (canvasWidth <= 0 || canvasHeight <= 0) return finalTileBuffer;

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
                    int iterVal = GetIterationsForPoint(re, im);
                    highResColorBuffer[x, y] = Palette(iterVal, MaxIterations, MaxColorIterations);
                }
            });

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
            return finalTileBuffer;
        }

        #endregion

        /// <summary>
        /// Рендерит фрактал в новый объект Bitmap с использованием суперсэмплинга.
        /// </summary>
        /// <param name="finalWidth">Финальная ширина изображения.</param>
        /// <param name="finalHeight">Финальная высота изображения.</param>
        /// <param name="numThreads">Количество потоков для рендеринга.</param>
        /// <param name="reportProgressCallback">Callback для отчета о прогрессе (0-100).</param>
        /// <param name="supersamplingFactor">Фактор суперсэмплинга (1 = выкл).</param>
        /// <param name="cancellationToken">Токен для отмены операции.</param>
        /// <returns>Объект Bitmap с отрисованным фракталом.</returns>
        public Bitmap RenderToBitmapSSAA(int finalWidth, int finalHeight, int numThreads, Action<int> reportProgressCallback, int supersamplingFactor, CancellationToken cancellationToken = default)
        {
            if (finalWidth <= 0 || finalHeight <= 0) return new Bitmap(1, 1);

            if (supersamplingFactor <= 1)
            {
                return RenderToBitmap(finalWidth, finalHeight, numThreads, reportProgressCallback, cancellationToken);
            }

            int highResWidth = finalWidth * supersamplingFactor;
            int highResHeight = finalHeight * supersamplingFactor;
            Color[,] tempColorBuffer = new Color[highResWidth, highResHeight];
            ParallelOptions po = new ParallelOptions { MaxDegreeOfParallelism = numThreads };
            long doneLines = 0;
            decimal unitsPerPixel = Scale / finalWidth;

            Parallel.For(0, highResHeight, po, y =>
            {
                cancellationToken.ThrowIfCancellationRequested(); // <<< ИЗМЕНЕНИЕ
                for (int x = 0; x < highResWidth; x++)
                {
                    decimal re = CenterX + (x - highResWidth / 2.0m) * (unitsPerPixel / supersamplingFactor);
                    decimal im = CenterY - (y - highResHeight / 2.0m) * (unitsPerPixel / supersamplingFactor);
                    int iterVal = GetIterationsForPoint(re, im);
                    tempColorBuffer[x, y] = Palette(iterVal, MaxIterations, MaxColorIterations);
                }
                long currentDone = Interlocked.Increment(ref doneLines);
                if (highResHeight > 0) reportProgressCallback((int)(50.0 * currentDone / highResHeight));
            });

            cancellationToken.ThrowIfCancellationRequested(); // <<< ИЗМЕНЕНИЕ

            Bitmap bmp = new Bitmap(finalWidth, finalHeight, PixelFormat.Format24bppRgb);
            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, finalWidth, finalHeight), ImageLockMode.WriteOnly, bmp.PixelFormat);
            byte[] finalBuffer = new byte[Math.Abs(bmpData.Stride) * finalHeight];
            int sampleCount = supersamplingFactor * supersamplingFactor;
            doneLines = 0;

            Parallel.For(0, finalHeight, po, finalY =>
            {
                cancellationToken.ThrowIfCancellationRequested(); // <<< ИЗМЕНЕНИЕ
                int rowOffset = finalY * bmpData.Stride;
                for (int finalX = 0; finalX < finalWidth; finalX++)
                {
                    long totalR = 0, totalG = 0, totalB = 0;
                    int startX = finalX * supersamplingFactor;
                    int startY = finalY * supersamplingFactor;
                    for (int subY = 0; subY < supersamplingFactor; subY++)
                    {
                        for (int subX = 0; subX < supersamplingFactor; subX++)
                        {
                            Color pixelColor = tempColorBuffer[startX + subX, startY + subY];
                            totalR += pixelColor.R;
                            totalG += pixelColor.G;
                            totalB += pixelColor.B;
                        }
                    }
                    int index = rowOffset + finalX * 3;
                    finalBuffer[index] = (byte)(totalB / sampleCount);
                    finalBuffer[index + 1] = (byte)(totalG / sampleCount);
                    finalBuffer[index + 2] = (byte)(totalR / sampleCount);
                }
                long currentDone = Interlocked.Increment(ref doneLines);
                if (finalHeight > 0) reportProgressCallback(50 + (int)(50.0 * currentDone / finalHeight));
            });

            Marshal.Copy(finalBuffer, 0, bmpData.Scan0, finalBuffer.Length);
            bmp.UnlockBits(bmpData);
            return bmp;
        }

        /// <summary>
        /// Рендерит фрактал в новый объект Bitmap (без суперсэмплинга).
        /// </summary>
        /// <param name="renderWidth">Ширина изображения.</param>
        /// <param name="renderHeight">Высота изображения.</param>
        /// <param name="numThreads">Количество потоков.</param>
        /// <param name="reportProgressCallback">Callback для отчета о прогрессе.</param>
        /// <param name="cancellationToken">Токен для отмены операции.</param>
        /// <returns>Объект Bitmap с отрисованным фракталом.</returns>
        public Bitmap RenderToBitmap(int renderWidth, int renderHeight, int numThreads, Action<int> reportProgressCallback, CancellationToken cancellationToken = default)
        {
            if (renderWidth <= 0 || renderHeight <= 0) return new Bitmap(1, 1);

            Bitmap bmp = new Bitmap(renderWidth, renderHeight, PixelFormat.Format24bppRgb);
            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, renderWidth, renderHeight), ImageLockMode.WriteOnly, bmp.PixelFormat);
            byte[] buffer = new byte[Math.Abs(bmpData.Stride) * renderHeight];
            ParallelOptions po = new ParallelOptions { MaxDegreeOfParallelism = numThreads };
            long done = 0;
            decimal halfWidthPixels = renderWidth / 2.0m;
            decimal halfHeightPixels = renderHeight / 2.0m;
            decimal unitsPerPixel = Scale / renderWidth;

            Parallel.For(0, renderHeight, po, y =>
            {
                cancellationToken.ThrowIfCancellationRequested(); // <<< ИЗМЕНЕНИЕ
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
                if (renderHeight > 0) reportProgressCallback((int)(100.0 * currentDone / renderHeight));
            });

            Marshal.Copy(buffer, 0, bmpData.Scan0, buffer.Length);
            bmp.UnlockBits(bmpData);
            return bmp;
        }

        /// <summary>
        /// Абстрактный метод для определения, какой метод расчета итераций использовать в зависимости от типа фрактала.
        /// </summary>
        /// <param name="re">Действительная часть комплексной точки.</param>
        /// <param name="im">Мнимая часть комплексной точки.</param>
        /// <returns>Количество итераций до выхода из множества.</returns>
        protected abstract int GetIterationsForPoint(decimal re, decimal im);
    }

    #region Concrete Engines

    /// <summary>
    /// Движок рендеринга для множества Мандельброта.
    /// </summary>
    public class MandelbrotEngine : FractalMandelbrotFamilyEngine
    {
        /// <summary>
        /// Определяет количество итераций для точки Мандельброта (c = z0, z_init = 0).
        /// </summary>
        protected override int GetIterationsForPoint(decimal re, decimal im)
        {
            return CalculateIterations(ComplexDecimal.Zero, new ComplexDecimal(re, im));
        }

        /// <summary>
        /// Вычисляет итерации по формуле z = z^2 + c.
        /// </summary>
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

    /// <summary>
    /// Движок рендеринга для множества Жюлиа.
    /// </summary>
    public class JuliaEngine : FractalMandelbrotFamilyEngine
    {
        /// <summary>
        /// Определяет количество итераций для точки Жюлиа (z = z0, c = const).
        /// </summary>
        protected override int GetIterationsForPoint(decimal re, decimal im)
        {
            return CalculateIterations(new ComplexDecimal(re, im), C);
        }

        /// <summary>
        /// Вычисляет итерации по формуле z = z^2 + c.
        /// </summary>
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

    /// <summary>
    /// Движок рендеринга для фрактала "Пылающий корабль" Мандельброта.
    /// </summary>
    public class MandelbrotBurningShipEngine : FractalMandelbrotFamilyEngine
    {
        /// <summary>
        /// Определяет количество итераций для точки "Пылающего корабля" (c = z0, z_init = 0).
        /// </summary>
        protected override int GetIterationsForPoint(decimal re, decimal im)
        {
            return CalculateIterations(ComplexDecimal.Zero, new ComplexDecimal(re, im));
        }

        /// <summary>
        /// Вычисляет итерации по формуле "Пылающего корабля": z = (|Re(z)| + i*|Im(z)|)^2 + c.
        /// </summary>
        public override int CalculateIterations(ComplexDecimal z, ComplexDecimal c)
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
    }

    /// <summary>
    /// Движок рендеринга для фрактала "Пылающий корабль" Жюлиа.
    /// </summary>
    public class JuliaBurningShipEngine : FractalMandelbrotFamilyEngine
    {
        /// <summary>
        /// Определяет количество итераций для точки "Пылающего корабля" Жюлиа (z = z0, c = const).
        /// </summary>
        protected override int GetIterationsForPoint(decimal re, decimal im)
        {
            return CalculateIterations(new ComplexDecimal(re, im), C);
        }

        /// <summary>
        /// Вычисляет итерации по формуле "Пылающего корабля": z = (|Re(z)| + i*|Im(z)|)^2 + c.
        /// </summary>
        public override int CalculateIterations(ComplexDecimal z, ComplexDecimal c)
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
    }

    #endregion
}