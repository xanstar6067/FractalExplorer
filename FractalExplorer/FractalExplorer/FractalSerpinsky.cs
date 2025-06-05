using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace FractalExplorer
{
    public partial class FractalSerpinsky : Form
    {
        private Bitmap canvasBitmap;
        private volatile bool isRenderingPreview = false;
        private volatile bool isHighResRendering = false;
        private CancellationTokenSource previewRenderCts;
        private System.Windows.Forms.Timer renderTimer;

        // Параметры рендеринга
        private double currentZoom = 1.0;
        private double centerX = 0.0; // Центр в "мировых" координатах
        private double centerY = 0.0; // Центр в "мировых" координатах
        private const double BASE_SCALE = 1.0; // Базовый масштаб для фрактала

        // Параметры, с которыми был отрисован текущий canvasBitmap
        private double renderedZoom = 1.0;
        private double renderedCenterX = 0.0;
        private double renderedCenterY = 0.0;

        // Панорамирование
        private Point panStart;
        private bool panning = false;

        // Цвета
        private Color fractalColor = Color.Black;
        private Color backgroundColor = Color.White;
        private ColorDialog colorDialog;

        public FractalSerpinsky()
        {
            InitializeComponent();
            InitializeCustomComponents();
        }

        private void InitializeCustomComponents()
        {
            // Таймер для отложенного рендеринга
            renderTimer = new System.Windows.Forms.Timer { Interval = 300 };
            renderTimer.Tick += RenderTimer_Tick;

            // Заполнение ComboBox для потоков
            int cores = Environment.ProcessorCount;
            for (int i = 1; i <= cores; i++)
            {
                cbCPUThreads.Items.Add(i);
            }
            cbCPUThreads.Items.Add("Auto");
            cbCPUThreads.SelectedItem = "Auto";

            // Диалог выбора цвета
            colorDialog = new ColorDialog();

            // Начальные значения
            FractalTypeIsGeometry.Checked = true; // По умолчанию геометрический
            colorGrayscale.Checked = true;      // По умолчанию оттенки серого

            // Подписки на события (некоторые уже сделаны в дизайнере)
            this.Load += FractalSerpinsky_Load;
            canvasSerpinsky.Paint += CanvasSerpinsky_Paint;
            canvasSerpinsky.MouseWheel += CanvasSerpinsky_MouseWheel;
            canvasSerpinsky.MouseDown += CanvasSerpinsky_MouseDown;
            canvasSerpinsky.MouseMove += CanvasSerpinsky_MouseMove;
            canvasSerpinsky.MouseUp += CanvasSerpinsky_MouseUp;
            this.Resize += FractalSerpinsky_Resize;
            canvasSerpinsky.Resize += CanvasSerpinsky_Resize;

            // Настройка NumericUpDown (значения уже заданы в дизайнере, но можно перепроверить логические пределы)
            nudIterations.Minimum = 1; // Глубина для геометрического, количество точек для хаоса
            nudIterations.Maximum = 15; // Разумный предел для глубины, для точек хаоса может быть больше
            nudIterations.Value = 5;


            nudZoom.Value = 1m;
            nudZoom.DecimalPlaces = 2;


            // Взаимоисключающие чекбоксы
            FractalTypeIsGeometry.CheckedChanged += FractalType_CheckedChanged;
            FractalTypeIsChaos.CheckedChanged += FractalType_CheckedChanged;

            renderBW.CheckedChanged += ColorMode_CheckedChanged;
            colorGrayscale.CheckedChanged += ColorMode_CheckedChanged;

            colorBackground.CheckedChanged += ColorTarget_CheckedChanged;
            colorFractal.CheckedChanged += ColorTarget_CheckedChanged;
            colorFractal.Checked = true; // По умолчанию выбираем цвет фрактала

            // Обновление отображения палитры (canvasPalette)
            UpdatePaletteCanvas();
        }

        private void FractalSerpinsky_Load(object sender, EventArgs e)
        {
            renderedCenterX = centerX;
            renderedCenterY = centerY;
            renderedZoom = currentZoom;
            ScheduleRender();
        }

        private void FractalSerpinsky_Resize(object sender, EventArgs e) => HandleResize();
        private void CanvasSerpinsky_Resize(object sender, EventArgs e) => HandleResize();

        private void HandleResize()
        {
            if (isHighResRendering) return;
            if (canvasSerpinsky.Width <= 0 || canvasSerpinsky.Height <= 0) return;
            ScheduleRender();
        }

        #region Взаимоисключающие CheckBox'ы

        private void FractalType_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox activeCb = sender as CheckBox;
            if (activeCb == null || !activeCb.Checked) return;

            if (activeCb == FractalTypeIsGeometry)
            {
                FractalTypeIsChaos.Checked = false;
                nudIterations.Maximum = 15; // Глубина для геометрического
                if (nudIterations.Value > 15) nudIterations.Value = 15;
                nudIterations.Minimum = 0;
            }
            else if (activeCb == FractalTypeIsChaos)
            {
                FractalTypeIsGeometry.Checked = false;
                nudIterations.Maximum = int.MaxValue; // Количество точек для хаоса
                nudIterations.Minimum = 1000;
                if (nudIterations.Value < 1000 && nudIterations.Value > 0) nudIterations.Value = 50000; // reasonable default for chaos
                else if (nudIterations.Value == 0) nudIterations.Value = 50000;
            }
            ScheduleRender();
        }

        private void ColorMode_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox activeCb = sender as CheckBox;
            if (activeCb == null || !activeCb.Checked)
            {
                // Если все сняты, вернем Grayscale по умолчанию
                if (!renderBW.Checked && !colorGrayscale.Checked)
                {
                    colorGrayscale.CheckedChanged -= ColorMode_CheckedChanged;
                    colorGrayscale.Checked = true;
                    colorGrayscale.CheckedChanged += ColorMode_CheckedChanged;
                }
                UpdatePaletteCanvas(); // Обновить доступность выбора цвета
                ScheduleRender();
                return;
            }

            if (activeCb == renderBW)
            {
                colorGrayscale.Checked = false;
            }
            else if (activeCb == colorGrayscale)
            {
                renderBW.Checked = false;
            }
            UpdatePaletteCanvas(); // Обновить доступность выбора цвета
            ScheduleRender();
        }

        private void ColorTarget_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox activeCb = sender as CheckBox;
            if (activeCb == null || !activeCb.Checked)
            {
                // Если все сняты, вернем colorFractal по умолчанию
                if (!colorBackground.Checked && !colorFractal.Checked)
                {
                    colorFractal.CheckedChanged -= ColorTarget_CheckedChanged;
                    colorFractal.Checked = true;
                    colorFractal.CheckedChanged += ColorTarget_CheckedChanged;
                }
                UpdatePaletteCanvas();
                return;
            }

            if (activeCb == colorBackground)
            {
                colorFractal.Checked = false;
            }
            else if (activeCb == colorFractal)
            {
                colorBackground.Checked = false;
            }
            UpdatePaletteCanvas();
        }

        #endregion

        #region Управление параметрами и рендеринг

        private void ParamControl_Changed(object sender, EventArgs e)
        {
            if (isHighResRendering) return;

            if (sender == nudZoom)
            {
                currentZoom = Math.Max(0.01, (double)nudZoom.Value); // Предотвращаем слишком маленький зум
                nudZoom.Value = (decimal)currentZoom;
            }
            ScheduleRender();
        }

        private void ScheduleRender()
        {
            if (isHighResRendering) return;

            previewRenderCts?.Cancel();
            renderTimer.Stop();
            renderTimer.Start();
        }

        private async void RenderTimer_Tick(object sender, EventArgs e)
        {
            renderTimer.Stop();
            if (isHighResRendering || isRenderingPreview)
            {
                if (isRenderingPreview) renderTimer.Start(); // Если уже идет, подождем и попробуем снова
                return;
            }

            isRenderingPreview = true;
            SetMainControlsEnabled(false);

            previewRenderCts?.Dispose();
            previewRenderCts = new CancellationTokenSource();
            CancellationToken token = previewRenderCts.Token;

            // Захват текущих параметров для рендера
            int iterations = (int)nudIterations.Value;
            int numThreads = cbCPUThreads.SelectedItem.ToString() == "Auto" ? Environment.ProcessorCount : Convert.ToInt32(cbCPUThreads.SelectedItem);
            bool isGeometric = FractalTypeIsGeometry.Checked;
            bool useBW = renderBW.Checked;
            bool useGrayscale = colorGrayscale.Checked;

            double captureZoom = currentZoom;
            double captureCenterX = centerX;
            double captureCenterY = centerY;

            try
            {
                await Task.Run(() =>
                    RenderFractal(token, canvasSerpinsky.Width, canvasSerpinsky.Height,
                                  captureZoom, captureCenterX, captureCenterY,
                                  iterations, numThreads, isGeometric, useBW, useGrayscale,
                                  fractalColor, backgroundColor,
                                  (progress) => UpdateProgressBar(progressBarSerpinsky, progress))
                , token);
            }
            catch (OperationCanceledException) { /* Рендер отменен */ }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Render Error: {ex.Message}");
            }
            finally
            {
                isRenderingPreview = false;
                SetMainControlsEnabled(true);
                UpdateProgressBar(progressBarSerpinsky, 0); // Сброс прогресс бара
            }
        }

        private void RenderFractal(CancellationToken token, int renderWidth, int renderHeight,
                                   double zoomVal, double cX, double cY,
                                   int iterationsVal, int numThreadsVal, bool isGeometricVal,
                                   bool useBWVal, bool useGrayscaleVal,
                                   Color frColor, Color bgColor, Action<int> reportProgress)
        {
            if (token.IsCancellationRequested) return;
            if (renderWidth <= 0 || renderHeight <= 0) return;

            Bitmap bmp = null;
            BitmapData bmpData = null;

            try
            {
                bmp = new Bitmap(renderWidth, renderHeight, PixelFormat.Format32bppArgb); // Используем ARGB для прозрачности фона если нужно
                token.ThrowIfCancellationRequested();

                bmpData = bmp.LockBits(new Rectangle(0, 0, renderWidth, renderHeight), ImageLockMode.WriteOnly, bmp.PixelFormat);
                token.ThrowIfCancellationRequested();

                int bytesPerPixel = Image.GetPixelFormatSize(bmp.PixelFormat) / 8;
                int stride = bmpData.Stride;
                byte[] buffer = new byte[Math.Abs(stride) * renderHeight];
                IntPtr scan0 = bmpData.Scan0;

                // Заполняем фон
                Color currentBgColor = bgColor;
                if (useBWVal) currentBgColor = Color.White;
                else if (useGrayscaleVal) currentBgColor = Color.White; // Или другой оттенок серого для фона

                for (int y = 0; y < renderHeight; y++)
                {
                    for (int x = 0; x < renderWidth; x++)
                    {
                        int idx = y * stride + x * bytesPerPixel;
                        buffer[idx + 0] = currentBgColor.B; // Blue
                        buffer[idx + 1] = currentBgColor.G; // Green
                        buffer[idx + 2] = currentBgColor.R; // Red
                        buffer[idx + 3] = currentBgColor.A; // Alpha
                    }
                }
                token.ThrowIfCancellationRequested();


                // Логика рендеринга фрактала
                if (isGeometricVal)
                {
                    RenderSierpinskiGeometric(token, buffer, renderWidth, renderHeight, stride, bytesPerPixel,
                                              zoomVal, cX, cY, iterationsVal,
                                              useBWVal, useGrayscaleVal, frColor, numThreadsVal, reportProgress);
                }
                else // Chaos Game
                {
                    RenderSierpinskiChaos(token, buffer, renderWidth, renderHeight, stride, bytesPerPixel,
                                          zoomVal, cX, cY, iterationsVal,
                                          useBWVal, useGrayscaleVal, frColor, numThreadsVal, reportProgress);
                }
                token.ThrowIfCancellationRequested();

                Marshal.Copy(buffer, 0, scan0, buffer.Length);
                bmp.UnlockBits(bmpData);
                bmpData = null;
                token.ThrowIfCancellationRequested();

                if (canvasSerpinsky.IsHandleCreated && !canvasSerpinsky.IsDisposed)
                {
                    Bitmap oldImage = null;
                    canvasSerpinsky.Invoke((Action)(() =>
                    {
                        if (token.IsCancellationRequested) { bmp?.Dispose(); return; }
                        oldImage = canvasBitmap;
                        canvasBitmap = bmp;
                        renderedZoom = zoomVal;
                        renderedCenterX = cX;
                        renderedCenterY = cY;
                        canvasSerpinsky.Invalidate(); // Запросить перерисовку через Paint
                        bmp = null; // Владение передано canvasBitmap
                    }));
                    oldImage?.Dispose();
                }
                else
                {
                    bmp?.Dispose();
                }
            }
            finally
            {
                if (bmpData != null && bmp != null) try { bmp.UnlockBits(bmpData); } catch { }
                bmp?.Dispose();
            }
        }

        private void RenderSierpinskiGeometric(CancellationToken token, byte[] buffer, int W, int H, int stride, int bpp,
                                         double zoom, double cX, double cY, int depth,
                                         bool useBW, bool useGrayscale, Color frColor, int numThreads, Action<int> reportProgress)
        {
            if (depth < 0)
            {
                reportProgress(100); // Если глубина некорректна, считаем завершенным
                return;
            }

            Color actualFractalColor;
            if (useBW) actualFractalColor = Color.Black;
            else if (useGrayscale) actualFractalColor = Color.FromArgb(200, 50, 50, 50); // Темно-серый, но не слишком, чтобы отличался от черного фона если он есть
            else actualFractalColor = frColor;

            double side = 1.0;
            double height_triangle = side * Math.Sqrt(3) / 2.0;

            // Центрируем начальный треугольник относительно (0,0) в мировых координатах
            // Вершина направлена вверх
            PointF p1_world = new PointF(0, (float)(height_triangle * 2.0 / 3.0 - height_triangle / 2.0));
            PointF p2_world = new PointF((float)(-side / 2.0), (float)(-height_triangle / 3.0 - height_triangle / 2.0));
            PointF p3_world = new PointF((float)(side / 2.0), (float)(-height_triangle / 3.0 - height_triangle / 2.0));

            // Используем List<Action> для сбора финальных действий по отрисовке.
            // BlockingCollection здесь избыточна, так как генерация будет однопоточной.
            var drawingActions = new List<Action>();
            long totalTrianglesToDraw = (long)Math.Pow(3, depth);
            long drawnTrianglesCounter = 0;

            Action<int, PointF, PointF, PointF> generateDrawingTasksRecursive = null;
            generateDrawingTasksRecursive = (d, pA, pB, pC) =>
            {
                if (token.IsCancellationRequested) return;

                if (d == 0)
                {
                    // Это листовой узел, добавляем действие по его отрисовке
                    drawingActions.Add(() =>
                    {
                        if (token.IsCancellationRequested) return;

                        PointF sP1 = WorldToScreen(pA, W, H, zoom, cX, cY);
                        PointF sP2 = WorldToScreen(pB, W, H, zoom, cX, cY);
                        PointF sP3 = WorldToScreen(pC, W, H, zoom, cX, cY);

                        // Для Серпинского нам нужна заливка треугольников
                        FillTriangleToBuffer(buffer, W, H, stride, bpp, sP1, sP2, sP3, actualFractalColor);

                        long currentCount = Interlocked.Increment(ref drawnTrianglesCounter);
                        if (totalTrianglesToDraw > 0)
                        {
                            // Обновляем прогресс не на каждом треугольнике, если их много
                            if (currentCount == totalTrianglesToDraw || (totalTrianglesToDraw > 100 && currentCount % (totalTrianglesToDraw / 100) == 0))
                            {
                                reportProgress((int)Math.Min(100, (100 * currentCount / totalTrianglesToDraw)));
                            }
                            else if (totalTrianglesToDraw <= 100) // Для малого числа обновляем всегда
                            {
                                reportProgress((int)Math.Min(100, (100 * currentCount / totalTrianglesToDraw)));
                            }
                        }
                    });
                    return;
                }

                PointF pAB = MidPoint(pA, pB);
                PointF pBC = MidPoint(pB, pC);
                PointF pCA = MidPoint(pC, pA);

                generateDrawingTasksRecursive(d - 1, pA, pAB, pCA);
                if (token.IsCancellationRequested) return;
                generateDrawingTasksRecursive(d - 1, pAB, pB, pBC);
                if (token.IsCancellationRequested) return;
                generateDrawingTasksRecursive(d - 1, pCA, pBC, pC);
            };

            // Этап 1: Генерация всех действий по отрисовке (последовательно)
            try
            {
                generateDrawingTasksRecursive(depth, p1_world, p2_world, p3_world);
            }
            catch (OperationCanceledException)
            {
                // Если отмена произошла во время генерации, выходим
                reportProgress(drawnTrianglesCounter > 0 && totalTrianglesToDraw > 0 ? (int)(100 * drawnTrianglesCounter / totalTrianglesToDraw) : 0);
                return;
            }

            if (token.IsCancellationRequested)
            {
                reportProgress(drawnTrianglesCounter > 0 && totalTrianglesToDraw > 0 ? (int)(100 * drawnTrianglesCounter / totalTrianglesToDraw) : 0);
                return;
            }

            // Этап 2: Параллельное выполнение сгенерированных действий по отрисовке
            var po = new ParallelOptions { MaxDegreeOfParallelism = numThreads, CancellationToken = token };
            try
            {
                Parallel.ForEach(drawingActions, po, drawingAction =>
                {
                    // CancellationToken должен проверяться внутри drawingAction, если оно длительное.
                    // Но сами лямбды уже проверяют токен в начале.
                    drawingAction();
                });
            }
            catch (OperationCanceledException)
            {
                // Операция была отменена во время параллельного выполнения
            }
            finally // Убедимся, что прогресс показан корректно
            {
                if (token.IsCancellationRequested)
                {
                    reportProgress(drawnTrianglesCounter > 0 && totalTrianglesToDraw > 0 ? (int)Math.Min(100, (100 * drawnTrianglesCounter / totalTrianglesToDraw)) : 0);
                }
                else
                {
                    reportProgress(100); // Успешное завершение
                }
            }
        }

        // Вспомогательный метод для заливки треугольника (Scanline Fill)
        // Это упрощенная версия для выпуклых многоугольников (треугольник всегда выпуклый)
        private void FillTriangleToBuffer(byte[] buffer, int W, int H, int stride, int bpp, PointF p1, PointF p2, PointF p3, Color color)
        {
            // Сортируем вершины по Y
            PointF[] v = { p1, p2, p3 };
            Array.Sort(v, (a, b) => a.Y.CompareTo(b.Y));

            PointF vTop = v[0];
            PointF vMid = v[1];
            PointF vBot = v[2];

            byte cB = color.B; byte cG = color.G; byte cR = color.R; byte cA = color.A;

            // Растеризация верхней половины треугольника (vTop -> vMid)
            if (Math.Abs(vMid.Y - vTop.Y) > 0.001) // Проверка на горизонтальную линию
            {
                for (float y = vTop.Y; y <= vMid.Y; y += 1.0f)
                {
                    if (y < 0 || y >= H) continue;

                    float x1 = vTop.X + (vBot.X - vTop.X) * (y - vTop.Y) / (vBot.Y - vTop.Y); // Линия vTop-vBot
                    float x2 = vTop.X + (vMid.X - vTop.X) * (y - vTop.Y) / (vMid.Y - vTop.Y); // Линия vTop-vMid

                    if (float.IsNaN(x1) || float.IsInfinity(x1)) x1 = (vBot.Y == vTop.Y) ? vTop.X : float.NaN; // Обработка горизонтальной vTop-vBot
                    if (float.IsNaN(x2) || float.IsInfinity(x2)) x2 = (vMid.Y == vTop.Y) ? vTop.X : float.NaN; // Обработка горизонтальной vTop-vMid

                    if (float.IsNaN(x1) && float.IsNaN(x2)) continue;
                    if (float.IsNaN(x1)) x1 = x2; // если одна из линий вертикальная
                    if (float.IsNaN(x2)) x2 = x1;


                    int startX = (int)Math.Min(x1, x2);
                    int endX = (int)Math.Max(x1, x2);

                    for (int x = startX; x <= endX; x++)
                    {
                        if (x < 0 || x >= W) continue;
                        int idx = (int)y * stride + x * bpp;
                        if (idx < 0 || idx + (bpp - 1) >= buffer.Length) continue;

                        buffer[idx + 0] = cB; buffer[idx + 1] = cG;
                        buffer[idx + 2] = cR; buffer[idx + 3] = cA;
                    }
                }
            }

            // Растеризация нижней половины треугольника (vMid -> vBot)
            if (Math.Abs(vBot.Y - vMid.Y) > 0.001) // Проверка на горизонтальную линию
            {
                for (float y = vMid.Y; y <= vBot.Y; y += 1.0f)
                {
                    if (y < 0 || y >= H) continue;

                    float x1 = vTop.X + (vBot.X - vTop.X) * (y - vTop.Y) / (vBot.Y - vTop.Y);       // Линия vTop-vBot
                    float x2 = vMid.X + (vBot.X - vMid.X) * (y - vMid.Y) / (vBot.Y - vMid.Y);       // Линия vMid-vBot

                    if (float.IsNaN(x1) || float.IsInfinity(x1)) x1 = (vBot.Y == vTop.Y) ? vTop.X : float.NaN;
                    if (float.IsNaN(x2) || float.IsInfinity(x2)) x2 = (vBot.Y == vMid.Y) ? vMid.X : float.NaN;

                    if (float.IsNaN(x1) && float.IsNaN(x2)) continue;
                    if (float.IsNaN(x1)) x1 = x2;
                    if (float.IsNaN(x2)) x2 = x1;

                    int startX = (int)Math.Min(x1, x2);
                    int endX = (int)Math.Max(x1, x2);

                    for (int x = startX; x <= endX; x++)
                    {
                        if (x < 0 || x >= W) continue;
                        int idx = (int)y * stride + x * bpp;
                        if (idx < 0 || idx + (bpp - 1) >= buffer.Length) continue;

                        buffer[idx + 0] = cB; buffer[idx + 1] = cG;
                        buffer[idx + 2] = cR; buffer[idx + 3] = cA;
                    }
                }
            }
        }

        private void RenderSierpinskiChaos(CancellationToken token, byte[] buffer, int W, int H, int stride, int bpp,
                                       double zoom, double cX, double cY, int numPoints,
                                       bool useBW, bool useGrayscale, Color frColor, int numThreads, Action<int> reportProgress)
        {
            Color pointColor;
            if (useBW) pointColor = Color.Black;
            else if (useGrayscale) pointColor = Color.DarkGray;
            else pointColor = frColor;

            byte cB = pointColor.B;
            byte cG = pointColor.G;
            byte cR = pointColor.R;
            byte cA = pointColor.A;

            double side = 1.0;
            double height_triangle = side * Math.Sqrt(3) / 2.0;

            PointF[] vertices_world = new PointF[3];
            vertices_world[0] = new PointF(0, (float)(height_triangle * 2.0 / 3.0));
            vertices_world[1] = new PointF((float)(-side / 2.0), (float)(-height_triangle / 3.0));
            vertices_world[2] = new PointF((float)(side / 2.0), (float)(-height_triangle / 3.0));

            Random rand = new Random();
            PointF currentPoint_world = vertices_world[0]; // Начать с одной из вершин

            // "Разогрев" - несколько итераций, чтобы точка попала внутрь фрактала
            for (int i = 0; i < 20; ++i)
            {
                int vertexIdx = rand.Next(3);
                currentPoint_world = MidPoint(currentPoint_world, vertices_world[vertexIdx]);
            }

            int pointsPerThread = numPoints / numThreads;
            if (pointsPerThread == 0 && numPoints > 0) pointsPerThread = numPoints; // Если мало точек

            long totalDrawnPoints = 0;

            Parallel.For(0, numThreads, new ParallelOptions { CancellationToken = token }, threadId =>
            {
                Random localRand = new Random(rand.Next() + threadId); // У каждого потока свой Random
                PointF localCurrentPoint_world = currentPoint_world; // Копия для потока

                // У каждого потока своя начальная точка, немного смещенная от общей
                // Это помогает избежать начальных артефактов, если все потоки стартуют строго из одной точки
                if (threadId > 0)
                {
                    for (int i = 0; i < 5; ++i)
                    { // Небольшой "разогрев" для каждой копии потока
                        int vertexIdx = localRand.Next(3);
                        localCurrentPoint_world = MidPoint(localCurrentPoint_world, vertices_world[vertexIdx]);
                    }
                }

                for (int i = 0; i < pointsPerThread; i++)
                {
                    if (token.IsCancellationRequested) break;

                    int vertexIdx = localRand.Next(3);
                    localCurrentPoint_world = MidPoint(localCurrentPoint_world, vertices_world[vertexIdx]);

                    Point screenPoint = Point.Round(WorldToScreen(localCurrentPoint_world, W, H, zoom, cX, cY));

                    if (screenPoint.X >= 0 && screenPoint.X < W && screenPoint.Y >= 0 && screenPoint.Y < H)
                    {
                        int idx = screenPoint.Y * stride + screenPoint.X * bpp;
                        // Не используем блокировку здесь, т.к. это редкие записи, и возможна перезапись пикселя, что допустимо для хаоса
                        buffer[idx + 0] = cB;
                        buffer[idx + 1] = cG;
                        buffer[idx + 2] = cR;
                        buffer[idx + 3] = cA;
                    }

                    if (i % 1000 == 0) // Обновление прогресса не на каждой точке
                    {
                        long currentTotal = Interlocked.Add(ref totalDrawnPoints, 1000);
                        reportProgress((int)Math.Min(100, (100 * currentTotal / numPoints)));
                    }
                }
            });
            reportProgress(100);
        }

        #endregion

        #region Вспомогательные функции для рендеринга (координаты, точки, треугольники)

        private PointF WorldToScreen(PointF worldPoint, int screenWidth, int screenHeight, double zoomVal, double centerXVal, double centerYVal)
        {
            // Масштабирование и центрирование
            // 1. Применяем зум: чем больше zoomVal, тем "ближе" мы смотрим, т.е. мировые координаты умножаются на zoomVal
            // 2. Сдвигаем относительно центра: (worldX - centerXVal)
            // 3. Переводим в пиксели относительно центра экрана
            // 4. Сдвигаем на половину ширины/высоты экрана

            double viewWidthWorld = (BASE_SCALE / zoomVal) * ((double)screenWidth / screenHeight); // Ширина видимой области в мировых координатах
            double viewHeightWorld = BASE_SCALE / zoomVal;                                      // Высота видимой области в мировых координатах

            // Координаты левого верхнего угла видимой области в мировых координатах
            double minRe = centerXVal - viewWidthWorld / 2.0;
            double minIm = centerYVal - viewHeightWorld / 2.0; // Y в мировых растет вверх

            float screenX = (float)(((worldPoint.X - minRe) / viewWidthWorld) * screenWidth);
            float screenY = (float)((((centerYVal + viewHeightWorld / 2.0) - worldPoint.Y) / viewHeightWorld) * screenHeight); // Инверсия Y

            return new PointF(screenX, screenY);
        }

        private PointF ScreenToWorld(Point screenPoint, int screenWidth, int screenHeight, double zoomVal, double centerXVal, double centerYVal)
        {
            double viewWidthWorld = (BASE_SCALE / zoomVal) * ((double)screenWidth / screenHeight);
            double viewHeightWorld = BASE_SCALE / zoomVal;

            double minRe = centerXVal - viewWidthWorld / 2.0;
            double minIm = centerYVal - viewHeightWorld / 2.0;

            float worldX = (float)(minRe + (screenPoint.X / (double)screenWidth) * viewWidthWorld);
            float worldY = (float)((centerYVal + viewHeightWorld / 2.0) - (screenPoint.Y / (double)screenHeight) * viewHeightWorld); // Инверсия Y

            return new PointF(worldX, worldY);
        }


        private PointF MidPoint(PointF p1, PointF p2)
        {
            return new PointF((p1.X + p2.X) / 2f, (p1.Y + p2.Y) / 2f);
        }

        private void DrawTriangleToBuffer(byte[] buffer, int W, int H, int stride, int bpp, PointF p1, PointF p2, PointF p3, Color color)
        {
            // Простая реализация: рисуем линии. Для заливки нужен более сложный алгоритм растеризации.
            // Для Серпинского нам нужны именно "дырки", поэтому фон уже должен быть, а мы рисуем сами треугольники.
            // Либо наоборот, если мы "удаляем" центральный, то нужно рисовать 3 оставшихся.
            // В данной реализации RenderSierpinskiGeometric рисует 3 меньших треугольника.
            // Этот метод должен ЗАЛИТЬ треугольник.

            // Используем Graphics для рисования на временном Bitmap, затем скопируем в буфер
            // Это медленнее, но проще для заливки. Для производительности нужна своя растеризация.
            // Для простоты примера, я буду рисовать линии. Для заливки нужен алгоритм растеризации.
            // Для демонстрации лучше использовать готовый Graphics.DrawPolygon / FillPolygon.
            // Однако, мы работаем с byte[] buffer.

            // Растеризация треугольника (Scanline algorithm) - упрощенная версия
            PointF[] vertices = { p1, p2, p3 };
            Array.Sort(vertices, (a, b) => a.Y.CompareTo(b.Y)); // Сортировка по Y

            PointF v1 = vertices[0];
            PointF v2 = vertices[1];
            PointF v3 = vertices[2];

            byte cB = color.B;
            byte cG = color.G;
            byte cR = color.R;
            byte cA = color.A;

            Action<int, int> plot = (x, y) =>
            {
                if (x >= 0 && x < W && y >= 0 && y < H)
                {
                    int idx = y * stride + x * bpp;
                    buffer[idx + 0] = cB;
                    buffer[idx + 1] = cG;
                    buffer[idx + 2] = cR;
                    buffer[idx + 3] = cA;
                }
            };

            // Рисуем линии между вершинами (очень грубо, для примера)
            DrawLineToBuffer(buffer, W, H, stride, bpp, Point.Round(p1), Point.Round(p2), color);
            DrawLineToBuffer(buffer, W, H, stride, bpp, Point.Round(p2), Point.Round(p3), color);
            DrawLineToBuffer(buffer, W, H, stride, bpp, Point.Round(p3), Point.Round(p1), color);

            // TODO: Реализовать заливку треугольника, если это требуется.
            // Текущая реализация RenderSierpinskiGeometric подразумевает, что мы рисуем
            // сами "оставшиеся" треугольники, а не "вырезаем" центральный.
            // Поэтому их нужно заливать.
            // Для простоты, пока только контуры. Заливка требует алгоритма растеризации.
        }

        // Алгоритм Брезенхэма для рисования линий (упрощенный)
        private void DrawLineToBuffer(byte[] buffer, int W, int H, int stride, int bpp, Point p0, Point p1, Color color)
        {
            int x0 = p0.X, y0 = p0.Y;
            int x1 = p1.X, y1 = p1.Y;

            int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
            int dy = -Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
            int err = dx + dy, e2;

            byte cB = color.B; byte cG = color.G; byte cR = color.R; byte cA = color.A;

            for (; ; )
            {
                if (x0 >= 0 && x0 < W && y0 >= 0 && y0 < H)
                {
                    int idx = y0 * stride + x0 * bpp;
                    buffer[idx + 0] = cB; buffer[idx + 1] = cG; buffer[idx + 2] = cR; buffer[idx + 3] = cA;
                }
                if (x0 == x1 && y0 == y1) break;
                e2 = 2 * err;
                if (e2 >= dy) { err += dy; x0 += sx; }
                if (e2 <= dx) { err += dx; y0 += sy; }
            }
        }

        #endregion

        #region Масштабирование и панорамирование Canvas

        private void CanvasSerpinsky_Paint(object sender, PaintEventArgs e)
        {
            // Определяем эффективный цвет фона в самом начале
            bool useBW = renderBW.Checked; // Захватываем текущие значения чекбоксов
            bool useGrayscale = colorGrayscale.Checked;
            Color effectiveBgColor = useBW ? Color.White : (useGrayscale ? Color.White : backgroundColor);

            if (canvasBitmap == null || canvasSerpinsky.Width <= 0 || canvasSerpinsky.Height <= 0)
            {
                e.Graphics.Clear(effectiveBgColor); // Используем актуальный цвет фона
                return;
            }

            // Параметры, с которыми был отрисован canvasBitmap
            double rZoom = renderedZoom;
            double rCX = renderedCenterX;
            double rCY = renderedCenterY;

            // Текущие параметры отображения (целевые)
            double cZoom = currentZoom;
            double cCX = centerX;
            double cCY = centerY;

            if (rZoom <= 0 || cZoom <= 0) // Проверка на корректность зума
            {
                e.Graphics.Clear(effectiveBgColor);
                e.Graphics.DrawImageUnscaled(canvasBitmap, Point.Empty); // Безопасный фолбэк
                return;
            }

            // Ширина и высота видимой области в мировых координатах для отрисованного bitmap
            double renderedViewWidthWorld = (BASE_SCALE / rZoom) * ((double)canvasBitmap.Width / canvasBitmap.Height); // Используем размеры bitmap'а
            double renderedViewHeightWorld = BASE_SCALE / rZoom;

            // Координаты левого верхнего угла отрисованного bitmap'а в мировых координатах
            double renderedMinRe = rCX - renderedViewWidthWorld / 2.0;
            double renderedMinIm = rCY - renderedViewHeightWorld / 2.0; // Нижняя граница, так как Y мировой растет вверх

            // Ширина и высота видимой области в мировых координатах для текущего отображения на canvas
            double currentViewWidthWorld = (BASE_SCALE / cZoom) * ((double)canvasSerpinsky.Width / canvasSerpinsky.Height);
            double currentViewHeightWorld = BASE_SCALE / cZoom;

            // Координаты левого верхнего угла текущего отображения на canvas в мировых координатах
            double currentMinRe = cCX - currentViewWidthWorld / 2.0;
            double currentMinIm = cCY - currentViewHeightWorld / 2.0;

            // Рассчитываем положение и размер отрисованного bitmap'а на текущем canvas
            // p1_X, p1_Y - экранные координаты верхнего левого угла bitmap'а на canvas'е
            float p1_X = (float)(((renderedMinRe - currentMinRe) / currentViewWidthWorld) * canvasSerpinsky.Width);
            // Для Y: мировой Y растет вверх, экранный Y вниз.
            // (currentMaxIm - renderedMaxIm) / currentViewHeightWorld * canvasHeight
            // currentMaxIm = cCY + currentViewHeightWorld / 2.0
            // renderedMaxIm = rCY + renderedViewHeightWorld / 2.0
            float p1_Y = (float)((((cCY + currentViewHeightWorld / 2.0) - (rCY + renderedViewHeightWorld / 2.0)) / currentViewHeightWorld) * canvasSerpinsky.Height);

            // w_prime, h_prime - новые экранные размеры bitmap'а на canvas'е
            float w_prime = (float)((renderedViewWidthWorld / currentViewWidthWorld) * canvasSerpinsky.Width);
            float h_prime = (float)((renderedViewHeightWorld / currentViewHeightWorld) * canvasSerpinsky.Height);

            PointF destPoint1 = new PointF(p1_X, p1_Y);
            PointF destPoint2 = new PointF(p1_X + w_prime, p1_Y);
            PointF destPoint3 = new PointF(p1_X, p1_Y + h_prime);

            e.Graphics.Clear(effectiveBgColor); // Очищаем актуальным цветом фона
            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

            if (w_prime > 0 && h_prime > 0)
            {
                try
                {
                    e.Graphics.DrawImage(canvasBitmap, new PointF[] { destPoint1, destPoint2, destPoint3 });
                }
                catch (ArgumentException)
                {
                    e.Graphics.DrawImageUnscaled(canvasBitmap, Point.Empty);
                }
            }
            else
            {
                e.Graphics.DrawImageUnscaled(canvasBitmap, Point.Empty);
            }
        }

        private void CanvasSerpinsky_MouseWheel(object sender, MouseEventArgs e)
        {
            if (isHighResRendering) return;
            if (canvasSerpinsky.Width <= 0 || canvasSerpinsky.Height <= 0) return;

            double zoomFactor = e.Delta > 0 ? 1.2 : 1.0 / 1.2;
            double oldZoom = currentZoom;

            PointF worldPosUnderCursor = ScreenToWorld(e.Location, canvasSerpinsky.Width, canvasSerpinsky.Height, oldZoom, centerX, centerY);

            double minZoomFromNud = (double)nudZoom.Minimum;
            if (minZoomFromNud <= 0) minZoomFromNud = 0.01; // Запасной минимум

            // Применяем зум, ограничивая его min/max значениями из nudZoom
            currentZoom = Math.Max(minZoomFromNud, Math.Min((double)nudZoom.Maximum, currentZoom * zoomFactor));

            // Рассчитываем новый центр, чтобы точка под курсором осталась на месте
            double newViewWidthWorld = (BASE_SCALE / currentZoom) * ((double)canvasSerpinsky.Width / canvasSerpinsky.Height);
            double newViewHeightWorld = BASE_SCALE / currentZoom;

            centerX = worldPosUnderCursor.X - (((double)e.X / canvasSerpinsky.Width) - 0.5) * newViewWidthWorld;
            centerY = worldPosUnderCursor.Y - (0.5 - ((double)e.Y / canvasSerpinsky.Height)) * newViewHeightWorld;

            canvasSerpinsky.Invalidate();

            // Обновляем nudZoom, если currentZoom изменился и отличается от значения в NUD
            if (Math.Abs((double)nudZoom.Value - currentZoom) > 0.00001)
            {
                nudZoom.ValueChanged -= ParamControl_Changed; // Временно отписаться
                nudZoom.Value = (decimal)currentZoom;
                nudZoom.ValueChanged += ParamControl_Changed; // Подписаться обратно
                ScheduleRender(); // Нужен полный рендер, т.к. центр и зум изменились
            }
            else
            {
                // Если nudZoom.Value уже равен currentZoom (например, достигли предела),
                // но центр изменился, все равно нужен рендер
                ScheduleRender();
            }
        }

        private void CanvasSerpinsky_MouseDown(object sender, MouseEventArgs e)
        {
            if (isHighResRendering) return;
            if (e.Button == MouseButtons.Left)
            {
                panning = true;
                panStart = e.Location;
            }
        }

        private void CanvasSerpinsky_MouseMove(object sender, MouseEventArgs e)
        {
            if (isHighResRendering || !panning) return;
            if (canvasSerpinsky.Width <= 0 || canvasSerpinsky.Height <= 0) return;

            // Масштабные коэффициенты: сколько мировых единиц в одном пикселе
            double worldUnitsPerPixelX = (BASE_SCALE / currentZoom) * ((double)canvasSerpinsky.Width / canvasSerpinsky.Height) / canvasSerpinsky.Width;
            double worldUnitsPerPixelY = (BASE_SCALE / currentZoom) / canvasSerpinsky.Height;

            // Смещение мыши в пикселях
            double pixelDeltaX = e.X - panStart.X;
            double pixelDeltaY = e.Y - panStart.Y;

            // Обновляем мировой центр
            // Если мышь двигается вправо (pixelDeltaX > 0), фрактал должен сдвинуться вправо,
            // значит мировой centerX должен увеличиться
            centerX -= pixelDeltaX * worldUnitsPerPixelX;

            // Если мышь двигается вниз (pixelDeltaY > 0), фрактал должен сдвинуться вниз,
            // значит мировой centerY должен увеличиться (при стандартной системе координат где Y растет вверх)
            centerY += pixelDeltaY * worldUnitsPerPixelY; // ИСПРАВЛЕНО: используем плюс

            panStart = e.Location;
            canvasSerpinsky.Invalidate();
            ScheduleRender();
        }

        private void CanvasSerpinsky_MouseUp(object sender, MouseEventArgs e)
        {
            if (isHighResRendering) return;
            if (e.Button == MouseButtons.Left)
            {
                panning = false;
            }
        }

        #endregion

        #region Сохранение и управление UI

        private void btnRender_Click(object sender, EventArgs e)
        {
            ScheduleRender();
        }

        private async void btnSavePNG_Click(object sender, EventArgs e)
        {
            if (isHighResRendering)
            {
                MessageBox.Show("Процесс сохранения уже запущен.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int saveWidth = (int)nudW2.Value;
            int saveHeight = (int)nudH2.Value;

            if (saveWidth <= 0 || saveHeight <= 0)
            {
                MessageBox.Show("Ширина и высота должны быть больше 0.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string suggestedFileName = $"serpinski_fractal_{timestamp}.png";

            using (SaveFileDialog saveDialog = new SaveFileDialog
            {
                Filter = "PNG Image|*.png",
                Title = "Сохранить Треугольник Серпинского",
                FileName = suggestedFileName
            })
            {
                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    isHighResRendering = true;
                    SetMainControlsEnabled(false);
                    UpdateProgressBar(progressPNGSerpinsky, 0);
                    progressPNGSerpinsky.Visible = true;

                    // Захват параметров для рендеринга в высоком разрешении
                    int iterations = (int)nudIterations.Value;
                    int numThreads = cbCPUThreads.SelectedItem.ToString() == "Auto" ? Environment.ProcessorCount : Convert.ToInt32(cbCPUThreads.SelectedItem);
                    bool isGeometric = FractalTypeIsGeometry.Checked;
                    bool useBW = renderBW.Checked;
                    bool useGrayscale = colorGrayscale.Checked;
                    Color currentFrColor = fractalColor; // Захват текущих цветов
                    Color currentBgColor = backgroundColor;
                    double captureZoom = currentZoom; // Захват текущего зума и центра
                    double captureCenterX = centerX;
                    double captureCenterY = centerY;

                    try
                    {
                        Bitmap highResBitmap = await Task.Run(() =>
                        {
                            // Создаем новый Bitmap для высокого разрешения
                            Bitmap tempBmp = new Bitmap(saveWidth, saveHeight, PixelFormat.Format32bppArgb);
                            BitmapData tempData = tempBmp.LockBits(new Rectangle(0, 0, saveWidth, saveHeight), ImageLockMode.WriteOnly, tempBmp.PixelFormat);
                            int tempBpp = Image.GetPixelFormatSize(tempBmp.PixelFormat) / 8;
                            int tempStride = tempData.Stride;
                            byte[] tempBuffer = new byte[Math.Abs(tempStride) * saveHeight];

                            // Фон
                            Color effectiveBgColor = useBW ? Color.White : (useGrayscale ? Color.White : currentBgColor);
                            for (int y_bg = 0; y_bg < saveHeight; y_bg++)
                            {
                                for (int x_bg = 0; x_bg < saveWidth; x_bg++)
                                {
                                    int idx_bg = y_bg * tempStride + x_bg * tempBpp;
                                    tempBuffer[idx_bg + 0] = effectiveBgColor.B; tempBuffer[idx_bg + 1] = effectiveBgColor.G;
                                    tempBuffer[idx_bg + 2] = effectiveBgColor.R; tempBuffer[idx_bg + 3] = effectiveBgColor.A;
                                }
                            }

                            if (isGeometric)
                            {
                                RenderSierpinskiGeometric(CancellationToken.None, tempBuffer, saveWidth, saveHeight, tempStride, tempBpp,
                                                          captureZoom, captureCenterX, captureCenterY, iterations,
                                                          useBW, useGrayscale, currentFrColor, numThreads,
                                                          (progress) => UpdateProgressBar(progressPNGSerpinsky, progress));
                            }
                            else
                            {
                                RenderSierpinskiChaos(CancellationToken.None, tempBuffer, saveWidth, saveHeight, tempStride, tempBpp,
                                                      captureZoom, captureCenterX, captureCenterY, iterations,
                                                      useBW, useGrayscale, currentFrColor, numThreads,
                                                      (progress) => UpdateProgressBar(progressPNGSerpinsky, progress));
                            }

                            Marshal.Copy(tempBuffer, 0, tempData.Scan0, tempBuffer.Length);
                            tempBmp.UnlockBits(tempData);
                            return tempBmp;
                        });

                        highResBitmap.Save(saveDialog.FileName, ImageFormat.Png);
                        highResBitmap.Dispose();
                        MessageBox.Show("Изображение успешно сохранено!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        isHighResRendering = false;
                        SetMainControlsEnabled(true);
                        progressPNGSerpinsky.Visible = false;
                        UpdateProgressBar(progressPNGSerpinsky, 0);
                    }
                }
            }
        }

        private void SetMainControlsEnabled(bool enabled)
        {
            Action action = () =>
            {
                panel1.Enabled = enabled; // Отключаем всю панель с контролами
                                          // Но кнопки сохранения/рендера нужно отдельно, т.к. они могут быть нажаты для старта
                btnRender.Enabled = enabled;
                btnSavePNG.Enabled = enabled;

                if (enabled) UpdatePaletteCanvas(); // Обновить доступность canvasPalette
                else canvasPalette.Enabled = false;
            };

            if (this.InvokeRequired) this.Invoke(action);
            else action();
        }

        private void UpdateProgressBar(ProgressBar pb, int percentage)
        {
            if (pb.IsHandleCreated && !pb.IsDisposed)
            {
                try
                {
                    pb.Invoke((Action)(() =>
                    {
                        if (pb.IsHandleCreated && !pb.IsDisposed)
                            pb.Value = Math.Min(pb.Maximum, Math.Max(pb.Minimum, percentage));
                    }));
                }
                catch (Exception) { /* Игнорируем ошибки, если контрол уже уничтожен */ }
            }
        }

        #endregion

        #region Управление цветом и палитрой

        private void cancasPalette_Click(object sender, EventArgs e)
        {
            if (renderBW.Checked || colorGrayscale.Checked) return; // Не даем выбирать цвет в ЧБ/сером

            if (colorDialog.ShowDialog() == DialogResult.OK)
            {
                if (colorFractal.Checked)
                {
                    fractalColor = colorDialog.Color;
                }
                else if (colorBackground.Checked)
                {
                    backgroundColor = colorDialog.Color;
                }
                UpdatePaletteCanvas();
                ScheduleRender();
            }
        }

        private void UpdatePaletteCanvas()
        {
            bool enableColorPicking = !renderBW.Checked && !colorGrayscale.Checked;
            colorFractal.Enabled = enableColorPicking;
            colorBackground.Enabled = enableColorPicking;
            canvasPalette.Enabled = enableColorPicking;

            if (canvasPalette.IsHandleCreated && !canvasPalette.IsDisposed)
            {
                using (Graphics g = canvasPalette.CreateGraphics())
                {
                    Color previewColor = Color.DarkGray; // Цвет по умолчанию, если выбор цвета недоступен
                    if (enableColorPicking)
                    {
                        previewColor = colorFractal.Checked ? fractalColor : backgroundColor;
                    }
                    g.Clear(previewColor);

                    // Рисуем крестик, если требуется (пользователь просил)
                    // Однако, если это просто превью, крестик не очень осмыслен.
                    // Возможно, имелось в виду, что canvasPalette - это сама палитра для клика.
                    // Оставим пока просто заливку активным цветом.
                    // Если canvasPalette должен быть интерактивной палитрой, логика будет сложнее.
                    if (enableColorPicking)
                    {
                        using (Pen p = new Pen(previewColor.GetBrightness() < 0.5f ? Color.White : Color.Black, 1))
                        {
                            g.DrawLine(p, canvasPalette.Width / 2 - 5, canvasPalette.Height / 2, canvasPalette.Width / 2 + 5, canvasPalette.Height / 2);
                            g.DrawLine(p, canvasPalette.Width / 2, canvasPalette.Height / 2 - 5, canvasPalette.Width / 2, canvasPalette.Height / 2 + 5);
                        }
                    }
                }
            }
        }


        #endregion

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            previewRenderCts?.Cancel();
            previewRenderCts?.Dispose();
            renderTimer?.Stop();
            renderTimer?.Dispose();
            canvasBitmap?.Dispose();
            colorDialog?.Dispose();
            base.OnFormClosed(e);
        }
    }
}