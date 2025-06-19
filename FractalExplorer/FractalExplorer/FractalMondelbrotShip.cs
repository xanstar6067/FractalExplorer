// --- START OF FILE FractalMondelbrotShip.cs ---

using System;
using System.Drawing;
using System.Drawing.Imaging;
// using System.Numerics; // ИЗМЕНЕНИЕ: Заменено на наш ComplexDecimal
using System.Runtime.InteropServices; // Для использования Marshal.Copy
using System.Threading;
using System.Windows.Forms;
using System.Linq; // Для FirstOrDefault и других LINQ операций

namespace FractalDraving
{
    /// <summary>
    /// Главная форма приложения для отображения и взаимодействия с фракталом "Горящий Корабль" (Мандельброт-версия).
    /// </summary>
    public partial class FractalMondelbrotShip : Form
    {
        // Таймер для отложенного рендеринга предварительного просмотра фрактала
        private System.Windows.Forms.Timer renderTimer;

        // Максимальное количество итераций для определения принадлежности точки множеству
        private int maxIterations;
        // Порог для определения "ухода в бесконечность" при итерациях
        private double threshold;
        // ИЗМЕНЕНИЕ: Квадрат порога для оптимизации вычислений
        private decimal thresholdSquared;
        // Количество потоков для параллельного рендеринга
        private int threadCount;
        // Ширина и высота области отрисовки (canvas)
        private int width, height;

        // Начальная точка для панорамирования изображения
        private Point panStart;
        // Флаг, указывающий, происходит ли панорамирование в данный момент
        private bool panning = false;

        // ИЗМЕНЕНИЕ: Текущий уровень масштабирования фрактала (используем decimal)
        private decimal zoom = 1.0m;
        // ИЗМЕНЕНИЕ: Координаты центра отображаемой области фрактала (используем decimal)
        private decimal centerX = 0.0m;
        private decimal centerY = 0.0m;
        // ИЗМЕНЕНИЕ: Базовый масштаб для вычисления отображаемой области фрактала (используем decimal)
        private const decimal BASE_SCALE = 3.0m;

        // Флаг, указывающий, выполняется ли рендеринг в высоком разрешении (для сохранения)
        private bool isHighResRendering = false;

        // Флаг, указывающий, выполняется ли рендеринг предварительного просмотра (volatile для потокобезопасности)
        private volatile bool isRenderingPreview = false;
        // Источник токенов отмены для асинхронного рендеринга предварительного просмотра
        private CancellationTokenSource previewRenderCts;

        // Массив чекбоксов для выбора цветовой палитры
        private CheckBox[] paletteCheckBoxes;
        // Последний выбранный чекбокс палитры
        private CheckBox lastSelectedPaletteCheckBox = null;

        // ИЗМЕНЕНИЕ: Поля для хранения параметров отрисованного битмапа (используем decimal)
        private decimal renderedCenterX;
        private decimal renderedCenterY;
        private decimal renderedZoom;

        // ИЗМЕНЕНИЕ: Начальные константы для Горящего Корабля (используем decimal)
        private const decimal INITIAL_MIN_RE_BS = -2.0m;
        private const decimal INITIAL_MAX_RE_BS = 1.5m;
        private const decimal INITIAL_MIN_IM_BS = -1.5m;
        private const decimal INITIAL_MAX_IM_BS = 1.0m;


