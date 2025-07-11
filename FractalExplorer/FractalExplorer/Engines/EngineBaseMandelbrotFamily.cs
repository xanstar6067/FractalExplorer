using FractalExplorer.Engines.EngineInterfaces;
using FractalExplorer.Resources;
using FractalExplorer.Utilities;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace FractalExplorer.Engines
{
    /// <summary>
    /// Абстрактный базовый класс, реализующий общую логику рендеринга.
    /// </summary>
    public abstract class EngineBase : IFractalEngine
    {
        public int MaxIterations { get; set; }
        public double ThresholdSquared { get; set; } = 4.0;
        public Func<int, int, int, Color> Palette { get; set; }
        public int MaxColorIterations { get; set; }

        public Bitmap Render(RenderOptions options, CancellationToken cancellationToken)
        {
            int finalWidth = options.Width;
            int finalHeight = options.Height;
            int ssaa = options.SsaaFactor;

            int renderWidth = finalWidth * ssaa;
            int renderHeight = finalHeight * ssaa;

            if (renderWidth <= 0 || renderHeight <= 0) return new Bitmap(1, 1);

            // Создаем временный буфер для цветов, чтобы избежать проблем с потоками
            Color[,] colorBuffer = new Color[renderWidth, renderHeight];

            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = options.NumThreads,
                CancellationToken = cancellationToken
            };

            // Главный цикл рендеринга
            Parallel.For(0, renderHeight, parallelOptions, y =>
            {
                for (int x = 0; x < renderWidth; x++)
                {
                    // Абстрактный метод, который каждый движок реализует по-своему
                    int iterations = GetIterationsForPixel(x, y, renderWidth, renderHeight, options);
                    colorBuffer[x, y] = Palette(iterations, MaxIterations, MaxColorIterations);
                }
            });

            // Создаем финальный битмап и усредняем цвета, если SSAA > 1
            return CreateBitmapFromColorBuffer(colorBuffer, finalWidth, finalHeight, ssaa);
        }

        /// <summary>
        /// Каждый дочерний класс должен реализовать этот метод для вычисления
        /// итераций для конкретного пикселя, используя свою числовую точность.
        /// </summary>
        protected abstract int GetIterationsForPixel(int px, int py, int width, int height, RenderOptions options);

        /// <summary>
        /// Вспомогательный метод для создания Bitmap из буфера цветов с учетом SSAA.
        /// </summary>
        private Bitmap CreateBitmapFromColorBuffer(Color[,] buffer, int finalWidth, int finalHeight, int ssaa)
        {
            Bitmap bmp = new Bitmap(finalWidth, finalHeight, PixelFormat.Format24bppRgb);
            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, finalWidth, finalHeight), ImageLockMode.WriteOnly, bmp.PixelFormat);
            byte[] finalBuffer = new byte[Math.Abs(bmpData.Stride) * finalHeight];
            int sampleCount = ssaa * ssaa;

            Parallel.For(0, finalHeight, y =>
            {
                int rowOffset = y * bmpData.Stride;
                for (int x = 0; x < finalWidth; x++)
                {
                    if (ssaa == 1)
                    {
                        Color c = buffer[x, y];
                        int index = rowOffset + x * 3;
                        finalBuffer[index] = c.B;
                        finalBuffer[index + 1] = c.G;
                        finalBuffer[index + 2] = c.R;
                    }
                    else // Усреднение для SSAA
                    {
                        long totalR = 0, totalG = 0, totalB = 0;
                        for (int subY = 0; subY < ssaa; subY++)
                        {
                            for (int subX = 0; subX < ssaa; subX++)
                            {
                                Color pixelColor = buffer[x * ssaa + subX, y * ssaa + subY];
                                totalR += pixelColor.R;
                                totalG += pixelColor.G;
                                totalB += pixelColor.B;
                            }
                        }
                        int index = rowOffset + x * 3;
                        finalBuffer[index] = (byte)(totalB / sampleCount);
                        finalBuffer[index + 1] = (byte)(totalG / sampleCount);
                        finalBuffer[index + 2] = (byte)(totalR / sampleCount);
                    }
                }
            });

            Marshal.Copy(finalBuffer, 0, bmpData.Scan0, finalBuffer.Length);
            bmp.UnlockBits(bmpData);
            return bmp;
        }
    }
}
