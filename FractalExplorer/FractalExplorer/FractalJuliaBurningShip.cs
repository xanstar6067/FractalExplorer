// --- START OF FILE FractalburningShipJulia.cs ---

using System.Drawing.Imaging;
// using System.Numerics; // ИЗМЕНЕНИЕ: Заменено на наш ComplexDecimal
using System.Runtime.InteropServices; // Для использования Marshal.Copy

namespace FractalDraving
{
    /// <summary>
    /// Главная форма приложения для отображения и взаимодействия с фракталом "Горящий Корабль" (версия Жюлиа).
    /// </summary>
    public partial class FractalJuliaBurningShip : Form, IFractalForm
    {
        // Таймер для отложенного рендеринга предварительного просмотра фрактала
        private System.Windows.Forms.Timer renderTimer;

        // Комплексное число 'c', определяющее конкретный фрактал
        private ComplexDecimal c;
        // Максимальное количество итераций для определения принадлежности точки множеству
        private int maxIterations;
        // Порог для определения "ухода в бесконечность" при итерациях
        private double threshold;
        // Квадрат порога для оптимизации вычислений
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
        private const decimal BASE_SCALE = 1.0m;

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

        // ИЗМЕНЕНИЕ: Константы для отображения "карты" фрактала (используем decimal)
        private const decimal MANDELBROT_MIN_RE = -2.0m;
        private const decimal MANDELBROT_MAX_RE = 1.5m;
        private const decimal MANDELBROT_MIN_IM = -1.5m;
        private const decimal MANDELBROT_MAX_IM = 1.0m;
        private const int MANDELBROT_PREVIEW_ITERATIONS = 75;

        // Форма для выбора параметра 'c'
        private BurningShipCSelectorForm mandelbrotCSelectorWindow;

        // ИЗМЕНЕНИЕ: Поля для хранения параметров отрисованного битмапа (используем decimal)
        private decimal renderedCenterX;
        private decimal renderedCenterY;
        private decimal renderedZoom;

        /// <summary>
        /// Свойство для доступа к масштабу лупы в окне выбора 'c'.
        /// Значение берется из элемента управления NumericUpDown.
        /// </summary>
        public double LoupeZoom => (double)nudBaseScale.Value;

        /// <summary>
        /// Событие, возникающее при изменении масштаба лупы.
        /// </summary>
        public event EventHandler LoupeZoomChanged;

        /// <summary>
        /// Конструктор формы фрактала "Горящий Корабль" (Жюлиа).
        /// </summary>
        public FractalJuliaBurningShip()
        {
            InitializeComponent();
            this.Text = "Фрактал Горящий Корабль (Жюлиа)";
        }

