using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace FractalExplorer.Engines
{
    #region Enums

    /// <summary>
    /// Определяет режим рендеринга для фрактала Серпинского.
    /// </summary>
    public enum SerpinskyRenderMode
    {
        /// <summary>
        /// Геометрический режим рендеринга, рисует заполненные треугольники.
        /// </summary>
        Geometric,
        /// <summary>
        /// Режим "Игра Хаоса", рисует отдельные точки.
        /// </summary>
        Chaos
    }

    /// <summary>
    /// Определяет режим цветового оформления для фрактала Серпинского.
    /// </summary>
    public enum SerpinskyColorMode
    {
        /// <summary>
        /// Рендеринг в черно-белом цвете.
        /// </summary>
        BlackAndWhite,
        /// <summary>
        /// Рендеринг в оттенках серого.
        /// </summary>
        Grayscale,
        /// <summary>
        /// Рендеринг с использованием пользовательских цветов.
        /// </summary>
        CustomColor
    }

    #endregion

    /// <summary>
    /// Движок для рендеринга фрактала Серпинского в различных режимах.
    /// Поддерживает геометрический рендеринг и метод "Игры Хаоса".
    /// </summary>
    public class FractalSerpinskyEngine
    {
        #region Fields

        /// <summary>
        /// Режим рендеринга фрактала Серпинского (Geometric или Chaos).
        /// </summary>
        public SerpinskyRenderMode RenderMode { get; set; } = SerpinskyRenderMode.Geometric;

        /// <summary>
        /// Количество итераций для построения фрактала.
        /// В геометрическом режиме определяет глубину рекурсии.
        /// В режиме хаоса определяет целевое количество точек для отрисовки.
        /// </summary>
        public int Iterations { get; set; } = 5;

        /// <summary>
        /// Коэффициент масштабирования для видимой области.
        /// </summary>
        public double Zoom { get; set; } = 1.0;

        /// <summary>
        /// Координата X центра видимой области фрактала.
        /// </summary>
        public double CenterX { get; set; } = 0.0;

        /// <summary>
        /// Координата Y центра видимой области фрактала.
        /// </summary>
        public double CenterY { get; set; } = 0.0;

        /// <summary>
        /// Базовый масштаб для преобразования мировых координат в экранные.
        /// </summary>
        private const double BASE_SCALE = 1.0;

        /// <summary>
        /// Режим цветового оформления фрактала (BlackAndWhite, Grayscale, CustomColor).
        /// </summary>
        public SerpinskyColorMode ColorMode { get; set; } = SerpinskyColorMode.Grayscale;

        /// <summary>
        /// Основной цвет фрактала при использовании CustomColor.
        /// </summary>
        public Color FractalColor { get; set; } = Color.Black;

        /// <summary>
        /// Цвет фона фрактала при использовании CustomColor.
        /// </summary>
        public Color BackgroundColor { get; set; } = Color.White;

        #endregion

        #region Public Methods

        /// <summary>
        /// Основной метод рендеринга, который вызывает соответствующий внутренний метод
        /// для отрисовки фрактала Серпинского в предоставленный буфер пикселей.
        /// </summary>
        /// <param name="buffer">Буфер байтов, в который будут записаны пиксельные данные.</param>
        /// <param name="width">Ширина области рендеринга в пикселях.</param>
        /// <param name="height">Высота области рендеринга в пикселях.</param>
        /// <param name="stride">Длина строки в байтах (количество байтов на одну строку изображения).</param>
        /// <param name="bytesPerPixel">Количество байтов на один пиксель.</param>
        /// <param name="numThreads">Количество потоков для использования в параллельных вычислениях.</param>
        /// <param name="token">Токен отмены для прерывания операции рендеринга.</param>
        /// <param name="reportProgress">Callback-функция для отчета о прогрессе рендеринга (значение от 0 до 100).</param>
        public void RenderToBuffer(byte[] buffer, int width, int height, int stride, int bytesPerPixel,
                                   int numThreads, CancellationToken token, Action<int> reportProgress)
        {
            Color effectiveBackgroundColor, effectiveFractalColor;
            switch (ColorMode)
            {
                case SerpinskyColorMode.BlackAndWhite:
                    effectiveBackgroundColor = Color.White;
                    effectiveFractalColor = Color.Black;
                    break;
                case SerpinskyColorMode.Grayscale:
                    effectiveBackgroundColor = Color.White;
                    effectiveFractalColor = Color.FromArgb(255, 50, 50, 50);
                    break;
                case SerpinskyColorMode.CustomColor:
                default:
                    effectiveBackgroundColor = BackgroundColor;
                    effectiveFractalColor = FractalColor;
                    break;
            }

            FillBackground(buffer, width, height, stride, bytesPerPixel, effectiveBackgroundColor, token);
            token.ThrowIfCancellationRequested();

            if (RenderMode == SerpinskyRenderMode.Geometric)
            {
                RenderGeometric(token, buffer, width, height, stride, bytesPerPixel, effectiveFractalColor, numThreads, reportProgress);
            }
            else // SerpinskyRenderMode.Chaos
            {
                RenderChaos(token, buffer, width, height, stride, bytesPerPixel, effectiveFractalColor, numThreads, reportProgress);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Заполняет весь буфер пикселей заданным цветом фона.
        /// </summary>
        /// <param name="buffer">Буфер байтов для заполнения.</param>
        /// <param name="width">Ширина буфера в пикселях.</param>
        /// <param name="height">Высота буфера в пикселях.</param>
        /// <param name="stride">Длина строки в байтах.</param>
        /// <param name="bytesPerPixel">Количество байтов на пиксель.</param>
        /// <param name="color">Цвет для заполнения.</param>
        /// <param name="token">Токен отмены для прерывания операции.</param>
        private void FillBackground(byte[] buffer, int width, int height, int stride, int bytesPerPixel, Color color, CancellationToken token)
        {
            byte colorB = color.B;
            byte colorG = color.G;
            byte colorR = color.R;
            byte colorA = color.A;

            for (int y = 0; y < height; y++)
            {
                token.ThrowIfCancellationRequested();
                for (int x = 0; x < width; x++)
                {
                    int index = y * stride + x * bytesPerPixel;
                    buffer[index + 0] = colorB;
                    buffer[index + 1] = colorG;
                    buffer[index + 2] = colorR;
                    buffer[index + 3] = colorA;
                }
            }
        }

        /// <summary>
        /// Рендерит фрактал Серпинского в геометрическом режиме, рекурсивно рисуя треугольники.
        /// </summary>
        /// <param name="token">Токен отмены для прерывания операции.</param>
        /// <param name="buffer">Буфер байтов для отрисовки.</param>
        /// <param name="width">Ширина области рендеринга.</param>
        /// <param name="height">Высота области рендеринга.</param>
        /// <param name="stride">Длина строки в байтах.</param>
        /// <param name="bytesPerPixel">Количество байтов на пиксель.</param>
        /// <param name="fractalColor">Цвет, используемый для рисования фрактала.</param>
        /// <param name="numThreads">Количество потоков для параллельной обработки.</param>
        /// <param name="reportProgress">Callback-функция для отчета о прогрессе.</param>
        private void RenderGeometric(CancellationToken token, byte[] buffer, int width, int height, int stride, int bytesPerPixel,
                                     Color fractalColor, int numThreads, Action<int> reportProgress)
        {
            if (Iterations < 0)
            {
                reportProgress(100);
                return;
            }
            token.ThrowIfCancellationRequested();

            double side = 1.0;
            double triangleHeight = side * Math.Sqrt(3) / 2.0;

            // Координаты вершин базового равностороннего треугольника в мировых координатах
            PointF p1World = new PointF(0, (float)(triangleHeight * 2.0 / 3.0));
            PointF p2World = new PointF((float)(-side / 2.0), (float)(-triangleHeight / 3.0));
            PointF p3World = new PointF((float)(side / 2.0), (float)(-triangleHeight / 3.0));

            // Делегат для рекурсивной отрисовки треугольников
            Action<int, PointF, PointF, PointF> drawTriangleBranch = null;
            drawTriangleBranch = (depth, pA, pB, pC) =>
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }

                if (depth == 0)
                {
                    // Преобразуем мировые координаты в экранные
                    PointF screenP1 = WorldToScreen(pA, width, height);
                    PointF screenP2 = WorldToScreen(pB, width, height);
                    PointF screenP3 = WorldToScreen(pC, width, height);
                    FillTriangleToBuffer(buffer, width, height, stride, bytesPerPixel, screenP1, screenP2, screenP3, fractalColor);
                    return;
                }

                // Находим средние точки сторон
                PointF pAB = MidPoint(pA, pB);
                PointF pBC = MidPoint(pB, pC);
                PointF pCA = MidPoint(pC, pA);

                // Рекурсивный вызов для трех внешних треугольников
                drawTriangleBranch(depth - 1, pA, pAB, pCA);
                drawTriangleBranch(depth - 1, pAB, pB, pBC);
                drawTriangleBranch(depth - 1, pCA, pBC, pC);
            };

            // Генерируем задачи для параллельного выполнения на верхних уровнях рекурсии
            var topLevelTasks = new List<Tuple<int, PointF, PointF, PointF>>();
            // Ограничиваем глубину параллелизации, чтобы избежать слишком большого количества мелких задач
            int parallelDepth = Math.Min(Iterations, 5);

            Action<int, PointF, PointF, PointF> generateTopLevelTasks = null;
            generateTopLevelTasks = (depth, pA, pB, pC) =>
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }
                if (depth == 0)
                {
                    topLevelTasks.Add(Tuple.Create(Iterations - parallelDepth, pA, pB, pC));
                    return;
                }
                PointF pAB = MidPoint(pA, pB);
                PointF pBC = MidPoint(pB, pC);
                PointF pCA = MidPoint(pC, pA);
                generateTopLevelTasks(depth - 1, pA, pAB, pCA);
                generateTopLevelTasks(depth - 1, pAB, pB, pBC);
                generateTopLevelTasks(depth - 1, pCA, pBC, pC);
            };

            if (Iterations == 0)
            {
                drawTriangleBranch(0, p1World, p2World, p3World);
                reportProgress(100);
                return;
            }

            generateTopLevelTasks(parallelDepth, p1World, p2World, p3World);
            token.ThrowIfCancellationRequested();

            var po = new ParallelOptions { MaxDegreeOfParallelism = numThreads, CancellationToken = token };
            long tasksDone = 0;

            try
            {
                Parallel.ForEach(topLevelTasks, po, task =>
                {
                    // Каждая задача рекурсивно отрисовывает свою ветвь фрактала
                    drawTriangleBranch(task.Item1, task.Item2, task.Item3, task.Item4);
                    long currentDone = Interlocked.Increment(ref tasksDone);
                    reportProgress((int)(100.0 * currentDone / topLevelTasks.Count));
                });
            }
            catch (OperationCanceledException)
            {
                // Игнорируем, так как отмена обработки ожидаема
            }
        }

        /// <summary>
        /// Рендерит фрактал Серпинского в режиме "Игры Хаоса", случайным образом выбирая вершины.
        /// </summary>
        /// <param name="token">Токен отмены для прерывания операции.</param>
        /// <param name="buffer">Буфер байтов для отрисовки.</param>
        /// <param name="width">Ширина области рендеринга.</param>
        /// <param name="height">Высота области рендеринга.</param>
        /// <param name="stride">Длина строки в байтах.</param>
        /// <param name="bytesPerPixel">Количество байтов на пиксель.</param>
        /// <param name="fractalColor">Цвет, используемый для рисования фрактала.</param>
        /// <param name="numThreads">Количество потоков для параллельной обработки.</param>
        /// <param name="reportProgress">Callback-функция для отчета о прогрессе.</param>
        private void RenderChaos(CancellationToken token, byte[] buffer, int width, int height, int stride, int bytesPerPixel,
                                 Color fractalColor, int numThreads, Action<int> reportProgress)
        {
            byte colorB = fractalColor.B;
            byte colorG = fractalColor.G;
            byte colorR = fractalColor.R;
            byte colorA = fractalColor.A;

            double side = 1.0;
            double triangleHeight = side * Math.Sqrt(3) / 2.0;

            // Вершины базового треугольника в мировых координатах
            PointF[] verticesWorld =
            {
                new PointF(0, (float)(triangleHeight * 2.0 / 3.0)),
                new PointF((float)(-side / 2.0), (float)(-triangleHeight / 3.0)),
                new PointF((float)(side / 2.0), (float)(-triangleHeight / 3.0))
            };

            int targetVisiblePoints = Iterations;
            if (targetVisiblePoints <= 0)
            {
                reportProgress(100);
                return;
            }

            long visiblePointsDrawn = 0;
            long totalGeneratedPoints = 0;
            // Аварийный лимит для предотвращения бесконечного цикла, если точки не попадают на экран
            long maxTotalGenerations = (long)targetVisiblePoints * 1000 + 1000000;

            Parallel.For(0, numThreads, new ParallelOptions { MaxDegreeOfParallelism = numThreads, CancellationToken = token }, threadId =>
            {
                Random localRand = new Random();
                // Начальная случайная точка внутри области (например, квадрат от -0.5 до 0.5)
                PointF currentPoint = new PointF((float)(localRand.NextDouble() - 0.5), (float)(localRand.NextDouble() - 0.5));

                // "Прогрев" - несколько начальных итераций для приведения точки в область фрактала
                for (int i = 0; i < 20; i++)
                {
                    currentPoint = MidPoint(currentPoint, verticesWorld[localRand.Next(3)]);
                }

                // Основной цикл генерации точек
                while (true)
                {
                    // Проверки на условия выхода из цикла
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }
                    if (Interlocked.Read(ref visiblePointsDrawn) >= targetVisiblePoints)
                    {
                        break;
                    }
                    if (Interlocked.Read(ref totalGeneratedPoints) > maxTotalGenerations)
                    {
                        break;
                    }

                    Interlocked.Increment(ref totalGeneratedPoints);

                    // Генерируем новую точку: середина между текущей точкой и случайно выбранной вершиной
                    currentPoint = MidPoint(currentPoint, verticesWorld[localRand.Next(3)]);
                    Point screenPoint = Point.Round(WorldToScreen(currentPoint, width, height));

                    // Если точка находится в пределах экрана, отрисовываем ее
                    if (screenPoint.X >= 0 && screenPoint.X < width && screenPoint.Y >= 0 && screenPoint.Y < height)
                    {
                        int index = screenPoint.Y * stride + screenPoint.X * bytesPerPixel;
                        buffer[index + 0] = colorB;
                        buffer[index + 1] = colorG;
                        buffer[index + 2] = colorR;
                        buffer[index + 3] = colorA;

                        long currentVisible = Interlocked.Increment(ref visiblePointsDrawn);

                        // Обновляем прогресс, но не слишком часто, чтобы не создавать накладные расходы
                        if (currentVisible % 1000 == 0)
                        {
                            reportProgress((int)Math.Min(100, (100L * currentVisible / targetVisiblePoints)));
                        }
                    }
                }
            });

            // Убеждаемся, что прогресс установлен на 100%, если не было отмены
            if (!token.IsCancellationRequested)
            {
                reportProgress(100);
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Преобразует координаты из мирового пространства в экранные пиксельные координаты.
        /// </summary>
        /// <param name="worldPoint">Точка в мировых координатах.</param>
        /// <param name="screenWidth">Ширина экрана/области рендеринга в пикселях.</param>
        /// <param name="screenHeight">Высота экрана/области рендеринга в пикселях.</param>
        /// <returns>Точка в экранных пиксельных координатах.</returns>
        private PointF WorldToScreen(PointF worldPoint, int screenWidth, int screenHeight)
        {
            double aspectRatio = (double)screenWidth / screenHeight;
            double viewHeightWorld = BASE_SCALE / Zoom;
            double viewWidthWorld = viewHeightWorld * aspectRatio;
            double minReal = CenterX - viewWidthWorld / 2.0;
            double maxImaginary = CenterY + viewHeightWorld / 2.0; // Для Y-оси в мировых координатах вверх

            float screenX = (float)(((worldPoint.X - minReal) / viewWidthWorld) * screenWidth);
            // Ось Y в экранных координатах обычно направлена вниз, поэтому (maxImaginary - worldPoint.Y)
            float screenY = (float)(((maxImaginary - worldPoint.Y) / viewHeightWorld) * screenHeight);

            return new PointF(screenX, screenY);
        }

        /// <summary>
        /// Вычисляет среднюю точку между двумя заданными точками.
        /// </summary>
        /// <param name="p1">Первая точка.</param>
        /// <param name="p2">Вторая точка.</param>
        /// <returns>Средняя точка.</returns>
        private PointF MidPoint(PointF p1, PointF p2)
        {
            return new PointF((p1.X + p2.X) / 2f, (p1.Y + p2.Y) / 2f);
        }

        /// <summary>
        /// Заполняет треугольник заданным цветом в предоставленном буфере пикселей.
        /// Использует алгоритм растеризации треугольника по scanline.
        /// </summary>
        /// <param name="buffer">Буфер байтов для отрисовки.</param>
        /// <param name="width">Ширина буфера.</param>
        /// <param name="height">Высота буфера.</param>
        /// <param name="stride">Длина строки в байтах.</param>
        /// <param name="bytesPerPixel">Количество байтов на пиксель.</param>
        /// <param name="p1">Первая вершина треугольника (экранные координаты).</param>
        /// <param name="p2">Вторая вершина треугольника (экранные координаты).</param>
        /// <param name="p3">Третья вершина треугольника (экранные координаты).</param>
        /// <param name="color">Цвет для заполнения треугольника.</param>
        private void FillTriangleToBuffer(byte[] buffer, int width, int height, int stride, int bytesPerPixel, PointF p1, PointF p2, PointF p3, Color color)
        {
            // Сортируем вершины по Y-координате
            PointF[] vertices = { p1, p2, p3 };
            Array.Sort(vertices, (a, b) => a.Y.CompareTo(b.Y));
            PointF vertexTop = vertices[0];
            PointF vertexMid = vertices[1];
            PointF vertexBottom = vertices[2];

            byte colorB = color.B;
            byte colorG = color.G;
            byte colorR = color.R;
            byte colorA = color.A;

            // Внутренний делегат для заполнения горизонтальной линии (scanline)
            Action<float, float, float> fillScanline = (yScanline, xStart, xEnd) =>
            {
                // Проверка границ по Y
                if (yScanline < 0 || yScanline >= height)
                {
                    return;
                }
                // Ограничиваем X-координаты в пределах экрана
                int startX = (int)Math.Max(0, Math.Min(xStart, xEnd));
                int endX = (int)Math.Min(width - 1, Math.Max(xStart, xEnd));

                for (int x = startX; x <= endX; x++)
                {
                    int index = (int)yScanline * stride + x * bytesPerPixel;
                    buffer[index + 0] = colorB;
                    buffer[index + 1] = colorG;
                    buffer[index + 2] = colorR;
                    buffer[index + 3] = colorA;
                }
            };

            // Растеризация верхней части треугольника (от vertexTop до vertexMid)
            if (vertexMid.Y - vertexTop.Y > 0.0001f) // Избегаем деления на ноль для горизонтальных линий
            {
                float inverseSlope1 = (vertexMid.X - vertexTop.X) / (vertexMid.Y - vertexTop.Y);
                float inverseSlope2 = (vertexBottom.X - vertexTop.X) / (vertexBottom.Y - vertexTop.Y);

                float currentX1 = vertexTop.X;
                float currentX2 = vertexTop.X;

                for (float y = vertexTop.Y; y < vertexMid.Y; y++)
                {
                    fillScanline(y, currentX1, currentX2);
                    currentX1 += inverseSlope1;
                    currentX2 += inverseSlope2;
                }
            }

            // Растеризация нижней части треугольника (от vertexMid до vertexBottom)
            if (vertexBottom.Y - vertexMid.Y > 0.0001f) // Избегаем деления на ноль для горизонтальных линий
            {
                float inverseSlope1 = (vertexBottom.X - vertexMid.X) / (vertexBottom.Y - vertexMid.Y);
                float inverseSlope2 = (vertexBottom.X - vertexTop.X) / (vertexBottom.Y - vertexTop.Y);

                float currentX1 = vertexMid.X;
                // Начальное значение для currentX2, соответствующее Y-координате vertexMid на длинной стороне
                float currentX2 = (Math.Abs(vertexBottom.Y - vertexTop.Y) > 0.0001f) ? vertexTop.X + (vertexMid.Y - vertexTop.Y) * inverseSlope2 : vertexTop.X;

                for (float y = vertexMid.Y; y <= vertexBottom.Y; y++)
                {
                    fillScanline(y, currentX1, currentX2);
                    currentX1 += inverseSlope1;
                    // Только если длинная сторона не горизонтальна, обновляем X2
                    if (Math.Abs(vertexBottom.Y - vertexTop.Y) > 0.0001f)
                    {
                        currentX2 += inverseSlope2;
                    }
                }
            }
        }

        #endregion
    }
}