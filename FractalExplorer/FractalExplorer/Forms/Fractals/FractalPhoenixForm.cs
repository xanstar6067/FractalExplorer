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
using System.Diagnostics;
using FractalExplorer.Forms.Other;
using FractalExplorer.Utilities.RenderUtilities;

namespace FractalExplorer.Forms
{
    /// <summary>
    /// Представляет основную форму для отображения и взаимодействия с фракталом Феникс.
    /// Реализует интерфейс <see cref="ISaveLoadCapableFractal"/> для сохранения и загрузки состояний фрактала.
    /// </summary>
    public partial class FractalPhoenixForm : Form, ISaveLoadCapableFractal, IHighResRenderable
    {
        #region Fields
        /// <summary>
        /// Движок для рендеринга фрактала Феникс.
        /// </summary>
        private PhoenixEngine _fractalEngine;
        /// <summary>
        /// Компонент для визуализации процесса рендеринга (например, отображения тайлов).
        /// </summary>
        private RenderVisualizerComponent _renderVisualizer;
        /// <summary>
        /// Менеджер цветовых палитр для фракталов семейства Мандельброта.
        /// </summary>
        private PaletteManager _paletteManager;
        /// <summary>
        /// Форма для настройки параметров цветовой палитры.
        /// </summary>
        private ColorConfigurationForm _colorConfigForm;
        /// <summary>
        /// Форма для выбора C-параметров фрактала Феникс.
        /// </summary>
        private PhoenixCSelectorForm _phoenixCSelectorWindow;

        /// <summary>
        /// Размер тайла для рендеринга в пикселях.
        /// </summary>
        private const int TILE_SIZE = 16;
        /// <summary>
        /// Объект для синхронизации доступа к битмапам из разных потоков.
        /// </summary>
        private readonly object _bitmapLock = new object();
        /// <summary>
        /// Битмап с отрендеренным предпросмотром фрактала.
        /// </summary>
        private Bitmap _previewBitmap;
        /// <summary>
        /// Битмап, на котором происходит текущий рендеринг.
        /// </summary>
        private Bitmap _currentRenderingBitmap;
        /// <summary>
        /// Источник токенов для отмены текущего рендеринга предпросмотра.
        /// </summary>
        private CancellationTokenSource _previewRenderCts;

        /// <summary>
        /// Флаг, указывающий, что в данный момент выполняется рендеринг в высоком разрешении.
        /// </summary>
        private volatile bool _isHighResRendering = false;
        /// <summary>
        /// Флаг, указывающий, что в данный момент выполняется рендеринг предпросмотра.
        /// </summary>
        private volatile bool _isRenderingPreview = false;

        /// <summary>
        /// Текущий уровень масштабирования.
        /// </summary>
        protected decimal _zoom = 1.0m;
        /// <summary>
        /// Координата X центра отображаемой области.
        /// </summary>
        protected decimal _centerX = 0.0m;
        /// <summary>
        /// Координата Y центра отображаемой области.
        /// </summary>
        protected decimal _centerY = 0.0m;

        /// <summary>
        /// Координата X центра на момент последнего завершенного рендеринга.
        /// </summary>
        private decimal _renderedCenterX;
        /// <summary>
        /// Координата Y центра на момент последнего завершенного рендеринга.
        /// </summary>
        private decimal _renderedCenterY;
        /// <summary>
        /// Уровень масштабирования на момент последнего завершенного рендеринга.
        /// </summary>
        private decimal _renderedZoom;

        /// <summary>
        /// Начальная точка для панорамирования.
        /// </summary>
        private Point _panStart;
        /// <summary>
        /// Флаг, указывающий, что выполняется панорамирование.
        /// </summary>
        private bool _panning = false;

        /// <summary>
        /// Таймер для отложенного запуска рендеринга после изменения параметров.
        /// </summary>
        private System.Windows.Forms.Timer _renderDebounceTimer;
        /// <summary>
        /// Базовый заголовок окна.
        /// </summary>
        private string _baseTitle;
        /// <summary>
        /// Базовый масштаб для вычислений.
        /// </summary>
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

