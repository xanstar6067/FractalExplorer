// --- START OF FILE FractalburningShipJulia.cs ---

using System.Drawing.Imaging;
using System.Runtime.InteropServices; // Для использования Marshal.Copy
using System.Globalization; // Для ToString("F...")

namespace FractalDraving
{
    public partial class FractalburningShipJulia : Form, IFractalForm
    {
        private System.Windows.Forms.Timer renderTimer;
        private ComplexDecimal c;
        private int maxIterations;
        private decimal thresholdSquared; // Квадрат порога для оптимизации
        private int threadCount;
        private int width, height;

        private Point panStart;
        private bool panning = false;

        // ИЗМЕНЕНИЕ: Типы полей на decimal для высокой точности представления области
        private decimal zoom = 1.0m;
        private decimal centerX = 0.0m;
        private decimal centerY = 0.0m;
        private const decimal BASE_SCALE = 2.0m; // Базовый размер видимой области по большей стороне (можно настроить)

        private bool isHighResRendering = false;
        private volatile bool isRenderingPreview = false;
        private CancellationTokenSource previewRenderCts;

        private CheckBox[] paletteCheckBoxes;
        private CheckBox lastSelectedPaletteCheckBox = null;

        // Константы для карты выбора 'c' (остаются double, т.к. карта не требует сверхглубины)
        private const double MANDELBROT_MAP_MIN_RE_D = -2.0;
        private const double MANDELBROT_MAP_MAX_RE_D = 1.5;
        private const double MANDELBROT_MAP_MIN_IM_D = -1.5;
        private const double MANDELBROT_MAP_MAX_IM_D = 1.0;
        private const int MANDELBROT_MAP_PREVIEW_ITERATIONS = 75;

        private BurningShipCSelectorForm mandelbrotCSelectorWindow;

        // ИЗМЕНЕНИЕ: Типы полей для отрендеренного состояния на decimal
        private decimal renderedCenterX;
        private decimal renderedCenterY;
        private decimal renderedZoom;

        // LoupeZoom для IFractalForm (возвращает double, как в интерфейсе)
        public double LoupeZoom => (double)nudBaseScale.Value; // nudBaseScale - это scale для карты, не основной zoom
        public event EventHandler LoupeZoomChanged;

        public FractalburningShipJulia()
        {
            InitializeComponent();
            this.Text = "Фрактал Горящий Корабль (Жюлиа) - Decimal Precision";
            // Инициализация полей отрендеренного состояния
            renderedCenterX = centerX;
            renderedCenterY = centerY;
            renderedZoom = zoom;
        }

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

            // Подписки
            nudRe1.ValueChanged += ParamControl_Changed;
            nudIm1.ValueChanged += ParamControl_Changed;
            nudIterations1.ValueChanged += ParamControl_Changed;
            nudThreshold1.ValueChanged += ParamControl_Changed; // Обновит thresholdSquared в UpdateParameters
            cbThreads1.SelectedIndexChanged += ParamControl_Changed;
            nudZoom.ValueChanged += NudZoom_ValueChanged; // Отдельный обработчик для nudZoom
            nudBaseScale.ValueChanged += NudBaseScale_ValueChanged; // Это для карты 'c'

            canvas1.MouseWheel += Canvas_MouseWheel;
            canvas1.MouseDown += Canvas_MouseDown;
            canvas1.MouseMove += Canvas_MouseMove;
            canvas1.MouseUp += Canvas_MouseUp;
            canvas1.Paint += Canvas_Paint;

            if (mandelbrotCanvas1 != null)
            {
                mandelbrotCanvas1.Click += mandelbrotCanvas_Click;
                mandelbrotCanvas1.Paint += mandelbrotCanvas_Paint;
                await Task.Run(() => RenderAndDisplayMandelbrotMap());
            }

            int cores = Environment.ProcessorCount;
            for (int i = 1; i <= cores; i++) cbThreads1.Items.Add(i);
            cbThreads1.Items.Add("Auto");
            cbThreads1.SelectedItem = "Auto";

            // Настройка NumericUpDown для параметров 'c'
            nudRe1.Minimum = -2.5m; nudRe1.Maximum = 2.5m;
            nudRe1.DecimalPlaces = 28; // Максимальная точность для decimal
            nudRe1.Increment = 0.000000000000000000000000001m;
            nudRe1.Value = -0.74543m; // Пример для Жюлиа

            nudIm1.Minimum = -2.5m; nudIm1.Maximum = 2.5m;
            nudIm1.DecimalPlaces = 28;
            nudIm1.Increment = 0.000000000000000000000000001m;
            nudIm1.Value = 0.11301m; // Пример для Жюлиа

            nudIterations1.Minimum = 50; nudIterations1.Maximum = 200000;
            nudIterations1.Value = 600;

            nudThreshold1.Minimum = 2m; nudThreshold1.Maximum = 10000m; // Порог (не квадрат)
            nudThreshold1.DecimalPlaces = 2; nudThreshold1.Increment = 0.1m;
            nudThreshold1.Value = 2.0m; // thresholdSquared будет 4.0m

            // Настройка NumericUpDown для zoom (теперь управляет decimal zoom)
            nudZoom.DecimalPlaces = 2; // Отображение для пользователя, реальная точность в `this.zoom`
            nudZoom.Increment = 0.1m;
            // Для decimal zoom, min/max должны быть другие. Zoom - это множитель увеличения.
            // Если zoom = 1, BASE_SCALE виден. Если zoom = 1000, видим в 1000 раз меньшую область.
            nudZoom.Minimum = 1m; // Минимальный зум - 1x
            nudZoom.Maximum = 1E+28m; // Огромный зум
            nudZoom.Value = this.zoom;

