using FractalExplorer.Engines;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace FractalExplorer
{
    public partial class FractalSerpinsky : Form
    {
        private readonly SerpinskyFractalEngine _engine;

        private Bitmap canvasBitmap;
        private volatile bool isRenderingPreview = false;
        private volatile bool isHighResRendering = false;
        private CancellationTokenSource previewRenderCts;
        private CancellationTokenSource highResRenderCts;
        private System.Windows.Forms.Timer renderTimer;

        // Параметры вида остаются в форме для управления UI
        private double currentZoom = 1.0;
        private double centerX = 0.0;
        private double centerY = 0.0;
        private double renderedZoom = 1.0;
        private double renderedCenterX = 0.0;
        private double renderedCenterY = 0.0;
        private Point panStart;
        private bool panning = false;

        private ColorDialog colorDialog;

        public FractalSerpinsky()
        {
            InitializeComponent();
            _engine = new SerpinskyFractalEngine();
            InitializeCustomComponents();
        }

        private void InitializeCustomComponents()
        {
            renderTimer = new System.Windows.Forms.Timer { Interval = 300 };
            renderTimer.Tick += RenderTimer_Tick;

            int cores = Environment.ProcessorCount;
            for (int i = 1; i <= cores; i++) cbCPUThreads.Items.Add(i);
            cbCPUThreads.Items.Add("Auto");
            cbCPUThreads.SelectedItem = "Auto";

            colorDialog = new ColorDialog();

            this.Load += (s, e) => { renderedCenterX = centerX; renderedCenterY = centerY; renderedZoom = currentZoom; ScheduleRender(); };
            canvasSerpinsky.Paint += CanvasSerpinsky_Paint;
            canvasSerpinsky.MouseWheel += CanvasSerpinsky_MouseWheel;
            canvasSerpinsky.MouseDown += CanvasSerpinsky_MouseDown;
            canvasSerpinsky.MouseMove += CanvasSerpinsky_MouseMove;
            canvasSerpinsky.MouseUp += CanvasSerpinsky_MouseUp;
            this.Resize += (s, e) => ScheduleRender();
            canvasSerpinsky.Resize += (s, e) => ScheduleRender();

            nudZoom.ValueChanged += ParamControl_Changed;
            nudIterations.ValueChanged += ParamControl_Changed;
            cbCPUThreads.SelectedIndexChanged += ParamControl_Changed;

            nudIterations.Minimum = 0;
            nudIterations.Maximum = 20; // Увеличен лимит
            nudIterations.Value = 8;

            nudZoom.Minimum = 0.01m;
            nudZoom.Maximum = 10000000m;
            nudZoom.Value = 1m;
            nudZoom.DecimalPlaces = 2;

            FractalTypeIsGeometry.CheckedChanged += FractalType_CheckedChanged;
            FractalTypeIsChaos.CheckedChanged += FractalType_CheckedChanged;
            renderBW.CheckedChanged += ColorMode_CheckedChanged;
            colorGrayscale.CheckedChanged += ColorMode_CheckedChanged;
            colorColor.CheckedChanged += ColorChoiceMode_CheckedChanged;
            colorBackground.CheckedChanged += ColorTarget_CheckedChanged;
            colorFractal.CheckedChanged += ColorTarget_CheckedChanged;

            FractalTypeIsGeometry.Checked = true;
            colorGrayscale.Checked = true;
            UpdatePaletteCanvas();
            UpdateAbortButtonState();
        }

        #region UI Event Handlers (CheckBoxes, etc.)

        private void FractalType_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox activeCb = sender as CheckBox;
            if (activeCb == null || !activeCb.Checked) return;

            if (activeCb == FractalTypeIsGeometry)
            {
                FractalTypeIsChaos.Checked = false;
                nudIterations.Maximum = 20; // Новый лимит
                nudIterations.Minimum = 0;
                // Если значение было от режима "Хаос", сбрасываем на адекватное
                if (nudIterations.Value > 20) nudIterations.Value = 8;
            }
            else // FractalTypeIsChaos
            {
                FractalTypeIsGeometry.Checked = false;
                nudIterations.Maximum = int.MaxValue;
                nudIterations.Minimum = 1000;
                if (nudIterations.Value < 1000) nudIterations.Value = 50000;
            }
            ScheduleRender();
        }

        private void ColorChoiceMode_CheckedChanged(object sender, EventArgs e)
        {
            // Эта и другие функции управления UI остаются без изменений,
            // так как они просто меняют состояние, которое потом читается для движка.
            CheckBox currentCheckBox = sender as CheckBox;
            if (currentCheckBox == null) return;
            renderBW.CheckedChanged -= ColorMode_CheckedChanged;
            colorGrayscale.CheckedChanged -= ColorMode_CheckedChanged;
            if (currentCheckBox.Checked)
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
            else if (!renderBW.Checked && !colorGrayscale.Checked)
            {
                colorGrayscale.Checked = true;
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
            if (colorColor != null) colorColor.CheckedChanged -= ColorChoiceMode_CheckedChanged;
            if (activeCb.Checked)
            {
                if (colorColor != null && colorColor.Checked) colorColor.Checked = false;
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
            else if (!renderBW.Checked && !colorGrayscale.Checked && (colorColor == null || !colorColor.Checked))
            {
                colorGrayscale.CheckedChanged -= ColorMode_CheckedChanged;
                colorGrayscale.Checked = true;
                colorGrayscale.CheckedChanged += ColorMode_CheckedChanged;
            }
            if (colorColor != null) colorColor.CheckedChanged += ColorChoiceMode_CheckedChanged;
            UpdatePaletteCanvas();
            ScheduleRender();
        }

        private void ColorTarget_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox activeCb = sender as CheckBox;
            if (activeCb == null || !activeCb.Checked)
            {
                if ((colorColor != null && colorColor.Checked) && !colorBackground.Checked && !colorFractal.Checked)
                {
                    colorFractal.CheckedChanged -= ColorTarget_CheckedChanged;
                    colorFractal.Checked = true;
                    colorFractal.CheckedChanged += ColorTarget_CheckedChanged;
                }
                UpdatePaletteCanvas();
                return;
            }
            if (activeCb == colorBackground) colorFractal.Checked = false;
            else if (activeCb == colorFractal) colorBackground.Checked = false;
            UpdatePaletteCanvas();
        }

        #endregion

        #region Rendering Logic

        private void ParamControl_Changed(object sender, EventArgs e)
        {
            if (isHighResRendering) return;
            if (sender == nudZoom) currentZoom = (double)nudZoom.Value;
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
            if (isHighResRendering || isRenderingPreview) return;

            isRenderingPreview = true;
            SetMainControlsEnabled(false);
            UpdateAbortButtonState();

            previewRenderCts?.Dispose();
            previewRenderCts = new CancellationTokenSource();
            CancellationToken token = previewRenderCts.Token;

            // Обновляем параметры движка из UI
            UpdateEngineParameters();

            int renderWidth = canvasSerpinsky.Width;
            int renderHeight = canvasSerpinsky.Height;
            if (renderWidth <= 0 || renderHeight <= 0)
            {
                isRenderingPreview = false;
                SetMainControlsEnabled(true);
                UpdateAbortButtonState();
                return;
            }

            try
            {
                // Создаем буфер и битмап для рендеринга
                var bmp = new Bitmap(renderWidth, renderHeight, PixelFormat.Format32bppArgb);
                var bmpData = bmp.LockBits(new Rectangle(0, 0, renderWidth, renderHeight), ImageLockMode.WriteOnly, bmp.PixelFormat);
                var buffer = new byte[bmpData.Stride * renderHeight];

                await Task.Run(() => _engine.RenderToBuffer(
                    buffer, renderWidth, renderHeight, bmpData.Stride, 4,
                    GetThreadCount(), token,
                    (progress) => UpdateProgressBar(progressBarSerpinsky, progress)),
                token);

                token.ThrowIfCancellationRequested();

                // Копируем результат в битмап
                Marshal.Copy(buffer, 0, bmpData.Scan0, buffer.Length);
                bmp.UnlockBits(bmpData);

                // Обновляем UI
                Bitmap oldImage = canvasBitmap;
                canvasBitmap = bmp;
                renderedZoom = currentZoom;
                renderedCenterX = centerX;
                renderedCenterY = centerY;
                canvasSerpinsky.Invalidate();
                oldImage?.Dispose();
            }
            catch (OperationCanceledException) { /* Ignore */ }
            catch (Exception ex) { MessageBox.Show($"Render Error: {ex.Message}"); }
            finally
            {
                isRenderingPreview = false;
                if (!isHighResRendering) SetMainControlsEnabled(true);
                UpdateAbortButtonState();
                UpdateProgressBar(progressBarSerpinsky, 0);
            }
        }

        #endregion

        #region Canvas Interaction (Paint, Mouse)

        // Код для Paint, MouseWheel, MouseDown, MouseMove, MouseUp остается без изменений
        // так как он управляет состоянием формы (centerX, currentZoom и т.д.),
        // а движок это состояние просто читает.
        private void CanvasSerpinsky_Paint(object sender, PaintEventArgs e)
        {
            // Код остается тот же
            bool useBW = renderBW.Checked;
            bool useGrayscale = colorGrayscale.Checked;

            Color effectiveBgColor;
            if (useBW || useGrayscale) effectiveBgColor = Color.White;
            else effectiveBgColor = _engine.BackgroundColor;

            e.Graphics.Clear(effectiveBgColor);

            if (canvasBitmap == null || canvasSerpinsky.Width <= 0 || canvasSerpinsky.Height <= 0) return;

            double rAspect = (double)canvasBitmap.Width / canvasBitmap.Height;
            double rViewHeightWorld = 1.0 / renderedZoom;
            double rViewWidthWorld = rViewHeightWorld * rAspect;
            double rMinRe = renderedCenterX - rViewWidthWorld / 2.0;
            double rMaxIm = renderedCenterY + rViewHeightWorld / 2.0;

            double cAspect = (double)canvasSerpinsky.Width / canvasSerpinsky.Height;
            double cViewHeightWorld = 1.0 / currentZoom;
            double cViewWidthWorld = cViewHeightWorld * cAspect;
            double cMinRe = centerX - cViewWidthWorld / 2.0;
            double cMaxIm = centerY + cViewHeightWorld / 2.0;

            float p1_X = (float)(((rMinRe - cMinRe) / cViewWidthWorld) * canvasSerpinsky.Width);
            float p1_Y = (float)(((cMaxIm - rMaxIm) / cViewHeightWorld) * canvasSerpinsky.Height);
            float w_prime = (float)((rViewWidthWorld / cViewWidthWorld) * canvasSerpinsky.Width);
            float h_prime = (float)((rViewHeightWorld / cViewHeightWorld) * canvasSerpinsky.Height);

            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            e.Graphics.DrawImage(canvasBitmap, new RectangleF(p1_X, p1_Y, w_prime, h_prime));
        }

        private void CanvasSerpinsky_MouseWheel(object sender, MouseEventArgs e)
        {
            // Код остается тот же
            if (isHighResRendering || canvasSerpinsky.Width <= 0) return;
            double zoomFactor = e.Delta > 0 ? 1.2 : 1.0 / 1.2;
            PointF worldPos = ScreenToWorld(e.Location, canvasSerpinsky.Width, canvasSerpinsky.Height, currentZoom, centerX, centerY);
            currentZoom = Math.Max((double)nudZoom.Minimum, Math.Min((double)nudZoom.Maximum, currentZoom * zoomFactor));
            PointF newWorldPos = ScreenToWorld(e.Location, canvasSerpinsky.Width, canvasSerpinsky.Height, currentZoom, centerX, centerY);
            centerX += worldPos.X - newWorldPos.X;
            centerY += worldPos.Y - newWorldPos.Y;

            canvasSerpinsky.Invalidate();
            if (nudZoom.Value != (decimal)currentZoom) nudZoom.Value = (decimal)currentZoom; else ScheduleRender();
        }

        private void CanvasSerpinsky_MouseDown(object sender, MouseEventArgs e) { if (e.Button == MouseButtons.Left) { panning = true; panStart = e.Location; } }

        private void CanvasSerpinsky_MouseMove(object sender, MouseEventArgs e)
        {
            if (!panning) return;
            PointF worldBefore = ScreenToWorld(panStart, canvasSerpinsky.Width, canvasSerpinsky.Height, currentZoom, centerX, centerY);
            PointF worldAfter = ScreenToWorld(e.Location, canvasSerpinsky.Width, canvasSerpinsky.Height, currentZoom, centerX, centerY);
            centerX += worldBefore.X - worldAfter.X;
            centerY += worldBefore.Y - worldAfter.Y;
            panStart = e.Location;
            canvasSerpinsky.Invalidate();
            ScheduleRender();
        }
        private void CanvasSerpinsky_MouseUp(object sender, MouseEventArgs e) { if (e.Button == MouseButtons.Left) panning = false; }

        private PointF ScreenToWorld(Point screenPoint, int screenWidth, int screenHeight, double zoomVal, double centerXVal, double centerYVal)
        {
            double aspect = (double)screenWidth / screenHeight;
            double viewHeightWorld = 1.0 / zoomVal;
            double viewWidthWorld = viewHeightWorld * aspect;

            double minRe = centerXVal - viewWidthWorld / 2.0;
            double maxIm = centerYVal + viewHeightWorld / 2.0;

            float worldX = (float)(minRe + (screenPoint.X / (double)screenWidth) * viewWidthWorld);
            float worldY = (float)(maxIm - (screenPoint.Y / (double)screenHeight) * viewHeightWorld);
            return new PointF(worldX, worldY);
        }

        #endregion

        #region Save & Helpers

        private void UpdateEngineParameters()
        {
            _engine.RenderMode = FractalTypeIsGeometry.Checked ? SerpinskyRenderMode.Geometric : SerpinskyRenderMode.Chaos;
            if (renderBW.Checked) _engine.ColorMode = SerpinskyColorMode.BlackAndWhite;
            else if (colorGrayscale.Checked) _engine.ColorMode = SerpinskyColorMode.Grayscale;
            else _engine.ColorMode = SerpinskyColorMode.CustomColor;

            _engine.Iterations = (int)nudIterations.Value;
            _engine.Zoom = currentZoom;
            _engine.CenterX = centerX;
            _engine.CenterY = centerY;
            // Цвета для _engine.FractalColor и BackgroundColor уже установлены в cancasPalette_Click
        }

        private async void btnSavePNG_Click(object sender, EventArgs e)
        {
            if (isRenderingPreview) previewRenderCts?.Cancel();
            if (isHighResRendering) return;

            int saveWidth = (int)nudW2.Value;
            int saveHeight = (int)nudH2.Value;

            using (var saveDialog = new SaveFileDialog { Filter = "PNG Image|*.png", FileName = $"serpinski_{DateTime.Now:yyyyMMdd_HHmmss}.png" })
            {
                if (saveDialog.ShowDialog() != DialogResult.OK) return;

                isHighResRendering = true;
                SetMainControlsEnabled(false);
                UpdateAbortButtonState();
                progressPNGSerpinsky.Visible = true;
                UpdateProgressBar(progressPNGSerpinsky, 0);

                highResRenderCts = new CancellationTokenSource();
                CancellationToken token = highResRenderCts.Token;

                // Создаем и настраиваем отдельный движок для сохранения
                var saveEngine = new SerpinskyFractalEngine();
                UpdateEngineParameters(); // Настраиваем основной движок
                saveEngine.RenderMode = _engine.RenderMode; // Копируем параметры
                saveEngine.ColorMode = _engine.ColorMode;
                saveEngine.Iterations = _engine.Iterations;
                saveEngine.Zoom = _engine.Zoom;
                saveEngine.CenterX = _engine.CenterX;
                saveEngine.CenterY = _engine.CenterY;
                saveEngine.FractalColor = _engine.FractalColor;
                saveEngine.BackgroundColor = _engine.BackgroundColor;

                try
                {
                    Bitmap highResBitmap = await Task.Run(() => {
                        var bmp = new Bitmap(saveWidth, saveHeight, PixelFormat.Format32bppArgb);
                        var bmpData = bmp.LockBits(new Rectangle(0, 0, saveWidth, saveHeight), ImageLockMode.WriteOnly, bmp.PixelFormat);
                        var buffer = new byte[bmpData.Stride * saveHeight];

                        saveEngine.RenderToBuffer(
                            buffer, saveWidth, saveHeight, bmpData.Stride, 4,
                            GetThreadCount(), token,
                            (progress) => UpdateProgressBar(progressPNGSerpinsky, progress));

                        token.ThrowIfCancellationRequested();
                        Marshal.Copy(buffer, 0, bmpData.Scan0, buffer.Length);
                        bmp.UnlockBits(bmpData);
                        return bmp;
                    }, token);

                    highResBitmap.Save(saveDialog.FileName, ImageFormat.Png);
                    MessageBox.Show("Изображение сохранено!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    highResBitmap.Dispose();
                }
                catch (OperationCanceledException) { MessageBox.Show("Сохранение было отменено.", "Отмена", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
                catch (Exception ex) { MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                finally
                {
                    isHighResRendering = false;
                    SetMainControlsEnabled(true);
                    UpdateAbortButtonState();
                    progressPNGSerpinsky.Visible = false;
                    highResRenderCts.Dispose();
                }
            }
        }

        private int GetThreadCount() => cbCPUThreads.SelectedItem?.ToString() == "Auto" ? Environment.ProcessorCount : Convert.ToInt32(cbCPUThreads.SelectedItem);

        private void cancasPalette_Click(object sender, EventArgs e)
        {
            if (!colorColor.Checked) return;
            if (colorDialog.ShowDialog() == DialogResult.OK)
            {
                if (colorFractal.Checked) _engine.FractalColor = colorDialog.Color;
                else if (colorBackground.Checked) _engine.BackgroundColor = colorDialog.Color;
                UpdatePaletteCanvas();
                ScheduleRender();
            }
        }

        // Методы UpdatePaletteCanvas, SetMainControlsEnabled, UpdateAbortButtonState, UpdateProgressBar
        // остаются практически без изменений, т.к. их логика не зависит от рендеринга.
        private void UpdatePaletteCanvas()
        {
            if (this.IsDisposed || !this.IsHandleCreated) return;
            bool isColorModeActive = colorColor.Checked;
            bool areMainControlsActive = nudZoom.Enabled;

            colorFractal.Enabled = areMainControlsActive && isColorModeActive;
            colorBackground.Enabled = areMainControlsActive && isColorModeActive;
            canvasPalette.Enabled = areMainControlsActive && isColorModeActive;

            using (Graphics g = canvasPalette.CreateGraphics())
            {
                if (canvasPalette.Enabled)
                {
                    if (isColorModeActive)
                    {
                        Color previewColor = colorFractal.Checked ? _engine.FractalColor : _engine.BackgroundColor;
                        g.Clear(previewColor);
                    }
                    else if (renderBW.Checked) { g.Clear(Color.White); g.FillRectangle(Brushes.Black, 0, 0, canvasPalette.Width / 2, canvasPalette.Height); }
                    else if (colorGrayscale.Checked)
                    {
                        using (var lgb = new System.Drawing.Drawing2D.LinearGradientBrush(canvasPalette.ClientRectangle, Color.Gainsboro, Color.DarkSlateGray, 0f))
                            g.FillRectangle(lgb, canvasPalette.ClientRectangle);
                    }
                }
                else g.Clear(SystemColors.ControlDark);
            }
        }

        private void SetMainControlsEnabled(bool enabled)
        {
            panel1.Enabled = enabled;
            // Кнопку отмены нужно обрабатывать отдельно, т.к. она должна быть активна, когда все остальное выключено
            UpdateAbortButtonState();
        }
        private void UpdateAbortButtonState() { if (this.IsHandleCreated) this.Invoke((Action)(() => abortRender.Enabled = isRenderingPreview || isHighResRendering)); }
        private void UpdateProgressBar(ProgressBar pb, int percentage) { if (pb.IsHandleCreated) pb.Invoke((Action)(() => pb.Value = Math.Min(100, Math.Max(0, percentage)))); }
        private void abortRender_Click(object sender, EventArgs e) { if (isRenderingPreview) previewRenderCts?.Cancel(); if (isHighResRendering) highResRenderCts?.Cancel(); }

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
        #endregion
    }
}