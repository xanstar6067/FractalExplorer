using System.Drawing.Imaging;
using System.Numerics; // Пространство имен для работы с комплексными числами
using System.Runtime.InteropServices; // Для использования Marshal.Copy

namespace FractalDraving
{
    /// <summary>
    /// Главная форма приложения для отображения и взаимодействия с фракталом Жюлиа.
    /// </summary>
    public partial class FractalJulia : Form
    {
        // Таймер для отложенного рендеринга предварительного просмотра фрактала
        private System.Windows.Forms.Timer renderTimer;

        // Комплексное число 'c', определяющее конкретный фрактал Жюлиа
        private Complex c;
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
        private const double BASE_SCALE = 1.0; // Константа базового масштаба для фрактала

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

        // Константы для отображения множества Мандельброта (используется для выбора 'c')
        private const double MANDELBROT_MIN_RE = -2.0; // Минимальная вещественная часть для предпросмотра Мандельброта
        private const double MANDELBROT_MAX_RE = 1.0;  // Максимальная вещественная часть для предпросмотра Мандельброта
        private const double MANDELBROT_MIN_IM = -1.2; // Минимальная мнимая часть для предпросмотра Мандельброта
        private const double MANDELBROT_MAX_IM = 1.2;  // Максимальная мнимая часть для предпросмотра Мандельброта
        private const int MANDELBROT_PREVIEW_ITERATIONS = 75; // Количество итераций для предпросмотра Мандельброта

        // Форма для выбора параметра 'c' с помощью множества Мандельброта
        private MandelbrotSelectorForm mandelbrotCSelectorWindow;

        // Поля для хранения параметров отрисованного битмапа
        private double renderedCenterX;
        private double renderedCenterY;
        private double renderedZoom;

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
        /// Конструктор формы фрактала Жюлиа.
        /// </summary>
        public FractalJulia()
        {
            InitializeComponent(); // Инициализация компонентов формы, созданных дизайнером
        }

        /// <summary>
        /// Обработчик события загрузки формы.
        /// Выполняет инициализацию параметров, элементов управления и подписывается на события.
        /// </summary>
        private async void Form1_Load(object sender, EventArgs e)
        {
            // Инициализация размеров области отрисовки на основе размеров элемента canvas
            width = canvas1.Width;
            height = canvas1.Height;

            // Настройка таймера для отложенного рендеринга предварительного просмотра
            renderTimer = new System.Windows.Forms.Timer { Interval = 300 }; // Интервал срабатывания таймера 300 мс
            renderTimer.Tick += RenderTimer_Tick;                             // Подписка на событие тика таймера

            // Инициализация массива чекбоксов для выбора цветовых палитр
            // Некоторые чекбоксы находятся по имени с помощью Controls.Find
            paletteCheckBoxes = new CheckBox[] {
                colorBox, // Чекбокс для старой цветной палитры
                oldRenderBW, // Чекбокс для старой черно-белой палитры
                Controls.Find("checkBox1", true).FirstOrDefault() as CheckBox, // Новая палитра 1
                Controls.Find("checkBox2", true).FirstOrDefault() as CheckBox, // Новая палитра 2
                Controls.Find("checkBox3", true).FirstOrDefault() as CheckBox, // Новая палитра 3
                Controls.Find("checkBox4", true).FirstOrDefault() as CheckBox, // Новая палитра 4
                Controls.Find("checkBox5", true).FirstOrDefault() as CheckBox, // Новая палитра 5
                Controls.Find("checkBox6", true).FirstOrDefault() as CheckBox  // Новая палитра 6
            };

            // Подписка на событие изменения состояния (CheckedChanged) для каждого существующего чекбокса палитры
            foreach (var cb in paletteCheckBoxes.Where(cb => cb != null))
            {
                cb.CheckedChanged += PaletteCheckBox_CheckedChanged;
            }

            // Подписка на события изменения значений элементов управления параметрами фрактала
            nudRe1.ValueChanged += ParamControl_Changed;            // Вещественная часть 'c'
            nudIm1.ValueChanged += ParamControl_Changed;            // Мнимая часть 'c'
            nudIterations1.ValueChanged += ParamControl_Changed;    // Количество итераций
            nudThreshold1.ValueChanged += ParamControl_Changed;     // Порог "убегания"
            cbThreads1.SelectedIndexChanged += ParamControl_Changed;// Количество потоков
            nudZoom.ValueChanged += ParamControl_Changed;          // Уровень масштаба
            nudBaseScale.ValueChanged += NudBaseScale_ValueChanged; // Масштаб лупы для окна выбора 'c'

            // Подписка на события мыши для области отрисовки (canvas)
            canvas1.MouseWheel += Canvas_MouseWheel;   // Масштабирование колесом мыши
            canvas1.MouseDown += Canvas_MouseDown;     // Начало панорамирования
            canvas1.MouseMove += Canvas_MouseMove;     // Процесс панорамирования
            canvas1.MouseUp += Canvas_MouseUp;         // Окончание панорамирования
            canvas1.Paint += Canvas_Paint;             // Перерисовка canvas для "мгновенного" панорамирования/масштабирования

            // Если элемент для отображения множества Мандельброта (mandelbrotCanvas) существует, настраиваем его
            if (mandelbrotCanvas1 != null)
            {
                mandelbrotCanvas1.Click += mandelbrotCanvas_Click;       // Обработка клика для выбора 'c' из предпросмотра Мандельброта
                mandelbrotCanvas1.Paint += mandelbrotCanvas_Paint;       // Отрисовка маркера на множестве Мандельброта
                await Task.Run(() => RenderAndDisplayMandelbrotSet());  // Асинхронная отрисовка множества Мандельброта при загрузке
            }

            // Заполнение выпадающего списка (ComboBox) для выбора количества потоков рендеринга
            int cores = Environment.ProcessorCount; // Получение количества логических процессоров в системе
            for (int i = 1; i <= cores; i++)
            {
                cbThreads1.Items.Add(i); // Добавление числовых значений от 1 до количества ядер
            }
            cbThreads1.Items.Add("Auto"); // Добавление опции автоматического выбора (использует все ядра)
            cbThreads1.SelectedItem = "Auto"; // Установка "Auto" как значения по умолчанию

            // Настройка параметров для элементов NumericUpDown (Re, Im, Iterations, Threshold, Zoom, BaseScale)
            // Установка минимальных, максимальных значений, количества десятичных знаков и шага инкремента.
            nudRe1.Minimum = -2m;
            nudRe1.Maximum = 2m;
            nudRe1.DecimalPlaces = 3;
            nudRe1.Increment = 0.001m;

            nudIm1.Minimum = -2m;
            nudIm1.Maximum = 2m;
            nudIm1.DecimalPlaces = 3;
            nudIm1.Increment = 0.001m;

            nudIterations1.Minimum = 50;
            nudIterations1.Maximum = 100000;
            nudIterations1.Value = 600; // Значение по умолчанию для количества итераций

            nudThreshold1.Minimum = 2m;
            nudThreshold1.Maximum = 10m;
            nudThreshold1.DecimalPlaces = 1;
            nudThreshold1.Increment = 0.1m;

            // *** ИЗМЕНЕНИЯ ДЛЯ ZOOM ***
            nudZoom.DecimalPlaces = 4;    // Увеличим точность для малых значений
            nudZoom.Increment = 0.1m;       // Шаг изменения 0.1
            nudZoom.Minimum = 0.01m;        // Устанавливаем минимальный порог для отдаления
            nudZoom.Value = 1m;             // Значение по умолчанию для масштаба

            nudBaseScale.Minimum = 1m;
            nudBaseScale.Maximum = 10m;
            nudBaseScale.DecimalPlaces = 1;
            nudBaseScale.Increment = 0.1m;
            nudBaseScale.Value = 4m; // Значение по умолчанию для масштаба лупы

            // Подписка на события изменения размера формы и области отрисовки (canvas)
            this.Resize += Form1_Resize;    // При изменении размера формы
            canvas1.Resize += Canvas_Resize; // При изменении размера canvas

            // Инициализация параметров отрисованного битмапа значениями по умолчанию (текущим центром и масштабом)
            renderedCenterX = centerX;
            renderedCenterY = centerY;
            renderedZoom = zoom;

            // Установка начального состояния доступности чекбокса "colorBox" (старая цветная палитра)
            HandleColorBoxEnableState();
            // Запуск первоначального рендеринга фрактала (с небольшой задержкой)
            ScheduleRender();
        }

        /// <summary>
        /// Обработчик события Paint для canvas.
        /// Отрисовывает текущий битмап (canvas.Image) с учетом текущих параметров панорамирования и масштабирования (centerX, centerY, zoom),
        /// трансформируя его относительно параметров, с которыми он был изначально отрисован (renderedCenterX, renderedCenterY, renderedZoom).
        /// Это позволяет создать эффект "мгновенного" панорамирования и зума до того, как будет готов новый, полностью отрендеренный кадр.
        /// </summary>
        private void Canvas_Paint(object sender, PaintEventArgs e)
        {
            // Если изображения нет или размеры области отрисовки некорректны, очищаем область черным цветом и выходим.
            if (canvas1.Image == null || width <= 0 || height <= 0)
            {
                e.Graphics.Clear(Color.Black);
                return;
            }

            // renderedZoom, renderedCenterX, renderedCenterY - параметры, с которыми был создан canvas.Image (предыдущий кадр)
            // zoom, centerX, centerY - текущие целевые параметры отображения (после панорамирования/масштабирования)

            // Масштаб комплексной плоскости, с которым был создан canvas.Image
            double scaleRendered = BASE_SCALE / renderedZoom;
            // Текущий целевой масштаб комплексной плоскости для отображения
            double scaleCurrent = BASE_SCALE / zoom;

            // Проверка на корректность (положительность) масштабов.
            // Если какой-либо масштаб некорректен, очищаем область и рисуем изображение без трансформации (безопасный фолбэк).
            if (renderedZoom <= 0 || zoom <= 0 || scaleRendered <= 0 || scaleCurrent <= 0)
            {
                e.Graphics.Clear(Color.Black);
                e.Graphics.DrawImageUnscaled(canvas1.Image, Point.Empty); // Отрисовка без масштабирования
                return;
            }

            // Определяем границы отрендеренного изображения (canvas.Image) на комплексной плоскости
            // Минимальная вещественная часть отрендерованного изображения
            double renderedImage_re_min = renderedCenterX - scaleRendered / 2.0;
            // Минимальная мнимая часть отрендерованного изображения (Y в комплексной плоскости соответствует экранному Y)
            double renderedImage_im_min = renderedCenterY - scaleRendered / 2.0;


            // Определяем границы текущего вида (то, что мы хотим видеть сейчас) на комплексной плоскости
            // Минимальная вещественная часть текущего вида
            double currentView_re_min = centerX - scaleCurrent / 2.0;
            // Минимальная мнимая часть текущего вида
            double currentView_im_min = centerY - scaleCurrent / 2.0;


            // Преобразуем левый верхний угол отрендеренного изображения (canvas.Image)
            // из координат комплексной плоскости в экранные координаты ТЕКУЩЕГО вида.
            // p1_X: Экранная X-координата левого края отрендеренного изображения в текущей системе координат.
            // (координата на комплексной плоскости - левая граница видимой области на комплексной плоскости) * (пикселей на единицу комплексной плоскости)
            float p1_X = (float)((renderedImage_re_min - currentView_re_min) / scaleCurrent * width);
            // p1_Y: Экранная Y-координата верхнего края отрендеренного изображения в текущей системе координат.
            // (В RenderFractal: im = renderCenterY + (y - height / 2.0) * scale / height; im увеличивается с экранным y)
            float p1_Y = (float)((renderedImage_im_min - currentView_im_min) / scaleCurrent * height);


            // Ширина и высота отрендеренного изображения (canvas.Image) в пикселях ТЕКУЩЕГО масштаба
            // (на сколько нужно растянуть/сжать существующее изображение)
            float w_prime = (float)(width * (scaleRendered / scaleCurrent));
            float h_prime = (float)(height * (scaleRendered / scaleCurrent));

            // Точки назначения для метода DrawImage, определяющие трансформацию (масштабирование и сдвиг)
            // существующего изображения для отображения в новой системе координат.
            PointF destPoint1 = new PointF(p1_X, p1_Y);                // Верхний левый угол трансформированного изображения
            PointF destPoint2 = new PointF(p1_X + w_prime, p1_Y);      // Верхний правый угол
            PointF destPoint3 = new PointF(p1_X, p1_Y + h_prime);      // Нижний левый угол

            e.Graphics.Clear(Color.Black); // Очистка области перед отрисовкой

            // Установка режима интерполяции для сглаживания при масштабировании изображения
            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Bilinear;
            // Установка смещения пикселей для лучшего совмещения при масштабировании (уменьшает артефакты на границах пикселей)
            e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

            // Отрисовка трансформированного изображения, если вычисленные размеры (w_prime, h_prime) корректны (положительны).
            if (w_prime > 0 && h_prime > 0)
            {
                try
                {
                    // Отрисовываем существующее изображение (canvas.Image) с применением рассчитанной трансформации.
                    // Исходная область для отрисовки - всё изображение (0,0 до Image.Width, Image.Height).
                    // Она будет отображена в параллелограмме, заданном точками destPoint1, destPoint2, destPoint3.
                    e.Graphics.DrawImage(canvas1.Image, new PointF[] { destPoint1, destPoint2, destPoint3 });
                }
                catch (ArgumentException) // Исключение может возникнуть при некорректных параметрах (например, вырожденный параллелограмм).
                {
                    // В случае ошибки, отрисовываем изображение без трансформации (безопасный фолбэк).
                    e.Graphics.DrawImageUnscaled(canvas1.Image, Point.Empty);
                }
            }
            else
            {
                // Если вычисленная ширина/высота некорректны, рисуем как есть (без трансформации).
                e.Graphics.DrawImageUnscaled(canvas1.Image, Point.Empty);
            }
        }

