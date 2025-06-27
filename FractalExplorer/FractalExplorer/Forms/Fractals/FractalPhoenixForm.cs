using FractalExplorer.Utilities;
using FractalExplorer.Engines;
using FractalExplorer.Resources;
using FractalExplorer.SelectorsForms;
using System.Drawing.Imaging;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json;
using FractalExplorer.Utilities.SaveIO.SaveStateImplementations;
using FractalExplorer.Utilities.SaveIO;
using FractalExplorer.Utilities.SaveIO.ColorPalettes;

namespace FractalExplorer.Forms
{
    /// <summary>
    /// Представляет основную форму для отображения и взаимодействия с фракталом Феникс.
    /// Реализует интерфейс <see cref="ISaveLoadCapableFractal"/> для сохранения и загрузки состояний фрактала.
    /// </summary>
    public partial class FractalPhoenixForm : Form, ISaveLoadCapableFractal
    {
        #region Fields
        private PhoenixEngine _fractalEngine;
        private RenderVisualizerComponent _renderVisualizer;
        private ColorPaletteMandelbrotFamily _paletteManager;
        private ColorConfigurationMandelbrotFamilyForm _colorConfigForm;
        private PhoenixCSelectorForm _phoenixCSelectorWindow;

        // Размер плиток для тайлового рендеринга.
        private const int TILE_SIZE = 32;
        // Объект для блокировки доступа к битмапам во время рендеринга или модификации.
        private readonly object _bitmapLock = new object();
        private Bitmap _previewBitmap;
        private Bitmap _currentRenderingBitmap;
        private CancellationTokenSource _previewRenderCts;

        private volatile bool _isHighResRendering = false;
        private volatile bool _isRenderingPreview = false;

        /// <summary>
        /// Текущий уровень масштабирования фрактала.
        /// </summary>
        protected decimal _zoom = 1.0m;

        /// <summary>
        /// Текущая координата X центра фрактала в комплексной плоскости.
        /// </summary>
        protected decimal _centerX = 0.0m;

        /// <summary>
        /// Текущая координата Y центра фрактала в комплексной плоскости.
        /// </summary>
        protected decimal _centerY = 0.0m;

        private decimal _renderedCenterX;
        private decimal _renderedCenterY;
        private decimal _renderedZoom;

        private Point _panStart;
        private bool _panning = false;

        private System.Windows.Forms.Timer _renderDebounceTimer;
        // Базовый масштаб, от которого вычисляется текущий масштаб рендеринга.
        private const decimal BASE_SCALE = 4.0m;
        #endregion

        #region Constructor
        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="FractalPhoenixForm"/>.
        /// </summary>
        public FractalPhoenixForm()
        {
            InitializeComponent();
            Text = "Фрактал Феникс";
        }
        #endregion

        #region UI Initialization & Event Handlers
        /// <summary>
        /// Инициализирует значения по умолчанию и ограничения для элементов управления UI.
        /// </summary>
        private void InitializeControls()
        {
            // Заполняем выпадающий список количеством потоков ЦП, включая опцию "Авто".
            int cores = Environment.ProcessorCount;
            cbThreads.Items.Clear();
            for (int i = 1; i <= cores; i++)
            {
                cbThreads.Items.Add(i);
            }
            cbThreads.Items.Add("Auto");
            cbThreads.SelectedItem = "Auto";

            nudIterations.Minimum = 10;
            nudIterations.Maximum = 100000;
            nudIterations.Value = 100;

            nudThreshold.Minimum = 2m;
            nudThreshold.Maximum = 1000m;
            nudThreshold.DecimalPlaces = 1;
            nudThreshold.Increment = 0.1m;
            nudThreshold.Value = 4m;

            nudZoom.DecimalPlaces = 15;
            nudZoom.Increment = 0.1m;
            nudZoom.Minimum = 0.000000000000001m;
            nudZoom.Maximum = 1000000000000000m;
            // Устанавливаем начальный масштаб относительно базового.
            _zoom = BASE_SCALE / 4.0m;
            nudZoom.Value = _zoom;

            nudC1Re.Value = 0.56m;
            nudC1Im.Value = -0.5m;
            nudC2Re.Value = 0.0m;
            nudC2Im.Value = 0.0m;

            nudC1Re.DecimalPlaces = 15;
            nudC1Im.DecimalPlaces = 15;
            nudC2Re.DecimalPlaces = 15;
            nudC2Im.DecimalPlaces = 15;

            nudC1Re.Increment = 0.001m;
            nudC1Im.Increment = 0.001m;
            nudC2Re.Increment = 0.001m;
            nudC2Im.Increment = 0.001m;

            nudC1Re.Minimum = -2m; nudC1Re.Maximum = 2m;
            nudC1Im.Minimum = -2m; nudC1Im.Maximum = 2m;
            nudC2Re.Minimum = -2m; nudC2Re.Maximum = 2m;
            nudC2Im.Minimum = -2m; nudC2Im.Maximum = 2m;
        }

        /// <summary>
        /// Инициализирует обработчики событий для различных элементов управления UI.
        /// </summary>
        private void InitializeEventHandlers()
        {
            // Подписываемся на изменения числовых значений и выбора элементов.
            nudC1Re.ValueChanged += ParamControl_Changed;
            nudC1Im.ValueChanged += ParamControl_Changed;
            nudC2Re.ValueChanged += ParamControl_Changed;
            nudC2Im.ValueChanged += ParamControl_Changed;

            nudIterations.ValueChanged += ParamControl_Changed;
            nudThreshold.ValueChanged += ParamControl_Changed;
            cbThreads.SelectedIndexChanged += ParamControl_Changed;
            nudZoom.ValueChanged += ParamControl_Changed;

            // Подписываемся на события мыши и изменения размера для области рисования.
            canvas.MouseWheel += Canvas_MouseWheel;
            canvas.MouseDown += Canvas_MouseDown;
            canvas.MouseMove += Canvas_MouseMove;
            canvas.MouseUp += Canvas_MouseUp;
            canvas.Paint += Canvas_Paint;
            canvas.Resize += (s, e) =>
            {
                // Планируем рендеринг при изменении размера окна, если оно не свернуто.
                if (WindowState != FormWindowState.Minimized)
                {
                    ScheduleRender();
                }
            };
        }

        /// <summary>
        /// Обрабатывает событие загрузки формы.
        /// Инициализирует менеджер палитр, движок фрактала, таймер и визуализатор рендеринга.
        /// Устанавливает начальные параметры и запускает первый рендеринг.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Данные события.</param>
        private void FractalPhoenixForm_Load(object sender, EventArgs e)
        {
            _paletteManager = new ColorPaletteMandelbrotFamily();
            _fractalEngine = new PhoenixEngine();
            _renderDebounceTimer = new System.Windows.Forms.Timer { Interval = 300 };
            _renderDebounceTimer.Tick += RenderDebounceTimer_Tick;
            _renderVisualizer = new RenderVisualizerComponent(TILE_SIZE);
            _renderVisualizer.NeedsRedraw += OnVisualizerNeedsRedraw;

            InitializeControls();
            InitializeEventHandlers();

            // Устанавливаем начальные координаты и масштаб.
            _centerX = 0.0m;
            _centerY = 0.0m;
            _renderedCenterX = _centerX;
            _renderedCenterY = _centerY;
            _renderedZoom = _zoom;

            ApplyActivePalette();
            ScheduleRender();
        }

