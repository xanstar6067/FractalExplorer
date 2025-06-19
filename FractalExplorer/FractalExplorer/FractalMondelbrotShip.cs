// --- START OF FILE FractalMondelbrotShip.cs ---

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Linq;
using System.Globalization; // Для ToString("F...")

namespace FractalDraving
{
    public partial class FractalMondelbrotShip : Form
    {
        private System.Windows.Forms.Timer renderTimer;
        private int maxIterations;
        private decimal thresholdSquared;
        private int threadCount;
        private int width, height;

        private Point panStart;
        private bool panning = false;

        // ИЗМЕНЕНИЕ: Типы полей на decimal
        private decimal zoom = 1.0m;
        private decimal centerX = -0.5m; // Типичный центр для Мандельброта/Корабля
        private decimal centerY = -0.5m; // 
        private const decimal BASE_SCALE = 3.5m; // Начальный видимый диапазон по ширине (примерно)

        private bool isHighResRendering = false;
        private volatile bool isRenderingPreview = false;
        private CancellationTokenSource previewRenderCts;

        private CheckBox[] paletteCheckBoxes;
        private CheckBox lastSelectedPaletteCheckBox = null;

        // ИЗМЕНЕНИЕ: Типы полей для отрендеренного состояния
        private decimal renderedCenterX;
        private decimal renderedCenterY;
        private decimal renderedZoom;

        // Начальные константы для Горящего Корабля (Мандельброт-версия) для инициализации
        private const double INITIAL_MIN_RE_BS_D = -2.0;
        private const double INITIAL_MAX_RE_BS_D = 1.5;  // Область [-2.0, 1.5] -> ширина 3.5
        private const double INITIAL_MIN_IM_BS_D = -1.5;
        private const double INITIAL_MAX_IM_BS_D = 1.0; // Область [-1.5, 1.0] -> высота 2.5


        public FractalMondelbrotShip()
        {
            InitializeComponent();
            this.Text = "Фрактал Горящий Корабль (Мандельброт) - Decimal Precision";

            // Инициализация центра и зума для Горящего Корабля
            this.centerX = (decimal)(INITIAL_MIN_RE_BS_D + INITIAL_MAX_RE_BS_D) / 2.0m; // -0.25m
            this.centerY = (decimal)(INITIAL_MIN_IM_BS_D + INITIAL_MAX_IM_BS_D) / 2.0m; // -0.25m

            // Начальный zoom = 1, BASE_SCALE определяет видимую ширину.
            // Если BASE_SCALE = 3.5, то при zoom = 1 мы видим область шириной 3.5.
            this.zoom = 1.0m;

            renderedCenterX = centerX;
            renderedCenterY = centerY;
            renderedZoom = zoom;
        }

        private async void Form1_Load(object sender, EventArgs e) // Имя метода Form1_Load оставлено
        {
            width = canvas2.Width; // canvas2 - имя из дизайнера FractalMondelbrot
            height = canvas2.Height;

            renderTimer = new System.Windows.Forms.Timer { Interval = 300 };
            renderTimer.Tick += RenderTimer_Tick;

            CheckBox mondelbrotClassicCb = Controls.Find("mondelbrotClassicBox", true).FirstOrDefault() as CheckBox;

            paletteCheckBoxes = new CheckBox[] {
                colorBox, oldRenderBW,
                Controls.Find("checkBox1", true).FirstOrDefault() as CheckBox,
                Controls.Find("checkBox2", true).FirstOrDefault() as CheckBox,
                Controls.Find("checkBox3", true).FirstOrDefault() as CheckBox,
                Controls.Find("checkBox4", true).FirstOrDefault() as CheckBox,
                Controls.Find("checkBox5", true).FirstOrDefault() as CheckBox,
                Controls.Find("checkBox6", true).FirstOrDefault() as CheckBox,
                mondelbrotClassicCb
            };

            foreach (var cb in paletteCheckBoxes.Where(cb => cb != null))
            {
                cb.CheckedChanged += PaletteCheckBox_CheckedChanged;
            }

            nudIterations.ValueChanged += ParamControl_Changed;
            nudThreshold.ValueChanged += ParamControl_Changed;
            cbThreads.SelectedIndexChanged += ParamControl_Changed;
            nudZoom.ValueChanged += NudZoom_ValueChanged; // Отдельный обработчик

            canvas2.MouseWheel += Canvas_MouseWheel;
            canvas2.MouseDown += Canvas_MouseDown;
            canvas2.MouseMove += Canvas_MouseMove;
            canvas2.MouseUp += Canvas_MouseUp;
            canvas2.Paint += Canvas_Paint;

            int cores = Environment.ProcessorCount;
            for (int i = 1; i <= cores; i++) cbThreads.Items.Add(i);
            cbThreads.Items.Add("Auto");
            cbThreads.SelectedItem = "Auto";

            nudIterations.Minimum = 50; nudIterations.Maximum = 200000;
            nudIterations.Value = 250; // Для Корабля может понадобиться больше итераций

            nudThreshold.Minimum = 2m; nudThreshold.Maximum = 10000m;
            nudThreshold.DecimalPlaces = 2; nudThreshold.Increment = 0.1m;
            nudThreshold.Value = 2.0m; // thresholdSquared будет 4.0m

            nudZoom.DecimalPlaces = 2;
            nudZoom.Increment = 0.1m;
            nudZoom.Minimum = 1m;
            nudZoom.Maximum = 1E+28m;
            nudZoom.Value = this.zoom;

            this.Resize += Form1_Resize;
            canvas2.Resize += Canvas_Resize;

            HandleColorBoxEnableState();
            ScheduleRender();
        }