        /// <summary>
        /// Обработчик события загрузки формы.
        /// </summary>
        private async void Form1_Load(object sender, EventArgs e)
        {
            width = canvas1.Width;
            height = canvas1.Height;

            renderTimer = new System.Windows.Forms.Timer { Interval = 300 };
            renderTimer.Tick += RenderTimer_Tick;

            paletteCheckBoxes = new CheckBox[] {
                colorBox, oldRenderBW,
                Controls.Find("checkBox1", true).FirstOrDefault() as CheckBox,
                Controls.Find("checkBox2", true).FirstOrDefault() as CheckBox,
                Controls.Find("checkBox3", true).FirstOrDefault() as CheckBox,
                Controls.Find("checkBox4", true).FirstOrDefault() as CheckBox,
                Controls.Find("checkBox5", true).FirstOrDefault() as CheckBox,
                Controls.Find("checkBox6", true).FirstOrDefault() as CheckBox
            };

            foreach (var cb in paletteCheckBoxes.Where(cb => cb != null))
            {
                cb.CheckedChanged += PaletteCheckBox_CheckedChanged;
            }

            nudRe1.ValueChanged += ParamControl_Changed;
            nudIm1.ValueChanged += ParamControl_Changed;
            nudIterations1.ValueChanged += ParamControl_Changed;
            nudThreshold1.ValueChanged += ParamControl_Changed;
            cbThreads1.SelectedIndexChanged += ParamControl_Changed;
            nudZoom.ValueChanged += ParamControl_Changed;
            nudBaseScale.ValueChanged += NudBaseScale_ValueChanged;

            canvas1.MouseWheel += Canvas_MouseWheel;
            canvas1.MouseDown += Canvas_MouseDown;
            canvas1.MouseMove += Canvas_MouseMove;
            canvas1.MouseUp += Canvas_MouseUp;
            canvas1.Paint += Canvas_Paint;

            if (mandelbrotCanvas1 != null)
            {
                mandelbrotCanvas1.Click += mandelbrotCanvas_Click;
                mandelbrotCanvas1.Paint += mandelbrotCanvas_Paint;
                await Task.Run(() => RenderAndDisplayMandelbrotSet());
            }

            int cores = Environment.ProcessorCount;
            for (int i = 1; i <= cores; i++)
            {
                cbThreads1.Items.Add(i);
            }
            cbThreads1.Items.Add("Auto");
            cbThreads1.SelectedItem = "Auto";

            nudRe1.Minimum = -2m;
            nudRe1.Maximum = 2m;
            nudRe1.DecimalPlaces = 15;
            nudRe1.Increment = 0.001m;

            nudIm1.Minimum = -2m;
            nudIm1.Maximum = 2m;
            nudIm1.DecimalPlaces = 15;
            nudIm1.Increment = 0.001m;

            nudIterations1.Minimum = 50;
            nudIterations1.Maximum = 100000;
            nudIterations1.Value = 600;

            nudThreshold1.Minimum = 2m;
            nudThreshold1.Maximum = 10m;
            nudThreshold1.DecimalPlaces = 1;
            nudThreshold1.Increment = 0.1m;
            nudThreshold1.Value = 2m;

            nudZoom.DecimalPlaces = 4;
            nudZoom.Increment = 0.1m;
            nudZoom.Minimum = 0.01m;
            //nudZoom.Maximum = 1000000000000m;
            nudZoom.Value = 1m;

            nudBaseScale.Minimum = 1m;
            nudBaseScale.Maximum = 10m;
            nudBaseScale.DecimalPlaces = 1;
            nudBaseScale.Increment = 0.1m;
            nudBaseScale.Value = 4m;

            this.Resize += Form1_Resize;
            canvas1.Resize += Canvas_Resize;

            renderedCenterX = centerX;
            renderedCenterY = centerY;
            renderedZoom = zoom;

            HandleColorBoxEnableState();
            ScheduleRender();
        }

        #region Unchanged_Drawing_Methods
        private void Canvas_Paint(object sender, PaintEventArgs e)
        {
            if (canvas1.Image == null || width <= 0 || height <= 0)
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
                e.Graphics.DrawImageUnscaled(canvas1.Image, Point.Empty);
                return;
            }
            decimal renderedImage_re_min = renderedCenterX - scaleRendered / 2.0m;
            decimal renderedImage_im_min = renderedCenterY - scaleRendered / 2.0m;
            decimal currentView_re_min = centerX - scaleCurrent / 2.0m;
            decimal currentView_im_min = centerY - scaleCurrent / 2.0m;
            float p1_X = (float)((renderedImage_re_min - currentView_re_min) / scaleCurrent * width);
            float p1_Y = (float)((renderedImage_im_min - currentView_im_min) / scaleCurrent * height);
            float w_prime = (float)(width * (scaleRendered / scaleCurrent));
            float h_prime = (float)(height * (scaleRendered / scaleCurrent));
            PointF destPoint1 = new PointF(p1_X, p1_Y);
            PointF destPoint2 = new PointF(p1_X + w_prime, p1_Y);
            PointF destPoint3 = new PointF(p1_X, p1_Y + h_prime);
            e.Graphics.Clear(Color.Black);
            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Bilinear;
            e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
            if (w_prime > 0 && h_prime > 0)
            {
                try
                {
                    e.Graphics.DrawImage(canvas1.Image, new PointF[] { destPoint1, destPoint2, destPoint3 });
                }
                catch (ArgumentException)
                {
                    e.Graphics.DrawImageUnscaled(canvas1.Image, Point.Empty);
                }
            }
            else
            {
                e.Graphics.DrawImageUnscaled(canvas1.Image, Point.Empty);
            }
        }
        private void NudBaseScale_ValueChanged(object sender, EventArgs e)
        {
            LoupeZoomChanged?.Invoke(this, EventArgs.Empty);
        }
        private void RenderAndDisplayMandelbrotSet()
        {
            if (mandelbrotCanvas1 == null || mandelbrotCanvas1.Width <= 0 || mandelbrotCanvas1.Height <= 0) return;
            Bitmap mandelbrotImage = RenderMandelbrotSetInternal(mandelbrotCanvas1.Width, mandelbrotCanvas1.Height, MANDELBROT_PREVIEW_ITERATIONS);
            if (mandelbrotCanvas1.IsHandleCreated && !mandelbrotCanvas1.IsDisposed)
            {
                mandelbrotCanvas1.Invoke((Action)(() =>
                {
                    mandelbrotCanvas1.Image?.Dispose();
                    mandelbrotCanvas1.Image = mandelbrotImage;
                    mandelbrotCanvas1.Invalidate();
                }));
            }
            else
            {
                mandelbrotImage?.Dispose();
            }
        }
        #endregion