        #region UI Initialization
        /// <summary>
        /// Инициализирует значения по умолчанию и ограничения для элементов управления UI.
        /// </summary>
        private void InitializeControls()
        {
            int cores = Environment.ProcessorCount;
            cbThreads.Items.Clear();
            for (int i = 1; i <= cores; i++) cbThreads.Items.Add(i);
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
            _zoom = BASE_SCALE / 4.0m;
            nudZoom.Value = _zoom;

            nudC1Re.Value = 0.56m; nudC1Im.Value = -0.5m;
            nudC2Re.Value = 0.0m; nudC2Im.Value = 0.0m;
            nudC1Re.DecimalPlaces = 15; nudC1Im.DecimalPlaces = 15;
            nudC2Re.DecimalPlaces = 15; nudC2Im.DecimalPlaces = 15;
            nudC1Re.Increment = 0.001m; nudC1Im.Increment = 0.001m;
            nudC2Re.Increment = 0.001m; nudC2Im.Increment = 0.001m;
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
            nudC1Re.ValueChanged += ParamControl_Changed;
            nudC1Im.ValueChanged += ParamControl_Changed;
            nudC2Re.ValueChanged += ParamControl_Changed;
            nudC2Im.ValueChanged += ParamControl_Changed;
            nudIterations.ValueChanged += ParamControl_Changed;
            nudThreshold.ValueChanged += ParamControl_Changed;
            cbThreads.SelectedIndexChanged += ParamControl_Changed;
            nudZoom.ValueChanged += ParamControl_Changed;

            canvas.MouseWheel += Canvas_MouseWheel;
            canvas.MouseDown += Canvas_MouseDown;
            canvas.MouseMove += Canvas_MouseMove;
            canvas.MouseUp += Canvas_MouseUp;
            canvas.Paint += Canvas_Paint;
            canvas.Resize += (s, e) => { if (WindowState != FormWindowState.Minimized) ScheduleRender(); };
        }
        #endregion

        #region UI Event Handlers
        /// <summary>
        /// Обрабатывает изменение значения в элементах управления параметрами фрактала.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Данные события.</param>
        private void ParamControl_Changed(object sender, EventArgs e)
        {
            if (_isHighResRendering) return;
            if (sender == nudZoom && nudZoom.Value != _zoom) _zoom = nudZoom.Value;
            ScheduleRender();
        }

        /// <summary>
        /// Обрабатывает нажатие на кнопку "Рендер", запуская рендеринг предпросмотра.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Данные события.</param>
        private async void btnRender_Click(object sender, EventArgs e)
        {
            _previewRenderCts?.Cancel();
            _renderDebounceTimer.Stop();
            if (_isHighResRendering || _isRenderingPreview) return;
            await StartPreviewRender();
        }

