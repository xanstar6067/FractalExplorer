using FractalExplorer.Utilities;
using FractalExplorer.Engines;
using FractalExplorer.Forms;
using FractalExplorer.Projects;
using FractalExplorer.Resources;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using FractalExplorer.Utilities.SaveIO.SaveStateImplementations;
using System.Text.Json;
using FractalExplorer.Utilities.SaveIO;
using FractalExplorer.Utilities.SaveIO.ColorPalettes;
using FractalExplorer.Utilities.JsonConverters; // Добавлен, так как он используется в Serpinsky. Возможно, здесь тоже пригодится или был пропущен.

namespace FractalDraving
{
    /// <summary>
    /// Базовый абстрактный класс для форм, отображающих фракталы семейства Мандельброта.
    /// Предоставляет общую логику для управления движком рендеринга,
    /// палитрой, масштабированием, панорамированием и сохранением изображений.
    /// </summary>
    public abstract partial class FractalMandelbrotFamilyForm : Form, IFractalForm, ISaveLoadCapableFractal
    {
        #region Fields

        /// <summary>
        /// Компонент для визуализации процесса рендеринга плиток.
        /// </summary>
        private RenderVisualizerComponent _renderVisualizer;

        /// <summary>
        /// Менеджер палитр, используемый этой формой для управления цветовыми схемами.
        /// </summary>
        private ColorPaletteMandelbrotFamily _paletteManager;

        /// <summary>
        /// Форма конфигурации палитр, связанная с этой формой, для настройки цветов.
        /// </summary>
        private ColorConfigurationMandelbrotFamilyForm _colorConfigForm;

        /// <summary>
        /// Размер одной плитки (тайла) в пикселях для пошагового рендеринга.
        /// Использование плиток позволяет отображать прогресс и быстрее реагировать на отмену.
        /// </summary>
        private const int TILE_SIZE = 32;

        /// <summary>
        /// Объект для блокировки доступа к битмапам во время операций рендеринга,
        /// чтобы предотвратить состояния гонки при модификации пикселей из разных потоков.
        /// </summary>
        private readonly object _bitmapLock = new object();

        /// <summary>
        /// Битмап, содержащий отрисованное изображение для предпросмотра фрактала.
        /// Это изображение может быть интерполировано при изменении масштаба или панорамирования до полного рендеринга.
        /// </summary>
        private Bitmap _previewBitmap;

        /// <summary>
        /// Битмап, в который в текущий момент происходит рендеринг плиток.
        /// Он отображается поверх <see cref="_previewBitmap"/> по мере готовности плиток.
        /// </summary>
        private Bitmap _currentRenderingBitmap;

        /// <summary>
        /// Токен отмены для операций рендеринга предпросмотра.
        /// Позволяет досрочно завершить рендеринг, например, при изменении параметров.
        /// </summary>
        private CancellationTokenSource _previewRenderCts;

        /// <summary>
        /// Флаг, указывающий, выполняется ли сейчас рендеринг в высоком разрешении для сохранения в файл.
        /// Этот режим блокирует интерактивные действия, чтобы обеспечить стабильность рендеринга.
        /// </summary>
        private volatile bool _isHighResRendering = false;

        /// <summary>
        /// Флаг, указывающий, выполняется ли сейчас рендеринг предпросмотра.
        /// Используется для предотвращения одновременного запуска нескольких операций рендеринга превью.
        /// </summary>
        private volatile bool _isRenderingPreview = false;

        /// <summary>
        /// Экземпляр движка для рендеринга фрактала, содержащий основную логику вычислений.
        /// </summary>
        protected FractalMandelbrotFamilyEngine _fractalEngine;

        /// <summary>
        /// Текущий коэффициент масштабирования фрактала.
        /// Меньшее значение соответствует большему увеличению.
        /// </summary>
        protected decimal _zoom = 1.0m;

        /// <summary>
        /// Текущая координата X (реальная часть) центра видимой области фрактала.
        /// </summary>
        protected decimal _centerX = 0.0m;

        /// <summary>
        /// Текущая координата Y (мнимая часть) центра видимой области фрактала.
        /// </summary>
        protected decimal _centerY = 0.0m;

        /// <summary>
        /// Координата X центра, по которой был отрисован текущий <see cref="_previewBitmap"/>.
        /// Используется для корректной интерполяции изображения при панорамировании/масштабировании.
        /// </summary>
        private decimal _renderedCenterX;

        /// <summary>
        /// Координата Y центра, по которой был отрисован текущий <see cref="_previewBitmap"/>.
        /// Используется для корректной интерполяции изображения при панорамировании/масштабировании.
        /// </summary>
        private decimal _renderedCenterY;

        /// <summary>
        /// Коэффициент масштабирования, по которому был отрисован текущий <see cref="_previewBitmap"/>.
        /// Используется для корректной интерполяции изображения при панорамировании/масштабировании.
        /// </summary>
        private decimal _renderedZoom;

        /// <summary>
        /// Начальная позиция курсора мыши при инициировании панорамирования.
        /// </summary>
        private Point _panStart;

        /// <summary>
        /// Флаг, указывающий, находится ли пользователь в режиме панорамирования.
        /// </summary>
        private bool _panning = false;

        /// <summary>
        /// Таймер для отложенного запуска рендеринга.
        /// Это позволяет агрегировать быстрые изменения параметров (например, ввод с ползунка)
        /// и запускать рендеринг только после небольшой паузы, снижая нагрузку на CPU.
        /// </summary>
        private System.Windows.Forms.Timer _renderDebounceTimer;

        #endregion

        #region Constructor

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="FractalMandelbrotFamilyForm"/>.
        /// Устанавливает начальные координаты центра фрактала.
        /// </summary>
        protected FractalMandelbrotFamilyForm()
        {
            InitializeComponent();
            _centerX = InitialCenterX;
            _centerY = InitialCenterY;
        }

        #endregion

        #region Protected Abstract/Virtual Methods

        /// <summary>
        /// Абстрактный метод, который должен быть реализован в производных классах
        /// для создания конкретного экземпляра движка фрактала, специфичного для данного фрактала.
        /// </summary>
        /// <returns>Экземпляр <see cref="FractalMandelbrotFamilyEngine"/>.</returns>
        protected abstract FractalMandelbrotFamilyEngine CreateEngine();

        /// <summary>
        /// Получает базовый масштаб для фрактала. Этот параметр определяет "размер" фрактала по умолчанию в комплексной плоскости.
        /// </summary>
        protected virtual decimal BaseScale => 3.0m;

        /// <summary>
        /// Получает начальную координату X (реальную часть) центра фрактала.
        /// </summary>
        protected virtual decimal InitialCenterX => -0.5m;

        /// <summary>
        /// Получает начальную координату Y (мнимую часть) центра фрактала.
        /// </summary>
        protected virtual decimal InitialCenterY => 0.0m;

        /// <summary>
        /// Виртуальный метод, который может быть переопределен в производных классах
        /// для обновления специфических параметров движка фрактала, которые не являются общими для всего семейства.
        /// </summary>
        protected virtual void UpdateEngineSpecificParameters() { }

        /// <summary>
        /// Виртуальный метод, вызываемый после завершения инициализации формы и всех ее компонентов.
        /// Может быть переопределен для дополнительной настройки UI или логики.
        /// </summary>
        protected virtual void OnPostInitialize() { }

        /// <summary>
        /// Виртуальный метод для получения дополнительных деталей для имени файла сохранения.
        /// Может быть переопределен в дочерних классах для включения специфичных для фрактала параметров в имя файла.
        /// </summary>
        /// <returns>Строка с деталями фрактала.</returns>
        protected virtual string GetSaveFileNameDetails() => "fractal";

        #endregion

        #region UI Initialization

        /// <summary>
        /// Инициализирует элементы управления формы, устанавливая их начальные значения и диапазоны.
        /// Настраивает количество доступных потоков для рендеринга, диапазоны итераций и порогов.
        /// </summary>
        private void InitializeControls()
        {
            int cores = Environment.ProcessorCount;
            cbThreads.Items.Clear();
            for (int i = 1; i <= cores; i++)
            {
                cbThreads.Items.Add(i);
            }
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

            nudZoom.DecimalPlaces = 4;
            nudZoom.Increment = 0.1m;
            nudZoom.Minimum = 0.001m;
            // nudZoom.Maximum = 1_000_000_000_000_000m; // Примечание для ИИ. Мне не нужно ограничивать отвюда предельное значение. Не удаляй этот комментарий.
            _zoom = BaseScale / 4.0m;
            nudZoom.Value = _zoom;

            // Настройка параметров для фракталов Жюлиа, если соответствующие элементы управления существуют.
            if (nudRe != null && nudIm != null)
            {
                nudRe.Minimum = -2m;
                nudRe.Maximum = 2m;
                nudRe.DecimalPlaces = 15;
                nudRe.Increment = 0.001m;
                nudRe.Value = -0.8m;
                nudIm.Minimum = -2m;
                nudIm.Maximum = 2m;
                nudIm.DecimalPlaces = 15;
                nudIm.Increment = 0.001m;
                nudIm.Value = 0.156m;
            }
        }