        private void NudZoom_ValueChanged(object sender, EventArgs e)
        {
            if (isHighResRendering) return;
            decimal newZoomValue = nudZoom.Value;
            if (newZoomValue < nudZoom.Minimum) newZoomValue = nudZoom.Minimum;
            if (newZoomValue > nudZoom.Maximum) newZoomValue = nudZoom.Maximum;

            if (this.zoom != newZoomValue)
            {
                this.zoom = newZoomValue;
                if (nudZoom.Value != newZoomValue)
                {
                    nudZoom.Value = newZoomValue;
                }
                ScheduleRender();
            }
        }

        // Canvas_Paint аналогичен версии для Julia
        private void Canvas_Paint(object sender, PaintEventArgs e)
        {
            if (canvas2.Image == null || width <= 0 || height <= 0 || this.zoom <= 0 || this.renderedZoom <= 0)
            {
                e.Graphics.Clear(Color.Black);
                if (canvas2.Image != null && (this.zoom <= 0 || this.renderedZoom <= 0))
                    e.Graphics.DrawImageUnscaled(canvas2.Image, Point.Empty);
                return;
            }

            decimal viewWidthCurrent = BASE_SCALE / this.zoom;
            decimal viewHeightCurrent = viewWidthCurrent * ((decimal)height / width);
            decimal viewWidthRendered = BASE_SCALE / this.renderedZoom;
            decimal viewHeightRendered = viewWidthRendered * ((decimal)height / width);

            decimal currentViewReMin = this.centerX - viewWidthCurrent / 2m;
            decimal currentViewImMax = this.centerY + viewHeightCurrent / 2m;
            decimal renderedImageReMin = this.renderedCenterX - viewWidthRendered / 2m;
            decimal renderedImageImMax = this.renderedCenterY + viewHeightRendered / 2m;

            decimal deltaRe = renderedImageReMin - currentViewReMin;
            decimal deltaIm = renderedImageImMax - currentViewImMax;

            float drawX = (float)(deltaRe / viewWidthCurrent * width);
            float drawY = (float)(-deltaIm / viewHeightCurrent * height);
            float scaleFactor = (float)(viewWidthRendered / viewWidthCurrent);
            float drawWidth = width * scaleFactor;
            float drawHeight = height * scaleFactor;

            e.Graphics.Clear(Color.Black);
            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;

            if (drawWidth > 0 && drawHeight > 0 && canvas2.Image != null)
            {
                try { e.Graphics.DrawImage(canvas2.Image, drawX, drawY, drawWidth, drawHeight); }
                catch (OverflowException) { e.Graphics.DrawImageUnscaled(canvas2.Image, Point.Empty); }
                catch (ArgumentException) { e.Graphics.DrawImageUnscaled(canvas2.Image, Point.Empty); }
            }
            else if (canvas2.Image != null) { e.Graphics.DrawImageUnscaled(canvas2.Image, Point.Empty); }
        }

        private void UpdateParameters()
        {
            maxIterations = (int)nudIterations.Value;
            decimal thresholdVal = nudThreshold.Value;
            thresholdSquared = thresholdVal * thresholdVal;
            threadCount = cbThreads.SelectedItem.ToString() == "Auto"
                ? Environment.ProcessorCount
                : Convert.ToInt32(cbThreads.SelectedItem);
        }

