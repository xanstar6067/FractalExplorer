using FractalExplorer.Engines;
using FractalExplorer.Engines.EngineImplementations;
using FractalExplorer.Engines.EngineInterfaces;
using FractalExplorer.Forms;
using FractalExplorer.Forms.Other;
using FractalExplorer.Projects;
using FractalExplorer.Resources;
using FractalExplorer.Utilities;
using FractalExplorer.Utilities.RenderUtilities;
using FractalExplorer.Utilities.SaveIO;
using FractalExplorer.Utilities.SaveIO.ColorPalettes;
using FractalExplorer.Utilities.SaveIO.SaveStateImplementations;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace FractalDraving
{
    /// <summary>
    /// Базовый абстрактный класс для форм, отображающих фракталы семейства Мандельброта.
    /// Предоставляет общую логику для управления движком рендеринга, палитрой,
    /// масштабированием, панорамированием и сохранением состояний.
    /// Использует движки с различной точностью (Double, Decimal, BigDecimal) в зависимости от уровня масштабирования.
    /// </summary>
    public abstract partial class FractalMandelbrotFamilyForm : Form, IFractalForm, ISaveLoadCapableFractal, IHighResRenderable
    {
        #region Fields

        /// <summary>
        /// Компонент для визуализации процесса рендеринга плиток (зеленые и красные рамки).
        /// </summary>
        private RenderVisualizerComponent _renderVisualizer;

        /// <summary>
        /// Менеджер палитр, используемый для управления цветовыми схемами фрактала.
        /// </summary>
        private ColorPaletteMandelbrotFamily _paletteManager;

        /// <summary>
        /// Кэш для цветов палитры с уже примененной гамма-коррекцией. Ускоряет процесс окрашивания.
        /// </summary>
        private Color[] _gammaCorrectedPaletteCache;

        /// <summary>
        /// Уникальная "подпись" палитры, для которой был сгенерирован кэш. Используется для определения необходимости обновления кэша.
        /// </summary>
        private string _paletteCacheSignature;

        /// <summary>
        /// Форма для детальной конфигурации палитр.
        /// </summary>
        private ColorConfigurationMandelbrotFamilyForm _colorConfigForm;

        /// <summary>
        /// Размер одной плитки (тайла) в пикселях для пошагового рендеринга.
        /// </summary>
        private const int TILE_SIZE = 16;

        /// <summary>
        /// Объект для синхронизации доступа к битмапам из разных потоков.
        /// </summary>
        private readonly object _bitmapLock = new object();

        /// <summary>
        /// Основной битмап, содержащий последнее полностью отрисованное изображение фрактала.
        /// </summary>
        private Bitmap _previewBitmap;

        /// <summary>
        /// Источник токенов для отмены текущей операции рендеринга.
        /// </summary>
        private CancellationTokenSource _renderCts;

        /// <summary>
        /// Флаг, указывающий, выполняется ли в данный момент рендеринг в высоком разрешении.
        /// </summary>
        private volatile bool _isHighResRendering = false;

        /// <summary>
        /// Флаг, указывающий, выполняется ли в данный момент основной рендеринг предпросмотра.
        /// </summary>
        private volatile bool _isRendering = false;

        // --- Ключевые параметры фрактала с использованием BigDecimal для высокой точности ---

        /// <summary>
        /// Текущий коэффициент масштабирования фрактала.
        /// </summary>
        protected BigDecimal _zoom;

        /// <summary>
        /// Текущая координата X (реальная часть) центра видимой области фрактала.
        /// </summary>
        protected BigDecimal _centerX;

        /// <summary>
        /// Текущая координата Y (мнимая часть) центра видимой области фрактала.
        /// </summary>
        protected BigDecimal _centerY;

        /// <summary>
        /// Координата X центра, по которой был отрисован текущий <see cref="_previewBitmap"/>.
        /// </summary>
        private BigDecimal _renderedCenterX;

        /// <summary>
        /// Координата Y центра, по которой был отрисован текущий <see cref="_previewBitmap"/>.
        /// </summary>
        private BigDecimal _renderedCenterY;

        /// <summary>
        /// Коэффициент масштабирования, по которому был отрисован текущий <see cref="_previewBitmap"/>.
        /// </summary>
        private BigDecimal _renderedZoom;

        // --- Пороги для автоматической смены движка рендеринга ---
        private static readonly BigDecimal ZoomLevel2Threshold = new BigDecimal(20000m);
        private static readonly BigDecimal ZoomLevel3Threshold = BigDecimal.Parse("2.2758e25");

        /// <summary>
        /// Начальная позиция курсора мыши при инициировании панорамирования.
        /// </summary>
        private Point _panStart;

        /// <summary>
        /// Флаг, указывающий, находится ли пользователь в режиме панорамирования (перетаскивания изображения).
        /// </summary>
        private bool _panning = false;

        /// <summary>
        /// Таймер для отложенного запуска рендеринга после последнего действия пользователя.
        /// </summary>
        private System.Windows.Forms.Timer _renderDebounceTimer;

        /// <summary>
        /// Базовый заголовок окна для восстановления после отображения служебной информации (время рендера, точность).
        /// </summary>
        private string _baseTitle;

        /// <summary>
        /// Задача, представляющая текущую операцию рендеринга.
        /// </summary>
        private Task _renderTask;

        #endregion

        #region Constructor

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="FractalMandelbrotFamilyForm"/>.
        /// </summary>
        protected FractalMandelbrotFamilyForm()
        {
            InitializeComponent();
            // Инициализация координат с использованием BigDecimal
            _centerX = new BigDecimal(InitialCenterX);
            _centerY = new BigDecimal(InitialCenterY);
            _zoom = new BigDecimal(1.0m);
        }

        #endregion

        #region Protected Abstract/Virtual Methods

        /// <summary>
        /// Получает тип фрактала, который должен быть отрендерен.
        /// Это определяет, какая формула будет использоваться в движке.
        /// </summary>
        protected abstract FractalType GetFractalType();

        /// <summary>
        /// Получает базовый "ширинный" масштаб для фрактала в комплексной плоскости.
        /// Обычно для Мандельброта это ~3.0-4.0. Возвращает BigDecimal.
        /// </summary>
        protected virtual BigDecimal BaseScale => new BigDecimal(3.0m);

        /// <summary>
        /// Получает начальную координату X (реальная часть) центра для данного типа фрактала.
        /// </summary>
        protected virtual decimal InitialCenterX => -0.5m;

        /// <summary>
        /// Получает начальную координату Y (мнимая часть) центра для данного типа фрактала.
        /// </summary>
        protected virtual decimal InitialCenterY => 0.0m;

        /// <summary>
        /// Вызывается после завершения основной инициализации формы для выполнения специфичной для фрактала настройки.
        /// </summary>
        protected virtual void OnPostInitialize() { }

        /// <summary>
        /// Получает строку с деталями (например, константа Жюлиа) для формирования уникального имени файла при сохранении.
        /// </summary>
        protected virtual string GetSaveFileNameDetails() => "fractal";

        #endregion

        #region Form Lifecycle & UI Initialization

        /// <summary>
        /// Обработчик события загрузки формы. Выполняет всю первоначальную настройку.
        /// </summary>
        private void FormBase_Load(object sender, EventArgs e)
        {
            _baseTitle = this.Text;
            _paletteManager = new ColorPaletteMandelbrotFamily();
            _renderDebounceTimer = new System.Windows.Forms.Timer { Interval = 300 };
            _renderDebounceTimer.Tick += RenderDebounceTimer_Tick;
            _renderVisualizer = new RenderVisualizerComponent(TILE_SIZE);
            _renderVisualizer.NeedsRedraw += OnVisualizerNeedsRedraw;

            InitializeControls();
            InitializeEventHandlers();

            // Динамический поиск и настройка выпадающего списка SSAA
            var cbSSAA = this.Controls.Find("cbSSAA", true).FirstOrDefault() as ComboBox;
            if (cbSSAA != null)
            {
                cbSSAA.Items.Add("Выкл (1x)");
                cbSSAA.Items.Add("Низкое (2x)");
                cbSSAA.Items.Add("Высокое (4x)");
                cbSSAA.SelectedItem = "Выкл (1x)";
                cbSSAA.SelectedIndexChanged += (s, ev) => ScheduleRender();
            }

            _zoom = BaseScale / new BigDecimal(4.0m);
            UpdateZoomUI();

            _renderedCenterX = _centerX;
            _renderedCenterY = _centerY;
            _renderedZoom = _zoom;
            OnPostInitialize();
            ScheduleRender();
        }

        /// <summary>
        /// Обработчик события закрытия формы. Освобождает все управляемые и неуправляемые ресурсы.
        /// </summary>
        private void FractalMandelbrotFamilyForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            _renderDebounceTimer?.Stop();
            _renderDebounceTimer?.Dispose();
            _renderCts?.Cancel();
            _renderCts?.Dispose();
            lock (_bitmapLock)
            {
                _previewBitmap?.Dispose();
                _previewBitmap = null;
            }
            if (_renderVisualizer != null)
            {
                _renderVisualizer.NeedsRedraw -= OnVisualizerNeedsRedraw;
                _renderVisualizer.Dispose();
            }
        }

        /// <summary>
        /// Инициализирует элементы управления на форме (NumericUpDown, ComboBox и т.д.).
        /// </summary>
        private void InitializeControls()
        {
            int cores = Environment.ProcessorCount;
            cbThreads.Items.Clear();
            for (int i = 1; i <= cores; i++) cbThreads.Items.Add(i);
            cbThreads.Items.Add("Auto");
            cbThreads.SelectedItem = "Auto";

            nudIterations.Minimum = 50;
            nudIterations.Maximum = 100000;
            nudIterations.Value = 500;

            nudThreshold.Minimum = 2m;
            nudThreshold.Maximum = 1000m;
            nudThreshold.DecimalPlaces = 1;
            nudThreshold.Increment = 0.1m;
            nudThreshold.Value = 2m;

            nudZoom.DecimalPlaces = 4; // Меньше знаков для UI, реальная точность хранится в _zoom
            nudZoom.Increment = 0.1m;
            nudZoom.Minimum = 0.0001m;
            nudZoom.Maximum = decimal.MaxValue;

            if (nudRe != null && nudIm != null)
            {
                nudRe.Minimum = -2m;
                nudRe.Maximum = 2m;
                nudRe.DecimalPlaces = 15;
                nudRe.Increment = 0.001m;
                nudRe.Value = -0.8m; // Значение по умолчанию для Жюлиа
                nudIm.Minimum = -2m;
                nudIm.Maximum = 2m;
                nudIm.DecimalPlaces = 15;
                nudIm.Increment = 0.001m;
                nudIm.Value = 0.156m; // Значение по умолчанию для Жюлиа
            }
        }

        /// <summary>
        /// Привязывает обработчики событий к элементам управления и холсту.
        /// </summary>
        private void InitializeEventHandlers()
        {
            nudIterations.ValueChanged += ParamControl_Changed;
            nudThreshold.ValueChanged += ParamControl_Changed;
            cbThreads.SelectedIndexChanged += ParamControl_Changed;
            nudZoom.ValueChanged += NudZoom_ValueChanged;
            if (nudRe != null) nudRe.ValueChanged += ParamControl_Changed;
            if (nudIm != null) nudIm.ValueChanged += ParamControl_Changed;

            var stateManagerButton = Controls.Find("btnStateManager", true).FirstOrDefault();
            if (stateManagerButton != null) stateManagerButton.Click += btnStateManager_Click;

            var configButton = Controls.Find("color_configurations", true).FirstOrDefault();
            if (configButton != null) configButton.Click += color_configurations_Click;

            canvas.MouseWheel += Canvas_MouseWheel;
            canvas.MouseDown += Canvas_MouseDown;
            canvas.MouseMove += Canvas_MouseMove;
            canvas.MouseUp += Canvas_MouseUp;
            canvas.Paint += Canvas_Paint;
            canvas.Resize += (s, e) =>
            {
                if (WindowState != FormWindowState.Minimized) ScheduleRender();
            };
        }

        #endregion

        #region UI Event Handlers

        /// <summary>
        /// Универсальный обработчик изменения параметров, который запускает перерисовку.
        /// </summary>
        private void ParamControl_Changed(object sender, EventArgs e)
        {
            if (_isHighResRendering) return;
            ScheduleRender();
        }

        /// <summary>
        /// Обработчик изменения значения в поле масштабирования (nudZoom).
        /// Обновляет основное значение _zoom типа BigDecimal.
        /// </summary>
        private void NudZoom_ValueChanged(object sender, EventArgs e)
        {
            var newZoom = new BigDecimal(nudZoom.Value);
            if (_zoom != newZoom)
            {
                _zoom = newZoom;
                ScheduleRender();
            }
        }

        /// <summary>
        /// Открывает диалоговое окно для настройки цветовых палитр.
        /// </summary>
        private void color_configurations_Click(object sender, EventArgs e)
        {
            if (_colorConfigForm == null || _colorConfigForm.IsDisposed)
            {
                _colorConfigForm = new ColorConfigurationMandelbrotFamilyForm(_paletteManager);
                _colorConfigForm.PaletteApplied += (s, ev) => ScheduleRender();
                _colorConfigForm.FormClosed += (s, args) => _colorConfigForm = null;
                _colorConfigForm.Show(this);
            }
            else
            {
                _colorConfigForm.Activate();
            }
        }

        /// <summary>
        /// Открывает диалоговое окно для управления сохранениями и пресетами.
        /// </summary>
        private void btnStateManager_Click(object sender, EventArgs e)
        {
            using (var dialog = new SaveLoadDialogForm(this))
            {
                dialog.ShowDialog(this);
            }
        }

        #endregion

        #region Canvas Interaction

        /// <summary>
        /// Обрабатывает масштабирование с помощью колеса мыши.
        /// Вычисляет новую позицию и масштаб с высокой точностью.
        /// </summary>
        private void Canvas_MouseWheel(object sender, MouseEventArgs e)
        {
            if (_isRendering || canvas.Width <= 0 || canvas.Height <= 0) return;

            decimal zoomFactorDecimal = e.Delta > 0 ? 1.5m : 1.0m / 1.5m;
            var bigZoomFactor = new BigDecimal(zoomFactorDecimal);

            // Вычисляем комплексные координаты точки под курсором до масштабирования
            var scaleBeforeZoom = BaseScale / _zoom;
            var mouseReal = _centerX + (new BigDecimal(e.X) - new BigDecimal(canvas.Width) / new BigDecimal(2)) * scaleBeforeZoom / new BigDecimal(canvas.Width);
            var mouseImaginary = _centerY - (new BigDecimal(e.Y) - new BigDecimal(canvas.Height) / new BigDecimal(2)) * scaleBeforeZoom / new BigDecimal(canvas.Height);

            // Применяем масштабирование
            _zoom *= bigZoomFactor;

            // Вычисляем новый центр так, чтобы точка под курсором осталась на месте
            var scaleAfterZoom = BaseScale / _zoom;
            _centerX = mouseReal - (new BigDecimal(e.X) - new BigDecimal(canvas.Width) / new BigDecimal(2)) * scaleAfterZoom / new BigDecimal(canvas.Width);
            _centerY = mouseImaginary + (new BigDecimal(e.Y) - new BigDecimal(canvas.Height) / new BigDecimal(2)) * scaleAfterZoom / new BigDecimal(canvas.Height);

            UpdateZoomUI();
            canvas.Invalidate(); // Немедленно перерисовываем с трансформацией
            ScheduleRender();    // Планируем полный рендер
        }

        /// <summary>
        /// Обрабатывает нажатие кнопки мыши для начала панорамирования.
        /// </summary>
        private void Canvas_MouseDown(object sender, MouseEventArgs e)
        {
            if (_isRendering) return;
            if (e.Button == MouseButtons.Left)
            {
                _panning = true;
                _panStart = e.Location;
                canvas.Cursor = Cursors.Hand;
            }
        }

        /// <summary>
        /// Обрабатывает перемещение мыши в режиме панорамирования.
        /// </summary>
        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_panning || canvas.Width <= 0) return;

            var unitsPerPixel = BaseScale / _zoom / new BigDecimal(canvas.Width);
            _centerX -= new BigDecimal(e.X - _panStart.X) * unitsPerPixel;
            _centerY += new BigDecimal(e.Y - _panStart.Y) * unitsPerPixel;
            _panStart = e.Location;

            canvas.Invalidate();
            ScheduleRender();
        }

        /// <summary>
        /// Обрабатывает отпускание кнопки мыши для завершения панорамирования.
        /// </summary>
        private void Canvas_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _panning = false;
                canvas.Cursor = Cursors.Default;
            }
        }

        /// <summary>
        /// Обрабатывает перерисовку холста (canvas).
        /// Обеспечивает плавное отображение при масштабировании и панорамировании за счет трансформации старого изображения.
        /// </summary>
        private void Canvas_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.Clear(Color.Black);

            lock (_bitmapLock)
            {
                if (_previewBitmap != null && _previewBitmap.Width > 0 && _previewBitmap.Height > 0)
                {
                    // Если текущие координаты соответствуют отрендеренным, просто рисуем битмап 1-в-1.
                    if (_renderedCenterX == _centerX && _renderedCenterY == _centerY && _renderedZoom == _zoom)
                    {
                        e.Graphics.DrawImageUnscaled(_previewBitmap, Point.Empty);
                    }
                    else // Иначе, трансформируем старый битмап для создания плавного предпросмотра.
                    {
                        try
                        {
                            // Рассчитываем, как изменились масштаб и положение.
                            var renderedComplexWidth = BaseScale / _renderedZoom;
                            var currentComplexWidth = BaseScale / _zoom;

                            var unitsPerPixelRendered = renderedComplexWidth / new BigDecimal(_previewBitmap.Width);
                            var unitsPerPixelCurrent = currentComplexWidth / new BigDecimal(canvas.Width);

                            var renderedReMin = _renderedCenterX - (renderedComplexWidth / new BigDecimal(2));
                            var currentReMin = _centerX - (currentComplexWidth / new BigDecimal(2));

                            var renderedImMax = _renderedCenterY + (new BigDecimal(_previewBitmap.Height) * unitsPerPixelRendered / new BigDecimal(2));
                            var currentImMax = _centerY + (new BigDecimal(canvas.Height) * unitsPerPixelCurrent / new BigDecimal(2));

                            // Вычисляем смещение и новый размер в пикселях.
                            var offsetXPixels = (renderedReMin - currentReMin) / unitsPerPixelCurrent;
                            var offsetYPixels = (currentImMax - renderedImMax) / unitsPerPixelCurrent;
                            var scaleRatio = unitsPerPixelRendered / unitsPerPixelCurrent;
                            var newWidthPixels = new BigDecimal(_previewBitmap.Width) * scaleRatio;
                            var newHeightPixels = new BigDecimal(_previewBitmap.Height) * scaleRatio;

                            // Преобразуем BigDecimal в float для отрисовки.
                            if (offsetXPixels.TryToDecimal(out var ox) && offsetYPixels.TryToDecimal(out var oy) &&
                                newWidthPixels.TryToDecimal(out var nw) && newHeightPixels.TryToDecimal(out var nh))
                            {
                                e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                                e.Graphics.DrawImage(_previewBitmap, new RectangleF((float)ox, (float)oy, (float)nw, (float)nh));
                            }
                            else // Если значения слишком велики для decimal, просто рисуем без масштабирования.
                            {
                                e.Graphics.DrawImageUnscaled(_previewBitmap, Point.Empty);
                            }
                        }
                        catch
                        {
                            // В случае любой ошибки просто рисуем последний удачный битмап.
                            e.Graphics.DrawImageUnscaled(_previewBitmap, Point.Empty);
                        }
                    }
                }
            }
        }

        #endregion

        #region Rendering Logic

        /// <summary>
        /// Планирует запуск рендеринга с небольшой задержкой, чтобы не перегружать систему при частых изменениях.
        /// </summary>
        private void ScheduleRender()
        {
            if (_isHighResRendering || WindowState == FormWindowState.Minimized) return;
            _renderDebounceTimer.Stop();
            _renderDebounceTimer.Start();
        }

        /// <summary>
        /// Обработчик тика таймера, который запускает асинхронный рендеринг.
        /// </summary>
        private void RenderDebounceTimer_Tick(object sender, EventArgs e)
        {
            _renderDebounceTimer.Stop();
            // Если предыдущая задача рендеринга еще выполняется, отменяем ее.
            if (_renderTask != null && !_renderTask.IsCompleted)
            {
                _renderCts?.Cancel();
            }

            // Создаем новый источник отмены и запускаем рендер.
            _renderCts = new CancellationTokenSource();
            _renderTask = StartRenderAsync(_renderCts.Token);
        }

        /// <summary>
        /// Основной асинхронный метод для рендеринга фрактала.
        /// </summary>
        /// <param name="token">Токен для отмены операции.</param>
        private async Task StartRenderAsync(CancellationToken token)
        {
            if (canvas.Width <= 0 || canvas.Height <= 0) return;

            _isRendering = true;
            var stopwatch = Stopwatch.StartNew();

            // 1. Выбираем движок рендеринга в зависимости от текущего уровня масштабирования.
            IFractalEngine engine;
            string precisionText;

            if (_zoom < ZoomLevel2Threshold)
            {
                engine = new EngineDouble();
                precisionText = "Double";
            }
            else if (_zoom < ZoomLevel3Threshold)
            {
                engine = new EngineDecimal();
                precisionText = "Decimal (x1)";
            }
            else
            {
                engine = new EngineBig();
                precisionText = "BigDecimal (Deep)";
            }

            if (this.IsHandleCreated && !this.IsDisposed)
            {
                this.Invoke((Action)(() => this.Text = $"{_baseTitle} - Точность: {precisionText}"));
            }

            // 2. Настраиваем палитру и параметры рендеринга.
            ApplyActivePalette(engine);
            var options = new RenderOptions
            {
                Width = canvas.Width,
                Height = canvas.Height,
                CenterX = _centerX.ToString(),
                CenterY = _centerY.ToString(),
                Scale = (BaseScale / _zoom).ToString(),
                FractalType = this.GetFractalType(),
                JuliaC = (this is FractalJulia || this is FractalJuliaBurningShip) ? new ComplexDecimal(nudRe.Value, nudIm.Value) : new ComplexDecimal(0, 0),
                SsaaFactor = GetSelectedSsaaFactor(),
                NumThreads = GetThreadCount()
            };

            // Запоминаем текущие координаты, для которых будет идти рендер.
            var currentRenderedCenterX = _centerX;
            var currentRenderedCenterY = _centerY;
            var currentRenderedZoom = _zoom;

            try
            {
                // Запускаем рендеринг в фоновом потоке, чтобы не блокировать UI.
                Bitmap newBitmap = await Task.Run(() => engine.Render(options, token), token);

                token.ThrowIfCancellationRequested();

                // После успешного рендеринга, заменяем старый битмап на новый.
                lock (_bitmapLock)
                {
                    _previewBitmap?.Dispose();
                    _previewBitmap = newBitmap;
                    _renderedCenterX = currentRenderedCenterX;
                    _renderedCenterY = currentRenderedCenterY;
                    _renderedZoom = currentRenderedZoom;
                }

                stopwatch.Stop();
                if (this.IsHandleCreated && !this.IsDisposed)
                {
                    this.Invoke((Action)(() => this.Text += $" - Время: {stopwatch.Elapsed.TotalSeconds:F3} сек."));
                }

                if (canvas.IsHandleCreated && !canvas.IsDisposed) canvas.Invalidate();
            }
            catch (OperationCanceledException)
            {
                // Это ожидаемое исключение, когда мы отменяем рендер. Просто игнорируем.
            }
            catch (Exception ex)
            {
                if (IsHandleCreated && !IsDisposed) MessageBox.Show($"Ошибка рендеринга: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _isRendering = false;
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Безопасно обновляет элемент управления nudZoom, чтобы избежать исключений переполнения.
        /// </summary>
        private void UpdateZoomUI()
        {
            if (_zoom.TryToDecimal(out decimal zoomDecimal))
            {
                // Временно отписываемся от события, чтобы избежать рекурсивного вызова.
                nudZoom.ValueChanged -= NudZoom_ValueChanged;
                nudZoom.Value = ClampDecimal(zoomDecimal, nudZoom.Minimum, nudZoom.Maximum);
                nudZoom.ValueChanged += NudZoom_ValueChanged;
            }
            // Если преобразование в decimal невозможно (число слишком большое),
            // оставляем в nudZoom максимальное значение, но _zoom сохраняет реальную точность.
        }

        /// <summary>
        /// Получает выбранный пользователем фактор суперсэмплинга (SSAA) из выпадающего списка.
        /// </summary>
        private int GetSelectedSsaaFactor()
        {
            var cbSSAA = this.Controls.Find("cbSSAA", true).FirstOrDefault() as ComboBox;
            if (cbSSAA == null) return 1;

            Func<int> getFactor = () =>
            {
                if (cbSSAA.SelectedItem == null) return 1;
                switch (cbSSAA.SelectedItem.ToString())
                {
                    case "Низкое (2x)": return 2;
                    case "Высокое (4x)": return 4;
                    default: return 1;
                }
            };
            return cbSSAA.InvokeRequired ? (int)cbSSAA.Invoke(getFactor) : getFactor();
        }

        /// <summary>
        /// Получает выбранное пользователем количество потоков для рендеринга.
        /// </summary>
        private int GetThreadCount()
        {
            if (cbThreads.InvokeRequired)
            {
                return (int)cbThreads.Invoke(new Func<int>(GetThreadCount));
            }
            return cbThreads.SelectedItem?.ToString() == "Auto" ? Environment.ProcessorCount : Convert.ToInt32(cbThreads.SelectedItem);
        }

        /// <summary>
        /// Ограничивает значение типа decimal заданным диапазоном [min, max].
        /// </summary>
        private decimal ClampDecimal(decimal value, decimal min, decimal max) => Math.Max(min, Math.Min(max, value));

        /// <summary>
        /// Обработчик события от визуализатора, запрашивающий перерисовку холста для отображения прогресса.
        /// </summary>
        private void OnVisualizerNeedsRedraw()
        {
            if (canvas.IsHandleCreated && !canvas.IsDisposed)
            {
                canvas.BeginInvoke((Action)(() => canvas.Invalidate()));
            }
        }

        #endregion

        #region Palette Management

        /// <summary>
        /// Применяет активную палитру к движку рендеринга, используя кэширование для производительности.
        /// </summary>
        /// <param name="engine">Движок, к которому применяется палитра.</param>
        private void ApplyActivePalette(IFractalEngine engine)
        {
            engine.MaxIterations = (int)nudIterations.Value;
            engine.ThresholdSquared = (double)(nudThreshold.Value * nudThreshold.Value);

            var activePalette = _paletteManager.ActivePalette;

            // Если палитра не выбрана, используем палитру по умолчанию, чтобы избежать сбоя.
            if (activePalette == null)
            {
                activePalette = _paletteManager.GetDefaultPalette();
            }

            int effectiveMaxColorIterations = activePalette.AlignWithRenderIterations ? engine.MaxIterations : activePalette.MaxColorIterations;
            string newSignature = GeneratePaletteSignature(activePalette, engine.MaxIterations);

            // Если палитра или ее параметры изменились, пересоздаем кэш цветов.
            if (_gammaCorrectedPaletteCache == null || newSignature != _paletteCacheSignature)
            {
                _paletteCacheSignature = newSignature;
                var paletteGeneratorFunc = GeneratePaletteFunction(activePalette);
                _gammaCorrectedPaletteCache = new Color[effectiveMaxColorIterations + 1];
                for (int i = 0; i <= effectiveMaxColorIterations; i++)
                {
                    _gammaCorrectedPaletteCache[i] = paletteGeneratorFunc(i, engine.MaxIterations, effectiveMaxColorIterations);
                }
            }

            engine.MaxColorIterations = effectiveMaxColorIterations;
            engine.Palette = (iter, maxIter, maxColorIter) =>
            {
                if (iter >= maxIter) return Color.Black;
                int index = Math.Min(iter, _gammaCorrectedPaletteCache.Length - 1);
                return _gammaCorrectedPaletteCache[index];
            };
        }

        /// <summary>
        /// Генерирует уникальную строку-"подпись" для текущего состояния палитры.
        /// </summary>
        private string GeneratePaletteSignature(PaletteManagerMandelbrotFamily palette, int maxIterationsForAlignment)
        {
            var sb = new StringBuilder();
            sb.Append(palette.Name);
            sb.Append(':');
            foreach (var color in palette.Colors) sb.Append(color.ToArgb().ToString("X8"));
            sb.Append(':');
            sb.Append(palette.IsGradient);
            sb.Append(':');
            sb.Append(palette.Gamma.ToString("F2"));
            sb.Append(':');
            int effectiveMaxColorIterations = palette.AlignWithRenderIterations ? maxIterationsForAlignment : palette.MaxColorIterations;
            sb.Append(effectiveMaxColorIterations);
            return sb.ToString();
        }

        /// <summary>
        /// Создает функцию, которая возвращает цвет для заданного числа итераций на основе настроек палитры.
        /// </summary>
        private Func<int, int, int, Color> GeneratePaletteFunction(PaletteManagerMandelbrotFamily palette)
        {
            double gamma = palette.Gamma;
            var colors = new List<Color>(palette.Colors);
            bool isGradient = palette.IsGradient;
            int colorCount = colors.Count;

            if (colorCount == 0) return (i, m, mc) => Color.Black;
            if (colorCount == 1) return (iter, max, clrMax) => ColorCorrection.ApplyGamma((iter == max) ? Color.Black : colors[0], gamma);

            return (iter, maxIter, maxColorIter) =>
            {
                if (iter >= maxIter) return Color.Black;
                double normalizedIter = maxColorIter > 0 ? (double)Math.Min(iter, maxColorIter) / maxColorIter : 0;
                Color baseColor;
                if (isGradient)
                {
                    double scaledT = normalizedIter * (colorCount - 1);
                    int index1 = (int)Math.Floor(scaledT);
                    int index2 = Math.Min(index1 + 1, colorCount - 1);
                    double localT = scaledT - index1;
                    baseColor = LerpColor(colors[index1], colors[index2], localT);
                }
                else
                {
                    int colorIndex = (int)(normalizedIter * colorCount);
                    if (colorIndex >= colorCount) colorIndex = colorCount - 1;
                    baseColor = colors[colorIndex];
                }
                return ColorCorrection.ApplyGamma(baseColor, gamma);
            };
        }

        /// <summary>
        /// Выполняет линейную интерполяцию между двумя цветами.
        /// </summary>
        private Color LerpColor(Color a, Color b, double t)
        {
            t = Math.Max(0, Math.Min(1, t));
            return Color.FromArgb(
                (int)(a.A + (b.A - a.A) * t),
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t)
            );
        }

        #endregion

        #region ISaveLoadCapableFractal Implementation

        /// <inheritdoc/>
        public abstract string FractalTypeIdentifier { get; }

        /// <inheritdoc/>
        public abstract Type ConcreteSaveStateType { get; }

        /// <inheritdoc/>
        public virtual FractalSaveStateBase GetCurrentStateForSave(string saveName)
        {
            MandelbrotFamilySaveState state;
            if (this.GetFractalType() == FractalType.Julia || this.GetFractalType() == FractalType.JuliaBurningShip)
                state = new JuliaFamilySaveState(this.FractalTypeIdentifier);
            else
                state = new MandelbrotFamilySaveState(this.FractalTypeIdentifier);

            state.SaveName = saveName;
            state.Timestamp = DateTime.Now;
            state.Threshold = nudThreshold.Value;
            state.Iterations = (int)nudIterations.Value;
            state.PaletteName = _paletteManager.ActivePalette?.Name ?? "Стандартный серый";
            state.PreviewEngineType = this.FractalTypeIdentifier;

            // Безопасно сохраняем BigDecimal. Если он слишком большой для decimal,
            // используем специальное значение, чтобы пометить его как "глубокое" состояние.
            if (!_centerX.TryToDecimal(out var centerXDecimal) ||
                !_centerY.TryToDecimal(out var centerYDecimal) ||
                !_zoom.TryToDecimal(out var zoomDecimal))
            {
                state.CenterX = 0;
                state.CenterY = 0;
                state.Zoom = decimal.MaxValue; // Флаг для сверхглубоких состояний.
            }
            else
            {
                state.CenterX = centerXDecimal;
                state.CenterY = centerYDecimal;
                state.Zoom = zoomDecimal;
            }

            if (state is JuliaFamilySaveState juliaState)
            {
                juliaState.CRe = nudRe.Value;
                juliaState.CIm = nudIm.Value;
            }

            // Для совместимости со старой системой превью, можно сериализовать параметры.
            // Однако, новая система RenderPreviewTileAsync не использует это.
            // state.PreviewParametersJson = ...;

            return state;
        }

        /// <inheritdoc/>
        public virtual void LoadState(FractalSaveStateBase stateBase)
        {
            if (!(stateBase is MandelbrotFamilySaveState state))
            {
                MessageBox.Show("Несовместимый тип состояния для загрузки.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _renderCts?.Cancel();
            _renderDebounceTimer.Stop();

            // Загружаем координаты с преобразованием в BigDecimal.
            _centerX = new BigDecimal(state.CenterX);
            _centerY = new BigDecimal(state.CenterY);
            _zoom = new BigDecimal(state.Zoom);

            UpdateZoomUI();
            nudThreshold.Value = ClampDecimal(state.Threshold, nudThreshold.Minimum, nudThreshold.Maximum);
            nudIterations.Value = (int)ClampDecimal(state.Iterations, nudIterations.Minimum, nudIterations.Maximum);

            var paletteToLoad = _paletteManager.Palettes.FirstOrDefault(p => p.Name == state.PaletteName);
            if (paletteToLoad != null) _paletteManager.ActivePalette = paletteToLoad;

            if (state is JuliaFamilySaveState juliaState && nudRe != null && nudIm != null && nudRe.Visible)
            {
                nudRe.Value = ClampDecimal(juliaState.CRe, nudRe.Minimum, nudRe.Maximum);
                nudIm.Value = ClampDecimal(juliaState.CIm, nudIm.Minimum, nudIm.Maximum);
            }

            lock (_bitmapLock)
            {
                _previewBitmap?.Dispose(); _previewBitmap = null;
            }

            _renderedCenterX = _centerX;
            _renderedCenterY = _centerY;
            _renderedZoom = _zoom;
            ScheduleRender();
        }

        /// <inheritdoc/>
        public virtual Bitmap RenderPreview(FractalSaveStateBase stateBase, int previewWidth, int previewHeight)
        {
            if (!(stateBase is MandelbrotFamilySaveState state)) return new Bitmap(previewWidth, previewHeight);

            var bmp = new Bitmap(previewWidth, previewHeight, PixelFormat.Format32bppArgb);

            // Генерируем плитки для превью
            var tiles = new List<TileInfo>();
            for (int y = 0; y < previewHeight; y += TILE_SIZE)
            {
                for (int x = 0; x < previewWidth; x += TILE_SIZE)
                {
                    tiles.Add(new TileInfo(x, y, Math.Min(TILE_SIZE, previewWidth - x), Math.Min(TILE_SIZE, previewHeight - y)));
                }
            }

            // Запускаем рендеринг всех плиток и дожидаемся завершения.
            var dispatcher = new TileRenderDispatcher(tiles, Environment.ProcessorCount);
            var renderTask = dispatcher.RenderAsync(async (tile, ct) =>
            {
                // Для каждой плитки асинхронно рендерим ее данные
                byte[] tileBuffer = await RenderPreviewTileAsync(state, tile, previewWidth, previewHeight, TILE_SIZE);

                // Копируем данные в итоговый битмап
                lock (bmp)
                {
                    BitmapData bmpData = bmp.LockBits(tile.Bounds, ImageLockMode.WriteOnly, bmp.PixelFormat);
                    int tileRowBytes = tile.Bounds.Width * 4; // 32bpp = 4 байта
                    for (int y = 0; y < tile.Bounds.Height; y++)
                    {
                        IntPtr destPtr = IntPtr.Add(bmpData.Scan0, y * bmpData.Stride);
                        int sourceOffset = y * tileRowBytes;
                        Marshal.Copy(tileBuffer, sourceOffset, destPtr, tileRowBytes);
                    }
                    bmp.UnlockBits(bmpData);
                }
            }, CancellationToken.None);

            // Синхронно ждем завершения рендера превью.
            renderTask.GetAwaiter().GetResult();

            return bmp;
        }

        /// <summary>
        /// Асинхронно рендерит одну плитку (tile) для предпросмотра в менеджере сохранений.
        /// Это исправленная, рабочая версия.
        /// </summary>
        public virtual async Task<byte[]> RenderPreviewTileAsync(FractalSaveStateBase stateBase, TileInfo tile, int totalWidth, int totalHeight, int tileSize)
        {
            return await Task.Run(() =>
            {
                if (!(stateBase is MandelbrotFamilySaveState state))
                {
                    return new byte[tile.Bounds.Width * tile.Bounds.Height * 4];
                }

                // Для превью всегда используем EngineDecimal - хороший баланс скорости и качества.
                var engine = new EngineDecimal();

                // Настраиваем палитру
                var paletteForPreview = _paletteManager.Palettes.FirstOrDefault(p => p.Name == state.PaletteName) ?? _paletteManager.GetDefaultPalette();
                int maxIterations = Math.Min(state.Iterations, 150); // Ограничиваем итерации для быстрого превью
                engine.MaxIterations = maxIterations;
                engine.ThresholdSquared = (double)(state.Threshold * state.Threshold);
                int effectiveMaxColorIterations = paletteForPreview.AlignWithRenderIterations ? maxIterations : paletteForPreview.MaxColorIterations;
                var paletteFunc = GeneratePaletteFunction(paletteForPreview);
                engine.Palette = (iter, maxIter, maxColorIter) => iter >= maxIter ? Color.Black : paletteFunc(iter, maxIter, effectiveMaxColorIterations);
                engine.MaxColorIterations = effectiveMaxColorIterations;

                // Настраиваем параметры рендеринга
                var juliaC = state is JuliaFamilySaveState js ? new ComplexDecimal(js.CRe, js.CIm) : ComplexDecimal.Zero;
                Enum.TryParse(state.PreviewEngineType, out FractalType fractalType);
                var options = new RenderOptions
                {
                    Width = totalWidth,
                    Height = totalHeight,
                    CenterX = state.CenterX.ToString(CultureInfo.InvariantCulture),
                    CenterY = state.CenterY.ToString(CultureInfo.InvariantCulture),
                    Scale = (BaseScale / new BigDecimal(state.Zoom)).ToString(CultureInfo.InvariantCulture),
                    FractalType = fractalType,
                    JuliaC = juliaC
                };

                // Рендерим плитку пиксель за пикселем
                int tileWidth = tile.Bounds.Width;
                int tileHeight = tile.Bounds.Height;
                byte[] tileBuffer = new byte[tileWidth * tileHeight * 4]; // Формат BGRA (32bpp)

                for (int y = 0; y < tileHeight; y++)
                {
                    for (int x = 0; x < tileWidth; x++)
                    {
                        int px = tile.Bounds.X + x;
                        int py = tile.Bounds.Y + y;

                        // Получаем количество итераций для пикселя
                        int iterations = ((EngineBaseMandelbrotFamily)engine).GetIterationsForPixel(px, py, totalWidth, totalHeight, options);
                        // Получаем цвет
                        Color color = engine.Palette(iterations, engine.MaxIterations, engine.MaxColorIterations);

                        // Записываем цвет в буфер в формате BGRA
                        int index = (y * tileWidth + x) * 4;
                        tileBuffer[index] = color.B;
                        tileBuffer[index + 1] = color.G;
                        tileBuffer[index + 2] = color.R;
                        tileBuffer[index + 3] = color.A;
                    }
                }
                return tileBuffer;
            });
        }

        /// <inheritdoc/>
        public virtual List<FractalSaveStateBase> LoadAllSavesForThisType() { throw new NotImplementedException("Этот метод должен быть переопределен в дочернем классе."); }

        /// <inheritdoc/>
        public virtual void SaveAllSavesForThisType(List<FractalSaveStateBase> saves) { throw new NotImplementedException("Этот метод должен быть переопределен в дочернем классе."); }

        #endregion

        #region Unused/Dummy Implementations for Interfaces (for future use)

        /// <inheritdoc/>
        public double LoupeZoom => nudBaseScale != null ? (double)nudBaseScale.Value : 4.0;

        /// <inheritdoc/>
        public event EventHandler LoupeZoomChanged;

        /// <summary>
        /// Получает текущее состояние фрактала для рендеринга в высоком разрешении.
        /// </summary>
        public HighResRenderState GetRenderState()
        {
            var state = new HighResRenderState
            {
                EngineType = this.FractalTypeIdentifier,
                CenterX = (decimal)_centerX, // Может потерять точность, требует доработки для сверхглубин
                CenterY = (decimal)_centerY,
                Zoom = (decimal)_zoom,
                BaseScale = (decimal)this.BaseScale,
                Iterations = (int)nudIterations.Value,
                Threshold = nudThreshold.Value,
                ActivePaletteName = _paletteManager.ActivePalette?.Name ?? "Стандартный серый",
                FileNameDetails = this.GetSaveFileNameDetails()
            };

            if (this is FractalJulia || this is FractalJuliaBurningShip)
            {
                state.JuliaC = new ComplexDecimal(nudRe.Value, nudIm.Value);
            }

            return state;
        }

        /// <summary>
        /// Асинхронно рендерит фрактал в высоком разрешении (заглушка для будущей реализации).
        /// </summary>
        public async Task<Bitmap> RenderHighResolutionAsync(HighResRenderState state, int width, int height, int ssaaFactor, IProgress<RenderProgress> progress, CancellationToken cancellationToken)
        {
            // Эта логика может быть реализована аналогично StartRenderAsync, но с параметрами из 'state'
            return await Task.FromResult(new Bitmap(width, height));
        }

        /// <summary>
        /// Рендерит небольшое превью фрактала (заглушка для будущей реализации).
        /// </summary>
        public Bitmap RenderPreview(HighResRenderState state, int previewWidth, int previewHeight)
        {
            // Эта логика может быть реализована аналогично RenderPreview
            return new Bitmap(previewWidth, previewHeight);
        }

        #endregion
    }
}