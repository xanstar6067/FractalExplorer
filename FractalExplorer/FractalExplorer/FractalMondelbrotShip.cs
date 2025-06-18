using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Numerics; // Пространство имен для работы с комплексными числами
using System.Runtime.InteropServices; // Для использования Marshal.Copy
using System.Threading;
using System.Windows.Forms;
using System.Linq; // Для FirstOrDefault и других LINQ операций

namespace FractalDraving // Изменено с FractalExplorer на FractalDraving
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
        // Количество потоков для параллельного рендеринга
        private int threadCount;
        // Ширина и высота области отрисовки (canvas)
        private int width, height;

        // Начальная точка для панорамирования изображения
        private Point panStart;
        // Флаг, указывающий, происходит ли панорамирование в данный момент
        private bool panning = false;

        // Текущий уровень масштабирования фрактала
        private double zoom = 1.0;
        // Координаты центра отображаемой области фрактала
        private double centerX = 0.0;
        private double centerY = 0.0;
        // Базовый масштаб для вычисления отображаемой области фрактала
        private const double BASE_SCALE = 3.0; // Оставим как в Mandelbrot для консистентности масштабирования

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

        // Поля для хранения параметров отрисованного битмапа
        private double renderedCenterX;
        private double renderedCenterY;
        private double renderedZoom;

        // Начальные константы для Горящего Корабля (из MandelbrotBurningShipSelectorForm)
        private const double INITIAL_MIN_RE_BS = -2.0;
        private const double INITIAL_MAX_RE_BS = 1.5;
        private const double INITIAL_MIN_IM_BS = -1.5;
        private const double INITIAL_MAX_IM_BS = 1.0;


        public FractalMondelbrotShip()
        {
            InitializeComponent();
            // Начальные параметры для Горящего Корабля
            this.centerX = (INITIAL_MIN_RE_BS + INITIAL_MAX_RE_BS) / 2.0; // -0.25
            this.centerY = (INITIAL_MIN_IM_BS + INITIAL_MAX_IM_BS) / 2.0; // -0.25

            double initialRangeX = INITIAL_MAX_RE_BS - INITIAL_MIN_RE_BS; // 3.5
            double initialRangeY = INITIAL_MAX_IM_BS - INITIAL_MIN_IM_BS; // 2.5
            double initialViewRange = Math.Max(initialRangeX, initialRangeY); // 3.5

            this.zoom = BASE_SCALE / initialViewRange; // 3.0 / 3.5 ~= 0.857
        }

        private async void Form1_Load(object sender, EventArgs e) // Имя метода Form1_Load оставлено для совместимости с дизайнером, если он его ожидает
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
                mondelbrotClassicCb // Палитра, похожая на ту, что в BurningShipCSelectorForm
            };

            foreach (var cb in paletteCheckBoxes.Where(cb => cb != null))
            {
                cb.CheckedChanged += PaletteCheckBox_CheckedChanged;
            }

            nudIterations.ValueChanged += ParamControl_Changed;
            nudThreshold.ValueChanged += ParamControl_Changed;
            cbThreads.SelectedIndexChanged += ParamControl_Changed;
            nudZoom.ValueChanged += ParamControl_Changed;

            canvas2.MouseWheel += Canvas_MouseWheel;
            canvas2.MouseDown += Canvas_MouseDown;
            canvas2.MouseMove += Canvas_MouseMove;
            canvas2.MouseUp += Canvas_MouseUp;
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
            nudIterations.Value = 200; // Начальное значение итераций для Горящего Корабля (из селектора)

            nudThreshold.Minimum = 2m; // Порог для Burning Ship обычно 2.0, как и для Mandelbrot
            nudThreshold.Maximum = 1000m;
            nudThreshold.DecimalPlaces = 1;
            nudThreshold.Increment = 0.1m;
            nudThreshold.Value = 2m;

            nudZoom.DecimalPlaces = 3; // Больше точности для зума
            nudZoom.Increment = 0.1m;
            nudZoom.Minimum = 0.001m; // Позволим больший зум
            nudZoom.Maximum = 1000000000m; // Теоретический максимум

            nudZoom.Value = (decimal)this.zoom;


            this.Resize += Form1_Resize; // Имя метода Form1_Resize оставлено
            canvas2.Resize += Canvas_Resize;

            renderedCenterX = centerX;
            renderedCenterY = centerY;
            renderedZoom = zoom;

            HandleColorBoxEnableState();
            ScheduleRender();
        }

        private void Canvas_Paint(object sender, PaintEventArgs e)
        {
            if (canvas2.Image == null || width <= 0 || height <= 0)
            {
                e.Graphics.Clear(Color.Black);
                return;
            }

            // Логика из FractalMondelbrot.cs для масштабирования и панорамирования отрендеренного изображения
            // Важно: эта логика предполагает, что BASE_SCALE / zoom определяет размер по обеим осям одинаково,
            // что может приводить к искажению, если RenderFractal не компенсирует соотношение сторон канваса.
            // Для консистентности с референсом, оставляем как есть.
            double scaleRendered = BASE_SCALE / renderedZoom;
            double scaleCurrent = BASE_SCALE / zoom;

            if (renderedZoom <= 0 || zoom <= 0 || scaleRendered <= 0 || scaleCurrent <= 0)
            {
                e.Graphics.Clear(Color.Black);
                if (canvas2.Image != null)
                    e.Graphics.DrawImageUnscaled(canvas2.Image, Point.Empty);
                return;
            }

            double complex_half_width_rendered = (BASE_SCALE / renderedZoom) / 2.0;
            double complex_half_height_rendered = (BASE_SCALE / renderedZoom) / 2.0; // Предполагает квадратную область

            double complex_half_width_current = (BASE_SCALE / zoom) / 2.0;
            double complex_half_height_current = (BASE_SCALE / zoom) / 2.0; // Предполагает квадратную область

            double renderedImage_re_min = renderedCenterX - complex_half_width_rendered;
            double renderedImage_im_min = renderedCenterY - complex_half_height_rendered;

            double currentView_re_min = centerX - complex_half_width_current;
            double currentView_im_min = centerY - complex_half_height_current;

            float p1_X = (float)((renderedImage_re_min - currentView_re_min) / (complex_half_width_current * 2.0) * width);
            float p1_Y = (float)((renderedImage_im_min - currentView_im_min) / (complex_half_height_current * 2.0) * height);

            float w_prime = (float)(width * ((BASE_SCALE / renderedZoom) / (BASE_SCALE / zoom)));
            float h_prime = (float)(height * ((BASE_SCALE / renderedZoom) / (BASE_SCALE / zoom)));

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
                // Если отжали последний активный, то не должно ничего оставаться выбранным,
                // кроме случая, когда пользователь кликает по уже выбранному (чтобы его отжать),
                // и это не приводит к снятию всех галочек.
                // Если все сняты, lastSelectedPaletteCheckBox станет null.
                if (paletteCheckBoxes.All(cb => cb == null || !cb.Checked))
                {
                    lastSelectedPaletteCheckBox = null;
                }
                // Иначе (если какой-то другой остался выбранным, хотя логика выше это предотвращает), то:
                // lastSelectedPaletteCheckBox = paletteCheckBoxes.FirstOrDefault(cb => cb != null && cb.Checked);
            }


            foreach (var cb in paletteCheckBoxes.Where(cb => cb != null))
            {
                cb.CheckedChanged += PaletteCheckBox_CheckedChanged;
            }

            // Если ни один не выбран, можно выбрать "Классика" по умолчанию или оставить без палитры (черный цвет для точек вне множества)
            if (lastSelectedPaletteCheckBox == null && mondelbrotClassicBox != null)
            {
                mondelbrotClassicBox.Checked = true; // Это снова вызовет PaletteCheckBox_CheckedChanged
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
            else if (colorBox.Checked && !oldRenderBW.Checked) // Если colorBox - единственный выбранный из старых
            {
                colorBox.Enabled = true;
            }
            else
            {
                colorBox.Enabled = !oldRenderBW.Checked; // colorBox активен, если ЧБ не выбран
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
                zoom = Math.Max((double)nudZoom.Minimum, Math.Min((double)nudZoom.Maximum, (double)nudZoom.Value));
                if (nudZoom.Value != (decimal)zoom)
                {
                    nudZoom.Value = (decimal)zoom; // Синхронизируем, если значение было скорректировано
                }
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
                renderTimer.Start(); // Перезапустить таймер, если рендеринг уже идет
                return;
            }

            isRenderingPreview = true;

            previewRenderCts?.Dispose();
            previewRenderCts = new CancellationTokenSource();
            CancellationToken token = previewRenderCts.Token;

            UpdateParameters();

            double currentRenderCenterX = centerX;
            double currentRenderCenterY = centerY;
            double currentRenderZoom = zoom;

            try
            {
                // Запускаем рендеринг в фоновом потоке
                await Task.Run(() => RenderFractal(token, currentRenderCenterX, currentRenderCenterY, currentRenderZoom), token);
            }
            catch (OperationCanceledException) { /* Ожидаемое исключение при отмене */ }
            catch (Exception ex)
            {
                // Можно добавить логирование или MessageBox для других ошибок
                // System.Diagnostics.Debug.WriteLine($"Render Error: {ex.Message}"); 
            }
            finally
            {
                isRenderingPreview = false;
            }
        }

        private void UpdateParameters()
        {
            maxIterations = (int)nudIterations.Value;
            threshold = (double)nudThreshold.Value;
            threadCount = cbThreads.SelectedItem.ToString() == "Auto"
                ? Environment.ProcessorCount
                : Convert.ToInt32(cbThreads.SelectedItem);
        }

        // --- Делегаты и методы для палитр (скопированы из FractalMondelbrot.cs) ---
        private delegate Color PaletteFunction(double t, int iter, int maxIterations, int maxColorIter);

        private Color GetPixelColor(int iter, int currentMaxIterations, int currentMaxColorIter)
        {
            if (iter == currentMaxIterations)
            {
                // Для палитры "6" (темная) внутренняя часть множества немного светлее черного
                return (lastSelectedPaletteCheckBox?.Name == "checkBox6") ? Color.FromArgb(50, 50, 50) : Color.Black;
            }

            // Нормализованное значение итерации для цвета
            // t_capped используется для большинства палитр, чтобы избежать слишком резких переходов в конце
            double t_capped = (double)Math.Min(iter, currentMaxColorIter) / currentMaxColorIter;
            // t_log используется для стандартной черно-белой палитры для сглаживания
            double t_log = Math.Log(Math.Min(iter, currentMaxColorIter) + 1) / Math.Log(currentMaxColorIter + 1);

            PaletteFunction selectedPaletteFunc = GetDefaultPaletteColor; // По умолчанию ЧБ логарифмическая

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

            // Большинство палитр используют t_capped, кроме стандартной ЧБ (GetDefaultPaletteColor)
            double t_param = (selectedPaletteFunc == GetDefaultPaletteColor) ? t_log : t_capped;
            return selectedPaletteFunc(t_param, iter, currentMaxIterations, currentMaxColorIter);
        }

        // Палитра, аналогичная MandelbrotBurningShipSelectorForm и Mandelbrot.cs "Классика"
        private Color GetPaletteMandelbrotClassicColor(double t_ignored, int iter, int maxIter, int maxClrIter)
        {
            double t_classic = (double)iter / maxIter; // Используем полное количество итераций для этой палитры
            byte r_val, g_val, b_val;

            if (t_classic < 0.5)
            {
                double t_half = t_classic * 2.0; // от 0 до 1
                r_val = (byte)(t_half * 200);        // 0..200
                g_val = (byte)(t_half * 50);         // 0..50
                b_val = (byte)(t_half * 30);         // 0..30
            }
            else
            {
                double t_half = (t_classic - 0.5) * 2.0; // от 0 до 1
                r_val = (byte)(200 + t_half * 55);       // 200..255
                g_val = (byte)(50 + t_half * 205);        // 50..255
                b_val = (byte)(30 + t_half * 225);        // 30..255
            }
            return Color.FromArgb(r_val, g_val, b_val);
        }


        private Color GetDefaultPaletteColor(double t_log, int iter, int maxIter, int maxClrIter) // Стандартная ЧБ (логарифмическая)
        {
            int cVal = (int)(255.0 * (1 - t_log)); // Белый к черному
            return Color.FromArgb(cVal, cVal, cVal);
        }

        private Color GetPaletteColorBoxColor(double t_capped, int iter, int maxIter, int maxClrIter) // Радужная HSV
        {
            return ColorFromHSV(360.0 * t_capped, 0.8, 0.9); // Насыщенность и яркость можно подстроить
        }

        private Color GetPaletteOldBWColor(double t_capped, int iter, int maxIter, int maxClrIter) // Старая ЧБ (линейная)
        {
            int cVal = 255 - (int)(255.0 * t_capped); // Белый к черному
            return Color.FromArgb(cVal, cVal, cVal);
        }

        private Color LerpColor(Color a, Color b, double t)
        {
            t = Math.Max(0, Math.Min(1, t)); // Ограничить t диапазоном [0, 1]
            return Color.FromArgb(
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t)
            );
        }

        // Палитры 1-6 идентичны FractalMondelbrot.cs
        private Color GetPalette1Color(double t, int iter, int maxIter, int maxClrIter) // Огненная
        {
            Color c1 = Color.Black;
            Color c2 = Color.FromArgb(200, 0, 0);    // Темно-красный
            Color c3 = Color.FromArgb(255, 100, 0);  // Оранжевый
            Color c4 = Color.FromArgb(255, 255, 100); // Желтый
            Color c5 = Color.White;                  // Белый (для самых быстрых точек)

            if (t < 0.25) return LerpColor(c1, c2, t / 0.25);
            if (t < 0.50) return LerpColor(c2, c3, (t - 0.25) / 0.25);
            if (t < 0.75) return LerpColor(c3, c4, (t - 0.50) / 0.25);
            return LerpColor(c4, c5, (t - 0.75) / 0.25);
        }

        private Color GetPalette2Color(double t, int iter, int maxIter, int maxClrIter) // Ледяная
        {
            Color c1 = Color.Black;
            Color c2 = Color.FromArgb(0, 0, 100);    // Темно-синий
            Color c3 = Color.FromArgb(0, 120, 200);  // Голубой
            Color c4 = Color.FromArgb(170, 220, 255); // Светло-голубой
            Color c5 = Color.White;                  // Белый

            if (t < 0.25) return LerpColor(c1, c2, t / 0.25);
            if (t < 0.50) return LerpColor(c2, c3, (t - 0.25) / 0.25);
            if (t < 0.75) return LerpColor(c3, c4, (t - 0.50) / 0.25);
            return LerpColor(c4, c5, (t - 0.75) / 0.25);
        }

        private Color GetPalette3Color(double t, int iter, int maxIter, int maxClrIter) // Психоделическая (на основе синусов)
        {
            // t здесь от 0 до 1
            double r_comp = Math.Sin(t * Math.PI * 3.0 + 0.5) * 0.45 + 0.5; // Диапазон ~0.05 - 0.95
            double g_comp = Math.Sin(t * Math.PI * 3.0 + Math.PI * 2.0 / 3.0 + 0.5) * 0.45 + 0.5;
            double b_comp = Math.Sin(t * Math.PI * 3.0 + Math.PI * 4.0 / 3.0 + 0.5) * 0.45 + 0.5;
            return Color.FromArgb((int)(r_comp * 255), (int)(g_comp * 255), (int)(b_comp * 255));
        }
        private Color GetPalette4Color(double t, int iter, int maxIter, int maxClrIter) // "Электрик"
        {
            Color c1 = Color.FromArgb(10, 0, 20); // Глубокий фиолетовый
            Color c2 = Color.FromArgb(255, 0, 255); // Ярко-пурпурный (Magenta)
            Color c3 = Color.FromArgb(0, 255, 255); // Ярко-голубой (Cyan)
            Color c4 = Color.FromArgb(230, 230, 250); // Лавандовый (почти белый)

            if (t < 0.1) return LerpColor(c1, c2, t / 0.1);         // Быстрый переход к пурпурному
            if (t < 0.4) return LerpColor(c2, c1, (t - 0.1) / 0.3); // Обратно к темному
            if (t < 0.5) return LerpColor(c1, c3, (t - 0.4) / 0.1); // Быстрый переход к голубому
            if (t < 0.8) return LerpColor(c3, c1, (t - 0.5) / 0.3); // Обратно к темному
            return LerpColor(c1, c4, (t - 0.8) / 0.2);              // К светлому
        }

        private Color GetPalette5Color(double t, int iter, int maxIter, int maxClrIter) // "Металлик"
        {
            // t от 0 до 1
            int baseGray = 50 + (int)(t * 150); // от 50 до 200
            double shine = Math.Sin(t * Math.PI * 5); // [-1, 1], 2.5 цикла синусоиды
            int finalGray = Math.Max(0, Math.Min(255, baseGray + (int)(shine * 40))); // Добавляем "блик"
            return Color.FromArgb(finalGray, finalGray, Math.Min(255, finalGray + (int)(t * 25))); // Немного синевы
        }


        private Color GetPalette6Color(double t, int iter, int maxIter, int maxClrIter) // "Туманность" (темная с оттенками)
        {
            // t от 0 до 1
            double hue = (t * 200.0 + 180.0) % 360.0; // От синего (180) через пурпурный, красный, к зеленоватому (380%360=20)
            double sat = Math.Max(0.2, Math.Min(0.6, 0.35 + (Math.Sin(t * Math.PI * 2) * 0.1))); // Низкая/средняя насыщенность, слегка колеблется
            double val = Math.Max(0.7, Math.Min(0.95, 0.80 + (Math.Cos(t * Math.PI * 2.5) * 0.15))); // Высокая яркость, слегка колеблется
            return ColorFromHSV(hue, sat, val);
        }
        // --- Конец методов палитр ---

        private void RenderFractal(CancellationToken token, double renderCenterX, double renderCenterY, double renderZoom)
        {
            if (token.IsCancellationRequested) return;
            // Проверка на нулевые размеры перед созданием Bitmap
            if (isHighResRendering || canvas2.Width <= 0 || canvas2.Height <= 0) return;

            Bitmap bmp = null;
            BitmapData bmpData = null;

            try
            {
                bmp = new Bitmap(width, height, PixelFormat.Format24bppRgb);
                token.ThrowIfCancellationRequested(); // Проверка перед LockBits

                Rectangle rect = new Rectangle(0, 0, width, height);
                bmpData = bmp.LockBits(rect, ImageLockMode.WriteOnly, bmp.PixelFormat);
                token.ThrowIfCancellationRequested(); // Проверка после LockBits

                int stride = bmpData.Stride;
                IntPtr scan0 = bmpData.Scan0;
                // Размер буфера должен быть Math.Abs(stride) * height
                byte[] buffer = new byte[Math.Abs(stride) * height];

                ParallelOptions po = new ParallelOptions { MaxDegreeOfParallelism = threadCount, CancellationToken = token };
                int done = 0;
                // Максимальное количество итераций для раскраски, может отличаться от maxIterations для расчета
                // Для Горящего Корабля, как и для Мандельброта, 1000 итераций для цвета - хороший старт.
                const int currentMaxColorIter = 1000;

                // Расчет масштаба на пиксель (как в FractalMondelbrot.cs, может вызывать искажения на неквадратных канвасах)
                // Для консистентности с референсом, оставляем эту логику.
                // Если нужно исправить искажения, здесь потребуется другая логика расчета масштаба.
                double current_render_scale_factor_w = (BASE_SCALE / renderZoom) / width;
                double current_render_scale_factor_h = (BASE_SCALE / renderZoom) / height;


                Parallel.For(0, height, po, y =>
                {
                    if (token.IsCancellationRequested) return; // Проверка внутри цикла

                    int rowOffset = y * stride;
                    for (int x = 0; x < width; x++)
                    {
                        // Преобразование координат пикселя в комплексное число c0
                        double c_re = renderCenterX + (x - width / 2.0) * current_render_scale_factor_w;
                        double c_im = renderCenterY + (y - height / 2.0) * current_render_scale_factor_h;
                        // Замечание: в FractalMondelbrot.cs ось Y не инвертируется (y=0 -> min Im, y=height -> max Im).
                        // В MandelbrotBurningShipSelectorForm ось Y инвертировалась.
                        // Оставляем как в FractalMondelbrot.cs для консистентности.

                        Complex c0 = new Complex(c_re, c_im);
                        Complex z = Complex.Zero;
                        int iter_val = 0;

                        // --- ИЗМЕНЕНИЕ: Формула Горящего Корабля (Мандельброт-версия) ---
                        while (iter_val < maxIterations && z.Magnitude <= threshold) // threshold вместо 2.0 для гибкости
                        {
                            z = new Complex(Math.Abs(z.Real), Math.Abs(z.Imaginary)); // Взять абсолютные значения
                            z = z * z + c0;                                          // Стандартная итерация
                            iter_val++;
                        }
                        // --- КОНЕЦ ИЗМЕНЕНИЯ ---

                        Color pixelColor = GetPixelColor(iter_val, maxIterations, currentMaxColorIter);
                        int index = rowOffset + x * 3; // 3 байта на пиксель (B, G, R)
                        buffer[index] = pixelColor.B;
                        buffer[index + 1] = pixelColor.G;
                        buffer[index + 2] = pixelColor.R;
                    }

                    // Обновление прогресс-бара
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
                        catch (InvalidOperationException) { /* Окно может быть уже закрыто */ }
                    }
                });

                token.ThrowIfCancellationRequested(); // Проверка перед Marshal.Copy
                Marshal.Copy(buffer, 0, scan0, buffer.Length);
                bmp.UnlockBits(bmpData); // Важно освободить биты
                bmpData = null; // Пометить, что биты освобождены
                token.ThrowIfCancellationRequested(); // Проверка перед обновлением UI

                // Обновление изображения на форме (в UI потоке)
                if (canvas2.IsHandleCreated && !canvas2.IsDisposed)
                {
                    Bitmap oldImage = null;
                    canvas2.Invoke((Action)(() =>
                    {
                        if (token.IsCancellationRequested)
                        { // Еще одна проверка перед присвоением
                            bmp?.Dispose(); // Освободить новый битмап, если отмена
                            return;
                        }
                        oldImage = canvas2.Image as Bitmap;
                        canvas2.Image = bmp;
                        // Сохраняем параметры, с которыми был сделан этот рендер
                        renderedCenterX = renderCenterX;
                        renderedCenterY = renderCenterY;
                        renderedZoom = renderZoom;
                        bmp = null; // Передача владения битмапом PictureBox'у
                    }));
                    oldImage?.Dispose(); // Освободить старый битмап
                }
                else
                {
                    bmp?.Dispose(); // Если канвас уже не существует
                }
            }
            // catch (OperationCanceledException) { /* Уже обработано выше или ожидается */ } // Не нужно здесь, т.к. finally выполнится
            finally
            {
                if (bmpData != null && bmp != null) // Если UnlockBits не был вызван из-за исключения
                {
                    try { bmp.UnlockBits(bmpData); } catch { /* Игнорировать ошибки при попытке разблокировать */}
                }
                if (bmp != null) // Если bmp не был присвоен PictureBox'у или не был освобожден ранее
                {
                    bmp.Dispose();
                }
            }
        }

        private Bitmap RenderFractalToBitmap(int renderWidth, int renderHeight, double currentCenterX, double currentCenterY,
                                             double currentZoom, double currentBaseScale,
                                             int currentMaxIterations_param, double currentThreshold_param, int numThreads,
                                             Action<int> reportProgressCallback)
        {
            if (renderWidth <= 0 || renderHeight <= 0)
            {
                return new Bitmap(1, 1); // Возвращаем минимальный битмап
            }

            Bitmap bmp = new Bitmap(renderWidth, renderHeight, PixelFormat.Format24bppRgb);
            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, renderWidth, renderHeight), ImageLockMode.WriteOnly, bmp.PixelFormat);
            int stride = bmpData.Stride;
            IntPtr scan0 = bmpData.Scan0;
            byte[] buffer = new byte[Math.Abs(stride) * renderHeight];

            ParallelOptions po = new ParallelOptions { MaxDegreeOfParallelism = numThreads };
            long done = 0;
            const int currentMaxColorIter_param = 1000; // Можно сделать настраиваемым или передавать

            // Логика масштаба, аналогичная RenderFractal (с потенциальным искажением)
            double scale_factor_w = (currentBaseScale / currentZoom) / renderWidth;
            double scale_factor_h = (currentBaseScale / currentZoom) / renderHeight;

            Parallel.For(0, renderHeight, po, y =>
            {
                int rowOffset = y * stride;
                for (int x = 0; x < renderWidth; x++)
                {
                    double c_re = currentCenterX + (x - renderWidth / 2.0) * scale_factor_w;
                    double c_im = currentCenterY + (y - renderHeight / 2.0) * scale_factor_h;
                    Complex c0 = new Complex(c_re, c_im);

                    Complex z = Complex.Zero;
                    int iter_val = 0;

                    // --- ИЗМЕНЕНИЕ: Формула Горящего Корабля (Мандельброт-версия) ---
                    while (iter_val < currentMaxIterations_param && z.Magnitude <= currentThreshold_param)
                    {
                        z = new Complex(Math.Abs(z.Real), Math.Abs(z.Imaginary));
                        z = z * z + c0;
                        iter_val++;
                    }
                    // --- КОНЕЦ ИЗМЕНЕНИЯ ---

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


        private Color ColorFromHSV(double hue, double saturation, double value)
        {
            hue = (hue % 360 + 360) % 360; // Гарантирует, что hue в [0, 360)
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);

            value = Math.Max(0, Math.Min(1, value)); // Ограничение value [0,1]
            saturation = Math.Max(0, Math.Min(1, saturation)); // Ограничение saturation [0,1]

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
                default: return Color.FromArgb(v_comp, p_comp, q_comp); // case 5
            }
        }

        private void Canvas_MouseWheel(object sender, MouseEventArgs e)
        {
            if (isHighResRendering) return;

            double zoomFactor = e.Delta > 0 ? 1.5 : 1.0 / 1.5;
            double oldZoom = zoom;

            // Расчет координат курсора в комплексной плоскости перед масштабированием
            // Опять же, эта логика масштаба предполагает одинаковый масштаб по осям,
            // что может быть неверно, если RenderFractal имеет искажение.
            // Для консистентности с референсом, оставляем как есть.
            double scaleBeforeZoomX = BASE_SCALE / oldZoom / width;
            double scaleBeforeZoomY = BASE_SCALE / oldZoom / height;

            double mouseRe = centerX + (e.X - width / 2.0) * scaleBeforeZoomX;
            double mouseIm = centerY + (e.Y - height / 2.0) * scaleBeforeZoomY;

            zoom = Math.Max((double)nudZoom.Minimum, Math.Min((double)nudZoom.Maximum, zoom * zoomFactor));

            // Расчет новых centerX, centerY, чтобы точка под курсором осталась на месте
            double scaleAfterZoomX = BASE_SCALE / zoom / width;
            double scaleAfterZoomY = BASE_SCALE / zoom / height;

            centerX = mouseRe - (e.X - width / 2.0) * scaleAfterZoomX;
            centerY = mouseIm - (e.Y - height / 2.0) * scaleAfterZoomY;

            canvas2.Invalidate(); // Запросить перерисовку с новым масштабом/положением

            // Обновить NumericUpDown для zoom и инициировать рендер, если значение изменилось
            if (nudZoom.Value != (decimal)zoom)
            {
                nudZoom.Value = (decimal)zoom; // Это вызовет ParamControl_Changed -> ScheduleRender
            }
            else
            {
                ScheduleRender(); // Если nudZoom.Value уже было равно новому zoom (маловероятно из-за точности)
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

            // Логика панорамирования (аналогично FractalMondelbrot.cs)
            double scaleX = BASE_SCALE / zoom / width;
            double scaleY = BASE_SCALE / zoom / height;

            centerX -= (e.X - panStart.X) * scaleX;
            centerY -= (e.Y - panStart.Y) * scaleY;
            panStart = e.Location;

            canvas2.Invalidate(); // Перерисовать с новым положением
            ScheduleRender();     // Запланировать полный рендер
        }

        private void Canvas_MouseUp(object sender, MouseEventArgs e)
        {
            if (isHighResRendering) return;
            if (e.Button == MouseButtons.Left)
            {
                panning = false;
            }
        }

        private void BtnSave_Click(object sender, EventArgs e) // Сохранение текущего изображения из PictureBox
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

        private void btnRender_Click(object sender, EventArgs e) // Кнопка "Запустить рендер"
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

                if (nudW != null) nudW.Enabled = enabled; // nudW - имя из дизайнера FractalMondelbrot
                if (nudH != null) nudH.Enabled = enabled; // nudH - имя из дизайнера FractalMondelbrot

                foreach (var cb in paletteCheckBoxes.Where(cb => cb != null))
                {
                    cb.Enabled = enabled;
                }
                if (enabled)
                {
                    HandleColorBoxEnableState(); // Восстановить состояние colorBox
                }
                else if (colorBox != null)
                {
                    colorBox.Enabled = false; // Отключить принудительно
                }
                // Кнопка сохранения текущего превью
                Button savePreviewButton = Controls.Find("btnSave", true).FirstOrDefault() as Button;
                if (savePreviewButton != null) savePreviewButton.Enabled = enabled;

                // Кнопка сохранения в высоком разрешении (btnSaveHighRes - предполагаемое имя, если бы оно отличалось)
                // В вашем дизайнере она называется btnSave (для Mandelbrot), и мы ее переиспользуем.
                // Если у вас две разные кнопки, нужно будет управлять ими по их именам.
                // Здесь я предполагаю, что btnSave_Click_1 привязан к кнопке сохранения PNG.

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

        private async void btnSave_Click_1(object sender, EventArgs e) // Сохранение в высоком разрешении (PNG)
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
            // --- ИЗМЕНЕНИЕ: Имя файла и заголовок для Горящего Корабля ---
            string suggestedFileName = $"fractal_burningship_{timestamp}.png";

            using (SaveFileDialog saveDialog = new SaveFileDialog
            {
                Filter = "PNG Image|*.png",
                Title = "Сохранить фрактал Горящий Корабль (Высокое разрешение)",
                FileName = suggestedFileName
            })
            // --- КОНЕЦ ИЗМЕНЕНИЯ ---
            {
                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    Button currentActionSaveButton = sender as Button; // Кнопка, вызвавшая событие
                    isHighResRendering = true;
                    if (currentActionSaveButton != null) currentActionSaveButton.Enabled = false;
                    SetMainControlsEnabled(false); // Отключить основные контролы

                    if (progressPNG != null) // progressPNG - имя из дизайнера FractalMondelbrot
                    {
                        progressPNG.Value = 0;
                        progressPNG.Visible = true;
                    }

                    try
                    {
                        UpdateParameters(); // Убедиться, что параметры актуальны
                        // Захват текущих параметров для передачи в Task.Run
                        int currentMaxIterations_Capture = this.maxIterations;
                        double currentThreshold_Capture = this.threshold;
                        double currentZoom_Capture = this.zoom;
                        double currentCenterX_Capture = this.centerX;
                        double currentCenterY_Capture = this.centerY;
                        int currentThreadCount_Capture = this.threadCount;

                        Bitmap highResBitmap = await Task.Run(() => RenderFractalToBitmap(
                            saveWidth, saveHeight,
                            currentCenterX_Capture, currentCenterY_Capture, currentZoom_Capture,
                            BASE_SCALE, // Используем тот же BASE_SCALE
                            currentMaxIterations_Capture, currentThreshold_Capture,
                            currentThreadCount_Capture,
                            progressPercentage =>
                            {
                                if (progressPNG != null && progressPNG.IsHandleCreated && !progressPNG.IsDisposed)
                                {
                                    try
                                    {
                                        progressPNG.Invoke((Action)(() => {
                                            if (progressPNG.Maximum > 0 && progressPNG.Value <= progressPNG.Maximum) // Доп. проверка
                                            {
                                                progressPNG.Value = Math.Min(progressPNG.Maximum, progressPercentage);
                                            }
                                        }));
                                    }
                                    catch (ObjectDisposedException) { } // Игнорировать, если форма уже закрывается
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
                        SetMainControlsEnabled(true); // Включить основные контролы

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
            previewRenderCts?.Cancel(); // Отменить текущий рендеринг
            previewRenderCts?.Dispose();
            renderTimer?.Dispose();

            base.OnFormClosed(e);
        }
    }
}