            // nudBaseScale - для карты 'c' (LoupeZoom)
            nudBaseScale.Minimum = 1m; nudBaseScale.Maximum = 10m;
            nudBaseScale.DecimalPlaces = 1; nudBaseScale.Increment = 0.1m;
            nudBaseScale.Value = 4m; // Типичное значение для "лупы" в селекторе 'c'

            this.Resize += Form1_Resize;
            canvas1.Resize += Canvas_Resize;

            HandleColorBoxEnableState();
            ScheduleRender();
        }

        private void NudZoom_ValueChanged(object sender, EventArgs e)
        {
            if (isHighResRendering) return;
            decimal newZoomValue = nudZoom.Value; // nudZoom.Value уже decimal
            if (newZoomValue < nudZoom.Minimum) newZoomValue = nudZoom.Minimum;
            if (newZoomValue > nudZoom.Maximum) newZoomValue = nudZoom.Maximum;

            if (this.zoom != newZoomValue)
            {
                this.zoom = newZoomValue;
                if (nudZoom.Value != newZoomValue) // Синхронизация, если значение было обрезано
                {
                    nudZoom.Value = newZoomValue;
                }
                // Центр остается тем же, меняется только масштаб отображения
                ScheduleRender();
            }
        }

        // Canvas_Paint рисует уже существующий битмап с учетом текущего decimal zoom/center
        // относительно отрендеренного decimal zoom/center.
        private void Canvas_Paint(object sender, PaintEventArgs e)
        {
            if (canvas1.Image == null || width <= 0 || height <= 0 || this.zoom <= 0 || this.renderedZoom <= 0)
            {
                e.Graphics.Clear(Color.Black);
                if (canvas1.Image != null && (this.zoom <= 0 || this.renderedZoom <= 0))
                    e.Graphics.DrawImageUnscaled(canvas1.Image, Point.Empty);
                return;
            }

            // Все расчеты для позиционирования битмапа должны быть на decimal,
            // затем конвертированы в float для Graphics API.

            decimal viewWidthCurrent = BASE_SCALE / this.zoom;
            decimal viewHeightCurrent = viewWidthCurrent * ((decimal)height / width);

            decimal viewWidthRendered = BASE_SCALE / this.renderedZoom;
            decimal viewHeightRendered = viewWidthRendered * ((decimal)height / width);

            // Верхний левый угол текущего вида в комплексной плоскости
            decimal currentViewReMin = this.centerX - viewWidthCurrent / 2m;
            decimal currentViewImMax = this.centerY + viewHeightCurrent / 2m; // Y растет вверх в комплексной

            // Верхний левый угол отрендеренного изображения в комплексной плоскости
            decimal renderedImageReMin = this.renderedCenterX - viewWidthRendered / 2m;
            decimal renderedImageImMax = this.renderedCenterY + viewHeightRendered / 2m;

            // Разница в комплексных координатах
            decimal deltaRe = renderedImageReMin - currentViewReMin;
            decimal deltaIm = renderedImageImMax - currentViewImMax; // renderedY - currentY

            // Преобразование разницы в пиксели текущего вида
            float drawX = (float)(deltaRe / viewWidthCurrent * width);
            float drawY = (float)(-deltaIm / viewHeightCurrent * height); // -deltaIm, т.к. Y экрана вниз

            // Масштаб для отрисовки изображения
            float scaleFactor = (float)(viewWidthRendered / viewWidthCurrent);
            // float scaleFactor = (float)(this.zoom / this.renderedZoom); // Эквивалентно

            float drawWidth = width * scaleFactor;
            float drawHeight = height * scaleFactor;

            e.Graphics.Clear(Color.Black);
            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;

            if (drawWidth > 0 && drawHeight > 0 && canvas1.Image != null)
            {
                try
                {
                    e.Graphics.DrawImage(canvas1.Image, drawX, drawY, drawWidth, drawHeight);
                }
                catch (OverflowException) // Слишком большие значения float
                {
                    e.Graphics.DrawImageUnscaled(canvas1.Image, Point.Empty);
                }
                catch (ArgumentException)
                {
                    e.Graphics.DrawImageUnscaled(canvas1.Image, Point.Empty);
                }
            }
            else if (canvas1.Image != null)
            {
                e.Graphics.DrawImageUnscaled(canvas1.Image, Point.Empty);
            }
        }

        private void NudBaseScale_ValueChanged(object sender, EventArgs e)
        {
            LoupeZoomChanged?.Invoke(this, EventArgs.Empty);
            // Это BaseScale для карты 'c', не влияет на основной рендер напрямую.
            // Если бы он влиял, нужно было бы ScheduleRender().
        }

