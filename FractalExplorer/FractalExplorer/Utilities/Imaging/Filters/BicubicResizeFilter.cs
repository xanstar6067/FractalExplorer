using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;

namespace FractalExplorer.Utilities.Imaging.Filters
{
    /// <summary>
    /// Фильтр для изменения размера изображения с использованием бикубической интерполяции.
    /// </summary>
    public class BicubicResizeFilter : IImageFilter
    {
        public int NewWidth { get; }
        public int NewHeight { get; }

        public BicubicResizeFilter(int newWidth, int newHeight)
        {
            NewWidth = newWidth;
            NewHeight = newHeight;
        }

        public unsafe Bitmap Apply(Bitmap sourceImage)
        {
            if (sourceImage == null) throw new ArgumentNullException(nameof(sourceImage));

            var newBitmap = new Bitmap(NewWidth, NewHeight, sourceImage.PixelFormat);

            // ИСПРАВЛЕНИЕ: Считываем размеры ДО блокировки и цикла.
            int sourceWidth = sourceImage.Width;
            int sourceHeight = sourceImage.Height;

            BitmapData sourceData = sourceImage.LockBits(
                new Rectangle(0, 0, sourceWidth, sourceHeight), // Используем локальную переменную
                ImageLockMode.ReadOnly, sourceImage.PixelFormat);

            BitmapData destData = newBitmap.LockBits(
                new Rectangle(0, 0, NewWidth, NewHeight),
                ImageLockMode.WriteOnly, newBitmap.PixelFormat);

            int bytesPerPixel = Image.GetPixelFormatSize(sourceImage.PixelFormat) / 8;
            int sourceStride = sourceData.Stride;
            int destStride = destData.Stride;

            IntPtr sourceScan0 = sourceData.Scan0;
            IntPtr destScan0 = destData.Scan0;

            // ИСПРАВЛЕНИЕ: Используем локальные переменные
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

                    // ИСПРАВЛЕНИЕ: Передаем в метод локальные переменные, а не свойства объекта
                    byte b = InterpolateChannel(sourceScan0, sourceWidth, sourceHeight, sourceStride, bytesPerPixel, 0, ix, iy, fx, fy);
                    byte g = InterpolateChannel(sourceScan0, sourceWidth, sourceHeight, sourceStride, bytesPerPixel, 1, ix, iy, fx, fy);
                    byte r = InterpolateChannel(sourceScan0, sourceWidth, sourceHeight, sourceStride, bytesPerPixel, 2, ix, iy, fx, fy);
                    byte a = (bytesPerPixel == 4) ?
                             InterpolateChannel(sourceScan0, sourceWidth, sourceHeight, sourceStride, bytesPerPixel, 3, ix, iy, fx, fy)
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