        /// <summary>
        /// Инициализирует обработчики событий для различных элементов управления формы.
        /// Включает обработку изменений параметров, действий мыши на холсте, кнопок и закрытия формы.
        /// </summary>
        private void InitializeEventHandlers()
        {
            // Обработчики изменений параметров, которые должны вызвать перерисовку.
            nudIterations.ValueChanged += ParamControl_Changed;
            nudThreshold.ValueChanged += ParamControl_Changed;
            cbThreads.SelectedIndexChanged += ParamControl_Changed;
            nudZoom.ValueChanged += ParamControl_Changed;
            if (nudRe != null)
            {
                nudRe.ValueChanged += ParamControl_Changed;
            }
            if (nudIm != null)
            {
                nudIm.ValueChanged += ParamControl_Changed;
            }

            // Обработчики нажатий кнопок.
            btnRender.Click += (s, e) => ScheduleRender();
            btnSaveHighRes.Click += btnSave_Click;

            // Динамический поиск кнопки конфигурации цвета для более гибкой архитектуры.
            var configButton = Controls.Find("color_configurations", true).FirstOrDefault();
            if (configButton != null)
            {
                configButton.Click += color_configurations_Click;
            }

            // Обработчики событий мыши и изменения размера для канваса фрактала.
            canvas.MouseWheel += Canvas_MouseWheel;
            canvas.MouseDown += Canvas_MouseDown;
            canvas.MouseMove += Canvas_MouseMove;
            canvas.MouseUp += Canvas_MouseUp;
            canvas.Paint += Canvas_Paint;
            canvas.Resize += (s, e) =>
            {
                // Запускаем рендеринг при изменении размера окна, если оно не свернуто,
                // чтобы изображение соответствовало новым размерам.
                if (WindowState != FormWindowState.Minimized)
                {
                    ScheduleRender();
                }
            };

            // Обработчик события закрытия формы для освобождения ресурсов.
            FormClosed += (s, e) =>
            {
                _renderDebounceTimer?.Stop();
                _renderDebounceTimer?.Dispose();

                // Отменяем активные операции рендеринга и даем им немного времени на завершение.
                if (_previewRenderCts != null)
                {
                    _previewRenderCts.Cancel();
                    Thread.Sleep(50);
                    _previewRenderCts.Dispose();
                }

                // Освобождаем ресурсы битмапов.
                lock (_bitmapLock)
                {
                    _previewBitmap?.Dispose();
                    _previewBitmap = null;
                    _currentRenderingBitmap?.Dispose();
                    _currentRenderingBitmap = null;
                }

                // Отписываемся от событий визуализатора и освобождаем его ресурсы.
                if (_renderVisualizer != null)
                {
                    _renderVisualizer.NeedsRedraw -= OnVisualizerNeedsRedraw;
                    _renderVisualizer.Dispose();
                }
            };
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Обработчик события загрузки формы.
        /// Инициализирует менеджеры, движки, таймеры и элементы UI,
        /// а также запускает первый рендеринг фрактала.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события.</param>
        private void FormBase_Load(object sender, EventArgs e)
        {
            _paletteManager = new ColorPaletteMandelbrotFamily();
            _fractalEngine = CreateEngine();
            _renderDebounceTimer = new System.Windows.Forms.Timer { Interval = 300 };
            _renderDebounceTimer.Tick += RenderDebounceTimer_Tick;
            _renderVisualizer = new RenderVisualizerComponent(TILE_SIZE);
            _renderVisualizer.NeedsRedraw += OnVisualizerNeedsRedraw;

            InitializeControls();
            InitializeEventHandlers();

            // Инициализация отображаемых параметров для корректной интерполяции
            // на канвасе до выполнения первого рендеринга.
            _renderedCenterX = _centerX;
            _renderedCenterY = _centerY;
            _renderedZoom = _zoom;
            OnPostInitialize(); // Вызов виртуального метода для дочерних классов.

            ApplyActivePalette(); // Применение активной палитры к движку.
            ScheduleRender(); // Планирование первого рендеринга.
        }

        /// <summary>
        /// Обработчик события, когда визуализатор рендеринга запрашивает перерисовку канваса.
        /// Используется для обновления визуализации плиток.
        /// </summary>
        private void OnVisualizerNeedsRedraw()
        {
            // Безопасно вызываем Invalidate() на UI потоке,
            // чтобы перерисовать канвас и обновить визуализацию плиток.
            if (canvas.IsHandleCreated && !canvas.IsDisposed)
            {
                canvas.BeginInvoke((Action)(() => canvas.Invalidate()));
            }
        }

        /// <summary>
        /// Обработчик события клика по кнопке конфигурации цвета.
        /// Открывает или активирует форму настройки палитры для фрактала.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события.</param>
        private void color_configurations_Click(object sender, EventArgs e)
        {
            // Создаем новую форму, если она еще не открыта или была закрыта.
            if (_colorConfigForm == null || _colorConfigForm.IsDisposed)
            {
                _colorConfigForm = new ColorConfigurationMandelbrotFamilyForm(_paletteManager);
                _colorConfigForm.PaletteApplied += OnPaletteApplied; // Подписываемся на событие применения палитры.
                _colorConfigForm.FormClosed += (s, args) => _colorConfigForm = null; // Обнуляем ссылку при закрытии формы.
                _colorConfigForm.Show(this);
            }
            else
            {
                _colorConfigForm.Activate(); // Если форма уже открыта, просто активируем ее.
            }
        }

        /// <summary>
        /// Обработчик события применения новой палитры.
        /// Обновляет палитру движка фрактала и планирует новый рендеринг для отображения изменений.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события.</param>
        private void OnPaletteApplied(object sender, EventArgs e)
        {
            ApplyActivePalette(); // Применяем выбранную палитру к движку.
            ScheduleRender(); // Запускаем рендеринг с новой палитрой.
        }

        /// <summary>
        /// Обработчик события изменения любого параметра фрактала на панели управления.
        /// Планирует новый рендеринг предпросмотра после небольшой задержки.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события.</param>
        private void ParamControl_Changed(object sender, EventArgs e)
        {
            // Игнорируем изменения, если в данный момент выполняется рендеринг в высоком разрешении,
            // чтобы избежать конфликтов и нестабильного поведения.
            if (_isHighResRendering)
            {
                return;
            }

            // Обновляем внутреннее значение зума, если оно изменилось через NumericUpDown.
            // Это необходимо, чтобы избежать рекурсивного вызова при программном изменении nudZoom.Value.
            if (sender == nudZoom && nudZoom.Value != _zoom)
            {
                _zoom = nudZoom.Value;
            }
            ScheduleRender(); // Планируем рендеринг, чтобы изменения вступили в силу.
        }

        /// <summary>
        /// Обработчик события прокрутки колеса мыши над канвасом.
        /// Изменяет масштаб фрактала и обновляет центр таким образом,
        /// чтобы точка под курсором оставалась на своем месте.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события колеса мыши.</param>
        private void Canvas_MouseWheel(object sender, MouseEventArgs e)
        {
            // Запрещаем зум, если идет рендеринг в высоком разрешении, чтобы не прерывать его.
            if (_isHighResRendering)
            {
                return;
            }

            // Объединяем текущий прогресс рендеринга в основной битмап предпросмотра.
            // Это делается, чтобы обеспечить плавное масштабирование уже отрисованных частей.
            CommitAndBakePreview();

            // Определяем фактор масштабирования: 1.5 для увеличения, 1/1.5 для уменьшения.
            decimal zoomFactor = e.Delta > 0 ? 1.5m : 1.0m / 1.5m;
            // Вычисляем текущее "количество единиц" комплексной плоскости на один пиксель до зума.
            decimal scaleBeforeZoom = BaseScale / _zoom;

            // Вычисляем мировые координаты точки под курсором до изменения масштаба.
            decimal mouseReal = _centerX + (e.X - canvas.Width / 2.0m) * scaleBeforeZoom / canvas.Width;
            decimal mouseImaginary = _centerY - (e.Y - canvas.Height / 2.0m) * scaleBeforeZoom / canvas.Height;

            // Применяем новый масштаб, ограничивая его допустимыми значениями.
            _zoom = Math.Max(nudZoom.Minimum, Math.Min(nudZoom.Maximum, _zoom * zoomFactor));

            // Вычисляем новое "количество единиц" комплексной плоскости на один пиксель после зума.
            decimal scaleAfterZoom = BaseScale / _zoom;

            // Пересчитываем новый центр фрактала так, чтобы точка, которая была под курсором,
            // осталась под ним после изменения масштаба.
            _centerX = mouseReal - (e.X - canvas.Width / 2.0m) * scaleAfterZoom / canvas.Width;
            _centerY = mouseImaginary + (e.Y - canvas.Height / 2.0m) * scaleAfterZoom / canvas.Height;

            canvas.Invalidate(); // Запрашиваем немедленную перерисовку для плавного эффекта зума.

            // Обновляем значение NumericUpDown для зума.
            // Проверяем, чтобы избежать рекурсивного вызова события ValueChanged.
            if (nudZoom.Value != _zoom)
            {
                nudZoom.Value = _zoom;
            }
            else
            {
                // Если значение nudZoom не изменилось (например, достигнуты Max/Min),
                // все равно планируем рендеринг, чтобы обновить фрактал в новом масштабе.
                ScheduleRender();
            }
        }

        /// <summary>
        /// Обработчик события нажатия кнопки мыши над канвасом.
        /// Инициирует режим панорамирования при нажатии левой кнопки мыши.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события мыши.</param>
        private void Canvas_MouseDown(object sender, MouseEventArgs e)
        {
            // Запрещаем панорамирование, если идет рендеринг в высоком разрешении.
            if (_isHighResRendering)
            {
                return;
            }
            if (e.Button == MouseButtons.Left)
            {
                _panning = true; // Активируем флаг панорамирования.
                _panStart = e.Location; // Сохраняем начальную позицию курсора.
                canvas.Cursor = Cursors.Hand; // Изменяем курсор на "руку" для визуальной обратной связи.
            }
        }

        /// <summary>
        /// Обработчик события перемещения мыши над канвасом.
        /// Выполняет панорамирование фрактала, перемещая центр в соответствии с движением мыши.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события мыши.</param>
        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            // Пропускаем, если идет рендеринг в высоком разрешении или панорамирование не активно.
            if (_isHighResRendering || !_panning)
            {
                return;
            }

            // Объединяем текущий прогресс рендеринга в основной битмап предпросмотра.
            // Это делается для того, чтобы при быстром панорамировании не было "пустых" областей.
            CommitAndBakePreview();

            // Вычисляем количество единиц комплексной плоскости, соответствующих одному пикселю.
            decimal unitsPerPixel = BaseScale / _zoom / canvas.Width;

            // Обновляем центр фрактала на основе смещения мыши.
            // Вычитаем для X, потому что увеличение X смещает фрактал влево.
            // Прибавляем для Y, потому что увеличение Y смещает фрактал вверх на экране.
            _centerX -= (decimal)(e.X - _panStart.X) * unitsPerPixel;
            _centerY += (decimal)(e.Y - _panStart.Y) * unitsPerPixel;
            _panStart = e.Location; // Обновляем начальную точку панорамирования для следующего шага.

            canvas.Invalidate(); // Запрашиваем немедленную перерисовку для плавного панорамирования.
            ScheduleRender(); // Планируем новый рендеринг для высокой четкости после завершения панорамирования.
        }