        private void RenderAndDisplayMandelbrotMap()
        {
            if (mandelbrotCanvas1 == null || mandelbrotCanvas1.Width <= 0 || mandelbrotCanvas1.Height <= 0) return;
            Bitmap mandelbrotImage = RenderMandelbrotMapInternal(mandelbrotCanvas1.Width, mandelbrotCanvas1.Height, MANDELBROT_MAP_PREVIEW_ITERATIONS);
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

        // Рендеринг карты для выбора 'c' (Burning Ship Mandelbrot-like)
        private Bitmap RenderMandelbrotMapInternal(int mapCanvasWidth, int mapCanvasHeight, int iterationsLimit)
        {
            Bitmap bmp = new Bitmap(mapCanvasWidth, mapCanvasHeight, PixelFormat.Format24bppRgb);
            if (mapCanvasWidth <= 0 || mapCanvasHeight <= 0) return bmp;

            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, mapCanvasWidth, mapCanvasHeight), ImageLockMode.WriteOnly, bmp.PixelFormat);
            int stride = bmpData.Stride;
            IntPtr scan0 = bmpData.Scan0;
            int bytes = Math.Abs(stride) * mapCanvasHeight;
            byte[] buffer = new byte[bytes];

            // Границы для карты (double достаточно)
            double reRange = MANDELBROT_MAP_MAX_RE_D - MANDELBROT_MAP_MIN_RE_D;
            double imRange = MANDELBROT_MAP_MAX_IM_D - MANDELBROT_MAP_MIN_IM_D;

            decimal mapThresholdSquared = 4m; // (2*2)

            Parallel.For(0, mapCanvasHeight, y_coord =>
            {
                int rowOffset = y_coord * stride;
                for (int x_coord = 0; x_coord < mapCanvasWidth; x_coord++)
                {
                    // Координаты 'c' для карты вычисляются через double
                    double c_re_d = MANDELBROT_MAP_MIN_RE_D + (x_coord / (double)mapCanvasWidth) * reRange;
                    double c_im_d = MANDELBROT_MAP_MAX_IM_D - (y_coord / (double)mapCanvasHeight) * imRange; // Y инвертирована

                    ComplexDecimal c0_map = new ComplexDecimal((decimal)c_re_d, (decimal)c_im_d);
                    ComplexDecimal z_map = ComplexDecimal.Zero;
                    int iter = 0;

                    // Итерации Burning Ship (Mandelbrot set style)
                    while (iter < iterationsLimit && z_map.MagnitudeSquared < mapThresholdSquared)
                    {
                        z_map = new ComplexDecimal(Math.Abs(z_map.Real), Math.Abs(z_map.Imaginary));
                        z_map = z_map * z_map + c0_map;
                        iter++;
                    }

                    // Простое окрашивание для карты
                    byte r_val, g_val, b_val;
                    if (iter == iterationsLimit) { r_val = g_val = b_val = 0; } // Внутри множества
                    else // Снаружи
                    {
                        double t = (double)iter / iterationsLimit;
                        r_val = (byte)(Math.Min(255, 50 + t * 205));
                        g_val = (byte)(Math.Min(255, 100 + t * 155));
                        b_val = (byte)(Math.Min(255, 150 + t * 105));
                    }
                    int index = rowOffset + x_coord * 3;
                    buffer[index] = b_val; buffer[index + 1] = g_val; buffer[index + 2] = r_val;
                }
            });