        public FractalMondelbrotShip()
        {
            InitializeComponent();
            // ИЗМЕНЕНИЕ: Используем decimal для вычислений
            this.centerX = (INITIAL_MIN_RE_BS + INITIAL_MAX_RE_BS) / 2.0m;
            this.centerY = (INITIAL_MIN_IM_BS + INITIAL_MAX_IM_BS) / 2.0m;

            decimal initialRangeX = INITIAL_MAX_RE_BS - INITIAL_MIN_RE_BS;
            decimal initialRangeY = INITIAL_MAX_IM_BS - INITIAL_MIN_IM_BS;
            decimal initialViewRange = Math.Max(initialRangeX, initialRangeY);

            this.zoom = BASE_SCALE / initialViewRange;
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            width = canvas2.Width;
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
            nudZoom.ValueChanged += ParamControl_Changed;

            canvas2.MouseUp += Canvas_MouseUp;
            canvas2.MouseWheel += Canvas_MouseWheel;
            canvas2.MouseDown += Canvas_MouseDown;
            canvas2.MouseMove += Canvas_MouseMove;
            canvas2.Paint += Canvas_Paint;

            int cores = Environment.ProcessorCount;
            for (int i = 1; i <= cores; i++)
            {
                cbThreads.Items.Add(i);
            }
            cbThreads.Items.Add("Auto");
            cbThreads.SelectedItem = "Auto";

            nudIterations.Minimum = 50;
            nudIterations.Maximum = 100000;
            nudIterations.Value = 200;

            nudThreshold.Minimum = 2m;
            nudThreshold.Maximum = 1000m;
            nudThreshold.DecimalPlaces = 1;
            nudThreshold.Increment = 0.1m;
            nudThreshold.Value = 2m;

            nudZoom.DecimalPlaces = 3;
            nudZoom.Increment = 0.1m;
            nudZoom.Minimum = 0.001m;
            nudZoom.Maximum = 1000000000000m;

            nudZoom.Value = this.zoom;


            this.Resize += Form1_Resize;
            canvas2.Resize += Canvas_Resize;

            renderedCenterX = centerX;
            renderedCenterY = centerY;
            renderedZoom = zoom;

            HandleColorBoxEnableState();
            ScheduleRender();
        }


        #region UI_Handlers_And_Palettes
        private void Canvas_Paint(object sender, PaintEventArgs e)
        {
            if (canvas2.Image == null || width <= 0 || height <= 0)
            {
                e.Graphics.Clear(Color.Black);
                return;
            }

            // ИЗМЕНЕНИЕ: Используем decimal для всех вычислений масштаба
            decimal scaleRendered = BASE_SCALE / renderedZoom;
            decimal scaleCurrent = BASE_SCALE / zoom;

            if (renderedZoom <= 0 || zoom <= 0 || scaleRendered <= 0 || scaleCurrent <= 0)
            {
                e.Graphics.Clear(Color.Black);
                if (canvas2.Image != null)
                    e.Graphics.DrawImageUnscaled(canvas2.Image, Point.Empty);
                return;
            }

            decimal complex_half_width_rendered = scaleRendered / 2.0m;
            decimal complex_half_height_rendered = scaleRendered / 2.0m; // Предполагаем квадратные пиксели

            decimal complex_half_width_current = scaleCurrent / 2.0m;
            decimal complex_half_height_current = scaleCurrent / 2.0m;

            decimal renderedImage_re_min = renderedCenterX - complex_half_width_rendered;
            decimal renderedImage_im_min = renderedCenterY - complex_half_height_rendered;

            decimal currentView_re_min = centerX - complex_half_width_current;
            decimal currentView_im_min = centerY - complex_half_height_current;

            float p1_X = (float)((renderedImage_re_min - currentView_re_min) / (complex_half_width_current * 2.0m) * width);
            float p1_Y = (float)((renderedImage_im_min - currentView_im_min) / (complex_half_height_current * 2.0m) * height);

            float w_prime = (float)(width * (scaleRendered / scaleCurrent));
            float h_prime = (float)(height * (scaleRendered / scaleCurrent));

            e.Graphics.Clear(Color.Black);
            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Bilinear;
            e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

            if (w_prime > 0 && h_prime > 0 && canvas2.Image != null)
            {
                try
                {
                    e.Graphics.DrawImage(canvas2.Image, new PointF[] { new PointF(p1_X, p1_Y), new PointF(p1_X + w_prime, p1_Y), new PointF(p1_X, p1_Y + h_prime) });
                }
                catch (ArgumentException)
                {
                    if (canvas2.Image != null)
                        e.Graphics.DrawImageUnscaled(canvas2.Image, Point.Empty);
                }
            }
            else
            {
                if (canvas2.Image != null)
                    e.Graphics.DrawImageUnscaled(canvas2.Image, Point.Empty);
            }
        }
        private void PaletteCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox currentCb = sender as CheckBox;
            if (currentCb == null) return;

