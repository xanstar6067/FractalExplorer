using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;

namespace FractalExplorer.Utilities.Imaging.Filters
{
    /// <summary>
    /// Фильтр для изменения размера изображения с использованием интерполяции Ланцоша.
    /// Обеспечивает высокое качество, хорошо сохраняя детализацию.
    /// </summary>
    public class Lanczos3ResizeFilter : IImageFilter
    {
        public int NewWidth { get; }
        public int NewHeight { get; }

        /// <summary>
        /// Размер ядра фильтра (оконная функция). Обычно используется значение 2 или 3.
        /// Значение 3 дает более резкий результат, но может вызывать артефакты (звон).
        /// </summary>
        public int A { get; }

        /// <summary>
        /// Создает новый экземпляр фильтра Ланцоша.
        /// </summary>
        /// <param name="newWidth">Новая ширина изображения.</param>
        /// <param name="newHeight">Новая высота изображения.</param>
        /// <param name="a">Параметр 'a' для ядра Ланцоша (рекомендуется 3 для лучшего качества).</param>
        public Lanczos3ResizeFilter(int newWidth, int newHeight, int a = 3)
        {
            if (newWidth <= 0 || newHeight <= 0)
                throw new ArgumentException("Новые размеры должны быть положительными числами.");
            if (a <= 0)
                throw new ArgumentException("Параметр 'a' должен быть положительным целым числом.");

            NewWidth = newWidth;
            NewHeight = newHeight;
            A = a;
        }

        /// <summary>
        /// Применяет фильтр к исходному изображению.
        /// </summary>
        /// <param name="sourceImage">Исходное изображение.</param>
        /// <returns>Новое изображение с примененным фильтром.</returns>
        public unsafe Bitmap Apply(Bitmap sourceImage)
        {
            if (sourceImage == null) throw new ArgumentNullException(nameof(sourceImage));

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
                    // Для большей точности вычисляем координаты с учетом центров пикселей.
                    float sourceX = (x + 0.5f) * xRatio - 0.5f;
                    float sourceY = (y + 0.5f) * yRatio - 0.5f;

                    float b = InterpolateChannel(sourceScan0, sourceWidth, sourceHeight, sourceStride, bytesPerPixel, 0, sourceX, sourceY);
                    float g = InterpolateChannel(sourceScan0, sourceWidth, sourceHeight, sourceStride, bytesPerPixel, 1, sourceX, sourceY);
                    float r = InterpolateChannel(sourceScan0, sourceWidth, sourceHeight, sourceStride, bytesPerPixel, 2, sourceX, sourceY);
                    float a = 255f;

                    if (bytesPerPixel == 4)
                    {
                        a = InterpolateChannel(sourceScan0, sourceWidth, sourceHeight, sourceStride, bytesPerPixel, 3, sourceX, sourceY);
                    }

                    destRow[x * bytesPerPixel] = (byte)Math.Max(0, Math.Min(255, b));
                    destRow[x * bytesPerPixel + 1] = (byte)Math.Max(0, Math.Min(255, g));
                    destRow[x * bytesPerPixel + 2] = (byte)Math.Max(0, Math.Min(255, r));

                    if (bytesPerPixel == 4)
                    {
                        destRow[x * bytesPerPixel + 3] = (byte)Math.Max(0, Math.Min(255, a));
                    }
                }
            });

            sourceImage.UnlockBits(sourceData);
            newBitmap.UnlockBits(destData);

            return newBitmap;
        }

        /// <summary>
        /// Реализация функции ядра (кернела) Ланцоша.
        /// </summary>
        private float LanczosKernel(float x)
        {
            if (x == 0.0f) return 1.0f;
            // Ядро определено только в интервале (-A, A)
            if (Math.Abs(x) >= A) return 0.0f;

            float pi_x = (float)Math.PI * x;
            // Формула: sinc(pi*x) * sinc(pi*x/a)
            return (float)(A * Math.Sin(pi_x) * Math.Sin(pi_x / A) / (pi_x * pi_x));
        }

        /// <summary>
        /// Вычисляет значение одного цветового канала для нового пикселя.
        /// </summary>
        private unsafe float InterpolateChannel(IntPtr sourceScan0, int width, int height, int stride, int bpp, int offset, float sx, float sy)
        {
            byte* sourcePointer = (byte*)sourceScan0;
            float totalWeight = 0.0f;
            float sum = 0.0f;

            int ix_center = (int)sx;
            int iy_center = (int)sy;

            // Итерируем по окрестности пикселя размером (2*A) x (2*A)
            for (int y = iy_center - A + 1; y <= iy_center + A; y++)
            {
                // Проверка границ по Y
                if (y < 0 || y >= height) continue;

                float y_dist = sy - y;
                float y_weight = LanczosKernel(y_dist);

                if (y_weight == 0) continue;

                for (int x = ix_center - A + 1; x <= ix_center + A; x++)
                {
                    // Проверка границ по X
                    if (x < 0 || x >= width) continue;

                    float x_dist = sx - x;
                    float x_weight = LanczosKernel(x_dist);

                    if (x_weight == 0) continue;

                    float weight = x_weight * y_weight;

                    // Добавляем взвешенное значение пикселя из исходного изображения
                    sum += sourcePointer[y * stride + x * bpp + offset] * weight;
                    totalWeight += weight;
                }
            }

            // Нормализуем результат
            return (totalWeight == 0) ? 0 : sum / totalWeight;
        }
    }
}