        // Рендеринг Мандельброт-версии Горящего Корабля
        private void RenderFractal(CancellationToken token, decimal renderCenterX, decimal renderCenterY, decimal renderZoom)
        {
            if (token.IsCancellationRequested) return;
            if (isHighResRendering || width <= 0 || height <= 0) return;

            Bitmap bmp = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            BitmapData bmpData = null;
            try
            {
                Rectangle rect = new Rectangle(0, 0, width, height);
                bmpData = bmp.LockBits(rect, ImageLockMode.WriteOnly, bmp.PixelFormat);
                token.ThrowIfCancellationRequested();

                int stride = bmpData.Stride;
                IntPtr scan0 = bmpData.Scan0;
                byte[] buffer = new byte[Math.Abs(stride) * height];
                ParallelOptions po = new ParallelOptions { MaxDegreeOfParallelism = threadCount, CancellationToken = token };
                int done = 0;
                const int currentMaxColorIter = 1000;

                decimal currentViewWidth = BASE_SCALE / renderZoom;
                decimal currentViewHeight = currentViewWidth * ((decimal)height / width);
                decimal reOffset = renderCenterX - currentViewWidth / 2m;
                decimal imOffset = renderCenterY - currentViewHeight / 2m;
                decimal pixelWidthComplex = currentViewWidth / width;
                decimal pixelHeightComplex = currentViewHeight / height;

                Parallel.For(0, height, po, y_coord =>
                {
                    if (token.IsCancellationRequested) return;
                    int rowOffset = y_coord * stride;
                    for (int x_coord = 0; x_coord < width; x_coord++)
                    {
                        decimal c_re = reOffset + (x_coord + 0.5m) * pixelWidthComplex;
                        decimal c_im = imOffset + ((height - 1 - y_coord) + 0.5m) * pixelHeightComplex;

                        ComplexDecimal c0 = new ComplexDecimal(c_re, c_im);
                        ComplexDecimal z = ComplexDecimal.Zero;
                        int iter_val = 0;

                        while (iter_val < maxIterations && z.MagnitudeSquared <= thresholdSquared)
                        {
                            z = new ComplexDecimal(Math.Abs(z.Real), Math.Abs(z.Imaginary));
                            z = z * z + c0;
                            iter_val++;
                        }

                        Color pixelColor = GetPixelColor(iter_val, maxIterations, currentMaxColorIter);
                        int index = rowOffset + x_coord * 3;
                        buffer[index] = pixelColor.B; buffer[index + 1] = pixelColor.G; buffer[index + 2] = pixelColor.R;
                    }

                    int progress = Interlocked.Increment(ref done);
                    if (!token.IsCancellationRequested && progressBar.IsHandleCreated && !progressBar.IsDisposed && height > 0)
                    {
                        try { progressBar.BeginInvoke((Action)(() => { if (progressBar.IsHandleCreated && !progressBar.IsDisposed) progressBar.Value = Math.Min(progressBar.Maximum, (int)(100.0 * progress / height)); })); }
                        catch (InvalidOperationException) { }
                    }
                });

                token.ThrowIfCancellationRequested();
                Marshal.Copy(buffer, 0, scan0, buffer.Length);
                bmp.UnlockBits(bmpData);
                bmpData = null;
                token.ThrowIfCancellationRequested();

                if (canvas2.IsHandleCreated && !canvas2.IsDisposed)
                {
                    Bitmap oldImage = null;
                    canvas2.Invoke((Action)(() =>
                    {
                        if (token.IsCancellationRequested) { bmp?.Dispose(); return; }
                        oldImage = canvas2.Image as Bitmap;
                        canvas2.Image = bmp;
                        this.renderedCenterX = renderCenterX;
                        this.renderedCenterY = renderCenterY;
                        this.renderedZoom = renderZoom;
                        bmp = null;
                    }));
                    oldImage?.Dispose();
                }
                else { bmp?.Dispose(); }
            }
            finally
            {
                if (bmpData != null && bmp != null) { try { bmp.UnlockBits(bmpData); } catch { } }
                if (bmp != null) bmp.Dispose();
            }
        }

        // Рендеринг для сохранения в файл
        private Bitmap RenderFractalToBitmap(
            int customRenderWidth, int customRenderHeight,
            decimal currentRenderCenterX, decimal currentRenderCenterY, decimal currentRenderZoom,
            decimal currentRenderBaseScale,
            int currentRenderMaxIterations, decimal currentRenderThresholdSquared,
            int numRenderThreads,
            Action<int> reportProgressCallback)
        {
            if (customRenderWidth <= 0 || customRenderHeight <= 0) return new Bitmap(1, 1);
            Bitmap bmp = new Bitmap(customRenderWidth, customRenderHeight, PixelFormat.Format24bppRgb);
            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, customRenderWidth, customRenderHeight), ImageLockMode.WriteOnly, bmp.PixelFormat);

            int stride = bmpData.Stride;
            IntPtr scan0 = bmpData.Scan0;
            byte[] buffer = new byte[Math.Abs(stride) * customRenderHeight];
            ParallelOptions po = new ParallelOptions { MaxDegreeOfParallelism = numRenderThreads };
            long done = 0;
            const int colorIterLimit = 1000;

