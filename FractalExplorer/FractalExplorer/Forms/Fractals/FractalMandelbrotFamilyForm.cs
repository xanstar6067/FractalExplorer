using FractalExplorer.Engines;
using FractalExplorer.Forms;
using FractalExplorer.Projects;
using FractalExplorer.Resources;
using FractalExplorer.Utilities;
using FractalExplorer.Utilities.SaveIO;
using FractalExplorer.Utilities.SaveIO.ColorPalettes;
using FractalExplorer.Utilities.SaveIO.SaveStateImplementations;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text.Json;

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
        private const int TILE_SIZE = 16;

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

        private string _baseTitle;

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

            nudZoom.DecimalPlaces = 15;
            nudZoom.Increment = 0.1m;
            nudZoom.Minimum = 0.000000000000001m;
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
            _baseTitle = this.Text;
            _paletteManager = new ColorPaletteMandelbrotFamily();
            _fractalEngine = CreateEngine();
            _renderDebounceTimer = new System.Windows.Forms.Timer { Interval = 300 };
            _renderDebounceTimer.Tick += RenderDebounceTimer_Tick;
            _renderVisualizer = new RenderVisualizerComponent(TILE_SIZE);
            _renderVisualizer.NeedsRedraw += OnVisualizerNeedsRedraw;

            InitializeControls();
            InitializeEventHandlers();

            // Инициализация ComboBox для выбора качества SSAA.
            // Ищем контрол по имени, чтобы код был независим от его точного расположения на форме.
            var cbSSAA = this.Controls.Find("cbSSAA", true).FirstOrDefault() as ComboBox;
            if (cbSSAA != null)
            {
                // Добавляем понятные для пользователя пункты.
                cbSSAA.Items.Add("Выкл (1x)");
                cbSSAA.Items.Add("Низкое (2x)");
                cbSSAA.Items.Add("Высокое (4x)");

                // Устанавливаем значение по умолчанию.
                cbSSAA.SelectedItem = "Выкл (1x)";

                // Подписываемся на событие изменения выбора, чтобы запустить новый рендеринг.
                cbSSAA.SelectedIndexChanged += (s, ev) => ScheduleRender();
            }

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
            // ИСПРАВЛЕНИЕ: Перед применением логики палитры, принудительно обновляем
            // значение итераций в движке из элемента управления на форме.
            // Это гарантирует, что режим "AlignWithRenderIterations" получит актуальное значение.
            _fractalEngine.MaxIterations = (int)nudIterations.Value;

            // Теперь применяем палитру, которая будет использовать уже обновленное значение MaxIterations.
            ApplyActivePalette();

            // Запускаем рендеринг с новой палитрой.
            ScheduleRender();
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

        // ... (Код обработчиков мыши Canvas_MouseWheel, Canvas_MouseDown, Canvas_MouseMove, Canvas_MouseUp остается без изменений) ...
        #region Mouse Handlers (No Changes)
        private void Canvas_MouseWheel(object sender, MouseEventArgs e)
        {
            if (_isHighResRendering) return;
            CommitAndBakePreview();
            decimal zoomFactor = e.Delta > 0 ? 1.5m : 1.0m / 1.5m;
            decimal scaleBeforeZoom = BaseScale / _zoom;
            decimal mouseReal = _centerX + (e.X - canvas.Width / 2.0m) * scaleBeforeZoom / canvas.Width;
            decimal mouseImaginary = _centerY - (e.Y - canvas.Height / 2.0m) * scaleBeforeZoom / canvas.Height;
            _zoom = Math.Max(nudZoom.Minimum, Math.Min(nudZoom.Maximum, _zoom * zoomFactor));
            decimal scaleAfterZoom = BaseScale / _zoom;
            _centerX = mouseReal - (e.X - canvas.Width / 2.0m) * scaleAfterZoom / canvas.Width;
            _centerY = mouseImaginary + (e.Y - canvas.Height / 2.0m) * scaleAfterZoom / canvas.Height;
            canvas.Invalidate();
            if (nudZoom.Value != _zoom)
            {
                nudZoom.Value = _zoom;
            }
            else
            {
                ScheduleRender();
            }
        }
        private void Canvas_MouseDown(object sender, MouseEventArgs e)
        {
            if (_isHighResRendering) return;
            if (e.Button == MouseButtons.Left)
            {
                _panning = true;
                _panStart = e.Location;
                canvas.Cursor = Cursors.Hand;
            }
        }
        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isHighResRendering || !_panning) return;
            CommitAndBakePreview();
            decimal unitsPerPixel = BaseScale / _zoom / canvas.Width;
            _centerX -= (e.X - _panStart.X) * unitsPerPixel;
            _centerY += (e.Y - _panStart.Y) * unitsPerPixel;
            _panStart = e.Location;
            canvas.Invalidate();
            ScheduleRender();
        }
        private void Canvas_MouseUp(object sender, MouseEventArgs e)
        {
            if (_isHighResRendering) return;
            if (e.Button == MouseButtons.Left)
            {
                _panning = false;
                canvas.Cursor = Cursors.Default;
            }
        }
        #endregion

        // ... (Код отрисовки Canvas_Paint и сохранения btnSave_Click остается без изменений) ...
        #region Painting and Saving (No Changes)
        private void Canvas_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.Clear(Color.Black);
            lock (_bitmapLock)
            {
                if (_previewBitmap != null && canvas.Width > 0 && canvas.Height > 0)
                {
                    if (_renderedCenterX == _centerX && _renderedCenterY == _centerY && _renderedZoom == _zoom)
                    {
                        e.Graphics.DrawImageUnscaled(_previewBitmap, Point.Empty);
                    }
                    else
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
                                e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                                PointF destPoint1 = new PointF((float)offsetXPixels, (float)offsetYPixels);
                                PointF destPoint2 = new PointF((float)(offsetXPixels + newWidthPixels), (float)offsetYPixels);
                                PointF destPoint3 = new PointF((float)offsetXPixels, (float)(offsetYPixels + newHeightPixels));
                                e.Graphics.DrawImage(_previewBitmap, new PointF[] { destPoint1, destPoint2, destPoint3 });
                            }
                        }
                        catch (ArgumentException)
                        {
                            if (_previewBitmap != null)
                            {
                                e.Graphics.DrawImageUnscaled(_previewBitmap, Point.Empty);
                            }
                        }
                    }
                }
                if (_currentRenderingBitmap != null)
                {
                    e.Graphics.DrawImageUnscaled(_currentRenderingBitmap, Point.Empty);
                }
            }
            if (_renderVisualizer != null && _isRenderingPreview)
            {
                _renderVisualizer.DrawVisualization(e.Graphics);
            }
        }
        private async void btnSave_Click(object sender, EventArgs e)
        {
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
                    if (_isRenderingPreview)
                    {
                        _previewRenderCts?.Cancel();
                    }
                    _isHighResRendering = true;
                    pnlControls.Enabled = false;
                    pbHighResProgress.Value = 0;
                    pbHighResProgress.Visible = true;
                    try
                    {
                        FractalMandelbrotFamilyEngine renderEngine = CreateEngine();
                        UpdateEngineParameters();
                        renderEngine.MaxIterations = _fractalEngine.MaxIterations;
                        renderEngine.ThresholdSquared = _fractalEngine.ThresholdSquared;
                        renderEngine.CenterX = _fractalEngine.CenterX;
                        renderEngine.CenterY = _fractalEngine.CenterY;
                        renderEngine.Scale = _fractalEngine.Scale;
                        renderEngine.C = this is FractalJulia || this is FractalJuliaBurningShip ? new ComplexDecimal(nudRe.Value, nudIm.Value) : _fractalEngine.C;
                        renderEngine.Palette = GeneratePaletteFunction(_paletteManager.ActivePalette);
                        renderEngine.MaxColorIterations = _fractalEngine.MaxColorIterations;
                        int threadCount = GetThreadCount();
                        int ssaaFactor = GetSelectedSsaaFactor();
                        var stopwatch = Stopwatch.StartNew();
                        Bitmap highResBitmap = await Task.Run(() => renderEngine.RenderToBitmapSSAA(
                            saveWidth, saveHeight, threadCount,
                            progress =>
                            {
                                if (pbHighResProgress.IsHandleCreated && !pbHighResProgress.IsDisposed)
                                {
                                    pbHighResProgress.Invoke((Action)(() =>
                                    {
                                        pbHighResProgress.Value = Math.Min(pbHighResProgress.Maximum, progress);
                                    }));
                                }
                            }, ssaaFactor));
                        stopwatch.Stop();
                        highResBitmap.Save(saveDialog.FileName, ImageFormat.Png);
                        highResBitmap.Dispose();
                        double elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
                        MessageBox.Show($"Изображение успешно сохранено!\nВремя рендеринга: {elapsedSeconds:F3} сек.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        _isHighResRendering = false;
                        pnlControls.Enabled = true;
                        if (pbHighResProgress.IsHandleCreated && !pbHighResProgress.IsDisposed)
                        {
                            pbHighResProgress.Invoke((Action)(() =>
                            {
                                pbHighResProgress.Visible = false;
                                pbHighResProgress.Value = 0;
                            }));
                        }
                        ScheduleRender();
                    }
                }
            }
        }
        #endregion

        /// <summary>
        /// Обработчик события тика таймера задержки рендеринга.
        /// Запускает рендеринг предпросмотра, выбирая метод (с SSAA или без)
        /// на основе выбора пользователя в ComboBox.
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

            // Получаем выбранный фактор SSAA
            int ssaaFactor = GetSelectedSsaaFactor();
            // Обновляем заголовок для отладки, чтобы видеть, какой режим активен
            this.Text = $"{_baseTitle} - Качество: {ssaaFactor}x";

            if (ssaaFactor > 1)
            {
                // Если выбран SSAA, вызываем соответствующий метод рендеринга
                await StartPreviewRenderSSAA(ssaaFactor);
            }
            else
            {
                // Иначе вызываем стандартный рендеринг
                await StartPreviewRender();
            }
        }

        #endregion

        #region Rendering Logic

        // ... (Код рендеринга StartPreviewRenderSSAA, StartPreviewRender, GenerateTiles, ScheduleRender, CommitAndBakePreview остается без изменений) ...
        #region Rendering Methods (No Changes)
        private async Task StartPreviewRenderSSAA(int ssaaFactor)
        {
            if (canvas.Width <= 0 || canvas.Height <= 0) return;
            var stopwatch = Stopwatch.StartNew();
            _isRenderingPreview = true;
            _previewRenderCts?.Cancel();
            _previewRenderCts = new CancellationTokenSource();
            var token = _previewRenderCts.Token;
            _renderVisualizer?.NotifyRenderSessionStart();
            var newRenderingBitmap = new Bitmap(canvas.Width, canvas.Height, PixelFormat.Format32bppArgb);
            lock (_bitmapLock)
            {
                _currentRenderingBitmap?.Dispose();
                _currentRenderingBitmap = newRenderingBitmap;
            }
            UpdateEngineParameters();
            var currentRenderedCenterX = _centerX;
            var currentRenderedCenterY = _centerY;
            var currentRenderedZoom = _zoom;
            var renderEngineCopy = CreateEngine();
            renderEngineCopy.MaxIterations = _fractalEngine.MaxIterations;
            renderEngineCopy.ThresholdSquared = _fractalEngine.ThresholdSquared;
            renderEngineCopy.CenterX = _fractalEngine.CenterX;
            renderEngineCopy.CenterY = _fractalEngine.CenterY;
            renderEngineCopy.Scale = _fractalEngine.Scale;
            renderEngineCopy.C = _fractalEngine.C;
            renderEngineCopy.Palette = _fractalEngine.Palette;
            renderEngineCopy.MaxColorIterations = _fractalEngine.MaxColorIterations;
            var tiles = GenerateTiles(canvas.Width, canvas.Height);
            var dispatcher = new TileRenderDispatcher(tiles, GetThreadCount());
            if (pbRenderProgress.IsHandleCreated && !pbRenderProgress.IsDisposed)
            {
                pbRenderProgress.Invoke((Action)(() => { pbRenderProgress.Value = 0; pbRenderProgress.Maximum = tiles.Count; }));
            }
            int progress = 0;
            try
            {
                await dispatcher.RenderAsync(async (tile, ct) =>
                {
                    ct.ThrowIfCancellationRequested();
                    _renderVisualizer?.NotifyTileRenderStart(tile.Bounds);
                    var tileBuffer = renderEngineCopy.RenderSingleTileSSAA(tile, canvas.Width, canvas.Height, ssaaFactor, out int bytesPerPixel);
                    ct.ThrowIfCancellationRequested();
                    lock (_bitmapLock)
                    {
                        if (ct.IsCancellationRequested || _currentRenderingBitmap != newRenderingBitmap) return;
                        var tileRect = tile.Bounds;
                        var bitmapRect = new Rectangle(0, 0, _currentRenderingBitmap.Width, _currentRenderingBitmap.Height);
                        tileRect.Intersect(bitmapRect);
                        if (tileRect.Width == 0 || tileRect.Height == 0) return;
                        BitmapData bmpData = _currentRenderingBitmap.LockBits(tileRect, ImageLockMode.WriteOnly, _currentRenderingBitmap.PixelFormat);
                        for (int y = 0; y < tileRect.Height; y++)
                        {
                            IntPtr destPtr = IntPtr.Add(bmpData.Scan0, y * bmpData.Stride);
                            int srcOffset = y * tileRect.Width * bytesPerPixel;
                            Marshal.Copy(tileBuffer, srcOffset, destPtr, tileRect.Width * bytesPerPixel);
                        }
                        _currentRenderingBitmap.UnlockBits(bmpData);
                    }
                    _renderVisualizer?.NotifyTileRenderComplete(tile.Bounds);
                    if (ct.IsCancellationRequested || !canvas.IsHandleCreated || canvas.IsDisposed) return;
                    canvas.Invoke((Action)(() =>
                    {
                        if (!ct.IsCancellationRequested && pbRenderProgress.IsHandleCreated && !pbRenderProgress.IsDisposed)
                        {
                            pbRenderProgress.Value = Math.Min(pbRenderProgress.Maximum, Interlocked.Increment(ref progress));
                        }
                    }));
                    await Task.Yield();
                }, token);
                token.ThrowIfCancellationRequested();
                stopwatch.Stop();
                this.Text = $"{_baseTitle} - Время рендера (SSAA {ssaaFactor}x): {stopwatch.Elapsed.TotalSeconds:F3} сек.";
                lock (_bitmapLock)
                {
                    if (_currentRenderingBitmap == newRenderingBitmap)
                    {
                        _previewBitmap?.Dispose();
                        _previewBitmap = _currentRenderingBitmap;
                        _currentRenderingBitmap = null;
                        _renderedCenterX = currentRenderedCenterX;
                        _renderedCenterY = currentRenderedCenterY;
                        _renderedZoom = currentRenderedZoom;
                    }
                    else
                    {
                        newRenderingBitmap?.Dispose();
                    }
                }
                if (canvas.IsHandleCreated && !canvas.IsDisposed)
                {
                    canvas.Invalidate();
                }
            }
            catch (OperationCanceledException)
            {
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
                newRenderingBitmap?.Dispose();
                if (IsHandleCreated && !IsDisposed)
                {
                    MessageBox.Show($"Ошибка рендеринга SSAA: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            finally
            {
                _isRenderingPreview = false;
                _renderVisualizer?.NotifyRenderSessionComplete();
                if (pbRenderProgress.IsHandleCreated && !pbRenderProgress.IsDisposed)
                {
                    pbRenderProgress.Invoke((Action)(() => pbRenderProgress.Value = 0));
                }
            }
        }
        private async Task StartPreviewRender()
        {
            if (canvas.Width <= 0 || canvas.Height <= 0) return;
            var stopwatch = Stopwatch.StartNew();
            _isRenderingPreview = true;
            _previewRenderCts?.Cancel();
            _previewRenderCts = new CancellationTokenSource();
            var token = _previewRenderCts.Token;
            _renderVisualizer?.NotifyRenderSessionStart();
            var newRenderingBitmap = new Bitmap(canvas.Width, canvas.Height, PixelFormat.Format32bppArgb);
            lock (_bitmapLock)
            {
                _currentRenderingBitmap?.Dispose();
                _currentRenderingBitmap = newRenderingBitmap;
            }
            UpdateEngineParameters();
            var currentRenderedCenterX = _centerX;
            var currentRenderedCenterY = _centerY;
            var currentRenderedZoom = _zoom;
            var renderEngineCopy = CreateEngine();
            renderEngineCopy.MaxIterations = _fractalEngine.MaxIterations;
            renderEngineCopy.ThresholdSquared = _fractalEngine.ThresholdSquared;
            renderEngineCopy.CenterX = _fractalEngine.CenterX;
            renderEngineCopy.CenterY = _fractalEngine.CenterY;
            renderEngineCopy.Scale = _fractalEngine.Scale;
            renderEngineCopy.C = _fractalEngine.C;
            renderEngineCopy.Palette = _fractalEngine.Palette;
            renderEngineCopy.MaxColorIterations = _fractalEngine.MaxColorIterations;
            var tiles = GenerateTiles(canvas.Width, canvas.Height);
            var dispatcher = new TileRenderDispatcher(tiles, GetThreadCount());
            if (pbRenderProgress.IsHandleCreated && !pbRenderProgress.IsDisposed)
            {
                pbRenderProgress.Invoke((Action)(() => { pbRenderProgress.Value = 0; pbRenderProgress.Maximum = tiles.Count; }));
            }
            int progress = 0;
            try
            {
                await dispatcher.RenderAsync(async (tile, ct) =>
                {
                    ct.ThrowIfCancellationRequested();
                    _renderVisualizer?.NotifyTileRenderStart(tile.Bounds);
                    var tileBuffer = renderEngineCopy.RenderSingleTile(tile, canvas.Width, canvas.Height, out int bytesPerPixel);
                    ct.ThrowIfCancellationRequested();
                    lock (_bitmapLock)
                    {
                        if (ct.IsCancellationRequested || _currentRenderingBitmap != newRenderingBitmap) return;
                        var tileRect = tile.Bounds;
                        var bitmapRect = new Rectangle(0, 0, _currentRenderingBitmap.Width, _currentRenderingBitmap.Height);
                        tileRect.Intersect(bitmapRect);
                        if (tileRect.Width == 0 || tileRect.Height == 0) return;
                        BitmapData bmpData = _currentRenderingBitmap.LockBits(tileRect, ImageLockMode.WriteOnly, _currentRenderingBitmap.PixelFormat);
                        int originalTileWidthInBytes = tile.Bounds.Width * bytesPerPixel;
                        for (int y = 0; y < tileRect.Height; y++)
                        {
                            IntPtr destPtr = IntPtr.Add(bmpData.Scan0, y * bmpData.Stride);
                            int srcOffset = ((y + tileRect.Y) - tile.Bounds.Y) * originalTileWidthInBytes + ((tileRect.X - tile.Bounds.X) * bytesPerPixel);
                            Marshal.Copy(tileBuffer, srcOffset, destPtr, tileRect.Width * bytesPerPixel);
                        }
                        _currentRenderingBitmap.UnlockBits(bmpData);
                    }
                    _renderVisualizer?.NotifyTileRenderComplete(tile.Bounds);
                    if (ct.IsCancellationRequested || !canvas.IsHandleCreated || canvas.IsDisposed) return;
                    canvas.Invoke((Action)(() =>
                    {
                        if (!ct.IsCancellationRequested && pbRenderProgress.IsHandleCreated && !pbRenderProgress.IsDisposed)
                        {
                            pbRenderProgress.Value = Math.Min(pbRenderProgress.Maximum, Interlocked.Increment(ref progress));
                        }
                    }));
                    await Task.Yield();
                }, token);
                token.ThrowIfCancellationRequested();
                stopwatch.Stop();
                this.Text = $"{_baseTitle} - Время последнего рендера: {stopwatch.Elapsed.TotalSeconds:F3} сек.";
                lock (_bitmapLock)
                {
                    if (_currentRenderingBitmap == newRenderingBitmap)
                    {
                        _previewBitmap?.Dispose();
                        _previewBitmap = _currentRenderingBitmap;
                        _currentRenderingBitmap = null;
                        _renderedCenterX = currentRenderedCenterX;
                        _renderedCenterY = currentRenderedCenterY;
                        _renderedZoom = currentRenderedZoom;
                    }
                    else
                    {
                        newRenderingBitmap?.Dispose();
                    }
                }
                if (canvas.IsHandleCreated && !canvas.IsDisposed)
                {
                    canvas.Invalidate();
                }
            }
            catch (OperationCanceledException)
            {
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
                newRenderingBitmap?.Dispose();
                if (IsHandleCreated && !IsDisposed)
                {
                    MessageBox.Show($"Ошибка рендеринга: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            finally
            {
                _isRenderingPreview = false;
                _renderVisualizer?.NotifyRenderSessionComplete();
                if (pbRenderProgress.IsHandleCreated && !pbRenderProgress.IsDisposed)
                {
                    pbRenderProgress.Invoke((Action)(() => pbRenderProgress.Value = 0));
                }
            }
        }
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
            return tiles.OrderBy(t => Math.Pow(t.Center.X - center.X, 2) + Math.Pow(t.Center.Y - center.Y, 2)).ToList();
        }
        private void ScheduleRender()
        {
            if (_isHighResRendering || WindowState == FormWindowState.Minimized) return;
            if (_isRenderingPreview)
            {
                _previewRenderCts?.Cancel();
            }
            _renderDebounceTimer.Stop();
            _renderDebounceTimer.Start();
        }
        private void CommitAndBakePreview()
        {
            lock (_bitmapLock)
            {
                if (!_isRenderingPreview || _currentRenderingBitmap == null) return;
            }
            _previewRenderCts?.Cancel();
            lock (_bitmapLock)
            {
                if (_currentRenderingBitmap == null) return;
                var bakedBitmap = new Bitmap(canvas.Width, canvas.Height, PixelFormat.Format24bppRgb);
                using (var g = Graphics.FromImage(bakedBitmap))
                {
                    g.Clear(Color.Black);
                    g.InterpolationMode = InterpolationMode.Bilinear;
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
                        catch (Exception) { }
                    }
                    if (_currentRenderingBitmap != null)
                    {
                        g.DrawImageUnscaled(_currentRenderingBitmap, Point.Empty);
                    }
                }
                _previewBitmap?.Dispose();
                _previewBitmap = bakedBitmap;
                _currentRenderingBitmap.Dispose();
                _currentRenderingBitmap = null;
                _renderedCenterX = _centerX;
                _renderedCenterY = _centerY;
                _renderedZoom = _zoom;
            }
        }
        #endregion

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
            UpdateEngineSpecificParameters();

            // Убеждаемся, что палитра и ее параметры (MaxColorIterations, Gamma) применены к движку.
            ApplyActivePalette();
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
            // Получаем параметры из объекта палитры
            double gamma = palette.Gamma;
            var colors = new List<Color>(palette.Colors);
            bool isGradient = palette.IsGradient;
            int colorCount = colors.Count;

            // Специальная обработка для встроенной серой палитры для сохранения логарифмического сглаживания
            if (palette.Name == "Стандартный серый")
            {
                return (iter, maxIter, maxColorIter) =>
                {
                    if (iter == maxIter) return Color.Black;

                    // Используем maxColorIter для нормализации
                    double tLog = Math.Log(Math.Min(iter, maxColorIter) + 1) / Math.Log(maxColorIter + 1);
                    int cVal = (int)(255.0 * (1 - tLog));

                    Color baseColor = Color.FromArgb(cVal, cVal, cVal);
                    // Применяем гамму
                    return ColorCorrection.ApplyGamma(baseColor, gamma);
                };
            }

            // Обработка крайних случаев
            if (colorCount == 0) return (i, m, mc) => Color.Black;
            if (colorCount == 1)
            {
                return (iter, max, clrMax) =>
                {
                    Color baseColor = (iter == max) ? Color.Black : colors[0];
                    return ColorCorrection.ApplyGamma(baseColor, gamma);
                };
            }

            // Основная логика генерации функции
            return (iter, maxIter, maxColorIter) =>
            {
                if (iter == maxIter) return Color.Black; // Точки внутри множества всегда черные

                Color baseColor;
                if (isGradient)
                {
                    // Для градиента используем линейную интерполяцию
                    int colorDomainIter = Math.Min(iter, maxColorIter);
                    double t = maxColorIter > 1 ? (double)colorDomainIter / (maxColorIter - 1) : 0;
                    double scaledT = t * (colorCount - 1);
                    int index1 = (int)Math.Floor(scaledT);
                    int index2 = Math.Min(index1 + 1, colorCount - 1);
                    double localT = scaledT - index1;
                    baseColor = LerpColor(colors[index1], colors[index2], localT);
                }
                else
                {
                    // ИСПРАВЛЕНИЕ: Новая логика для дискретных (не-градиентных) цветов
                    // Теперь ширина цветовых полос зависит от MaxColorIterations
                    int iterInCycle = iter % maxColorIter;
                    double t = (double)iterInCycle / maxColorIter;
                    int index = (int)(t * colorCount);
                    index = Math.Min(index, colorCount - 1); // Гарантируем, что индекс в пределах массива

                    baseColor = colors[index];
                }

                // Применяем гамма-коррекцию в самом конце
                return ColorCorrection.ApplyGamma(baseColor, gamma);
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
            t = Math.Max(0, Math.Min(1, t));
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
            if (_fractalEngine == null || _paletteManager.ActivePalette == null)
            {
                return;
            }

            var activePalette = _paletteManager.ActivePalette;

            // Логика выбора количества итераций для цвета.
            if (activePalette.AlignWithRenderIterations)
            {
                _fractalEngine.MaxColorIterations = _fractalEngine.MaxIterations;
            }
            else
            {
                _fractalEngine.MaxColorIterations = activePalette.MaxColorIterations;
            }

            _fractalEngine.Palette = GeneratePaletteFunction(activePalette);
        }

        #endregion

        #region Unchanged Code Sections (Helpers, Interfaces)
        private int GetSelectedSsaaFactor()
        {
            var cbSSAA = this.Controls.Find("cbSSAA", true).FirstOrDefault() as ComboBox;
            if (cbSSAA == null) return 1;
            if (cbSSAA.InvokeRequired)
            {
                return (int)cbSSAA.Invoke(new Func<int>(() =>
                {
                    if (cbSSAA.SelectedItem == null) return 1;
                    switch (cbSSAA.SelectedItem.ToString())
                    {
                        case "Низкое (2x)": return 2;
                        case "Высокое (4x)": return 4;
                        default: return 1;
                    }
                }));
            }
            else
            {
                if (cbSSAA.SelectedItem == null) return 1;
                switch (cbSSAA.SelectedItem.ToString())
                {
                    case "Низкое (2x)": return 2;
                    case "Высокое (4x)": return 4;
                    default: return 1;
                }
            }
        }
        private int GetThreadCount()
        {
            return cbThreads.SelectedItem?.ToString() == "Auto" ? Environment.ProcessorCount : Convert.ToInt32(cbThreads.SelectedItem);
        }
        private decimal ClampDecimal(decimal value, decimal min, decimal max)
        {
            return Math.Max(min, Math.Min(max, value));
        }
        private int ClampInt(int value, int min, int max)
        {
            return Math.Max(min, Math.Min(max, value));
        }
        public double LoupeZoom => nudBaseScale != null ? (double)nudBaseScale.Value : 4.0;
        public event EventHandler LoupeZoomChanged;
        public abstract string FractalTypeIdentifier { get; }
        public abstract Type ConcreteSaveStateType { get; }
        public class PreviewParams
        {
            public decimal CenterX { get; set; }
            public decimal CenterY { get; set; }
            public decimal Zoom { get; set; }
            public int Iterations { get; set; }
            public string PaletteName { get; set; }
            public decimal Threshold { get; set; }
            public decimal CRe { get; set; }
            public decimal CIm { get; set; }
            public string PreviewEngineType { get; set; }
        }
        private void btnStateManager_Click(object sender, EventArgs e)
        {
            using (var dialog = new SaveLoadDialogForm(this))
            {
                dialog.ShowDialog(this);
            }
        }
        public virtual FractalSaveStateBase GetCurrentStateForSave(string saveName)
        {
            MandelbrotFamilySaveState state;
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
            state.CenterX = _centerX;
            state.CenterY = _centerY;
            state.Zoom = _zoom;
            state.Threshold = nudThreshold.Value;
            state.Iterations = (int)nudIterations.Value;
            state.PaletteName = _paletteManager.ActivePalette?.Name ?? "Стандартный серый";
            state.PreviewEngineType = this.FractalTypeIdentifier;
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
        public virtual void LoadState(FractalSaveStateBase stateBase)
        {
            if (!(stateBase is MandelbrotFamilySaveState state))
            {
                MessageBox.Show("Несовместимый тип состояния для загрузки.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            _isRenderingPreview = false;
            _previewRenderCts?.Cancel();
            _renderDebounceTimer.Stop();
            _centerX = state.CenterX;
            _centerY = state.CenterY;
            _zoom = state.Zoom;
            nudZoom.Value = ClampDecimal(_zoom, nudZoom.Minimum, nudZoom.Maximum);
            nudThreshold.Value = ClampDecimal(state.Threshold, nudThreshold.Minimum, nudThreshold.Maximum);
            nudIterations.Value = ClampInt(state.Iterations, (int)nudIterations.Minimum, (int)nudIterations.Maximum);
            var paletteToLoad = _paletteManager.Palettes.FirstOrDefault(p => p.Name == state.PaletteName);
            if (paletteToLoad != null)
            {
                _paletteManager.ActivePalette = paletteToLoad;
                ApplyActivePalette();
            }
            if (state is JuliaFamilySaveState juliaState)
            {
                if (nudRe != null && nudIm != null && nudRe.Visible)
                {
                    nudRe.Value = ClampDecimal(juliaState.CRe, nudRe.Minimum, nudRe.Maximum);
                    nudIm.Value = ClampDecimal(juliaState.CIm, nudIm.Minimum, nudIm.Maximum);
                }
            }
            lock (_bitmapLock)
            {
                _previewBitmap?.Dispose();
                _previewBitmap = null;
                _currentRenderingBitmap?.Dispose();
                _currentRenderingBitmap = null;
            }
            _renderedCenterX = _centerX;
            _renderedCenterY = _centerY;
            _renderedZoom = _zoom;
            UpdateEngineParameters();
            ScheduleRender();
        }
        public virtual async Task<byte[]> RenderPreviewTileAsync(FractalSaveStateBase stateBase, TileInfo tile, int totalWidth, int totalHeight, int tileSize)
        {
            return await Task.Run(() =>
            {
                if (string.IsNullOrEmpty(stateBase.PreviewParametersJson))
                {
                    return new byte[tile.Bounds.Width * tile.Bounds.Height * 4];
                }
                PreviewParams previewParams;
                try
                {
                    previewParams = JsonSerializer.Deserialize<PreviewParams>(stateBase.PreviewParametersJson);
                }
                catch
                {
                    return new byte[tile.Bounds.Width * tile.Bounds.Height * 4];
                }
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
                    default: return new byte[tile.Bounds.Width * tile.Bounds.Height * 4];
                }
                previewEngine.MaxIterations = 400;
                previewEngine.CenterX = previewParams.CenterX;
                previewEngine.CenterY = previewParams.CenterY;
                decimal previewBaseScale = this.BaseScale;
                if (previewParams.Zoom == 0) previewParams.Zoom = 0.001m;
                previewEngine.Scale = previewBaseScale / previewParams.Zoom;
                previewEngine.ThresholdSquared = previewParams.Threshold * previewParams.Threshold;
                var paletteForPreview = _paletteManager.Palettes.FirstOrDefault(p => p.Name == previewParams.PaletteName) ?? _paletteManager.Palettes.First();
                previewEngine.Palette = GeneratePaletteFunction(paletteForPreview);
                if (paletteForPreview.Name == "Стандартный серый" || paletteForPreview.IsGradient)
                {
                    previewEngine.MaxColorIterations = Math.Max(1, previewEngine.MaxIterations);
                }
                else
                {
                    previewEngine.MaxColorIterations = Math.Max(1, paletteForPreview.Colors.Count);
                }
                return previewEngine.RenderSingleTile(tile, totalWidth, totalHeight, out _);
            });
        }
        public virtual Bitmap RenderPreview(FractalSaveStateBase stateBase, int previewWidth, int previewHeight)
        {
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
                default:
                    var bmpError = new Bitmap(previewWidth, previewHeight);
                    using (var g = Graphics.FromImage(bmpError))
                    {
                        g.Clear(Color.DarkOrange);
                        TextRenderer.DrawText(g, "Неизв. тип движка", Font, new Rectangle(0, 0, previewWidth, previewHeight), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                    }
                    return bmpError;
            }
            previewEngine.CenterX = previewParams.CenterX;
            previewEngine.CenterY = previewParams.CenterY;
            decimal previewBaseScale = this.BaseScale;
            if (previewParams.Zoom == 0) previewParams.Zoom = 0.001m;
            previewEngine.Scale = previewBaseScale / previewParams.Zoom;
            previewEngine.MaxIterations = previewParams.Iterations;
            previewEngine.ThresholdSquared = previewParams.Threshold * previewParams.Threshold;
            var paletteForPreview = _paletteManager.Palettes.FirstOrDefault(p => p.Name == previewParams.PaletteName) ?? _paletteManager.Palettes.First();
            previewEngine.Palette = GeneratePaletteFunction(paletteForPreview);
            if (paletteForPreview.Name == "Стандартный серый" || paletteForPreview.IsGradient)
            {
                previewEngine.MaxColorIterations = Math.Max(1, previewParams.Iterations);
            }
            else
            {
                previewEngine.MaxColorIterations = Math.Max(1, paletteForPreview.Colors.Count);
            }
            return previewEngine.RenderToBitmap(previewWidth, previewHeight, 1, progress => { });
        }
        public virtual List<FractalSaveStateBase> LoadAllSavesForThisType()
        {
            throw new NotImplementedException($"Метод LoadAllSavesForThisType должен быть переопределен в классе {this.GetType().Name}, чтобы загружать состояния типа {this.ConcreteSaveStateType.Name}.");
        }
        public virtual void SaveAllSavesForThisType(List<FractalSaveStateBase> saves)
        {
            throw new NotImplementedException($"Метод SaveAllSavesForThisType должен быть переопределен в классе {this.GetType().Name}, чтобы сохранять состояния типа {this.ConcreteSaveStateType.Name}.");
        }
        #endregion
    }
}