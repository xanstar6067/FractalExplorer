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
    /// Поддерживает как дискретное, так и непрерывное (сглаженное) окрашивание.
    /// </summary>
    public abstract class FractalMandelbrotFamilyEngine
    {
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

        #region Core Calculation Logic

        /// <summary>
        /// Главный абстрактный метод вычисления. Для данной точки z0 и константы c,
        /// вычисляет количество итераций и возвращает его, а также изменяет z на его финальное значение.
        /// </summary>
        /// <param name="z">Начальная точка итерации (передается по ссылке и будет изменена).</param>
        /// <param name="c">Константа для итерации.</param>
        /// <returns>Целочисленное количество итераций.</returns>
        public abstract int CalculateIterations(ref ComplexDecimal z, ComplexDecimal c);

        /// <summary>
        /// Вычисляет "сглаженное" значение итерации на основе финального значения z.
        /// </summary>
        /// <param name="iter">Целочисленное количество итераций.</param>
        /// <param name="finalZ">Комплексное число в момент выхода за порог.</param>
        /// <returns>Дробное (сглаженное) значение итерации.</returns>
        private double CalculateSmoothValue(int iter, ComplexDecimal finalZ)
        {
            if (iter >= MaxIterations)
            {
                return iter; // Точка внутри множества, сглаживать нечего.
            }

            // ПРИМЕЧАНИЕ: Эта формула математически корректна только для фракталов вида z = z^2 + c.
            // Для "Пылающего Корабля" она даст неверный, но все же "какой-то" сглаженный результат.
            // Для правильного сглаживания Burning Ship требуются другие, более сложные алгоритмы.
            double log_zn_sq = Math.Log((double)finalZ.MagnitudeSquared);
            double nu = Math.Log(log_zn_sq / (2 * Math.Log(2))) / Math.Log(2);

            return iter + 1 - nu;
        }

        /// <summary>
        /// Определяет, какие данные о итерациях получить для точки (дискретные или сглаженные).
        /// </summary>
        /// <param name="re">Действительная часть комплексной точки.</param>
        /// <param name="im">Мнимая часть комплексной точки.</param>
        /// <param name="initialZ">Выходной параметр: начальное значение z для расчета.</param>
        /// <param name="constantC">Выходной параметр: константа c для расчета.</param>
        protected abstract void GetCalculationParameters(decimal re, decimal im, out ComplexDecimal initialZ, out ComplexDecimal constantC);

        #endregion

        #region Rendering Methods

        /// <summary>
        /// Отрисовывает одну плитку в ее собственный, отдельный байтовый массив.
        /// Это основной метод, используемый в приложении.
        /// </summary>
        public byte[] RenderSingleTile(TileInfo tile, int canvasWidth, int canvasHeight, out int bytesPerPixel)
        {
            bytesPerPixel = 4; // Используем 4 байта на пиксель (BGRA)
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

                    // Получаем параметры расчета для конкретного типа фрактала
                    GetCalculationParameters(re, im, out ComplexDecimal z, out ComplexDecimal c);

                    if (UseSmoothColoring && SmoothPalette != null)
                    {
                        // --- ПУТЬ ДЛЯ СГЛАЖЕННОГО ОКРАШИВАНИЯ ---
                        int iter = CalculateIterations(ref z, c); // z теперь содержит финальное значение
                        double smoothIter = CalculateSmoothValue(iter, z);
                        pixelColor = SmoothPalette(smoothIter);
                    }
                    else
                    {
                        // --- СТАРЫЙ ПУТЬ ДЛЯ ДИСКРЕТНОГО ОКРАШИВАНИЯ ---
                        int iter = CalculateIterations(ref z, c);
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
        /// Отрисовывает одну плитку с использованием суперсэмплинга (SSAA) для сглаживания.
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

            int highResTileWidth = tile.Bounds.Width * supersamplingFactor;
            int highResTileHeight = tile.Bounds.Height * supersamplingFactor;

            // Мы не можем хранить Color[,] для сглаживания, т.к. нужно усреднять итерации, а не цвета.
            // Поэтому будем рендерить в высоком разрешении и потом усреднять.
            // Этот метод нуждается в адаптации под сглаженное окрашивание, но для начала оставим его
            // работающим по старому принципу, чтобы не усложнять.

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

                    if (UseSmoothColoring && SmoothPalette != null)
                    {
                        int iter = CalculateIterations(ref z, c);
                        double smoothIter = CalculateSmoothValue(iter, z);
                        highResColorBuffer[x, y] = SmoothPalette(smoothIter);
                    }
                    else
                    {
                        int iter = CalculateIterations(ref z, c);
                        highResColorBuffer[x, y] = Palette(iter, MaxIterations, MaxColorIterations);
                    }
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
            return finalTileBuffer;
        }

        /// <summary>
        /// Рендерит фрактал в новый объект Bitmap (без суперсэмплинга).
        /// </summary>
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
                cancellationToken.ThrowIfCancellationRequested();
                int rowOffset = y * bmpData.Stride;
                for (int x = 0; x < renderWidth; x++)
                {
                    decimal re = CenterX + (x - halfWidthPixels) * unitsPerPixel;
                    decimal im = CenterY - (y - halfHeightPixels) * unitsPerPixel;

                    Color pixelColor;
                    GetCalculationParameters(re, im, out ComplexDecimal z, out ComplexDecimal c);

                    if (UseSmoothColoring && SmoothPalette != null)
                    {
                        int iter = CalculateIterations(ref z, c);
                        double smoothIter = CalculateSmoothValue(iter, z);
                        pixelColor = SmoothPalette(smoothIter);
                    }
                    else
                    {
                        int iter = CalculateIterations(ref z, c);
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
                if (renderHeight > 0) reportProgressCallback((int)(100.0 * currentDone / renderHeight));
            });

            Marshal.Copy(buffer, 0, bmpData.Scan0, buffer.Length);
            bmp.UnlockBits(bmpData);
            return bmp;
        }

        // Метод RenderToBitmapSSAA не показан для краткости, но он должен быть изменен аналогично RenderToBitmap

        #endregion
    }

    #region Concrete Engines

    /// <summary>
    /// Движок рендеринга для множества Мандельброта.
    /// </summary>
    public class MandelbrotEngine : FractalMandelbrotFamilyEngine
    {
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
    }

    /// <summary>
    /// Движок рендеринга для множества Жюлиа.
    /// </summary>
    public class JuliaEngine : FractalMandelbrotFamilyEngine
    {
        protected override void GetCalculationParameters(decimal re, decimal im, out ComplexDecimal initialZ, out ComplexDecimal constantC)
        {
            initialZ = new ComplexDecimal(re, im);
            constantC = this.C; // Используем константу, заданную в UI
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
    }

    /// <summary>
    /// Движок рендеринга для фрактала "Пылающий корабль" Мандельброта.
    /// </summary>
    public class MandelbrotBurningShipEngine : FractalMandelbrotFamilyEngine
    {
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
    }

    #endregion
}