            foreach (var cb in paletteCheckBoxes.Where(cb => cb != null))
            {
                cb.CheckedChanged -= PaletteCheckBox_CheckedChanged;
            }

            if (currentCb.Checked)
            {
                lastSelectedPaletteCheckBox = currentCb;
                foreach (var cb in paletteCheckBoxes.Where(cb => cb != null && cb != currentCb))
                {
                    cb.Checked = false;
                }
            }
            else
            {
                if (paletteCheckBoxes.All(cb => cb == null || !cb.Checked))
                {
                    lastSelectedPaletteCheckBox = null;
                }
            }

            foreach (var cb in paletteCheckBoxes.Where(cb => cb != null))
            {
                cb.CheckedChanged += PaletteCheckBox_CheckedChanged;
            }

            if (lastSelectedPaletteCheckBox == null && mondelbrotClassicBox != null)
            {
                mondelbrotClassicBox.Checked = true;
            }

            HandleColorBoxEnableState();
            ScheduleRender();
        }
        private void HandleColorBoxEnableState()
        {
            if (paletteCheckBoxes == null || colorBox == null || oldRenderBW == null) return;
            bool isAnyNewPaletteCbChecked = paletteCheckBoxes.Skip(2).Any(cb => cb != null && cb.Checked && cb != mondelbrotClassicBox);
            if (isAnyNewPaletteCbChecked)
            {
                colorBox.Enabled = true;
            }
            else if (colorBox.Checked && !oldRenderBW.Checked)
            {
                colorBox.Enabled = true;
            }
            else
            {
                colorBox.Enabled = !oldRenderBW.Checked;
            }
        }
        private void Form1_Resize(object sender, EventArgs e) => ResizeCanvas();
        private void Canvas_Resize(object sender, EventArgs e) => ResizeCanvas();
        private void ResizeCanvas()
        {
            if (isHighResRendering) return;
            if (canvas2.Width <= 0 || canvas2.Height <= 0) return;
            width = canvas2.Width;
            height = canvas2.Height;
            ScheduleRender();
        }
        private void ParamControl_Changed(object sender, EventArgs e)
        {
            if (isHighResRendering) return;
            if (sender == nudZoom)
            {
                // ИЗМЕНЕНИЕ: Работаем с decimal напрямую
                zoom = nudZoom.Value;
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
            if (isHighResRendering) return;
            if (isRenderingPreview)
            {
                renderTimer.Start();
                return;
            }
            isRenderingPreview = true;
            previewRenderCts?.Dispose();
            previewRenderCts = new CancellationTokenSource();
            CancellationToken token = previewRenderCts.Token;
            UpdateParameters();
            // ИЗМЕНЕНИЕ: Захватываем переменные типа decimal
            decimal currentRenderCenterX = centerX;
            decimal currentRenderCenterY = centerY;
            decimal currentRenderZoom = zoom;
            try
            {
                await Task.Run(() => RenderFractal(token, currentRenderCenterX, currentRenderCenterY, currentRenderZoom), token);
            }
            catch (OperationCanceledException) { }
            catch (Exception) { /* Log Error */ }
            finally
            {
                isRenderingPreview = false;
            }
        }
        private delegate Color PaletteFunction(double t, int iter, int maxIterations, int maxColorIter);
        private Color GetPixelColor(int iter, int currentMaxIterations, int currentMaxColorIter)
        {
            if (iter == currentMaxIterations)
            {
                return (lastSelectedPaletteCheckBox?.Name == "checkBox6") ? Color.FromArgb(50, 50, 50) : Color.Black;
            }
            double t_capped = (double)Math.Min(iter, currentMaxColorIter) / currentMaxColorIter;
            double t_log = Math.Log(Math.Min(iter, currentMaxColorIter) + 1) / Math.Log(currentMaxColorIter + 1);
            PaletteFunction selectedPaletteFunc = GetDefaultPaletteColor;
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
            double t_param = (selectedPaletteFunc == GetDefaultPaletteColor) ? t_log : t_capped;
            return selectedPaletteFunc(t_param, iter, currentMaxIterations, currentMaxColorIter);
        }
        private Color GetPaletteMandelbrotClassicColor(double t_ignored, int iter, int maxIter, int maxClrIter)
        {
            double t_classic = (double)iter / maxIter;
            byte r_val, g_val, b_val;
            if (t_classic < 0.5)
            {
                double t_half = t_classic * 2.0;
                r_val = (byte)(t_half * 200);
                g_val = (byte)(t_half * 50);
                b_val = (byte)(t_half * 30);
            }
            else
            {
                double t_half = (t_classic - 0.5) * 2.0;
                r_val = (byte)(200 + t_half * 55);
                g_val = (byte)(50 + t_half * 205);
                b_val = (byte)(30 + t_half * 225);
            }
            return Color.FromArgb(r_val, g_val, b_val);
        }
        private Color GetDefaultPaletteColor(double t_log, int iter, int maxIter, int maxClrIter) { int cVal = (int)(255.0 * (1 - t_log)); return Color.FromArgb(cVal, cVal, cVal); }
        private Color GetPaletteColorBoxColor(double t_capped, int iter, int maxIter, int maxClrIter) { return ColorFromHSV(360.0 * t_capped, 0.8, 0.9); }
        private Color GetPaletteOldBWColor(double t_capped, int iter, int maxIter, int maxClrIter) { int cVal = 255 - (int)(255.0 * t_capped); return Color.FromArgb(cVal, cVal, cVal); }
        private Color LerpColor(Color a, Color b, double t) { t = Math.Max(0, Math.Min(1, t)); return Color.FromArgb((int)(a.R + (b.R - a.R) * t), (int)(a.G + (b.G - a.G) * t), (int)(a.B + (b.B - a.B) * t)); }
        private Color GetPalette1Color(double t, int iter, int maxIter, int maxClrIter) { Color c1 = Color.Black; Color c2 = Color.FromArgb(200, 0, 0); Color c3 = Color.FromArgb(255, 100, 0); Color c4 = Color.FromArgb(255, 255, 100); Color c5 = Color.White; if (t < 0.25) return LerpColor(c1, c2, t / 0.25); if (t < 0.50) return LerpColor(c2, c3, (t - 0.25) / 0.25); if (t < 0.75) return LerpColor(c3, c4, (t - 0.50) / 0.25); return LerpColor(c4, c5, (t - 0.75) / 0.25); }
        private Color GetPalette2Color(double t, int iter, int maxIter, int maxClrIter) { Color c1 = Color.Black; Color c2 = Color.FromArgb(0, 0, 100); Color c3 = Color.FromArgb(0, 120, 200); Color c4 = Color.FromArgb(170, 220, 255); Color c5 = Color.White; if (t < 0.25) return LerpColor(c1, c2, t / 0.25); if (t < 0.50) return LerpColor(c2, c3, (t - 0.25) / 0.25); if (t < 0.75) return LerpColor(c3, c4, (t - 0.50) / 0.25); return LerpColor(c4, c5, (t - 0.75) / 0.25); }
        private Color GetPalette3Color(double t, int iter, int maxIter, int maxClrIter) { double r_comp = Math.Sin(t * Math.PI * 3.0 + 0.5) * 0.45 + 0.5; double g_comp = Math.Sin(t * Math.PI * 3.0 + Math.PI * 2.0 / 3.0 + 0.5) * 0.45 + 0.5; double b_comp = Math.Sin(t * Math.PI * 3.0 + Math.PI * 4.0 / 3.0 + 0.5) * 0.45 + 0.5; return Color.FromArgb((int)(r_comp * 255), (int)(g_comp * 255), (int)(b_comp * 255)); }
        private Color GetPalette4Color(double t, int iter, int maxIter, int maxClrIter) { Color c1 = Color.FromArgb(10, 0, 20); Color c2 = Color.FromArgb(255, 0, 255); Color c3 = Color.FromArgb(0, 255, 255); Color c4 = Color.FromArgb(230, 230, 250); if (t < 0.1) return LerpColor(c1, c2, t / 0.1); if (t < 0.4) return LerpColor(c2, c1, (t - 0.1) / 0.3); if (t < 0.5) return LerpColor(c1, c3, (t - 0.4) / 0.1); if (t < 0.8) return LerpColor(c3, c1, (t - 0.5) / 0.3); return LerpColor(c1, c4, (t - 0.8) / 0.2); }
        private Color GetPalette5Color(double t, int iter, int maxIter, int maxClrIter) { int baseGray = 50 + (int)(t * 150); double shine = Math.Sin(t * Math.PI * 5); int finalGray = Math.Max(0, Math.Min(255, baseGray + (int)(shine * 40))); return Color.FromArgb(finalGray, finalGray, Math.Min(255, finalGray + (int)(t * 25))); }
        private Color GetPalette6Color(double t, int iter, int maxIter, int maxClrIter) { double hue = (t * 200.0 + 180.0) % 360.0; double sat = Math.Max(0.2, Math.Min(0.6, 0.35 + (Math.Sin(t * Math.PI * 2) * 0.1))); double val = Math.Max(0.7, Math.Min(0.95, 0.80 + (Math.Cos(t * Math.PI * 2.5) * 0.15))); return ColorFromHSV(hue, sat, val); }
        #endregion

        private void UpdateParameters()
        {
            maxIterations = (int)nudIterations.Value;
            threshold = (double)nudThreshold.Value;
            thresholdSquared = (decimal)threshold * (decimal)threshold;
            threadCount = cbThreads.SelectedItem.ToString() == "Auto"
                ? Environment.ProcessorCount
                : Convert.ToInt32(cbThreads.SelectedItem);
        }

        // ИЗМЕНЕНИЕ: Сигнатура метода для приема decimal
        private void RenderFractal(CancellationToken token, decimal renderCenterX, decimal renderCenterY, decimal renderZoom)
        {
            if (token.IsCancellationRequested) return;
            if (isHighResRendering || canvas2.Width <= 0 || canvas2.Height <= 0) return;

            Bitmap bmp = null;
            BitmapData bmpData = null;

            try
            {
                bmp = new Bitmap(width, height, PixelFormat.Format24bppRgb);
                token.ThrowIfCancellationRequested();

                Rectangle rect = new Rectangle(0, 0, width, height);
                bmpData = bmp.LockBits(rect, ImageLockMode.WriteOnly, bmp.PixelFormat);
                token.ThrowIfCancellationRequested();

                int stride = bmpData.Stride;
                IntPtr scan0 = bmpData.Scan0;
                byte[] buffer = new byte[Math.Abs(stride) * height];

                ParallelOptions po = new ParallelOptions { MaxDegreeOfParallelism = threadCount, CancellationToken = token };
                int done = 0;
                const int currentMaxColorIter = 1000;

                // ИЗМЕНЕНИЕ: Вычисляем масштаб с использованием decimal
                decimal scale = BASE_SCALE / renderZoom;
                decimal pixel_scale_w = scale / width;
                decimal pixel_scale_h = scale / height;
                decimal half_width = width / 2.0m;
                decimal half_height = height / 2.0m;


                Parallel.For(0, height, po, y =>
                {
                    if (token.IsCancellationRequested) return;

                    int rowOffset = y * stride;
                    for (int x = 0; x < width; x++)
                    {
                        // ИЗМЕНЕНИЕ: Вычисляем координаты с высокой точностью decimal
                        decimal c_re = renderCenterX + (x - half_width) * pixel_scale_w;
                        decimal c_im = renderCenterY + (y - half_height) * pixel_scale_h;

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
                        int index = rowOffset + x * 3;
                        buffer[index] = pixelColor.B;
                        buffer[index + 1] = pixelColor.G;
                        buffer[index + 2] = pixelColor.R;
                    }

                    int progress = Interlocked.Increment(ref done);
                    if (!token.IsCancellationRequested && progressBar.IsHandleCreated && !progressBar.IsDisposed && height > 0)
                    {
                        try
                        {
                            progressBar.BeginInvoke((Action)(() => {
                                if (progressBar.IsHandleCreated && !progressBar.IsDisposed && progressBar.Value <= progressBar.Maximum)
                                {
                                    progressBar.Value = Math.Min(progressBar.Maximum, (int)(100.0 * progress / height));
                                }
                            }));
                        }
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
                        if (token.IsCancellationRequested)
                        {
                            bmp?.Dispose();
                            return;
                        }
                        oldImage = canvas2.Image as Bitmap;
                        canvas2.Image = bmp;
                        renderedCenterX = renderCenterX;
                        renderedCenterY = renderCenterY;
                        renderedZoom = renderZoom;
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
                if (bmpData != null && bmp != null)
                {
                    try { bmp.UnlockBits(bmpData); } catch { }
                }
                if (bmp != null)
                {
                    bmp.Dispose();
                }
            }
        }

        // ИЗМЕНЕНИЕ: Сигнатура метода для приема decimal
        private Bitmap RenderFractalToBitmap(int renderWidth, int renderHeight, decimal currentCenterX, decimal currentCenterY,
                                             decimal currentZoom, decimal currentBaseScale,
                                             int currentMaxIterations_param, double currentThreshold_param, int numThreads,
                                             Action<int> reportProgressCallback)
        {
            if (renderWidth <= 0 || renderHeight <= 0)
            {
                return new Bitmap(1, 1);
            }

            Bitmap bmp = new Bitmap(renderWidth, renderHeight, PixelFormat.Format24bppRgb);
            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, renderWidth, renderHeight), ImageLockMode.WriteOnly, bmp.PixelFormat);
            int stride = bmpData.Stride;
            IntPtr scan0 = bmpData.Scan0;
            byte[] buffer = new byte[Math.Abs(stride) * renderHeight];

            ParallelOptions po = new ParallelOptions { MaxDegreeOfParallelism = numThreads };
            long done = 0;
            const int currentMaxColorIter_param = 1000;

            decimal currentThresholdSquared_param = (decimal)currentThreshold_param * (decimal)currentThreshold_param;

            // ИЗМЕНЕНИЕ: Вычисляем масштаб с использованием decimal
            decimal scale = currentBaseScale / currentZoom;
            decimal pixel_scale_w = scale / renderWidth;
            decimal pixel_scale_h = scale / renderHeight;
            decimal half_width = renderWidth / 2.0m;
            decimal half_height = renderHeight / 2.0m;

            Parallel.For(0, renderHeight, po, y =>
            {
                int rowOffset = y * stride;
                for (int x = 0; x < renderWidth; x++)
                {
                    // ИЗМЕНЕНИЕ: Вычисляем координаты с высокой точностью decimal
                    decimal c_re = currentCenterX + (x - half_width) * pixel_scale_w;
                    decimal c_im = currentCenterY + (y - half_height) * pixel_scale_h;

                    ComplexDecimal c0 = new ComplexDecimal(c_re, c_im);
                    ComplexDecimal z = ComplexDecimal.Zero;
                    int iter_val = 0;

                    while (iter_val < currentMaxIterations_param && z.MagnitudeSquared <= currentThresholdSquared_param)
                    {
                        z = new ComplexDecimal(Math.Abs(z.Real), Math.Abs(z.Imaginary));
                        z = z * z + c0;
                        iter_val++;
                    }

                    Color pixelColor = GetPixelColor(iter_val, currentMaxIterations_param, currentMaxColorIter_param);
                    int index = rowOffset + x * 3;
                    buffer[index] = pixelColor.B;
                    buffer[index + 1] = pixelColor.G;
                    buffer[index + 2] = pixelColor.R;
                }
                long currentDone = Interlocked.Increment(ref done);
                if (renderHeight > 0)
                {
                    reportProgressCallback((int)(100.0 * currentDone / renderHeight));
                }
            });

            Marshal.Copy(buffer, 0, scan0, buffer.Length);
            bmp.UnlockBits(bmpData);
            return bmp;
        }

        #region Final_Unchanged_Handlers
        private Color ColorFromHSV(double hue, double saturation, double value)
        {
            hue = (hue % 360 + 360) % 360;
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);

            value = Math.Max(0, Math.Min(1, value));
            saturation = Math.Max(0, Math.Min(1, saturation));

            int v_comp = Convert.ToInt32(value * 255);
            int p_comp = Convert.ToInt32(v_comp * (1 - saturation));
            int q_comp = Convert.ToInt32(v_comp * (1 - f * saturation));
            int t_comp = Convert.ToInt32(v_comp * (1 - (1 - f) * saturation));

            switch (hi)
            {
                case 0: return Color.FromArgb(v_comp, t_comp, p_comp);
                case 1: return Color.FromArgb(q_comp, v_comp, p_comp);
                case 2: return Color.FromArgb(p_comp, v_comp, t_comp);
                case 3: return Color.FromArgb(p_comp, q_comp, v_comp);
                case 4: return Color.FromArgb(t_comp, p_comp, v_comp);
                default: return Color.FromArgb(v_comp, p_comp, q_comp);
            }
        }
        private void Canvas_MouseWheel(object sender, MouseEventArgs e)
        {
            if (isHighResRendering) return;
            // ИЗМЕНЕНИЕ: Используем decimal для всех вычислений
            decimal zoomFactor = e.Delta > 0 ? 1.5m : 1.0m / 1.5m;
            decimal oldZoom = zoom;

            decimal scaleBeforeZoom = BASE_SCALE / oldZoom;
            decimal mouseRe = centerX + (e.X - width / 2.0m) * scaleBeforeZoom / width;
            decimal mouseIm = centerY + (e.Y - height / 2.0m) * scaleBeforeZoom / height;

            zoom = Math.Max(nudZoom.Minimum, Math.Min(nudZoom.Maximum, zoom * zoomFactor));

            decimal scaleAfterZoom = BASE_SCALE / zoom;
            centerX = mouseRe - (e.X - width / 2.0m) * scaleAfterZoom / width;
            centerY = mouseIm - (e.Y - height / 2.0m) * scaleAfterZoom / height;

            canvas2.Invalidate();

            if (nudZoom.Value != zoom)
            {
                nudZoom.Value = zoom;
            }
            else
            {
                ScheduleRender();
            }
        }
        private void Canvas_MouseDown(object sender, MouseEventArgs e)
        {
            if (isHighResRendering) return;
            if (e.Button == MouseButtons.Left)
            {
                panning = true;
                panStart = e.Location;
            }
        }
        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (isHighResRendering || !panning) return;
            // ИЗМЕНЕНИЕ: Используем decimal для всех вычислений
            decimal scale = BASE_SCALE / zoom;
            centerX -= (e.X - panStart.X) * scale / width;
            centerY -= (e.Y - panStart.Y) * scale / height;
            panStart = e.Location;
            canvas2.Invalidate();
            ScheduleRender();
        }