        /// <summary>
        /// Обрабатывает событие, когда визуализатору рендеринга требуется перерисовка.
        /// Запрашивает перерисовку канваса, обеспечивая обновление UI из потока UI.
        /// </summary>
        private void OnVisualizerNeedsRedraw()
        {
            // Используем BeginInvoke для безопасной перерисовки из другого потока.
            if (canvas.IsHandleCreated && !canvas.IsDisposed)
            {
                canvas.BeginInvoke((Action)(() => canvas.Invalidate()));
            }
        }

        /// <summary>
        /// Обрабатывает нажатие кнопки "Настройки цвета".
        /// Открывает или активирует форму настроек цвета для фрактала.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Данные события.</param>
        private void color_configurations_Click(object sender, EventArgs e)
        {
            // Создаем новую форму, если она еще не существует или была закрыта.
            if (_colorConfigForm == null || _colorConfigForm.IsDisposed)
            {
                _colorConfigForm = new ColorConfigurationMandelbrotFamilyForm(_paletteManager);
                _colorConfigForm.PaletteApplied += OnPaletteApplied;
                // Обнуляем ссылку на форму при ее закрытии, чтобы можно было создать новую.
                _colorConfigForm.FormClosed += (s, args) => _colorConfigForm = null;
                _colorConfigForm.Show(this);
            }
            else
            {
                // Если форма уже открыта, просто активируем ее.
                _colorConfigForm.Activate();
            }
        }

        /// <summary>
        /// Обрабатывает событие применения новой палитры цветов.
        /// Применяет выбранную палитру и перерисовывает фрактал.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Данные события.</param>
        private void OnPaletteApplied(object sender, EventArgs e)
        {
            ApplyActivePalette();
            ScheduleRender();
        }

        /// <summary>
        /// Обрабатывает изменение значений элементов управления, влияющих на параметры фрактала.
        /// Если происходит изменение масштаба, обновляет внутреннее значение масштаба.
        /// Запускает планирование нового рендеринга.
        /// </summary>
        /// <param name="sender">Источник события (например, NumericUpDown).</param>
        /// <param name="e">Данные события.</param>
        private void ParamControl_Changed(object sender, EventArgs e)
        {
            // Игнорируем изменения, если в данный момент идет высококачественный рендеринг,
            // чтобы избежать конфликтов и нестабильности.
            if (_isHighResRendering)
            {
                return;
            }

            if (sender == nudZoom)
            {
                // Обновляем внутреннее значение _zoom, только если оно отличается от значения NumericUpDown,
                // чтобы избежать бесконечного цикла, если ScheduleRender вызовет ParamControl_Changed.
                if (nudZoom.Value != _zoom)
                {
                    _zoom = nudZoom.Value;
                }
            }
            ScheduleRender();
        }

        /// <summary>
        /// Обрабатывает нажатие кнопки "Рендеринг".
        /// Отменяет текущий предпросмотр и немедленно запускает новый рендеринг.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Данные события.</param>
        private async void btnRender_Click(object sender, EventArgs e)
        {
            _previewRenderCts?.Cancel();
            _renderDebounceTimer.Stop();

            // Предотвращаем запуск нового рендеринга, если какой-либо рендеринг уже активен.
            if (_isHighResRendering || _isRenderingPreview)
            {
                return;
            }
            await StartPreviewRender();
        }

        /// <summary>
        /// Обрабатывает событие прокрутки колеса мыши для масштабирования фрактала.
        /// Изменяет текущий масштаб и центр фрактала, чтобы масштабирование происходило относительно точки под курсором.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Данные события мыши.</param>
        private void Canvas_MouseWheel(object sender, MouseEventArgs e)
        {
            // Игнорируем событие, если идет высококачественный рендеринг.
            if (_isHighResRendering)
            {
                return;
            }
            // "Запекаем" текущий частичный рендеринг в предпросмотр, чтобы избежать его потери при масштабировании.
            CommitAndBakePreview();

            decimal zoomFactor = e.Delta > 0 ? 1.5m : 1.0m / 1.5m;
            decimal scaleBeforeZoom = BASE_SCALE / _zoom;

            // Вычисляем мировые координаты точки под курсором до изменения масштаба.
            decimal mouseReal = _centerX + (e.X - canvas.Width / 2.0m) * scaleBeforeZoom / canvas.Width;
            decimal mouseImaginary = _centerY - (e.Y - canvas.Height / 2.0m) * scaleBeforeZoom / canvas.Height;

            _zoom = Math.Max(nudZoom.Minimum, Math.Min(nudZoom.Maximum, _zoom * zoomFactor));

            decimal scaleAfterZoom = BASE_SCALE / _zoom;
            // Корректируем центр, чтобы точка под курсором оставалась на месте после масштабирования.
            _centerX = mouseReal - (e.X - canvas.Width / 2.0m) * scaleAfterZoom / canvas.Width;
            _centerY = mouseImaginary + (e.Y - canvas.Height / 2.0m) * scaleAfterZoom / canvas.Height;

            canvas.Invalidate(); // Запрашиваем перерисовку элемента управления.
            // Обновляем значение NumericUpDown, если оно отличается, или планируем рендеринг, если нет.
            if (nudZoom.Value != _zoom)
            {
                nudZoom.Value = _zoom;
            }
            else
            {
                ScheduleRender();
            }
        }

        /// <summary>
        /// Обрабатывает событие нажатия кнопки мыши на канвасе для начала панорамирования.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Данные события мыши.</param>
        private void Canvas_MouseDown(object sender, MouseEventArgs e)
        {
            // Игнорируем событие, если идет высококачественный рендеринг.
            if (_isHighResRendering)
            {
                return;
            }
            if (e.Button == MouseButtons.Left)
            {
                _panning = true;
                _panStart = e.Location;
                canvas.Cursor = Cursors.Hand; // Меняем курсор для индикации панорамирования.
            }
        }

        /// <summary>
        /// Обрабатывает событие перемещения мыши по канвасу для выполнения панорамирования.
        /// Изменяет центр фрактала в соответствии с перемещением мыши.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Данные события мыши.</param>
        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            // Игнорируем событие, если идет высококачественный рендеринг или панорамирование не активно.
            if (_isHighResRendering || !_panning)
            {
                return;
            }
            // "Запекаем" текущий частичный рендеринг в предпросмотр, чтобы избежать его потери при панорамировании.
            CommitAndBakePreview();

            // Вычисляем, сколько мировых единиц соответствует одному пикселю на текущем масштабе.
            decimal unitsPerPixel = BASE_SCALE / _zoom / canvas.Width;
            // Обновляем центр фрактала, перемещая его в противоположном направлении от движения мыши.
            _centerX -= (decimal)(e.X - _panStart.X) * unitsPerPixel;
            _centerY += (decimal)(e.Y - _panStart.Y) * unitsPerPixel;
            _panStart = e.Location; // Обновляем начальную точку панорамирования для следующего шага.

            canvas.Invalidate(); // Запрашиваем перерисовку для визуального обновления.
            ScheduleRender(); // Планируем новый рендеринг с учетом нового центра.
        }

        /// <summary>
        /// Обрабатывает событие отпускания кнопки мыши на канвасе для завершения панорамирования.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Данные события мыши.</param>
        private void Canvas_MouseUp(object sender, MouseEventArgs e)
        {
            // Игнорируем событие, если идет высококачественный рендеринг.
            if (_isHighResRendering)
            {
                return;
            }
            if (e.Button == MouseButtons.Left)
            {
                _panning = false;
                canvas.Cursor = Cursors.Default; // Возвращаем обычный курсор.
            }
        }