        /// <summary>
        /// Обработчик события отпускания кнопки мыши над канвасом.
        /// Завершает режим панорамирования при отпускании левой кнопки мыши.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события мыши.</param>
        private void Canvas_MouseUp(object sender, MouseEventArgs e)
        {
            // Игнорируем, если идет рендеринг в высоком разрешении, так как панорамирование блокируется.
            if (_isHighResRendering)
            {
                return;
            }
            if (e.Button == MouseButtons.Left)
            {
                _panning = false; // Сбрасываем флаг панорамирования.
                canvas.Cursor = Cursors.Default; // Возвращаем стандартный курсор.
            }
        }

        /// <summary>
        /// Обработчик события отрисовки канваса.
        /// Отображает текущий предпросмотр фрактала и, при необходимости,
        /// обновляющиеся плитки рендеринга поверх него.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события рисования.</param>
        private void Canvas_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.Clear(Color.Black); // Очищаем фон канваса черным цветом.

            lock (_bitmapLock)
            {
                // Если есть готовый битмап предпросмотра и канвас имеет корректные размеры.
                if (_previewBitmap != null && canvas.Width > 0 && canvas.Height > 0)
                {
                    // Если параметры фрактала (центр и зум) не изменились с момента последнего полного рендеринга,
                    // просто рисуем битмап без масштабирования.
                    if (_renderedCenterX == _centerX && _renderedCenterY == _centerY && _renderedZoom == _zoom)
                    {
                        e.Graphics.DrawImageUnscaled(_previewBitmap, Point.Empty);
                    }
                    else
                    {
                        // Если параметры изменились (например, при панорамировании или зуме до нового рендеринга),
                        // интерполируем существующий битмап, чтобы создать эффект плавного движения.
                        try
                        {
                            // Вычисляем ширину и высоту области фрактала в комплексной плоскости для отрисованного и текущего состояния.
                            decimal renderedComplexWidth = BaseScale / _renderedZoom;
                            decimal currentComplexWidth = BaseScale / _zoom;

                            // Защита от деления на ноль или некорректных значений зума.
                            if (!(_renderedZoom <= 0 || _zoom <= 0 || renderedComplexWidth <= 0 || currentComplexWidth <= 0))
                            {
                                // Вычисляем количество единиц комплексной плоскости на один пиксель для обоих состояний.
                                decimal unitsPerPixelRendered = renderedComplexWidth / _previewBitmap.Width;
                                decimal unitsPerPixelCurrent = currentComplexWidth / canvas.Width;

                                // Вычисляем минимальные реальные и максимальные мнимые координаты
                                // для области, покрываемой отрендеренным битмапом.
                                decimal renderedReMin = _renderedCenterX - (renderedComplexWidth / 2.0m);
                                decimal renderedImMax = _renderedCenterY + (_previewBitmap.Height * unitsPerPixelRendered / 2.0m);

                                // Вычисляем минимальные реальные и максимальные мнимые координаты
                                // для текущей видимой области канваса.
                                decimal currentReMin = _centerX - (currentComplexWidth / 2.0m);
                                decimal currentImMax = _centerY + (canvas.Height * unitsPerPixelCurrent / 2.0m);

                                // Вычисляем смещение в пикселях, чтобы правильно расположить интерполированное изображение.
                                decimal offsetXPixels = (renderedReMin - currentReMin) / unitsPerPixelCurrent;
                                decimal offsetYPixels = (currentImMax - renderedImMax) / unitsPerPixelCurrent;

                                // Вычисляем новый размер интерполированного изображения в пикселях.
                                decimal newWidthPixels = _previewBitmap.Width * (unitsPerPixelRendered / unitsPerPixelCurrent);
                                decimal newHeightPixels = _previewBitmap.Height * (unitsPerPixelRendered / unitsPerPixelCurrent);

                                // Устанавливаем режим интерполяции для лучшего качества при масштабировании.
                                e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Bilinear;

                                // Отрисовываем интерполированное изображение с учетом смещения и масштаба.
                                PointF destPoint1 = new PointF((float)offsetXPixels, (float)offsetYPixels);
                                PointF destPoint2 = new PointF((float)(offsetXPixels + newWidthPixels), (float)offsetYPixels);
                                PointF destPoint3 = new PointF((float)offsetXPixels, (float)(offsetYPixels + newHeightPixels));
                                e.Graphics.DrawImage(_previewBitmap, new PointF[] { destPoint1, destPoint2, destPoint3 });
                            }
                        }
                        catch (ArgumentException)
                        {
                            // Если интерполяция не удалась из-за некорректных аргументов (например, слишком малые размеры),
                            // рисуем битмап без масштабирования как запасной вариант, чтобы избежать падения приложения.
                            if (_previewBitmap != null)
                            {
                                e.Graphics.DrawImageUnscaled(_previewBitmap, Point.Empty);
                            }
                        }
                    }
                }
                // Если идет текущий рендеринг (плитками), рисуем его поверх предпросмотра.
                // Это создает эффект постепенного проявления деталей.
                if (_currentRenderingBitmap != null)
                {
                    e.Graphics.DrawImageUnscaled(_currentRenderingBitmap, Point.Empty);
                }
            }

            // Рисуем визуализатор процесса рендеринга (сетка плиток, показывающая прогресс).
            if (_renderVisualizer != null && _isRenderingPreview)
            {
                _renderVisualizer.DrawVisualization(e.Graphics);
            }
        }

        /// <summary>
        /// Обработчик события тика таймера задержки рендеринга.
        /// Запускает рендеринг предпросмотра, если нет других активных рендерингов.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события.</param>
        private async void RenderDebounceTimer_Tick(object sender, EventArgs e)
        {
            _renderDebounceTimer.Stop(); // Останавливаем таймер, так как его задача выполнена.

            // Если уже идет рендеринг в высоком разрешении или предпросмотр,
            // откладываем выполнение еще раз, чтобы избежать конфликтов.
            if (_isHighResRendering || _isRenderingPreview)
            {
                ScheduleRender();
                return;
            }
            await StartPreviewRender(); // Запускаем асинхронный рендеринг предпросмотра.
        }

