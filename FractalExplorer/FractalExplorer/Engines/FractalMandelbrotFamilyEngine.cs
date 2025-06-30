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
    /// Абстрактный базовый класс для движков рендеринга фракталов.
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
        /// Константа 'C' для фракталов Жюлиа.
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
        /// Текущий масштаб рендеринга.
        /// </summary>
        public decimal Scale { get; set; }

        /// <summary>
        /// Функция палитры, используемая для преобразования количества итераций в цвет.
        /// </summary>
        public Func<int, int, int, Color> Palette { get; set; }

        /// <summary>
        /// Максимальное количество итераций для нормализации цвета в палитре.
        /// </summary>
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
        /// Отрисовывает одну плитку (тайл) в предоставленный буфер пикселей.
        /// </summary>
        /// <param name="buffer">Буфер байтов, в который будут записаны пиксельные данные.</param>
        /// <param name="stride">Длина строки в байтах (количество байтов на одну строку изображения).</param>
        /// <param name="bytesPerPixel">Количество байтов на один пиксель.</param>
        /// <param name="tile">Информация о плитке для отрисовки.</param>
        /// <param name="canvasWidth">Общая ширина холста.</param>
        /// <param name="canvasHeight">Общая высота холста.</param>
        public void RenderTile(byte[] buffer, int stride, int bytesPerPixel, TileInfo tile, int canvasWidth, int canvasHeight)
        {
            decimal halfWidthPixels = canvasWidth / 2.0m;
            decimal halfHeightPixels = canvasHeight / 2.0m;
            decimal unitsPerPixel = Scale / canvasWidth;

            for (int y = 0; y < tile.Bounds.Height; y++)
            {
                int canvasY = tile.Bounds.Y + y;
                if (canvasY >= canvasHeight)
                {
                    continue;
                }

                for (int x = 0; x < tile.Bounds.Width; x++)
                {
                    int canvasX = tile.Bounds.X + x;
                    if (canvasX >= canvasWidth)
                    {
                        continue;
                    }

                    decimal re = CenterX + (canvasX - halfWidthPixels) * unitsPerPixel;
                    decimal im = CenterY - (canvasY - halfHeightPixels) * unitsPerPixel;

                    int iter = GetIterationsForPoint(re, im);
                    Color pixelColor = Palette(iter, MaxIterations, MaxColorIterations);

                    int bufferIndex = canvasY * stride + canvasX * bytesPerPixel;
                    // Проверка границ буфера перед записью
                    if (bufferIndex + bytesPerPixel - 1 < buffer.Length)
                    {
                        buffer[bufferIndex] = pixelColor.B;
                        buffer[bufferIndex + 1] = pixelColor.G;
                        buffer[bufferIndex + 2] = pixelColor.R;
                        // Если используются 4 байта на пиксель, устанавливаем альфа-канал
                        if (bytesPerPixel == 4)
                        {
                            buffer[bufferIndex + 3] = pixelColor.A;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Отрисовывает одну плитку в ее собственный байтовый массив.
        /// </summary>
        /// <param name="tile">Информация о плитке для отрисовки.</param>
        /// <param name="canvasWidth">Общая ширина холста.</param>
        /// <param name="canvasHeight">Общая высота холста.</param>
        /// <param name="bytesPerPixel">Выходной параметр, возвращающий количество байтов на пиксель в созданном буфере.</param>
        /// <returns>Массив байт с данными пикселей плитки.</returns>
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
                if (canvasY >= canvasHeight)
                {
                    continue;
                }

                for (int x = 0; x < tile.Bounds.Width; x++)
                {
                    int canvasX = tile.Bounds.X + x;
                    if (canvasX >= canvasWidth)
                    {
                        continue;
                    }

                    decimal re = CenterX + (canvasX - halfWidthPixels) * unitsPerPixel;
                    decimal im = CenterY - (canvasY - halfHeightPixels) * unitsPerPixel;

                    int iter = GetIterationsForPoint(re, im);
                    Color pixelColor = Palette(iter, MaxIterations, MaxColorIterations);

                    int bufferIndex = (y * tile.Bounds.Width + x) * bytesPerPixel;
                    buffer[bufferIndex] = pixelColor.B;
                    buffer[bufferIndex + 1] = pixelColor.G;
                    buffer[bufferIndex + 2] = pixelColor.R;
                    buffer[bufferIndex + 3] = 255; // Alpha-канал, 255 = полностью непрозрачный
                }
            }
            return buffer;
        }

        #region Supersampling (SSAA) Implementation

        /// <summary>
        /// Отрисовывает одну плитку (тайл) с использованием суперсэмплинга (SSAA) для сглаживания.
        /// </summary>
        /// <param name="tile">Информация о плитке (ее финальные координаты и размер).</param>
        /// <param name="canvasWidth">Общая финальная ширина холста.</param>
        /// <param name="canvasHeight">Общая финальная высота холста.</param>
        /// <param name="supersamplingFactor">Фактор суперсэмплинга (например, 2 для 2x2 SSAA).</param>
        /// <param name="bytesPerPixel">Выходной параметр, возвращающий количество байтов на пиксель (всегда 4).</param>
        /// <returns>Массив байт с усредненными данными пикселей для плитки.</returns>
        public byte[] RenderSingleTileSSAA(TileInfo tile, int canvasWidth, int canvasHeight, int supersamplingFactor, out int bytesPerPixel)
        {
            bytesPerPixel = 4; // Используем 4 байта на пиксель для поддержки формата 32bppArgb (B, G, R, A)

            // Если суперсэмплинг не требуется, вызываем обычный, более быстрый метод.
            if (supersamplingFactor <= 1)
            {
                return RenderSingleTile(tile, canvasWidth, canvasHeight, out bytesPerPixel);
            }

            // --- 1. Первый проход: рендеринг в высоком разрешении ---

            // Размеры плитки в высоком разрешении
            int highResTileWidth = tile.Bounds.Width * supersamplingFactor;
            int highResTileHeight = tile.Bounds.Height * supersamplingFactor;

            // Временный буфер для хранения цветов каждого субпикселя этой плитки
            Color[,] highResColorBuffer = new Color[highResTileWidth, highResTileHeight];

            // Размеры всего холста в высоком разрешении
            long highResCanvasWidth = (long)canvasWidth * supersamplingFactor;
            long highResCanvasHeight = (long)canvasHeight * supersamplingFactor;

            // Единицы комплексной плоскости на один субпиксель
            decimal unitsPerSubPixel = Scale / highResCanvasWidth;

            // Половина размеров холста в субпикселях для центрирования
            decimal highResHalfWidthPixels = highResCanvasWidth / 2.0m;
            decimal highResHalfHeightPixels = highResCanvasHeight / 2.0m;

            // Параллельный рендеринг субпикселей внутри плитки
            Parallel.For(0, highResTileHeight, y =>
            {
                for (int x = 0; x < highResTileWidth; x++)
                {
                    // Вычисляем глобальные координаты субпикселя на всем холсте высокого разрешения
                    long globalHighResX = (long)tile.Bounds.X * supersamplingFactor + x;
                    long globalHighResY = (long)tile.Bounds.Y * supersamplingFactor + y;

                    // Преобразуем глобальные координаты субпикселя в комплексные координаты
                    decimal re = CenterX + (globalHighResX - highResHalfWidthPixels) * unitsPerSubPixel;
                    decimal im = CenterY - (globalHighResY - highResHalfHeightPixels) * unitsPerSubPixel; // Y инвертирован

                    int iterVal = GetIterationsForPoint(re, im);
                    highResColorBuffer[x, y] = Palette(iterVal, MaxIterations, MaxColorIterations);
                }
            });

            // --- 2. Второй проход: усреднение (Downsampling) ---

            // Финальный буфер для усредненных пикселей плитки
            byte[] finalTileBuffer = new byte[tile.Bounds.Width * tile.Bounds.Height * bytesPerPixel];
            int sampleCount = supersamplingFactor * supersamplingFactor;

            for (int finalY = 0; finalY < tile.Bounds.Height; finalY++)
            {
                for (int finalX = 0; finalX < tile.Bounds.Width; finalX++)
                {
                    // Используем long для сумм, чтобы избежать переполнения при больших факторах SSAA
                    long totalR = 0, totalG = 0, totalB = 0;

                    int startSubX = finalX * supersamplingFactor;
                    int startSubY = finalY * supersamplingFactor;

                    // Собираем цвета всех субпикселей для текущего финального пикселя
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

                    // Усредняем и записываем в финальный буфер
                    int bufferIndex = (finalY * tile.Bounds.Width + finalX) * bytesPerPixel;
                    finalTileBuffer[bufferIndex] = (byte)(totalB / sampleCount);
                    finalTileBuffer[bufferIndex + 1] = (byte)(totalG / sampleCount);
                    finalTileBuffer[bufferIndex + 2] = (byte)(totalR / sampleCount);
                    finalTileBuffer[bufferIndex + 3] = 255; // Alpha-канал
                }
            }

            return finalTileBuffer;
        }

        #endregion

        /// <summary>
        /// Рендерит фрактал в новый объект Bitmap, используя суперсэмплинг для улучшения качества.
        /// </summary>
        /// <param name="finalWidth">Финальная ширина генерируемого изображения в пикселях.</param>
        /// <param name="finalHeight">Финальная высота генерируемого изображения в пикселях.</param>
        /// <param name="numThreads">Количество потоков для использования в параллельных вычислениях.</param>
        /// <param name="reportProgressCallback">Callback-функция для отчета о прогрессе рендеринга (значение от 0 до 100).</param>
        /// <param name="supersamplingFactor">Фактор суперсэмплинга (1 = выкл, 2 = 4 сэмпла/пиксель, 3 = 9 сэмплов/пиксель).</param>
        /// <returns>Объект Bitmap, содержащий отрисованный фрактал с анти-алиасингом.</returns>
        public Bitmap RenderToBitmapSSAA(int finalWidth, int finalHeight, int numThreads, Action<int> reportProgressCallback, int supersamplingFactor)
        {
            if (finalWidth <= 0 || finalHeight <= 0)
            {
                return new Bitmap(1, 1);
            }

            // Если суперсэмплинг не требуется, вызываем обычный, более быстрый метод.
            if (supersamplingFactor <= 1)
            {
                return RenderToBitmap(finalWidth, finalHeight, numThreads, reportProgressCallback);
            }

            // --- 1. Первый проход: рендеринг в высоком разрешении ---
            int highResWidth = finalWidth * supersamplingFactor;
            int highResHeight = finalHeight * supersamplingFactor;

            // Временный буфер для хранения цветов каждого субпикселя
            Color[,] tempColorBuffer = new Color[highResWidth, highResHeight];

            ParallelOptions po = new ParallelOptions { MaxDegreeOfParallelism = numThreads };
            long doneLines = 0;

            // Единицы комплексной плоскости на пиксель рассчитываются относительно ФИНАЛЬНОЙ ширины
            decimal unitsPerPixel = Scale / finalWidth;

            Parallel.For(0, highResHeight, po, y =>
            {
                for (int x = 0; x < highResWidth; x++)
                {
                    // Вычисляем координаты для каждого субпикселя
                    decimal re = CenterX + (x - highResWidth / 2.0m) * (unitsPerPixel / supersamplingFactor);
                    decimal im = CenterY - (y - highResHeight / 2.0m) * (unitsPerPixel / supersamplingFactor);

                    int iterVal = GetIterationsForPoint(re, im);
                    tempColorBuffer[x, y] = Palette(iterVal, MaxIterations, MaxColorIterations);
                }

                long currentDone = Interlocked.Increment(ref doneLines);
                // Прогресс-бар обновляется на основе рендера в высоком разрешении (это занимает основное время)
                if (highResHeight > 0)
                {
                    // Делим прогресс на 2, так как будет еще второй проход (усреднение)
                    reportProgressCallback((int)(50.0 * currentDone / highResHeight));
                }
            });

            // --- 2. Второй проход: усреднение (Downsampling) ---
            Bitmap bmp = new Bitmap(finalWidth, finalHeight, PixelFormat.Format24bppRgb);
            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, finalWidth, finalHeight), ImageLockMode.WriteOnly, bmp.PixelFormat);
            int stride = bmpData.Stride;
            byte[] finalBuffer = new byte[Math.Abs(stride) * finalHeight];
            int sampleCount = supersamplingFactor * supersamplingFactor;

            doneLines = 0;

            Parallel.For(0, finalHeight, po, finalY =>
            {
                int rowOffset = finalY * stride;
                for (int finalX = 0; finalX < finalWidth; finalX++)
                {
                    // Используем long для сумм, чтобы избежать переполнения на больших факторах SSAA
                    long totalR = 0, totalG = 0, totalB = 0;

                    int startX = finalX * supersamplingFactor;
                    int startY = finalY * supersamplingFactor;

                    // Собираем цвета всех субпикселей для текущего финального пикселя
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

                    // Усредняем и записываем в финальный буфер
                    int index = rowOffset + finalX * 3;
                    finalBuffer[index] = (byte)(totalB / sampleCount);
                    finalBuffer[index + 1] = (byte)(totalG / sampleCount);
                    finalBuffer[index + 2] = (byte)(totalR / sampleCount);
                }

                long currentDone = Interlocked.Increment(ref doneLines);
                if (finalHeight > 0)
                {
                    // Вторая половина прогресса
                    reportProgressCallback(50 + (int)(50.0 * currentDone / finalHeight));
                }
            });

            Marshal.Copy(finalBuffer, 0, bmpData.Scan0, finalBuffer.Length);
            bmp.UnlockBits(bmpData);

            return bmp;
        }

        

        /// <summary>
        /// Рендерит фрактал в новый объект Bitmap, используя параллельные вычисления (без суперсэмплинга).
        /// </summary>
        /// <param name="renderWidth">Ширина генерируемого изображения в пикселях.</param>
        /// <param name="renderHeight">Высота генерируемого изображения в пикселях.</param>
        /// <param name="numThreads">Количество потоков для использования в параллельных вычислениях.</param>
        /// <param name="reportProgressCallback">Callback-функция для отчета о прогрессе рендеринга (значение от 0 до 100).</param>
        /// <returns>Объект Bitmap, содержащий отрисованный фрактал.</returns>
        public Bitmap RenderToBitmap(int renderWidth, int renderHeight, int numThreads, Action<int> reportProgressCallback)
        {
            if (renderWidth <= 0 || renderHeight <= 0)
            {
                return new Bitmap(1, 1);
            }

            Bitmap bmp = new Bitmap(renderWidth, renderHeight, PixelFormat.Format24bppRgb);
            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, renderWidth, renderHeight), ImageLockMode.WriteOnly, bmp.PixelFormat);
            int stride = bmpData.Stride;
            nint scan0 = bmpData.Scan0;
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
                    decimal im = CenterY - (y - halfHeightPixels) * unitsPerPixel;

                    int iterVal = GetIterationsForPoint(re, im);
                    Color pixelColor = Palette(iterVal, MaxIterations, MaxColorIterations);

                    int index = rowOffset + x * 3; // Предполагаем 3 байта на пиксель (24bpp)
                    // Проверка границ буфера
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

        /// <summary>
        /// Определяет, какой метод расчета итераций использовать в зависимости от типа фрактала.
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
        /// Определяет количество итераций для точки Мандельброта.
        /// </summary>
        /// <param name="re">Действительная часть начальной точки.</param>
        /// <param name="im">Мнимая часть начальной точки.</param>
        /// <returns>Количество итераций.</returns>
        protected override int GetIterationsForPoint(decimal re, decimal im)
        {
            var z0 = new ComplexDecimal(re, im);
            return CalculateIterations(ComplexDecimal.Zero, z0);
        }

        /// <summary>
        /// Вычисляет количество итераций для заданной комплексной точки по правилу Мандельброта (z = z^2 + c).
        /// </summary>
        /// <param name="z">Начальная точка итерации (в контексте Мандельброта, это начальное Z, обычно равно 0).</param>
        /// <param name="c">Константа для итерации (в контексте Мандельброта, это сама исследуемая точка).</param>
        /// <returns>Количество итераций.</returns>
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
        /// Определяет количество итераций для точки Жюлиа, используя фиксированную константу C.
        /// </summary>
        /// <param name="re">Действительная часть начальной точки (координата на экране).</param>
        /// <param name="im">Мнимая часть начальной точки (координата на экране).</param>
        /// <returns>Количество итераций.</returns>
        protected override int GetIterationsForPoint(decimal re, decimal im)
        {
            var z0 = new ComplexDecimal(re, im);
            // Для Жюлиа `c` - константа (из свойства C движка), а `z0` - точка на экране
            return CalculateIterations(z0, C);
        }

        /// <summary>
        /// Вычисляет количество итераций для заданной комплексной точки по правилу Жюлиа (z = z^2 + c).
        /// </summary>
        /// <param name="z">Начальная точка итерации (в контексте Жюлиа, это исследуемая точка).</param>
        /// <param name="c">Константа для итерации (в контексте Жюлиа, это константа C, заданная для множества).</param>
        /// <returns>Количество итераций.</returns>
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
        /// Определяет количество итераций для точки фрактала "Пылающий корабль" Мандельброта.
        /// </summary>
        /// <param name="re">Действительная часть начальной точки.</param>
        /// <param name="im">Мнимая часть начальной точки.</param>
        /// <returns>Количество итераций.</returns>
        protected override int GetIterationsForPoint(decimal re, decimal im)
        {
            var z0 = new ComplexDecimal(re, im);
            return CalculateIterations(ComplexDecimal.Zero, z0);
        }

        /// <summary>
        /// Вычисляет количество итераций для заданной комплексной точки по правилу "Пылающий корабль" (z = (|Re(z)| + i|Im(z)|)^2 + c).
        /// </summary>
        /// <param name="z">Начальная точка итерации (в контексте "Пылающего корабля" Мандельброта, это начальное Z, обычно равно 0).</param>
        /// <param name="c">Константа для итерации (в контексте "Пылающего корабля" Мандельброта, это сама исследуемая точка).</param>
        /// <returns>Количество итераций.</returns>
        public override int CalculateIterations(ComplexDecimal z, ComplexDecimal c)
        {
            int iter = 0;
            while (iter < MaxIterations && z.MagnitudeSquared <= ThresholdSquared)
            {
                // Примечание: данная реализация использует -Math.Abs(z.Imaginary), 
                // что соответствует инвертированному фракталу "Пылающий корабль".
                // Стандартный "Пылающий корабль" обычно использует Math.Abs(z.Imaginary).
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
        /// Определяет количество итераций для точки фрактала "Пылающий корабль" Жюлиа, используя фиксированную константу C.
        /// </summary>
        /// <param name="re">Действительная часть начальной точки (координата на экране).</param>
        /// <param name="im">Мнимая часть начальной точки (координата на экране).</param>
        /// <returns>Количество итераций.</returns>
        protected override int GetIterationsForPoint(decimal re, decimal im)
        {
            var z0 = new ComplexDecimal(re, im);
            return CalculateIterations(z0, C);
        }

        /// <summary>
        /// Вычисляет количество итераций для заданной комплексной точки по правилу "Пылающий корабль" Жюлиа (z = (|Re(z)| + i|Im(z)|)^2 + c).
        /// </summary>
        /// <param name="z">Начальная точка итерации (в контексте "Пылающего корабля" Жюлиа, это исследуемая точка).</param>
        /// <param name="c">Константа для итерации (в контексте "Пылающего корабля" Жюлиа, это константа C, заданная для множества).</param>
        /// <returns>Количество итераций.</returns>
        public override int CalculateIterations(ComplexDecimal z, ComplexDecimal c)
        {
            int iter = 0;
            while (iter < MaxIterations && z.MagnitudeSquared <= ThresholdSquared)
            {
                // Примечание: данная реализация использует -Math.Abs(z.Imaginary), 
                // что соответствует инвертированному фракталу "Пылающий корабль".
                // Стандартный "Пылающий корабль" обычно использует Math.Abs(z.Imaginary).
                z = new ComplexDecimal(Math.Abs(z.Real), -Math.Abs(z.Imaginary));
                z = z * z + c;
                iter++;
            }
            return iter;
        }
    }

    #endregion
}