            decimal viewWidth = currentRenderBaseScale / currentRenderZoom;
            decimal viewHeight = viewWidth * ((decimal)customRenderHeight / customRenderWidth);
            decimal reOffset = currentRenderCenterX - viewWidth / 2m;
            decimal imOffset = currentRenderCenterY - viewHeight / 2m;
            decimal pixelWidthComplex = viewWidth / customRenderWidth;
            decimal pixelHeightComplex = viewHeight / customRenderHeight;

            Parallel.For(0, customRenderHeight, po, y_coord =>
            {
                int rowOffset = y_coord * stride;
                for (int x_coord = 0; x_coord < customRenderWidth; x_coord++)
                {
                    decimal c_re = reOffset + (x_coord + 0.5m) * pixelWidthComplex;
                    decimal c_im = imOffset + ((customRenderHeight - 1 - y_coord) + 0.5m) * pixelHeightComplex;

                    ComplexDecimal c0 = new ComplexDecimal(c_re, c_im);
                    ComplexDecimal z = ComplexDecimal.Zero;
                    int iter_val = 0;

                    while (iter_val < currentRenderMaxIterations && z.MagnitudeSquared <= currentRenderThresholdSquared)
                    {
                        z = new ComplexDecimal(Math.Abs(z.Real), Math.Abs(z.Imaginary));
                        z = z * z + c0;
                        iter_val++;
                    }

                    Color pixelColor = GetPixelColor(iter_val, currentRenderMaxIterations, colorIterLimit);
                    int index = rowOffset + x_coord * 3;
                    buffer[index] = pixelColor.B; buffer[index + 1] = pixelColor.G; buffer[index + 2] = pixelColor.R;
                }
                long currentDone = Interlocked.Increment(ref done);
                if (customRenderHeight > 0) reportProgressCallback((int)(100.0 * currentDone / customRenderHeight));
            });