        /// <summary>
        /// Обработчик события клика по кнопке сохранения изображения в высоком разрешении.
        /// Запускает асинхронный процесс рендеринга и сохранения фрактала в PNG файл.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события.</param>
        private async void btnSave_Click(object sender, EventArgs e)
        {
            // Предотвращаем запуск нескольких операций сохранения одновременно.
            if (_isHighResRendering)
            {
                MessageBox.Show("Процесс сохранения уже запущен.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int saveWidth = (int)nudSaveWidth.Value;
            int saveHeight = (int)nudSaveHeight.Value;
            string fractalDetails = GetSaveFileNameDetails();
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string suggestedFileName = $"{fractalDetails}_{timestamp}.png";

            using (var saveDialog = new SaveFileDialog { Filter = "PNG Image|*.png", Title = "Сохранить фрактал (Высокое разрешение)", FileName = suggestedFileName })
            {
                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    // Отменяем текущий рендеринг предпросмотра, если он активен,
                    // так как мы переходим к более важному рендерингу высокого разрешения.
                    if (_isRenderingPreview)
                    {
                        _previewRenderCts?.Cancel();
                    }

                    _isHighResRendering = true; // Устанавливаем флаг, что начался рендеринг высокого разрешения.
                    pnlControls.Enabled = false; // Блокируем элементы управления, чтобы пользователь не менял параметры во время рендеринга.

                    // Показываем и сбрасываем прогресс-бар для рендеринга высокого разрешения.
                    pbHighResProgress.Value = 0;
                    pbHighResProgress.Visible = true;

                    try
                    {
                        // Создаем отдельный движок для рендеринга высокого разрешения.
                        // Это обеспечивает независимость процесса сохранения от текущего состояния UI движка.
                        FractalMandelbrotFamilyEngine renderEngine = CreateEngine();
                        UpdateEngineParameters(); // Обновляем параметры, чтобы убедиться, что они актуальны.

                        // Копируем все необходимые параметры из текущего движка в движок для высокого разрешения.
                        renderEngine.MaxIterations = _fractalEngine.MaxIterations;
                        renderEngine.ThresholdSquared = _fractalEngine.ThresholdSquared;
                        renderEngine.CenterX = _fractalEngine.CenterX;
                        renderEngine.CenterY = _fractalEngine.CenterY;
                        renderEngine.Scale = _fractalEngine.Scale;

                        // Специальная обработка для фракталов Жюлиа, так как у них есть дополнительный параметр C.
                        if (this is FractalJulia || this is FractalJuliaBurningShip)
                        {
                            renderEngine.C = new ComplexDecimal(nudRe.Value, nudIm.Value);
                        }
                        else
                        {
                            renderEngine.C = _fractalEngine.C;
                        }

                        renderEngine.Palette = GeneratePaletteFunction(_paletteManager.ActivePalette);
                        renderEngine.MaxColorIterations = _fractalEngine.MaxColorIterations;

                        int threadCount = GetThreadCount();

                        // Запускаем рендеринг в высоком разрешении в фоновом потоке,
                        // чтобы не блокировать основной поток UI.
                        Bitmap highResBitmap = await Task.Run(() => renderEngine.RenderToBitmap(
                            saveWidth, saveHeight, threadCount,
                            progress =>
                            {
                                // Обновляем прогресс-бар на UI потоке, используя Invoke для безопасности.
                                if (pbHighResProgress.IsHandleCreated && !pbHighResProgress.IsDisposed)
                                {
                                    pbHighResProgress.Invoke((Action)(() =>
                                    {
                                        pbHighResProgress.Value = Math.Min(pbHighResProgress.Maximum, progress);
                                    }));
                                }
                            }
                        ));

                        highResBitmap.Save(saveDialog.FileName, ImageFormat.Png); // Сохраняем отрендеренное изображение.
                        highResBitmap.Dispose(); // Освобождаем ресурсы битмапа.
                        MessageBox.Show("Изображение успешно сохранено!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        _isHighResRendering = false; // Сбрасываем флаг рендеринга высокого разрешения.
                        pnlControls.Enabled = true; // Разблокируем элементы управления.
                        // Скрываем и сбрасываем прогресс-бар на UI потоке.
                        if (pbHighResProgress.IsHandleCreated && !pbHighResProgress.IsDisposed)
                        {
                            pbHighResProgress.Invoke((Action)(() =>
                            {
                                pbHighResProgress.Visible = false;
                                pbHighResProgress.Value = 0;
                            }));
                        }
                        ScheduleRender(); // Запускаем рендеринг предпросмотра, если он был отменен или нужен новый.
                    }
                }
            }
        }

        #endregion

        #region Rendering Logic

        /// <summary>
        /// Запускает процесс рендеринга предпросмотра фрактала в фоновом потоке.
        /// Рендеринг выполняется по плиткам с отображением прогресса и возможностью отмены.
        /// </summary>
        /// <returns>Задача, представляющая асинхронную операцию рендеринга.</returns>
        private async Task StartPreviewRender()
        {
            // Пропускаем рендеринг, если канвас имеет некорректные размеры.
            if (canvas.Width <= 0 || canvas.Height <= 0)
            {
                return;
            }

            _isRenderingPreview = true; // Устанавливаем флаг, что рендеринг предпросмотра активен.
            _previewRenderCts?.Cancel(); // Отменяем предыдущий рендеринг, если он еще активен.
            _previewRenderCts = new CancellationTokenSource(); // Создаем новый источник токена отмены.
            var token = _previewRenderCts.Token; // Получаем токен отмены.

            _renderVisualizer?.NotifyRenderSessionStart(); // Уведомляем визуализатор о начале новой сессии рендеринга.

            // Создаем новый битмап для текущего рендеринга плиток.
            var newRenderingBitmap = new Bitmap(canvas.Width, canvas.Height, PixelFormat.Format32bppArgb);
            lock (_bitmapLock)
            {
                _currentRenderingBitmap?.Dispose(); // Освобождаем ресурсы старого текущего битмапа.
                _currentRenderingBitmap = newRenderingBitmap; // Устанавливаем новый битмап как текущий.
            }

            UpdateEngineParameters(); // Обновляем параметры движка перед началом рендеринга.

            // Сохраняем текущие параметры вида, чтобы позже знать, для какой области был выполнен этот рендеринг.
            var currentRenderedCenterX = _centerX;
            var currentRenderedCenterY = _centerY;
            var currentRenderedZoom = _zoom;

            // Создаем копию движка для безопасного использования в параллельных потоках.
            // Это важно, так как движок содержит состояние, и его модификация из разных потоков может привести к ошибкам.
            var renderEngineCopy = CreateEngine();
            renderEngineCopy.MaxIterations = _fractalEngine.MaxIterations;
            renderEngineCopy.ThresholdSquared = _fractalEngine.ThresholdSquared;
            renderEngineCopy.CenterX = _fractalEngine.CenterX;
            renderEngineCopy.CenterY = _fractalEngine.CenterY;
            renderEngineCopy.Scale = _fractalEngine.Scale;
            renderEngineCopy.C = _fractalEngine.C;
            renderEngineCopy.Palette = _fractalEngine.Palette;
            renderEngineCopy.MaxColorIterations = _fractalEngine.MaxColorIterations;

            // Генерируем плитки для рендеринга, сортируя их от центра к краям.
            // Это дает ощущение, что центральная часть фрактала появляется быстрее.
            var tiles = GenerateTiles(canvas.Width, canvas.Height);
            var dispatcher = new TileRenderDispatcher(tiles, GetThreadCount());

            // Инициализируем прогресс-бар на UI потоке.
            if (pbRenderProgress.IsHandleCreated && !pbRenderProgress.IsDisposed)
            {
                pbRenderProgress.Invoke((Action)(() =>
                {
                    pbRenderProgress.Value = 0;
                    pbRenderProgress.Maximum = tiles.Count;
                }));
            }
            int progress = 0;

            try
            {
                // Запускаем асинхронный рендеринг плиток, используя диспетчер.
                await dispatcher.RenderAsync(async (tile, ct) =>
                {
                    ct.ThrowIfCancellationRequested(); // Проверка на отмену перед началом рендеринга плитки.

                    _renderVisualizer?.NotifyTileRenderStart(tile.Bounds); // Уведомляем визуализатор о начале рендеринга плитки.

                    // Рендерим одну плитку.
                    var tileBuffer = renderEngineCopy.RenderSingleTile(tile, canvas.Width, canvas.Height, out int bytesPerPixel);

                    ct.ThrowIfCancellationRequested(); // Проверка на отмену после рендеринга плитки, но до записи в битмап.

                    lock (_bitmapLock)
                    {
                        // Если рендеринг был отменен или запущен новый рендеринг, не записываем в старый битмап,
                        // чтобы избежать записи в уже освобожденные или замененные ресурсы.
                        if (ct.IsCancellationRequested || _currentRenderingBitmap != newRenderingBitmap)
                        {
                            return;
                        }

                        // Записываем данные плитки в основной битмап.
                        var tileRect = tile.Bounds;
                        var bitmapRect = new Rectangle(0, 0, _currentRenderingBitmap.Width, _currentRenderingBitmap.Height);
                        tileRect.Intersect(bitmapRect); // Обрезаем Rect, чтобы не выйти за границы битмапа.

                        if (tileRect.Width == 0 || tileRect.Height == 0)
                        {
                            return;
                        }

                        // Блокируем часть битмапа для прямой записи пикселей,
                        // обеспечивая безопасный доступ из нескольких потоков.
                        BitmapData bmpData = _currentRenderingBitmap.LockBits(tileRect, ImageLockMode.WriteOnly, _currentRenderingBitmap.PixelFormat);
                        int originalTileWidthInBytes = tile.Bounds.Width * bytesPerPixel;

                        for (int y = 0; y < tileRect.Height; y++)
                        {
                            IntPtr destPtr = IntPtr.Add(bmpData.Scan0, y * bmpData.Stride);
                            // Вычисляем смещение в исходном буфере плитки.
                            int srcOffset = ((y + tileRect.Y) - tile.Bounds.Y) * originalTileWidthInBytes + ((tileRect.X - tile.Bounds.X) * bytesPerPixel);
                            Marshal.Copy(tileBuffer, srcOffset, destPtr, tileRect.Width * bytesPerPixel);
                        }
                        _currentRenderingBitmap.UnlockBits(bmpData);
                    }

                    _renderVisualizer?.NotifyTileRenderComplete(tile.Bounds); // Уведомляем визуализатор о завершении рендеринга плитки.

                    // Обновляем прогресс-бар на UI потоке, если операция не была отменена.
                    if (ct.IsCancellationRequested || !canvas.IsHandleCreated || canvas.IsDisposed)
                    {
                        return;
                    }
                    canvas.Invoke((Action)(() =>
                    {
                        if (!ct.IsCancellationRequested && pbRenderProgress.IsHandleCreated && !pbRenderProgress.IsDisposed)
                        {
                            pbRenderProgress.Value = Math.Min(pbRenderProgress.Maximum, Interlocked.Increment(ref progress));
                        }
                    }));
                    await Task.Yield(); // Освобождаем поток для UI для поддержания отзывчивости.
                }, token);

                token.ThrowIfCancellationRequested(); // Финальная проверка на отмену после завершения всех плиток.

                // По завершении рендеринга, заменяем основной битмап предпросмотра текущим.
                lock (_bitmapLock)
                {
                    if (_currentRenderingBitmap == newRenderingBitmap)
                    {
                        _previewBitmap?.Dispose(); // Освобождаем старый предпросмотр.
                        _previewBitmap = _currentRenderingBitmap; // Новый битмап становится предпросмотром.
                        _currentRenderingBitmap = null; // Обнуляем ссылку на текущий рендеринг битмапа.
                        // Сохраняем параметры, по которым был отрисован _previewBitmap, для интерполяции.
                        _renderedCenterX = currentRenderedCenterX;
                        _renderedCenterY = currentRenderedCenterY;
                        _renderedZoom = currentRenderedZoom;
                    }
                    else
                    {
                        newRenderingBitmap?.Dispose(); // Если битмап был заменен другим рендерингом, освобождаем текущий.
                    }
                }
                // Запрашиваем финальную перерисовку канваса для отображения полностью отрендеренного изображения.
                if (canvas.IsHandleCreated && !canvas.IsDisposed)
                {
                    canvas.Invalidate();
                }
            }
            catch (OperationCanceledException)
            {
                // Если операция была отменена, освобождаем текущий битмап, так как он не будет использоваться.
                lock (_bitmapLock)
                {
                    if (_currentRenderingBitmap == newRenderingBitmap)
                    {
                        _currentRenderingBitmap?.Dispose();
                        _currentRenderingBitmap = null;
                    }
                }
                newRenderingBitmap?.Dispose();
            }
            catch (Exception ex)
            {
                // Обрабатываем другие исключения, освобождаем битмап и показываем сообщение об ошибке.
                newRenderingBitmap?.Dispose();
                if (IsHandleCreated && !IsDisposed)
                {
                    MessageBox.Show($"Ошибка рендеринга: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            finally
            {
                _isRenderingPreview = false; // Сбрасываем флаг рендеринга предпросмотра.
                _renderVisualizer?.NotifyRenderSessionComplete(); // Уведомляем визуализатор о завершении сессии.
                // Сбрасываем прогресс-бар на UI потоке.
                if (pbRenderProgress.IsHandleCreated && !pbRenderProgress.IsDisposed)
                {
                    pbRenderProgress.Invoke((Action)(() => pbRenderProgress.Value = 0));
                }
            }
        }

        /// <summary>
        /// Генерирует список плиток для рендеринга.
        /// Плитки сортируются по удаленности от центра,
        /// чтобы обеспечить более быстрый рендеринг центральной части.
        /// </summary>
        /// <param name="width">Общая ширина области рендеринга.</param>
        /// <param name="height">Общая высота области рендеринга.</param>
        /// <returns>Список объектов <see cref="TileInfo"/>.</returns>
        private List<TileInfo> GenerateTiles(int width, int height)
        {
            var tiles = new List<TileInfo>();
            Point center = new Point(width / 2, height / 2);

            for (int y = 0; y < height; y += TILE_SIZE)
            {
                for (int x = 0; x < width; x += TILE_SIZE)
                {
                    tiles.Add(new TileInfo(x, y, TILE_SIZE, TILE_SIZE));
                }
            }
            // Сортируем плитки, чтобы сначала рендерились те, что ближе к центру.
            // Это улучшает визуальное восприятие прогресса рендеринга.
            return tiles.OrderBy(t => Math.Pow(t.Center.X - center.X, 2) + Math.Pow(t.Center.Y - center.Y, 2)).ToList();
        }

        /// <summary>
        /// Планирует рендеринг предпросмотра, используя таймер задержки.
        /// Это предотвращает избыточные рендеринги при частых изменениях параметров
        /// (например, при перетаскивании ползунка), агрегируя запросы на рендеринг.
        /// </summary>
        private void ScheduleRender()
        {
            // Не планируем рендеринг, если идет рендеринг в высоком разрешении
            // или если окно свернуто, чтобы избежать ненужных вычислений.
            if (_isHighResRendering || WindowState == FormWindowState.Minimized)
            {
                return;
            }
            // Если уже идет рендеринг предпросмотра, отменяем его,
            // так как новые параметры делают текущий рендеринг устаревшим.
            if (_isRenderingPreview)
            {
                _previewRenderCts?.Cancel();
            }
            _renderDebounceTimer.Stop(); // Останавливаем таймер, чтобы сбросить отсчет.
            _renderDebounceTimer.Start(); // Запускаем таймер заново.
        }

        /// <summary>
        /// Объединяет текущий рендеринг предпросмотра (<see cref="_currentRenderingBitmap"/>)
        /// с существующим основным битмапом предпросмотра (<see cref="_previewBitmap"/>).
        /// Эта операция "запекает" прогресс в основной битмап, что позволяет
        /// плавно интерактивно перемещаться/масштабировать уже отрисованные части.
        /// </summary>
        private void CommitAndBakePreview()
        {
            lock (_bitmapLock)
            {
                // Нечего "запекать", если нет активного рендеринга или текущего битмапа.
                if (!_isRenderingPreview || _currentRenderingBitmap == null)
                {
                    return;
                }
            }

            _previewRenderCts?.Cancel(); // Отменяем текущий процесс рендеринга плиток.
            // Примечание: Task.Run для RenderAsync может все еще продолжать выполняться
            // некоторое время после отмены, но его результаты не будут записаны в _currentRenderingBitmap
            // из-за проверки токена отмены.

            lock (_bitmapLock)
            {
                // Повторная проверка после получения блокировки.
                if (_currentRenderingBitmap == null)
                {
                    return;
                }

                // Создаем новый битмап для сохранения объединенного изображения.
                // Формат 24bppRgb используется для экономии памяти, так как альфа-канал не нужен.
                var bakedBitmap = new Bitmap(canvas.Width, canvas.Height, PixelFormat.Format24bppRgb);
                using (var g = Graphics.FromImage(bakedBitmap))
                {
                    g.Clear(Color.Black);
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Bilinear; // Для плавного масштабирования.

                    // Отрисовываем старый предпросмотр (если есть), интерполируя его до текущего вида.
                    // Это создает эффект "продолжения" движения.
                    if (_previewBitmap != null)
                    {
                        try
                        {
                            decimal renderedComplexWidth = BaseScale / _renderedZoom;
                            decimal currentComplexWidth = BaseScale / _zoom;

                            if (!(_renderedZoom <= 0 || _zoom <= 0 || renderedComplexWidth <= 0 || currentComplexWidth <= 0))
                            {
                                decimal unitsPerPixelRendered = renderedComplexWidth / _previewBitmap.Width;
                                decimal unitsPerPixelCurrent = currentComplexWidth / canvas.Width;

                                decimal renderedReMin = _renderedCenterX - (renderedComplexWidth / 2.0m);
                                decimal renderedImMax = _renderedCenterY + (_previewBitmap.Height * unitsPerPixelRendered / 2.0m);

                                decimal currentReMin = _centerX - (currentComplexWidth / 2.0m);
                                decimal currentImMax = _centerY + (canvas.Height * unitsPerPixelCurrent / 2.0m);

                                decimal offsetXPixels = (renderedReMin - currentReMin) / unitsPerPixelCurrent;
                                decimal offsetYPixels = (currentImMax - renderedImMax) / unitsPerPixelCurrent;
                                decimal newWidthPixels = _previewBitmap.Width * (unitsPerPixelRendered / unitsPerPixelCurrent);
                                decimal newHeightPixels = _previewBitmap.Height * (unitsPerPixelRendered / unitsPerPixelCurrent);

                                PointF destPoint1 = new PointF((float)offsetXPixels, (float)offsetYPixels);
                                PointF destPoint2 = new PointF((float)(offsetXPixels + newWidthPixels), (float)offsetYPixels);
                                PointF destPoint3 = new PointF((float)offsetXPixels, (float)(offsetYPixels + newHeightPixels));
                                g.DrawImage(_previewBitmap, new PointF[] { destPoint1, destPoint2, destPoint3 });
                            }
                        }
                        catch (Exception)
                        {
                            // Ошибки при интерполяции игнорируются, так как это вспомогательная функция.
                            // Если что-то пошло не так, просто не будем отрисовывать старое превью.
                        }
                    }
                    // Отрисовываем текущие, уже отрисованные плитки поверх (без масштабирования).
                    // Это гарантирует, что все, что уже было просчитано, будет включено в "запеченное" изображение.
                    g.DrawImageUnscaled(_currentRenderingBitmap, Point.Empty);
                }

                _previewBitmap?.Dispose(); // Освобождаем старый предпросмотр.
                _previewBitmap = bakedBitmap; // Новый объединенный битмап становится основным предпросмотром.
                _currentRenderingBitmap.Dispose(); // Освобождаем текущий битмап рендеринга.
                _currentRenderingBitmap = null; // Обнуляем ссылку.

                // Обновляем параметры, по которым был отрисован _previewBitmap,
                // чтобы он соответствовал текущему состоянию.
                _renderedCenterX = _centerX;
                _renderedCenterY = _centerY;
                _renderedZoom = _zoom;
            }
        }

        /// <summary>
        /// Обновляет параметры движка фрактала на основе текущих значений элементов управления.
        /// Это гарантирует, что следующий рендеринг будет использовать актуальные настройки.
        /// </summary>
        private void UpdateEngineParameters()
        {
            _fractalEngine.MaxIterations = (int)nudIterations.Value;
            _fractalEngine.ThresholdSquared = nudThreshold.Value * nudThreshold.Value;
            _fractalEngine.CenterX = _centerX;
            _fractalEngine.CenterY = _centerY;
            _fractalEngine.Scale = BaseScale / _zoom;
            UpdateEngineSpecificParameters(); // Вызов виртуального метода для специфичных параметров фрактала.
            ApplyActivePalette(); // Убеждаемся, что палитра также обновлена и применена к движку.
        }

        #endregion

        #region Palette Management

        /// <summary>
        /// Генерирует функцию палитры на основе выбранной палитры из менеджера.
        /// Эта функция преобразует количество итераций точки в ее цвет.
        /// </summary>
        /// <param name="palette">Объект палитры, содержащий настройки цвета.</param>
        /// <returns>Функция <c>Func<int, int, int, Color></c>, преобразующая количество итераций,
        /// максимальное количество итераций и максимальное количество цветовых итераций в цвет.</returns>
        private Func<int, int, int, Color> GeneratePaletteFunction(PaletteManagerMandelbrotFamily palette)
        {
            // Специальная обработка для стандартной серой палитры с логарифмическим сглаживанием.
            if (palette.Name == "Стандартный серый")
            {
                return (iter, maxIter, maxColorIterations) =>
                {
                    if (iter == maxIter)
                    {
                        return Color.Black; // Точки, входящие в множество, черные.
                    }
                    // Логарифмическое сглаживание для более плавного перехода цветов,
                    // особенно при большом количестве итераций.
                    double tLog = Math.Log(Math.Min(iter, maxColorIterations) + 1) / Math.Log(maxColorIterations + 1);
                    int cVal = (int)(255.0 * (1 - tLog));
                    return Color.FromArgb(cVal, cVal, cVal);
                };
            }

            var colors = new List<Color>(palette.Colors);
            bool isGradient = palette.IsGradient;
            int colorCount = colors.Count;

            // Обработка крайних случаев: пустая палитра или палитра с одним цветом.
            if (colorCount == 0)
            {
                return (iter, max, clrMax) => Color.Black;
            }
            if (colorCount == 1)
            {
                return (iter, max, clrMax) => (iter == max) ? Color.Black : colors[0];
            }

            // Основная логика генерации функции палитры.
            return (iter, maxIter, maxColorIterations) =>
            {
                if (iter == maxIter)
                {
                    return Color.Black; // Точки, входящие в множество, черные.
                }

                if (isGradient)
                {
                    // Линейная интерполяция между цветами палитры для плавных переходов.
                    double t = (double)Math.Min(iter, maxColorIterations) / maxColorIterations;
                    double scaledT = t * (colorCount - 1);
                    int index1 = (int)Math.Floor(scaledT);
                    int index2 = Math.Min(index1 + 1, colorCount - 1);
                    double localT = scaledT - index1; // Локальный коэффициент интерполяции между двумя цветами.
                    return LerpColor(colors[index1], colors[index2], localT);
                }
                else
                {
                    // Циклическое использование цветов палитры для повторяющегося узора.
                    int index = Math.Min(iter, maxColorIterations) % colorCount;
                    return colors[index];
                }
            };
        }

        /// <summary>
        /// Выполняет линейную интерполяцию между двумя цветами на основе коэффициента.
        /// </summary>
        /// <param name="a">Начальный цвет.</param>
        /// <param name="b">Конечный цвет.</param>
        /// <param name="t">Коэффициент интерполяции (от 0 до 1), где 0 - цвет A, 1 - цвет B.</param>
        /// <returns>Интерполированный цвет.</returns>
        private Color LerpColor(Color a, Color b, double t)
        {
            t = Math.Max(0, Math.Min(1, t)); // Ограничиваем t в пределах [0, 1] для корректной интерполяции.
            return Color.FromArgb(
                (int)(a.A + (b.A - a.A) * t),
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t)
            );
        }

        /// <summary>
        /// Применяет текущую активную палитру из менеджера палитр к движку фрактала.
        /// </summary>
        private void ApplyActivePalette()
        {
            // Пропускаем, если движок фрактала или активная палитра не инициализированы.
            if (_fractalEngine == null || _paletteManager.ActivePalette == null)
            {
                return;
            }
            _fractalEngine.Palette = GeneratePaletteFunction(_paletteManager.ActivePalette);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Определяет количество потоков для использования в параллельных вычислениях.
        /// Если выбрано "Auto", возвращает количество логических процессоров системы.
        /// </summary>
        /// <returns>Количество потоков.</returns>
        private int GetThreadCount()
        {
            return cbThreads.SelectedItem?.ToString() == "Auto" ? Environment.ProcessorCount : Convert.ToInt32(cbThreads.SelectedItem);
        }

        /// <summary>
        /// Вспомогательный метод для ограничения значения <c>decimal</c> в заданном диапазоне.
        /// Используется для безопасной установки значений контролов при загрузке состояния.
        /// </summary>
        /// <param name="value">Исходное значение.</param>
        /// <param name="min">Минимально допустимое значение.</param>
        /// <param name="max">Максимально допустимое значение.</param>
        /// <returns>Значение, ограниченное диапазоном [min, max].</returns>
        private decimal ClampDecimal(decimal value, decimal min, decimal max)
        {
            return Math.Max(min, Math.Min(max, value));
        }

        /// <summary>
        /// Вспомогательный метод для ограничения значения <c>int</c> в заданном диапазоне.
        /// Используется для безопасной установки значений контролов при загрузке состояния.
        /// </summary>
        /// <param name="value">Исходное значение.</param>
        /// <param name="min">Минимально допустимое значение.</param>
        /// <param name="max">Максимально допустимое значение.</param>
        /// <returns>Значение, ограниченное диапазоном [min, max].</returns>
        private int ClampInt(int value, int min, int max)
        {
            return Math.Max(min, Math.Min(max, value));
        }

        #endregion

        #region IFractalForm Implementation

        /// <summary>
        /// Получает значение зума для лупы (если применимо).
        /// Это свойство предназначено для форм, которые могут отображать лупу или мини-карту.
        /// </summary>
        public double LoupeZoom => nudBaseScale != null ? (double)nudBaseScale.Value : 4.0;

        /// <summary>
        /// Событие, которое возникает при изменении значения зума лупы.
        /// </summary>
        public event EventHandler LoupeZoomChanged;

        #endregion

        #region ISaveLoadCapableFractal Implementation

        /// <summary>
        /// Получает строковый идентификатор типа фрактала, используемый для сохранения/загрузки.
        /// Должен быть уникальным для каждого конкретного фрактала.
        /// </summary>
        public abstract string FractalTypeIdentifier { get; }

        /// <summary>
        /// Получает конкретный тип состояния сохранения, который используется для данного фрактала.
        /// </summary>
        public abstract Type ConcreteSaveStateType { get; }

        /// <summary>
        /// Представляет параметры, необходимые для рендеринга превью фрактала.
        /// Используется для быстрой генерации миниатюр состояний сохранения.
        /// </summary>
        public class PreviewParams
        {
            /// <summary>
            /// Получает или устанавливает X-координату центра фрактала для превью.
            /// </summary>
            public decimal CenterX { get; set; }

            /// <summary>
            /// Получает или устанавливает Y-координату центра фрактала для превью.
            /// </summary>
            public decimal CenterY { get; set; }

            /// <summary>
            /// Получает или устанавливает уровень масштабирования для превью.
            /// </summary>
            public decimal Zoom { get; set; }

            /// <summary>
            /// Получает или устанавливает количество итераций для рендеринга превью.
            /// Обычно меньше, чем для полного рендеринга, для ускорения генерации.
            /// </summary>
            public int Iterations { get; set; }

            /// <summary>
            /// Получает или устанавливает имя палитры, используемой для превью.
            /// </summary>
            public string PaletteName { get; set; }

            /// <summary>
            /// Получает или устанавливает пороговое значение для превью.
            /// </summary>
            public decimal Threshold { get; set; }

            /// <summary>
            /// Получает или устанавливает реальную часть константы C (для фракталов Жюлиа).
            /// </summary>
            public decimal CRe { get; set; }

            /// <summary>
            /// Получает или устанавливает мнимую часть константы C (для фракталов Жюлиа).
            /// </summary>
            public decimal CIm { get; set; }

            /// <summary>
            /// Получает или устанавливает тип движка, используемого для рендеринга превью (например, "Mandelbrot", "Julia").
            /// </summary>
            public string PreviewEngineType { get; set; }
        }

        /// <summary>
        /// Обработчик события клика по кнопке "Менеджер состояний".
        /// Открывает диалог для сохранения и загрузки состояний фрактала.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события.</param>
        private void btnStateManager_Click(object sender, EventArgs e)
        {
            // 'this' здесь - это экземпляр конкретной формы фрактала (Mandelbrot, Julia и т.д.),
            // которая реализует интерфейс ISaveLoadCapableFractal.
            using (var dialog = new SaveLoadDialogForm(this))
            {
                dialog.ShowDialog(this);
            }
        }

        /// <summary>
        /// Получает текущее состояние фрактала для сохранения.
        /// Этот метод может быть переопределен в наследниках (например, для фракталов Жюлиа)
        /// для добавления специфичных параметров в сохраняемое состояние.
        /// </summary>
        /// <param name="saveName">Имя, под которым будет сохранено состояние.</param>
        /// <returns>Объект <see cref="FractalSaveStateBase"/>, содержащий текущие параметры фрактала.</returns>
        public virtual FractalSaveStateBase GetCurrentStateForSave(string saveName)
        {
            MandelbrotFamilySaveState state;

            // Определяем тип сохраняемого состояния в зависимости от текущего типа фрактала.
            // Это позволяет корректно сохранять специфичные параметры, такие как константа C для Жюлиа.
            if (this is FractalJulia || this is FractalJuliaBurningShip)
            {
                state = new JuliaFamilySaveState(this.FractalTypeIdentifier);
            }
            else
            {
                state = new MandelbrotFamilySaveState(this.FractalTypeIdentifier);
            }

            state.SaveName = saveName;
            state.Timestamp = DateTime.Now;

            // Заполняем общие параметры фрактала.
            state.CenterX = _centerX;
            state.CenterY = _centerY;
            state.Zoom = _zoom;
            state.Threshold = nudThreshold.Value;
            state.Iterations = (int)nudIterations.Value;
            state.PaletteName = _paletteManager.ActivePalette?.Name ?? "Стандартный серый";
            state.PreviewEngineType = this.FractalTypeIdentifier;

            // Заполняем параметры для генерации превью.
            // Количество итераций для превью обычно уменьшается для ускорения.
            var previewParams = new PreviewParams
            {
                CenterX = _centerX,
                CenterY = _centerY,
                Zoom = _zoom,
                Iterations = Math.Min((int)nudIterations.Value, 75),
                PaletteName = state.PaletteName,
                Threshold = state.Threshold,
                PreviewEngineType = state.PreviewEngineType
            };

            // Если это фрактал Жюлиа, добавляем параметры C.
            // Важно проверять не только тип состояния, но и наличие/видимость UI элементов,
            // так как не все формы могут иметь эти контролы.
            if (state is JuliaFamilySaveState juliaState)
            {
                if (nudRe != null && nudIm != null && nudRe.Visible)
                {
                    juliaState.CRe = nudRe.Value;
                    juliaState.CIm = nudIm.Value;
                    previewParams.CRe = juliaState.CRe;
                    previewParams.CIm = juliaState.CIm;
                }
            }

            var jsonOptions = new JsonSerializerOptions();
            state.PreviewParametersJson = JsonSerializer.Serialize(previewParams, jsonOptions);

            return state;
        }

        /// <summary>
        /// Загружает состояние фрактала из предоставленного объекта состояния.
        /// Обновляет параметры UI и запускает новый рендеринг.
        /// </summary>
        /// <param name="stateBase">Базовый объект состояния фрактала.</param>
        public virtual void LoadState(FractalSaveStateBase stateBase)
        {
            // Убеждаемся, что тип состояния соответствует ожидаемому для этой формы или ее наследников.
            if (!(stateBase is MandelbrotFamilySaveState state))
            {
                MessageBox.Show("Несовместимый тип состояния для загрузки.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Отменяем все текущие операции рендеринга и останавливаем таймер.
            _isRenderingPreview = false;
            _previewRenderCts?.Cancel();
            _renderDebounceTimer.Stop();

            // Применяем загруженные параметры к внутренним полям формы.
            _centerX = state.CenterX;
            _centerY = state.CenterY;
            _zoom = state.Zoom;

            // Обновляем значения UI контролов, ограничивая их допустимыми диапазонами,
            // чтобы избежать ошибок или некорректного отображения.
            nudZoom.Value = ClampDecimal(_zoom, nudZoom.Minimum, nudZoom.Maximum);
            nudThreshold.Value = ClampDecimal(state.Threshold, nudThreshold.Minimum, nudThreshold.Maximum);
            nudIterations.Value = ClampInt(state.Iterations, (int)nudIterations.Minimum, (int)nudIterations.Maximum);

            // Загружаем и применяем сохраненную палитру.
            var paletteToLoad = _paletteManager.Palettes.FirstOrDefault(p => p.Name == state.PaletteName);
            if (paletteToLoad != null)
            {
                _paletteManager.ActivePalette = paletteToLoad;
                ApplyActivePalette();
            }

            // Если это фрактал Жюлиа и состояние содержит параметры C, применяем их к UI.
            if (state is JuliaFamilySaveState juliaState)
            {
                if (nudRe != null && nudIm != null && nudRe.Visible)
                {
                    nudRe.Value = ClampDecimal(juliaState.CRe, nudRe.Minimum, nudRe.Maximum);
                    nudIm.Value = ClampDecimal(juliaState.CIm, nudIm.Minimum, nudIm.Maximum);
                }
            }

            // Очищаем существующие битмапы предпросмотра и рендеринга,
            // чтобы новый рендеринг начался с чистого листа.
            lock (_bitmapLock)
            {
                _previewBitmap?.Dispose();
                _previewBitmap = null;
                _currentRenderingBitmap?.Dispose();
                _currentRenderingBitmap = null;
            }

            // Устанавливаем параметры, по которым будет отрисовано новое превью.
            _renderedCenterX = _centerX;
            _renderedCenterY = _centerY;
            _renderedZoom = _zoom;

            UpdateEngineParameters(); // Важно обновить параметры движка перед рендерингом.
            ScheduleRender(); // Запускаем новый рендеринг фрактала с загруженным состоянием.
        }

        /// <summary>
        /// Асинхронно рендерит плитку превью для заданного состояния фрактала.
        /// Этот метод используется для генерации миниатюр в диалоге сохранения/загрузки.
        /// </summary>
        /// <param name="stateBase">Базовый объект состояния фрактала, содержащий параметры для рендеринга.</param>
        /// <param name="tile">Информация о плитке для рендеринга.</param>
        /// <param name="totalWidth">Общая ширина всего превью.</param>
        /// <param name="totalHeight">Общая высота всего превью.</param>
        /// <param name="tileSize">Размер одной плитки (ширина и высота).</param>
        /// <returns>Массив байтов, представляющий данные пикселей отрендеренной плитки.</returns>
        public virtual async Task<byte[]> RenderPreviewTileAsync(FractalSaveStateBase stateBase, TileInfo tile, int totalWidth, int totalHeight, int tileSize)
        {
            return await Task.Run(() =>
            {
                // Если параметры превью отсутствуют, возвращаем пустую (черную) плитку.
                if (string.IsNullOrEmpty(stateBase.PreviewParametersJson))
                {
                    return new byte[tile.Bounds.Width * tile.Bounds.Height * 4];
                }

                PreviewParams previewParams;
                try
                {
                    // Десериализуем параметры превью из JSON.
                    previewParams = JsonSerializer.Deserialize<PreviewParams>(stateBase.PreviewParametersJson);
                }
                catch
                {
                    // В случае ошибки десериализации, возвращаем пустую плитку.
                    return new byte[tile.Bounds.Width * tile.Bounds.Height * 4];
                }

                // Создаем и настраиваем движок фрактала, специфичный для типа, указанного в параметрах превью.
                FractalMandelbrotFamilyEngine previewEngine = null;
                switch (previewParams.PreviewEngineType)
                {
                    case "Mandelbrot": previewEngine = new MandelbrotEngine(); break;
                    case "Julia":
                        previewEngine = new JuliaEngine();
                        ((JuliaEngine)previewEngine).C = new ComplexDecimal(previewParams.CRe, previewParams.CIm);
                        break;
                    case "MandelbrotBurningShip": previewEngine = new MandelbrotBurningShipEngine(); break;
                    case "JuliaBurningShip":
                        previewEngine = new JuliaBurningShipEngine();
                        ((JuliaBurningShipEngine)previewEngine).C = new ComplexDecimal(previewParams.CRe, previewParams.CIm);
                        break;
                    default: return new byte[tile.Bounds.Width * tile.Bounds.Height * 4]; // Неизвестный тип движка.
                }

                // Устанавливаем параметры для движка превью.
                // Количество итераций для превью может быть увеличено, чтобы обеспечить достаточную детализацию,
                // но при этом оставаясь разумным для быстрой генерации.
                previewEngine.MaxIterations = 400;
                previewEngine.CenterX = previewParams.CenterX;
                previewEngine.CenterY = previewParams.CenterY;
                decimal previewBaseScale = this.BaseScale;
                // Защита от деления на ноль, если зум равен нулю.
                if (previewParams.Zoom == 0) previewParams.Zoom = 0.001m;
                previewEngine.Scale = previewBaseScale / previewParams.Zoom;
                previewEngine.ThresholdSquared = previewParams.Threshold * previewParams.Threshold;

                // Находим палитру по имени; если не найдена, используем первую доступную.
                var paletteForPreview = _paletteManager.Palettes.FirstOrDefault(p => p.Name == previewParams.PaletteName) ?? _paletteManager.Palettes.First();
                previewEngine.Palette = GeneratePaletteFunction(paletteForPreview);

                // Устанавливаем MaxColorIterations в зависимости от типа палитры.
                if (paletteForPreview.Name == "Стандартный серый" || paletteForPreview.IsGradient)
                {
                    previewEngine.MaxColorIterations = Math.Max(1, previewEngine.MaxIterations);
                }
                else
                {
                    previewEngine.MaxColorIterations = Math.Max(1, paletteForPreview.Colors.Count);
                }

                // Вызываем метод рендеринга ОДНОЙ плитки из движка фрактала.
                return previewEngine.RenderSingleTile(tile, totalWidth, totalHeight, out _);
            });
        }

        /// <summary>
        /// Рендерит полное изображение превью для заданного состояния фрактала.
        /// Этот метод может быть использован для генерации целых миниатюр,
        /// когда не требуется пошаговый рендеринг плиток.
        /// </summary>
        /// <param name="stateBase">Объект состояния фрактала, содержащий параметры для рендеринга.</param>
        /// <param name="previewWidth">Желаемая ширина превью.</param>
        /// <param name="previewHeight">Желаемая высота превью.</param>
        /// <returns>Объект <see cref="Bitmap"/> с отрендеренным изображением превью.</returns>
        public virtual Bitmap RenderPreview(FractalSaveStateBase stateBase, int previewWidth, int previewHeight)
        {
            // Возвращаем изображение ошибки, если отсутствуют параметры превью.
            if (string.IsNullOrEmpty(stateBase.PreviewParametersJson))
            {
                var bmpError = new Bitmap(previewWidth, previewHeight);
                using (var g = Graphics.FromImage(bmpError))
                {
                    g.Clear(Color.DarkGray);
                    TextRenderer.DrawText(g, "Нет данных", Font, new Rectangle(0, 0, previewWidth, previewHeight), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
                return bmpError;
            }

            PreviewParams previewParams;
            try
            {
                var jsonOptions = new JsonSerializerOptions();
                previewParams = JsonSerializer.Deserialize<PreviewParams>(stateBase.PreviewParametersJson, jsonOptions);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка десериализации PreviewParametersJson: {ex.Message}");
                var bmpError = new Bitmap(previewWidth, previewHeight);
                using (var g = Graphics.FromImage(bmpError))
                {
                    g.Clear(Color.DarkRed);
                    TextRenderer.DrawText(g, "Ошибка параметров", Font, new Rectangle(0, 0, previewWidth, previewHeight), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
                return bmpError;
            }

            // Создаем и настраиваем движок фрактала, специфичный для типа, указанного в параметрах превью.
            FractalMandelbrotFamilyEngine previewEngine = null;

            switch (previewParams.PreviewEngineType)
            {
                case "Mandelbrot":
                    previewEngine = new MandelbrotEngine();
                    break;
                case "Julia":
                    previewEngine = new JuliaEngine();
                    ((JuliaEngine)previewEngine).C = new ComplexDecimal(previewParams.CRe, previewParams.CIm);
                    break;
                case "MandelbrotBurningShip":
                    previewEngine = new MandelbrotBurningShipEngine();
                    break;
                case "JuliaBurningShip":
                    previewEngine = new JuliaBurningShipEngine();
                    ((JuliaBurningShipEngine)previewEngine).C = new ComplexDecimal(previewParams.CRe, previewParams.CIm);
                    break;
                default:
                    // Если тип движка неизвестен, возвращаем изображение с сообщением об ошибке.
                    var bmpError = new Bitmap(previewWidth, previewHeight);
                    using (var g = Graphics.FromImage(bmpError))
                    {
                        g.Clear(Color.DarkOrange);
                        TextRenderer.DrawText(g, "Неизв. тип движка", Font, new Rectangle(0, 0, previewWidth, previewHeight), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                    }
                    return bmpError;
            }

            // Устанавливаем параметры для движка превью.
            previewEngine.CenterX = previewParams.CenterX;
            previewEngine.CenterY = previewParams.CenterY;
            decimal previewBaseScale = this.BaseScale;
            // Защита от деления на ноль, если зум равен нулю.
            if (previewParams.Zoom == 0) previewParams.Zoom = 0.001m;
            previewEngine.Scale = previewBaseScale / previewParams.Zoom;
            previewEngine.MaxIterations = previewParams.Iterations;
            previewEngine.ThresholdSquared = previewParams.Threshold * previewParams.Threshold;

            // Находим палитру по имени; если не найдена, используем первую доступную.
            var paletteForPreview = _paletteManager.Palettes.FirstOrDefault(p => p.Name == previewParams.PaletteName)
                                  ?? _paletteManager.Palettes.First();

            previewEngine.Palette = GeneratePaletteFunction(paletteForPreview);

            // Устанавливаем MaxColorIterations в зависимости от типа палитры.
            if (paletteForPreview.Name == "Стандартный серый" || paletteForPreview.IsGradient)
            {
                previewEngine.MaxColorIterations = Math.Max(1, previewParams.Iterations);
            }
            else
            {
                previewEngine.MaxColorIterations = Math.Max(1, paletteForPreview.Colors.Count);
            }

            // Рендерим изображение в битмап, используя один поток для генерации превью.
            return previewEngine.RenderToBitmap(previewWidth, previewHeight, 1, progress => { });
        }

        /// <summary>
        /// Загружает все сохраненные состояния, относящиеся к данному типу фрактала.
        /// Этот метод должен быть переопределен в конкретных классах фракталов,
        /// чтобы корректно загружать состояния соответствующего типа.
        /// </summary>
        /// <returns>Список базовых объектов состояний фрактала.</returns>
        /// <exception cref="NotImplementedException">Вызывается, если метод не был переопределен в дочернем классе.</exception>
        public virtual List<FractalSaveStateBase> LoadAllSavesForThisType()
        {
            throw new NotImplementedException($"Метод LoadAllSavesForThisType должен быть переопределен в классе {this.GetType().Name}, чтобы загружать состояния типа {this.ConcreteSaveStateType.Name}.");
        }

        /// <summary>
        /// Сохраняет список состояний для данного типа фрактала.
        /// Этот метод должен быть переопределен в конкретных классах фракталов,
        /// чтобы корректно сохранять состояния соответствующего типа.
        /// </summary>
        /// <param name="saves">Список базовых объектов состояний фрактала для сохранения.</param>
        /// <exception cref="NotImplementedException">Вызывается, если метод не был переопределен в дочернем классе.</exception>
        public virtual void SaveAllSavesForThisType(List<FractalSaveStateBase> saves)
        {
            throw new NotImplementedException($"Метод SaveAllSavesForThisType должен быть переопределен в классе {this.GetType().Name}, чтобы сохранять состояния типа {this.ConcreteSaveStateType.Name}.");
        }

        #endregion
    }
}