        /// <summary>
        /// Внутренний метод для рендеринга "карты" (Горящий Корабль) в объект Bitmap.
        /// </summary>
        private Bitmap RenderMandelbrotSetInternal(int canvasWidth, int canvasHeight, int iterationsLimit)
        {
            Bitmap bmp = new Bitmap(canvasWidth, canvasHeight, PixelFormat.Format24bppRgb);
            if (canvasWidth <= 0 || canvasHeight <= 0) return bmp;

            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, canvasWidth, canvasHeight), ImageLockMode.WriteOnly, bmp.PixelFormat);
            int stride = bmpData.Stride;
            IntPtr scan0 = bmpData.Scan0;
            int bytes = Math.Abs(stride) * canvasHeight;
            byte[] buffer = new byte[bytes];

            // ИЗМЕНЕНИЕ: Используем decimal для вычислений
            decimal reRange = MANDELBROT_MAX_RE - MANDELBROT_MIN_RE;
            decimal imRange = MANDELBROT_MAX_IM - MANDELBROT_MIN_IM;
            decimal previewThresholdSquared = 4m;

            Parallel.For(0, canvasHeight, y_coord =>
            {
                int rowOffset = y_coord * stride;
                for (int x_coord = 0; x_coord < canvasWidth; x_coord++)
                {
                    // ИЗМЕНЕНИЕ: Вычисляем координаты с высокой точностью decimal
                    decimal c_re = MANDELBROT_MIN_RE + (x_coord / (decimal)canvasWidth) * reRange;
                    decimal c_im = MANDELBROT_MAX_IM - (y_coord / (decimal)canvasHeight) * imRange;

                    ComplexDecimal c0 = new ComplexDecimal(c_re, c_im);
                    ComplexDecimal z = ComplexDecimal.Zero;
                    int iter = 0;

                    while (iter < iterationsLimit && z.MagnitudeSquared < previewThresholdSquared)
                    {
                        z = new ComplexDecimal(Math.Abs(z.Real), Math.Abs(z.Imaginary));
                        z = z * z + c0;
                        iter++;
                    }

                    byte r_val, g_val, b_val;
                    if (iter == iterationsLimit)
                    {
                        r_val = g_val = b_val = 0;
                    }
                    else
                    {
                        double t = (double)iter / iterationsLimit;
                        if (t < 0.5)
                        {
                            r_val = (byte)(t * 2 * 200); g_val = (byte)(t * 2 * 50); b_val = (byte)(t * 2 * 30);
                        }
                        else
                        {
                            t = (t - 0.5) * 2;
                            r_val = (byte)(200 + t * 55); g_val = (byte)(50 + t * 205); b_val = (byte)(30 + t * 225);
                        }
                    }
                    int index = rowOffset + x_coord * 3;
                    buffer[index] = b_val; buffer[index + 1] = g_val; buffer[index + 2] = r_val;
                }
            });

            Marshal.Copy(buffer, 0, scan0, bytes);
            bmp.UnlockBits(bmpData);
            return bmp;
        }

        #region Unchanged_UI_Handlers
        private void MandelbrotCSelectorWindow_CoordinatesSelected(double re, double im)
        {
            decimal dec_re = (decimal)re;
            decimal dec_im = (decimal)im;
            dec_re = Math.Max(nudRe1.Minimum, Math.Min(nudRe1.Maximum, dec_re));
            dec_im = Math.Max(nudIm1.Minimum, Math.Min(nudIm1.Maximum, dec_im));
            bool changed = false;
            if (nudRe1.Value != dec_re) { nudRe1.Value = dec_re; changed = true; }
            if (nudIm1.Value != dec_im) { nudIm1.Value = dec_im; changed = true; }
            if (changed && mandelbrotCanvas1 != null && mandelbrotCanvas1.IsHandleCreated && !mandelbrotCanvas1.IsDisposed)
            {
                mandelbrotCanvas1.Invoke((Action)(() => mandelbrotCanvas1.Invalidate()));
            }
        }
        private void mandelbrotCanvas_Paint(object sender, PaintEventArgs e)
        {
            if (mandelbrotCanvas1 == null || mandelbrotCanvas1.Width <= 0 || mandelbrotCanvas1.Height <= 0) return;
            // ИЗМЕНЕНИЕ: Используем decimal
            decimal reRange = MANDELBROT_MAX_RE - MANDELBROT_MIN_RE;
            decimal imRange = MANDELBROT_MAX_IM - MANDELBROT_MIN_IM;
            decimal currentCRe = nudRe1.Value;
            decimal currentCIm = nudIm1.Value;
            if (currentCRe >= MANDELBROT_MIN_RE && currentCRe <= MANDELBROT_MAX_RE && currentCIm >= MANDELBROT_MIN_IM && currentCIm <= MANDELBROT_MAX_IM)
            {
                int markerX = (int)((currentCRe - MANDELBROT_MIN_RE) / reRange * mandelbrotCanvas1.Width);
                int markerY = (int)((MANDELBROT_MAX_IM - currentCIm) / imRange * mandelbrotCanvas1.Height);
                using (Pen markerPen = new Pen(Color.FromArgb(200, Color.Green), 1.5f))
                {
                    e.Graphics.DrawLine(markerPen, 0, markerY, mandelbrotCanvas1.Width, markerY);
                    e.Graphics.DrawLine(markerPen, markerX, 0, markerX, mandelbrotCanvas1.Height);
                }
            }
        }
        private void mandelbrotCanvas_Click(object sender, EventArgs e)
        {
            if (mandelbrotCanvas1 == null || mandelbrotCanvas1.Width <= 0 || mandelbrotCanvas1.Height <= 0) return;
            MouseEventArgs mouseArgs = e as MouseEventArgs;
            if (mouseArgs == null)
            {
                Point clientPoint = mandelbrotCanvas1.PointToClient(Control.MousePosition);
                if ((Control.MouseButtons & MouseButtons.Left) != MouseButtons.Left) return;
                mouseArgs = new MouseEventArgs(MouseButtons.Left, 1, clientPoint.X, clientPoint.Y, 0);
            }
            if (mouseArgs.Button != MouseButtons.Left) return;
            double initialRe = (double)nudRe1.Value;
            double initialIm = (double)nudIm1.Value;
            if (mandelbrotCSelectorWindow == null || mandelbrotCSelectorWindow.IsDisposed)
            {
                mandelbrotCSelectorWindow = new BurningShipCSelectorForm(this, initialRe, initialIm);
                mandelbrotCSelectorWindow.CoordinatesSelected += MandelbrotCSelectorWindow_CoordinatesSelected;
                mandelbrotCSelectorWindow.FormClosed += (s, args) => { mandelbrotCSelectorWindow = null; };
                mandelbrotCSelectorWindow.Show(this);
            }
            else
            {
                mandelbrotCSelectorWindow.Activate();
                mandelbrotCSelectorWindow.SetSelectedCoordinates(initialRe, initialIm, true);
            }
            if (mandelbrotCanvas1.IsHandleCreated && !mandelbrotCanvas1.IsDisposed)
            {
                mandelbrotCanvas1.Invalidate();
            }
        }
        private void PaletteCheckBox_CheckedChanged(object sender, EventArgs e)
        {
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
            foreach (var cb in paletteCheckBoxes.Where(cb => cb != null)) cb.CheckedChanged += PaletteCheckBox_CheckedChanged;
            HandleColorBoxEnableState();
            ScheduleRender();
        }
        private void HandleColorBoxEnableState()
        {
            if (paletteCheckBoxes == null || colorBox == null || oldRenderBW == null) return;
            bool isAnyNewPaletteCbChecked = paletteCheckBoxes.Skip(2).Any(cb => cb != null && cb.Checked);
            if (isAnyNewPaletteCbChecked) colorBox.Enabled = true;
            else if (colorBox.Checked && !oldRenderBW.Checked) colorBox.Enabled = true;
            else colorBox.Enabled = !oldRenderBW.Checked;
        }
        private void Form1_Resize(object sender, EventArgs e) => ResizeCanvas();
        private void Canvas_Resize(object sender, EventArgs e) => ResizeCanvas();
        private void ResizeCanvas()
        {
            if (isHighResRendering) return;
            if (canvas1.Width <= 0 || canvas1.Height <= 0) return;
            width = canvas1.Width; height = canvas1.Height;
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
            if (mandelbrotCanvas1 != null && (sender == nudRe1 || sender == nudIm1))
            {
                if (mandelbrotCanvas1.IsHandleCreated && !mandelbrotCanvas1.IsDisposed) mandelbrotCanvas1.Invalidate();
            }
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
            // ИЗМЕНЕНИЕ: Захватываем переменные типа decimal
            decimal currentRenderCenterX = centerX;
            decimal currentRenderCenterY = centerY;
            decimal currentRenderZoom = zoom;
            try
            {
                await Task.Run(() => RenderFractal(token, currentRenderCenterX, currentRenderCenterY, currentRenderZoom), token);
            }
            catch (OperationCanceledException) { }
            catch (Exception) { /* log error */ }
            finally { isRenderingPreview = false; }
        }
        #endregion

        private void UpdateParameters()
        {
            c = new ComplexDecimal(nudRe1.Value, nudIm1.Value);
            maxIterations = (int)nudIterations1.Value;
            threshold = (double)nudThreshold1.Value;
            thresholdSquared = nudThreshold1.Value * nudThreshold1.Value;
            threadCount = cbThreads1.SelectedItem.ToString() == "Auto" ? Environment.ProcessorCount : Convert.ToInt32(cbThreads1.SelectedItem);
        }

        #region Palettes
        private delegate Color PaletteFunction(double t, int iter, int maxIterations, int maxColorIter);
        private Color GetPixelColor(int iter, int currentMaxIterations, int currentMaxColorIter)
        {
            if (iter == currentMaxIterations) return (lastSelectedPaletteCheckBox?.Name == "checkBox6") ? Color.FromArgb(50, 50, 50) : Color.Black;
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
            }
            double t_param = (selectedPaletteFunc == GetDefaultPaletteColor) ? t_log : t_capped;
            return selectedPaletteFunc(t_param, iter, currentMaxIterations, currentMaxColorIter);
        }
        private Color GetDefaultPaletteColor(double t_log, int iter, int maxIter, int maxClrIter) { int cVal = (int)(255.0 * (1 - t_log)); return Color.FromArgb(cVal, cVal, cVal); }
        private Color GetPaletteColorBoxColor(double t_capped, int iter, int maxIter, int maxClrIter) { return ColorFromHSV(360.0 * t_capped, 0.6, 1.0); }
        private Color GetPaletteOldBWColor(double t_capped, int iter, int maxIter, int maxClrIter) { int cVal = 255 - (int)(255.0 * t_capped); return Color.FromArgb(cVal, cVal, cVal); }
        private Color LerpColor(Color a, Color b, double t) { t = Math.Max(0, Math.Min(1, t)); return Color.FromArgb((int)(a.R + (b.R - a.R) * t), (int)(a.G + (b.G - a.G) * t), (int)(a.B + (b.B - a.B) * t)); }
        private Color GetPalette1Color(double t, int iter, int maxIter, int maxClrIter) { Color c1 = Color.Black, c2 = Color.FromArgb(200, 0, 0), c3 = Color.FromArgb(255, 100, 0), c4 = Color.FromArgb(255, 255, 100), c5 = Color.White; if (t < 0.25) return LerpColor(c1, c2, t / 0.25); if (t < 0.50) return LerpColor(c2, c3, (t - 0.25) / 0.25); if (t < 0.75) return LerpColor(c3, c4, (t - 0.50) / 0.25); return LerpColor(c4, c5, (t - 0.75) / 0.25); }
        private Color GetPalette2Color(double t, int iter, int maxIter, int maxClrIter) { Color c1 = Color.Black, c2 = Color.FromArgb(0, 0, 100), c3 = Color.FromArgb(0, 120, 200), c4 = Color.FromArgb(170, 220, 255), c5 = Color.White; if (t < 0.25) return LerpColor(c1, c2, t / 0.25); if (t < 0.50) return LerpColor(c2, c3, (t - 0.25) / 0.25); if (t < 0.75) return LerpColor(c3, c4, (t - 0.50) / 0.25); return LerpColor(c4, c5, (t - 0.75) / 0.25); }
        private Color GetPalette3Color(double t, int iter, int maxIter, int maxClrIter) { double r = Math.Sin(t * Math.PI * 3.0 + 0.5) * 0.45 + 0.5, g = Math.Sin(t * Math.PI * 3.0 + Math.PI * 2.0 / 3.0 + 0.5) * 0.45 + 0.5, b = Math.Sin(t * Math.PI * 3.0 + Math.PI * 4.0 / 3.0 + 0.5) * 0.45 + 0.5; return Color.FromArgb((int)(r * 255), (int)(g * 255), (int)(b * 255)); }
        private Color GetPalette4Color(double t, int iter, int maxIter, int maxClrIter) { Color c1 = Color.FromArgb(10, 0, 20), c2 = Color.FromArgb(255, 0, 255), c3 = Color.FromArgb(0, 255, 255), c4 = Color.FromArgb(230, 230, 250); if (t < 0.1) return LerpColor(c1, c2, t / 0.1); if (t < 0.4) return LerpColor(c2, c1, (t - 0.1) / 0.3); if (t < 0.5) return LerpColor(c1, c3, (t - 0.4) / 0.1); if (t < 0.8) return LerpColor(c3, c1, (t - 0.5) / 0.3); return LerpColor(c1, c4, (t - 0.8) / 0.2); }
        private Color GetPalette5Color(double t, int iter, int maxIter, int maxClrIter) { int g = 50 + (int)(t * 150); double s = Math.Sin(t * Math.PI * 5); int f = Math.Max(0, Math.Min(255, g + (int)(s * 40))); return Color.FromArgb(f, f, Math.Min(255, f + (int)(t * 25))); }
        private Color GetPalette6Color(double t, int iter, int maxIter, int maxClrIter) { double h = (t * 200.0 + 180.0) % 360.0, s = Math.Max(0.2, Math.Min(0.6, 0.35 + (Math.Sin(t * Math.PI * 2) * 0.1))), v = Math.Max(0.7, Math.Min(0.95, 0.80 + (Math.Cos(t * Math.PI * 2.5) * 0.15))); return ColorFromHSV(h, s, v); }
        #endregion

        /// <summary>
        /// Основной метод рендеринга фрактала.
        /// </summary>
        // ИЗМЕНЕНИЕ: Сигнатура метода для приема decimal
        private void RenderFractal(CancellationToken token, decimal renderCenterX, decimal renderCenterY, decimal renderZoom)
        {
            if (token.IsCancellationRequested) return;
            if (isHighResRendering || canvas1.Width <= 0 || canvas1.Height <= 0) return;

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
                decimal half_width = width / 2.0m;
                decimal half_height = height / 2.0m;

                Parallel.For(0, height, po, y =>
                {
                    int rowOffset = y * stride;
                    for (int x = 0; x < width; x++)
                    {
                        // ИЗМЕНЕНИЕ: Вычисляем координаты с высокой точностью decimal
                        decimal re = renderCenterX + (x - half_width) * scale / width;
                        decimal im = renderCenterY + (y - half_height) * scale / height;

                        ComplexDecimal z_val = new ComplexDecimal(re, im);
                        int iter_val = 0;

                        while (iter_val < maxIterations && z_val.MagnitudeSquared <= thresholdSquared)
                        {
                            z_val = new ComplexDecimal(Math.Abs(z_val.Real), Math.Abs(z_val.Imaginary));
                            z_val = z_val * z_val + c;
                            iter_val++;
                        }

                        Color pixelColor = GetPixelColor(iter_val, maxIterations, currentMaxColorIter);
                        int index = rowOffset + x * 3;
                        buffer[index] = pixelColor.B;
                        buffer[index + 1] = pixelColor.G;
                        buffer[index + 2] = pixelColor.R;
                    }

                    int progress = Interlocked.Increment(ref done);
                    if (!token.IsCancellationRequested && progressBar1.IsHandleCreated && !progressBar1.IsDisposed && height > 0)
                    {
                        try { progressBar1.BeginInvoke((Action)(() => { if (progressBar1.IsHandleCreated && !progressBar1.IsDisposed && progressBar1.Value <= progressBar1.Maximum) progressBar1.Value = Math.Min(progressBar1.Maximum, (int)(100.0 * progress / height)); })); } catch (InvalidOperationException) { }
                    }
                });

                token.ThrowIfCancellationRequested();
                Marshal.Copy(buffer, 0, scan0, buffer.Length);
                bmp.UnlockBits(bmpData);
                bmpData = null;
                token.ThrowIfCancellationRequested();

                if (canvas1.IsHandleCreated && !canvas1.IsDisposed)
                {
                    Bitmap oldImage = null;
                    canvas1.Invoke((Action)(() =>
                    {
                        if (token.IsCancellationRequested) { bmp?.Dispose(); return; }
                        oldImage = canvas1.Image as Bitmap;
                        canvas1.Image = bmp;
                        renderedCenterX = renderCenterX; renderedCenterY = renderCenterY; renderedZoom = renderZoom;
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
                if (bmpData != null && bmp != null) { try { bmp.UnlockBits(bmpData); } catch { } }
                if (bmp != null) bmp.Dispose();
            }
        }

        /// <summary>
        /// Рендерит фрактал в Bitmap для сохранения.
        /// </summary>
        // ИЗМЕНЕНИЕ: Сигнатура метода для приема decimal
        private Bitmap RenderFractalToBitmap(int renderWidth, int renderHeight, decimal currentCenterX, decimal currentCenterY,
                                             decimal currentZoom, decimal currentBaseScale,
                                             ComplexDecimal currentC_param,
                                             int currentMaxIterations_param, double currentThreshold_param, int numThreads,
                                             Action<int> reportProgressCallback)
        {
            if (renderWidth <= 0 || renderHeight <= 0) return new Bitmap(1, 1);
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
            decimal half_width = renderWidth / 2.0m;
            decimal half_height = renderHeight / 2.0m;


            Parallel.For(0, renderHeight, po, y =>
            {
                int rowOffset = y * stride;
                for (int x = 0; x < renderWidth; x++)
                {
                    // ИЗМЕНЕНИЕ: Вычисляем координаты с высокой точностью decimal
                    decimal re = currentCenterX + (x - half_width) * scale / renderWidth;
                    decimal im = currentCenterY + (y - half_height) * scale / renderHeight;

                    ComplexDecimal z_val = new ComplexDecimal(re, im);
                    int iter_val = 0;

                    while (iter_val < currentMaxIterations_param && z_val.MagnitudeSquared <= currentThresholdSquared_param)
                    {
                        z_val = new ComplexDecimal(Math.Abs(z_val.Real), Math.Abs(z_val.Imaginary));
                        z_val = z_val * z_val + currentC_param;
                        iter_val++;
                    }

                    Color pixelColor = GetPixelColor(iter_val, currentMaxIterations_param, currentMaxColorIter_param);
                    int index = rowOffset + x * 3;
                    buffer[index] = pixelColor.B; buffer[index + 1] = pixelColor.G; buffer[index + 2] = pixelColor.R;
                }
                long currentDone = Interlocked.Increment(ref done);
                if (renderHeight > 0) reportProgressCallback((int)(100.0 * currentDone / renderHeight));
            });

            Marshal.Copy(buffer, 0, scan0, buffer.Length);
            bmp.UnlockBits(bmpData);
            return bmp;
        }

        #region Unchanged_Handlers_and_Helpers
        private Color ColorFromHSV(double hue, double saturation, double value) { hue = (hue % 360 + 360) % 360; int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6; double f = hue / 60 - Math.Floor(hue / 60); value = Math.Max(0, Math.Min(1, value)); saturation = Math.Max(0, Math.Min(1, saturation)); int v_comp = Convert.ToInt32(value * 255); int p_comp = Convert.ToInt32(v_comp * (1 - saturation)); int q_comp = Convert.ToInt32(v_comp * (1 - f * saturation)); int t_comp = Convert.ToInt32(v_comp * (1 - (1 - f) * saturation)); switch (hi) { case 0: return Color.FromArgb(v_comp, t_comp, p_comp); case 1: return Color.FromArgb(q_comp, v_comp, p_comp); case 2: return Color.FromArgb(p_comp, v_comp, t_comp); case 3: return Color.FromArgb(p_comp, q_comp, v_comp); case 4: return Color.FromArgb(t_comp, p_comp, v_comp); default: return Color.FromArgb(v_comp, p_comp, q_comp); } }
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
            canvas1.Invalidate();
            if (nudZoom.Value != zoom) nudZoom.Value = zoom;
            else ScheduleRender();
        }
        private void Canvas_MouseDown(object sender, MouseEventArgs e) { if (isHighResRendering) return; if (e.Button == MouseButtons.Left) { panning = true; panStart = e.Location; } }
        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (isHighResRendering || !panning) return;
            // ИЗМЕНЕНИЕ: Используем decimal для всех вычислений
            decimal scale = BASE_SCALE / zoom;
            centerX -= (e.X - panStart.X) * scale / width;
            centerY -= (e.Y - panStart.Y) * scale / height;
            panStart = e.Location;
            canvas1.Invalidate();
            ScheduleRender();
        }
        private void Canvas_MouseUp(object sender, MouseEventArgs e) { if (isHighResRendering) return; if (e.Button == MouseButtons.Left) panning = false; }
        private void BtnSave_Click(object sender, EventArgs e) { if (isHighResRendering) { MessageBox.Show("Идет сохранение в высоком разрешении. Пожалуйста, подождите.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information); return; } using (var dlg = new SaveFileDialog { Filter = "PNG Image|*.png" }) { if (dlg.ShowDialog() == DialogResult.OK) { if (canvas1.Image != null) canvas1.Image.Save(dlg.FileName, ImageFormat.Png); else MessageBox.Show("Нет изображения для сохранения!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error); } } }
        private void btnRender_Click(object sender, EventArgs e) { if (isHighResRendering) return; ScheduleRender(); }
        private void SetMainControlsEnabled(bool enabled) { Action action = () => { if (btnRender1 != null) btnRender1.Enabled = enabled; nudRe1.Enabled = enabled; nudIm1.Enabled = enabled; nudIterations1.Enabled = enabled; nudThreshold1.Enabled = enabled; cbThreads1.Enabled = enabled; nudZoom.Enabled = enabled; nudBaseScale.Enabled = enabled; if (nudW != null) nudW.Enabled = enabled; if (nudH != null) nudH.Enabled = enabled; foreach (var cb in paletteCheckBoxes.Where(cb => cb != null)) cb.Enabled = enabled; if (enabled) HandleColorBoxEnableState(); else if (colorBox != null) colorBox.Enabled = false; }; if (this.InvokeRequired) this.Invoke(action); else action(); }
        private async void btnSave_Click_1(object sender, EventArgs e)
        {
            if (isHighResRendering) { MessageBox.Show("Процесс сохранения в высоком разрешении уже запущен.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            int saveWidth = (int)nudW.Value; int saveHeight = (int)nudH.Value;
            if (saveWidth <= 0 || saveHeight <= 0) { MessageBox.Show("Ширина и высота изображения для сохранения должны быть больше 0.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
            string reValueString = nudRe1.Value.ToString("F15", System.Globalization.CultureInfo.InvariantCulture).Replace(".", "_");
            string imValueString = nudIm1.Value.ToString("F15", System.Globalization.CultureInfo.InvariantCulture).Replace(".", "_");
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string suggestedFileName = $"fractal_burningship_re{reValueString}_im{imValueString}_{timestamp}.png";
            using (SaveFileDialog saveDialog = new SaveFileDialog { Filter = "PNG Image|*.png", Title = "Сохранить фрактал Горящий Корабль (Высокое разрешение)", FileName = suggestedFileName })
            {
                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    Button currentActionSaveButton = sender as Button; isHighResRendering = true; if (currentActionSaveButton != null) currentActionSaveButton.Enabled = false; SetMainControlsEnabled(false);
                    if (progressPNG != null) { progressPNG.Value = 0; progressPNG.Visible = true; }
                    try
                    {
                        UpdateParameters();
                        // ИЗМЕНЕНИЕ: Захватываем переменные типа decimal
                        ComplexDecimal currentC_Capture = this.c;
                        int currentMaxIterations_Capture = this.maxIterations; double currentThreshold_Capture = this.threshold;
                        decimal currentZoom_Capture = this.zoom; decimal currentCenterX_Capture = this.centerX; decimal currentCenterY_Capture = this.centerY;
                        int currentThreadCount_Capture = this.threadCount;
                        Bitmap highResBitmap = await Task.Run(() => RenderFractalToBitmap(saveWidth, saveHeight, currentCenterX_Capture, currentCenterY_Capture, currentZoom_Capture, BASE_SCALE, currentC_Capture, currentMaxIterations_Capture, currentThreshold_Capture, currentThreadCount_Capture, progressPercentage => { if (progressPNG != null && progressPNG.IsHandleCreated && !progressPNG.IsDisposed) { try { progressPNG.Invoke((Action)(() => { if (progressPNG.Maximum > 0 && progressPNG.Value <= progressPNG.Maximum) progressPNG.Value = Math.Min(progressPNG.Maximum, progressPercentage); })); } catch (ObjectDisposedException) { } catch (InvalidOperationException) { } } }));
                        highResBitmap.Save(saveDialog.FileName, ImageFormat.Png); highResBitmap.Dispose();
                        MessageBox.Show("Изображение успешно сохранено в высоком разрешении!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex) { MessageBox.Show($"Ошибка при сохранении изображения: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                    finally
                    {
                        isHighResRendering = false; if (currentActionSaveButton != null) currentActionSaveButton.Enabled = true; SetMainControlsEnabled(true);
                        if (progressPNG != null && progressPNG.IsHandleCreated && !progressPNG.IsDisposed) { try { progressPNG.Invoke((Action)(() => { progressPNG.Visible = false; progressPNG.Value = 0; })); } catch (ObjectDisposedException) { } catch (InvalidOperationException) { } }
                    }
                }
            }
        }
        protected override void OnFormClosed(FormClosedEventArgs e) { renderTimer?.Stop(); previewRenderCts?.Cancel(); previewRenderCts?.Dispose(); renderTimer?.Dispose(); mandelbrotCSelectorWindow?.Close(); base.OnFormClosed(e); }
        #endregion
    }
}
// --- END OF FILE FractalburningShipJulia.cs ---