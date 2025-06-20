using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace FractalExplorer.Engines
{
    // Перечисления для более чистого кода
    public enum SerpinskyRenderMode { Geometric, Chaos }
    public enum SerpinskyColorMode { BlackAndWhite, Grayscale, CustomColor }

    public class SerpinskyFractalEngine
    {
        // --- Параметры фрактала ---
        public SerpinskyRenderMode RenderMode { get; set; } = SerpinskyRenderMode.Geometric;
        public int Iterations { get; set; } = 5;

        // --- Параметры вида ---
        public double Zoom { get; set; } = 1.0;
        public double CenterX { get; set; } = 0.0;
        public double CenterY { get; set; } = 0.0;
        private const double BASE_SCALE = 1.0;

        // --- Параметры цвета ---
        public SerpinskyColorMode ColorMode { get; set; } = SerpinskyColorMode.Grayscale;
        public Color FractalColor { get; set; } = Color.Black;
        public Color BackgroundColor { get; set; } = Color.White;

        /// <summary>
        /// Основной метод рендеринга, который вызывает соответствующий внутренний метод.
        /// </summary>
        public void RenderToBuffer(byte[] buffer, int width, int height, int stride, int bytesPerPixel,
                                   int numThreads, CancellationToken token, Action<int> reportProgress)
        {
            // Определяем эффективные цвета на основе выбранного режима
            Color effectiveBgColor, effectiveFrColor;
            switch (ColorMode)
            {
                case SerpinskyColorMode.BlackAndWhite:
                    effectiveBgColor = Color.White;
                    effectiveFrColor = Color.Black;
                    break;
                case SerpinskyColorMode.Grayscale:
                    effectiveBgColor = Color.White;
                    effectiveFrColor = Color.FromArgb(255, 50, 50, 50);
                    break;
                case SerpinskyColorMode.CustomColor:
                default:
                    effectiveBgColor = BackgroundColor;
                    effectiveFrColor = FractalColor;
                    break;
            }

            // 1. Заполняем фон
            FillBackground(buffer, width, height, stride, bytesPerPixel, effectiveBgColor, token);
            token.ThrowIfCancellationRequested();

            // 2. Вызываем нужный рендерер
            if (RenderMode == SerpinskyRenderMode.Geometric)
            {
                RenderGeometric(token, buffer, width, height, stride, bytesPerPixel, effectiveFrColor, numThreads, reportProgress);
            }
            else // Chaos
            {
                RenderChaos(token, buffer, width, height, stride, bytesPerPixel, effectiveFrColor, numThreads, reportProgress);
            }
        }

        private void FillBackground(byte[] buffer, int W, int H, int stride, int bpp, Color color, CancellationToken token)
        {
            for (int y = 0; y < H; y++)
            {
                token.ThrowIfCancellationRequested();
                for (int x = 0; x < W; x++)
                {
                    int idx = y * stride + x * bpp;
                    buffer[idx + 0] = color.B;
                    buffer[idx + 1] = color.G;
                    buffer[idx + 2] = color.R;
                    buffer[idx + 3] = color.A;
                }
            }
        }

        private void RenderGeometric(CancellationToken token, byte[] buffer, int W, int H, int stride, int bpp,
                                     Color frColor, int numThreads, Action<int> reportProgress)
        {
            if (Iterations < 0)
            {
                reportProgress(100);
                return;
            }
            token.ThrowIfCancellationRequested();

            double side = 1.0;
            double height_triangle = side * Math.Sqrt(3) / 2.0;
            PointF p1_world = new PointF(0, (float)(height_triangle * 2.0 / 3.0));
            PointF p2_world = new PointF((float)(-side / 2.0), (float)(-height_triangle / 3.0));
            PointF p3_world = new PointF((float)(side / 2.0), (float)(-height_triangle / 3.0));

            Action<int, PointF, PointF, PointF> drawTriangleBranch = null;
            drawTriangleBranch = (d, pA, pB, pC) =>
            {
                if (token.IsCancellationRequested) return;

                if (d == 0)
                {
                    PointF sP1 = WorldToScreen(pA, W, H);
                    PointF sP2 = WorldToScreen(pB, W, H);
                    PointF sP3 = WorldToScreen(pC, W, H);
                    FillTriangleToBuffer(buffer, W, H, stride, bpp, sP1, sP2, sP3, frColor);
                    return;
                }
                PointF pAB = MidPoint(pA, pB); PointF pBC = MidPoint(pB, pC); PointF pCA = MidPoint(pC, pA);
                drawTriangleBranch(d - 1, pA, pAB, pCA);
                drawTriangleBranch(d - 1, pAB, pB, pBC);
                drawTriangleBranch(d - 1, pCA, pBC, pC);
            };

            var topLevelTasks = new List<Tuple<int, PointF, PointF, PointF>>();
            int parallelDepth = Math.Min(Iterations, 5); // 3^5 = 243 задачи

            Action<int, PointF, PointF, PointF> generateTopLevelTasks = null;
            generateTopLevelTasks = (d, pA, pB, pC) =>
            {
                if (token.IsCancellationRequested) return;
                if (d == 0)
                {
                    topLevelTasks.Add(Tuple.Create(Iterations - parallelDepth, pA, pB, pC));
                    return;
                }
                PointF pAB = MidPoint(pA, pB); PointF pBC = MidPoint(pB, pC); PointF pCA = MidPoint(pC, pA);
                generateTopLevelTasks(d - 1, pA, pAB, pCA);
                generateTopLevelTasks(d - 1, pAB, pB, pBC);
                generateTopLevelTasks(d - 1, pCA, pBC, pC);
            };

            if (Iterations == 0)
            {
                drawTriangleBranch(0, p1_world, p2_world, p3_world);
                reportProgress(100);
                return;
            }

            generateTopLevelTasks(parallelDepth, p1_world, p2_world, p3_world);
            token.ThrowIfCancellationRequested();

            var po = new ParallelOptions { MaxDegreeOfParallelism = numThreads, CancellationToken = token };
            long tasksDone = 0;

            try
            {
                Parallel.ForEach(topLevelTasks, po, task =>
                {
                    drawTriangleBranch(task.Item1, task.Item2, task.Item3, task.Item4);
                    long currentDone = Interlocked.Increment(ref tasksDone);
                    reportProgress((int)(100.0 * currentDone / topLevelTasks.Count));
                });
            }
            catch (OperationCanceledException) { /* Ignore */ }
        }

        private void RenderChaos(CancellationToken token, byte[] buffer, int W, int H, int stride, int bpp,
                                 Color frColor, int numThreads, Action<int> reportProgress)
        {
            byte cB = frColor.B; byte cG = frColor.G; byte cR = frColor.R; byte cA = frColor.A;
            double side = 1.0; double height_triangle = side * Math.Sqrt(3) / 2.0;
            PointF[] vertices_world = {
                new PointF(0, (float)(height_triangle * 2.0 / 3.0)),
                new PointF((float)(-side / 2.0), (float)(-height_triangle / 3.0)),
                new PointF((float)(side / 2.0), (float)(-height_triangle / 3.0))
            };

            Random masterRand = new Random();
            PointF initialPoint_world = vertices_world[0];
            for (int i = 0; i < 20; ++i) { initialPoint_world = MidPoint(initialPoint_world, vertices_world[masterRand.Next(3)]); }

            long totalDrawnPoints = 0;
            int numPoints = Iterations;
            int pointsPerThread = Math.Max(1, numPoints / numThreads);

            Parallel.For(0, numThreads, new ParallelOptions { MaxDegreeOfParallelism = numThreads, CancellationToken = token }, threadId =>
            {
                Random localRand = new Random(masterRand.Next() + threadId);
                PointF localCurrentPoint_world = initialPoint_world;
                if (threadId > 0) { for (int i = 0; i < 5 + threadId; ++i) { localCurrentPoint_world = MidPoint(localCurrentPoint_world, vertices_world[localRand.Next(3)]); } }

                for (int i = 0; i < pointsPerThread; i++)
                {
                    if (token.IsCancellationRequested) break;
                    localCurrentPoint_world = MidPoint(localCurrentPoint_world, vertices_world[localRand.Next(3)]);
                    Point screenPoint = Point.Round(WorldToScreen(localCurrentPoint_world, W, H));
                    if (screenPoint.X >= 0 && screenPoint.X < W && screenPoint.Y >= 0 && screenPoint.Y < H)
                    {
                        int idx = screenPoint.Y * stride + screenPoint.X * bpp;
                        buffer[idx + 0] = cB; buffer[idx + 1] = cG; buffer[idx + 2] = cR; buffer[idx + 3] = cA;
                    }
                }
                long drawn = Interlocked.Add(ref totalDrawnPoints, pointsPerThread);
                if (numPoints > 0) reportProgress((int)Math.Min(100, (100L * drawn / numPoints)));
            });

            if (!token.IsCancellationRequested) reportProgress(100);
        }

        // --- Вспомогательные методы ---
        private PointF WorldToScreen(PointF worldPoint, int screenWidth, int screenHeight)
        {
            double aspect = (double)screenWidth / screenHeight;
            double viewHeightWorld = BASE_SCALE / Zoom;
            double viewWidthWorld = viewHeightWorld * aspect;
            double minRe = CenterX - viewWidthWorld / 2.0;
            double maxIm = CenterY + viewHeightWorld / 2.0;

            float screenX = (float)(((worldPoint.X - minRe) / viewWidthWorld) * screenWidth);
            float screenY = (float)(((maxIm - worldPoint.Y) / viewHeightWorld) * screenHeight);

            return new PointF(screenX, screenY);
        }

        private PointF MidPoint(PointF p1, PointF p2) => new PointF((p1.X + p2.X) / 2f, (p1.Y + p2.Y) / 2f);

        private void FillTriangleToBuffer(byte[] buffer, int W, int H, int stride, int bpp, PointF p1, PointF p2, PointF p3, Color color)
        {
            PointF[] v = { p1, p2, p3 };
            Array.Sort(v, (a, b) => a.Y.CompareTo(b.Y));
            PointF vTop = v[0], vMid = v[1], vBot = v[2];
            byte cB = color.B, cG = color.G, cR = color.R, cA = color.A;

            Action<float, float, float> fillScanline = (yScan, xStart, xEnd) =>
            {
                if (yScan < 0 || yScan >= H) return;
                int startX = (int)Math.Max(0, Math.Min(xStart, xEnd));
                int endX = (int)Math.Min(W - 1, Math.Max(xStart, xEnd));
                for (int x = startX; x <= endX; x++)
                {
                    int idx = (int)yScan * stride + x * bpp;
                    buffer[idx + 0] = cB; buffer[idx + 1] = cG; buffer[idx + 2] = cR; buffer[idx + 3] = cA;
                }
            };

            if (vMid.Y - vTop.Y > 0.0001f)
            {
                float invSlope1 = (vMid.X - vTop.X) / (vMid.Y - vTop.Y);
                float invSlope2 = (vBot.X - vTop.X) / (vBot.Y - vTop.Y);
                float curX1 = vTop.X, curX2 = vTop.X;
                for (float y = vTop.Y; y < vMid.Y; y++) { fillScanline(y, curX1, curX2); curX1 += invSlope1; curX2 += invSlope2; }
            }
            if (vBot.Y - vMid.Y > 0.0001f)
            {
                float invSlope1 = (vBot.X - vMid.X) / (vBot.Y - vMid.Y);
                float invSlope2 = (vBot.X - vTop.X) / (vBot.Y - vTop.Y);
                float curX1 = vMid.X;
                float curX2 = (Math.Abs(vBot.Y - vTop.Y) > 0.0001f) ? vTop.X + (vMid.Y - vTop.Y) * invSlope2 : vTop.X;
                for (float y = vMid.Y; y <= vBot.Y; y++) { fillScanline(y, curX1, curX2); curX1 += invSlope1; if (Math.Abs(vBot.Y - vTop.Y) > 0.0001f) curX2 += invSlope2; }
            }
        }
    }
}