        private void Canvas_MouseUp(object? sender, MouseEventArgs e)
        {
            //throw new NotImplementedException();
            if (isHighResRendering) return;
            // ИСПРАВЛЕНИЕ: Проверяем, что кнопка отпущена во время панорамирования
            if (e.Button == MouseButtons.Left && panning)
            {
                panning = false;
                // ИСПРАВЛЕНИЕ: Освобождаем захват мыши
                canvas2.Capture = false;
                // ИСПРАВЛЕНИЕ: Гарантированно запускаем финальный рендер
                ScheduleRender();
            }

        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (isHighResRendering)
            {
                MessageBox.Show("Идет сохранение в высоком разрешении. Пожалуйста, подождите.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            using (var dlg = new SaveFileDialog { Filter = "PNG Image|*.png" })
            {
                dlg.FileName = $"burningship_preview_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    if (canvas2.Image != null)
                    {
                        canvas2.Image.Save(dlg.FileName, ImageFormat.Png);
                    }
                    else
                    {
                        MessageBox.Show("Нет изображения для сохранения!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
        private void btnRender_Click(object sender, EventArgs e)
        {
            if (isHighResRendering) return;
            ScheduleRender();
        }
        private void SetMainControlsEnabled(bool enabled)
        {
            Action action = () =>
            {
                if (btnRender != null) btnRender.Enabled = enabled;
                nudIterations.Enabled = enabled;
                nudThreshold.Enabled = enabled;
                cbThreads.Enabled = enabled;
                nudZoom.Enabled = enabled;
                if (nudW != null) nudW.Enabled = enabled;
                if (nudH != null) nudH.Enabled = enabled;
                foreach (var cb in paletteCheckBoxes.Where(cb => cb != null))
                {
                    cb.Enabled = enabled;
                }
                if (enabled)
                {
                    HandleColorBoxEnableState();
                }
                else if (colorBox != null)
                {
                    colorBox.Enabled = false;
                }
                Button savePreviewButton = Controls.Find("btnSave", true).FirstOrDefault() as Button;
                if (savePreviewButton != null) savePreviewButton.Enabled = enabled;
            };

            if (this.InvokeRequired)
            {
                this.Invoke(action);
            }
            else
            {
                action();
            }
        }
        private async void btnSave_Click_1(object sender, EventArgs e)
        {
            if (isHighResRendering)
            {
                MessageBox.Show("Процесс сохранения в высоком разрешении уже запущен.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            int saveWidth = (int)nudW.Value;
            int saveHeight = (int)nudH.Value;
            if (saveWidth <= 0 || saveHeight <= 0)
            {
                MessageBox.Show("Ширина и высота изображения для сохранения должны быть больше 0.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string suggestedFileName = $"fractal_burningship_{timestamp}.png";
            using (SaveFileDialog saveDialog = new SaveFileDialog
            {
                Filter = "PNG Image|*.png",
                Title = "Сохранить фрактал Горящий Корабль (Высокое разрешение)",
                FileName = suggestedFileName
            })
            {
                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    Button currentActionSaveButton = sender as Button;
                    isHighResRendering = true;
                    if (currentActionSaveButton != null) currentActionSaveButton.Enabled = false;
                    SetMainControlsEnabled(false);
                    if (progressPNG != null)
                    {
                        progressPNG.Value = 0;
                        progressPNG.Visible = true;
                    }
                    try
                    {
                        UpdateParameters();
                        // ИЗМЕНЕНИЕ: Захватываем переменные типа decimal
                        int currentMaxIterations_Capture = this.maxIterations;
                        double currentThreshold_Capture = this.threshold;
                        decimal currentZoom_Capture = this.zoom;
                        decimal currentCenterX_Capture = this.centerX;
                        decimal currentCenterY_Capture = this.centerY;
                        int currentThreadCount_Capture = this.threadCount;
                        Bitmap highResBitmap = await Task.Run(() => RenderFractalToBitmap(
                            saveWidth, saveHeight,
                            currentCenterX_Capture, currentCenterY_Capture, currentZoom_Capture,
                            BASE_SCALE, // BASE_SCALE теперь decimal
                            currentMaxIterations_Capture, currentThreshold_Capture,
                            currentThreadCount_Capture,
                            progressPercentage =>
                            {
                                if (progressPNG != null && progressPNG.IsHandleCreated && !progressPNG.IsDisposed)
                                {
                                    try
                                    {
                                        progressPNG.Invoke((Action)(() => {
                                            if (progressPNG.Maximum > 0 && progressPNG.Value <= progressPNG.Maximum)
                                            {
                                                progressPNG.Value = Math.Min(progressPNG.Maximum, progressPercentage);
                                            }
                                        }));
                                    }
                                    catch (ObjectDisposedException) { }
                                    catch (InvalidOperationException) { }
                                }
                            }
                        ));
                        highResBitmap.Save(saveDialog.FileName, ImageFormat.Png);
                        highResBitmap.Dispose();
                        MessageBox.Show("Изображение успешно сохранено в высоком разрешении!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при сохранении изображения: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        isHighResRendering = false;
                        if (currentActionSaveButton != null) currentActionSaveButton.Enabled = true;
                        SetMainControlsEnabled(true);
                        if (progressPNG != null && progressPNG.IsHandleCreated && !progressPNG.IsDisposed)
                        {
                            try
                            {
                                progressPNG.Invoke((Action)(() => {
                                    progressPNG.Visible = false;
                                    progressPNG.Value = 0;
                                }));
                            }
                            catch (ObjectDisposedException) { }
                            catch (InvalidOperationException) { }
                        }
                    }
                }
            }
        }
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            renderTimer?.Stop();
            previewRenderCts?.Cancel();
            previewRenderCts?.Dispose();
            renderTimer?.Dispose();
            base.OnFormClosed(e);
        }
        #endregion
    }
}
// --- END OF FILE FractalMondelbrotShip.cs ---