            Marshal.Copy(buffer, 0, scan0, bytes);
            bmp.UnlockBits(bmpData);
            return bmp;
        }

        private void UpdateParameters()
        {
            c = new ComplexDecimal(nudRe1.Value, nudIm1.Value);
            maxIterations = (int)nudIterations1.Value;
            decimal thresholdVal = nudThreshold1.Value;
            thresholdSquared = thresholdVal * thresholdVal;
            threadCount = cbThreads1.SelectedItem.ToString() == "Auto" ? Environment.ProcessorCount : Convert.ToInt32(cbThreads1.SelectedItem);
        }

        // Основной рендеринг фрактала Жюлиа
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
                const int currentMaxColorIter = 1000; // Для окраски

                // Расчет геометрии видимой области с decimal
                // BASE_SCALE определяет ширину видимой области при zoom=1.
                // Высота подстраивается под соотношение сторон холста.
                decimal currentViewWidth = BASE_SCALE / renderZoom;
                decimal currentViewHeight = currentViewWidth * ((decimal)height / width);

                // Смещение для верхнего левого угла (0,0) холста
                decimal reOffset = renderCenterX - currentViewWidth / 2m;
                decimal imOffset = renderCenterY - currentViewHeight / 2m; // Нижняя граница Y

                // Размер одного пикселя в комплексной плоскости
                decimal pixelWidthComplex = currentViewWidth / width;
                decimal pixelHeightComplex = currentViewHeight / height;

                Parallel.For(0, height, po, y_coord =>
                {
                    if (token.IsCancellationRequested) return;
                    int rowOffset = y_coord * stride;
                    for (int x_coord = 0; x_coord < width; x_coord++)
                    {
                        // Координаты центра пикселя (x_coord + 0.5, y_coord + 0.5)
                        // Y-координата инвертируется: (height - 1 - y_coord) для преобразования экранных Y в математические Y
                        decimal re_dec = reOffset + (x_coord + 0.5m) * pixelWidthComplex;
                        decimal im_dec = imOffset + ((height - 1 - y_coord) + 0.5m) * pixelHeightComplex;

                        ComplexDecimal z_val = new ComplexDecimal(re_dec, im_dec);
                        int iter_val = 0;

                        // Итерации для Жюлиа "Горящего Корабля"
                        while (iter_val < maxIterations && z_val.MagnitudeSquared <= thresholdSquared)
                        {
                            z_val = new ComplexDecimal(Math.Abs(z_val.Real), Math.Abs(z_val.Imaginary));
                            z_val = z_val * z_val + this.c; // Используем this.c
                            iter_val++;
                        }

                        Color pixelColor = GetPixelColor(iter_val, maxIterations, currentMaxColorIter);
                        int index = rowOffset + x_coord * 3; // BGR
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
                bmpData = null; // Важно для finally
                token.ThrowIfCancellationRequested();

                if (canvas1.IsHandleCreated && !canvas1.IsDisposed)
                {
                    Bitmap oldImage = null;
                    canvas1.Invoke((Action)(() =>
                    {
                        if (token.IsCancellationRequested) { bmp?.Dispose(); return; }
                        oldImage = canvas1.Image as Bitmap;
                        canvas1.Image = bmp;
                        // Сохраняем параметры, с которыми был сделан этот рендер
                        this.renderedCenterX = renderCenterX;
                        this.renderedCenterY = renderCenterY;
                        this.renderedZoom = renderZoom;
                        bmp = null; // Владение передано
                    }));
                    oldImage?.Dispose();
                }
                else { bmp?.Dispose(); }
            }
            finally
            {
                if (bmpData != null && bmp != null) { try { bmp.UnlockBits(bmpData); } catch { /* ignore */ } }
                if (bmp != null) bmp.Dispose(); // Если не был передан или произошла ошибка
            }
        }

        // Рендеринг для сохранения в файл (аналогичен RenderFractal, но с параметрами)
        private Bitmap RenderFractalToBitmap(
            int customRenderWidth, int customRenderHeight,
            decimal currentRenderCenterX, decimal currentRenderCenterY, decimal currentRenderZoom,
            decimal currentRenderBaseScale, // BASE_SCALE для этого рендера
            ComplexDecimal cParamForJulia, // Параметр 'c' для Жюлиа
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
                    decimal re_dec = reOffset + (x_coord + 0.5m) * pixelWidthComplex;
                    decimal im_dec = imOffset + ((customRenderHeight - 1 - y_coord) + 0.5m) * pixelHeightComplex;

                    ComplexDecimal z_val = new ComplexDecimal(re_dec, im_dec);
                    int iter_val = 0;

                    while (iter_val < currentRenderMaxIterations && z_val.MagnitudeSquared <= currentRenderThresholdSquared)
                    {
                        z_val = new ComplexDecimal(Math.Abs(z_val.Real), Math.Abs(z_val.Imaginary));
                        z_val = z_val * z_val + cParamForJulia;
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

        // Обработчики мыши для панорамирования и масштабирования
        private void Canvas_MouseWheel(object sender, MouseEventArgs e)
        {
            if (isHighResRendering) return;

            decimal zoomFactor = e.Delta > 0 ? 0.8m : 1.25m; // 0.8 для увеличения (уменьшаем видимую область), 1.25 для уменьшения
                                                             // this.zoom это множитель увеличения. Больше zoom = глубже.
                                                             // Значит, если Delta > 0, this.zoom должен УВЕЛИЧИВАТЬСЯ.
            zoomFactor = e.Delta > 0 ? 1.25m : 0.8m;


            decimal oldZoom = this.zoom;

            // Координаты мыши в пикселях холста
            decimal mouseCanvasX = e.X;
            decimal mouseCanvasY = e.Y;

            // Текущий размер видимой области в комплексных числах ДО масштабирования
            decimal viewWidthBefore = BASE_SCALE / oldZoom;
            decimal viewHeightBefore = viewWidthBefore * ((decimal)height / width);
            decimal reOffsetBefore = this.centerX - viewWidthBefore / 2m;
            decimal imOffsetBefore = this.centerY - viewHeightBefore / 2m; // Нижняя граница Y
            decimal pixelWidthComplexBefore = viewWidthBefore / width;
            decimal pixelHeightComplexBefore = viewHeightBefore / height;

            // Комплексные координаты точки под курсором
            decimal mouseRe = reOffsetBefore + (mouseCanvasX + 0.5m) * pixelWidthComplexBefore;
            decimal mouseIm = imOffsetBefore + ((height - 1 - mouseCanvasY) + 0.5m) * pixelHeightComplexBefore;

            // Новый зум
            decimal newZoom = oldZoom * zoomFactor;
            if (newZoom < nudZoom.Minimum) newZoom = nudZoom.Minimum; // Используем свойства NUD для границ
            if (newZoom > nudZoom.Maximum) newZoom = nudZoom.Maximum;
            this.zoom = newZoom;

            // Новый размер видимой области ПОСЛЕ масштабирования
            decimal viewWidthAfter = BASE_SCALE / this.zoom;
            decimal viewHeightAfter = viewWidthAfter * ((decimal)height / width);
            decimal pixelWidthComplexAfter = viewWidthAfter / width;
            decimal pixelHeightComplexAfter = viewHeightAfter / height;

            // Новый центр, чтобы точка под курсором осталась на месте
            this.centerX = mouseRe - ((mouseCanvasX + 0.5m) * pixelWidthComplexAfter - viewWidthAfter / 2m);
            this.centerY = mouseIm - (((height - 1 - mouseCanvasY) + 0.5m) * pixelHeightComplexAfter - viewHeightAfter / 2m);

            canvas1.Invalidate(); // Запрос на перерисовку (использует Canvas_Paint)

            if (nudZoom.Value != this.zoom) // Обновить значение в NUD
            {
                // Убедимся, что значение в допустимых пределах NUD перед присвоением
                decimal displayZoom = this.zoom;
                if (displayZoom < nudZoom.Minimum) displayZoom = nudZoom.Minimum;
                if (displayZoom > nudZoom.Maximum) displayZoom = nudZoom.Maximum;
                nudZoom.Value = displayZoom; // Это вызовет NudZoom_ValueChanged -> ScheduleRender
            }
            else
            {
                ScheduleRender(); // Если значение NUD не изменилось (например, уже на границе)
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

            decimal currentViewWidth = BASE_SCALE / this.zoom;
            //decimal currentViewHeight = currentViewWidth * ((decimal)height / width); // Не нужен для pixelSize

            decimal pixelSizeComplex; // Размер одного пикселя по X (или Y, если нормировать)
            if (width >= height) // Если холст шире или квадратный
                pixelSizeComplex = currentViewWidth / width;
            else // Если холст выше
                pixelSizeComplex = (currentViewWidth * ((decimal)height / width)) / height; // Размер по Y нормированный к ширине
                                                                                            // Это эквивалентно (BASE_SCALE / this.zoom * aspect_ratio_h_w) / height
                                                                                            // Проще: (BASE_SCALE / this.zoom) / width, если width > height
                                                                                            // (BASE_SCALE / this.zoom * ( (decimal)height / width ) ) / height

            // Для простоты и консистентности, используем размер пикселя по X
            pixelSizeComplex = currentViewWidth / width;


            decimal deltaX = e.X - panStart.X;
            decimal deltaY = e.Y - panStart.Y; // Y в пикселях экрана идет вниз

            this.centerX -= deltaX * pixelSizeComplex;
            this.centerY += deltaY * pixelSizeComplex; // Y в комплексной плоскости идет вверх, поэтому + (экранный Y инвертирован)

            panStart = e.Location;
            canvas1.Invalidate(); // Для Canvas_Paint
            ScheduleRender();     // Для полного рендера
        }

        private void Canvas_MouseUp(object sender, MouseEventArgs e)
        {
            if (isHighResRendering) return;
            if (e.Button == MouseButtons.Left)
            {
                panning = false;
            }
        }

        // Сохранение PNG высокого разрешения
        private async void btnSave_Click_1(object sender, EventArgs e)
        {
            if (isHighResRendering) { MessageBox.Show("Процесс сохранения уже запущен.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }

            int saveWidth = (int)nudW.Value;
            int saveHeight = (int)nudH.Value;
            if (saveWidth <= 0 || saveHeight <= 0) { MessageBox.Show("Ширина и высота должны быть больше 0.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }

            string reValStr = nudRe1.Value.ToString("F28", CultureInfo.InvariantCulture).Replace(".", "_");
            string imValStr = nudIm1.Value.ToString("F28", CultureInfo.InvariantCulture).Replace(".", "_");
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string suggestedFileName = $"Julia_BS_re{reValStr}_im{imValStr}_{timestamp}.png";

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
                        UpdateParameters(); // Убедиться, что this.c, iterations, thresholdSquared актуальны

                        // Захват текущих параметров (уже decimal где нужно)
                        ComplexDecimal cForSave = this.c;
                        int maxIterSave = this.maxIterations;
                        decimal thresholdSqSave = this.thresholdSquared;
                        decimal zoomSave = this.zoom; // Текущий зум окна
                        decimal centerXSave = this.centerX; // Текущий центр окна
                        decimal centerYSave = this.centerY;
                        int threadsSave = this.threadCount;
                        decimal baseScaleSave = BASE_SCALE; // Используем текущий BASE_SCALE

                        Bitmap highResBitmap = await Task.Run(() => RenderFractalToBitmap(
                            saveWidth, saveHeight,
                            centerXSave, centerYSave, zoomSave,
                            baseScaleSave, cForSave,
                            maxIterSave, thresholdSqSave,
                            threadsSave,
                            progressPercentage =>
                            {
                                if (progressPNG != null && progressPNG.IsHandleCreated && !progressPNG.IsDisposed)
                                {
                                    try { progressPNG.Invoke((Action)(() => { if (progressPNG.IsHandleCreated && !progressPNG.IsDisposed) progressPNG.Value = Math.Min(progressPNG.Maximum, progressPercentage); })); }
                                    catch (ObjectDisposedException) { }
                                    catch (InvalidOperationException) { }
                                }
                            }
                        ));

                        highResBitmap.Save(saveDialog.FileName, ImageFormat.Png);
                        highResBitmap.Dispose();
                        MessageBox.Show("Изображение успешно сохранено!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex) { MessageBox.Show($"Ошибка при сохранении: {ex.Message}\n{ex.StackTrace}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                    finally
                    {
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

        #region Остальные методы (UI, Палитры, Жизненный цикл) - в основном без изменений от предыдущей decimal-версии

        private void ParamControl_Changed(object sender, EventArgs e) // Обрабатывает nudRe1, Im1, Iterations, Threshold, Threads
        {
            if (isHighResRendering) return;
            // nudZoom обрабатывается отдельно в NudZoom_ValueChanged
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
            if (isRenderingPreview) { renderTimer.Start(); return; } // Если уже рендерится, перезапустить таймер

            isRenderingPreview = true;
            previewRenderCts?.Dispose();
            previewRenderCts = new CancellationTokenSource();
            CancellationToken token = previewRenderCts.Token;

            UpdateParameters(); // Обновляет this.c, this.maxIterations, this.thresholdSquared

            // Захватываем текущие значения (уже decimal)
            decimal currentRenderCenterX = this.centerX;
            decimal currentRenderCenterY = this.centerY;
            decimal currentRenderZoom = this.zoom;

            try
            {
                await Task.Run(() => RenderFractal(token, currentRenderCenterX, currentRenderCenterY, currentRenderZoom), token);
            }
            catch (OperationCanceledException) { /* Ожидаемо при отмене */ }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"RenderTimer_Tick Error: {ex}"); /* Можно логировать */ }
            finally { isRenderingPreview = false; }
        }

        // --- Палитры ---
        private delegate Color PaletteFunctionDelegate(double t, int iter, int maxIterations, int maxColorIter);
        private Color GetPixelColor(int iter, int currentMaxIterations, int currentMaxColorIter)
        {
            if (iter == currentMaxIterations) return (lastSelectedPaletteCheckBox?.Name == "checkBox6") ? Color.FromArgb(50, 50, 50) : Color.Black;
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
            }
            double t_param = (selectedPaletteFunc == GetDefaultPaletteColor) ? t_log : t_capped;
            return selectedPaletteFunc(t_param, iter, currentMaxIterations, currentMaxColorIter);
        }
        private Color GetDefaultPaletteColor(double t_log, int iter, int maxIter, int maxClrIter) { int cVal = (int)(255.0 * (1 - t_log)); return Color.FromArgb(cVal, cVal, cVal); }
        private Color GetPaletteColorBoxColor(double t_capped, int iter, int maxIter, int maxClrIter) { return ColorFromHSV(360.0 * t_capped, 0.8, 0.9); } // Немного насыщеннее
        private Color GetPaletteOldBWColor(double t_capped, int iter, int maxIter, int maxClrIter) { int cVal = 255 - (int)(255.0 * t_capped); return Color.FromArgb(cVal, cVal, cVal); }
        private Color LerpColor(Color a, Color b, double t) { t = Math.Max(0, Math.Min(1, t)); return Color.FromArgb((int)(a.R + (b.R - a.R) * t), (int)(a.G + (b.G - a.G) * t), (int)(a.B + (b.B - a.B) * t)); }
        private Color GetPalette1Color(double t, int iter, int maxIter, int maxClrIter) { Color c1 = Color.Black, c2 = Color.FromArgb(200, 0, 0), c3 = Color.FromArgb(255, 100, 0), c4 = Color.FromArgb(255, 255, 100), c5 = Color.White; if (t < 0.25) return LerpColor(c1, c2, t / 0.25); if (t < 0.50) return LerpColor(c2, c3, (t - 0.25) / 0.25); if (t < 0.75) return LerpColor(c3, c4, (t - 0.50) / 0.25); return LerpColor(c4, c5, (t - 0.75) / 0.25); }
        private Color GetPalette2Color(double t, int iter, int maxIter, int maxClrIter) { Color c1 = Color.Black, c2 = Color.FromArgb(0, 0, 100), c3 = Color.FromArgb(0, 120, 200), c4 = Color.FromArgb(170, 220, 255), c5 = Color.White; if (t < 0.25) return LerpColor(c1, c2, t / 0.25); if (t < 0.50) return LerpColor(c2, c3, (t - 0.25) / 0.25); if (t < 0.75) return LerpColor(c3, c4, (t - 0.50) / 0.25); return LerpColor(c4, c5, (t - 0.75) / 0.25); }
        private Color GetPalette3Color(double t, int iter, int maxIter, int maxClrIter) { double r = Math.Sin(t * Math.PI * 3.0 + 0.5) * 0.45 + 0.5, g = Math.Sin(t * Math.PI * 3.0 + Math.PI * 2.0 / 3.0 + 0.5) * 0.45 + 0.5, b = Math.Sin(t * Math.PI * 3.0 + Math.PI * 4.0 / 3.0 + 0.5) * 0.45 + 0.5; return Color.FromArgb((int)(r * 255), (int)(g * 255), (int)(b * 255)); }
        private Color GetPalette4Color(double t, int iter, int maxIter, int maxClrIter) { Color c1 = Color.FromArgb(10, 0, 20), c2 = Color.FromArgb(255, 0, 255), c3 = Color.FromArgb(0, 255, 255), c4 = Color.FromArgb(230, 230, 250); if (t < 0.1) return LerpColor(c1, c2, t / 0.1); if (t < 0.4) return LerpColor(c2, c1, (t - 0.1) / 0.3); if (t < 0.5) return LerpColor(c1, c3, (t - 0.4) / 0.1); if (t < 0.8) return LerpColor(c3, c1, (t - 0.5) / 0.3); return LerpColor(c1, c4, (t - 0.8) / 0.2); }
        private Color GetPalette5Color(double t, int iter, int maxIter, int maxClrIter) { int gVal = 50 + (int)(t * 150); double shine = Math.Sin(t * Math.PI * 5); int finalG = Math.Max(0, Math.Min(255, gVal + (int)(shine * 40))); return Color.FromArgb(finalG, finalG, Math.Min(255, finalG + (int)(t * 25))); }
        private Color GetPalette6Color(double t, int iter, int maxIter, int maxClrIter) { double hue = (t * 200.0 + 180.0) % 360.0, sat = Math.Max(0.2, Math.Min(0.6, 0.35 + (Math.Sin(t * Math.PI * 2) * 0.1))), val = Math.Max(0.7, Math.Min(0.95, 0.80 + (Math.Cos(t * Math.PI * 2.5) * 0.15))); return ColorFromHSV(hue, sat, val); }
        private Color ColorFromHSV(double hue, double saturation, double value) { hue = (hue % 360 + 360) % 360; int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6; double f = hue / 60 - Math.Floor(hue / 60); value = Math.Max(0, Math.Min(1, value)); saturation = Math.Max(0, Math.Min(1, saturation)); int v_comp = Convert.ToInt32(value * 255); int p_comp = Convert.ToInt32(v_comp * (1 - saturation)); int q_comp = Convert.ToInt32(v_comp * (1 - f * saturation)); int t_comp = Convert.ToInt32(v_comp * (1 - (1 - f) * saturation)); switch (hi) { case 0: return Color.FromArgb(v_comp, t_comp, p_comp); case 1: return Color.FromArgb(q_comp, v_comp, p_comp); case 2: return Color.FromArgb(p_comp, v_comp, t_comp); case 3: return Color.FromArgb(p_comp, q_comp, v_comp); case 4: return Color.FromArgb(t_comp, p_comp, v_comp); default: return Color.FromArgb(v_comp, p_comp, q_comp); } }
        // --- Конец палитр ---

        // Обработчики UI для карты 'c'
        private void MandelbrotCSelectorWindow_CoordinatesSelected(double re, double im) // double, т.к. селектор не требует сверхточности
        {
            decimal re_dec = (decimal)Math.Max((double)nudRe1.Minimum, Math.Min((double)nudRe1.Maximum, re)); // Приводим к decimal для nud
            decimal im_dec = (decimal)Math.Max((double)nudIm1.Minimum, Math.Min((double)nudIm1.Maximum, im));
            bool changed = false;
            if (nudRe1.Value != re_dec) { nudRe1.Value = re_dec; changed = true; }
            if (nudIm1.Value != im_dec) { nudIm1.Value = im_dec; changed = true; }
            if (changed && mandelbrotCanvas1 != null && mandelbrotCanvas1.IsHandleCreated && !mandelbrotCanvas1.IsDisposed)
            {
                mandelbrotCanvas1.Invoke((Action)(() => mandelbrotCanvas1.Invalidate())); // Перерисовать маркер на карте
            }
        }
        private void mandelbrotCanvas_Paint(object sender, PaintEventArgs e) // Карта 'c' использует double
        {
            if (mandelbrotCanvas1 == null || mandelbrotCanvas1.Width <= 0 || mandelbrotCanvas1.Height <= 0) return;
            double reRange = MANDELBROT_MAP_MAX_RE_D - MANDELBROT_MAP_MIN_RE_D;
            double imRange = MANDELBROT_MAP_MAX_IM_D - MANDELBROT_MAP_MIN_IM_D;
            double currentCRe = (double)nudRe1.Value; // nudRe1.Value is decimal, cast to double for map
            double currentCIm = (double)nudIm1.Value;
            if (currentCRe >= MANDELBROT_MAP_MIN_RE_D && currentCRe <= MANDELBROT_MAP_MAX_RE_D &&
                currentCIm >= MANDELBROT_MAP_MIN_IM_D && currentCIm <= MANDELBROT_MAP_MAX_IM_D)
            {
                int markerX = (int)((currentCRe - MANDELBROT_MAP_MIN_RE_D) / reRange * mandelbrotCanvas1.Width);
                int markerY = (int)((MANDELBROT_MAP_MAX_IM_D - currentCIm) / imRange * mandelbrotCanvas1.Height); // Y инвертирована
                using (Pen markerPen = new Pen(Color.FromArgb(200, Color.LimeGreen), 1.5f)) // Ярче маркер
                {
                    e.Graphics.DrawLine(markerPen, 0, markerY, mandelbrotCanvas1.Width, markerY);
                    e.Graphics.DrawLine(markerPen, markerX, 0, markerX, mandelbrotCanvas1.Height);
                }
            }
        }
        private void mandelbrotCanvas_Click(object sender, EventArgs e)
        {
            // ... (логика открытия окна BurningShipCSelectorForm, передает double re/im)
            if (mandelbrotCanvas1 == null || mandelbrotCanvas1.Width <= 0 || mandelbrotCanvas1.Height <= 0) return;
            MouseEventArgs mouseArgs = e as MouseEventArgs; // Если клик был не мышью, а, например, программный
            if (mouseArgs == null)
            {
                Point clientPoint = mandelbrotCanvas1.PointToClient(Control.MousePosition);
                if ((Control.MouseButtons & MouseButtons.Left) != MouseButtons.Left) return; // Только левая кнопка
                mouseArgs = new MouseEventArgs(MouseButtons.Left, 1, clientPoint.X, clientPoint.Y, 0);
            }
            if (mouseArgs.Button != MouseButtons.Left) return;

            double initialRe = (double)nudRe1.Value; // Для передачи в селектор 'c'
            double initialIm = (double)nudIm1.Value;

            if (mandelbrotCSelectorWindow == null || mandelbrotCSelectorWindow.IsDisposed)
            {
                mandelbrotCSelectorWindow = new BurningShipCSelectorForm(this, initialRe, initialIm);
                mandelbrotCSelectorWindow.CoordinatesSelected += MandelbrotCSelectorWindow_CoordinatesSelected;
                mandelbrotCSelectorWindow.FormClosed += (s, args) => { mandelbrotCSelectorWindow = null; };
                mandelbrotCSelectorWindow.Show(this.Owner ?? this); // Показать относительно владельца или главной формы
            }
            else
            {
                mandelbrotCSelectorWindow.Activate();
                mandelbrotCSelectorWindow.SetSelectedCoordinates(initialRe, initialIm, true); // Обновить координаты в открытом окне
            }
            if (mandelbrotCanvas1.IsHandleCreated && !mandelbrotCanvas1.IsDisposed)
            {
                mandelbrotCanvas1.Invalidate(); // Обновить маркер
            }
        }

        // Управление состоянием чекбоксов палитр
        private void PaletteCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox currentCb = sender as CheckBox; if (currentCb == null) return;
            // Временно отписываемся, чтобы избежать рекурсивных вызовов
            foreach (var cb in paletteCheckBoxes.Where(cb => cb != null)) cb.CheckedChanged -= PaletteCheckBox_CheckedChanged;

            if (currentCb.Checked)
            {
                lastSelectedPaletteCheckBox = currentCb;
                // Снимаем галочки со всех остальных
                foreach (var cb in paletteCheckBoxes.Where(cb => cb != null && cb != currentCb)) cb.Checked = false;
            }
            else
            {
                // Если пользователь снял галочку с активного, и больше ни один не выбран,
                // lastSelectedPaletteCheckBox станет null (или можно выбрать дефолтный).
                // Если какой-то другой остался выбранным (маловероятно при такой логике), он будет lastSelected.
                lastSelectedPaletteCheckBox = paletteCheckBoxes.FirstOrDefault(cb => cb != null && cb.Checked);
            }

            // Подписываемся обратно
            foreach (var cb in paletteCheckBoxes.Where(cb => cb != null)) cb.CheckedChanged += PaletteCheckBox_CheckedChanged;

            HandleColorBoxEnableState();
            ScheduleRender();
        }
        private void HandleColorBoxEnableState()
        {
            if (paletteCheckBoxes == null || colorBox == null || oldRenderBW == null) return;
            bool isAnyNewPaletteCbChecked = paletteCheckBoxes.Skip(2).Any(cb => cb != null && cb.Checked); // Пропускаем colorBox и oldRenderBW
            if (isAnyNewPaletteCbChecked) colorBox.Enabled = true; // Если выбрана одна из "новых" палитр 1-6
            else if (colorBox.Checked && !oldRenderBW.Checked) colorBox.Enabled = true; // Если выбран colorBox и не Ч/Б
            else colorBox.Enabled = !oldRenderBW.Checked; // colorBox активен, если Ч/Б не выбран
        }

        // Изменение размеров
        private void Form1_Resize(object sender, EventArgs e) => ResizeCanvas();
        private void Canvas_Resize(object sender, EventArgs e) => ResizeCanvas();
        private void ResizeCanvas()
        {
            if (isHighResRendering) return;
            if (canvas1.Width <= 0 || canvas1.Height <= 0) return; // Предотвращение ошибок при сворачивании
            width = canvas1.Width;
            height = canvas1.Height;
            ScheduleRender(); // Перерисовать с новыми размерами
        }

        // Кнопка "Сохранить текущий вид" (не высокое разрешение)
        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (isHighResRendering) { MessageBox.Show("Идет сохранение в высоком разрешении.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            using (var dlg = new SaveFileDialog { Filter = "PNG Image|*.png", FileName = $"Julia_BS_preview_{DateTime.Now:yyyyMMdd_HHmmss}.png" })
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    if (canvas1.Image != null) canvas1.Image.Save(dlg.FileName, ImageFormat.Png);
                    else MessageBox.Show("Нет изображения для сохранения!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // Кнопка "Перерисовать"
        private void btnRender_Click(object sender, EventArgs e) // Убедитесь, что имя кнопки совпадает (btnRender1?)
        {
            if (isHighResRendering) return;
            ScheduleRender();
        }

        // Управление доступностью контролов
        private void SetMainControlsEnabled(bool enabled)
        {
            Action action = () =>
            {
                // Убедитесь, что имена контролов совпадают с вашим дизайнером
                Control btnRenderControl = Controls.Find("btnRender1", true).FirstOrDefault() ?? Controls.Find("btnRender", true).FirstOrDefault();
                if (btnRenderControl != null) btnRenderControl.Enabled = enabled;

                nudRe1.Enabled = enabled; nudIm1.Enabled = enabled;
                nudIterations1.Enabled = enabled; nudThreshold1.Enabled = enabled;
                cbThreads1.Enabled = enabled; nudZoom.Enabled = enabled;
                nudBaseScale.Enabled = enabled; // Для карты 'c'

                Control nudWControl = Controls.Find("nudW", true).FirstOrDefault();
                if (nudWControl != null) nudWControl.Enabled = enabled;
                Control nudHControl = Controls.Find("nudH", true).FirstOrDefault();
                if (nudHControl != null) nudHControl.Enabled = enabled;

                foreach (var cb in paletteCheckBoxes.Where(cb => cb != null)) cb.Enabled = enabled;
                if (enabled) HandleColorBoxEnableState();
                else if (colorBox != null) colorBox.Enabled = false;
            };
            if (this.InvokeRequired) this.Invoke(action); else action();
        }

        // Закрытие формы
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            renderTimer?.Stop(); renderTimer?.Dispose();
            previewRenderCts?.Cancel(); previewRenderCts?.Dispose();
            mandelbrotCSelectorWindow?.Close(); // Закрыть окно выбора 'c', если оно открыто
            base.OnFormClosed(e);
        }
        #endregion
    }
}
// --- END OF FILE FractalburningShipJulia.cs ---