        /// <summary>
        /// Обрабатывает событие рисования на канвасе фрактала.
        /// Очищает фон и отрисовывает отрендеренный или текущий рендеринг, масштабируя его относительно текущего положения и масштаба.
        /// Также отрисовывает визуализатор прогресса тайлового рендеринга.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Данные события рисования.</param>
        private void Canvas_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.Clear(Color.Black);
            lock (_bitmapLock) // Используем блокировку для безопасного доступа к битмапам.
            {
                // Если есть отрендеренный предпросмотр и размеры канваса корректны.
                if (_previewBitmap != null && canvas.Width > 0 && canvas.Height > 0)
                {
                    // Если текущие параметры соответствуют отрендеренному изображению, отрисовываем без масштабирования.
                    if (_renderedCenterX == _centerX && _renderedCenterY == _centerY && _renderedZoom == _zoom)
                    {
                        e.Graphics.DrawImageUnscaled(_previewBitmap, Point.Empty);
                    }
                    else
                    {
                        // Если параметры изменились, масштабируем и сдвигаем отрендеренный битмап,
                        // чтобы он соответствовал текущему виду.
                        try
                        {
                            decimal renderedComplexWidth = BASE_SCALE / _renderedZoom;
                            decimal currentComplexWidth = BASE_SCALE / _zoom;

                            // Проверяем на деление на ноль или некорректные значения.
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

                                // Используем билинейную интерполяцию для более плавного масштабирования.
                                e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Bilinear;
                                PointF destPoint1 = new PointF((float)offsetXPixels, (float)offsetYPixels);
                                PointF destPoint2 = new PointF((float)(offsetXPixels + newWidthPixels), (float)offsetYPixels);
                                PointF destPoint3 = new PointF((float)offsetXPixels, (float)(offsetYPixels + newHeightPixels));
                                e.Graphics.DrawImage(_previewBitmap, new PointF[] { destPoint1, destPoint2, destPoint3 });
                            }
                        }
                        catch (Exception) { /* Игнорируем ошибки интерполяции, чтобы не прерывать отрисовку. */ }
                    }
                }
                // Отрисовываем текущий, незавершенный рендеринг поверх предпросмотра.
                if (_currentRenderingBitmap != null)
                {
                    e.Graphics.DrawImageUnscaled(_currentRenderingBitmap, Point.Empty);
                }
            }
            // Отрисовываем визуализатор рендеринга (сетку тайлов и прогресс).
            if (_renderVisualizer != null && _isRenderingPreview)
            {
                _renderVisualizer.DrawVisualization(e.Graphics);
            }
        }

        /// <summary>
        /// Обрабатывает событие тика таймера отложенного рендеринга.
        /// Запускает рендеринг предпросмотра, если нет активного рендеринга.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Данные события.</param>
        private async void RenderDebounceTimer_Tick(object sender, EventArgs e)
        {
            _renderDebounceTimer.Stop();
            // Если уже идет рендеринг (высококачественный или предпросмотр),
            // планируем рендеринг снова, так как текущий тик может быть устаревшим.
            if (_isHighResRendering || _isRenderingPreview)
            {
                ScheduleRender();
                return;
            }
            await StartPreviewRender();
        }
        #endregion

        #region Rendering Logic
        /// <summary>
        /// Планирует рендеринг фрактала.
        /// Отменяет любой текущий рендеринг предпросмотра и запускает таймер для задержки рендеринга,
        /// чтобы избежать чрезмерной перерисовки при быстрых изменениях параметров.
        /// </summary>
        private void ScheduleRender()
        {
            // Не планируем рендеринг, если идет высококачественный рендеринг или форма свернута.
            if (_isHighResRendering || WindowState == FormWindowState.Minimized)
            {
                return;
            }
            // Если идет предпросмотр, отменяем его, чтобы начать новый.
            if (_isRenderingPreview)
            {
                _previewRenderCts?.Cancel();
            }
            _renderDebounceTimer.Stop();
            _renderDebounceTimer.Start();
        }

        /// <summary>
        /// Запускает асинхронный процесс рендеринга предпросмотра фрактала.
        /// Рендеринг осуществляется по тайлам в фоновом потоке, с обновлением UI.
        /// </summary>
        /// <returns>Задача, представляющая асинхронную операцию рендеринга.</returns>
        private async Task StartPreviewRender()
        {
            // Не рендерим, если размеры канваса некорректны.
            if (canvas.Width <= 0 || canvas.Height <= 0)
            {
                return;
            }

            _isRenderingPreview = true;
            _previewRenderCts?.Cancel(); // Отменяем предыдущую задачу рендеринга, если таковая имеется.
            _previewRenderCts = new CancellationTokenSource();
            var token = _previewRenderCts.Token;

            _renderVisualizer?.NotifyRenderSessionStart(); // Уведомляем визуализатор о начале новой сессии рендеринга.

            var newRenderingBitmap = new Bitmap(canvas.Width, canvas.Height, PixelFormat.Format32bppArgb);
            lock (_bitmapLock) // Блокируем доступ к битмапам во время их обновления.
            {
                _currentRenderingBitmap?.Dispose(); // Освобождаем ресурсы предыдущего текущего битмапа.
                _currentRenderingBitmap = newRenderingBitmap; // Устанавливаем новый битмап как текущий.
            }

            UpdateEngineParameters(); // Обновляем параметры движка фрактала перед рендерингом.

            // Сохраняем текущие параметры вида, чтобы знать, при каких параметрах был сделан рендеринг.
            var currentRenderedCenterX = _centerX;
            var currentRenderedCenterY = _centerY;
            var currentRenderedZoom = _zoom;

            // Создаем копию движка для рендеринга, чтобы избежать конфликтов с основным движком.
            var renderEngineCopy = new PhoenixEngine();
            renderEngineCopy.MaxIterations = _fractalEngine.MaxIterations;
            renderEngineCopy.ThresholdSquared = _fractalEngine.ThresholdSquared;
            renderEngineCopy.CenterX = _fractalEngine.CenterX;
            renderEngineCopy.CenterY = _fractalEngine.CenterY;
            renderEngineCopy.Scale = _fractalEngine.Scale;
            renderEngineCopy.C1 = _fractalEngine.C1;
            renderEngineCopy.C2 = _fractalEngine.C2;
            renderEngineCopy.Palette = _fractalEngine.Palette;
            renderEngineCopy.MaxColorIterations = _fractalEngine.MaxColorIterations;

            var tiles = GenerateTiles(canvas.Width, canvas.Height); // Генерируем список тайлов для рендеринга.
            var dispatcher = new TileRenderDispatcher(tiles, GetThreadCount()); // Создаем диспетчер для рендеринга тайлов.

            // Инициализируем ProgressBar.
            if (pbRenderProgress.IsHandleCreated && !pbRenderProgress.IsDisposed)
            {
                pbRenderProgress.Invoke((Action)(() => { pbRenderProgress.Value = 0; pbRenderProgress.Maximum = tiles.Count; }));
            }
            int progress = 0; // Переменная для отслеживания прогресса.

            try
            {
                await dispatcher.RenderAsync(async (tile, ct) =>
                {
                    ct.ThrowIfCancellationRequested(); // Проверяем токен отмены.
                    _renderVisualizer?.NotifyTileRenderStart(tile.Bounds); // Уведомляем визуализатор о начале рендеринга тайла.
                    var tileBuffer = renderEngineCopy.RenderSingleTile(tile, canvas.Width, canvas.Height, out int bytesPerPixel);
                    ct.ThrowIfCancellationRequested(); // Проверяем токен отмены после рендеринга тайла.

                    lock (_bitmapLock) // Блокируем для безопасной записи в битмап.
                    {
                        // Проверяем, не была ли задача отменена или не был ли запущен новый рендеринг.
                        if (ct.IsCancellationRequested || _currentRenderingBitmap != newRenderingBitmap)
                        {
                            return;
                        }

                        // Копируем данные тайла в основной битмап.
                        var tileRect = tile.Bounds;
                        var bitmapRect = new Rectangle(0, 0, _currentRenderingBitmap.Width, _currentRenderingBitmap.Height);
                        tileRect.Intersect(bitmapRect); // Обрезаем прямоугольник тайла по границам битмапа.

                        if (tileRect.Width == 0 || tileRect.Height == 0)
                        {
                            return;
                        }

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
                    _renderVisualizer?.NotifyTileRenderComplete(tile.Bounds); // Уведомляем визуализатор о завершении рендеринга тайла.

                    // Обновляем ProgressBar в потоке UI.
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
                    await Task.Yield(); // Освобождаем поток для других задач.
                }, token);

                token.ThrowIfCancellationRequested(); // Окончательная проверка токена отмены.

                lock (_bitmapLock) // Блокируем для безопасного обмена битмапами.
                {
                    // Если текущий рендеринг успешно завершен и не был заменен новым.
                    if (_currentRenderingBitmap == newRenderingBitmap)
                    {
                        _previewBitmap?.Dispose(); // Освобождаем ресурсы старого предпросмотра.
                        _previewBitmap = _currentRenderingBitmap; // Устанавливаем текущий как новый предпросмотр.
                        _currentRenderingBitmap = null; // Обнуляем текущий битмап.
                        // Сохраняем параметры, при которых был отрендерен _previewBitmap.
                        _renderedCenterX = currentRenderedCenterX;
                        _renderedCenterY = currentRenderedCenterY;
                        _renderedZoom = currentRenderedZoom;
                    }
                    else
                    {
                        newRenderingBitmap?.Dispose(); // Если битмап был заменен, освобождаем ресурсы этого.
                    }
                }
                if (canvas.IsHandleCreated && !canvas.IsDisposed)
                {
                    canvas.Invalidate(); // Запрашиваем перерисовку канваса.
                }
            }
            catch (OperationCanceledException)
            {
                lock (_bitmapLock)
                {
                    // Освобождаем ресурсы текущего битмапа, если рендеринг был отменен.
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
                newRenderingBitmap?.Dispose(); // В случае ошибки освобождаем ресурсы.
                if (IsHandleCreated && !IsDisposed)
                {
                    MessageBox.Show($"Ошибка рендеринга: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            finally
            {
                _isRenderingPreview = false; // Сбрасываем флаг активного предпросмотра.
                _renderVisualizer?.NotifyRenderSessionComplete(); // Уведомляем визуализатор о завершении сессии.
                // Сбрасываем ProgressBar в потоке UI.
                if (pbRenderProgress.IsHandleCreated && !pbRenderProgress.IsDisposed)
                {
                    pbRenderProgress.Invoke((Action)(() => pbRenderProgress.Value = 0));
                }
            }
        }

        /// <summary>
        /// "Запекает" текущий незавершенный рендеринг (если он есть) в основной битмап предпросмотра.
        /// Это используется при интерактивных операциях, таких как масштабирование или панорамирование,
        /// чтобы сохранить уже отрендеренные части изображения до запуска нового рендеринга.
        /// </summary>
        private void CommitAndBakePreview()
        {
            lock (_bitmapLock) // Блокируем доступ к битмапам.
            {
                // Проверяем, идет ли рендеринг предпросмотра и есть ли текущий битмап.
                if (!_isRenderingPreview || _currentRenderingBitmap == null)
                {
                    return;
                }
            }
            _previewRenderCts?.Cancel(); // Отменяем текущий рендеринг, чтобы он не конфликтовал с "запеканием".

            lock (_bitmapLock) // Повторно блокируем, так как доступ к битмапам должен быть синхронизирован.
            {
                if (_currentRenderingBitmap == null)
                {
                    return;
                }

                // Создаем новый битмап для "запеченного" изображения.
                var bakedBitmap = new Bitmap(canvas.Width, canvas.Height, PixelFormat.Format24bppRgb);
                using (var g = Graphics.FromImage(bakedBitmap))
                {
                    g.Clear(Color.Black);
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Bilinear;

                    // Отрисовываем существующий предпросмотр, если он есть, масштабируя его по текущим параметрам.
                    if (_previewBitmap != null)
                    {
                        try
                        {
                            decimal renderedComplexWidth = BASE_SCALE / _renderedZoom;
                            decimal currentComplexWidth = BASE_SCALE / _zoom;

                            // Проверяем на деление на ноль или некорректные значения.
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
                        catch (Exception) { /* Игнорируем ошибки отрисовки. */ }
                    }
                    // Отрисовываем текущий незавершенный рендеринг поверх всего.
                    g.DrawImageUnscaled(_currentRenderingBitmap, Point.Empty);
                }
                _previewBitmap?.Dispose(); // Освобождаем старый битмап предпросмотра.
                _previewBitmap = bakedBitmap; // Назначаем новый "запеченный" битмап.
                _currentRenderingBitmap.Dispose(); // Освобождаем текущий рендеринг, так как он "запечен".
                _currentRenderingBitmap = null;

                // Обновляем параметры, при которых был "запечен" новый предпросмотр.
                _renderedCenterX = _centerX;
                _renderedCenterY = _centerY;
                _renderedZoom = _zoom;
            }
        }

        /// <summary>
        /// Обновляет параметры движка рендеринга фрактала, используя текущие значения из элементов управления UI.
        /// </summary>
        private void UpdateEngineParameters()
        {
            _fractalEngine.MaxIterations = (int)nudIterations.Value;
            _fractalEngine.ThresholdSquared = nudThreshold.Value * nudThreshold.Value;
            _fractalEngine.CenterX = _centerX;
            _fractalEngine.CenterY = _centerY;
            // Вычисляем масштаб для движка на основе BASE_SCALE и текущего зума.
            _fractalEngine.Scale = BASE_SCALE / _zoom;
            _fractalEngine.C1 = new ComplexDecimal(nudC1Re.Value, nudC1Im.Value);
            _fractalEngine.C2 = new ComplexDecimal(nudC2Re.Value, nudC2Im.Value);
            ApplyActivePalette(); // Применяем активную палитру, так как она тоже является параметром движка.
        }

        /// <summary>
        /// Генерирует список объектов <see cref="TileInfo"/> для тайлового рендеринга фрактала.
        /// Тайлы сортируются от центра к краям, чтобы наиболее важные части изображения рендерились первыми.
        /// </summary>
        /// <param name="width">Общая ширина области рендеринга.</param>
        /// <param name="height">Общая высота области рендеринга.</param>
        /// <returns>Список <see cref="TileInfo"/>, представляющих тайлы для рендеринга.</returns>
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
            // Сортируем тайлы по удаленности от центра, чтобы центральные части рендерились первыми.
            return tiles.OrderBy(t => Math.Pow(t.Center.X - center.X, 2) + Math.Pow(t.Center.Y - center.Y, 2)).ToList();
        }

        /// <summary>
        /// Определяет количество потоков ЦП для рендеринга на основе выбора пользователя.
        /// Если выбрано "Авто", возвращает количество логических процессоров системы.
        /// </summary>
        /// <returns>Количество потоков ЦП для использования в рендеринге.</returns>
        private int GetThreadCount()
        {
            return cbThreads.SelectedItem?.ToString() == "Auto" ? Environment.ProcessorCount : Convert.ToInt32(cbThreads.SelectedItem);
        }
        #endregion

        #region Save High-Res
        /// <summary>
        /// Обрабатывает нажатие кнопки "Сохранить в высоком разрешении".
        /// Запускает асинхронный процесс рендеринга фрактала в высоком разрешении и сохранения его в PNG файл.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Данные события.</param>
        private async void btnSaveHighRes_Click(object sender, EventArgs e)
        {
            if (_isHighResRendering)
            {
                MessageBox.Show("Процесс сохранения уже запущен.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int saveWidth = (int)nudSaveWidth.Value;
            int saveHeight = (int)nudSaveHeight.Value;

            // Формируем предлагаемое имя файла на основе текущих параметров фрактала.
            string c1ReStr = nudC1Re.Value.ToString("F6", CultureInfo.InvariantCulture).Replace(".", "_");
            string c1ImStr = nudC1Im.Value.ToString("F6", CultureInfo.InvariantCulture).Replace(".", "_");
            string c2ReStr = nudC2Re.Value.ToString("F6", CultureInfo.InvariantCulture).Replace(".", "_");
            string c2ImStr = nudC2Im.Value.ToString("F6", CultureInfo.InvariantCulture).Replace(".", "_");
            string fractalDetails = $"phoenix_P{c1ReStr}_Q{c1ImStr}_C2Re{c2ReStr}_C2Im{c2ImStr}";
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string suggestedFileName = $"{fractalDetails}_{timestamp}.png";

            using (var saveDialog = new SaveFileDialog { Filter = "PNG Image|*.png", Title = "Сохранить фрактал Феникс (Высокое разрешение)", FileName = suggestedFileName })
            {
                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    // Отменяем текущий предпросмотр, чтобы избежать конфликтов.
                    if (_isRenderingPreview)
                    {
                        _previewRenderCts?.Cancel();
                    }
                    _isHighResRendering = true;
                    pnlControls.Enabled = false; // Отключаем элементы управления во время высококачественного рендеринга.
                    pbHighResProgress.Value = 0;
                    pbHighResProgress.Visible = true;

                    try
                    {
                        var renderEngine = new PhoenixEngine(); // Создаем новый экземпляр движка для сохранения.
                        UpdateEngineParameters(); // Обновляем параметры из UI.

                        // Копируем параметры из основного движка в новый для высококачественного рендеринга.
                        renderEngine.MaxIterations = _fractalEngine.MaxIterations;
                        renderEngine.ThresholdSquared = _fractalEngine.ThresholdSquared;
                        renderEngine.CenterX = _fractalEngine.CenterX;
                        renderEngine.CenterY = _fractalEngine.CenterY;
                        renderEngine.Scale = _fractalEngine.Scale;
                        renderEngine.C1 = _fractalEngine.C1;
                        renderEngine.C2 = _fractalEngine.C2;
                        renderEngine.Palette = GeneratePaletteFunction(_paletteManager.ActivePalette); // Генерируем функцию палитры.
                        renderEngine.MaxColorIterations = _fractalEngine.MaxColorIterations;

                        int threadCount = GetThreadCount();

                        Bitmap highResBitmap = await Task.Run(() => renderEngine.RenderToBitmap(
                            saveWidth, saveHeight, threadCount,
                            progress =>
                            {
                                // Обновляем ProgressBar в потоке UI.
                                if (pbHighResProgress.IsHandleCreated && !pbHighResProgress.IsDisposed)
                                {
                                    pbHighResProgress.Invoke((Action)(() => pbHighResProgress.Value = Math.Min(pbHighResProgress.Maximum, progress)));
                                }
                            }
                        ));

                        highResBitmap.Save(saveDialog.FileName, ImageFormat.Png);
                        highResBitmap.Dispose();
                        MessageBox.Show("Изображение успешно сохранено!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        _isHighResRendering = false;
                        pnlControls.Enabled = true; // Включаем элементы управления обратно.
                        // Сбрасываем ProgressBar в потоке UI.
                        if (pbHighResProgress.IsHandleCreated && !pbHighResProgress.IsDisposed)
                        {
                            pbHighResProgress.Invoke((Action)(() => { pbHighResProgress.Visible = false; pbHighResProgress.Value = 0; }));
                        }
                        ScheduleRender(); // Планируем новый рендеринг предпросмотра после сохранения.
                    }
                }
            }
        }
        #endregion

        #region Palette Management
        /// <summary>
        /// Ограничивает значение цветового компонента в диапазоне [0, 255].
        /// </summary>
        /// <param name="component">Входное значение компонента цвета.</param>
        /// <returns>Ограниченное значение компонента цвета.</returns>
        private static int ClampColorComponent(int component)
        {
            if (component < 0) return 0;
            if (component > 255) return 255;
            return component;
        }

        /// <summary>
        /// Ограничивает значение цветового компонента в диапазоне [0, 255].
        /// Перегрузка для типа double.
        /// </summary>
        /// <param name="component">Входное значение компонента цвета.</param>
        /// <returns>Ограниченное значение компонента цвета, приведенное к int.</returns>
        private static int ClampColorComponent(double component)
        {
            if (component < 0) return 0;
            if (component > 255) return 255;
            return (int)component;
        }

        /// <summary>
        /// Применяет активную палитру цветов из менеджера палитр к движку рендеринга.
        /// Также настраивает количество итераций для раскраски в зависимости от типа палитры.
        /// </summary>
        private void ApplyActivePalette()
        {
            if (_fractalEngine == null || _paletteManager.ActivePalette == null)
            {
                return;
            }
            _fractalEngine.Palette = GeneratePaletteFunction(_paletteManager.ActivePalette);

            var activePalette = _paletteManager.ActivePalette;

            // Устанавливаем MaxColorIterations в зависимости от типа палитры.
            // Для "Стандартного серого" или градиентных палитр используем MaxIterations движка.
            // Для дискретных палитр используем количество цветов в палитре.
            if (activePalette.Name == "Стандартный серый")
            {
                _fractalEngine.MaxColorIterations = Math.Max(1, _fractalEngine.MaxIterations);
            }
            else if (activePalette.IsGradient)
            {
                _fractalEngine.MaxColorIterations = Math.Max(1, _fractalEngine.MaxIterations);
            }
            else // Дискретная палитра (не "Стандартный серый")
            {
                _fractalEngine.MaxColorIterations = Math.Max(1, activePalette.Colors.Count);
            }
        }

        /// <summary>
        /// Генерирует функцию, которая определяет цвет пикселя на основе количества итераций,
        /// максимального количества итераций и максимального количества итераций для раскраски,
        /// используя заданную палитру.
        /// </summary>
        /// <param name="palette">Объект <see cref="PaletteManagerMandelbrotFamily"/>, содержащий информацию о палитре.</param>
        /// <returns>Функция <see cref="Func{T1,T2,T3,TResult}"/>, принимающая количество итераций,
        /// максимальное количество итераций, максимальное количество итераций для раскраски и возвращающая цвет <see cref="Color"/>.</returns>
        private Func<int, int, int, Color> GeneratePaletteFunction(PaletteManagerMandelbrotFamily palette)
        {
            // Специальная обработка для "Стандартного серого" режима раскраски.
            if (palette.Name == "Стандартный серый")
            {
                return (iter, maxIter, maxClrIter) =>
                {
                    if (iter == maxIter) return Color.Black; // Точки внутри множества всегда черные.
                    if (maxClrIter <= 0) return Color.Gray; // Защита от деления на ноль, если MaxIterations вдруг 0.

                    // Логарифмическое масштабирование для более плавного перехода цветов.
                    double tLog = Math.Log(Math.Min(iter, maxClrIter) + 1) / Math.Log(maxClrIter + 1);
                    int cValRaw = (int)(255.0 * (1 - tLog));
                    int cVal = ClampColorComponent(cValRaw);
                    return Color.FromArgb(cVal, cVal, cVal);
                };
            }

            var colors = new List<Color>(palette.Colors);
            bool isGradient = palette.IsGradient;
            int colorCount = colors.Count;

            if (colorCount == 0) return (iter, max, clrMax) => Color.Black; // Если палитра пуста, все черное.
            if (colorCount == 1) return (iter, max, clrMax) => (iter == max) ? Color.Black : colors[0]; // Если один цвет, используем его.

            return (iter, maxIter, maxColorIterationsParam) =>
            {
                if (iter == maxIter) return Color.Black; // Точки внутри множества всегда черные.

                if (isGradient)
                {
                    // Если движок настроен на MaxColorIterations = 1, используем первый цвет палитры.
                    if (maxColorIterationsParam == 1)
                    {
                        return colors[0];
                    }
                    // Нормализуем количество итераций в диапазон [0, 1] для градиента.
                    double t = (double)Math.Min(iter, maxColorIterationsParam - 1) / (maxColorIterationsParam - 1);

                    // Вычисляем индексы двух соседних цветов в палитре и локальный коэффициент интерполяции.
                    double scaledT = t * (colorCount - 1);
                    int index1 = (int)Math.Floor(scaledT);
                    int index2 = Math.Min(index1 + 1, colorCount - 1);
                    double localT = scaledT - index1;

                    // Дополнительная проверка индексов на всякий случай.
                    index1 = Math.Max(0, Math.Min(index1, colorCount - 1));
                    index2 = Math.Max(0, Math.Min(index2, colorCount - 1));

                    return LerpColor(colors[index1], colors[index2], localT);
                }
                else
                {
                    // Для дискретной палитры просто выбираем цвет по остатку от деления.
                    int index = iter % colorCount;
                    return colors[index];
                }
            };
        }

        /// <summary>
        /// Выполняет линейную интерполяцию между двумя цветами.
        /// </summary>
        /// <param name="a">Начальный цвет.</param>
        /// <param name="b">Конечный цвет.</param>
        /// <param name="t">Коэффициент интерполяции, должен быть в диапазоне [0, 1].</param>
        /// <returns>Интерполированный цвет.</returns>
        private Color LerpColor(Color a, Color b, double t)
        {
            t = Math.Max(0, Math.Min(1, t)); // Убеждаемся, что t находится в диапазоне [0, 1].
            return Color.FromArgb(
                ClampColorComponent((int)(a.A + (b.A - a.A) * t)),
                ClampColorComponent((int)(a.R + (b.R - a.R) * t)),
                ClampColorComponent((int)(a.G + (b.G - a.G) * t)),
                ClampColorComponent((int)(a.B + (b.B - a.B) * t))
            );
        }
        #endregion

        #region Phoenix Specific UI
        /// <summary>
        /// Обрабатывает нажатие кнопки "Выбрать параметры Феникса".
        /// Открывает или активирует дополнительную форму <see cref="PhoenixCSelectorForm"/>
        /// для выбора параметров C1 фрактала Феникс.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Данные события.</param>
        private void btnSelectPhoenixParameters_Click(object sender, EventArgs e)
        {
            // Получаем текущие значения C1 и C2 из NumericUpDown на основной форме.
            ComplexDecimal currentC1 = new ComplexDecimal(nudC1Re.Value, nudC1Im.Value);
            ComplexDecimal currentC2 = new ComplexDecimal(nudC2Re.Value, nudC2Im.Value);

            // Создаем новую форму селектора, если она еще не существует или была закрыта.
            if (_phoenixCSelectorWindow == null || _phoenixCSelectorWindow.IsDisposed)
            {
                _phoenixCSelectorWindow = new PhoenixCSelectorForm(this, currentC1, currentC2);
                _phoenixCSelectorWindow.ParametersSelected += (selectedC1, selectedC2) =>
                {
                    // selectedC1.Real - это выбранное P.Re
                    // selectedC1.Imaginary - это выбранное P.Im

                    // Обновляем NumericUpDown на основной форме.
                    // Изменение значения NumericUpDown автоматически вызовет ParamControl_Changed,
                    // который в свою очередь запланирует рендеринг.
                    // Это предотвращает двойной вызов рендеринга.
                    bool c1ReChanged = false;
                    bool c1ImChanged = false;

                    if (nudC1Re.Value != selectedC1.Real)
                    {
                        nudC1Re.Value = selectedC1.Real;
                        c1ReChanged = true;
                    }
                    if (nudC1Im.Value != selectedC1.Imaginary)
                    {
                        nudC1Im.Value = selectedC1.Imaginary;
                        c1ImChanged = true;
                    }

                    // В текущей реализации селектор Phoenix C не изменяет C2.
                    // Если бы это было изменено, код для C2 был бы аналогичен C1:
                    // if (nudC2Re.Value != selectedC2.Real) nudC2Re.Value = selectedC2.Real;
                    // if (nudC2Im.Value != selectedC2.Imaginary) nudC2Im.Value = selectedC2.Imaginary;

                    // Если значения не изменились (что маловероятно, если пользователь что-то выбрал),
                    // ScheduleRender не будет вызван автоматически через ParamControl_Changed.
                    // Однако, в данном случае, если C1 не изменилось, то и ререндер не требуется.
                };
                // Обрабатываем закрытие формы селектора для освобождения ресурсов.
                _phoenixCSelectorWindow.FormClosed += (s, args) =>
                {
                    _phoenixCSelectorWindow.Dispose(); // Убедимся, что ресурсы освобождены.
                    _phoenixCSelectorWindow = null;
                };
                // Показываем форму селектора как немодальное окно, связанное с этой формой.
                _phoenixCSelectorWindow.Show(this);
            }
            else
            {
                // Если окно уже существует, просто активируем его и передаем текущие значения.
                // Передаем только C1, так как C2 не меняется в текущей реализации селектора.
                _phoenixCSelectorWindow.SetSelectedParameters(currentC1);
                _phoenixCSelectorWindow.Activate();
            }
        }
        #endregion

        #region Form Closing
        /// <summary>
        /// Обрабатывает событие закрытия формы.
        /// Отменяет все активные операции рендеринга и освобождает связанные ресурсы.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Данные события закрытия формы.</param>
        private void FractalPhoenixForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            _renderDebounceTimer?.Stop();
            _renderDebounceTimer?.Dispose();
            if (_previewRenderCts != null)
            {
                _previewRenderCts.Cancel();
                try { _previewRenderCts.Dispose(); } catch { /* Игнорируем исключения при Dispose, если CancellationTokenSource уже отменен. */ }
                _previewRenderCts = null;
            }
            lock (_bitmapLock) // Блокируем для безопасного освобождения битмапов.
            {
                _previewBitmap?.Dispose();
                _previewBitmap = null;
                _currentRenderingBitmap?.Dispose();
                _currentRenderingBitmap = null;
            }
            if (_renderVisualizer != null)
            {
                _renderVisualizer.NeedsRedraw -= OnVisualizerNeedsRedraw; // Отписываемся от события.
                _renderVisualizer.Dispose();
                _renderVisualizer = null;
            }
            _colorConfigForm?.Close();
            _colorConfigForm?.Dispose(); // Важно вызвать Dispose для освобождения неуправляемых ресурсов.
            _colorConfigForm = null;
        }
        #endregion

        #region ISaveLoadCapableFractal Implementation
        /// <summary>
        /// Обрабатывает нажатие кнопки "Менеджер состояний".
        /// Открывает диалоговое окно для сохранения и загрузки состояний фрактала.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Данные события.</param>
        private void btnStateManager_Click(object sender, EventArgs e)
        {
            using (var dialog = new SaveLoadDialogForm(this))
            {
                dialog.ShowDialog(this);
            }
        }

        /// <summary>
        /// Получает строковый идентификатор типа фрактала.
        /// </summary>
        /// <value>Идентификатор типа фрактала, используемый для сохранения/загрузки.</value>
        public string FractalTypeIdentifier => "Phoenix";

        /// <summary>
        /// Получает конкретный тип состояния сохранения для данного фрактала.
        /// </summary>
        /// <value>Тип состояния сохранения, который должен использоваться для сериализации/десериализации.</value>
        public Type ConcreteSaveStateType => typeof(PhoenixSaveState);

        /// <summary>
        /// Представляет параметры, используемые для рендеринга миниатюры (предпросмотра) фрактала Феникс.
        /// </summary>
        public class PhoenixPreviewParams
        {
            /// <summary>
            /// Получает или задает координату X центра фрактала.
            /// </summary>
            public decimal CenterX { get; set; }

            /// <summary>
            /// Получает или задает координату Y центра фрактала.
            /// </summary>
            public decimal CenterY { get; set; }

            /// <summary>
            /// Получает или задает уровень масштабирования.
            /// </summary>
            public decimal Zoom { get; set; }

            /// <summary>
            /// Получает или задает количество итераций для рендеринга.
            /// </summary>
            public int Iterations { get; set; }

            /// <summary>
            /// Получает или задает имя используемой палитры.
            /// </summary>
            public string PaletteName { get; set; }

            /// <summary>
            /// Получает или задает значение порога.
            /// </summary>
            public decimal Threshold { get; set; }

            /// <summary>
            /// Получает или задает действительную часть параметра C1.
            /// </summary>
            public decimal C1Re { get; set; }

            /// <summary>
            /// Получает или задает мнимую часть параметра C1.
            /// </summary>
            public decimal C1Im { get; set; }

            /// <summary>
            /// Получает или задает действительную часть параметра C2.
            /// </summary>
            public decimal C2Re { get; set; }

            /// <summary>
            /// Получает или задает мнимую часть параметра C2.
            /// </summary>
            public decimal C2Im { get; set; }
        }

        /// <summary>
        /// Получает текущее состояние фрактала для сохранения.
        /// </summary>
        /// <param name="saveName">Имя, присвоенное сохраняемому состоянию.</param>
        /// <returns>Объект <see cref="FractalSaveStateBase"/>, содержащий текущие параметры фрактала.</returns>
        public FractalSaveStateBase GetCurrentStateForSave(string saveName)
        {
            var state = new PhoenixSaveState(this.FractalTypeIdentifier)
            {
                SaveName = saveName,
                Timestamp = DateTime.Now,
                CenterX = _centerX,
                CenterY = _centerY,
                Zoom = _zoom,
                Threshold = nudThreshold.Value,
                Iterations = (int)nudIterations.Value,
                PaletteName = _paletteManager.ActivePalette?.Name ?? "Стандартный серый", // Сохраняем имя активной палитры.
                C1Re = nudC1Re.Value,
                C1Im = nudC1Im.Value,
                C2Re = nudC2Re.Value,
                C2Im = nudC2Im.Value
            };

            // Создаем укороченные параметры для быстрого рендеринга превью,
            // чтобы предпросмотр загружался быстрее.
            var previewParams = new PhoenixPreviewParams
            {
                CenterX = state.CenterX,
                CenterY = state.CenterY,
                Zoom = state.Zoom,
                Iterations = Math.Min(state.Iterations, 75), // Используем меньше итераций для превью.
                PaletteName = state.PaletteName,
                Threshold = state.Threshold,
                C1Re = state.C1Re,
                C1Im = state.C1Im,
                C2Re = state.C2Re,
                C2Im = state.C2Im
            };

            state.PreviewParametersJson = JsonSerializer.Serialize(previewParams);
            return state;
        }

        /// <summary>
        /// Загружает состояние фрактала из предоставленного объекта состояния.
        /// Обновляет параметры UI и запускает новый рендеринг.
        /// </summary>
        /// <param name="stateBase">Базовый объект состояния фрактала для загрузки.</param>
        public void LoadState(FractalSaveStateBase stateBase)
        {
            if (stateBase is PhoenixSaveState state)
            {
                // Останавливаем текущие рендеры, чтобы избежать конфликтов при загрузке нового состояния.
                _isRenderingPreview = false;
                _previewRenderCts?.Cancel();
                _renderDebounceTimer.Stop();

                // Обновляем внутренние поля фрактала.
                _centerX = state.CenterX;
                _centerY = state.CenterY;
                _zoom = state.Zoom;

                // Обновляем элементы управления UI.
                nudZoom.Value = state.Zoom;
                nudThreshold.Value = state.Threshold;
                nudIterations.Value = state.Iterations;
                nudC1Re.Value = state.C1Re;
                nudC1Im.Value = state.C1Im;
                nudC2Re.Value = state.C2Re;
                nudC2Im.Value = state.C2Im;

                // Загружаем активную палитру по имени.
                var paletteToLoad = _paletteManager.Palettes.FirstOrDefault(p => p.Name == state.PaletteName);
                if (paletteToLoad != null)
                {
                    _paletteManager.ActivePalette = paletteToLoad;
                    ApplyActivePalette(); // Применяем загруженную палитру к движку.
                }

                lock (_bitmapLock) // Блокируем для безопасного обнуления битмапов.
                {
                    _previewBitmap?.Dispose();
                    _previewBitmap = null;
                    _currentRenderingBitmap?.Dispose();
                    _currentRenderingBitmap = null;
                }
                // Обновляем параметры, которые использовались для последнего отрендеренного изображения.
                _renderedCenterX = _centerX;
                _renderedCenterY = _centerY;
                _renderedZoom = _zoom;

                UpdateEngineParameters(); // Обновляем параметры движка.
                ScheduleRender(); // Запускаем рендеринг нового состояния.
            }
            else
            {
                MessageBox.Show("Несовместимый тип состояния для загрузки.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Асинхронно рендерит плитку для предварительного просмотра.
        /// Создает временный движок для рендеринга на основе параметров состояния и возвращает буфер пикселей для запрошенной плитки.
        /// </summary>
        /// <param name="state">Состояние фрактала для рендеринга.</param>
        /// <param name="tile">Информация о запрашиваемой плитке.</param>
        /// <param name="totalWidth">Общая ширина изображения предварительного просмотра.</param>
        /// <param name="totalHeight">Общая высота изображения предварительного просмотра.</param>
        /// <param name="tileSize">Размер одной плитки (для вывода).</param>
        /// <returns>Массив байтов, представляющий данные пикселей для запрошенной плитки.</returns>
        public async Task<byte[]> RenderPreviewTileAsync(FractalSaveStateBase state, TileInfo tile, int totalWidth, int totalHeight, int tileSize)
        {
            // Запускаем рендеринг в фоновом потоке.
            return await Task.Run(() =>
            {
                // Если параметры предпросмотра отсутствуют, возвращаем пустой буфер.
                if (string.IsNullOrEmpty(state.PreviewParametersJson))
                {
                    return new byte[tile.Bounds.Width * tile.Bounds.Height * 4];
                }

                PhoenixPreviewParams previewParams;
                try
                {
                    // Десериализуем параметры предпросмотра.
                    previewParams = JsonSerializer.Deserialize<PhoenixPreviewParams>(state.PreviewParametersJson);
                }
                catch
                {
                    // В случае ошибки десериализации возвращаем пустой буфер.
                    return new byte[tile.Bounds.Width * tile.Bounds.Height * 4];
                }

                var previewEngine = new PhoenixEngine(); // Создаем временный движок для рендеринга.
                previewEngine.CenterX = previewParams.CenterX;
                previewEngine.CenterY = previewParams.CenterY;
                // Масштаб для движка вычисляется из зума, используя BASE_SCALE.
                if (previewParams.Zoom == 0) previewParams.Zoom = 0.001m; // Защита от деления на ноль.
                previewEngine.Scale = 4.0m / previewParams.Zoom;
                previewEngine.MaxIterations = 250; // Используем повышенные итерации для лучшего качества предпросмотра.
                previewEngine.ThresholdSquared = previewParams.Threshold * previewParams.Threshold;
                previewEngine.C1 = new ComplexDecimal(previewParams.C1Re, previewParams.C1Im);
                previewEngine.C2 = new ComplexDecimal(previewParams.C2Re, previewParams.C2Im);

                // Находим палитру по имени или используем первую доступную.
                var paletteForPreview = _paletteManager.Palettes.FirstOrDefault(p => p.Name == previewParams.PaletteName) ?? _paletteManager.Palettes.First();
                previewEngine.Palette = GeneratePaletteFunction(paletteForPreview);

                // Настраиваем MaxColorIterations для движка предпросмотра.
                if (paletteForPreview.Name == "Стандартный серый" || paletteForPreview.IsGradient)
                {
                    previewEngine.MaxColorIterations = Math.Max(1, previewEngine.MaxIterations);
                }
                else
                {
                    previewEngine.MaxColorIterations = Math.Max(1, paletteForPreview.Colors.Count);
                }

                // Рендерим только запрошенную плитку.
                return previewEngine.RenderSingleTile(tile, totalWidth, totalHeight, out _);
            });
        }

        /// <summary>
        /// Рендерит полное изображение предварительного просмотра фрактала на основе заданного состояния.
        /// </summary>
        /// <param name="state">Состояние фрактала, содержащее параметры для рендеринга.</param>
        /// <param name="previewWidth">Желаемая ширина изображения предварительного просмотра.</param>
        /// <param name="previewHeight">Желаемая высота изображения предварительного просмотра.</param>
        /// <returns>Объект <see cref="Bitmap"/>, содержащий отрендеренное изображение предварительного просмотра.</returns>
        public Bitmap RenderPreview(FractalSaveStateBase state, int previewWidth, int previewHeight)
        {
            // Если параметры предпросмотра отсутствуют, возвращаем изображение с сообщением об ошибке.
            if (string.IsNullOrEmpty(state.PreviewParametersJson))
            {
                var bmpError = new Bitmap(previewWidth, previewHeight);
                using (var g = Graphics.FromImage(bmpError))
                {
                    g.Clear(Color.DarkGray);
                    TextRenderer.DrawText(g, "Нет данных", Font, new Rectangle(0, 0, previewWidth, previewHeight), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
                return bmpError;
            }

            PhoenixPreviewParams previewParams;
            try
            {
                // Десериализуем параметры предпросмотра из JSON.
                previewParams = JsonSerializer.Deserialize<PhoenixPreviewParams>(state.PreviewParametersJson);
            }
            catch (Exception)
            {
                // В случае ошибки десериализации возвращаем изображение с сообщением об ошибке.
                var bmpError = new Bitmap(previewWidth, previewHeight);
                using (var g = Graphics.FromImage(bmpError))
                {
                    g.Clear(Color.DarkRed);
                    TextRenderer.DrawText(g, "Ошибка параметров", Font, new Rectangle(0, 0, previewWidth, previewHeight), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
                return bmpError;
            }

            var previewEngine = new PhoenixEngine(); // Создаем новый движок для рендеринга.

            previewEngine.CenterX = previewParams.CenterX;
            previewEngine.CenterY = previewParams.CenterY;
            // Защита от нулевого зума, предотвращающая деление на ноль.
            if (previewParams.Zoom == 0) previewParams.Zoom = 0.001m;
            previewEngine.Scale = 4.0m / previewParams.Zoom; // Для Феникса BASE_SCALE = 4.0.
            previewEngine.MaxIterations = previewParams.Iterations;
            previewEngine.ThresholdSquared = previewParams.Threshold * previewParams.Threshold;
            previewEngine.C1 = new ComplexDecimal(previewParams.C1Re, previewParams.C1Im);
            previewEngine.C2 = new ComplexDecimal(previewParams.C2Re, previewParams.C2Im);

            // Находим палитру по имени или используем первую доступную, если имя палитры не найдено.
            var paletteForPreview = _paletteManager.Palettes.FirstOrDefault(p => p.Name == previewParams.PaletteName)
                                      ?? _paletteManager.Palettes.First();

            previewEngine.Palette = GeneratePaletteFunction(paletteForPreview);
            // Настраиваем MaxColorIterations для движка предпросмотра.
            if (paletteForPreview.Name == "Стандартный серый" || paletteForPreview.IsGradient)
            {
                previewEngine.MaxColorIterations = Math.Max(1, previewParams.Iterations);
            }
            else
            {
                previewEngine.MaxColorIterations = Math.Max(1, paletteForPreview.Colors.Count);
            }

            // Рендерим полное изображение в битмап. Используем 1 поток и пустой обработчик прогресса.
            return previewEngine.RenderToBitmap(previewWidth, previewHeight, 1, progress => { });
        }

        /// <summary>
        /// Загружает все сохраненные состояния фрактала для данного типа.
        /// </summary>
        /// <returns>Список базовых объектов состояний фрактала.</returns>
        public List<FractalSaveStateBase> LoadAllSavesForThisType()
        {
            var specificSaves = SaveFileManager.LoadSaves<PhoenixSaveState>(this.FractalTypeIdentifier);
            // Приводим список к базовому типу для соответствия интерфейсу.
            return specificSaves.Cast<FractalSaveStateBase>().ToList();
        }

        /// <summary>
        /// Сохраняет список состояний фрактала для данного типа.
        /// </summary>
        /// <param name="saves">Список базовых объектов состояний фрактала для сохранения.</param>
        public void SaveAllSavesForThisType(List<FractalSaveStateBase> saves)
        {
            var specificSaves = saves.Cast<PhoenixSaveState>().ToList();
            SaveFileManager.SaveSaves(this.FractalTypeIdentifier, specificSaves);
        }

        #endregion
    }
}