        /// <summary>
        /// Обрабатывает нажатие на кнопку конфигурации цветов, открывая соответствующее окно.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Данные события.</param>
        private void color_configurations_Click(object sender, EventArgs e)
        {
            if (_colorConfigForm == null || _colorConfigForm.IsDisposed)
            {
                _colorConfigForm = new ColorConfigurationForm(_paletteManager);
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
        /// Открывает окно для выбора параметров C для фрактала Феникс.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Данные события.</param>
        private void btnSelectPhoenixParameters_Click(object sender, EventArgs e)
        {
            ComplexDecimal currentC1 = new ComplexDecimal(nudC1Re.Value, nudC1Im.Value);
            ComplexDecimal currentC2 = new ComplexDecimal(nudC2Re.Value, nudC2Im.Value);
            if (_phoenixCSelectorWindow == null || _phoenixCSelectorWindow.IsDisposed)
            {
                _phoenixCSelectorWindow = new PhoenixCSelectorForm(this, currentC1, currentC2);
                _phoenixCSelectorWindow.ParametersSelected += (selectedC1, selectedC2) =>
                {
                    if (nudC1Re.Value != selectedC1.Real) nudC1Re.Value = selectedC1.Real;
                    if (nudC1Im.Value != selectedC1.Imaginary) nudC1Im.Value = selectedC1.Imaginary;
                };
                _phoenixCSelectorWindow.FormClosed += (s, args) => { _phoenixCSelectorWindow = null; };
                _phoenixCSelectorWindow.Show(this);
            }
            else
            {
                _phoenixCSelectorWindow.SetSelectedParameters(currentC1);
                _phoenixCSelectorWindow.Activate();
            }
        }

        /// <summary>
        /// Открывает менеджер сохранения изображений.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Данные события.</param>
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

        #endregion

        #region Canvas Interaction
        /// <summary>
        /// Обрабатывает событие прокрутки колеса мыши для масштабирования.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Данные события мыши.</param>
        private void Canvas_MouseWheel(object sender, MouseEventArgs e)
        {
            if (_isHighResRendering || canvas.Width <= 0 || canvas.Height <= 0) return;
            CommitAndBakePreview();
            decimal zoomFactor = e.Delta > 0 ? 1.5m : 1.0m / 1.5m;
            decimal scaleBeforeZoom = BASE_SCALE / _zoom;
            decimal mouseReal = _centerX + (e.X - canvas.Width / 2.0m) * scaleBeforeZoom / canvas.Width;
            decimal mouseImaginary = _centerY - (e.Y - canvas.Height / 2.0m) * scaleBeforeZoom / canvas.Height;
            _zoom = Math.Max(nudZoom.Minimum, Math.Min(nudZoom.Maximum, _zoom * zoomFactor));
            decimal scaleAfterZoom = BASE_SCALE / _zoom;
            _centerX = mouseReal - (e.X - canvas.Width / 2.0m) * scaleAfterZoom / canvas.Width;
            _centerY = mouseImaginary + (e.Y - canvas.Height / 2.0m) * scaleAfterZoom / canvas.Height;
            canvas.Invalidate();
            if (nudZoom.Value != _zoom) nudZoom.Value = _zoom;
            else ScheduleRender();
        }

        /// <summary>
        /// Обрабатывает нажатие кнопки мыши для начала панорамирования.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Данные события мыши.</param>
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
        /// Обрабатывает движение мыши для выполнения панорамирования.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Данные события мыши.</param>
        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isHighResRendering || !_panning || canvas.Width <= 0) return;
            CommitAndBakePreview();
            decimal unitsPerPixel = BASE_SCALE / _zoom / canvas.Width;
            _centerX -= (decimal)(e.X - _panStart.X) * unitsPerPixel;
            _centerY += (decimal)(e.Y - _panStart.Y) * unitsPerPixel;
            _panStart = e.Location;
            canvas.Invalidate();
            ScheduleRender();
        }

