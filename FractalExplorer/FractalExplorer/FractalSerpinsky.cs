using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
// using System.Numerics; // Пока не используется напрямую для Серпинского
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FractalExplorer
{
    public partial class FractalSerpinsky : Form
    {
        private Bitmap canvasBitmap;
        private volatile bool isRenderingPreview = false;
        private volatile bool isHighResRendering = false;
        private CancellationTokenSource previewRenderCts;
        private CancellationTokenSource highResRenderCts; // Для отмены рендера высокого разрешения
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

            colorDialog = new ColorDialog();

            // Подписки на события
            this.Load += FractalSerpinsky_Load;
            canvasSerpinsky.Paint += CanvasSerpinsky_Paint;
            canvasSerpinsky.MouseWheel += CanvasSerpinsky_MouseWheel;
            canvasSerpinsky.MouseDown += CanvasSerpinsky_MouseDown;
            canvasSerpinsky.MouseMove += CanvasSerpinsky_MouseMove;
            canvasSerpinsky.MouseUp += CanvasSerpinsky_MouseUp;
            this.Resize += FractalSerpinsky_Resize;
            canvasSerpinsky.Resize += CanvasSerpinsky_Resize;

            // Связываем обработчики для NumericUpDown и ComboBox
            nudZoom.ValueChanged += ParamControl_Changed;
            nudIterations.ValueChanged += ParamControl_Changed;
            cbCPUThreads.SelectedIndexChanged += ParamControl_Changed;


            nudIterations.Minimum = 0;
            nudIterations.Maximum = 15;
            nudIterations.Value = 5;

            nudZoom.Minimum = 0.01m;
            nudZoom.Maximum = 10000000m;
            nudZoom.Value = 1m;
            nudZoom.DecimalPlaces = 2;

            FractalTypeIsGeometry.CheckedChanged += FractalType_CheckedChanged;
            FractalTypeIsChaos.CheckedChanged += FractalType_CheckedChanged;

            renderBW.CheckedChanged += ColorMode_CheckedChanged;
            colorGrayscale.CheckedChanged += ColorMode_CheckedChanged;

            CheckBox colorColorCheckbox = this.Controls.Find("colorColor", true).FirstOrDefault() as CheckBox;
            if (colorColorCheckbox != null)
            {
                colorColorCheckbox.CheckedChanged += ColorChoiceMode_CheckedChanged;
            }


            colorBackground.CheckedChanged += ColorTarget_CheckedChanged;
            colorFractal.CheckedChanged += ColorTarget_CheckedChanged;

            FractalTypeIsGeometry.Checked = true;
            colorGrayscale.Checked = true;
            if (colorColorCheckbox != null) colorColorCheckbox.Checked = false;
            colorFractal.Checked = true;

            UpdatePaletteCanvas();
            UpdateAbortButtonState(); // Начальное состояние кнопки отмены
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

        #region Взаимоисключающие CheckBox'ы и управление цветом

        private void FractalType_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox activeCb = sender as CheckBox;
            if (activeCb == null || !activeCb.Checked) return;

            if (activeCb == FractalTypeIsGeometry)
            {
                // Не трогаем этот блок по вашей просьбе
                FractalTypeIsChaos.Checked = false;
                nudIterations.Maximum = 15; // Глубина для геометрического
                nudIterations.Minimum = 0;
                if (nudIterations.Value >= 15) nudIterations.Value = 5;
            }
            else if (activeCb == FractalTypeIsChaos)
            {
                FractalTypeIsGeometry.Checked = false;
                nudIterations.Maximum = int.MaxValue; // Для игры Хаоса - количество точек
                nudIterations.Minimum = 1000;         // Минимальное количество точек
                if (nudIterations.Value < 1000) // Если текущее значение меньше минимума для Хаоса
                {
                    nudIterations.Value = 50000; // Устанавливаем значение по умолчанию для Хаоса
                }
            }
            ScheduleRender();
        }

        private void ColorChoiceMode_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox colorColorCb = sender as CheckBox;
            if (colorColorCb == null) return;

            renderBW.CheckedChanged -= ColorMode_CheckedChanged;
            colorGrayscale.CheckedChanged -= ColorMode_CheckedChanged;

            if (colorColorCb.Checked)
            {
                if (renderBW.Checked) renderBW.Checked = false;
                if (colorGrayscale.Checked) colorGrayscale.Checked = false;

                if (!colorFractal.Checked && !colorBackground.Checked)
                {
                    colorFractal.CheckedChanged -= ColorTarget_CheckedChanged;
                    colorFractal.Checked = true;
                    colorFractal.CheckedChanged += ColorTarget_CheckedChanged;
                }
            }
            else
            {
                if (!renderBW.Checked && !colorGrayscale.Checked)
                {
                    colorGrayscale.Checked = true;
                }
            }

            renderBW.CheckedChanged += ColorMode_CheckedChanged;
            colorGrayscale.CheckedChanged += ColorMode_CheckedChanged;

            UpdatePaletteCanvas();
            ScheduleRender();
        }


        private void ColorMode_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox activeCb = sender as CheckBox;
            if (activeCb == null) return;

            CheckBox colorColorCb = this.Controls.Find("colorColor", true).FirstOrDefault() as CheckBox;

            if (colorColorCb != null) colorColorCb.CheckedChanged -= ColorChoiceMode_CheckedChanged;


            if (activeCb.Checked)
            {
                if (colorColorCb != null && colorColorCb.Checked)
                {
                    colorColorCb.Checked = false;
                }

                if (activeCb == renderBW && colorGrayscale.Checked)
                {
                    colorGrayscale.CheckedChanged -= ColorMode_CheckedChanged;
                    colorGrayscale.Checked = false;
                    colorGrayscale.CheckedChanged += ColorMode_CheckedChanged;
                }
                else if (activeCb == colorGrayscale && renderBW.Checked)
                {
                    renderBW.CheckedChanged -= ColorMode_CheckedChanged;
                    renderBW.Checked = false;
                    renderBW.CheckedChanged += ColorMode_CheckedChanged;
                }
            }
            else
            {
                if (!renderBW.Checked && !colorGrayscale.Checked && (colorColorCb == null || !colorColorCb.Checked))
                {
                    colorGrayscale.CheckedChanged -= ColorMode_CheckedChanged;
                    colorGrayscale.Checked = true;
                    colorGrayscale.CheckedChanged += ColorMode_CheckedChanged;
                }
            }

            if (colorColorCb != null) colorColorCb.CheckedChanged += ColorChoiceMode_CheckedChanged;

            UpdatePaletteCanvas();
            ScheduleRender();
        }


        private void ColorTarget_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox activeCb = sender as CheckBox;
            if (activeCb == null || !activeCb.Checked)
            {
                CheckBox colorColorCb = this.Controls.Find("colorColor", true).FirstOrDefault() as CheckBox;
                if ((colorColorCb != null && colorColorCb.Checked) && !colorBackground.Checked && !colorFractal.Checked)
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
                currentZoom = Math.Max((double)nudZoom.Minimum, (double)nudZoom.Value);
            }
            ScheduleRender();
        }


        private void ScheduleRender()
        {
            if (isHighResRendering) return; // Не перезапускаем предпросмотр, если идет сохранение

            previewRenderCts?.Cancel(); // Отменяем предыдущий запланированный или идущий рендер предпросмотра
            renderTimer.Stop();
            renderTimer.Start();
        }

        private async void RenderTimer_Tick(object sender, EventArgs e)
        {
            renderTimer.Stop();
            if (isHighResRendering || isRenderingPreview)
            {
                // Если уже идет рендер (любой), и это был "отложенный" запуск,
                // то просто ждем его окончания или отмены.
                // Если это был предпросмотр, можно его перезапустить позже, но лучше дать текущему завершиться или отмениться.
                // Если isRenderingPreview, то он сам себя перезапустит, если надо, или отменится.
                return;
            }

            isRenderingPreview = true;
            SetMainControlsEnabled(false);
            UpdateAbortButtonState(); // Включаем кнопку отмены

            previewRenderCts?.Dispose(); // Освобождаем предыдущий CTS, если был
            previewRenderCts = new CancellationTokenSource();
            CancellationToken token = previewRenderCts.Token;

            int iterations = (int)nudIterations.Value;
            int numThreads = cbCPUThreads.SelectedItem.ToString() == "Auto" ? Environment.ProcessorCount : Convert.ToInt32(cbCPUThreads.SelectedItem);
            bool isGeometric = FractalTypeIsGeometry.Checked;

            CheckBox colorColorCb = this.Controls.Find("colorColor", true).FirstOrDefault() as CheckBox;
            bool useColorMode = colorColorCb?.Checked ?? false;
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
                                  iterations, numThreads, isGeometric,
                                  useColorMode, useBW, useGrayscale,
                                  fractalColor, backgroundColor,
                                  (progress) => UpdateProgressBar(progressBarSerpinsky, progress))
                , token); // Передаем токен также в Task.Run для быстрой отмены, если задача еще не стартовала

                if (token.IsCancellationRequested)
                {
                    System.Diagnostics.Debug.WriteLine("Preview render was cancelled by request.");
                }
            }
            catch (OperationCanceledException)
            {
                System.Diagnostics.Debug.WriteLine("Preview render operation was cancelled via exception.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Render Error: {ex.Message}");
                // Можно добавить MessageBox.Show для пользователя
            }
            finally
            {
                isRenderingPreview = false;
                // Только если не идет рендер высокого разрешения, включаем контролы
                if (!isHighResRendering)
                {
                    SetMainControlsEnabled(true);
                }
                UpdateAbortButtonState(); // Обновляем состояние кнопки (скорее всего, выключится)
                UpdateProgressBar(progressBarSerpinsky, 0);
                // previewRenderCts не нужно здесь Dispose, он пересоздается в начале или в OnFormClosed
            }
        }

        private void RenderFractal(CancellationToken token, int renderWidth, int renderHeight,
                           double zoomVal, double cX, double cY,
                           int iterationsVal, int numThreadsVal, bool isGeometricVal,
                           bool useColorModeVal, bool useBWVal, bool useGrayscaleVal,
                           Color frColor, Color bgColor, Action<int> reportProgress)
        {
            if (token.IsCancellationRequested) return;
            if (renderWidth <= 0 || renderHeight <= 0) return;

            Bitmap bmp = null;
            BitmapData bmpData = null;

            try
            {
                bmp = new Bitmap(renderWidth, renderHeight, PixelFormat.Format32bppArgb);
                token.ThrowIfCancellationRequested();

                bmpData = bmp.LockBits(new Rectangle(0, 0, renderWidth, renderHeight), ImageLockMode.WriteOnly, bmp.PixelFormat);
                token.ThrowIfCancellationRequested();

                int bytesPerPixel = Image.GetPixelFormatSize(bmp.PixelFormat) / 8;
                int stride = bmpData.Stride;
                byte[] buffer = new byte[Math.Abs(stride) * renderHeight]; // Используем Math.Abs для stride
                IntPtr scan0 = bmpData.Scan0;

                Color currentBgColor;
                if (useBWVal) currentBgColor = Color.White;
                else if (useGrayscaleVal) currentBgColor = Color.White;
                else currentBgColor = bgColor;

                // Инициализация буфера цветом фона
                // Этот цикл можно оптимизировать, если фон всегда одинаков (например, белый для ЧБ/серого)
                // или если фон всегда полностью непрозрачный.
                for (int y_bg = 0; y_bg < renderHeight; y_bg++)
                {
                    token.ThrowIfCancellationRequested(); // Проверка перед каждой строкой
                    for (int x_bg = 0; x_bg < renderWidth; x_bg++)
                    {
                        int idx_bg = y_bg * stride + x_bg * bytesPerPixel;
                        buffer[idx_bg + 0] = currentBgColor.B; // Blue
                        buffer[idx_bg + 1] = currentBgColor.G; // Green
                        buffer[idx_bg + 2] = currentBgColor.R; // Red
                        buffer[idx_bg + 3] = currentBgColor.A; // Alpha
                    }
                }
                token.ThrowIfCancellationRequested();

                if (isGeometricVal)
                {
                    RenderSierpinskiGeometric(token, buffer, renderWidth, renderHeight, stride, bytesPerPixel,
                                              zoomVal, cX, cY, iterationsVal,
                                              useColorModeVal, useBWVal, useGrayscaleVal, frColor, numThreadsVal, reportProgress);
                }
                else
                {
                    RenderSierpinskiChaos(token, buffer, renderWidth, renderHeight, stride, bytesPerPixel,
                                          zoomVal, cX, cY, iterationsVal,
                                          useColorModeVal, useBWVal, useGrayscaleVal, frColor, numThreadsVal, reportProgress);
                }
                token.ThrowIfCancellationRequested();

                Marshal.Copy(buffer, 0, scan0, buffer.Length);
                bmp.UnlockBits(bmpData);
                bmpData = null; // Важно, чтобы избежать двойного UnlockBits в finally
                token.ThrowIfCancellationRequested();

                // Обновление canvasBitmap в UI потоке
                if (canvasSerpinsky.IsHandleCreated && !canvasSerpinsky.IsDisposed)
                {
                    Bitmap oldImage = null;
                    canvasSerpinsky.Invoke((Action)(() =>
                    {
                        if (token.IsCancellationRequested) { bmp?.Dispose(); return; } // Если отменили во время Invoke
                        oldImage = canvasBitmap;
                        canvasBitmap = bmp;
                        renderedZoom = zoomVal; // Сохраняем параметры текущего рендера
                        renderedCenterX = cX;
                        renderedCenterY = cY;
                        canvasSerpinsky.Invalidate();
                        bmp = null; // bmp теперь принадлежит canvasBitmap, не удаляем его здесь
                    }));
                    oldImage?.Dispose(); // Удаляем старое изображение
                }
                else
                {
                    bmp?.Dispose(); // Если canvas уже не существует
                }
            }
            catch (OperationCanceledException)
            {
                // Если отмена произошла внутри RenderFractal (до Invoke)
                bmp?.Dispose(); // Освобождаем созданный bitmap, если он не был передан
                throw; // Передаем исключение выше, чтобы finally в RenderTimer_Tick/btnSavePNG_Click сработал корректно
            }
            finally
            {
                if (bmpData != null && bmp != null) try { bmp.UnlockBits(bmpData); } catch { /* Игнорируем ошибки при разблокировке, если уже была ошибка */ }
                // bmp?.Dispose(); // Не удаляем здесь, если он был успешно присвоен canvasBitmap
                // Если была ошибка до присвоения или отмена, bmp должен быть удален в catch или перед выходом
            }
        }

        private void RenderSierpinskiGeometric(CancellationToken token, byte[] buffer, int W, int H, int stride, int bpp,
                                     double zoom, double cX, double cY, int depth,
                                     bool useColorMode, bool useBW, bool useGrayscale, Color frColor,
                                     int numThreads, Action<int> reportProgress)
        {
            if (depth < 0) // Базовый случай для рекурсии, если глубина некорректна
            {
                reportProgress(100);
                return;
            }
            token.ThrowIfCancellationRequested();

            Color actualFractalColor;
            if (useBW) actualFractalColor = Color.Black;
            else if (useGrayscale) actualFractalColor = Color.FromArgb(255, 50, 50, 50);
            else actualFractalColor = frColor;

            double side = 1.0;
            double height_triangle = side * Math.Sqrt(3) / 2.0;
            float y_offset = 0;
            PointF p1_world = new PointF(0, (float)(height_triangle * 2.0 / 3.0) + y_offset);
            PointF p2_world = new PointF((float)(-side / 2.0), (float)(-height_triangle / 3.0) + y_offset);
            PointF p3_world = new PointF((float)(side / 2.0), (float)(-height_triangle / 3.0) + y_offset);

            var drawingActions = new List<Action>();
            long totalTrianglesToDraw = (long)Math.Pow(3, depth);
            long drawnTrianglesCounter = 0;

            Action<int, PointF, PointF, PointF> generateDrawingTasksRecursive = null;
            generateDrawingTasksRecursive = (d, pA, pB, pC) =>
            {
                if (token.IsCancellationRequested) return;

                if (d == 0)
                {
                    drawingActions.Add(() =>
                    {
                        if (token.IsCancellationRequested) return;
                        PointF sP1 = WorldToScreen(pA, W, H, zoom, cX, cY);
                        PointF sP2 = WorldToScreen(pB, W, H, zoom, cX, cY);
                        PointF sP3 = WorldToScreen(pC, W, H, zoom, cX, cY);
                        FillTriangleToBuffer(buffer, W, H, stride, bpp, sP1, sP2, sP3, actualFractalColor);

                        long currentCount = Interlocked.Increment(ref drawnTrianglesCounter);
                        if (totalTrianglesToDraw > 0)
                        {
                            // Обновляем прогресс-бар менее часто для больших глубин
                            if (currentCount == totalTrianglesToDraw || (totalTrianglesToDraw > 100 && currentCount % (totalTrianglesToDraw / 100) == 0))
                            { reportProgress((int)Math.Min(100, (100 * currentCount / totalTrianglesToDraw))); }
                            else if (totalTrianglesToDraw <= 100) // Для малых глубин обновляем чаще
                            { reportProgress((int)Math.Min(100, (100 * currentCount / totalTrianglesToDraw))); }
                        }
                    });
                    return;
                }
                PointF pAB = MidPoint(pA, pB); PointF pBC = MidPoint(pB, pC); PointF pCA = MidPoint(pC, pA);
                generateDrawingTasksRecursive(d - 1, pA, pAB, pCA); if (token.IsCancellationRequested) return;
                generateDrawingTasksRecursive(d - 1, pAB, pB, pBC); if (token.IsCancellationRequested) return;
                generateDrawingTasksRecursive(d - 1, pCA, pBC, pC);
            };

            try
            {
                generateDrawingTasksRecursive(depth, p1_world, p2_world, p3_world);
                token.ThrowIfCancellationRequested(); // Проверка после генерации списка задач

                var po = new ParallelOptions { MaxDegreeOfParallelism = numThreads, CancellationToken = token };
                Parallel.ForEach(drawingActions, po, drawingAction =>
                {
                    // token.ThrowIfCancellationRequested(); // Parallel.ForEach сам обработает токен
                    drawingAction();
                });
            }
            catch (OperationCanceledException)
            {
                // Просто выходим, finally обновит прогресс до текущего состояния
            }
            finally // Гарантируем, что прогресс-бар будет обновлен
            {
                if (token.IsCancellationRequested)
                {
                    reportProgress(drawnTrianglesCounter > 0 && totalTrianglesToDraw > 0 ? (int)Math.Min(100, (100 * drawnTrianglesCounter / totalTrianglesToDraw)) : 0);
                }
                else
                {
                    reportProgress(100); // Если не отменено, то завершено на 100%
                }
            }
        }

        private void FillTriangleToBuffer(byte[] buffer, int W, int H, int stride, int bpp, PointF p1, PointF p2, PointF p3, Color color)
        {
            PointF[] v = { p1, p2, p3 };
            Array.Sort(v, (a, b) => a.Y.CompareTo(b.Y)); // Сортировка по Y
            PointF vTop = v[0]; PointF vMid = v[1]; PointF vBot = v[2];
            byte cB = color.B; byte cG = color.G; byte cR = color.R; byte cA = color.A;

            Action<float, float, float> fillScanline = (yScan, xStart, xEnd) =>
            {
                if (yScan < 0 || yScan >= H) return; // За пределами высоты буфера
                int startX = (int)Math.Max(0, Math.Min(xStart, xEnd));
                int endX = (int)Math.Min(W - 1, Math.Max(xStart, xEnd));
                for (int x = startX; x <= endX; x++)
                {
                    int idx = (int)yScan * stride + x * bpp;
                    if (idx >= 0 && idx + (bpp - 1) < buffer.Length) // Проверка границ буфера
                    { buffer[idx + 0] = cB; buffer[idx + 1] = cG; buffer[idx + 2] = cR; buffer[idx + 3] = cA; }
                }
            };

            float invSlope1, invSlope2;
            float curX1, curX2;

            // Верхняя половина (от vTop до vMid)
            if (vMid.Y - vTop.Y > 0.0001f) // Проверка на вырожденность (не горизонтальная линия)
            {
                invSlope1 = (vMid.X - vTop.X) / (vMid.Y - vTop.Y);
                invSlope2 = (vBot.X - vTop.X) / (vBot.Y - vTop.Y); // Эта линия от vTop до vBot
                curX1 = vTop.X; curX2 = vTop.X;
                for (float y = vTop.Y; y < vMid.Y; y += 1.0f)
                {
                    fillScanline(y, curX1, curX2);
                    curX1 += invSlope1; curX2 += invSlope2;
                }
            }
            // Нижняя половина (от vMid до vBot)
            if (vBot.Y - vMid.Y > 0.0001f) // Проверка на вырожденность
            {
                invSlope1 = (vBot.X - vMid.X) / (vBot.Y - vMid.Y); // Линия от vMid до vBot
                invSlope2 = (vBot.X - vTop.X) / (vBot.Y - vTop.Y); // Линия от vTop до vBot (тот же наклон)

                curX1 = vMid.X;
                // curX2 должен быть на линии vTop-vBot на высоте vMid.Y
                // Если vBot.Y - vTop.Y очень мало (vTop и vBot почти на одной Y), то vTop.X
                if (Math.Abs(vBot.Y - vTop.Y) > 0.0001f)
                    curX2 = vTop.X + (vMid.Y - vTop.Y) * invSlope2;
                else
                    curX2 = vTop.X; // или vMid.X, если vTop=vMid


                for (float y = vMid.Y; y <= vBot.Y; y += 1.0f)
                {
                    fillScanline(y, curX1, curX2);
                    curX1 += invSlope1;
                    if (Math.Abs(vBot.Y - vTop.Y) > 0.0001f) // Обновляем curX2 только если vTop-vBot не горизонтальна
                        curX2 += invSlope2;
                }
            }
            // Если треугольник - это одна горизонтальная линия (vTop.Y == vMid.Y == vBot.Y),
            // то текущая логика его не нарисует. Для заливки это обычно не проблема.
            // Если vTop.Y == vMid.Y, но vBot.Y > vMid.Y, рисуется только нижняя половина.
            // Если vMid.Y == vBot.Y, но vMid.Y > vTop.Y, рисуется только верхняя половина.
            // Если все три точки на одной линии (не горизонтальной), то это линия, а не треугольник.
        }


        private void RenderSierpinskiChaos(CancellationToken token, byte[] buffer, int W, int H, int stride, int bpp,
                                   double zoom, double cX, double cY, int numPoints,
                                   bool useColorMode, bool useBW, bool useGrayscale, Color frColor,
                                   int numThreads, Action<int> reportProgress)
        {
            token.ThrowIfCancellationRequested();
            Color pointColor;
            if (useBW) pointColor = Color.Black;
            else if (useGrayscale) pointColor = Color.FromArgb(255, 100, 100, 100);
            else pointColor = frColor;

            byte cB = pointColor.B; byte cG = pointColor.G; byte cR = pointColor.R; byte cA = pointColor.A;
            double side = 1.0; double height_triangle = side * Math.Sqrt(3) / 2.0;
            PointF[] vertices_world = new PointF[3];
            float y_offset = 0;
            vertices_world[0] = new PointF(0, (float)(height_triangle * 2.0 / 3.0) + y_offset);
            vertices_world[1] = new PointF((float)(-side / 2.0), (float)(-height_triangle / 3.0) + y_offset);
            vertices_world[2] = new PointF((float)(side / 2.0), (float)(-height_triangle / 3.0) + y_offset);

            Random masterRand = new Random();
            PointF initialPoint_world = vertices_world[0];
            for (int i = 0; i < 20; ++i) { initialPoint_world = MidPoint(initialPoint_world, vertices_world[masterRand.Next(3)]); }

            long totalDrawnPoints = 0;
            // Убедимся, что pointsPerThread не 0, если numPoints < numThreads
            int pointsPerThread = (numPoints < numThreads && numPoints > 0) ? 1 : Math.Max(1, numPoints / numThreads);
            if (numPoints == 0) pointsPerThread = 0; // Если 0 точек, то 0 на поток


            Parallel.For(0, numThreads, new ParallelOptions { MaxDegreeOfParallelism = numThreads, CancellationToken = token }, threadId =>
            {
                if (token.IsCancellationRequested) return; // Проверка перед началом работы потока

                Random localRand = new Random(masterRand.Next() + threadId);
                PointF localCurrentPoint_world = initialPoint_world;
                // Небольшая "разогревочная" итерация для каждого потока, чтобы точки распределились
                if (threadId > 0) { for (int i = 0; i < 5 + threadId; ++i) { localCurrentPoint_world = MidPoint(localCurrentPoint_world, vertices_world[localRand.Next(3)]); } }

                // Корректное распределение точек, если numPoints не делится на numThreads
                int startPoint = threadId * pointsPerThread;
                int endPoint = (threadId == numThreads - 1) ? numPoints : startPoint + pointsPerThread;
                int pointsForThisThread = endPoint - startPoint;


                for (int i = 0; i < pointsForThisThread; i++)
                {
                    if (token.IsCancellationRequested) break; // Проверка в цикле
                    localCurrentPoint_world = MidPoint(localCurrentPoint_world, vertices_world[localRand.Next(3)]);
                    Point screenPoint = Point.Round(WorldToScreen(localCurrentPoint_world, W, H, zoom, cX, cY));
                    if (screenPoint.X >= 0 && screenPoint.X < W && screenPoint.Y >= 0 && screenPoint.Y < H)
                    {
                        int idx = screenPoint.Y * stride + screenPoint.X * bpp;
                        if (idx >= 0 && idx + bpp - 1 < buffer.Length)
                        {
                            buffer[idx + 0] = cB; buffer[idx + 1] = cG; buffer[idx + 2] = cR; buffer[idx + 3] = cA;
                        }
                    }
                    // Обновление прогресс-бара (менее часто)
                    if (i % 1000 == 0) // Обновляем каждые 1000 точек на поток
                    {
                        long currentTotal = Interlocked.Add(ref totalDrawnPoints, (i == 0 && threadId == 0 && pointsForThisThread > 0) ? 1 : (i > 0 ? 1000 : 0));
                        if (numPoints > 0) reportProgress((int)Math.Min(100, (100 * currentTotal / numPoints)));
                    }
                }
                // Добавляем оставшиеся точки, если цикл завершился не на кратном 1000
                if (pointsForThisThread > 0 && pointsForThisThread % 1000 != 0)
                {
                    Interlocked.Add(ref totalDrawnPoints, pointsForThisThread % 1000);
                }

            });
            // Финальное обновление прогресс-бара
            if (token.IsCancellationRequested)
            {
                reportProgress(numPoints > 0 ? (int)Math.Min(100, (100 * Interlocked.Read(ref totalDrawnPoints) / numPoints)) : 0);
            }
            else
            {
                reportProgress(100);
            }
        }

        #endregion

        #region Вспомогательные функции для рендеринга (координаты, точки)

        private PointF WorldToScreen(PointF worldPoint, int screenWidth, int screenHeight, double zoomVal, double centerXVal, double centerYVal)
        {
            // Предотвращение деления на ноль, если screenHeight или zoomVal равны нулю
            if (screenHeight == 0 || zoomVal == 0) return new PointF(0, 0);

            double aspect = (double)screenWidth / screenHeight;
            double viewHeightWorld = BASE_SCALE / zoomVal;
            double viewWidthWorld = viewHeightWorld * aspect;

            double minRe = centerXVal - viewWidthWorld / 2.0;
            double maxIm = centerYVal + viewHeightWorld / 2.0;

            // Предотвращение деления на ноль, если viewWidthWorld или viewHeightWorld равны нулю
            if (viewWidthWorld == 0 || viewHeightWorld == 0) return new PointF(0, 0);


            float screenX = (float)(((worldPoint.X - minRe) / viewWidthWorld) * screenWidth);
            float screenY = (float)(((maxIm - worldPoint.Y) / viewHeightWorld) * screenHeight);

            return new PointF(screenX, screenY);
        }

        private PointF ScreenToWorld(Point screenPoint, int screenWidth, int screenHeight, double zoomVal, double centerXVal, double centerYVal)
        {
            if (screenWidth == 0 || screenHeight == 0 || zoomVal == 0) return new PointF(0, 0); // Предотвращение деления на ноль

            double aspect = (double)screenWidth / screenHeight;
            double viewHeightWorld = BASE_SCALE / zoomVal;
            double viewWidthWorld = viewHeightWorld * aspect;

            double minRe = centerXVal - viewWidthWorld / 2.0;
            double maxIm = centerYVal + viewHeightWorld / 2.0;

            float worldX = (float)(minRe + (screenPoint.X / (double)screenWidth) * viewWidthWorld);
            float worldY = (float)(maxIm - (screenPoint.Y / (double)screenHeight) * viewHeightWorld);

            return new PointF(worldX, worldY);
        }

        private PointF MidPoint(PointF p1, PointF p2)
        {
            return new PointF((p1.X + p2.X) / 2f, (p1.Y + p2.Y) / 2f);
        }
        #endregion

        #region Масштабирование и панорамирование Canvas

        private void CanvasSerpinsky_Paint(object sender, PaintEventArgs e)
        {
            CheckBox colorColorCb = this.Controls.Find("colorColor", true).FirstOrDefault() as CheckBox;
            bool useColorMode = colorColorCb?.Checked ?? false;
            bool useBW = renderBW.Checked;
            bool useGrayscale = colorGrayscale.Checked;

            Color effectiveBgColor;
            if (useBW) effectiveBgColor = Color.White;
            else if (useGrayscale) effectiveBgColor = Color.White;
            else effectiveBgColor = backgroundColor;

            if (canvasBitmap == null || canvasSerpinsky.Width <= 0 || canvasSerpinsky.Height <= 0)
            {
                e.Graphics.Clear(effectiveBgColor);
                return;
            }

            double rZoom = renderedZoom; double rCX = renderedCenterX; double rCY = renderedCenterY;
            double cZoom = currentZoom; double cCX = centerX; double cCY = centerY;

            if (rZoom <= 0 || cZoom <= 0 || canvasBitmap.Height == 0 || canvasSerpinsky.Height == 0) // Добавил проверки на 0
            { e.Graphics.Clear(effectiveBgColor); if (canvasBitmap != null) e.Graphics.DrawImageUnscaled(canvasBitmap, Point.Empty); return; }


            double rAspect = (double)canvasBitmap.Width / canvasBitmap.Height;
            double rViewHeightWorld = BASE_SCALE / rZoom;
            double rViewWidthWorld = rViewHeightWorld * rAspect;
            double rMinRe = rCX - rViewWidthWorld / 2.0;
            double rMaxIm = rCY + rViewHeightWorld / 2.0;

            double cAspect = (double)canvasSerpinsky.Width / canvasSerpinsky.Height;
            double cViewHeightWorld = BASE_SCALE / cZoom;
            double cViewWidthWorld = cViewHeightWorld * cAspect;
            double cMinRe = cCX - cViewWidthWorld / 2.0;
            double cMaxIm = cCY + cViewHeightWorld / 2.0;

            if (cViewWidthWorld == 0 || cViewHeightWorld == 0) // Предотвращение деления на ноль
            { e.Graphics.Clear(effectiveBgColor); e.Graphics.DrawImageUnscaled(canvasBitmap, Point.Empty); return; }


            float p1_X = (float)(((rMinRe - cMinRe) / cViewWidthWorld) * canvasSerpinsky.Width);
            float p1_Y = (float)(((cMaxIm - rMaxIm) / cViewHeightWorld) * canvasSerpinsky.Height);
            float w_prime = (float)((rViewWidthWorld / cViewWidthWorld) * canvasSerpinsky.Width);
            float h_prime = (float)((rViewHeightWorld / cViewHeightWorld) * canvasSerpinsky.Height);

            PointF destPoint1 = new PointF(p1_X, p1_Y);
            PointF destPoint2 = new PointF(p1_X + w_prime, p1_Y);
            PointF destPoint3 = new PointF(p1_X, p1_Y + h_prime);

            e.Graphics.Clear(effectiveBgColor);
            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

            if (w_prime > 0 && h_prime > 0)
            { try { e.Graphics.DrawImage(canvasBitmap, new PointF[] { destPoint1, destPoint2, destPoint3 }); } catch (ArgumentException) { e.Graphics.DrawImageUnscaled(canvasBitmap, Point.Empty); } }
            else { e.Graphics.DrawImageUnscaled(canvasBitmap, Point.Empty); }
        }

        private void CanvasSerpinsky_MouseWheel(object sender, MouseEventArgs e)
        {
            if (isHighResRendering) return;
            if (canvasSerpinsky.Width <= 0 || canvasSerpinsky.Height <= 0) return;
            double zoomFactor = e.Delta > 0 ? 1.2 : 1.0 / 1.2;
            double oldZoom = currentZoom;
            PointF worldPosUnderCursor = ScreenToWorld(e.Location, canvasSerpinsky.Width, canvasSerpinsky.Height, oldZoom, centerX, centerY);
            double minZoomFromNud = (double)nudZoom.Minimum; if (minZoomFromNud <= 0) minZoomFromNud = 0.01;
            currentZoom = Math.Max(minZoomFromNud, Math.Min((double)nudZoom.Maximum, currentZoom * zoomFactor));

            // Защита от слишком большого/малого currentZoom, который может вызвать проблемы
            if (currentZoom < 0.000001) currentZoom = 0.000001;
            if (currentZoom > 1000000000) currentZoom = 1000000000;


            if (currentZoom == 0 || canvasSerpinsky.Width == 0 || canvasSerpinsky.Height == 0) return; // Защита

            double cAspect = (double)canvasSerpinsky.Width / canvasSerpinsky.Height;
            double newViewHeightWorld = BASE_SCALE / currentZoom;
            double newViewWidthWorld = newViewHeightWorld * cAspect;

            if (newViewWidthWorld == 0 || newViewHeightWorld == 0) return; // Защита


            centerX = worldPosUnderCursor.X - (((double)e.X / canvasSerpinsky.Width) - 0.5) * newViewWidthWorld;
            centerY = worldPosUnderCursor.Y + (((double)e.Y / canvasSerpinsky.Height) - 0.5) * newViewHeightWorld; // Y в мировых координатах инвертирован относительно экранных Y в ScreenToWorld, поэтому +


            canvasSerpinsky.Invalidate();
            if (Math.Abs((double)nudZoom.Value - currentZoom) > 0.00001)
            {
                nudZoom.ValueChanged -= ParamControl_Changed;
                // Убедимся, что новое значение не выходит за пределы nudZoom
                decimal newNudZoomValue = (decimal)currentZoom;
                if (newNudZoomValue < nudZoom.Minimum) newNudZoomValue = nudZoom.Minimum;
                if (newNudZoomValue > nudZoom.Maximum) newNudZoomValue = nudZoom.Maximum;
                nudZoom.Value = newNudZoomValue;
                nudZoom.ValueChanged += ParamControl_Changed;
                // ScheduleRender(); // ParamControl_Changed вызовет ScheduleRender
            }
            // else { ScheduleRender(); } // Если значение nudZoom не изменилось, но currentZoom изменился (например, из-за округления decimal)
            ScheduleRender(); // Всегда планируем рендер после зума
        }

        private void CanvasSerpinsky_MouseDown(object sender, MouseEventArgs e)
        { if (isHighResRendering) return; if (e.Button == MouseButtons.Left) { panning = true; panStart = e.Location; canvasSerpinsky.Cursor = Cursors.Hand; } }

        private void CanvasSerpinsky_MouseMove(object sender, MouseEventArgs e)
        {
            if (isHighResRendering || !panning) return;
            if (canvasSerpinsky.Width <= 0 || canvasSerpinsky.Height <= 0 || currentZoom == 0) return; // Добавил currentZoom == 0

            double aspect = (double)canvasSerpinsky.Width / canvasSerpinsky.Height;
            double viewHeightWorld = BASE_SCALE / currentZoom;
            double viewWidthWorld = viewHeightWorld * aspect;

            if (canvasSerpinsky.Width == 0 || canvasSerpinsky.Height == 0) return; // Защита от деления на ноль

            double worldUnitsPerPixelX = viewWidthWorld / canvasSerpinsky.Width;
            double worldUnitsPerPixelY = viewHeightWorld / canvasSerpinsky.Height;

            double pixelDeltaX = e.X - panStart.X;
            double pixelDeltaY = e.Y - panStart.Y;

            centerX -= pixelDeltaX * worldUnitsPerPixelX;
            centerY += pixelDeltaY * worldUnitsPerPixelY;

            panStart = e.Location;
            canvasSerpinsky.Invalidate();
            ScheduleRender();
        }

        private void CanvasSerpinsky_MouseUp(object sender, MouseEventArgs e)
        { if (isHighResRendering) return; if (e.Button == MouseButtons.Left) { panning = false; canvasSerpinsky.Cursor = Cursors.Default; } }

        #endregion

        #region Сохранение и управление UI

        private void btnRender_Click(object sender, EventArgs e)
        {
            // Отменяем текущий предпросмотр, если он есть, и запускаем новый
            previewRenderCts?.Cancel(); // Это приведет к остановке RenderTimer_Tick, если он активен
            // ScheduleRender() сделает все остальное (остановит таймер, запустит новый)
            ScheduleRender();
        }

        private async void btnSavePNG_Click(object sender, EventArgs e)
        {
            if (isRenderingPreview) // Если идет предпросмотр, отменяем его
            {
                previewRenderCts?.Cancel();
                // Даем немного времени на завершение отмены предпросмотра, прежде чем начинать сохранение
                // Это можно сделать более изящно, ожидая завершения isRenderingPreview
                // Но для простоты пока оставим так или пользователь должен будет нажать еще раз.
                // Лучше всего - дождаться, пока isRenderingPreview станет false.
                // Однако, это может заблокировать UI, если сделать просто цикл ожидания.
                // Более правильный подход - дизейблить кнопку сохранения, пока isRenderingPreview.
                // Но сейчас мы просто прерываем и продолжаем.
                // Можно добавить небольшую задержку Task.Delay, но это не очень надежно.
                // Попробуем просто продолжить, предполагая, что отмена сработает быстро.
            }

            if (isHighResRendering) { MessageBox.Show("Процесс сохранения уже запущен.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }

            int saveWidth = (int)nudW2.Value; int saveHeight = (int)nudH2.Value;
            if (saveWidth <= 0 || saveHeight <= 0) { MessageBox.Show("Размеры изображения для сохранения должны быть больше нуля.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss"); string suggestedFileName = $"serpinski_{timestamp}.png";
            using (SaveFileDialog saveDialog = new SaveFileDialog { Filter = "PNG Image|*.png", Title = "Сохранить Треугольник Серпинского", FileName = suggestedFileName })
            {
                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    isHighResRendering = true;
                    SetMainControlsEnabled(false); // Дизейблим основные контролы
                    UpdateAbortButtonState();      // Включаем кнопку отмены для рендера высокого разрешения

                    progressPNGSerpinsky.Visible = true;
                    UpdateProgressBar(progressPNGSerpinsky, 0);

                    int iterations = (int)nudIterations.Value;
                    int numThreads = cbCPUThreads.SelectedItem.ToString() == "Auto" ? Environment.ProcessorCount : Convert.ToInt32(cbCPUThreads.SelectedItem);
                    bool isGeometric = FractalTypeIsGeometry.Checked;
                    CheckBox colorColorCb = this.Controls.Find("colorColor", true).FirstOrDefault() as CheckBox;
                    bool useColorMode = colorColorCb?.Checked ?? false;
                    bool useBW = renderBW.Checked; bool useGrayscale = colorGrayscale.Checked;
                    Color currentFrColor = fractalColor; Color currentBgColor = backgroundColor;
                    double captureZoom = currentZoom; double captureCenterX = centerX; double captureCenterY = centerY;

                    highResRenderCts?.Dispose(); // Освобождаем предыдущий, если был
                    highResRenderCts = new CancellationTokenSource();
                    CancellationToken token = highResRenderCts.Token;

                    try
                    {
                        Bitmap highResBitmap = null;
                        await Task.Run(() => // Используем await для Task.Run, чтобы не блокировать UI
                        {
                            // Вся логика рендеринга должна быть внутри Task.Run
                            Bitmap tempBmp = new Bitmap(saveWidth, saveHeight, PixelFormat.Format32bppArgb);
                            token.ThrowIfCancellationRequested(); // Проверка перед LockBits
                            BitmapData tempData = tempBmp.LockBits(new Rectangle(0, 0, saveWidth, saveHeight), ImageLockMode.WriteOnly, tempBmp.PixelFormat);
                            token.ThrowIfCancellationRequested();

                            int tempBpp = Image.GetPixelFormatSize(tempBmp.PixelFormat) / 8;
                            int tempStride = tempData.Stride;
                            byte[] tempBuffer = new byte[Math.Abs(tempStride) * saveHeight];

                            Color effectiveBgColor;
                            if (useBW) effectiveBgColor = Color.White;
                            else if (useGrayscale) effectiveBgColor = Color.White;
                            else effectiveBgColor = currentBgColor;

                            for (int y_bg = 0; y_bg < saveHeight; y_bg++)
                            {
                                token.ThrowIfCancellationRequested();
                                for (int x_bg = 0; x_bg < saveWidth; x_bg++)
                                {
                                    int idx_bg = y_bg * tempStride + x_bg * tempBpp;
                                    tempBuffer[idx_bg + 0] = effectiveBgColor.B;
                                    tempBuffer[idx_bg + 1] = effectiveBgColor.G;
                                    tempBuffer[idx_bg + 2] = effectiveBgColor.R;
                                    tempBuffer[idx_bg + 3] = effectiveBgColor.A;
                                }
                            }
                            token.ThrowIfCancellationRequested();

                            if (isGeometric)
                            {
                                RenderSierpinskiGeometric(token, tempBuffer, saveWidth, saveHeight, tempStride, tempBpp, captureZoom, captureCenterX, captureCenterY, iterations, useColorMode, useBW, useGrayscale, currentFrColor, numThreads, (progress) => UpdateProgressBar(progressPNGSerpinsky, progress));
                            }
                            else
                            {
                                RenderSierpinskiChaos(token, tempBuffer, saveWidth, saveHeight, tempStride, tempBpp, captureZoom, captureCenterX, captureCenterY, iterations, useColorMode, useBW, useGrayscale, currentFrColor, numThreads, (progress) => UpdateProgressBar(progressPNGSerpinsky, progress));
                            }
                            token.ThrowIfCancellationRequested(); // Проверка перед Marshal.Copy

                            Marshal.Copy(tempBuffer, 0, tempData.Scan0, tempBuffer.Length);
                            tempBmp.UnlockBits(tempData);
                            highResBitmap = tempBmp; // Присваиваем результат
                        }, token); // Передаем токен в Task.Run

                        // После завершения Task.Run (и если не было отмены)
                        if (!token.IsCancellationRequested && highResBitmap != null)
                        {
                            highResBitmap.Save(saveDialog.FileName, ImageFormat.Png);
                            MessageBox.Show("Изображение успешно сохранено!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else if (token.IsCancellationRequested)
                        {
                            MessageBox.Show("Сохранение было отменено.", "Отмена", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                        highResBitmap?.Dispose(); // Освобождаем bitmap в любом случае
                    }
                    catch (OperationCanceledException)
                    {
                        MessageBox.Show("Сохранение было отменено.", "Отмена", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        // highResBitmap уже должен быть null или будет очищен в finally, если создавался локально в task
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Произошла ошибка при сохранении: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        isHighResRendering = false;
                        SetMainControlsEnabled(true); // Включаем контролы обратно
                        UpdateAbortButtonState();     // Обновляем кнопку отмены (выключится)
                        progressPNGSerpinsky.Visible = false;
                        UpdateProgressBar(progressPNGSerpinsky, 0);
                        highResRenderCts?.Dispose(); // Освобождаем CTS для сохранения
                        highResRenderCts = null;
                    }
                }
                else // Пользователь отменил SaveFileDialog
                {
                    // Ничего не делаем, isHighResRendering не был установлен в true
                }
            }
        }

        private void SetMainControlsEnabled(bool enabled)
        {
            // Этот метод вызывается из разных потоков, поэтому нужна проверка InvokeRequired
            Action action = () =>
            {
                panel1.Enabled = enabled; // Если abortRender на panel1, он тоже будет затронут
                // Если abortRender НЕ на panel1, то его состояние управляется отдельно UpdateAbortButtonState

                btnRender.Enabled = enabled;
                btnSavePNG.Enabled = enabled;

                // Управление элементами выбора цвета
                FractalTypeIsGeometry.Enabled = enabled;
                FractalTypeIsChaos.Enabled = enabled;
                nudIterations.Enabled = enabled;
                nudZoom.Enabled = enabled;
                cbCPUThreads.Enabled = enabled;
                //groupBoxColors.Enabled = enabled; // Предполагая, что все чекбоксы цвета в GroupBox
                //nudW2, nudH2 (для сохранения) также должны быть здесь, если они на panel1 или подобном
                nudW2.Enabled = enabled;
                nudH2.Enabled = enabled;


                if (enabled)
                {
                    UpdatePaletteCanvas(); // Обновляем палитру, если контролы включаются
                }
                else
                {
                    canvasPalette.Enabled = false; // Отключаем палитру при блокировке
                }
            };

            if (this.InvokeRequired)
            {
                try { this.Invoke(action); } catch (ObjectDisposedException) { /* Форма закрывается */ }
            }
            else
            {
                action();
            }
        }

        // Новый метод для управления состоянием кнопки отмены
        private void UpdateAbortButtonState()
        {
            if (this.IsDisposed || !this.IsHandleCreated || abortRender == null || abortRender.IsDisposed) return;

            Action action = () =>
            {
                // Кнопка доступна, если идет любой из видов рендеринга
                abortRender.Enabled = isRenderingPreview || isHighResRendering;
            };

            if (abortRender.InvokeRequired)
            {
                try { abortRender.Invoke(action); } catch (ObjectDisposedException) { /* Контрол или форма закрывается */ }
            }
            else
            {
                action();
            }
        }


        private void UpdateProgressBar(ProgressBar pb, int percentage)
        {
            if (pb == null || pb.IsDisposed || !pb.IsHandleCreated) return;

            // Убедимся, что percentage в допустимых границах ProgressBar
            int val = Math.Min(pb.Maximum, Math.Max(pb.Minimum, percentage));

            try
            {
                pb.Invoke((Action)(() =>
                {
                    if (pb.IsHandleCreated && !pb.IsDisposed) pb.Value = val;
                }));
            }
            catch (ObjectDisposedException) { /* Прогресс-бар или форма уже удалены */ }
            catch (InvalidOperationException) { /* Может возникнуть, если хэндл уничтожается во время вызова Invoke */ }
        }

        #endregion

        #region Управление цветом и палитрой

        private void cancasPalette_Click(object sender, EventArgs e) // Убедитесь, что имя контрола `canvasPalette`, а не `cancasPalette`
        {
            CheckBox colorColorCb = this.Controls.Find("colorColor", true).FirstOrDefault() as CheckBox;
            if (!(colorColorCb?.Checked ?? false)) return;

            if (colorDialog.ShowDialog() == DialogResult.OK)
            {
                if (colorFractal.Checked) fractalColor = colorDialog.Color;
                else if (colorBackground.Checked) backgroundColor = colorDialog.Color;
                UpdatePaletteCanvas();
                ScheduleRender();
            }
        }

        private void UpdatePaletteCanvas()
        {
            if (canvasPalette == null || canvasPalette.IsDisposed || !canvasPalette.IsHandleCreated) return;


            CheckBox colorColorCb = this.Controls.Find("colorColor", true).FirstOrDefault() as CheckBox;
            bool isColorModeActive = colorColorCb?.Checked ?? false;

            // Включение/выключение контролов выбора цели цвета и самой палитры
            // Эти контролы должны быть доступны только если активен "цветной" режим.
            // Предполагается, что `label1` - это "Выберите цвет для:" или подобное.
            // Если `colorFractal` и `colorBackground` находятся на `panel1`, то их `Enabled`
            // будет управляться `SetMainControlsEnabled`. Если нет, то здесь:
            if (this.IsHandleCreated && !this.IsDisposed) // Добавим проверку на существование формы
            {
                Action updateUI = () => {
                    if (colorFractal != null && !colorFractal.IsDisposed) colorFractal.Enabled = isColorModeActive;
                    if (colorBackground != null && !colorBackground.IsDisposed) colorBackground.Enabled = isColorModeActive;
                    if (label1 != null && !label1.IsDisposed) label1.Enabled = isColorModeActive;
                    if (canvasPalette != null && !canvasPalette.IsDisposed) canvasPalette.Enabled = isColorModeActive && panel1.Enabled; // Палитра активна, если цветной режим и панель активна
                };

                if (this.InvokeRequired) { try { this.Invoke(updateUI); } catch { /* форма закрывается */ } }
                else { updateUI(); }
            }


            if (canvasPalette.Enabled) // Рисуем на палитре только если она активна
            {
                try
                {
                    using (Graphics g = canvasPalette.CreateGraphics())
                    {
                        if (isColorModeActive)
                        {
                            Color previewColor = colorFractal.Checked ? fractalColor : backgroundColor;
                            g.Clear(previewColor);
                            // Рисуем крестик для лучшей видимости, если цвет палитры сливается с цветом фона формы
                            using (Pen p = new Pen(previewColor.GetBrightness() < 0.5f ? Color.LightGray : Color.DarkGray, 1))
                            {
                                g.DrawLine(p, canvasPalette.Width / 2 - 5, canvasPalette.Height / 2, canvasPalette.Width / 2 + 5, canvasPalette.Height / 2);
                                g.DrawLine(p, canvasPalette.Width / 2, canvasPalette.Height / 2 - 5, canvasPalette.Width / 2, canvasPalette.Height / 2 + 5);
                            }
                        }
                        else if (renderBW.Checked)
                        {
                            g.Clear(Color.White); // Фон для ЧБ режима - белый
                            using (Brush b = new SolidBrush(Color.Black)) { g.FillRectangle(b, 0, 0, canvasPalette.Width / 2, canvasPalette.Height); }
                            // Вторую половину можно оставить белой или показать светло-серый для контраста
                            using (Brush b = new SolidBrush(Color.LightGray)) { g.FillRectangle(b, canvasPalette.Width / 2, 0, canvasPalette.Width / 2, canvasPalette.Height); }
                        }
                        else if (colorGrayscale.Checked)
                        {
                            using (System.Drawing.Drawing2D.LinearGradientBrush lgb = new System.Drawing.Drawing2D.LinearGradientBrush(canvasPalette.ClientRectangle, Color.Gainsboro, Color.DarkSlateGray, 0f))
                            { g.FillRectangle(lgb, canvasPalette.ClientRectangle); }
                        }
                        else
                        {
                            g.Clear(SystemColors.Control); // Цвет по умолчанию, если ничего не выбрано (не должно происходить)
                        }
                    }
                }
                catch (Exception ex) // Например, если canvasPalette был удален между проверкой и CreateGraphics
                {
                    System.Diagnostics.Debug.WriteLine($"Error updating palette: {ex.Message}");
                }
            }
            else // Если палитра отключена, можно ее просто очистить цветом фона контрола
            {
                try
                {
                    using (Graphics g = canvasPalette.CreateGraphics())
                    {
                        g.Clear(SystemColors.ControlDark); // Или другой цвет для неактивного состояния
                    }
                }
                catch { /* Игнор */ }
            }
        }
        #endregion

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            previewRenderCts?.Cancel();
            previewRenderCts?.Dispose();
            highResRenderCts?.Cancel();
            highResRenderCts?.Dispose();

            renderTimer?.Stop();
            renderTimer?.Dispose();
            canvasBitmap?.Dispose();
            colorDialog?.Dispose();
            base.OnFormClosed(e);
        }

        private void abortRender_Click(object sender, EventArgs e)
        {
            bool cancellationRequested = false;
            if (isRenderingPreview && previewRenderCts != null && !previewRenderCts.IsCancellationRequested)
            {
                previewRenderCts.Cancel();
                System.Diagnostics.Debug.WriteLine("Preview render cancellation explicitly requested by user.");
                cancellationRequested = true;
            }

            if (isHighResRendering && highResRenderCts != null && !highResRenderCts.IsCancellationRequested)
            {
                highResRenderCts.Cancel();
                System.Diagnostics.Debug.WriteLine("High-resolution render cancellation explicitly requested by user.");
                cancellationRequested = true;
            }

            if (cancellationRequested)
            {
                // Кнопка отмены будет обновлена в finally блоках соответствующих рендеров,
                // когда isRenderingPreview/isHighResRendering станут false.
                // Можно временно задизейблить ее здесь, чтобы избежать повторных нажатий,
                // но UpdateAbortButtonState() в finally блоках сделает это более надежно.
                // abortRender.Enabled = false; // Немедленная обратная связь
            }
        }
    }
}