using FractalExplorer.Engines;
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
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace FractalDraving
{
    /// <summary>
    /// Базовый абстрактный класс для форм, отображающих фракталы семейства Мандельброта.
    /// Предоставляет общую логику для управления движком рендеринга,
    /// палитрой, масштабированием, панорамированием и сохранением изображений.
    /// Включает исправления для предотвращения сбоев при сворачивании окна.
    /// </summary>
    public abstract partial class FractalMandelbrotFamilyForm : Form, IFractalForm, ISaveLoadCapableFractal, IHighResRenderable
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
        /// Кэш для цветов палитры с уже примененной гамма-коррекцией.
        /// Индекс массива соответствует номеру итерации.
        /// </summary>
        private Color[] _gammaCorrectedPaletteCache;

        /// <summary>
        /// "Подпись" палитры, для которой был сгенерирован кэш. 
        /// Используется для определения необходимости обновления кэша.
        /// </summary>
        private string _paletteCacheSignature;

        /// <summary>
        /// Форма конфигурации палитр, связанная с этой формой, для настройки цветов.
        /// </summary>
        private ColorConfigurationMandelbrotFamilyForm _colorConfigForm;

        /// <summary>
        /// Размер одной плитки (тайла) в пикселях для пошагового рендеринга.
        /// </summary>
        private const int TILE_SIZE = 16;

        /// <summary>
        /// Объект для блокировки доступа к битмапам во время операций рендеринга.
        /// </summary>
        private readonly object _bitmapLock = new object();

        /// <summary>
        /// Битмап, содержащий отрисованное изображение для предпросмотра фрактала.
        /// </summary>
        private Bitmap _previewBitmap;

        /// <summary>
        /// Битмап, в который в текущий момент происходит рендеринг плиток.
        /// </summary>
        private Bitmap _currentRenderingBitmap;

        /// <summary>
        /// Токен отмены для операций рендеринга предпросмотра.
        /// </summary>
        private CancellationTokenSource _previewRenderCts;

        /// <summary>
        /// Флаг, указывающий, выполняется ли сейчас рендеринг в высоком разрешении.
        /// </summary>
        private volatile bool _isHighResRendering = false;

        /// <summary>
        /// Флаг, указывающий, выполняется ли сейчас рендеринг предпросмотра.
        /// </summary>
        private volatile bool _isRenderingPreview = false;

        /// <summary>
        /// Экземпляр движка для рендеринга фрактала.
        /// </summary>
        protected FractalMandelbrotFamilyEngine _fractalEngine;

        /// <summary>
        /// Текущий коэффициент масштабирования фрактала.
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
        /// </summary>
        private decimal _renderedCenterX;

        /// <summary>
        /// Координата Y центра, по которой был отрисован текущий <see cref="_previewBitmap"/>.
        /// </summary>
        private decimal _renderedCenterY;

        /// <summary>
        /// Коэффициент масштабирования, по которому был отрисован текущий <see cref="_previewBitmap"/>.
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
        /// </summary>
        private System.Windows.Forms.Timer _renderDebounceTimer;

        /// <summary>
        /// Базовый заголовок окна для восстановления после отображения времени рендера.
        /// </summary>
        private string _baseTitle;

        #endregion

        #region Constructor

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="FractalMandelbrotFamilyForm"/>.
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
        /// Создает конкретный экземпляр движка фрактала.
        /// </summary>
        protected abstract FractalMandelbrotFamilyEngine CreateEngine();

        /// <summary>
        /// Получает базовый масштаб для фрактала.
        /// </summary>
        protected virtual decimal BaseScale => 3.0m;

        /// <summary>
        /// Получает начальную координату X центра.
        /// </summary>
        protected virtual decimal InitialCenterX => -0.5m;

        /// <summary>
        /// Получает начальную координату Y центра.
        /// </summary>
        protected virtual decimal InitialCenterY => 0.0m;

        /// <summary>
        /// Обновляет специфические параметры движка.
        /// </summary>
        protected virtual void UpdateEngineSpecificParameters() { }

        /// <summary>
        /// Вызывается после завершения инициализации формы.
        /// </summary>
        protected virtual void OnPostInitialize() { }

        /// <summary>
        /// Получает детали для имени файла сохранения.
        /// </summary>
        protected virtual string GetSaveFileNameDetails() => "fractal";

        #endregion

        #region UI Initialization

        /// <summary>
        /// Инициализирует элементы управления формы.
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
        /// Инициализирует обработчики событий.
        /// </summary>
        private void InitializeEventHandlers()
        {
            nudIterations.ValueChanged += ParamControl_Changed;
            nudThreshold.ValueChanged += ParamControl_Changed;
            cbThreads.SelectedIndexChanged += ParamControl_Changed;
            nudZoom.ValueChanged += ParamControl_Changed;
            if (nudRe != null) nudRe.ValueChanged += ParamControl_Changed;
            if (nudIm != null) nudIm.ValueChanged += ParamControl_Changed;

            btnRender.Click += (s, e) => ScheduleRender();
            //btnSaveHighRes.Click += btnSaveHighRes_Click;

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
            FormClosed += FractalMandelbrotFamilyForm_FormClosed;
        }

        #endregion

        #region UI Event Handlers

        /// <summary>
        /// Открывает форму настройки палитры.
        /// </summary>
        private void color_configurations_Click(object sender, EventArgs e)
        {
            if (_colorConfigForm == null || _colorConfigForm.IsDisposed)
            {
                _colorConfigForm = new ColorConfigurationMandelbrotFamilyForm(_paletteManager);
                _colorConfigForm.PaletteApplied += OnPaletteApplied;
                _colorConfigForm.FormClosed += (s, args) => _colorConfigForm = null;
                _colorConfigForm.Show(this);
            }
            else
            {
                _colorConfigForm.Activate();
            }
        }

        /// <summary>
        /// Применяет новую палитру и планирует рендеринг.
        /// </summary>
        private void OnPaletteApplied(object sender, EventArgs e)
        {
            UpdateEngineParameters();
            ScheduleRender();
        }

        /// <summary>
        /// Обрабатывает изменение параметров и планирует рендеринг.
        /// </summary>
        private void ParamControl_Changed(object sender, EventArgs e)
        {
            if (_isHighResRendering) return;
            if (sender == nudZoom && nudZoom.Value != _zoom)
            {
                _zoom = nudZoom.Value;
            }
            ScheduleRender();
        }

        #endregion

        #region Canvas Interaction

        /// <summary>
        /// Обрабатывает масштабирование колесом мыши.
        /// </summary>
        private void Canvas_MouseWheel(object sender, MouseEventArgs e)
        {
            if (_isHighResRendering || canvas.Width <= 0 || canvas.Height <= 0) return;
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
            if (nudZoom.Value != _zoom) nudZoom.Value = _zoom;
            else ScheduleRender();
        }

        /// <summary>
        /// Обрабатывает начало панорамирования.
        /// </summary>
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

        /// <summary>
        /// Обрабатывает панорамирование.
        /// </summary>
        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isHighResRendering || !_panning || canvas.Width <= 0) return;
            CommitAndBakePreview();
            decimal unitsPerPixel = BaseScale / _zoom / canvas.Width;
            _centerX -= (e.X - _panStart.X) * unitsPerPixel;
            _centerY += (e.Y - _panStart.Y) * unitsPerPixel;
            _panStart = e.Location;
            canvas.Invalidate();
            ScheduleRender();
        }

        /// <summary>
        /// Обрабатывает завершение панорамирования.
        /// </summary>
        private void Canvas_MouseUp(object sender, MouseEventArgs e)
        {
            if (_isHighResRendering) return;
            if (e.Button == MouseButtons.Left)
            {
                _panning = false;
                canvas.Cursor = Cursors.Default;
            }
        }

        /// <summary>
        /// Обрабатывает перерисовку холста.
        /// </summary>
        private void Canvas_Paint(object sender, PaintEventArgs e)
        {
            if (canvas.Width <= 0 || canvas.Height <= 0)
            {
                e.Graphics.Clear(Color.Black);
                return;
            }
            e.Graphics.Clear(Color.Black);
            lock (_bitmapLock)
            {
                if (_previewBitmap != null)
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
                        catch (Exception)
                        {
                            if (_previewBitmap != null) e.Graphics.DrawImageUnscaled(_previewBitmap, Point.Empty);
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
        #endregion

        #region Rendering Logic

        /// <summary>
        /// Асинхронно запускает рендеринг предпросмотра с SSAA.
        /// </summary>
        private async Task StartPreviewRenderSSAA(int ssaaFactor)
        {
            if (canvas.Width <= 0 || canvas.Height <= 0) return;

            // ИСПРАВЛЕНИЕ: Замораживаем размеры в самом начале.
            int currentWidth = canvas.Width;
            int currentHeight = canvas.Height;

            var stopwatch = Stopwatch.StartNew();
            _isRenderingPreview = true;
            _previewRenderCts?.Cancel();
            _previewRenderCts = new CancellationTokenSource();
            var token = _previewRenderCts.Token;
            _renderVisualizer?.NotifyRenderSessionStart();

            var newRenderingBitmap = new Bitmap(currentWidth, currentHeight, PixelFormat.Format32bppArgb);
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

            var tiles = GenerateTiles(currentWidth, currentHeight);
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

                    // ИСПРАВЛЕНИЕ: Передаем в движок "замороженные" размеры.
                    var tileBuffer = renderEngineCopy.RenderSingleTileSSAA(tile, currentWidth, currentHeight, ssaaFactor, out int bytesPerPixel);

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
                if (canvas.IsHandleCreated && !canvas.IsDisposed) canvas.Invalidate();
            }
            catch (OperationCanceledException)
            {
                lock (_bitmapLock) { if (_currentRenderingBitmap == newRenderingBitmap) { _currentRenderingBitmap?.Dispose(); _currentRenderingBitmap = null; } }
                newRenderingBitmap?.Dispose();
            }
            catch (Exception ex)
            {
                newRenderingBitmap?.Dispose();
                if (IsHandleCreated && !IsDisposed) MessageBox.Show($"Ошибка рендеринга SSAA: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _isRenderingPreview = false;
                _renderVisualizer?.NotifyRenderSessionComplete();
                if (pbRenderProgress.IsHandleCreated && !pbRenderProgress.IsDisposed) pbRenderProgress.Invoke((Action)(() => pbRenderProgress.Value = 0));
            }
        }

        /// <summary>
        /// Асинхронно запускает рендеринг предпросмотра.
        /// </summary>
        private async Task StartPreviewRender()
        {
            if (canvas.Width <= 0 || canvas.Height <= 0) return;

            // ИСПРАВЛЕНИЕ: Замораживаем размеры в самом начале.
            int currentWidth = canvas.Width;
            int currentHeight = canvas.Height;

            var stopwatch = Stopwatch.StartNew();
            _isRenderingPreview = true;
            _previewRenderCts?.Cancel();
            _previewRenderCts = new CancellationTokenSource();
            var token = _previewRenderCts.Token;
            _renderVisualizer?.NotifyRenderSessionStart();

            var newRenderingBitmap = new Bitmap(currentWidth, currentHeight, PixelFormat.Format32bppArgb);
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

            var tiles = GenerateTiles(currentWidth, currentHeight);
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

                    // ИСПРАВЛЕНИЕ: Передаем в движок "замороженные" размеры.
                    var tileBuffer = renderEngineCopy.RenderSingleTile(tile, currentWidth, currentHeight, out int bytesPerPixel);

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
                if (canvas.IsHandleCreated && !canvas.IsDisposed) canvas.Invalidate();
            }
            catch (OperationCanceledException)
            {
                lock (_bitmapLock) { if (_currentRenderingBitmap == newRenderingBitmap) { _currentRenderingBitmap?.Dispose(); _currentRenderingBitmap = null; } }
                newRenderingBitmap?.Dispose();
            }
            catch (Exception ex)
            {
                newRenderingBitmap?.Dispose();
                if (IsHandleCreated && !IsDisposed) MessageBox.Show($"Ошибка рендеринга: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _isRenderingPreview = false;
                _renderVisualizer?.NotifyRenderSessionComplete();
                if (pbRenderProgress.IsHandleCreated && !pbRenderProgress.IsDisposed) pbRenderProgress.Invoke((Action)(() => pbRenderProgress.Value = 0));
            }
        }

        /// <summary>
        /// Обработчик таймера задержки рендеринга.
        /// </summary>
        private async void RenderDebounceTimer_Tick(object sender, EventArgs e)
        {
            _renderDebounceTimer.Stop();
            if (_isHighResRendering || _isRenderingPreview)
            {
                ScheduleRender();
                return;
            }
            int ssaaFactor = GetSelectedSsaaFactor();
            this.Text = $"{_baseTitle} - Качество: {ssaaFactor}x";
            if (ssaaFactor > 1) await StartPreviewRenderSSAA(ssaaFactor);
            else await StartPreviewRender();
        }

        /// <summary>
        /// Обработчик запроса на перерисовку от визуализатора.
        /// </summary>
        private void OnVisualizerNeedsRedraw()
        {
            if (canvas.IsHandleCreated && !canvas.IsDisposed)
            {
                canvas.BeginInvoke((Action)(() => canvas.Invalidate()));
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Генерирует список плиток для рендеринга.
        /// </summary>
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

        /// <summary>
        /// Планирует запуск рендеринга с задержкой.
        /// </summary>
        private void ScheduleRender()
        {
            if (_isHighResRendering || WindowState == FormWindowState.Minimized) return;
            if (_isRenderingPreview) _previewRenderCts?.Cancel();
            _renderDebounceTimer.Stop();
            _renderDebounceTimer.Start();
        }

        /// <summary>
        /// "Запекает" текущий рендеринг в битмап превью.
        /// </summary>
        private void CommitAndBakePreview()
        {
            lock (_bitmapLock) { if (!_isRenderingPreview || _currentRenderingBitmap == null) return; }
            _previewRenderCts?.Cancel();
            lock (_bitmapLock)
            {
                if (_currentRenderingBitmap == null || canvas.Width <= 0 || canvas.Height <= 0) return;
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
                    if (_currentRenderingBitmap != null) g.DrawImageUnscaled(_currentRenderingBitmap, Point.Empty);
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

        private void btnOpenSaveManager_Click(object sender, EventArgs e)
        {
            if (_isHighResRendering)
            {
                MessageBox.Show("Процесс рендеринга уже запущен.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 'this' реализует IHighResRenderable, поэтому мы можем передать его в конструктор
            using (var saveManager = new SaveImageManagerForm(this))
            {
                saveManager.ShowDialog(this);
            }
        }

        /// <summary>
        /// Обрабатывает сохранение изображения в высоком разрешении.
        /// </summary>
        /*private async void btnSaveHighRes_Click(object sender, EventArgs e)
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
                    if (_isRenderingPreview) _previewRenderCts?.Cancel();
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
        }*/

        /// <summary>
        /// Обновляет параметры движка из UI.
        /// </summary>
        private void UpdateEngineParameters()
        {
            _fractalEngine.MaxIterations = (int)nudIterations.Value;
            _fractalEngine.ThresholdSquared = nudThreshold.Value * nudThreshold.Value;
            _fractalEngine.CenterX = _centerX;
            _fractalEngine.CenterY = _centerY;
            _fractalEngine.Scale = BaseScale / _zoom;
            UpdateEngineSpecificParameters();
            ApplyActivePalette();
        }

        /// <summary>
        /// Получает выбранный фактор SSAA.
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
        /// Получает выбранное количество потоков.
        /// </summary>
        private int GetThreadCount()
        {
            return cbThreads.SelectedItem?.ToString() == "Auto" ? Environment.ProcessorCount : Convert.ToInt32(cbThreads.SelectedItem);
        }

        /// <summary>
        /// Ограничивает значение decimal.
        /// </summary>
        private decimal ClampDecimal(decimal value, decimal min, decimal max) => Math.Max(min, Math.Min(max, value));

        /// <summary>
        /// Ограничивает значение int.
        /// </summary>
        private int ClampInt(int value, int min, int max) => Math.Max(min, Math.Min(max, value));

        /// <inheritdoc/>
        public double LoupeZoom => nudBaseScale != null ? (double)nudBaseScale.Value : 4.0;

        /// <inheritdoc/>
        public event EventHandler LoupeZoomChanged;

        #endregion

        #region Palette Management

        /// <summary>
        /// Генерирует "подпись" для палитры.
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
        /// Генерирует функцию палитры.
        /// </summary>
        private Func<int, int, int, Color> GeneratePaletteFunction(PaletteManagerMandelbrotFamily palette)
        {
            double gamma = palette.Gamma;
            var colors = new List<Color>(palette.Colors);
            bool isGradient = palette.IsGradient;
            int colorCount = colors.Count;

            if (palette.Name == "Стандартный серый")
            {
                return (iter, maxIter, maxColorIter) =>
                {
                    if (iter == maxIter) return Color.Black;
                    double logMax = Math.Log(maxColorIter + 1);
                    if (logMax == 0) return Color.Black;
                    double tLog = Math.Log(Math.Min(iter, maxColorIter) + 1) / logMax;
                    int cVal = (int)(255.0 * (1 - tLog));
                    return ColorCorrection.ApplyGamma(Color.FromArgb(cVal, cVal, cVal), gamma);
                };
            }

            if (colorCount == 0) return (i, m, mc) => Color.Black;
            if (colorCount == 1) return (iter, max, clrMax) => ColorCorrection.ApplyGamma((iter == max) ? Color.Black : colors[0], gamma);

            return (iter, maxIter, maxColorIter) =>
            {
                if (iter == maxIter) return Color.Black;
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
        /// Выполняет линейную интерполяцию цветов.
        /// </summary>
        private Color LerpColor(Color a, Color b, double t)
        {
            t = Math.Max(0, Math.Min(1, t));
            return Color.FromArgb((int)(a.A + (b.A - a.A) * t), (int)(a.R + (b.R - a.R) * t), (int)(a.G + (b.G - a.G) * t), (int)(a.B + (b.B - a.B) * t));
        }

        /// <summary>
        /// Применяет активную палитру, используя кэш.
        /// </summary>
        private void ApplyActivePalette()
        {
            if (_fractalEngine == null || _paletteManager.ActivePalette == null) return;
            var activePalette = _paletteManager.ActivePalette;
            int effectiveMaxColorIterations = activePalette.AlignWithRenderIterations ? _fractalEngine.MaxIterations : activePalette.MaxColorIterations;
            string newSignature = GeneratePaletteSignature(activePalette, _fractalEngine.MaxIterations);

            if (_gammaCorrectedPaletteCache == null || newSignature != _paletteCacheSignature)
            {
                _paletteCacheSignature = newSignature;
                var paletteGeneratorFunc = GeneratePaletteFunction(activePalette);
                _gammaCorrectedPaletteCache = new Color[effectiveMaxColorIterations + 1];
                for (int i = 0; i <= effectiveMaxColorIterations; i++)
                {
                    _gammaCorrectedPaletteCache[i] = paletteGeneratorFunc(i, _fractalEngine.MaxIterations, effectiveMaxColorIterations);
                }
            }

            _fractalEngine.MaxColorIterations = effectiveMaxColorIterations;
            _fractalEngine.Palette = (iter, maxIter, maxColorIter) =>
            {
                if (iter == maxIter) return Color.Black;
                int index = Math.Min(iter, _gammaCorrectedPaletteCache.Length - 1);
                return _gammaCorrectedPaletteCache[index];
            };
        }

        #endregion

        #region Form Lifecycle

        /// <summary>
        /// Обработчик загрузки формы.
        /// </summary>
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

            var cbSSAA = this.Controls.Find("cbSSAA", true).FirstOrDefault() as ComboBox;
            if (cbSSAA != null)
            {
                cbSSAA.Items.Add("Выкл (1x)");
                cbSSAA.Items.Add("Низкое (2x)");
                cbSSAA.Items.Add("Высокое (4x)");
                cbSSAA.SelectedItem = "Выкл (1x)";
                cbSSAA.SelectedIndexChanged += (s, ev) => ScheduleRender();
            }

            _renderedCenterX = _centerX;
            _renderedCenterY = _centerY;
            _renderedZoom = _zoom;
            OnPostInitialize();
            UpdateEngineParameters();
            ScheduleRender();
        }

        /// <summary>
        /// Обрабатывает закрытие формы, освобождая ресурсы.
        /// </summary>
        private void FractalMandelbrotFamilyForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            _renderDebounceTimer?.Stop();
            _renderDebounceTimer?.Dispose();
            if (_previewRenderCts != null)
            {
                _previewRenderCts.Cancel();
                _previewRenderCts.Dispose();
            }
            lock (_bitmapLock)
            {
                _previewBitmap?.Dispose();
                _previewBitmap = null;
                _currentRenderingBitmap?.Dispose();
                _currentRenderingBitmap = null;
            }
            if (_renderVisualizer != null)
            {
                _renderVisualizer.NeedsRedraw -= OnVisualizerNeedsRedraw;
                _renderVisualizer.Dispose();
            }
        }

        #endregion

        #region ISaveLoadCapableFractal Implementation

        // ... Этот регион остается без изменений, так как он не участвует в рендеринге и не вызывает ошибку.
        // Он содержит логику сохранения/загрузки, которая уже корректна.

        /// <inheritdoc/>
        public abstract string FractalTypeIdentifier { get; }

        /// <inheritdoc/>
        public abstract Type ConcreteSaveStateType { get; }

        /// <summary>
        /// Параметры для рендеринга превью.
        /// </summary>
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
            if (this is FractalJulia || this is FractalJuliaBurningShip) state = new JuliaFamilySaveState(this.FractalTypeIdentifier);
            else state = new MandelbrotFamilySaveState(this.FractalTypeIdentifier);

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
            state.PreviewParametersJson = JsonSerializer.Serialize(previewParams, new JsonSerializerOptions());
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
            if (paletteToLoad != null) _paletteManager.ActivePalette = paletteToLoad;

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
                _previewBitmap?.Dispose(); _previewBitmap = null;
                _currentRenderingBitmap?.Dispose(); _currentRenderingBitmap = null;
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
                if (string.IsNullOrEmpty(stateBase.PreviewParametersJson)) return new byte[tile.Bounds.Width * tile.Bounds.Height * 4];
                PreviewParams previewParams;
                try { previewParams = JsonSerializer.Deserialize<PreviewParams>(stateBase.PreviewParametersJson); }
                catch { return new byte[tile.Bounds.Width * tile.Bounds.Height * 4]; }

                FractalMandelbrotFamilyEngine previewEngine;
                switch (previewParams.PreviewEngineType)
                {
                    case "Mandelbrot": previewEngine = new MandelbrotEngine(); break;
                    case "Julia": previewEngine = new JuliaEngine { C = new ComplexDecimal(previewParams.CRe, previewParams.CIm) }; break;
                    case "MandelbrotBurningShip": previewEngine = new MandelbrotBurningShipEngine(); break;
                    case "JuliaBurningShip": previewEngine = new JuliaBurningShipEngine { C = new ComplexDecimal(previewParams.CRe, previewParams.CIm) }; break;
                    default: return new byte[tile.Bounds.Width * tile.Bounds.Height * 4];
                }

                previewEngine.MaxIterations = 400;
                previewEngine.CenterX = previewParams.CenterX;
                previewEngine.CenterY = previewParams.CenterY;
                if (previewParams.Zoom == 0) previewParams.Zoom = 0.001m;
                previewEngine.Scale = this.BaseScale / previewParams.Zoom;
                previewEngine.ThresholdSquared = previewParams.Threshold * previewParams.Threshold;
                var paletteForPreview = _paletteManager.Palettes.FirstOrDefault(p => p.Name == previewParams.PaletteName) ?? _paletteManager.Palettes.First();
                previewEngine.MaxColorIterations = paletteForPreview.AlignWithRenderIterations ? previewEngine.MaxIterations : paletteForPreview.MaxColorIterations;
                previewEngine.Palette = GeneratePaletteFunction(paletteForPreview);
                return previewEngine.RenderSingleTile(tile, totalWidth, totalHeight, out _);
            });
        }

        public virtual Bitmap RenderPreview(FractalSaveStateBase stateBase, int previewWidth, int previewHeight)
        {
            if (string.IsNullOrEmpty(stateBase.PreviewParametersJson))
            {
                var bmpError = new Bitmap(previewWidth, previewHeight);
                using (var g = Graphics.FromImage(bmpError)) { g.Clear(Color.DarkGray); TextRenderer.DrawText(g, "Нет данных", Font, new Rectangle(0, 0, previewWidth, previewHeight), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); }
                return bmpError;
            }
            PreviewParams previewParams;
            try { previewParams = JsonSerializer.Deserialize<PreviewParams>(stateBase.PreviewParametersJson, new JsonSerializerOptions()); }
            catch (Exception)
            {
                var bmpError = new Bitmap(previewWidth, previewHeight);
                using (var g = Graphics.FromImage(bmpError)) { g.Clear(Color.DarkRed); TextRenderer.DrawText(g, "Ошибка параметров", Font, new Rectangle(0, 0, previewWidth, previewHeight), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); }
                return bmpError;
            }

            FractalMandelbrotFamilyEngine previewEngine;
            switch (previewParams.PreviewEngineType)
            {
                case "Mandelbrot": previewEngine = new MandelbrotEngine(); break;
                case "Julia": previewEngine = new JuliaEngine { C = new ComplexDecimal(previewParams.CRe, previewParams.CIm) }; break;
                case "MandelbrotBurningShip": previewEngine = new MandelbrotBurningShipEngine(); break;
                case "JuliaBurningShip": previewEngine = new JuliaBurningShipEngine { C = new ComplexDecimal(previewParams.CRe, previewParams.CIm) }; break;
                default:
                    var bmpError = new Bitmap(previewWidth, previewHeight);
                    using (var g = Graphics.FromImage(bmpError)) { g.Clear(Color.DarkOrange); TextRenderer.DrawText(g, "Неизв. тип движка", Font, new Rectangle(0, 0, previewWidth, previewHeight), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); }
                    return bmpError;
            }
            previewEngine.CenterX = previewParams.CenterX;
            previewEngine.CenterY = previewParams.CenterY;
            if (previewParams.Zoom == 0) previewParams.Zoom = 0.001m;
            previewEngine.Scale = this.BaseScale / previewParams.Zoom;
            previewEngine.MaxIterations = previewParams.Iterations;
            previewEngine.ThresholdSquared = previewParams.Threshold * previewParams.Threshold;
            var paletteForPreview = _paletteManager.Palettes.FirstOrDefault(p => p.Name == previewParams.PaletteName) ?? _paletteManager.Palettes.First();
            previewEngine.MaxColorIterations = paletteForPreview.AlignWithRenderIterations ? previewEngine.MaxIterations : paletteForPreview.MaxColorIterations;
            previewEngine.Palette = GeneratePaletteFunction(paletteForPreview);
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

        #region IHighResRenderable Implementation

        public HighResRenderState GetRenderState()
        {
            var state = new HighResRenderState
            {
                EngineType = this.FractalTypeIdentifier,
                CenterX = _centerX,
                CenterY = _centerY,
                Zoom = _zoom,
                BaseScale = this.BaseScale,
                Iterations = (int)nudIterations.Value,
                Threshold = nudThreshold.Value,
                ActivePaletteName = _paletteManager.ActivePalette?.Name ?? "Стандартный серый"
            };

            if (this is FractalJulia || this is FractalJuliaBurningShip)
            {
                state.JuliaC = new ComplexDecimal(nudRe.Value, nudIm.Value);
            }

            return state;
        }

        private FractalMandelbrotFamilyEngine CreateEngineFromState(HighResRenderState state)
        {
            FractalMandelbrotFamilyEngine engine;
            switch (state.EngineType)
            {
                case "Mandelbrot": engine = new MandelbrotEngine(); break;
                case "Julia": engine = new JuliaEngine { C = state.JuliaC.Value }; break;
                case "MandelbrotBurningShip": engine = new MandelbrotBurningShipEngine(); break;
                case "JuliaBurningShip": engine = new JuliaBurningShipEngine { C = state.JuliaC.Value }; break;
                default: throw new NotSupportedException($"Тип движка '{state.EngineType}' не поддерживается.");
            }

            engine.MaxIterations = state.Iterations;
            engine.ThresholdSquared = state.Threshold * state.Threshold;
            engine.CenterX = state.CenterX;
            engine.CenterY = state.CenterY;
            engine.Scale = state.BaseScale / state.Zoom;
            var paletteForRender = _paletteManager.Palettes.FirstOrDefault(p => p.Name == state.ActivePaletteName) ?? _paletteManager.Palettes.First();
            engine.MaxColorIterations = paletteForRender.AlignWithRenderIterations ? engine.MaxIterations : paletteForRender.MaxColorIterations;
            engine.Palette = GeneratePaletteFunction(paletteForRender);

            return engine;
        }

        public async Task<Bitmap> RenderHighResolutionAsync(HighResRenderState state, int width, int height, int ssaaFactor, IProgress<RenderProgress> progress, CancellationToken cancellationToken)
        {
            _isHighResRendering = true;
            try
            {
                FractalMandelbrotFamilyEngine renderEngine = CreateEngineFromState(state);
                int threadCount = GetThreadCount();

                Bitmap highResBitmap = await Task.Run(() => renderEngine.RenderToBitmapSSAA(
                    width, height, threadCount,
                    p => progress.Report(new RenderProgress { Percentage = p, Status = "Рендеринг..." }),
                    ssaaFactor,
                    cancellationToken), cancellationToken);

                return highResBitmap;
            }
            finally
            {
                _isHighResRendering = false;
            }
        }

        public Bitmap RenderPreview(HighResRenderState state, int previewWidth, int previewHeight)
        {
            var engine = CreateEngineFromState(state);
            // Используем меньше итераций для быстрого предпросмотра
            engine.MaxIterations = Math.Min(state.Iterations, 150);
            return engine.RenderToBitmap(previewWidth, previewHeight, 1, _ => { }, CancellationToken.None);
        }

        #endregion

    }
}