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

        /// <summary>
        /// Рендерит фрактал в новый объект Bitmap, используя параллельные вычисления.
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