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

            nudIterations.Minimum = 0; // Изменено на 0 для возможности сброса
            nudIterations.Maximum = 15;
            nudIterations.Value = 5;

            nudZoom.Minimum = 0.01m; // Установим разумный минимум для зума
            nudZoom.Maximum = 10000000m; // И максимум
            nudZoom.Value = 1m;
            nudZoom.DecimalPlaces = 2;

            // Взаимоисключающие чекбоксы и управление цветом
            FractalTypeIsGeometry.CheckedChanged += FractalType_CheckedChanged;
            FractalTypeIsChaos.CheckedChanged += FractalType_CheckedChanged;

            renderBW.CheckedChanged += ColorMode_CheckedChanged;
            colorGrayscale.CheckedChanged += ColorMode_CheckedChanged;

            // Предположим, что чекбокс colorColorMode (или просто colorColor) существует
            // Убедитесь, что он добавлен в ваш Designer файл.
            // Если его имя colorColor, то используйте его.
            // Пример: this.colorColor.CheckedChanged += ColorChoiceMode_CheckedChanged;
            // Для кода ниже я буду использовать имя colorColor (как вы просили)
            // Если его нет в дизайнере, нужно будет добавить
            // this.Controls.OfType<CheckBox>().FirstOrDefault(cb => cb.Name == "colorColor")?.CheckedChanged += ColorChoiceMode_CheckedChanged;
            // Это плохая практика искать контрол по имени здесь, лучше чтобы он был полем класса.
            // Предположим, вы его добавили и он называется `colorColor`
            // (Если в дизайнере он назван иначе, например, `colorColorModeCheckBox`, замените имя)
            CheckBox colorColorCheckbox = this.Controls.Find("colorColor", true).FirstOrDefault() as CheckBox;
            if (colorColorCheckbox != null)
            {
                colorColorCheckbox.CheckedChanged += ColorChoiceMode_CheckedChanged;
            }


            colorBackground.CheckedChanged += ColorTarget_CheckedChanged;
            colorFractal.CheckedChanged += ColorTarget_CheckedChanged;

            // Начальные значения
            FractalTypeIsGeometry.Checked = true;
            colorGrayscale.Checked = true;      // По умолчанию оттенки серого
            if (colorColorCheckbox != null) colorColorCheckbox.Checked = false; // По умолчанию цветной режим выключен
            colorFractal.Checked = true;

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

        #region Взаимоисключающие CheckBox'ы и управление цветом

        private void FractalType_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox activeCb = sender as CheckBox;
            if (activeCb == null || !activeCb.Checked) return;

            if (activeCb == FractalTypeIsGeometry)
            {
                FractalTypeIsChaos.Checked = false;
                nudIterations.Maximum = 15; // Глубина для геометрического
                nudIterations.Minimum = 0;
                if (nudIterations.Value >= 15) nudIterations.Value = 5;
            }
            else if (activeCb == FractalTypeIsChaos)
            {
                FractalTypeIsGeometry.Checked = false;
                nudIterations.Maximum = int.MaxValue;
                nudIterations.Minimum = 1000;
                if (nudIterations.Value < 1000)
                {
                    nudIterations.Value = 50000;
                }
            }
            ScheduleRender();
        }

        // Обработчик для colorColor (полноцветный режим)
        private void ColorChoiceMode_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox colorColorCb = sender as CheckBox;
            if (colorColorCb == null) return;

            // Временно отписываемся от событий других чекбоксов цвета, чтобы избежать зацикливания
            renderBW.CheckedChanged -= ColorMode_CheckedChanged;
            colorGrayscale.CheckedChanged -= ColorMode_CheckedChanged;

            if (colorColorCb.Checked) // Если выбрали "Цветной"
            {
                if (renderBW.Checked) renderBW.Checked = false;
                if (colorGrayscale.Checked) colorGrayscale.Checked = false;

                // По умолчанию, если выбираем цветной режим, пусть будет активен выбор цвета фрактала
                if (!colorFractal.Checked && !colorBackground.Checked)
                {
                    colorFractal.CheckedChanged -= ColorTarget_CheckedChanged;
                    colorFractal.Checked = true;
                    colorFractal.CheckedChanged += ColorTarget_CheckedChanged;
                }
            }
            else // Если сняли "Цветной"
            {
                // Если ни ЧБ, ни Серый не выбраны, по умолчанию включаем Серый
                if (!renderBW.Checked && !colorGrayscale.Checked)
                {
                    colorGrayscale.Checked = true; // Это вызовет ColorMode_CheckedChanged
                }
            }

            // Подписываемся обратно
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

            // Временно отписываемся от colorColor, чтобы избежать зацикливания
            if (colorColorCb != null) colorColorCb.CheckedChanged -= ColorChoiceMode_CheckedChanged;


            if (activeCb.Checked) // Если выбрали renderBW или colorGrayscale
            {
                // Снимаем галочку с colorColor, если она была установлена
                if (colorColorCb != null && colorColorCb.Checked)
                {
                    colorColorCb.Checked = false;
                }

                // Взаимоисключение между renderBW и colorGrayscale
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
            else // Если сняли галочку с renderBW или colorGrayscale
            {
                // Если оба (renderBW и colorGrayscale) сняты, и colorColor тоже не выбран,
                // то по умолчанию восстанавливаем colorGrayscale.
                if (!renderBW.Checked && !colorGrayscale.Checked && (colorColorCb == null || !colorColorCb.Checked))
                {
                    colorGrayscale.CheckedChanged -= ColorMode_CheckedChanged;
                    colorGrayscale.Checked = true; // Это может вызвать рекурсию, если не отписаться/подписаться правильно
                    colorGrayscale.CheckedChanged += ColorMode_CheckedChanged;
                }
            }

            // Подписываемся обратно на colorColor
            if (colorColorCb != null) colorColorCb.CheckedChanged += ColorChoiceMode_CheckedChanged;

            UpdatePaletteCanvas();
            ScheduleRender();
        }


        private void ColorTarget_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox activeCb = sender as CheckBox;
            if (activeCb == null || !activeCb.Checked)
            {
                // Если оба сняты, но режим цветной активен, вернем colorFractal по умолчанию
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

        private void ParamControl_Changed(object sender, EventArgs e) // Вызывается для nudZoom, nudIterations и cbCPUThreads
        {
            if (isHighResRendering) return;

            if (sender == nudZoom)
            {
                currentZoom = Math.Max((double)nudZoom.Minimum, (double)nudZoom.Value);
                // nudZoom.Value уже обновлено элементом управления
            }
            // Для nudIterations и cbCPUThreads параметры будут считаны перед рендерингом
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
                if (isRenderingPreview) renderTimer.Start();
                return;
            }

            isRenderingPreview = true;
            SetMainControlsEnabled(false);

            previewRenderCts?.Dispose();
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
                                  useColorMode, useBW, useGrayscale, // Передаем новые флаги
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
                UpdateProgressBar(progressBarSerpinsky, 0);
            }
        }

        private void RenderFractal(CancellationToken token, int renderWidth, int renderHeight,
                           double zoomVal, double cX, double cY,
                           int iterationsVal, int numThreadsVal, bool isGeometricVal,
                           bool useColorModeVal, bool useBWVal, bool useGrayscaleVal, // Новые флаги
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
                byte[] buffer = new byte[Math.Abs(stride) * renderHeight];
                IntPtr scan0 = bmpData.Scan0;

                Color currentBgColor;
                if (useBWVal) currentBgColor = Color.White;
                else if (useGrayscaleVal) currentBgColor = Color.White;
                else currentBgColor = bgColor; // Если useColorModeVal == true

                for (int y = 0; y < renderHeight; y++)
                {
                    for (int x = 0; x < renderWidth; x++)
                    {
                        int idx = y * stride + x * bytesPerPixel;
                        buffer[idx + 0] = currentBgColor.B;
                        buffer[idx + 1] = currentBgColor.G;
                        buffer[idx + 2] = currentBgColor.R;
                        buffer[idx + 3] = currentBgColor.A;
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
                        canvasSerpinsky.Invalidate();
                        bmp = null;
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
                                     bool useColorMode, bool useBW, bool useGrayscale, Color frColor,
                                     int numThreads, Action<int> reportProgress)
        {
            if (depth < 0)
            {
                reportProgress(100);
                return;
            }

            Color actualFractalColor;
            if (useBW) actualFractalColor = Color.Black;
            else if (useGrayscale) actualFractalColor = Color.FromArgb(255, 50, 50, 50); // Непрозрачный серый
            else actualFractalColor = frColor; // Если useColorMode == true

            double side = 1.0;
            double height_triangle = side * Math.Sqrt(3) / 2.0;

            // Центрируем по Y, чтобы основание было внизу, а вершина вверху относительно 0.
            // Сдвигаем весь треугольник так, чтобы его центр масс (пересечение медиан) был в (0,0)
            // Высота от основания до центра масс = height_triangle / 3
            // Высота от вершины до центра масс = height_triangle * 2 / 3
            float y_offset = 0; // (float)(height_triangle / 2.0 - height_triangle / 3.0);

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
                            if (currentCount == totalTrianglesToDraw || (totalTrianglesToDraw > 100 && currentCount % (totalTrianglesToDraw / 100) == 0))
                            { reportProgress((int)Math.Min(100, (100 * currentCount / totalTrianglesToDraw))); }
                            else if (totalTrianglesToDraw <= 100)
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
            try { generateDrawingTasksRecursive(depth, p1_world, p2_world, p3_world); }
            catch (OperationCanceledException) { reportProgress(drawnTrianglesCounter > 0 && totalTrianglesToDraw > 0 ? (int)(100 * drawnTrianglesCounter / totalTrianglesToDraw) : 0); return; }
            if (token.IsCancellationRequested) { reportProgress(drawnTrianglesCounter > 0 && totalTrianglesToDraw > 0 ? (int)(100 * drawnTrianglesCounter / totalTrianglesToDraw) : 0); return; }

            var po = new ParallelOptions { MaxDegreeOfParallelism = numThreads, CancellationToken = token };
            try { Parallel.ForEach(drawingActions, po, drawingAction => drawingAction()); }
            catch (OperationCanceledException) { }
            finally
            {
                if (token.IsCancellationRequested) { reportProgress(drawnTrianglesCounter > 0 && totalTrianglesToDraw > 0 ? (int)Math.Min(100, (100 * drawnTrianglesCounter / totalTrianglesToDraw)) : 0); }
                else { reportProgress(100); }
            }
        }

        private void FillTriangleToBuffer(byte[] buffer, int W, int H, int stride, int bpp, PointF p1, PointF p2, PointF p3, Color color)
        {
            PointF[] v = { p1, p2, p3 };
            Array.Sort(v, (a, b) => a.Y.CompareTo(b.Y));
            PointF vTop = v[0]; PointF vMid = v[1]; PointF vBot = v[2];
            byte cB = color.B; byte cG = color.G; byte cR = color.R; byte cA = color.A;

            Action<float, float, float> fillScanline = (yScan, xStart, xEnd) =>
            {
                if (yScan < 0 || yScan >= H) return;
                int startX = (int)Math.Max(0, Math.Min(xStart, xEnd));
                int endX = (int)Math.Min(W - 1, Math.Max(xStart, xEnd));
                for (int x = startX; x <= endX; x++)
                {
                    int idx = (int)yScan * stride + x * bpp;
                    if (idx >= 0 && idx + (bpp - 1) < buffer.Length)
                    { buffer[idx + 0] = cB; buffer[idx + 1] = cG; buffer[idx + 2] = cR; buffer[idx + 3] = cA; }
                }
            };

            float invSlope1, invSlope2;
            float curX1, curX2;

            // Верхняя половина
            if (vMid.Y - vTop.Y > 0)
            {
                invSlope1 = (vMid.X - vTop.X) / (vMid.Y - vTop.Y);
                invSlope2 = (vBot.X - vTop.X) / (vBot.Y - vTop.Y);
                curX1 = vTop.X; curX2 = vTop.X;
                for (float y = vTop.Y; y < vMid.Y; y += 1.0f)
                {
                    fillScanline(y, curX1, curX2);
                    curX1 += invSlope1; curX2 += invSlope2;
                }
            }
            // Нижняя половина
            if (vBot.Y - vMid.Y > 0)
            {
                invSlope1 = (vBot.X - vMid.X) / (vBot.Y - vMid.Y);
                // invSlope2 уже посчитан и curX2 уже в правильной позиции (или почти)
                // curX1 для линии vMid-vBot должен начаться с vMid.X
                curX1 = vMid.X;
                // curX2 продолжает линию vTop-vBot, так что его нужно обновить до текущего Y
                if (vBot.Y - vTop.Y > 0) curX2 = vTop.X + (vBot.X - vTop.X) * (vMid.Y - vTop.Y) / (vBot.Y - vTop.Y); else curX2 = vBot.X;


                for (float y = vMid.Y; y <= vBot.Y; y += 1.0f)
                {
                    fillScanline(y, curX1, curX2);
                    curX1 += invSlope1;
                    if (vBot.Y - vTop.Y > 0) curX2 += (vBot.X - vTop.X) / (vBot.Y - vTop.Y); // invSlope2 для vTop-vBot
                    else if (y < vBot.Y) curX2 = vBot.X; // если vTop-vBot была горизонтальной или вырожденной
                }
            }
            // Если треугольник - это горизонтальная линия (все Y одинаковы), то ничего не нарисуется.
            // Это можно обработать отдельно, если нужно рисовать линии. Но для заливки это не критично.
        }


        private void RenderSierpinskiChaos(CancellationToken token, byte[] buffer, int W, int H, int stride, int bpp,
                                   double zoom, double cX, double cY, int numPoints,
                                   bool useColorMode, bool useBW, bool useGrayscale, Color frColor,
                                   int numThreads, Action<int> reportProgress)
        {
            Color pointColor;
            if (useBW) pointColor = Color.Black;
            else if (useGrayscale) pointColor = Color.FromArgb(255, 100, 100, 100); // Средне-серый непрозрачный
            else pointColor = frColor; // Если useColorMode == true

            byte cB = pointColor.B; byte cG = pointColor.G; byte cR = pointColor.R; byte cA = pointColor.A;
            double side = 1.0; double height_triangle = side * Math.Sqrt(3) / 2.0;
            PointF[] vertices_world = new PointF[3];
            float y_offset = 0; // (float)(height_triangle / 2.0 - height_triangle / 3.0);
            vertices_world[0] = new PointF(0, (float)(height_triangle * 2.0 / 3.0) + y_offset);
            vertices_world[1] = new PointF((float)(-side / 2.0), (float)(-height_triangle / 3.0) + y_offset);
            vertices_world[2] = new PointF((float)(side / 2.0), (float)(-height_triangle / 3.0) + y_offset);

            Random masterRand = new Random(); // Один мастер-Random для инициализации локальных
            PointF initialPoint_world = vertices_world[0];
            for (int i = 0; i < 20; ++i) { initialPoint_world = MidPoint(initialPoint_world, vertices_world[masterRand.Next(3)]); }

            long totalDrawnPoints = 0;
            int pointsPerThread = Math.Max(1, numPoints / numThreads); // Убедимся, что не 0

            Parallel.For(0, numThreads, new ParallelOptions { CancellationToken = token }, threadId =>
            {
                Random localRand = new Random(masterRand.Next() + threadId);
                PointF localCurrentPoint_world = initialPoint_world;
                if (threadId > 0) { for (int i = 0; i < 5; ++i) { localCurrentPoint_world = MidPoint(localCurrentPoint_world, vertices_world[localRand.Next(3)]); } }

                int pointsForThisThread = (threadId == numThreads - 1) ? (numPoints - (numThreads - 1) * pointsPerThread) : pointsPerThread;

                for (int i = 0; i < pointsForThisThread; i++)
                {
                    if (token.IsCancellationRequested) break;
                    localCurrentPoint_world = MidPoint(localCurrentPoint_world, vertices_world[localRand.Next(3)]);
                    Point screenPoint = Point.Round(WorldToScreen(localCurrentPoint_world, W, H, zoom, cX, cY));
                    if (screenPoint.X >= 0 && screenPoint.X < W && screenPoint.Y >= 0 && screenPoint.Y < H)
                    {
                        int idx = screenPoint.Y * stride + screenPoint.X * bpp;
                        if (idx >= 0 && idx + bpp - 1 < buffer.Length)
                        { // Проверка границ буфера
                            buffer[idx + 0] = cB; buffer[idx + 1] = cG; buffer[idx + 2] = cR; buffer[idx + 3] = cA;
                        }
                    }
                    if (i % 1000 == 0)
                    {
                        long currentTotal = Interlocked.Add(ref totalDrawnPoints, (i == 0 && threadId == 0 ? 1 : 1000)); // прибавляем 1000, кроме самой первой точки
                        if (numPoints > 0) reportProgress((int)Math.Min(100, (100 * currentTotal / numPoints)));
                    }
                }
            });
            reportProgress(100);
        }

        #endregion

        #region Вспомогательные функции для рендеринга (координаты, точки)

        private PointF WorldToScreen(PointF worldPoint, int screenWidth, int screenHeight, double zoomVal, double centerXVal, double centerYVal)
        {
            double aspect = (double)screenWidth / screenHeight;
            double viewHeightWorld = BASE_SCALE / zoomVal;
            double viewWidthWorld = viewHeightWorld * aspect;

            double minRe = centerXVal - viewWidthWorld / 2.0;
            double maxIm = centerYVal + viewHeightWorld / 2.0; // Верхняя граница Y в мировых

            float screenX = (float)(((worldPoint.X - minRe) / viewWidthWorld) * screenWidth);
            float screenY = (float)(((maxIm - worldPoint.Y) / viewHeightWorld) * screenHeight);

            return new PointF(screenX, screenY);
        }

        private PointF ScreenToWorld(Point screenPoint, int screenWidth, int screenHeight, double zoomVal, double centerXVal, double centerYVal)
        {
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
            else effectiveBgColor = backgroundColor; // Если useColorMode

            if (canvasBitmap == null || canvasSerpinsky.Width <= 0 || canvasSerpinsky.Height <= 0)
            {
                e.Graphics.Clear(effectiveBgColor);
                return;
            }

            double rZoom = renderedZoom; double rCX = renderedCenterX; double rCY = renderedCenterY;
            double cZoom = currentZoom; double cCX = centerX; double cCY = centerY;

            if (rZoom <= 0 || cZoom <= 0)
            { e.Graphics.Clear(effectiveBgColor); e.Graphics.DrawImageUnscaled(canvasBitmap, Point.Empty); return; }

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

            double cAspect = (double)canvasSerpinsky.Width / canvasSerpinsky.Height;
            double newViewHeightWorld = BASE_SCALE / currentZoom;
            double newViewWidthWorld = newViewHeightWorld * cAspect;
            centerX = worldPosUnderCursor.X - (((double)e.X / canvasSerpinsky.Width) - 0.5) * newViewWidthWorld;
            centerY = worldPosUnderCursor.Y - (0.5 - ((double)e.Y / canvasSerpinsky.Height)) * newViewHeightWorld; // Y инвертирован в ScreenToWorld

            canvasSerpinsky.Invalidate();
            if (Math.Abs((double)nudZoom.Value - currentZoom) > 0.00001)
            {
                nudZoom.ValueChanged -= ParamControl_Changed;
                nudZoom.Value = (decimal)currentZoom;
                nudZoom.ValueChanged += ParamControl_Changed;
                ScheduleRender();
            }
            else { ScheduleRender(); }
        }

        private void CanvasSerpinsky_MouseDown(object sender, MouseEventArgs e)
        { if (isHighResRendering) return; if (e.Button == MouseButtons.Left) { panning = true; panStart = e.Location; } }

        private void CanvasSerpinsky_MouseMove(object sender, MouseEventArgs e)
        {
            if (isHighResRendering || !panning) return;
            if (canvasSerpinsky.Width <= 0 || canvasSerpinsky.Height <= 0) return;

            double aspect = (double)canvasSerpinsky.Width / canvasSerpinsky.Height;
            double viewHeightWorld = BASE_SCALE / currentZoom;
            double viewWidthWorld = viewHeightWorld * aspect;

            double worldUnitsPerPixelX = viewWidthWorld / canvasSerpinsky.Width;
            double worldUnitsPerPixelY = viewHeightWorld / canvasSerpinsky.Height;

            double pixelDeltaX = e.X - panStart.X;
            double pixelDeltaY = e.Y - panStart.Y;

            centerX -= pixelDeltaX * worldUnitsPerPixelX;
            centerY += pixelDeltaY * worldUnitsPerPixelY; // Y мировой инвертирован относительно экранного

            panStart = e.Location;
            canvasSerpinsky.Invalidate();
            ScheduleRender();
        }

        private void CanvasSerpinsky_MouseUp(object sender, MouseEventArgs e)
        { if (isHighResRendering) return; if (e.Button == MouseButtons.Left) { panning = false; } }

        #endregion

        #region Сохранение и управление UI

        private void btnRender_Click(object sender, EventArgs e) { ScheduleRender(); }

        private async void btnSavePNG_Click(object sender, EventArgs e)
        {
            if (isHighResRendering) { MessageBox.Show("Сохранение уже идет.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            int saveWidth = (int)nudW2.Value; int saveHeight = (int)nudH2.Value;
            if (saveWidth <= 0 || saveHeight <= 0) { MessageBox.Show("Размеры > 0.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss"); string suggestedFileName = $"serpinski_{timestamp}.png";
            using (SaveFileDialog saveDialog = new SaveFileDialog { Filter = "PNG Image|*.png", Title = "Сохранить Треугольник Серпинского", FileName = suggestedFileName })
            {
                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    isHighResRendering = true; SetMainControlsEnabled(false); UpdateProgressBar(progressPNGSerpinsky, 0); progressPNGSerpinsky.Visible = true;
                    int iterations = (int)nudIterations.Value;
                    int numThreads = cbCPUThreads.SelectedItem.ToString() == "Auto" ? Environment.ProcessorCount : Convert.ToInt32(cbCPUThreads.SelectedItem);
                    bool isGeometric = FractalTypeIsGeometry.Checked;
                    CheckBox colorColorCb = this.Controls.Find("colorColor", true).FirstOrDefault() as CheckBox;
                    bool useColorMode = colorColorCb?.Checked ?? false;
                    bool useBW = renderBW.Checked; bool useGrayscale = colorGrayscale.Checked;
                    Color currentFrColor = fractalColor; Color currentBgColor = backgroundColor;
                    double captureZoom = currentZoom; double captureCenterX = centerX; double captureCenterY = centerY;
                    try
                    {
                        Bitmap highResBitmap = await Task.Run(() =>
                        {
                            Bitmap tempBmp = new Bitmap(saveWidth, saveHeight, PixelFormat.Format32bppArgb);
                            BitmapData tempData = tempBmp.LockBits(new Rectangle(0, 0, saveWidth, saveHeight), ImageLockMode.WriteOnly, tempBmp.PixelFormat);
                            int tempBpp = Image.GetPixelFormatSize(tempBmp.PixelFormat) / 8; int tempStride = tempData.Stride; byte[] tempBuffer = new byte[Math.Abs(tempStride) * saveHeight];
                            Color effectiveBgColor;
                            if (useBW) effectiveBgColor = Color.White; else if (useGrayscale) effectiveBgColor = Color.White; else effectiveBgColor = currentBgColor;
                            for (int y_bg = 0; y_bg < saveHeight; y_bg++) { for (int x_bg = 0; x_bg < saveWidth; x_bg++) { int idx_bg = y_bg * tempStride + x_bg * tempBpp; tempBuffer[idx_bg + 0] = effectiveBgColor.B; tempBuffer[idx_bg + 1] = effectiveBgColor.G; tempBuffer[idx_bg + 2] = effectiveBgColor.R; tempBuffer[idx_bg + 3] = effectiveBgColor.A; } }
                            if (isGeometric) { RenderSierpinskiGeometric(CancellationToken.None, tempBuffer, saveWidth, saveHeight, tempStride, tempBpp, captureZoom, captureCenterX, captureCenterY, iterations, useColorMode, useBW, useGrayscale, currentFrColor, numThreads, (progress) => UpdateProgressBar(progressPNGSerpinsky, progress)); }
                            else { RenderSierpinskiChaos(CancellationToken.None, tempBuffer, saveWidth, saveHeight, tempStride, tempBpp, captureZoom, captureCenterX, captureCenterY, iterations, useColorMode, useBW, useGrayscale, currentFrColor, numThreads, (progress) => UpdateProgressBar(progressPNGSerpinsky, progress)); }
                            Marshal.Copy(tempBuffer, 0, tempData.Scan0, tempBuffer.Length); tempBmp.UnlockBits(tempData); return tempBmp;
                        });
                        highResBitmap.Save(saveDialog.FileName, ImageFormat.Png); highResBitmap.Dispose(); MessageBox.Show("Сохранено!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                    finally { isHighResRendering = false; SetMainControlsEnabled(true); progressPNGSerpinsky.Visible = false; UpdateProgressBar(progressPNGSerpinsky, 0); }
                }
            }
        }

        private void SetMainControlsEnabled(bool enabled)
        {
            Action action = () =>
            {
                panel1.Enabled = enabled;
                btnRender.Enabled = enabled;
                btnSavePNG.Enabled = enabled;
                if (enabled) UpdatePaletteCanvas(); else canvasPalette.Enabled = false;
            };
            if (this.InvokeRequired) this.Invoke(action); else action();
        }

        private void UpdateProgressBar(ProgressBar pb, int percentage)
        {
            if (pb.IsHandleCreated && !pb.IsDisposed)
            { try { pb.Invoke((Action)(() => { if (pb.IsHandleCreated && !pb.IsDisposed) pb.Value = Math.Min(pb.Maximum, Math.Max(pb.Minimum, percentage)); })); } catch (Exception) { } }
        }

        #endregion

        #region Управление цветом и палитрой

        private void cancasPalette_Click(object sender, EventArgs e)
        {
            CheckBox colorColorCb = this.Controls.Find("colorColor", true).FirstOrDefault() as CheckBox;
            if (!(colorColorCb?.Checked ?? false)) return; // Выбор цвета только если colorColor активен

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
            CheckBox colorColorCb = this.Controls.Find("colorColor", true).FirstOrDefault() as CheckBox;
            bool isColorModeActive = colorColorCb?.Checked ?? false;

            colorFractal.Enabled = isColorModeActive;
            colorBackground.Enabled = isColorModeActive;
            label1.Enabled = isColorModeActive; // "Выберите цвет"
            canvasPalette.Enabled = isColorModeActive; // Сам PictureBox для клика

            if (canvasPalette.IsHandleCreated && !canvasPalette.IsDisposed)
            {
                using (Graphics g = canvasPalette.CreateGraphics())
                {
                    if (isColorModeActive)
                    {
                        Color previewColor = colorFractal.Checked ? fractalColor : backgroundColor;
                        g.Clear(previewColor);
                        using (Pen p = new Pen(previewColor.GetBrightness() < 0.5f ? Color.White : Color.Black, 1))
                        {
                            g.DrawLine(p, canvasPalette.Width / 2 - 5, canvasPalette.Height / 2, canvasPalette.Width / 2 + 5, canvasPalette.Height / 2);
                            g.DrawLine(p, canvasPalette.Width / 2, canvasPalette.Height / 2 - 5, canvasPalette.Width / 2, canvasPalette.Height / 2 + 5);
                        }
                    }
                    else if (renderBW.Checked)
                    {
                        g.Clear(Color.White);
                        using (Brush b = new SolidBrush(Color.Black)) { g.FillRectangle(b, 0, 0, canvasPalette.Width / 2, canvasPalette.Height); }
                        using (Brush b = new SolidBrush(Color.LightGray)) { g.FillRectangle(b, canvasPalette.Width / 2, 0, canvasPalette.Width / 2, canvasPalette.Height); } // Пример фона для ЧБ
                    }
                    else if (colorGrayscale.Checked)
                    {
                        using (System.Drawing.Drawing2D.LinearGradientBrush lgb = new System.Drawing.Drawing2D.LinearGradientBrush(canvasPalette.ClientRectangle, Color.Gainsboro, Color.DarkSlateGray, 0f))
                        { g.FillRectangle(lgb, canvasPalette.ClientRectangle); }
                    }
                    else // На случай, если ни один режим не активен (не должно быть, но для подстраховки)
                    {
                        g.Clear(Color.LightGray);
                    }
                }
            }
        }
        #endregion

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            previewRenderCts?.Cancel(); previewRenderCts?.Dispose();
            renderTimer?.Stop(); renderTimer?.Dispose();
            canvasBitmap?.Dispose(); colorDialog?.Dispose();
            base.OnFormClosed(e);
        }
    }
}