        /// <summary>
        /// Обработчик изменения значения в NumericUpDown для базового масштаба (nudBaseScale).
        /// Это значение используется для лупы в окне выбора 'c' (MandelbrotSelectorForm).
        /// Вызывает событие LoupeZoomChanged, чтобы уведомить подписчиков (MandelbrotSelectorForm).
        /// </summary>
        private void NudBaseScale_ValueChanged(object sender, EventArgs e)
        {
            // Уведомляем подписчиков (MandelbrotSelectorForm) об изменении масштаба лупы
            LoupeZoomChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Асинхронно рендерит и отображает множество Мандельброта на элементе mandelbrotCanvas.
        /// Используется для предпросмотра и выбора точки 'c' на главной форме.
        /// </summary>
        private void RenderAndDisplayMandelbrotSet()
        {
            // Проверка, что элемент управления mandelbrotCanvas существует и имеет корректные (положительные) размеры.
            if (mandelbrotCanvas1 == null || mandelbrotCanvas1.Width <= 0 || mandelbrotCanvas1.Height <= 0)
            {
                return; // Если нет, выходим из метода.
            }

            // Рендеринг множества Мандельброта во внутреннем методе.
            // Используются константные значения итераций для предпросмотра.
            Bitmap mandelbrotImage = RenderMandelbrotSetInternal(mandelbrotCanvas1.Width, mandelbrotCanvas1.Height, MANDELBROT_PREVIEW_ITERATIONS);

            // Потокобезопасное обновление изображения на элементе mandelbrotCanvas.
            // Проверяем, что элемент управления создан и не уничтожен.
            if (mandelbrotCanvas1.IsHandleCreated && !mandelbrotCanvas1.IsDisposed)
            {
                // Выполняем обновление UI в основном потоке с помощью Invoke.
                mandelbrotCanvas1.Invoke((Action)(() =>
                {
                    mandelbrotCanvas1.Image?.Dispose(); // Освобождение предыдущего изображения, если оно существует.
                    mandelbrotCanvas1.Image = mandelbrotImage; // Установка нового, отрендеренного изображения.
                    mandelbrotCanvas1.Invalidate(); // Запрос на перерисовку элемента управления (для отображения маркера, если он есть).
                }));
            }
            else
            {
                // Если элемент управления уже уничтожен (например, форма закрывается во время рендеринга),
                // просто освобождаем созданное изображение Bitmap.
                mandelbrotImage?.Dispose();
            }
        }

        /// <summary>
        /// Внутренний метод для рендеринга множества Мандельброта в объект Bitmap.
        /// </summary>
        /// <param name="canvasWidth">Ширина области рендеринга в пикселях.</param>
        /// <param name="canvasHeight">Высота области рендеринга в пикселях.</param>
        /// <param name="iterationsLimit">Максимальное количество итераций для расчета принадлежности точки множеству.</param>
        /// <returns>Объект Bitmap с изображением множества Мандельброта.</returns>
        private Bitmap RenderMandelbrotSetInternal(int canvasWidth, int canvasHeight, int iterationsLimit)
        {
            // Создание нового Bitmap указанных размеров с форматом 24 бита на пиксель (RGB).
            Bitmap bmp = new Bitmap(canvasWidth, canvasHeight, PixelFormat.Format24bppRgb);
            // Если размеры некорректны (меньше или равны 0), возвращаем пустой (черный) Bitmap.
            if (canvasWidth <= 0 || canvasHeight <= 0)
            {
                return bmp;
            }

            // Блокировка битов Bitmap для прямого доступа к его памяти.
            // Это необходимо для быстрой записи пиксельных данных.
            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, canvasWidth, canvasHeight), ImageLockMode.WriteOnly, bmp.PixelFormat);
            int stride = bmpData.Stride; // Ширина одной строки изображения в байтах (может включать байты выравнивания).
            IntPtr scan0 = bmpData.Scan0; // Указатель на начало данных изображения в памяти.
            int bytes = Math.Abs(stride) * canvasHeight; // Общее количество байт в изображении.
            byte[] buffer = new byte[bytes]; // Буфер для хранения данных всех пикселей изображения.

            // Диапазоны вещественной и мнимой частей для отображения множества Мандельброта.
            // Эти значения определены как константы класса.
            double reRange = MANDELBROT_MAX_RE - MANDELBROT_MIN_RE;
            double imRange = MANDELBROT_MAX_IM - MANDELBROT_MIN_IM;

            // Параллельный расчет пикселей для ускорения рендеринга.
            // Цикл выполняется для каждой строки изображения (y_coord).
            Parallel.For(0, canvasHeight, y_coord =>
            {
                int rowOffset = y_coord * stride; // Смещение в байтовом буфере для текущей строки y_coord.
                // Внутренний цикл по пикселям в текущей строке (x_coord).
                for (int x_coord = 0; x_coord < canvasWidth; x_coord++)
                {
                    // Преобразование экранных координат (x_coord, y_coord) в координаты на комплексной плоскости (c_re, c_im).
                    // Эти c_re, c_im будут параметром 'c0' в формуле Мандельброта z = z^2 + c0.
                    double c_re = MANDELBROT_MIN_RE + (x_coord / (double)canvasWidth) * reRange;
                    double c_im = MANDELBROT_MAX_IM - (y_coord / (double)canvasHeight) * imRange; // Y-координата инвертирована, так как экранные Y растут вниз, а мнимая ось обычно направлена вверх.

                    Complex c0 = new Complex(c_re, c_im); // Комплексное число c0 для текущей точки.
                    Complex z = Complex.Zero; // Начальное значение z для итераций (z0 = 0).
                    int iter = 0; // Счетчик итераций.

                    // Итерационный процесс для определения принадлежности точки множеству Мандельброта.
                    // Точка считается принадлежащей множеству, если последовательность z_n+1 = z_n^2 + c0
                    // не "уходит в бесконечность" (т.е. |z| остается меньше порога, обычно 2)
                    // за заданное число итераций (iterationsLimit).
                    while (iter < iterationsLimit && z.Magnitude < 2) // Порог выхода |z| < 2.
                    {
                        z = z * z + c0; // Формула Мандельброта.
                        iter++;
                    }

                    // Определение цвета пикселя в зависимости от количества итераций.
                    byte r_val, g_val, b_val; // Компоненты цвета (Red, Green, Blue).
                    if (iter == iterationsLimit)
                    {
                        // Точка принадлежит множеству (или очень медленно "уходит в бесконечность") - красим в черный.
                        r_val = g_val = b_val = 0;
                    }
                    else
                    {
                        // Точка не принадлежит множеству, цвет зависит от скорости "ухода в бесконечность" (количества итераций iter).
                        double t = (double)iter / iterationsLimit; // Нормализованное значение итераций (от 0 до 1).
                        // Простой градиент для окраски внешней области (от красного к желто-синему).
                        // Это примерная цветовая схема, может быть изменена для других визуальных эффектов.
                        if (t < 0.5) // Первая половина градиента
                        {
                            r_val = (byte)(t * 2 * 200); // Компонента Red
                            g_val = (byte)(t * 2 * 50);  // Компонента Green
                            b_val = (byte)(t * 2 * 30);  // Компонента Blue
                        }
                        else // Вторая половина градиента
                        {
                            t = (t - 0.5) * 2; // Нормализуем вторую половину диапазона t (снова от 0 до 1).
                            r_val = (byte)(200 + t * 55); // Компонента Red
                            g_val = (byte)(50 + t * 205);  // Компонента Green
                            b_val = (byte)(30 + t * 225);  // Компонента Blue
                        }
                    }
                    // Запись цвета пикселя в байтовый буфер.
                    // Формат пикселя Format24bppRgb обычно хранится как BGR (Blue, Green, Red).
                    int index = rowOffset + x_coord * 3; // 3 байта на пиксель (B, G, R).
                    buffer[index] = b_val;     // Blue
                    buffer[index + 1] = g_val; // Green
                    buffer[index + 2] = r_val; // Red
                }
            });

            // Копирование данных из байтового буфера (buffer) в память, выделенную для Bitmap (scan0).
            Marshal.Copy(buffer, 0, scan0, bytes);
            // Разблокировка битов Bitmap, делая его снова доступным для стандартных операций GDI+.
            bmp.UnlockBits(bmpData);
            return bmp; // Возвращаем созданный и заполненный Bitmap.
        }

        /// <summary>
        /// Обработчик события выбора координат из окна MandelbrotSelectorForm.
        /// Обновляет значения Re (вещественная часть 'c') и Im (мнимая часть 'c')
        /// в соответствующих элементах NumericUpDown на главной форме.
        /// </summary>
        /// <param name="re">Выбранная вещественная часть 'c'.</param>
        /// <param name="im">Выбранная мнимая часть 'c'.</param>
        private void MandelbrotCSelectorWindow_CoordinatesSelected(double re, double im)
        {
            // Ограничение полученных значений Re и Im диапазоном, доступным
            // в элементах NumericUpDown (nudRe, nudIm) на главной форме.
            re = Math.Max((double)nudRe1.Minimum, Math.Min((double)nudRe1.Maximum, re));
            im = Math.Max((double)nudIm1.Minimum, Math.Min((double)nudIm1.Maximum, im));

            bool changed = false; // Флаг, указывающий, изменились ли значения Re или Im.

            // Обновление значения Re в NumericUpDown (nudRe), если оно отличается от полученного.
            if (nudRe1.Value != (decimal)re)
            {
                nudRe1.Value = (decimal)re; // Установка нового значения. Это вызовет событие ValueChanged для nudRe.
                changed = true;
            }
            // Обновление значения Im в NumericUpDown (nudIm), если оно отличается от полученного.
            if (nudIm1.Value != (decimal)im)
            {
                nudIm1.Value = (decimal)im; // Установка нового значения. Это вызовет событие ValueChanged для nudIm.
                changed = true;
            }

            // Если значения Re или Im изменились (или были установлены впервые),
            // и элемент mandelbrotCanvas (для предпросмотра Мандельброта) существует и готов к отрисовке,
            // то необходимо обновить его отображение (для перерисовки маркера на нем).
            if (changed && mandelbrotCanvas1 != null && mandelbrotCanvas1.IsHandleCreated && !mandelbrotCanvas1.IsDisposed)
            {
                // Выполняем инвалидацию (запрос на перерисовку) в основном UI потоке.
                mandelbrotCanvas1.Invoke((Action)(() => mandelbrotCanvas1.Invalidate()));
            }
        }

