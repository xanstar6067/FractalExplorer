using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;

namespace FractalExplorer.Utilities.Imaging.Filters
{
    /// <summary>
    /// Применяет фильтр для изменения размера изображения с использованием бикубической интерполяции.
    /// </summary>
    public class BicubicResizeFilter : IImageFilter
    {
        /// <summary>
        /// Получает целевую ширину изображения.
        /// </summary>
        public int NewWidth { get; }

        /// <summary>
        /// Получает целевую высоту изображения.
        /// </summary>
        public int NewHeight { get; }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="BicubicResizeFilter"/>.
        /// </summary>
        /// <param name="newWidth">Новая ширина изображения в пикселях.</param>
        /// <param name="newHeight">Новая высота изображения в пикселях.</param>
        public BicubicResizeFilter(int newWidth, int newHeight)
        {
            NewWidth = newWidth;
            NewHeight = newHeight;
        }

        /// <summary>
        /// Применяет фильтр к исходному изображению.
        /// </summary>
        /// <param name="sourceImage">Исходное изображение для изменения размера.</param>
        /// <returns>Новое изображение с измененным размером.</returns>
        /// <exception cref="ArgumentNullException">Вызывается, если sourceImage равен null.</exception>
        public unsafe Bitmap Apply(Bitmap sourceImage)
        {
            if (sourceImage == null)
            {
                throw new ArgumentNullException(nameof(sourceImage));
            }

            var newBitmap = new Bitmap(NewWidth, NewHeight, sourceImage.PixelFormat);
            int sourceWidth = sourceImage.Width;
            int sourceHeight = sourceImage.Height;

            BitmapData sourceData = sourceImage.LockBits(
                new Rectangle(0, 0, sourceWidth, sourceHeight),
                ImageLockMode.ReadOnly, sourceImage.PixelFormat);

            BitmapData destData = newBitmap.LockBits(
                new Rectangle(0, 0, NewWidth, NewHeight),
                ImageLockMode.WriteOnly, newBitmap.PixelFormat);

            int bytesPerPixel = Image.GetPixelFormatSize(sourceImage.PixelFormat) / 8;
            int sourceStride = sourceData.Stride;
            int destStride = destData.Stride;

            IntPtr sourceScan0 = sourceData.Scan0;
            IntPtr destScan0 = destData.Scan0;

            float xRatio = (float)sourceWidth / NewWidth;
            float yRatio = (float)sourceHeight / NewHeight;

            Parallel.For(0, NewHeight, y =>
            {
                byte* destRow = (byte*)destScan0 + (y * destStride);

                for (int x = 0; x < NewWidth; x++)
                {
                    float sourceX = x * xRatio;
                    float sourceY = y * yRatio;

                    int ix = (int)sourceX;
                    int iy = (int)sourceY;

                    float fx = sourceX - ix;
                    float fy = sourceY - iy;

                    byte b = InterpolateChannel(sourceScan0, sourceWidth, sourceHeight, sourceStride, bytesPerPixel, 0, ix, iy, fx, fy);
                    byte g = InterpolateChannel(sourceScan0, sourceWidth, sourceHeight, sourceStride, bytesPerPixel, 1, ix, iy, fx, fy);
                    byte r = InterpolateChannel(sourceScan0, sourceWidth, sourceHeight, sourceStride, bytesPerPixel, 2, ix, iy, fx, fy);

                    byte a = (bytesPerPixel == 4)
                           ? InterpolateChannel(sourceScan0, sourceWidth, sourceHeight, sourceStride, bytesPerPixel, 3, ix, iy, fx, fy)
                           : (byte)255;

                    destRow[x * bytesPerPixel] = b;
                    destRow[x * bytesPerPixel + 1] = g;
                    destRow[x * bytesPerPixel + 2] = r;

                    if (bytesPerPixel == 4)
                    {
                        destRow[x * bytesPerPixel + 3] = a;
                    }
                }
            });

            sourceImage.UnlockBits(sourceData);
            newBitmap.UnlockBits(destData);

            return newBitmap;
        }

        /// <summary>
        /// Выполняет бикубическую интерполяцию для одного цветового канала.
        /// </summary>
        /// <param name="sourceScan0">Указатель на начало данных исходного изображения.</param>
        /// <param name="width">Ширина исходного изображения.</param>
        /// <param name="height">Высота исходного изображения.</param>
        /// <param name="stride">Stride (ширина строки в байтах) исходного изображения.</param>
        /// <param name="bpp">Количество байт на пиксель.</param>
        /// <param name="offset">Смещение цветового канала (0 - Blue, 1 - Green, 2 - Red, 3 - Alpha).</param>
        /// <param name="ix">Целая часть координаты X в исходном изображении.</param>
        /// <param name="iy">Целая часть координаты Y в исходном изображении.</param>
        /// <param name="fx">Дробная часть координаты X.</param>
        /// <param name="fy">Дробная часть координаты Y.</param>
        /// <returns>Интерполированное значение для канала в диапазоне от 0 до 255.</returns>
        private unsafe byte InterpolateChannel(IntPtr sourceScan0, int width, int height, int stride, int bpp, int offset, int ix, int iy, float fx, float fy)
        {
            byte* p = (byte*)sourceScan0;
            float[] col = new float[4];

            for (int j = 0; j < 4; j++)
            {
                int neighborY = iy - 1 + j;
                if (neighborY < 0) neighborY = 0;
                if (neighborY >= height) neighborY = height - 1;

                float[] row = new float[4];
                for (int i = 0; i < 4; i++)
                {
                    int neighborX = ix - 1 + i;
                    if (neighborX < 0) neighborX = 0;
                    if (neighborX >= width) neighborX = width - 1;

                    row[i] = p[neighborY * stride + neighborX * bpp + offset];
                }
                col[j] = CubicHermite(row[0], row[1], row[2], row[3], fx);
            }

            float value = CubicHermite(col[0], col[1], col[2], col[3], fy);

            if (value > 255) return 255;
            if (value < 0) return 0;

            return (byte)value;
        }

        /// <summary>
        /// Вычисляет значение кубического сплайна Эрмита (в данном случае, сплайна Катмулла-Рома) для 1D интерполяции.
        /// </summary>
        /// <param name="p0">Первая контрольная точка.</param>
        /// <param name="p1">Вторая контрольная точка (начало интервала).</param>
        /// <param name="p2">Третья контрольная точка (конец интервала).</param>
        /// <param name="p3">Четвертая контрольная точка.</param>
        /// <param name="t">Вес интерполяции (от 0 до 1).</param>
        /// <returns>Интерполированное значение.</returns>
        private float CubicHermite(float p0, float p1, float p2, float p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;

            float a = -0.5f * p0 + 1.5f * p1 - 1.5f * p2 + 0.5f * p3;
            float b = p0 - 2.5f * p1 + 2f * p2 - 0.5f * p3;
            float c = -0.5f * p0 + 0.5f * p2;
            float d = p1;

            return a * t3 + b * t2 + c * t + d;
        }
    }
}