            Marshal.Copy(buffer, 0, scan0, buffer.Length);
            bmp.UnlockBits(bmpData);
            return bmp;
        }

        // Обработчики мыши (аналогичны версии для Julia)
        private void Canvas_MouseWheel(object sender, MouseEventArgs e)
        {
            if (isHighResRendering) return;
            decimal zoomFactor = e.Delta > 0 ? 1.25m : 0.8m;
            decimal oldZoom = this.zoom;
            decimal mouseCanvasX = e.X;
            decimal mouseCanvasY = e.Y;

            decimal viewWidthBefore = BASE_SCALE / oldZoom;
            decimal viewHeightBefore = viewWidthBefore * ((decimal)height / width);
            decimal reOffsetBefore = this.centerX - viewWidthBefore / 2m;
            decimal imOffsetBefore = this.centerY - viewHeightBefore / 2m;
            decimal pixelWidthComplexBefore = viewWidthBefore / width;
            decimal pixelHeightComplexBefore = viewHeightBefore / height;

            decimal mouseRe = reOffsetBefore + (mouseCanvasX + 0.5m) * pixelWidthComplexBefore;
            decimal mouseIm = imOffsetBefore + ((height - 1 - mouseCanvasY) + 0.5m) * pixelHeightComplexBefore;

            decimal newZoom = oldZoom * zoomFactor;
            if (newZoom < nudZoom.Minimum) newZoom = nudZoom.Minimum;
            if (newZoom > nudZoom.Maximum) newZoom = nudZoom.Maximum;
            this.zoom = newZoom;

            decimal viewWidthAfter = BASE_SCALE / this.zoom;
            decimal viewHeightAfter = viewWidthAfter * ((decimal)height / width);
            decimal pixelWidthComplexAfter = viewWidthAfter / width;
            decimal pixelHeightComplexAfter = viewHeightAfter / height;

            this.centerX = mouseRe - ((mouseCanvasX + 0.5m) * pixelWidthComplexAfter - viewWidthAfter / 2m);
            this.centerY = mouseIm - (((height - 1 - mouseCanvasY) + 0.5m) * pixelHeightComplexAfter - viewHeightAfter / 2m);

            canvas2.Invalidate();
            if (nudZoom.Value != this.zoom)
            {
                decimal displayZoom = this.zoom;
                if (displayZoom < nudZoom.Minimum) displayZoom = nudZoom.Minimum;
                if (displayZoom > nudZoom.Maximum) displayZoom = nudZoom.Maximum;
                nudZoom.Value = displayZoom;
            }
            else { ScheduleRender(); }
        }

        private void Canvas_MouseDown(object sender, MouseEventArgs e)
        {
            if (isHighResRendering) return;
            if (e.Button == MouseButtons.Left) { panning = true; panStart = e.Location; }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (isHighResRendering || !panning) return;
            decimal currentViewWidth = BASE_SCALE / this.zoom;
            decimal pixelSizeComplex = currentViewWidth / width;
            decimal deltaX = e.X - panStart.X;
            decimal deltaY = e.Y - panStart.Y;
            this.centerX -= deltaX * pixelSizeComplex;
            this.centerY += deltaY * pixelSizeComplex;
            panStart = e.Location;
            canvas2.Invalidate();
            ScheduleRender();
        }

        private void Canvas_MouseUp(object sender, MouseEventArgs e)
        {
            if (isHighResRendering) return;
            if (e.Button == MouseButtons.Left) { panning = false; }
        }

        // Сохранение PNG высокого разрешения
        private async void btnSave_Click_1(object sender, EventArgs e)
        {
            if (isHighResRendering) { /* ... */ return; }
            int saveWidth = (int)nudW.Value; int saveHeight = (int)nudH.Value;
            if (saveWidth <= 0 || saveHeight <= 0) { /* ... */ return; }

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string suggestedFileName = $"MandelBS_{timestamp}_z{(this.zoom > 1000m ? this.zoom.ToString("E2", CultureInfo.InvariantCulture) : this.zoom.ToString("F2", CultureInfo.InvariantCulture))}.png";

            using (SaveFileDialog saveDialog = new SaveFileDialog { Filter = "PNG Image|*.png", Title = "Сохранить фрактал (Высокое разрешение)", FileName = suggestedFileName })
            {
                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    Button currentSaveBtn = sender as Button;
                    isHighResRendering = true;
                    if (currentSaveBtn != null) currentSaveBtn.Enabled = false;
                    SetMainControlsEnabled(false);
                    if (progressPNG != null) { progressPNG.Value = 0; progressPNG.Visible = true; }

                    try
                    {
                        UpdateParameters();
                        int maxIterSave = this.maxIterations;
                        decimal thresholdSqSave = this.thresholdSquared;
                        decimal zoomSave = this.zoom;
                        decimal centerXSave = this.centerX;
                        decimal centerYSave = this.centerY;
                        int threadsSave = this.threadCount;
                        decimal baseScaleSave = BASE_SCALE;

                        Bitmap highResBitmap = await Task.Run(() => RenderFractalToBitmap(
                            saveWidth, saveHeight,
                            centerXSave, centerYSave, zoomSave,
                            baseScaleSave,
                            maxIterSave, thresholdSqSave,
                            threadsSave,
                            progressPercentage => { /* ... progressPNG update ... */ }
                        ));

                        highResBitmap.Save(saveDialog.FileName, ImageFormat.Png);
                        highResBitmap.Dispose();
                        MessageBox.Show("Изображение успешно сохранено!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex) { MessageBox.Show($"Ошибка при сохранении: {ex.Message}\n{ex.StackTrace}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                    finally
                    { /* ... UI reset ... */
                        isHighResRendering = false;
                        if (currentSaveBtn != null) currentSaveBtn.Enabled = true;
                        SetMainControlsEnabled(true);
                        if (progressPNG != null && progressPNG.IsHandleCreated && !progressPNG.IsDisposed)
                        {
                            try { progressPNG.Invoke((Action)(() => { if (progressPNG.IsHandleCreated && !progressPNG.IsDisposed) { progressPNG.Visible = false; progressPNG.Value = 0; } })); }
                            catch (ObjectDisposedException) { }
                            catch (InvalidOperationException) { }
                        }
                    }
                }
            }
        }

        #region Остальные методы (UI, Палитры, Жизненный цикл) - как в Julia, но для контролов этой формы

        private void ParamControl_Changed(object sender, EventArgs e)
        {
            if (isHighResRendering) return;
            ScheduleRender();
        }

        private void ScheduleRender()
        {
            if (isHighResRendering) return;
            previewRenderCts?.Cancel();
            renderTimer.Stop(); renderTimer.Start();
        }

        private async void RenderTimer_Tick(object sender, EventArgs e)
        {
            renderTimer.Stop();
            if (isHighResRendering) return;
            if (isRenderingPreview) { renderTimer.Start(); return; }

            isRenderingPreview = true;
            previewRenderCts?.Dispose();
            previewRenderCts = new CancellationTokenSource();
            CancellationToken token = previewRenderCts.Token;

            UpdateParameters();
            decimal currentRenderCenterX = this.centerX;
            decimal currentRenderCenterY = this.centerY;
            decimal currentRenderZoom = this.zoom;

            try
            {
                await Task.Run(() => RenderFractal(token, currentRenderCenterX, currentRenderCenterY, currentRenderZoom), token);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"RenderTimer_Tick Error: {ex}"); }
            finally { isRenderingPreview = false; }
        }

        // --- Палитры (идентичны версии для Julia) ---
        private delegate Color PaletteFunctionDelegate(double t, int iter, int maxIterations, int maxColorIter);
        private Color GetPixelColor(int iter, int currentMaxIterations, int currentMaxColorIter)
        { /* ... Код палитр как в Julia ... */
            if (iter == currentMaxIterations)
            {
                return (lastSelectedPaletteCheckBox?.Name == "checkBox6" || lastSelectedPaletteCheckBox?.Name == "mondelbrotClassicBox")
                       ? Color.FromArgb(50, 50, 50)
                       : Color.Black;
            }
            double t_capped = (double)Math.Min(iter, currentMaxColorIter) / currentMaxColorIter;
            double t_log = Math.Log(Math.Min(iter, currentMaxColorIter) + 1) / Math.Log(currentMaxColorIter + 1);

            PaletteFunctionDelegate selectedPaletteFunc = GetDefaultPaletteColor;
            if (lastSelectedPaletteCheckBox != null)
            {
                if (lastSelectedPaletteCheckBox == colorBox) selectedPaletteFunc = GetPaletteColorBoxColor;
                else if (lastSelectedPaletteCheckBox == oldRenderBW) selectedPaletteFunc = GetPaletteOldBWColor;
                else if (lastSelectedPaletteCheckBox.Name == "checkBox1") selectedPaletteFunc = GetPalette1Color;
                else if (lastSelectedPaletteCheckBox.Name == "checkBox2") selectedPaletteFunc = GetPalette2Color;
                else if (lastSelectedPaletteCheckBox.Name == "checkBox3") selectedPaletteFunc = GetPalette3Color;
                else if (lastSelectedPaletteCheckBox.Name == "checkBox4") selectedPaletteFunc = GetPalette4Color;
                else if (lastSelectedPaletteCheckBox.Name == "checkBox5") selectedPaletteFunc = GetPalette5Color;
                else if (lastSelectedPaletteCheckBox.Name == "checkBox6") selectedPaletteFunc = GetPalette6Color;
                else if (lastSelectedPaletteCheckBox.Name == "mondelbrotClassicBox") selectedPaletteFunc = GetPaletteMandelbrotClassicColor;
            }
            double t_param = (selectedPaletteFunc == GetDefaultPaletteColor || selectedPaletteFunc == GetPaletteMandelbrotClassicColor) ? t_log : t_capped;
            // Для GetPaletteMandelbrotClassicColor лучше использовать оригинальный iter, а не t_param
            if (selectedPaletteFunc == GetPaletteMandelbrotClassicColor)
                return GetPaletteMandelbrotClassicColor((double)iter / currentMaxIterations, iter, currentMaxIterations, currentMaxColorIter); // Передаем нормализованный iter

            return selectedPaletteFunc(t_param, iter, currentMaxIterations, currentMaxColorIter);
        }
        private Color GetDefaultPaletteColor(double t_log, int iter, int maxIter, int maxClrIter) { int cVal = (int)(255.0 * (1 - t_log)); return Color.FromArgb(cVal, cVal, cVal); }
        private Color GetPaletteColorBoxColor(double t_capped, int iter, int maxIter, int maxClrIter) { return ColorFromHSV(360.0 * t_capped, 0.8, 0.9); }
        private Color GetPaletteOldBWColor(double t_capped, int iter, int maxIter, int maxClrIter) { int cVal = 255 - (int)(255.0 * t_capped); return Color.FromArgb(cVal, cVal, cVal); }
        private Color LerpColor(Color a, Color b, double t) { t = Math.Max(0, Math.Min(1, t)); return Color.FromArgb((int)(a.R + (b.R - a.R) * t), (int)(a.G + (b.G - a.G) * t), (int)(a.B + (b.B - a.B) * t)); }
        private Color GetPalette1Color(double t, int iter, int maxIter, int maxClrIter) { /* ... */ Color c1 = Color.Black, c2 = Color.FromArgb(200, 0, 0), c3 = Color.FromArgb(255, 100, 0), c4 = Color.FromArgb(255, 255, 100), c5 = Color.White; if (t < 0.25) return LerpColor(c1, c2, t / 0.25); if (t < 0.50) return LerpColor(c2, c3, (t - 0.25) / 0.25); if (t < 0.75) return LerpColor(c3, c4, (t - 0.50) / 0.25); return LerpColor(c4, c5, (t - 0.75) / 0.25); }
        private Color GetPalette2Color(double t, int iter, int maxIter, int maxClrIter) { /* ... */ Color c1 = Color.Black, c2 = Color.FromArgb(0, 0, 100), c3 = Color.FromArgb(0, 120, 200), c4 = Color.FromArgb(170, 220, 255), c5 = Color.White; if (t < 0.25) return LerpColor(c1, c2, t / 0.25); if (t < 0.50) return LerpColor(c2, c3, (t - 0.25) / 0.25); if (t < 0.75) return LerpColor(c3, c4, (t - 0.50) / 0.25); return LerpColor(c4, c5, (t - 0.75) / 0.25); }
        private Color GetPalette3Color(double t, int iter, int maxIter, int maxClrIter) { /* ... */ double r = Math.Sin(t * Math.PI * 3.0 + 0.5) * 0.45 + 0.5, g = Math.Sin(t * Math.PI * 3.0 + Math.PI * 2.0 / 3.0 + 0.5) * 0.45 + 0.5, b = Math.Sin(t * Math.PI * 3.0 + Math.PI * 4.0 / 3.0 + 0.5) * 0.45 + 0.5; return Color.FromArgb((int)(r * 255), (int)(g * 255), (int)(b * 255)); }
        private Color GetPalette4Color(double t, int iter, int maxIter, int maxClrIter) { /* ... */ Color c1 = Color.FromArgb(10, 0, 20), c2 = Color.FromArgb(255, 0, 255), c3 = Color.FromArgb(0, 255, 255), c4 = Color.FromArgb(230, 230, 250); if (t < 0.1) return LerpColor(c1, c2, t / 0.1); if (t < 0.4) return LerpColor(c2, c1, (t - 0.1) / 0.3); if (t < 0.5) return LerpColor(c1, c3, (t - 0.4) / 0.1); if (t < 0.8) return LerpColor(c3, c1, (t - 0.5) / 0.3); return LerpColor(c1, c4, (t - 0.8) / 0.2); }
        private Color GetPalette5Color(double t, int iter, int maxIter, int maxClrIter) { /* ... */ int gVal = 50 + (int)(t * 150); double shine = Math.Sin(t * Math.PI * 5); int finalG = Math.Max(0, Math.Min(255, gVal + (int)(shine * 40))); return Color.FromArgb(finalG, finalG, Math.Min(255, finalG + (int)(t * 25))); }
        private Color GetPalette6Color(double t, int iter, int maxIter, int maxClrIter) { /* ... */ double hue = (t * 200.0 + 180.0) % 360.0, sat = Math.Max(0.2, Math.Min(0.6, 0.35 + (Math.Sin(t * Math.PI * 2) * 0.1))), val = Math.Max(0.7, Math.Min(0.95, 0.80 + (Math.Cos(t * Math.PI * 2.5) * 0.15))); return ColorFromHSV(hue, sat, val); }
        private Color GetPaletteMandelbrotClassicColor(double t_norm_iter, int iter, int maxIter, int maxClrIter)
        {
            // t_norm_iter это iter / maxIter
            byte r_val, g_val, b_val;
            if (t_norm_iter < 0.5)
            {
                double t_half = t_norm_iter * 2.0;
                r_val = (byte)(t_half * 200); g_val = (byte)(t_half * 50); b_val = (byte)(t_half * 30);
            }
            else
            {
                double t_half = (t_norm_iter - 0.5) * 2.0;
                r_val = (byte)(200 + t_half * 55); g_val = (byte)(50 + t_half * 205); b_val = (byte)(30 + t_half * 225);
            }
            return Color.FromArgb(r_val, g_val, b_val);
        }
        private Color ColorFromHSV(double hue, double saturation, double value) { /* ... */ hue = (hue % 360 + 360) % 360; int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6; double f = hue / 60 - Math.Floor(hue / 60); value = Math.Max(0, Math.Min(1, value)); saturation = Math.Max(0, Math.Min(1, saturation)); int v_comp = Convert.ToInt32(value * 255); int p_comp = Convert.ToInt32(v_comp * (1 - saturation)); int q_comp = Convert.ToInt32(v_comp * (1 - f * saturation)); int t_comp = Convert.ToInt32(v_comp * (1 - (1 - f) * saturation)); switch (hi) { case 0: return Color.FromArgb(v_comp, t_comp, p_comp); case 1: return Color.FromArgb(q_comp, v_comp, p_comp); case 2: return Color.FromArgb(p_comp, v_comp, t_comp); case 3: return Color.FromArgb(p_comp, q_comp, v_comp); case 4: return Color.FromArgb(t_comp, p_comp, v_comp); default: return Color.FromArgb(v_comp, p_comp, q_comp); } }
        // --- Конец палитр ---

        private void PaletteCheckBox_CheckedChanged(object sender, EventArgs e)
        { /* ... как в Julia ... */
            CheckBox currentCb = sender as CheckBox; if (currentCb == null) return;
            foreach (var cb in paletteCheckBoxes.Where(cb => cb != null)) cb.CheckedChanged -= PaletteCheckBox_CheckedChanged;
            if (currentCb.Checked)
            {
                lastSelectedPaletteCheckBox = currentCb;
                foreach (var cb in paletteCheckBoxes.Where(cb => cb != null && cb != currentCb)) cb.Checked = false;
            }
            else
            {
                lastSelectedPaletteCheckBox = paletteCheckBoxes.FirstOrDefault(cb => cb != null && cb.Checked);
            }
            if (lastSelectedPaletteCheckBox == null && mondelbrotClassicBox != null && paletteCheckBoxes.Contains(mondelbrotClassicBox))
            {
                mondelbrotClassicBox.Checked = true; // Вызовет событие снова, но lastSelectedPaletteCheckBox уже будет mondelbrotClassicBox
            }
            foreach (var cb in paletteCheckBoxes.Where(cb => cb != null)) cb.CheckedChanged += PaletteCheckBox_CheckedChanged;
            HandleColorBoxEnableState();
            ScheduleRender();
        }
        private void HandleColorBoxEnableState()
        { /* ... как в Julia ... */
            if (paletteCheckBoxes == null || colorBox == null || oldRenderBW == null) return;
            bool isAnyNewPaletteCbChecked = paletteCheckBoxes.Skip(2).Any(cb => cb != null && cb.Checked && cb != mondelbrotClassicBox);
            if (isAnyNewPaletteCbChecked) colorBox.Enabled = true;
            else if (colorBox.Checked && !oldRenderBW.Checked) colorBox.Enabled = true;
            else colorBox.Enabled = !oldRenderBW.Checked;
        }
        private void Form1_Resize(object sender, EventArgs e) => ResizeCanvas();
        private void Canvas_Resize(object sender, EventArgs e) => ResizeCanvas();
        private void ResizeCanvas()
        { /* ... как в Julia, но для canvas2 ... */
            if (isHighResRendering) return;
            if (canvas2.Width <= 0 || canvas2.Height <= 0) return;
            width = canvas2.Width; height = canvas2.Height;
            ScheduleRender();
        }
        private void BtnSave_Click(object sender, EventArgs e)
        { /* ... как в Julia, но для canvas2 ... */
            if (isHighResRendering) { /* ... */ return; }
            using (var dlg = new SaveFileDialog { Filter = "PNG Image|*.png", FileName = $"MandelBS_preview_{DateTime.Now:yyyyMMdd_HHmmss}.png" })
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    if (canvas2.Image != null) canvas2.Image.Save(dlg.FileName, ImageFormat.Png);
                    else MessageBox.Show("Нет изображения!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        private void btnRender_Click(object sender, EventArgs e)
        { /* ... как в Julia ... */
            if (isHighResRendering) return; ScheduleRender();
        }
        private void SetMainControlsEnabled(bool enabled)
        { /* ... как в Julia, но для контролов этой формы ... */
            Action action = () => {
                if (btnRender != null) btnRender.Enabled = enabled;
                nudIterations.Enabled = enabled; nudThreshold.Enabled = enabled;
                cbThreads.Enabled = enabled; nudZoom.Enabled = enabled;
                if (nudW != null) nudW.Enabled = enabled;
                if (nudH != null) nudH.Enabled = enabled;
                foreach (var cb in paletteCheckBoxes.Where(cb => cb != null)) cb.Enabled = enabled;
                if (enabled) HandleColorBoxEnableState();
                else if (colorBox != null) colorBox.Enabled = false;
                Button savePreviewButton = Controls.Find("btnSave", true).FirstOrDefault() as Button;
                if (savePreviewButton != null) savePreviewButton.Enabled = enabled;
                using Button savePNGButton = (Button)(Controls.Find("btnSavePNG", true).FirstOrDefault() ?? Controls.Find("btnSave_Click_1", true).FirstOrDefault() ?? Controls.Find("button1", true).FirstOrDefault()); // Ищите правильное имя кнопки для сохранения PNG
                if (savePNGButton != null) savePNGButton.Enabled = enabled || !isHighResRendering; // Кнопку сохранения PNG можно блокировать отдельно

            };
            if (this.InvokeRequired) this.Invoke(action); else action();
        }
        protected override void OnFormClosed(FormClosedEventArgs e)
        { /* ... как в Julia ... */
            renderTimer?.Stop(); renderTimer?.Dispose();
            previewRenderCts?.Cancel(); previewRenderCts?.Dispose();
            base.OnFormClosed(e);
        }
        #endregion
    }
}
// --- END OF FILE FractalMondelbrotShip.cs ---