        /// <summary>
        /// Обработчик события Paint для элемента mandelbrotCanvas (предпросмотр множества Мандельброта).
        /// Отрисовывает маркер (перекрестие) на текущих координатах (Re, Im), выбранных для параметра 'c',
        /// поверх изображения множества Мандельброта.
        /// </summary>
        private void mandelbrotCanvas_Paint(object sender, PaintEventArgs e)
        {
            // Проверка, что элемент управления mandelbrotCanvas существует и имеет корректные (положительные) размеры.
            if (mandelbrotCanvas1 == null || mandelbrotCanvas1.Width <= 0 || mandelbrotCanvas1.Height <= 0)
            {
                return;
            }

            // Диапазоны отображения множества Мандельброта (определены константами)
            // и текущие значения Re, Im (из nudRe, nudIm) для позиционирования маркера.
            double reRange = MANDELBROT_MAX_RE - MANDELBROT_MIN_RE;
            double imRange = MANDELBROT_MAX_IM - MANDELBROT_MIN_IM;
            double currentCRe = (double)nudRe1.Value; // Текущая вещественная часть 'c'
            double currentCIm = (double)nudIm1.Value; // Текущая мнимая часть 'c'

            // Проверка, что текущие координаты 'c' (currentCRe, currentCIm)
            // находятся в пределах отображаемой области множества Мандельброта.
            if (currentCRe >= MANDELBROT_MIN_RE && currentCRe <= MANDELBROT_MAX_RE &&
                currentCIm >= MANDELBROT_MIN_IM && currentCIm <= MANDELBROT_MAX_IM)
            {
                // Расчет экранных координат (markerX, markerY) для маркера на mandelbrotCanvas.
                // X-координата: ((значение Re - мин. Re) / диапазон Re) * ширина холста
                int markerX = (int)((currentCRe - MANDELBROT_MIN_RE) / reRange * mandelbrotCanvas1.Width);
                // Y-координата: ((макс. Im - значение Im) / диапазон Im) * высота холста
                // (инверсия для Y, так как мнимая ось обычно растет вверх, а экранные Y-координаты растут вниз).
                int markerY = (int)((MANDELBROT_MAX_IM - currentCIm) / imRange * mandelbrotCanvas1.Height);

                // Отрисовка перекрестия (маркера)
                using (Pen markerPen = new Pen(Color.FromArgb(200, Color.Green), 1.5f)) // Полупрозрачное зеленое перо толщиной 1.5f.
                {
                    // Горизонтальная линия маркера (через всю ширину холста на высоте markerY).
                    e.Graphics.DrawLine(markerPen, 0, markerY, mandelbrotCanvas1.Width, markerY);
                    // Вертикальная линия маркера (через всю высоту холста на позиции markerX).
                    e.Graphics.DrawLine(markerPen, markerX, 0, markerX, mandelbrotCanvas1.Height);
                }
            }
        }

        /// <summary>
        /// Обработчик клика по элементу mandelbrotCanvas (предпросмотр множества Мандельброта).
        /// Открывает (или активирует, если уже открыто) окно MandelbrotSelectorForm
        /// для интерактивного выбора точки 'c'.
        /// </summary>
        private void mandelbrotCanvas_Click(object sender, EventArgs e)
        {
            // Проверка, что элемент управления mandelbrotCanvas существует и имеет корректные размеры.
            if (mandelbrotCanvas1 == null || mandelbrotCanvas1.Width <= 0 || mandelbrotCanvas1.Height <= 0)
            {
                return;
            }

            // Преобразование EventArgs в MouseEventArgs для получения координат клика.
            MouseEventArgs mouseArgs = e as MouseEventArgs;
            if (mouseArgs == null) // Если событие было вызвано не кликом мыши (например, программно через PerformClick())
            {
                // Получаем текущие координаты мыши относительно контрола mandelbrotCanvas.
                Point clientPoint = mandelbrotCanvas1.PointToClient(Control.MousePosition);
                // Проверяем, нажата ли левая кнопка мыши (если клик эмулируется, это может быть не установлено).
                if ((Control.MouseButtons & MouseButtons.Left) != MouseButtons.Left)
                {
                    return; // Если не левая кнопка (или никакая не нажата), выходим.
                }
                // Создаем искусственный MouseEventArgs с текущими координатами и информацией о нажатой левой кнопке.
                mouseArgs = new MouseEventArgs(MouseButtons.Left, 1, clientPoint.X, clientPoint.Y, 0);
            }

            // Реагируем только на клик левой кнопкой мыши.
            if (mouseArgs.Button != MouseButtons.Left)
            {
                return;
            }

            // Получаем текущие значения Re и Im из NumericUpDown (nudRe, nudIm)
            // для передачи в окно выбора 'c' в качестве начальных.
            double initialRe = (double)nudRe1.Value;
            double initialIm = (double)nudIm1.Value;

            // Если окно выбора 'c' (mandelbrotCSelectorWindow) еще не создано или было закрыто (IsDisposed).
            if (mandelbrotCSelectorWindow == null || mandelbrotCSelectorWindow.IsDisposed)
            {
                // Создаем новый экземпляр окна выбора 'c' (MandelbrotSelectorForm).
                // Передаем ссылку на текущую форму (this) как владельца и начальные значения Re, Im.
                mandelbrotCSelectorWindow = new MandelbrotSelectorForm(this, initialRe, initialIm);
                // Подписываемся на событие CoordinatesSelected из окна выбора 'c',
                // чтобы обновить значения Re, Im на главной форме при выборе.
                mandelbrotCSelectorWindow.CoordinatesSelected += MandelbrotCSelectorWindow_CoordinatesSelected;
                // При закрытии окна выбора 'c', сбрасываем ссылку на него (mandelbrotCSelectorWindow = null),
                // чтобы при следующем клике было создано новое окно.
                mandelbrotCSelectorWindow.FormClosed += (s, args) => { mandelbrotCSelectorWindow = null; };
                // Отображаем окно выбора 'c' как немодальное (Show), с главной формой в качестве владельца (this).
                mandelbrotCSelectorWindow.Show(this);
            }
            else
            {
                // Если окно выбора 'c' уже существует, активируем его (переводим на передний план).
                mandelbrotCSelectorWindow.Activate();
                // И обновляем в нем выбранные координаты (на случай, если они изменились на главной форме
                // с момента последнего открытия/активации окна выбора 'c').
                // true указывает, что событие CoordinatesSelected в MandelbrotSelectorForm может быть вызвано,
                // что, в свою очередь, может обновить главную форму (хотя здесь это скорее для консистентности внутреннего состояния селектора).
                mandelbrotCSelectorWindow.SetSelectedCoordinates(initialRe, initialIm, true);
            }

            // Обновление отображения маркера на mandelbrotCanvas (на главной форме),
            // чтобы он соответствовал текущим (возможно, только что установленным) значениям Re и Im.
            // Проверяем, что mandelbrotCanvas существует и готов к отрисовке.
            if (mandelbrotCanvas1.IsHandleCreated && !mandelbrotCanvas1.IsDisposed)
            {
                mandelbrotCanvas1.Invalidate(); // Запрос на перерисовку mandelbrotCanvas.
            }
        }

        /// <summary>
        /// Обработчик изменения состояния (CheckedChanged) чекбоксов выбора палитры.
        /// Обеспечивает выбор только одной палитры одновременно (поведение, аналогичное радио-кнопкам).
        /// </summary>
        private void PaletteCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox currentCb = sender as CheckBox; // Чекбокс, вызвавший событие.
            if (currentCb == null) // Если приведение типа не удалось, выходим.
            {
                return;
            }

            // Временно отписываемся от событий изменения состояния всех чекбоксов палитр,
            // чтобы избежать рекурсивных вызовов этого обработчика при программном изменении их состояния (Checked).
            foreach (var cb in paletteCheckBoxes.Where(cb => cb != null))
            {
                cb.CheckedChanged -= PaletteCheckBox_CheckedChanged;
            }

            if (currentCb.Checked) // Если текущий чекбокс был выбран (установлена галочка):
            {
                // Запоминаем его как последний выбранный.
                lastSelectedPaletteCheckBox = currentCb;
                // Снимаем выбор (галочки) со всех остальных чекбоксов палитр в группе.
                foreach (var cb in paletteCheckBoxes.Where(cb => cb != null && cb != currentCb))
                {
                    cb.Checked = false;
                }
            }
            else // Если с текущего чекбокса был снят выбор (снята галочка):
            {
                // Пытаемся найти другой активный (выбранный) чекбокс.
                // Это важно, если пользователь снимает галочку с единственного выбранного чекбокса.
                // В этом случае lastSelectedPaletteCheckBox станет null, что приведет к использованию палитры по умолчанию.
                lastSelectedPaletteCheckBox = paletteCheckBoxes.FirstOrDefault(cb => cb != null && cb.Checked);
            }

            // Подписываемся обратно на события изменения состояния для всех чекбоксов палитр.
            foreach (var cb in paletteCheckBoxes.Where(cb => cb != null))
            {
                cb.CheckedChanged += PaletteCheckBox_CheckedChanged;
            }