        /// <summary>
        /// Обрабатывает отпускание кнопки мыши для завершения панорамирования.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Данные события мыши.</param>
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
        /// Обрабатывает событие перерисовки холста, отображая фрактал.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Данные события отрисовки.</param>
        private void Canvas_Paint(object sender, PaintEventArgs e)
        {
            if (canvas.Width <= 0 || canvas.Height <= 0) { e.Graphics.Clear(Color.Black); return; }
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
                            decimal renderedComplexWidth = BASE_SCALE / _renderedZoom;
                            decimal currentComplexWidth = BASE_SCALE / _zoom;
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
                                e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Bilinear;
                                PointF destPoint1 = new PointF((float)offsetXPixels, (float)offsetYPixels);
                                PointF destPoint2 = new PointF((float)(offsetXPixels + newWidthPixels), (float)offsetYPixels);
                                PointF destPoint3 = new PointF((float)offsetXPixels, (float)(offsetYPixels + newHeightPixels));
                                e.Graphics.DrawImage(_previewBitmap, new PointF[] { destPoint1, destPoint2, destPoint3 });
                            }
                        }
                        catch (Exception) { }
                    }
                }
                if (_currentRenderingBitmap != null) e.Graphics.DrawImageUnscaled(_currentRenderingBitmap, Point.Empty);
            }
            if (_renderVisualizer != null && _isRenderingPreview) _renderVisualizer.DrawVisualization(e.Graphics);
        }
        #endregion

        #region Rendering Logic
        /// <summary>
        /// Планирует отложенный запуск рендеринга.
        /// </summary>
        private void ScheduleRender()
        {
            if (_isHighResRendering || WindowState == FormWindowState.Minimized) return;
            if (_isRenderingPreview) _previewRenderCts?.Cancel();
            _renderDebounceTimer.Stop();
            _renderDebounceTimer.Start();
        }

        /// <summary>
        /// Запускает асинхронный процесс рендеринга предпросмотра фрактала.
        /// </summary>
        private async Task StartPreviewRender()
        {
            if (canvas.Width <= 0 || canvas.Height <= 0) return;

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
            var renderEngineCopy = new PhoenixEngine
            {
                MaxIterations = _fractalEngine.MaxIterations,
                ThresholdSquared = _fractalEngine.ThresholdSquared,
                CenterX = _fractalEngine.CenterX,
                CenterY = _fractalEngine.CenterY,
                Scale = _fractalEngine.Scale,
                C1 = _fractalEngine.C1,
                C2 = _fractalEngine.C2,
                Palette = _fractalEngine.Palette,
                MaxColorIterations = _fractalEngine.MaxColorIterations
            };

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
        /// Обрабатывает тик таймера отложенного рендеринга.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Данные события.</param>
        private async void RenderDebounceTimer_Tick(object sender, EventArgs e)
        {
            _renderDebounceTimer.Stop();
            if (_isHighResRendering || _isRenderingPreview)
            {
                ScheduleRender();
                return;
            }
            await StartPreviewRender();
        }

        /// <summary>
        /// Вызывается визуализатором рендеринга, когда требуется перерисовка.
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
        /// Завершает текущий рендеринг и "запекает" его результат в основной битмап предпросмотра.
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
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Bilinear;
                    if (_previewBitmap != null)
                    {
                        try
                        {
                            decimal renderedComplexWidth = BASE_SCALE / _renderedZoom;
                            decimal currentComplexWidth = BASE_SCALE / _zoom;
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
                    g.DrawImageUnscaled(_currentRenderingBitmap, Point.Empty);
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

        /// <summary>
        /// Обновляет параметры движка рендеринга на основе значений из элементов управления UI.
        /// </summary>
        private void UpdateEngineParameters()
        {
            _fractalEngine.MaxIterations = (int)nudIterations.Value;
            _fractalEngine.ThresholdSquared = nudThreshold.Value * nudThreshold.Value;
            _fractalEngine.CenterX = _centerX;
            _fractalEngine.CenterY = _centerY;
            _fractalEngine.Scale = BASE_SCALE / _zoom;
            _fractalEngine.C1 = new ComplexDecimal(nudC1Re.Value, nudC1Im.Value);
            _fractalEngine.C2 = new ComplexDecimal(nudC2Re.Value, nudC2Im.Value);
            ApplyActivePalette();
        }

        /// <summary>
        /// Генерирует список тайлов для рендеринга, отсортированных от центра к краям.
        /// </summary>
        /// <param name="width">Ширина области рендеринга.</param>
        /// <param name="height">Высота области рендеринга.</param>
        /// <returns>Список тайлов.</returns>
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
        /// Определяет количество потоков для рендеринга на основе выбора пользователя.
        /// </summary>
        /// <returns>Количество потоков.</returns>
        private int GetThreadCount()
        {
            return cbThreads.SelectedItem?.ToString() == "Auto" ? Environment.ProcessorCount : Convert.ToInt32(cbThreads.SelectedItem);
        }

        #endregion

        #region Palette Management
        /// <summary>
        /// Применяет активную цветовую палитру к движку рендеринга.
        /// </summary>
        private void ApplyActivePalette()
        {
            if (_fractalEngine == null || _paletteManager.ActivePalette == null) return;
            var activePalette = _paletteManager.ActivePalette;
            _fractalEngine.MaxColorIterations = activePalette.AlignWithRenderIterations ? _fractalEngine.MaxIterations : activePalette.MaxColorIterations;
            _fractalEngine.Palette = GeneratePaletteFunction(activePalette);
        }

        /// <summary>
        /// Генерирует функцию окрашивания на основе заданной палитры.
        /// </summary>
        /// <param name="palette">Палитра для создания функции.</param>
        /// <returns>Функция, возвращающая цвет для заданного числа итераций.</returns>
        private Func<int, int, int, Color> GeneratePaletteFunction(Palette palette)
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
        /// Выполняет линейную интерполяцию между двумя цветами.
        /// </summary>
        /// <param name="a">Начальный цвет.</param>
        /// <param name="b">Конечный цвет.</param>
        /// <param name="t">Коэффициент интерполяции (0.0 - 1.0).</param>
        /// <returns>Интерполированный цвет.</returns>
        private Color LerpColor(Color a, Color b, double t)
        {
            t = Math.Max(0, Math.Min(1, t));
            return Color.FromArgb((int)(a.A + (b.A - a.A) * t), (int)(a.R + (b.R - a.R) * t), (int)(a.G + (b.G - a.G) * t), (int)(a.B + (b.B - a.B) * t));
        }

        /// <summary>
        /// Обрабатывает событие применения новой палитры из формы конфигурации.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Данные события.</param>
        private void OnPaletteApplied(object sender, EventArgs e)
        {
            _fractalEngine.MaxIterations = (int)nudIterations.Value;
            ApplyActivePalette();
            ScheduleRender();
        }
        #endregion

        #region Form Lifecycle
        /// <summary>
        /// Обрабатывает событие загрузки формы.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Данные события.</param>
        private void FractalPhoenixForm_Load(object sender, EventArgs e)
        {
            _baseTitle = this.Text;
            _paletteManager = new PaletteManager();
            _fractalEngine = new PhoenixEngine();
            _renderDebounceTimer = new System.Windows.Forms.Timer { Interval = 300 };
            _renderDebounceTimer.Tick += RenderDebounceTimer_Tick;
            _renderVisualizer = new RenderVisualizerComponent(TILE_SIZE);
            _renderVisualizer.NeedsRedraw += OnVisualizerNeedsRedraw;

            InitializeControls();
            InitializeEventHandlers();

            _centerX = 0.0m; _centerY = 0.0m;
            _renderedCenterX = _centerX; _renderedCenterY = _centerY;
            _renderedZoom = _zoom;

            ApplyActivePalette();
            ScheduleRender();
        }

        /// <summary>
        /// Обрабатывает событие закрытия формы, освобождая ресурсы.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Данные события.</param>
        private void FractalPhoenixForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            _renderDebounceTimer?.Stop();
            _renderDebounceTimer?.Dispose();
            _previewRenderCts?.Cancel();
            _previewRenderCts?.Dispose();
            lock (_bitmapLock)
            {
                _previewBitmap?.Dispose(); _previewBitmap = null;
                _currentRenderingBitmap?.Dispose(); _currentRenderingBitmap = null;
            }
            if (_renderVisualizer != null)
            {
                _renderVisualizer.NeedsRedraw -= OnVisualizerNeedsRedraw;
                _renderVisualizer.Dispose();
            }
            _colorConfigForm?.Close();
            _colorConfigForm?.Dispose();
        }
        #endregion

        #region ISaveLoadCapableFractal Implementation

        /// <summary>
        /// Обрабатывает нажатие кнопки, открывая диалог сохранения/загрузки состояний.
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
        /// Уникальный идентификатор типа фрактала.
        /// </summary>
        public string FractalTypeIdentifier => "Phoenix";
        /// <summary>
        /// Тип конкретного класса состояния сохранения для этого фрактала.
        /// </summary>
        public Type ConcreteSaveStateType => typeof(PhoenixSaveState);

        /// <summary>
        /// Класс для хранения параметров предпросмотра фрактала Феникс.
        /// </summary>
        public class PhoenixPreviewParams
        {
            /// <summary>
            /// Координата X центра.
            /// </summary>
            public decimal CenterX { get; set; }
            /// <summary>
            /// Координата Y центра.
            /// </summary>
            public decimal CenterY { get; set; }
            /// <summary>
            /// Уровень масштабирования.
            /// </summary>
            public decimal Zoom { get; set; }
            /// <summary>
            /// Количество итераций.
            /// </summary>
            public int Iterations { get; set; }
            /// <summary>
            /// Имя палитры.
            /// </summary>
            public string PaletteName { get; set; }
            /// <summary>
            /// Пороговое значение.
            /// </summary>
            public decimal Threshold { get; set; }
            /// <summary>
            /// Вещественная часть C1.
            /// </summary>
            public decimal C1Re { get; set; }
            /// <summary>
            /// Мнимая часть C1.
            /// </summary>
            public decimal C1Im { get; set; }
            /// <summary>
            /// Вещественная часть C2.
            /// </summary>
            public decimal C2Re { get; set; }
            /// <summary>
            /// Мнимая часть C2.
            /// </summary>
            public decimal C2Im { get; set; }
        }

        /// <summary>
        /// Собирает текущее состояние фрактала для сохранения.
        /// </summary>
        /// <param name="saveName">Имя сохранения.</param>
        /// <returns>Объект состояния фрактала.</returns>
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
                PaletteName = _paletteManager.ActivePalette?.Name ?? "Стандартный серый",
                C1Re = nudC1Re.Value,
                C1Im = nudC1Im.Value,
                C2Re = nudC2Re.Value,
                C2Im = nudC2Im.Value
            };
            var previewParams = new PhoenixPreviewParams
            {
                CenterX = state.CenterX,
                CenterY = state.CenterY,
                Zoom = state.Zoom,
                Iterations = Math.Min(state.Iterations, 75),
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
        /// Загружает состояние фрактала из объекта сохранения.
        /// </summary>
        /// <param name="stateBase">Объект состояния для загрузки.</param>
        public void LoadState(FractalSaveStateBase stateBase)
        {
            if (stateBase is PhoenixSaveState state)
            {
                _isRenderingPreview = false;
                _previewRenderCts?.Cancel();
                _renderDebounceTimer.Stop();

                _centerX = state.CenterX; _centerY = state.CenterY; _zoom = state.Zoom;
                nudZoom.Value = state.Zoom; nudThreshold.Value = state.Threshold;
                nudIterations.Value = state.Iterations; nudC1Re.Value = state.C1Re;
                nudC1Im.Value = state.C1Im; nudC2Re.Value = state.C2Re; nudC2Im.Value = state.C2Im;

                var paletteToLoad = _paletteManager.Palettes.FirstOrDefault(p => p.Name == state.PaletteName);
                if (paletteToLoad != null) _paletteManager.ActivePalette = paletteToLoad;
                ApplyActivePalette();

                lock (_bitmapLock)
                {
                    _previewBitmap?.Dispose(); _previewBitmap = null;
                    _currentRenderingBitmap?.Dispose(); _currentRenderingBitmap = null;
                }
                _renderedCenterX = _centerX; _renderedCenterY = _centerY; _renderedZoom = _zoom;
                UpdateEngineParameters();
                ScheduleRender();
            }
            else
            {
                MessageBox.Show("Несовместимый тип состояния для загрузки.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Асинхронно рендерит тайл предпросмотра для заданного состояния.
        /// </summary>
        /// <param name="state">Состояние фрактала.</param>
        /// <param name="tile">Информация о тайле.</param>
        /// <param name="totalWidth">Общая ширина предпросмотра.</param>
        /// <param name="totalHeight">Общая высота предпросмотра.</param>
        /// <param name="tileSize">Размер тайла.</param>
        /// <returns>Массив байт с данными пикселей тайла.</returns>
        public async Task<byte[]> RenderPreviewTileAsync(FractalSaveStateBase state, TileInfo tile, int totalWidth, int totalHeight, int tileSize)
        {
            return await Task.Run(() =>
            {
                if (string.IsNullOrEmpty(state.PreviewParametersJson)) return new byte[tile.Bounds.Width * tile.Bounds.Height * 4];
                PhoenixPreviewParams previewParams;
                try { previewParams = JsonSerializer.Deserialize<PhoenixPreviewParams>(state.PreviewParametersJson); }
                catch { return new byte[tile.Bounds.Width * tile.Bounds.Height * 4]; }

                var previewEngine = new PhoenixEngine();
                previewEngine.CenterX = previewParams.CenterX;
                previewEngine.CenterY = previewParams.CenterY;
                if (previewParams.Zoom == 0) previewParams.Zoom = 0.001m;
                previewEngine.Scale = 4.0m / previewParams.Zoom;
                previewEngine.MaxIterations = 250;
                previewEngine.ThresholdSquared = previewParams.Threshold * previewParams.Threshold;
                previewEngine.C1 = new ComplexDecimal(previewParams.C1Re, previewParams.C1Im);
                previewEngine.C2 = new ComplexDecimal(previewParams.C2Re, previewParams.C2Im);

                var paletteForPreview = _paletteManager.Palettes.FirstOrDefault(p => p.Name == previewParams.PaletteName) ?? _paletteManager.Palettes.First();
                previewEngine.Palette = GeneratePaletteFunction(paletteForPreview);
                previewEngine.MaxColorIterations = paletteForPreview.AlignWithRenderIterations ? previewEngine.MaxIterations : paletteForPreview.MaxColorIterations;
                return previewEngine.RenderSingleTile(tile, totalWidth, totalHeight, out _);
            });
        }

        /// <summary>
        /// Рендерит полное изображение предпросмотра для заданного состояния.
        /// </summary>
        /// <param name="state">Состояние фрактала.</param>
        /// <param name="previewWidth">Ширина предпросмотра.</param>
        /// <param name="previewHeight">Высота предпросмотра.</param>
        /// <returns>Битмап с изображением предпросмотра.</returns>
        public Bitmap RenderPreview(FractalSaveStateBase state, int previewWidth, int previewHeight)
        {
            if (string.IsNullOrEmpty(state.PreviewParametersJson))
            {
                var bmpError = new Bitmap(previewWidth, previewHeight);
                using (var g = Graphics.FromImage(bmpError)) { g.Clear(Color.DarkGray); TextRenderer.DrawText(g, "Нет данных", Font, new Rectangle(0, 0, previewWidth, previewHeight), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); }
                return bmpError;
            }
            PhoenixPreviewParams previewParams;
            try { previewParams = JsonSerializer.Deserialize<PhoenixPreviewParams>(state.PreviewParametersJson); }
            catch (Exception)
            {
                var bmpError = new Bitmap(previewWidth, previewHeight);
                using (var g = Graphics.FromImage(bmpError)) { g.Clear(Color.DarkRed); TextRenderer.DrawText(g, "Ошибка параметров", Font, new Rectangle(0, 0, previewWidth, previewHeight), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); }
                return bmpError;
            }
            var previewEngine = new PhoenixEngine();
            previewEngine.CenterX = previewParams.CenterX;
            previewEngine.CenterY = previewParams.CenterY;
            if (previewParams.Zoom == 0) previewParams.Zoom = 0.001m;
            previewEngine.Scale = 4.0m / previewParams.Zoom;
            previewEngine.MaxIterations = previewParams.Iterations;
            previewEngine.ThresholdSquared = previewParams.Threshold * previewParams.Threshold;
            previewEngine.C1 = new ComplexDecimal(previewParams.C1Re, previewParams.C1Im);
            previewEngine.C2 = new ComplexDecimal(previewParams.C2Re, previewParams.C2Im);

            var paletteForPreview = _paletteManager.Palettes.FirstOrDefault(p => p.Name == previewParams.PaletteName) ?? _paletteManager.Palettes.First();
            previewEngine.Palette = GeneratePaletteFunction(paletteForPreview);
            previewEngine.MaxColorIterations = paletteForPreview.AlignWithRenderIterations ? previewEngine.MaxIterations : paletteForPreview.MaxColorIterations;
            return previewEngine.RenderToBitmap(previewWidth, previewHeight, 1, progress => { });
        }

        /// <summary>
        /// Загружает все сохранения для данного типа фрактала.
        /// </summary>
        /// <returns>Список объектов состояния.</returns>
        public List<FractalSaveStateBase> LoadAllSavesForThisType()
        {
            var specificSaves = SaveFileManager.LoadSaves<PhoenixSaveState>(this.FractalTypeIdentifier);
            return specificSaves.Cast<FractalSaveStateBase>().ToList();
        }

        /// <summary>
        /// Сохраняет все состояния для данного типа фрактала.
        /// </summary>
        /// <param name="saves">Список объектов состояния для сохранения.</param>
        public void SaveAllSavesForThisType(List<FractalSaveStateBase> saves)
        {
            var specificSaves = saves.Cast<PhoenixSaveState>().ToList();
            SaveFileManager.SaveSaves(this.FractalTypeIdentifier, specificSaves);
        }
        #endregion

        #region IHighResRenderable Implementation

        /// <summary>
        /// Получает текущее состояние для рендеринга в высоком разрешении.
        /// </summary>
        /// <returns>Объект состояния для рендеринга.</returns>
        public HighResRenderState GetRenderState()
        {
            string c1ReStr = nudC1Re.Value.ToString("F6", CultureInfo.InvariantCulture).Replace(".", "_");
            string c1ImStr = nudC1Im.Value.ToString("F6", CultureInfo.InvariantCulture).Replace(".", "_");
            string fileNameDetails = $"phoenix_P{c1ReStr}_Q{c1ImStr}";

            var state = new HighResRenderState
            {
                EngineType = this.FractalTypeIdentifier,
                CenterX = _centerX,
                CenterY = _centerY,
                Zoom = _zoom,
                BaseScale = BASE_SCALE,
                Iterations = (int)nudIterations.Value,
                Threshold = nudThreshold.Value,
                ActivePaletteName = _paletteManager.ActivePalette?.Name ?? "Стандартный серый",
                FileNameDetails = fileNameDetails,
                // Для фрактала Феникс основной параметр C1 сохраняется в поле JuliaC.
                // Параметр C2 считается нулевым, так как он редко используется.
                JuliaC = new ComplexDecimal(nudC1Re.Value, nudC1Im.Value)
            };

            return state;
        }

        /// <summary>
        /// Создает и настраивает экземпляр движка Phoenix на основе состояния рендеринга.
        /// </summary>
        /// <param name="state">Состояние рендеринга.</param>
        /// <param name="forPreview">Если true, настройки будут оптимизированы для быстрого предпросмотра.</param>
        /// <returns>Настроенный экземпляр <see cref="PhoenixEngine"/>.</returns>
        private PhoenixEngine CreateEngineFromState(HighResRenderState state, bool forPreview)
        {
            var engine = new PhoenixEngine();

            if (forPreview)
            {
                engine.MaxIterations = Math.Min(state.Iterations, 150);
            }
            else
            {
                engine.MaxIterations = state.Iterations;
            }

            engine.ThresholdSquared = state.Threshold * state.Threshold;
            engine.CenterX = state.CenterX;
            engine.CenterY = state.CenterY;
            engine.Scale = state.BaseScale / state.Zoom;
            engine.C1 = state.JuliaC.Value; // C1 берем из JuliaC
            engine.C2 = ComplexDecimal.Zero; // C2 предполагаем нулевым для рендера

            var paletteForRender = _paletteManager.Palettes.FirstOrDefault(p => p.Name == state.ActivePaletteName) ?? _paletteManager.Palettes.First();
            engine.MaxColorIterations = paletteForRender.AlignWithRenderIterations ? engine.MaxIterations : paletteForRender.MaxColorIterations;
            engine.Palette = GeneratePaletteFunction(paletteForRender);

            return engine;
        }

        /// <summary>
        /// Асинхронно рендерит изображение в высоком разрешении.
        /// </summary>
        /// <param name="state">Состояние рендеринга.</param>
        /// <param name="width">Ширина изображения.</param>
        /// <param name="height">Высота изображения.</param>
        /// <param name="ssaaFactor">Фактор суперсэмплинга.</param>
        /// <param name="progress">Объект для отслеживания прогресса.</param>
        /// <param name="cancellationToken">Токен для отмены операции.</param>
        /// <returns>Битмап с отрендеренным изображением.</returns>
        public async Task<Bitmap> RenderHighResolutionAsync(HighResRenderState state, int width, int height, int ssaaFactor, IProgress<RenderProgress> progress, CancellationToken cancellationToken)
        {
            _isHighResRendering = true;
            try
            {
                PhoenixEngine renderEngine = CreateEngineFromState(state, forPreview: false);
                int threadCount = GetThreadCount();

                Action<int> progressCallback = p => progress.Report(new RenderProgress { Percentage = p, Status = "Рендеринг..." });

                Bitmap highResBitmap = await Task.Run(() => renderEngine.RenderToBitmapSSAA(
                    width, height, threadCount, progressCallback, ssaaFactor, cancellationToken), cancellationToken);

                return highResBitmap;
            }
            finally
            {
                _isHighResRendering = false;
            }
        }

        /// <summary>
        /// Рендерит предпросмотр для окна рендеринга в высоком разрешении.
        /// </summary>
        /// <param name="state">Состояние рендеринга.</param>
        /// <param name="previewWidth">Ширина предпросмотра.</param>
        /// <param name="previewHeight">Высота предпросмотра.</param>
        /// <returns>Битмап с предпросмотром.</returns>
        public Bitmap RenderPreview(HighResRenderState state, int previewWidth, int previewHeight)
        {
            var engine = CreateEngineFromState(state, forPreview: true);
            return engine.RenderToBitmap(previewWidth, previewHeight, 1, _ => { }, CancellationToken.None);
        }
        #endregion
    }
}