            // Обновляем состояние доступности (Enabled) чекбокса "colorBox" (старая цветная палитра)
            // в зависимости от выбора других палитр.
            HandleColorBoxEnableState();
            // Планируем перерисовку фрактала, так как изменилась выбранная палитра.
            ScheduleRender();
        }

        /// <summary>
        /// Управляет состоянием доступности (Enabled) чекбокса "colorBox" (старая цветная палитра)
        /// в зависимости от выбора других палитр.
        /// "colorBox" должен быть доступен, если выбрана одна из "новых" палитр (checkBox1-6)
        /// или если он сам выбран и не выбран Ч/Б режим ("oldRenderBW").
        /// </summary>
        private void HandleColorBoxEnableState()
        {
            // Проверка на существование необходимых контролов (массив палитр, colorBox, oldRenderBW).
            if (paletteCheckBoxes == null || colorBox == null || oldRenderBW == null)
            {
                return;
            }

            // Проверяем, выбран ли какой-либо из "новых" чекбоксов палитр.
            // "Новые" палитры начинаются с третьего элемента в массиве paletteCheckBoxes (индекс 2).
            // paletteCheckBoxes[0] = colorBox, paletteCheckBoxes[1] = oldRenderBW.
            bool isAnyNewPaletteCbChecked = paletteCheckBoxes.Skip(2).Any(cb => cb != null && cb.Checked);

            if (isAnyNewPaletteCbChecked)
            {
                // Если выбрана одна из новых палитр (checkBox1-6), то "colorBox" (старая цветная)
                // становится доступен для выбора. Это позволяет пользователю переключиться
                // на старую цветную палитру, если он пробует новые.
                colorBox.Enabled = true;
            }
            else if (colorBox.Checked && !oldRenderBW.Checked)
            {
                // Если "colorBox" уже выбран и при этом не выбран Ч/Б режим ("oldRenderBW"),
                // то "colorBox" остается доступным (пользователь может снять с него галочку).
                colorBox.Enabled = true;
            }
            else
            {
                // В остальных случаях (например, если выбран "oldRenderBW", или не выбрана ни одна из новых палитр,
                // и "colorBox" не выбран), доступность "colorBox" определяется тем, не выбран ли Ч/Б режим.
                // Если Ч/Б режим ("oldRenderBW") выбран, "colorBox" должен быть недоступен.
                colorBox.Enabled = !oldRenderBW.Checked;
            }
        }

        /// <summary>
        /// Обработчик изменения размера формы. Вызывает метод ResizeCanvas для адаптации области отрисовки.
        /// </summary>
        private void Form1_Resize(object sender, EventArgs e) => ResizeCanvas();

        /// <summary>
        /// Обработчик изменения размера области отрисовки (canvas). Вызывает метод ResizeCanvas.
        /// </summary>
        private void Canvas_Resize(object sender, EventArgs e) => ResizeCanvas();

        /// <summary>
        /// Обновляет внутренние переменные ширины и высоты (width, height) области отрисовки
        /// на основе текущих размеров элемента canvas и планирует перерисовку фрактала с новыми размерами.
        /// </summary>
        private void ResizeCanvas()
        {
            // Не изменяем размер и не перерисовываем, если в данный момент идет рендеринг
            // в высоком разрешении (для сохранения файла), чтобы избежать конфликтов.
            if (isHighResRendering)
            {
                return;
            }
            // Проверка на корректность (положительность) размеров элемента canvas.
            // Если размеры некорректны, выходим из метода.
            if (canvas1.Width <= 0 || canvas1.Height <= 0)
            {
                return;
            }
            // Обновление внутренних переменных, хранящих текущие размеры области отрисовки.
            width = canvas1.Width;
            height = canvas1.Height;
            // Планирование перерисовки фрактала, так как размеры области отрисовки изменились.
            ScheduleRender();
        }

        /// <summary>
        /// Обработчик изменения значений в элементах управления параметрами фрактала
        /// (NumericUpDown для Re, Im, Iterations, Threshold, Zoom; ComboBox для Threads).
        /// </summary>
        private void ParamControl_Changed(object sender, EventArgs e)
        {
            // Не обрабатываем изменения параметров, если идет рендеринг в высоком разрешении (сохранение файла).
            if (isHighResRendering)
            {
                return;
            }

            // Если изменился масштаб (zoom) через соответствующий NumericUpDown (nudZoom).
            if (sender == nudZoom)
            {
                // *** ИЗМЕНЕНИЯ ДЛЯ ZOOM ***
                // Ограничиваем значение zoom в допустимых пределах, используя свойства Minimum/Maximum контрола
                // Было: zoom = Math.Max(1, Math.Min((double)nudZoom.Maximum, (double)nudZoom.Value));
                zoom = Math.Max((double)nudZoom.Minimum, Math.Min((double)nudZoom.Maximum, (double)nudZoom.Value));

                // Обновляем значение в элементе управления nudZoom, если оно было скорректировано.
                nudZoom.Value = (decimal)zoom;
            }

            // Если изменились параметры Re (nudRe) или Im (nudIm),
            // и элемент mandelbrotCanvas (для предпросмотра Мандельброта на главной форме) существует,
            // то необходимо обновить его отображение (перерисовать маркер выбранной точки 'c').
            if (mandelbrotCanvas1 != null && (sender == nudRe1 || sender == nudIm1))
            {
                // Проверяем, что mandelbrotCanvas создан и не уничтожен.
                if (mandelbrotCanvas1.IsHandleCreated && !mandelbrotCanvas1.IsDisposed)
                {
                    mandelbrotCanvas1.Invalidate(); // Запрос на перерисовку маркера на mandelbrotCanvas.
                }
            }
            // Планируем перерисовку фрактала с новыми параметрами.
            ScheduleRender();
        }

        /// <summary>
        /// Планирует рендеринг предварительного просмотра фрактала с небольшой задержкой (через таймер renderTimer).
        /// Если предыдущий запланированный рендеринг еще не начался (таймер не сработал), он отменяется.
        /// Это сделано для того, чтобы не запускать рендеринг на каждое мелкое изменение параметра
        /// (например, при быстром вводе числа в NumericUpDown или быстром вращении колеса мыши).
        /// </summary>
        private void ScheduleRender()
        {
            // Не планируем новый рендеринг, если идет процесс сохранения в высоком разрешении.
            if (isHighResRendering) return;

            // Отменяем предыдущую активную или запланированную асинхронную задачу рендеринга, если она есть.
            // CancellationTokenSource (previewRenderCts) используется для этой отмены.
            previewRenderCts?.Cancel();
            renderTimer.Stop();         // Останавливаем таймер (если он был запущен).
            renderTimer.Start();        // Запускаем таймер заново. Рендеринг начнется по событию Tick таймера.
        }

        /// <summary>
        /// Обработчик тика таймера renderTimer. Запускает асинхронный рендеринг предварительного просмотра фрактала.
        /// Срабатывает после небольшой задержки, установленной в ScheduleRender.
        /// </summary>
        private async void RenderTimer_Tick(object sender, EventArgs e)
        {
            renderTimer.Stop(); // Останавливаем таймер, так как он сработал, и его задача - инициировать рендеринг.

            // Не рендерим, если идет сохранение в высоком разрешении.
            if (isHighResRendering) return;

            // Если рендеринг предварительного просмотра уже идет (флаг isRenderingPreview установлен),
            // то перезапускаем таймер. Это означает, что пользователь продолжает изменять параметры,
            // и мы подождем завершения текущего рендеринга, а затем (если таймер снова сработает)
            // выполним новый рендеринг с последними параметрами.
            // Это предотвращает запуск нескольких одновременных рендерингов предпросмотра.
            if (isRenderingPreview)
            {
                renderTimer.Start(); // Перезапускаем таймер, чтобы отложить следующий рендеринг.
                return;
            }

            isRenderingPreview = true; // Устанавливаем флаг, что рендеринг предварительного просмотра начался.

            previewRenderCts?.Dispose(); // Освобождаем ресурсы предыдущего CancellationTokenSource, если он был.
            previewRenderCts = new CancellationTokenSource(); // Создаем новый CancellationTokenSource для возможности отмены текущего рендеринга.
            CancellationToken token = previewRenderCts.Token; // Получаем токен отмены из источника.

            UpdateParameters(); // Обновляем параметры фрактала (c, maxIterations, threshold, threadCount) из элементов управления.

            // Захватываем текущие значения центра (centerX, centerY) и масштаба (zoom) для передачи в метод рендеринга.
            // Это важно, так как пользователь может изменить их (например, панорамированием или масштабированием)
            // во время выполнения асинхронного рендеринга. Мы рендерим для того состояния, которое было на момент начала.
            double currentRenderCenterX = centerX;
            double currentRenderCenterY = centerY;
            double currentRenderZoom = zoom;

            try
            {
                // Запускаем рендеринг фрактала в фоновом потоке (с помощью Task.Run) с возможностью отмены (через token).
                // Передаем захваченные параметры центра и масштаба (currentRenderCenterX, currentRenderCenterY, currentRenderZoom).
                await Task.Run(() => RenderFractal(token, currentRenderCenterX, currentRenderCenterY, currentRenderZoom), token);
            }
            catch (OperationCanceledException)
            {
                // Рендеринг был отменен (например, пользователь снова изменил параметры, и ScheduleRender вызвал previewRenderCts.Cancel()).
                // Это нормальное ожидаемое поведение, никаких дополнительных действий не требуется.
            }
            catch (Exception ex)
            {
                // Обработка других непредвиденных исключений, которые могли возникнуть во время рендеринга.
                // В реальном приложении здесь могло бы быть логирование ошибки (например, Console.WriteLine($"Render Error: {ex.Message}")).
                // Согласно задаче, отладочный вывод был удален.
            }
            finally
            {
                isRenderingPreview = false; // Сбрасываем флаг по завершении рендеринга (независимо от того, был он успешным, отмененным или с ошибкой).
            }
        }

        /// <summary>
        /// Обновляет поля класса, хранящие параметры фрактала (c, maxIterations, threshold, threadCount),
        /// значениями из соответствующих элементов управления на форме (NumericUpDown, ComboBox).
        /// </summary>
        private void UpdateParameters()
        {
            // Установка комплексного числа 'c' (константа для множества Жюлиа)
            // на основе значений из NumericUpDown nudRe и nudIm.
            c = new Complex((double)nudRe1.Value, (double)nudIm1.Value);
            // Установка максимального количества итераций из NumericUpDown nudIterations.
            maxIterations = (int)nudIterations1.Value;
            // Установка порога для определения "ухода в бесконечность" из NumericUpDown nudThreshold.
            threshold = (double)nudThreshold1.Value;
            // Установка количества потоков для параллельного рендеринга из ComboBox cbThreads.
            // Если выбрано "Auto", используем количество логических процессоров системы (Environment.ProcessorCount).
            // Иначе, используем выбранное пользователем числовое значение, преобразованное в int.
            threadCount = cbThreads1.SelectedItem.ToString() == "Auto"
                ? Environment.ProcessorCount
                : Convert.ToInt32(cbThreads1.SelectedItem);
        }

        /// <summary>
        /// Делегат для функции, определяющей цвет пикселя на основе параметров итерации и выбранной палитры.
        /// </summary>
        /// <param name="t">Нормализованное значение (обычно от 0 до 1), используемое для градиента или другой логики цвета. Может быть линейным или логарифмическим.</param>
        /// <param name="iter">Текущее количество итераций, выполненное для данного пикселя.</param>
        /// <param name="maxIterations">Максимальное количество итераций, установленное пользователем (общий предел для расчета фрактала).</param>
        /// <param name="maxColorIter">Максимальное количество итераций, используемое для градации цвета (может быть меньше maxIterations для лучшей детализации цветов).</param>
        /// <returns>Цвет пикселя (объект Color).</returns>
        private delegate Color PaletteFunction(double t, int iter, int maxIterations, int maxColorIter);

        /// <summary>
        /// Определяет цвет пикселя на основе количества итераций (iter) и выбранной цветовой палитры.
        /// </summary>
        /// <param name="iter">Количество итераций, выполненное для данного пикселя до "ухода в бесконечность" или достижения предела.</param>
        /// <param name="currentMaxIterations">Общее максимальное количество итераций, используемое для расчета фрактала (параметр с формы).</param>
        /// <param name="currentMaxColorIter">Максимальное количество итераций, используемое для градации цвета (обычно константа, например 1000). Это помогает получить более плавные цветовые переходы, особенно если currentMaxIterations очень велико.</param>
        /// <returns>Вычисленный цвет пикселя (объект Color).</returns>
        private Color GetPixelColor(int iter, int currentMaxIterations, int currentMaxColorIter)
        {
            // Если точка принадлежит множеству (т.е. достигнуто максимальное количество итераций currentMaxIterations
            // без "ухода в бесконечность"), то цвет зависит от выбранной палитры.
            if (iter == currentMaxIterations)
            {
                // Для палитры "checkBox6" (пастельная) используется специальный цвет фона (темно-серый),
                // для всех остальных палитр точки внутри множества красятся в черный.
                return (lastSelectedPaletteCheckBox?.Name == "checkBox6") ? Color.FromArgb(50, 50, 50) : Color.Black;
            }

            // Нормализация значения итераций для использования в цветовых схемах.
            // t_capped: линейная нормализация количества итераций iter до currentMaxColorIter.
            // Значение iter ограничивается сверху currentMaxColorIter, затем делится на него.
            // Используется для большинства палитр, чтобы обеспечить предсказуемый диапазон (0-1) для цветовых функций.
            double t_capped = (double)Math.Min(iter, currentMaxColorIter) / currentMaxColorIter;
            // t_log: логарифмическая нормализация количества итераций iter до currentMaxColorIter.
            // Math.Log(iter + 1) используется, чтобы избежать log(0). Значение нормализуется делением на Math.Log(currentMaxColorIter + 1).
            // Используется для стандартной палитры (оттенки серого), чтобы сделать переходы более плавными
            // и лучше отобразить детали в областях с большим количеством итераций (где iter медленно растет).
            double t_log = Math.Log(Math.Min(iter, currentMaxColorIter) + 1) / Math.Log(currentMaxColorIter + 1);

            // Выбор функции палитры по умолчанию (стандартная палитра "оттенки серого" - GetDefaultPaletteColor).
            PaletteFunction selectedPaletteFunc = GetDefaultPaletteColor;

            // Определение конкретной функции палитры на основе выбранного пользователем чекбокса (lastSelectedPaletteCheckBox).
            if (lastSelectedPaletteCheckBox != null) // Если какой-либо чекбокс палитры выбран
            {
                // Сопоставление выбранного чекбокса с соответствующей функцией палитры.
                if (lastSelectedPaletteCheckBox == colorBox) selectedPaletteFunc = GetPaletteColorBoxColor;         // Старая цветная палитра (HSV-градиент)
                else if (lastSelectedPaletteCheckBox == oldRenderBW) selectedPaletteFunc = GetPaletteOldBWColor;    // Старая Ч/Б палитра (линейный градиент серого)
                else if (lastSelectedPaletteCheckBox.Name == "checkBox1") selectedPaletteFunc = GetPalette1Color;   // Палитра "Огонь"
                else if (lastSelectedPaletteCheckBox.Name == "checkBox2") selectedPaletteFunc = GetPalette2Color;   // Палитра "Вода/Лед"
                else if (lastSelectedPaletteCheckBox.Name == "checkBox3") selectedPaletteFunc = GetPalette3Color;   // Палитра "Психоделическая"
                else if (lastSelectedPaletteCheckBox.Name == "checkBox4") selectedPaletteFunc = GetPalette4Color;   // Палитра "Контрастная"
                else if (lastSelectedPaletteCheckBox.Name == "checkBox5") selectedPaletteFunc = GetPalette5Color;   // Палитра "Металлическая серая"
                else if (lastSelectedPaletteCheckBox.Name == "checkBox6") selectedPaletteFunc = GetPalette6Color;   // Палитра "Пастельная"
            }

            // Выбор параметра 't' (нормализованное значение итераций) для передачи в функцию палитры.
            // Для стандартной палитры (GetDefaultPaletteColor) используется логарифмическая шкала (t_log),
            // так как она лучше подходит для оттенков серого и выявления деталей.
            // Для всех остальных кастомных палитр используется линейная шкала (t_capped).
            double t_param = (selectedPaletteFunc == GetDefaultPaletteColor) ? t_log : t_capped;
            // Вызов выбранной функции палитры для получения цвета.
            // Передаются: нормализованное значение t_param, абсолютное количество итераций iter,
            // общее макс. итераций currentMaxIterations и макс. итераций для цвета currentMaxColorIter.
            return selectedPaletteFunc(t_param, iter, currentMaxIterations, currentMaxColorIter);
        }


        // --- Методы для различных цветовых палитр ---
        // Каждый такой метод принимает нормализованный параметр t (t_log или t_capped),
        // а также iter, maxIter (currentMaxIterations), maxClrIter (currentMaxColorIter)
        // и возвращает объект Color.

        /// <summary>
        /// Палитра по умолчанию: оттенки серого на основе логарифмической шкалы итераций (t_log).
        /// </summary>
        /// <param name="t_log">Нормализованное логарифмическое значение итераций (0-1). t_log=0 для iter=0, t_log=1 для iter=maxClrIter.</param>
        /// <param name="iter">Абсолютное количество итераций.</param>
        /// <param name="maxIter">Максимальное количество итераций для расчета фрактала.</param>
        /// <param name="maxClrIter">Максимальное количество итераций для цветовой шкалы.</param>
        /// <returns>Цвет в оттенках серого. Чем больше итераций (больше t_log), тем темнее цвет (из-за 1 - t_log).</returns>
        private Color GetDefaultPaletteColor(double t_log, int iter, int maxIter, int maxClrIter)
        {
            // (1 - t_log) инвертирует значение:
            // Если iter=0 (t_log=0), то (1-t_log)=1, cVal=255 (белый).
            // Если iter=maxClrIter (t_log=1), то (1-t_log)=0, cVal=0 (черный).
            // То есть, точки, "убегающие" быстрее (меньше iter), будут светлее.
            int cVal = (int)(255.0 * (1 - t_log));
            return Color.FromArgb(cVal, cVal, cVal); // Все компоненты (R,G,B) равны, что дает оттенок серого.
        }

        /// <summary>
        /// Палитра "ColorBox": использует HSV-градиент, где оттенок (Hue) зависит от количества итераций (t_capped).
        /// </summary>
        /// <param name="t_capped">Нормализованное линейное значение итераций (0-1).</param>
        /// <param name="iter">Абсолютное количество итераций.</param>
        /// <param name="maxIter">Максимальное количество итераций для расчета фрактала.</param>
        /// <param name="maxClrIter">Максимальное количество итераций для цветовой шкалы.</param>
        /// <returns>Цвет из HSV-градиента. Проходит по всему цветовому спектру.</returns>
        private Color GetPaletteColorBoxColor(double t_capped, int iter, int maxIter, int maxClrIter)
        {
            // Оттенок (Hue) изменяется от 0 до 360 градусов в зависимости от t_capped (0 до 1).
            // Насыщенность (Saturation) установлена в 0.6 (не максимальная, чтобы цвета были не слишком "кислотными").
            // Яркость (Value) установлена в 1.0 (максимальная яркость).
            return ColorFromHSV(360.0 * t_capped, 0.6, 1.0);
        }

        /// <summary>
        /// Старая черно-белая палитра: линейный градиент от белого к черному на основе t_capped.
        /// </summary>
        /// <param name="t_capped">Нормализованное линейное значение итераций (0-1).</param>
        /// <param name="iter">Абсолютное количество итераций.</param>
        /// <param name="maxIter">Максимальное количество итераций для расчета фрактала.</param>
        /// <param name="maxClrIter">Максимальное количество итераций для цветовой шкалы.</param>
        /// <returns>Цвет в оттенках серого. Чем больше итераций (больше t_capped), тем темнее цвет.</returns>
        private Color GetPaletteOldBWColor(double t_capped, int iter, int maxIter, int maxClrIter)
        {
            // t_capped = 0 (min iter для цвета) -> cVal = 255 (белый).
            // t_capped = 1 (maxClrIter) -> cVal = 0 (черный).
            // Точки, "убегающие" быстрее (меньше iter, меньше t_capped), будут светлее.
            int cVal = 255 - (int)(255.0 * t_capped);
            return Color.FromArgb(cVal, cVal, cVal);
        }

        /// <summary>
        /// Выполняет линейную интерполяцию между двумя цветами (a и b).
        /// </summary>
        /// <param name="a">Начальный цвет (при t=0).</param>
        /// <param name="b">Конечный цвет (при t=1).</param>
        /// <param name="t">Параметр интерполяции (от 0 до 1). 0 соответствует цвету 'a', 1 - цвету 'b'.</param>
        /// <returns>Интерполированный цвет. Если t выходит за пределы [0,1], он ограничивается этим диапазоном.</returns>
        private Color LerpColor(Color a, Color b, double t)
        {
            // Ограничение параметра t диапазоном [0, 1] для корректной интерполяции.
            t = Math.Max(0, Math.Min(1, t));
            // Линейная интерполяция для каждой компоненты цвета (Red, Green, Blue).
            // Формула: результат = начальное_значение + (конечное_значение - начальное_значение) * t
            return Color.FromArgb(
                (int)(a.R + (b.R - a.R) * t), // Интерполяция для красного компонента
                (int)(a.G + (b.G - a.G) * t), // Интерполяция для зеленого компонента
                (int)(a.B + (b.B - a.B) * t)  // Интерполяция для синего компонента
            );
        }

        /// <summary>
        /// Палитра 1: "Огненные тона". Градиент от черного через темно-красный, оранжевый, светло-желтый к белому.
        /// Использует многоступенчатую линейную интерполяцию (LerpColor).
        /// </summary>
        private Color GetPalette1Color(double t, int iter, int maxIter, int maxClrIter)
        {
            // Определяем опорные цвета для градиента "огня".
            Color c1 = Color.Black;            // Начальный цвет (для t=0)
            Color c2 = Color.FromArgb(200, 0, 0);    // Темно-красный
            Color c3 = Color.FromArgb(255, 100, 0);  // Оранжевый
            Color c4 = Color.FromArgb(255, 255, 100); // Светло-желтый
            Color c5 = Color.White;            // Конечный цвет (для t=1)

            // Многоступенчатая линейная интерполяция между опорными цветами в зависимости от значения t.
            // Диапазон t (0-1) делится на 4 сегмента по 0.25 каждый.
            if (t < 0.25) return LerpColor(c1, c2, t / 0.25);                   // Сегмент 1: от c1 к c2
            if (t < 0.50) return LerpColor(c2, c3, (t - 0.25) / 0.25);          // Сегмент 2: от c2 к c3
            if (t < 0.75) return LerpColor(c3, c4, (t - 0.50) / 0.25);           // Сегмент 3: от c3 к c4
            return LerpColor(c4, c5, (t - 0.75) / 0.25);                        // Сегмент 4: от c4 к c5 (для t >= 0.75)
        }

        /// <summary>
        /// Палитра 2: "Сине-голубые тона" (вода/лед). Градиент от черного через темно-синий, голубой, светло-голубой к белому.
        /// Использует многоступенчатую линейную интерполяцию.
        /// </summary>
        private Color GetPalette2Color(double t, int iter, int maxIter, int maxClrIter)
        {
            // Опорные цвета для "водной/ледяной" палитры.
            Color c1 = Color.Black;
            Color c2 = Color.FromArgb(0, 0, 100);    // Темно-синий
            Color c3 = Color.FromArgb(0, 120, 200);  // Голубой
            Color c4 = Color.FromArgb(170, 220, 255); // Светло-голубой
            Color c5 = Color.White;

            // Аналогичная многоступенчатая интерполяция, как в GetPalette1Color.
            if (t < 0.25) return LerpColor(c1, c2, t / 0.25);
            if (t < 0.50) return LerpColor(c2, c3, (t - 0.25) / 0.25);
            if (t < 0.75) return LerpColor(c3, c4, (t - 0.50) / 0.25);
            return LerpColor(c4, c5, (t - 0.75) / 0.25);
        }

        /// <summary>
        /// Палитра 3: "Психоделические цвета" на основе синусоидальных функций для RGB компонент.
        /// Создает плавные, циклические переливы различных цветов.
        /// </summary>
        private Color GetPalette3Color(double t, int iter, int maxIter, int maxClrIter)
        {
            // Генерация RGB компонентов с помощью синусоидальных функций.
            // Параметр 't' (0-1) отображается на аргумент синуса.
            // Различные фазовые сдвиги (0, Math.PI*2/3, Math.PI*4/3) для R, G, B компонент
            // создают цветовые волны.
            // Коэффициенты (например, * Math.PI * 3.0) и амплитуда/смещение (* 0.45 + 0.5)
            // подобраны для получения определенного визуального эффекта.
            // Результат каждой синусоиды находится в диапазоне [0.05, 0.95], который затем масштабируется до 0-255.
            double r_comp = Math.Sin(t * Math.PI * 3.0 + 0.5) * 0.45 + 0.5;                     // Компонента Red
            double g_comp = Math.Sin(t * Math.PI * 3.0 + Math.PI * 2.0 / 3.0 + 0.5) * 0.45 + 0.5; // Компонента Green (сдвиг фазы на 120 градусов)
            double b_comp = Math.Sin(t * Math.PI * 3.0 + Math.PI * 4.0 / 3.0 + 0.5) * 0.45 + 0.5; // Компонента Blue (сдвиг фазы на 240 градусов)
            // Преобразование нормализованных значений компонент (0-1, хотя здесь скорее 0.05-0.95) в диапазон 0-255 для цвета.
            return Color.FromArgb((int)(r_comp * 255), (int)(g_comp * 255), (int)(b_comp * 255));
        }

        /// <summary>
        /// Палитра 4: "Контрастные переходы". Использует резкие переходы между несколькими цветами
        /// (темно-фиолетовый, пурпурный, циановый, лавандовый) путем "ломаной" интерполяции.
        /// </summary>
        private Color GetPalette4Color(double t, int iter, int maxIter, int maxClrIter)
        {
            // Опорные цвета для контрастной палитры.
            Color c1 = Color.FromArgb(10, 0, 20);     // Очень темно-фиолетовый (почти черный)
            Color c2 = Color.FromArgb(255, 0, 255);   // Пурпурный (Magenta)
            Color c3 = Color.FromArgb(0, 255, 255);   // Циановый (Cyan)
            Color c4 = Color.FromArgb(230, 230, 250); // Лавандовый (светлый)

            // Сложная многоступенчатая интерполяция, включая "обратные" переходы (например, от c2 обратно к c1),
            // для создания эффекта полос или резких смен цвета.
            // Каждый 'if' блок обрабатывает свой сегмент диапазона t (0-1).
            if (t < 0.1) return LerpColor(c1, c2, t / 0.1);                   // Сегмент 1 (0.0 - 0.1): от c1 к c2 (быстро)
            if (t < 0.4) return LerpColor(c2, c1, (t - 0.1) / 0.3);         // Сегмент 2 (0.1 - 0.4): от c2 обратно к c1 (медленнее)
            if (t < 0.5) return LerpColor(c1, c3, (t - 0.4) / 0.1);         // Сегмент 3 (0.4 - 0.5): от c1 к c3 (быстро)
            if (t < 0.8) return LerpColor(c3, c1, (t - 0.5) / 0.3);         // Сегмент 4 (0.5 - 0.8): от c3 обратно к c1 (медленнее)
            return LerpColor(c1, c4, (t - 0.8) / 0.2);                      // Сегмент 5 (0.8 - 1.0): от c1 к c4 (средне)
        }

        /// <summary>
        /// Палитра 5: "Металлическая" серая палитра с эффектом блеска.
        /// Создает оттенки серого с волнами яркости и легким синим отливом.
        /// </summary>
        private Color GetPalette5Color(double t, int iter, int maxIter, int maxClrIter)
        {
            // Базовый серый цвет, линейно изменяющийся от темного (50) к светлому (200) в зависимости от t.
            int baseGray = 50 + (int)(t * 150);
            // Эффект "блеска" (осветление/затемнение) с помощью синусоиды.
            // Math.Sin(t * Math.PI * 5) создает 2.5 полных волны (5 полуволн) по диапазону t.
            // Амплитуда волны яркости составляет +/- 40.
            double shine = Math.Sin(t * Math.PI * 5);
            // Итоговый серый цвет с учетом блеска, ограниченный диапазоном 0-255.
            int finalGray = Math.Max(0, Math.Min(255, baseGray + (int)(shine * 40)));
            // Добавляем немного синего оттенка, который усиливается с ростом t (количества итераций).
            // Это придает "холодный металлический" оттенок. Синяя компонента не превышает 255.
            return Color.FromArgb(finalGray, finalGray, Math.Min(255, finalGray + (int)(t * 25)));
        }

        /// <summary>
        /// Палитра 6: "Пастельные тона". Использует преобразование HSV (Hue, Saturation, Value)
        /// для создания мягких, пастельных цветов с плавным изменением оттенка, насыщенности и яркости.
        /// </summary>
        private Color GetPalette6Color(double t, int iter, int maxIter, int maxClrIter)
        {
            // Оттенок (Hue) циклически изменяется в диапазоне.
            // (t * 200.0) дает диапазон 0-200. Добавление 180.0 сдвигает его к 180-380 (сине-зеленый -> фиолетовый -> красный -> оранжевый).
            // Операция % 360.0 возвращает оттенок в стандартный диапазон 0-360 градусов.
            double hue = (t * 200.0 + 180.0) % 360.0;
            // Насыщенность (Saturation) колеблется в пределах, создавая мягкость (не слишком яркие, не слишком блеклые цвета).
            // 0.35 - базовый уровень насыщенности. Math.Sin(t * Math.PI * 2) добавляет колебания +/- 0.1.
            // Результат ограничивается диапазоном [0.2, 0.6] для пастельных тонов.
            double sat = Math.Max(0.2, Math.Min(0.6, 0.35 + (Math.Sin(t * Math.PI * 2) * 0.1)));
            // Значение/Яркость (Value) также колеблется, избегая слишком темных или слишком ярких (почти белых) тонов.
            // 0.80 - базовый уровень яркости. Math.Cos(t * Math.PI * 2.5) добавляет колебания +/- 0.15.
            // Результат ограничивается диапазоном [0.7, 0.95].
            double val = Math.Max(0.7, Math.Min(0.95, 0.80 + (Math.Cos(t * Math.PI * 2.5) * 0.15)));
            // Преобразование вычисленных HSV-компонент в цвет RGB.
            return ColorFromHSV(hue, sat, val);
        }


        /// <summary>
        /// Основной метод рендеринга фрактала Жюлиа для предварительного просмотра на элементе canvas.
        /// Выполняется асинхронно и может быть отменен с помощью CancellationToken.
        /// </summary>
        /// <param name="token">Токен отмены для прерывания рендеринга, если пользователь изменил параметры или закрыл форму.</param>
        /// <param name="renderCenterX">Центральная точка по оси X (вещественная часть) для рендеринга. Зафиксирована на момент начала рендеринга.</param>
        /// <param name="renderCenterY">Центральная точка по оси Y (мнимая часть) для рендеринга. Зафиксирована на момент начала рендеринга.</param>
        /// <param name="renderZoom">Уровень масштабирования для рендеринга. Зафиксирован на момент начала рендеринга.</param>
        private void RenderFractal(CancellationToken token, double renderCenterX, double renderCenterY, double renderZoom)
        {
            // Проверка, не был ли запрошен токен отмены в самом начале выполнения метода.
            // Если да, то немедленно выходим, чтобы не выполнять лишнюю работу.
            if (token.IsCancellationRequested) return;

            // Проверка на случай, если рендеринг начался во время сохранения в высоком разрешении (isHighResRendering)
            // или если размеры области отрисовки (canvas) некорректны (меньше или равны 0).
            // В таких случаях рендеринг предпросмотра не выполняется.
            if (isHighResRendering || canvas1.Width <= 0 || canvas1.Height <= 0)
            {
                return;
            }

            Bitmap bmp = null; // Bitmap, в который будет производиться рендеринг. Инициализируется null.
            BitmapData bmpData = null; // Объект для прямого доступа к данным пикселей Bitmap. Инициализируется null.

            try
            {
                // Создание нового Bitmap для рендеринга с текущими размерами области (width, height)
                // и форматом 24 бита на пиксель (PixelFormat.Format24bppRgb).
                bmp = new Bitmap(width, height, PixelFormat.Format24bppRgb);
                token.ThrowIfCancellationRequested(); // Проверка отмены после выделения памяти под Bitmap. Если отменено, выбрасывает OperationCanceledException.

                // Прямоугольник, определяющий всю область Bitmap для блокировки.
                Rectangle rect = new Rectangle(0, 0, width, height);
                // Блокировка битов Bitmap для прямого доступа к его пиксельным данным.
                // Это значительно ускоряет запись пикселей по сравнению с SetPixel().
                // ImageLockMode.WriteOnly указывает, что мы будем только записывать данные.
                bmpData = bmp.LockBits(rect, ImageLockMode.WriteOnly, bmp.PixelFormat);
                token.ThrowIfCancellationRequested(); // Проверка отмены после блокировки битов.

                int stride = bmpData.Stride; // Ширина одной строки изображения в байтах. Может быть больше, чем (width * 3) из-за выравнивания памяти.
                IntPtr scan0 = bmpData.Scan0; // Указатель (IntPtr) на начало данных изображения в памяти (на первый пиксель первой строки).
                byte[] buffer = new byte[Math.Abs(stride) * height]; // Байтовый буфер для хранения пиксельных данных всего изображения.

                // Настройка параметров для параллельного выполнения цикла рендеринга.
                // MaxDegreeOfParallelism устанавливается в threadCount (выбранное пользователем количество потоков или "Auto").
                // CancellationToken (token) передается для возможности прерывания параллельных задач, если рендеринг отменен.
                ParallelOptions po = new ParallelOptions { MaxDegreeOfParallelism = threadCount, CancellationToken = token };
                int done = 0; // Счетчик обработанных строк. Используется для обновления ProgressBar.
                // Максимальное количество итераций, используемое для градации цвета в GetPixelColor.
                // Это значение (1000) может быть меньше, чем maxIterations (параметр с формы),
                // чтобы получить более плавные цветовые переходы в областях, где точки "убегают" относительно быстро.
                const int currentMaxColorIter = 1000;

                // Параллельный цикл по строкам изображения (y-координаты).
                // Parallel.For распределяет итерации по доступным потокам (согласно po.MaxDegreeOfParallelism).
                Parallel.For(0, height, po, y => // 'y' - текущая строка изображения.
                {
                    int rowOffset = y * stride; // Смещение в байтовом буфере (buffer) для начала текущей строки y.
                    // Внутренний цикл по пикселям в текущей строке (x-координаты).
                    for (int x = 0; x < width; x++) // 'x' - текущий столбец (пиксель в строке).
                    {
                        // Преобразование экранных координат (x, y) в координаты на комплексной плоскости (re, im).
                        // renderZoom, renderCenterX, renderCenterY - это параметры (масштаб и центр),
                        // зафиксированные на момент начала рендеринга текущего кадра.
                        double scale = BASE_SCALE / renderZoom; // Масштаб отображаемой области комплексной плоскости.
                                                                // Чем больше renderZoom, тем меньше scale (детальнее изображение).
                                                                // Вещественная часть (re) комплексного числа z0, соответствующего пикселю (x, y).
                                                                // (x - width / 2.0) - смещение от центра экрана по X.
                                                                // * scale / width - преобразование в единицы комплексной плоскости.
                        double re = renderCenterX + (x - width / 2.0) * scale / width;
                        // Мнимая часть (im) комплексного числа z0, соответствующего пикселю (x, y).
                        // (y - height / 2.0) - смещение от центра экрана по Y.
                        // Y-координата в RenderFractal соответствует направлению роста мнимой части (стандартная ориентация).
                        double im = renderCenterY + (y - height / 2.0) * scale / height;
                        Complex z_val = new Complex(re, im); // Начальное значение z (z0) для данной точки (пикселя).
                        int iter_val = 0; // Счетчик итераций для данной точки.

                        // Итерационный процесс для определения принадлежности точки множеству Жюлиа.
                        // Формула: z_next = z_current^2 + c (где 'c' - константа Жюлиа, заданная пользователем).
                        // Точка считается "убежавшей" (не принадлежит множеству), если ее модуль (|z_val|)
                        // превышает порог (threshold), обычно 2.0 или больше.
                        // Итерации продолжаются до тех пор, пока не будет достигнуто максимальное количество итераций (maxIterations)
                        // или пока точка не "убежит".
                        while (iter_val < maxIterations && z_val.Magnitude <= threshold)
                        {
                            z_val = z_val * z_val + c; // Основная формула множества Жюлиа.
                            iter_val++; // Увеличение счетчика итераций.
                        }

                        // Получение цвета пикселя на основе количества выполненных итераций (iter_val).
                        // Передаются iter_val, общее maxIterations и currentMaxColorIter для градации цвета.
                        Color pixelColor = GetPixelColor(iter_val, maxIterations, currentMaxColorIter);
                        // Запись цвета пикселя в байтовый буфер (buffer).
                        // Пиксели хранятся в формате BGR (Blue, Green, Red).
                        int index = rowOffset + x * 3; // Индекс первого байта (Blue) для пикселя (x,y) в буфере.
                        buffer[index] = pixelColor.B;       // Blue
                        buffer[index + 1] = pixelColor.G;   // Green
                        buffer[index + 2] = pixelColor.R;   // Red
                    }

                    // Обновление ProgressBar для отображения прогресса рендеринга (потокобезопасно).
                    int progress = Interlocked.Increment(ref done); // Атомарное (потокобезопасное) увеличение счетчика обработанных строк 'done'.
                    // Проверяем, что токен отмены не запрошен, ProgressBar существует, не уничтожен, и высота (height) корректна (больше 0).
                    if (!token.IsCancellationRequested && progressBar1.IsHandleCreated && !progressBar1.IsDisposed && height > 0)
                    {
                        try
                        {
                            // Выполняем обновление UI (ProgressBar) в основном потоке с помощью BeginInvoke (асинхронный вызов).
                            progressBar1.BeginInvoke((Action)(() =>
                            {
                                // Дополнительная проверка внутри делегата Invoke, так как состояние контрола могло измениться
                                // за время между внешней проверкой и выполнением делегата.
                                if (progressBar1.IsHandleCreated && !progressBar1.IsDisposed && progressBar1.Value <= progressBar1.Maximum)
                                {
                                    // Обновляем значение ProgressBar, рассчитывая процент выполнения.
                                    // Значение ограничивается progressBar.Maximum.
                                    progressBar1.Value = Math.Min(progressBar1.Maximum, (int)(100.0 * progress / height));
                                }
                            }));
                        }
                        catch (InvalidOperationException)
                        {
                            // Игнорируем ошибку InvalidOperationException, которая может возникнуть, если контрол ProgressBar
                            // уже уничтожен к моменту вызова BeginInvoke или выполнения делегата.
                        }
                    }
                }); // Конец Parallel.For

                token.ThrowIfCancellationRequested(); // Проверка отмены после завершения основного цикла рендеринга (Parallel.For).

                // Копирование данных из байтового буфера (buffer) в память, выделенную для Bitmap (доступную через scan0).
                Marshal.Copy(buffer, 0, scan0, buffer.Length);
                bmp.UnlockBits(bmpData); // Разблокировка битов Bitmap. После этого bmpData становится невалидным.
                bmpData = null; // Обнуляем ссылку на bmpData, так как он больше не действителен.

                token.ThrowIfCancellationRequested(); // Проверка отмены перед обновлением UI (присвоением canvas.Image).

                // Отображение отрендеренного изображения на элементе canvas (потокобезопасно).
                // Проверяем, что canvas существует (IsHandleCreated) и не уничтожен (IsDisposed).
                if (canvas1.IsHandleCreated && !canvas1.IsDisposed)
                {
                    Bitmap oldImage = null; // Ссылка для хранения предыдущего изображения, которое было на canvas.
                    // Выполняем обновление UI (canvas.Image) в основном потоке с помощью Invoke.
                    canvas1.Invoke((Action)(() =>
                    {
                        // Финальная проверка на отмену непосредственно перед присвоением нового изображения.
                        if (token.IsCancellationRequested)
                        {
                            bmp?.Dispose(); // Если рендеринг был отменен на этом последнем этапе, освобождаем созданный Bitmap.
                            return;         // И выходим, не обновляя canvas.Image.
                        }
                        oldImage = canvas1.Image as Bitmap; // Сохраняем ссылку на старое изображение (если оно было Bitmap).
                        canvas1.Image = bmp; // Устанавливаем новое, отрендеренное изображение на canvas.
                        // Обновляем параметры (центр и масштаб), с которыми был отрисован текущий canvas.Image.
                        // Эти значения (renderedCenterX, renderedCenterY, renderedZoom) используются в Canvas_Paint
                        // для "мгновенного" панорамирования/масштабирования путем трансформации этого изображения.
                        renderedCenterX = renderCenterX;
                        renderedCenterY = renderCenterY;
                        renderedZoom = renderZoom;
                        // После присвоения bmp элементу canvas.Image, элемент canvas "владеет" этим Bitmap.
                        // Обнуляем ссылку bmp здесь, чтобы блок finally его не удалил (если присвоение было успешным).
                        bmp = null;
                    }));
                    // Освобождаем старое изображение (oldImage), если оно было, после того как новое изображение установлено.
                    // Это важно для предотвращения утечек памяти.
                    oldImage?.Dispose();
                }
                else
                {
                    // Если canvas уже невалиден (например, форма закрывается во время рендеринга),
                    // просто освобождаем созданный Bitmap, так как он не будет отображен.
                    bmp?.Dispose();
                }
            }
            finally // Блок finally гарантирует освобождение ресурсов даже в случае исключений или отмены.
            {
                // Если bmpData не был разблокирован (например, из-за исключения, возникшего до bmp.UnlockBits(bmpData)),
                // и bmp существует, пытаемся разблокировать его.
                if (bmpData != null && bmp != null)
                {
                    try { bmp.UnlockBits(bmpData); }
                    catch { /* Игнорируем возможные ошибки при повторной или некорректной разблокировке. */ }
                }
                // Если bmp не был присвоен элементу canvas.Image (например, из-за отмены, ошибки до присвоения,
                // или если canvas был невалиден) и ссылка на него (bmp) не была обнулена (т.е. bmp != null),
                // то освобождаем этот Bitmap.
                if (bmp != null)
                {
                    bmp.Dispose();
                }
            }
        }

        /// <summary>
        /// Рендерит фрактал Жюлиа в объект Bitmap указанного размера (renderWidth, renderHeight).
        /// Используется для сохранения изображения в высоком разрешении.
        /// </summary>
        /// <param name="renderWidth">Ширина конечного изображения в пикселях.</param>
        /// <param name="renderHeight">Высота конечного изображения в пикселях.</param>
        /// <param name="currentCenterX">Центральная точка по оси X (вещественная часть) для рендеринга.</param>
        /// <param name="currentCenterY">Центральная точка по оси Y (мнимая часть) для рендеринга.</param>
        /// <param name="currentZoom">Уровень масштабирования для рендеринга.</param>
        /// <param name="currentBaseScale">Базовый масштаб (обычно константа BASE_SCALE).</param>
        /// <param name="currentC_param">Параметр 'c' фрактала Жюлиа.</param>
        /// <param name="currentMaxIterations_param">Максимальное количество итераций.</param>
        /// <param name="currentThreshold_param">Порог для определения "ухода в бесконечность".</param>
        /// <param name="numThreads">Количество потоков для параллельного рендеринга.</param>
        /// <param name="reportProgressCallback">Callback-функция для сообщения о прогрессе рендеринга (значение от 0 до 100).</param>
        /// <returns>Объект Bitmap с отрендеренным фракталом.</returns>
        private Bitmap RenderFractalToBitmap(int renderWidth, int renderHeight, double currentCenterX, double currentCenterY,
                                             double currentZoom, double currentBaseScale, Complex currentC_param,
                                             int currentMaxIterations_param, double currentThreshold_param, int numThreads,
                                             Action<int> reportProgressCallback)
        {
            // Проверка корректности размеров. Если размеры некорректны (меньше или равны 0),
            // возвращаем минимальный Bitmap (1x1 пиксель), чтобы избежать ошибок при создании Bitmap.
            if (renderWidth <= 0 || renderHeight <= 0)
            {
                return new Bitmap(1, 1);
            }

            // Создание Bitmap для рендеринга высокого разрешения.
            Bitmap bmp = new Bitmap(renderWidth, renderHeight, PixelFormat.Format24bppRgb);
            // Блокировка битов для прямого доступа к памяти.
            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, renderWidth, renderHeight), ImageLockMode.WriteOnly, bmp.PixelFormat);
            int stride = bmpData.Stride; // Ширина строки в байтах.
            IntPtr scan0 = bmpData.Scan0; // Указатель на начало данных.
            byte[] buffer = new byte[Math.Abs(stride) * renderHeight]; // Байтовый буфер для пикселей.

            // Настройка параметров для параллельного выполнения.
            ParallelOptions po = new ParallelOptions { MaxDegreeOfParallelism = numThreads };
            long done = 0; // Счетчик обработанных строк. Используем long для случая очень больших изображений, чтобы избежать переполнения int.
            // Максимальное количество итераций для градации цвета (аналогично RenderFractal).
            const int currentMaxColorIter_param = 1000;

            // Параллельный рендеринг, аналогичный методу RenderFractal, но:
            // - Использует явно переданные параметры (currentCenterX, currentZoom, currentC_param и т.д.).
            // - Использует renderWidth и renderHeight для размеров изображения и расчетов координат.
            // - Вызывает reportProgressCallback для сообщения о прогрессе.
            Parallel.For(0, renderHeight, po, y => // Цикл по строкам изображения.
            {
                int rowOffset = y * stride; // Смещение для текущей строки в буфере.
                for (int x = 0; x < renderWidth; x++) // Цикл по пикселям в строке.
                {
                    // Расчет координат (re, im) на комплексной плоскости для текущего пикселя (x,y).
                    // Используются переданные параметры currentBaseScale, currentZoom, currentCenterX, currentCenterY,
                    // а также renderWidth и renderHeight для масштабирования.
                    double scale = currentBaseScale / currentZoom;
                    double re = currentCenterX + (x - renderWidth / 2.0) * scale / renderWidth;
                    double im = currentCenterY + (y - renderHeight / 2.0) * scale / renderHeight;
                    Complex z_val = new Complex(re, im); // Начальное z0.
                    int iter_val = 0; // Счетчик итераций.

                    // Итерации для фрактала Жюлиа с использованием переданных параметров:
                    // currentMaxIterations_param, currentThreshold_param, currentC_param.
                    while (iter_val < currentMaxIterations_param && z_val.Magnitude <= currentThreshold_param)
                    {
                        z_val = z_val * z_val + currentC_param; // Формула Жюлиа.
                        iter_val++;
                    }
                    // Получение цвета пикселя с использованием тех же параметров.
                    Color pixelColor = GetPixelColor(iter_val, currentMaxIterations_param, currentMaxColorIter_param);
                    // Запись цвета в буфер (BGR).
                    int index = rowOffset + x * 3;
                    buffer[index] = pixelColor.B;
                    buffer[index + 1] = pixelColor.G;
                    buffer[index + 2] = pixelColor.R;
                }
                // Сообщение о прогрессе рендеринга через callback-функцию.
                long currentDone = Interlocked.Increment(ref done); // Атомарное увеличение счетчика.
                if (renderHeight > 0) // Проверка деления на ноль (хотя renderHeight уже проверен в начале).
                {
                    // Вызываем callback, передавая процент выполнения (от 0 до 100).
                    reportProgressCallback((int)(100.0 * currentDone / renderHeight));
                }
            });

            Marshal.Copy(buffer, 0, scan0, buffer.Length); // Копирование данных из буфера (buffer) в Bitmap (scan0).
            bmp.UnlockBits(bmpData); // Разблокировка битов Bitmap.
            return bmp; // Возвращаем готовый Bitmap высокого разрешения.
        }


        /// <summary>
        /// Преобразует цвет из цветовой модели HSV (Hue, Saturation, Value) в модель RGB.
        /// </summary>
        /// <param name="hue">Оттенок (Hue), обычно в диапазоне 0-360 градусов.</param>
        /// <param name="saturation">Насыщенность (Saturation), в диапазоне 0-1.</param>
        /// <param name="value">Значение/Яркость (Value), в диапазоне 0-1.</param>
        /// <returns>Цвет в формате RGB (объект Color).</returns>
        private Color ColorFromHSV(double hue, double saturation, double value)
        {
            // Нормализация hue в диапазон [0, 360) градусов.
            // (hue % 360 + 360) % 360 обрабатывает как положительные, так и отрицательные значения hue,
            // приводя их к эквивалентному значению в диапазоне [0, 359.99...].
            hue = (hue % 360 + 360) % 360;
            // Определение сектора на цветовом круге (0-5). Каждый сектор охватывает 60 градусов.
            // hi = 0 для hue [0, 60), hi = 1 для [60, 120) и т.д.
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            // Дробная часть hue внутри текущего 60-градусного сектора.
            // f = (hue / 60) - floor(hue / 60). Используется для интерполяции между основными цветами сектора.
            double f = hue / 60 - Math.Floor(hue / 60);

            // Ограничение насыщенности (saturation) и значения/яркости (value) диапазоном [0, 1].
            value = Math.Max(0, Math.Min(1, value));
            saturation = Math.Max(0, Math.Min(1, saturation));

            // Расчет компонентов RGB по стандартным формулам преобразования HSV в RGB.
            // v_comp - это value (яркость), преобразованное в диапазон 0-255 (для использования в Color.FromArgb).
            int v_comp = Convert.ToInt32(value * 255); // Соответствует 'V' в формулах HSV->RGB.
            // p_comp - компонент, зависящий от v_comp и saturation.
            int p_comp = Convert.ToInt32(v_comp * (1 - saturation)); // Соответствует 'p = V * (1 - S)'
            // q_comp - компонент, зависящий от v_comp, saturation и f.
            int q_comp = Convert.ToInt32(v_comp * (1 - f * saturation)); // Соответствует 'q = V * (1 - f*S)'
            // t_comp - компонент, зависящий от v_comp, saturation и f.
            int t_comp = Convert.ToInt32(v_comp * (1 - (1 - f) * saturation)); // Соответствует 't = V * (1 - (1-f)*S)'

            // Выбор правильной комбинации компонентов (v_comp, p_comp, q_comp, t_comp)
            // для R, G, B в зависимости от сектора hue (hi).
            switch (hi)
            {
                case 0: return Color.FromArgb(v_comp, t_comp, p_comp); // R=V, G=t, B=p
                case 1: return Color.FromArgb(q_comp, v_comp, p_comp); // R=q, G=V, B=p
                case 2: return Color.FromArgb(p_comp, v_comp, t_comp); // R=p, G=V, B=t
                case 3: return Color.FromArgb(p_comp, q_comp, v_comp); // R=p, G=q, B=V
                case 4: return Color.FromArgb(t_comp, p_comp, v_comp); // R=t, G=p, B=V
                default: return Color.FromArgb(v_comp, p_comp, q_comp); // Для hi = 5: R=V, G=p, B=q
            }
        }

        /// <summary>
        /// Обработчик события прокрутки колеса мыши на элементе canvas.
        /// Осуществляет масштабирование (зум) изображения фрактала относительно положения курсора мыши.
        /// </summary>
        private void Canvas_MouseWheel(object sender, MouseEventArgs e)
        {
            // Не обрабатываем событие, если идет процесс сохранения в высоком разрешении (isHighResRendering).
            if (isHighResRendering) return;

            // Коэффициент масштабирования:
            // 1.5 для увеличения (прокрутка колеса вверх, e.Delta > 0),
            // ~0.6667 (т.е. 1.0 / 1.5) для уменьшения (прокрутка колеса вниз, e.Delta < 0).
            // Использование 1.0 / 1.5 для уменьшения обеспечивает симметричность операции зума.
            double zoomFactor = e.Delta > 0 ? 1.5 : 1.0 / 1.5;
            double oldZoom = zoom; // Сохраняем текущий уровень масштаба (zoom) для расчетов.

            // Координаты курсора мыши на комплексной плоскости до масштабирования.
            // Это точка (mouseRe, mouseIm), которая должна остаться под курсором после масштабирования.
            // scaleBeforeZoom - масштаб комплексной плоскости до применения нового зума.
            double scaleBeforeZoom = BASE_SCALE / oldZoom;
            // Преобразуем экранные координаты курсора (e.X, e.Y) в координаты на комплексной плоскости.
            double mouseRe = centerX + (e.X - width / 2.0) * scaleBeforeZoom / width;
            double mouseIm = centerY + (e.Y - height / 2.0) * scaleBeforeZoom / height;

            // *** ИЗМЕНЕНИЯ ДЛЯ ZOOM ***
            // Обновление уровня масштаба (zoom) с учетом zoomFactor и нового минимального предела.
            // Было: zoom = Math.Max(1, Math.Min((double)nudZoom.Maximum, zoom * zoomFactor));
            zoom = Math.Max((double)nudZoom.Minimum, Math.Min((double)nudZoom.Maximum, zoom * zoomFactor));

            // Пересчет центра отображения (centerX, centerY) таким образом,
            // чтобы точка (mouseRe, mouseIm), которая была под курсором до зума,
            // осталась под курсором и после изменения масштаба (zoom).
            // scaleAfterZoom - новый масштаб комплексной плоскости после применения зума.
            double scaleAfterZoom = BASE_SCALE / zoom;
            // Новый centerX вычисляется так, чтобы mouseRe соответствовал e.X при новом масштабе.
            centerX = mouseRe - (e.X - width / 2.0) * scaleAfterZoom / width;
            // Аналогично для centerY и mouseIm.
            centerY = mouseIm - (e.Y - height / 2.0) * scaleAfterZoom / height;

            // Инвалидация (запрос на перерисовку) элемента canvas.
            // Это вызовет метод Canvas_Paint, который выполнит "мгновенное" масштабирование
            // путем трансформации существующего изображения.
            canvas1.Invalidate();

            // Обновление значения в NumericUpDown (nudZoom), если оно изменилось в результате масштабирования.
            // Если значение nudZoom.Value не изменилось (например, достигнут предел min/max),
            // но centerX/centerY изменились, все равно нужно запланировать полный рендер нового кадра.
            if (nudZoom.Value != (decimal)zoom)
            {
                // Установка нового значения в nudZoom. Это вызовет событие nudZoom.ValueChanged,
                // которое, в свою очередь, вызовет ParamControl_Changed и, следовательно, ScheduleRender.
                nudZoom.Value = (decimal)zoom;
            }
            else
            {
                // Если значение nudZoom.Value не изменилось (например, уперлись в min/max предел),
                // но центр (centerX, centerY) мог сместиться, то ScheduleRender нужно вызвать явно,
                // так как ParamControl_Changed (через nudZoom.ValueChanged) не будет вызван.
                ScheduleRender();
            }
        }

        /// <summary>
        /// Обработчик нажатия кнопки мыши на элементе canvas.
        /// Начинает процесс панорамирования изображения фрактала при нажатии левой кнопки мыши.
        /// </summary>
        private void Canvas_MouseDown(object sender, MouseEventArgs e)
        {
            // Не обрабатываем, если идет сохранение в высоком разрешении.
            if (isHighResRendering) return;
            // Если нажата левая кнопка мыши.
            if (e.Button == MouseButtons.Left)
            {
                panning = true; // Устанавливаем флаг, что начался процесс панорамирования.
                panStart = e.Location; // Запоминаем начальную позицию курсора (относительно canvas) для расчета смещения при движении.
            }
        }

        /// <summary>
        /// Обработчик движения мыши по элементу canvas.
        /// Осуществляет панорамирование изображения фрактала, если зажата левая кнопка мыши
        /// (т.е. флаг panning установлен в true).
        /// </summary>
        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            // Не обрабатываем, если идет сохранение или панорамирование не активно (флаг panning == false).
            if (isHighResRendering || !panning) return;

            // Расчет смещения центра (centerX, centerY) на комплексной плоскости
            // на основе смещения курсора мыши (e.X - panStart.X, e.Y - panStart.Y) в пикселях.
            double scale = BASE_SCALE / zoom; // Текущий масштаб комплексной плоскости.
            // Смещение по вещественной оси (Re).
            // (e.X - panStart.X) - смещение мыши в пикселях по X.
            // * scale / width - преобразование пиксельного смещения в смещение на комплексной плоскости.
            // Знак "-" используется потому, что если мышь двигается вправо (e.X > panStart.X),
            // то видимая область фрактала должна сдвигаться влево (т.е. centerX должен уменьшаться).
            centerX -= (e.X - panStart.X) * scale / width;
            // Аналогично для мнимой оси (Im) и centerY.
            centerY -= (e.Y - panStart.Y) * scale / height;
            panStart = e.Location; // Обновляем начальную позицию (panStart) текущим положением курсора
                                   // для следующего шага панорамирования (следующего события MouseMove).

            // Инвалидация (запрос на перерисовку) элемента canvas.
            // Это вызовет метод Canvas_Paint, который выполнит "мгновенное" панорамирование
            // путем трансформации существующего изображения.
            canvas1.Invalidate();

            // Планируем полный перерендер фрактала с новым центром (centerX, centerY).
            // Это произойдет с задержкой через renderTimer.
            ScheduleRender();
        }

        /// <summary>
        /// Обработчик отпускания кнопки мыши на элементе canvas.
        /// Завершает процесс панорамирования, если была отпущена левая кнопка мыши.
        /// </summary>
        private void Canvas_MouseUp(object sender, MouseEventArgs e)
        {
            // Не обрабатываем, если идет сохранение в высоком разрешении.
            if (isHighResRendering) return;
            // Если отпущена левая кнопка мыши.
            if (e.Button == MouseButtons.Left)
            {
                panning = false; // Сбрасываем флаг панорамирования.
            }
        }

        /// <summary>
        /// Обработчик нажатия кнопки "Сохранить" (старый вариант кнопки, BtnSave).
        /// Сохраняет текущее изображение, отображаемое на элементе canvas, в файл формата PNG.
        /// Это сохраняет изображение "как есть" с текущим разрешением canvas.
        /// </summary>
        private void BtnSave_Click(object sender, EventArgs e)
        {
            // Проверка, не идет ли уже сохранение в высоком разрешении (запущенное другой кнопкой).
            if (isHighResRendering)
            {
                MessageBox.Show("Идет сохранение в высоком разрешении. Пожалуйста, подождите.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Создание и настройка стандартного диалога сохранения файла (SaveFileDialog).
            // Фильтр устанавливается для файлов PNG (*.png).
            using (var dlg = new SaveFileDialog { Filter = "PNG Image|*.png" })
            {
                // Отображение диалога. Если пользователь выбрал файл и нажал "ОК" (DialogResult.OK).
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    // Если изображение на canvas существует (canvas.Image != null).
                    if (canvas1.Image != null)
                    {
                        // Сохраняем изображение в выбранный файл (dlg.FileName) в формате PNG (ImageFormat.Png).
                        canvas1.Image.Save(dlg.FileName, ImageFormat.Png);
                    }
                    else
                    {
                        // Если изображения нет, выводим сообщение об ошибке.
                        MessageBox.Show("Нет изображения для сохранения!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        /// <summary>
        /// Обработчик нажатия кнопки "Render" (Обновить, btnRender).
        /// Принудительно запускает (планирует) рендеринг предварительного просмотра фрактала.
        /// </summary>
        private void btnRender_Click(object sender, EventArgs e)
        {
            // Не обрабатываем, если идет сохранение в высоком разрешении.
            if (isHighResRendering) return;
            // Планируем рендеринг. Он начнется с задержкой по таймеру renderTimer.
            ScheduleRender();
        }

        /// <summary>
        /// Включает или отключает основные элементы управления на форме.
        /// Используется для блокировки UI во время длительных операций,
        /// таких как рендеринг и сохранение изображения в высоком разрешении.
        /// </summary>
        /// <param name="enabled">True, чтобы включить элементы управления; False, чтобы их отключить.</param>
        private void SetMainControlsEnabled(bool enabled)
        {
            // Действие (Action), которое будет выполнено для изменения состояния Enabled группы контролов.
            // Это сделано для того, чтобы можно было выполнить это действие в потоке UI с помощью Invoke, если необходимо.
            Action action = () =>
            {
                // Включение/отключение кнопки "Render" (Обновить).
                if (btnRender1 != null) btnRender1.Enabled = enabled;
                // Включение/отключение NumericUpDown для параметров фрактала.
                nudRe1.Enabled = enabled;
                nudIm1.Enabled = enabled;
                nudIterations1.Enabled = enabled;
                nudThreshold1.Enabled = enabled;
                // Включение/отключение ComboBox для выбора количества потоков.
                cbThreads1.Enabled = enabled;
                // Включение/отключение NumericUpDown для масштаба (zoom) и базового масштаба (для лупы).
                nudZoom.Enabled = enabled;
                nudBaseScale.Enabled = enabled;

                // Включение/отключение NumericUpDown для задания размеров (ширины W, высоты H) при сохранении изображения.
                if (nudW != null) nudW.Enabled = enabled;
                if (nudH != null) nudH.Enabled = enabled;

                // Включение/отключение всех чекбоксов выбора цветовой палитры.
                foreach (var cb in paletteCheckBoxes.Where(cb => cb != null))
                {
                    cb.Enabled = enabled;
                }

                // Восстановление корректного состояния доступности чекбокса colorBox (старая цветная палитра)
                // при общем включении контролов (enabled == true).
                if (enabled)
                {
                    HandleColorBoxEnableState(); // Вызывает метод, который определит, должен ли colorBox быть Enabled.
                }
                else if (colorBox != null) // При общем отключении контролов (enabled == false), colorBox также отключается.
                {
                    colorBox.Enabled = false;
                }
            };

            // Выполнение действия 'action' в потоке UI, если текущий вызов происходит из другого потока (this.InvokeRequired).
            // Если вызов из потока UI, то 'action' выполняется напрямую.
            if (this.InvokeRequired)
            {
                this.Invoke(action);
            }
            else
            {
                action();
            }
        }

        /// <summary>
        /// Обработчик нажатия кнопки "Сохранить PNG" (новый вариант, btnSave_Click_1).
        /// Позволяет сохранить фрактал в высоком разрешении, указанном пользователем
        /// в элементах NumericUpDown (nudW, nudH).
        /// </summary>
        private async void btnSave_Click_1(object sender, EventArgs e)
        {
            if (isHighResRendering)
            {
                MessageBox.Show("Процесс сохранения в высоком разрешении уже запущен.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int saveWidth = (int)nudW.Value; // Используем nudW1 для Жюлиа
            int saveHeight = (int)nudH.Value; // Используем nudH1 для Жюлиа

            if (saveWidth <= 0 || saveHeight <= 0)
            {
                MessageBox.Show("Ширина и высота изображения для сохранения должны быть больше 0.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Получаем значения Re и Im
            // Используем InvariantCulture для точки в качестве десятичного разделителя,
            // затем заменяем точку на подчеркивание для имени файла.
            string reValueString = nudRe1.Value.ToString("F3", System.Globalization.CultureInfo.InvariantCulture).Replace(".", "_");
            string imValueString = nudIm1.Value.ToString("F3", System.Globalization.CultureInfo.InvariantCulture).Replace(".", "_");
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            string suggestedFileName = $"fractal_julia_re{reValueString}_im{imValueString}_{timestamp}.png";

            using (SaveFileDialog saveDialog = new SaveFileDialog
            {
                Filter = "PNG Image|*.png",
                Title = "Сохранить фрактал Жюлиа (Высокое разрешение)",
                FileName = suggestedFileName // Устанавливаем новое имя файла по умолчанию
            })
            {
                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    Button currentActionSaveButton = sender as Button;
                    isHighResRendering = true;
                    if (currentActionSaveButton != null) currentActionSaveButton.Enabled = false;
                    SetMainControlsEnabled(false);

                    if (progressPNG != null) // Используем progressPNG1 для Жюлиа
                    {
                        progressPNG.Value = 0;
                        progressPNG.Visible = true;
                    }

                    try
                    {
                        UpdateParameters(); // Этот метод должен установить this.c из nudRe1 и nudIm1
                        Complex currentC_Capture = this.c;
                        int currentMaxIterations_Capture = this.maxIterations;
                        double currentThreshold_Capture = this.threshold;
                        double currentZoom_Capture = this.zoom;
                        double currentCenterX_Capture = this.centerX;
                        double currentCenterY_Capture = this.centerY;
                        int currentThreadCount_Capture = this.threadCount;

                        Bitmap highResBitmap = await Task.Run(() => RenderFractalToBitmap(
                            saveWidth, saveHeight,
                            currentCenterX_Capture, currentCenterY_Capture, currentZoom_Capture,
                            BASE_SCALE, // BASE_SCALE для Жюлиа
                            currentC_Capture, // Передаем захваченное значение 'c'
                            currentMaxIterations_Capture, currentThreshold_Capture,
                            currentThreadCount_Capture,
                            progressPercentage =>
                            {
                                if (progressPNG != null && progressPNG.IsHandleCreated && !progressPNG.IsDisposed) // Используем progressPNG1
                                {
                                    try
                                    {
                                        progressPNG.Invoke((Action)(() =>
                                        {
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

                        if (progressPNG != null && progressPNG.IsHandleCreated && !progressPNG.IsDisposed) // Используем progressPNG1
                        {
                            try
                            {
                                progressPNG.Invoke((Action)(() =>
                                {
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

        /// <summary>
        /// Обработчик события закрытия формы (OnFormClosed).
        /// Выполняет освобождение используемых ресурсов, таких как таймер (renderTimer)
        /// и источник токенов отмены (previewRenderCts).
        /// </summary>
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            renderTimer?.Stop();        // Остановка таймера отложенного рендеринга (если он существует).
            previewRenderCts?.Cancel(); // Отмена текущей задачи рендеринга предварительного просмотра, если она активна.
            previewRenderCts?.Dispose(); // Освобождение ресурсов CancellationTokenSource.
            renderTimer?.Dispose();      // Освобождение ресурсов таймера.

            // Закрытие окна выбора 'c' (MandelbrotSelectorForm), если оно открыто.
            // Это необходимо для корректного освобождения его ресурсов, особенно если оно было показано немодально.
            mandelbrotCSelectorWindow?.Close(); // ?. безопасный вызов Close(), если окно существует.

            base.OnFormClosed(e); // Вызов базового метода Form.OnFormClosed для стандартной обработки закрытия формы.
        }
    }
}