using FractalExplorer.Engines;
using FractalExplorer.Forms.Other;
using FractalExplorer.Resources;
using FractalExplorer.Utilities;
using FractalExplorer.Utilities.RenderUtilities;
using FractalExplorer.Utilities.SaveIO;
using FractalExplorer.Utilities.SaveIO.ColorPalettes;
using FractalExplorer.Utilities.SaveIO.SaveStateImplementations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FractalExplorer.Forms
{
    /// <summary>
    /// Представляет основную форму для отображения и взаимодействия с фракталом Нова.
    /// Реализует интерфейсы <see cref="IHighResRenderable"/> для рендеринга в высоком разрешении 
    /// и <see cref="ISaveLoadCapableFractal"/> для сохранения и загрузки состояний фрактала.
    /// </summary>
    public partial class FractalNovaMandelbrotForm : Form, IHighResRenderable, ISaveLoadCapableFractal
    {
        #region Fields
        /// <summary>
        /// Движок для рендеринга фрактала Нова.
        /// </summary>
        private FractalNovaFamilyEngine _fractalEngine;
        /// <summary>
        /// Компонент для визуализации процесса рендеринга (например, отображения тайлов).
        /// </summary>
        private RenderVisualizerComponent _renderVisualizer;
        /// <summary>
        /// Менеджер цветовых палитр для фракталов.
        /// </summary>
        private PaletteManager _paletteManager;
        /// <summary>
        /// Кэш для цветов палитры с уже примененной гамма-коррекцией (для дискретного режима).
        /// </summary>
        private Color[] _gammaCorrectedPaletteCache;
        /// <summary>
        /// "Подпись" палитры, для которой был сгенерирован кэш. 
        /// </summary>
        private string _paletteCacheSignature;
        /// <summary>
        /// Форма для настройки параметров цветовой палитры.
        /// </summary>
        private ColorConfigurationForm _colorConfigForm;

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
        /// Инициализирует новый экземпляр класса <see cref="FractalNovaMandelbrotForm"/>.
        /// </summary>
        public FractalNovaMandelbrotForm()
        {
            InitializeComponent();
            this.Load += FractalNovaForm_Load;
            this.FormClosed += FractalNovaForm_FormClosed;
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

            cbSSAA.Items.Add("Выкл (1x)");
            cbSSAA.Items.Add("Низкое (2x)");
            cbSSAA.Items.Add("Высокое (4x)");
            cbSSAA.SelectedItem = "Выкл (1x)";

            nudP_Re.Value = 3.0m; nudP_Im.Value = 0.0m;
            nudZ0_Re.Value = 1.0m; nudZ0_Im.Value = 0.0m;
            nudM.Value = 1.0m;

            nudIterations.Value = 100;
            nudThreshold.Value = 10m;
            nudZoom.Value = 1.0m;
        }

        /// <summary>
        /// Инициализирует обработчики событий для различных элементов управления UI.
        /// </summary>
        private void InitializeEventHandlers()
        {
            btnConfigurePalette.Click += btnConfigurePalette_Click;
            btnRender.Click += (s, e) => ScheduleRender(true);

            nudP_Re.ValueChanged += ParamControl_Changed;
            nudP_Im.ValueChanged += ParamControl_Changed;
            nudZ0_Re.ValueChanged += ParamControl_Changed;
            nudZ0_Im.ValueChanged += ParamControl_Changed;
            nudM.ValueChanged += ParamControl_Changed;

            nudIterations.ValueChanged += ParamControl_Changed;
            nudThreshold.ValueChanged += ParamControl_Changed;
            cbThreads.SelectedIndexChanged += ParamControl_Changed;
            nudZoom.ValueChanged += ParamControl_Changed;
            cbSmooth.CheckedChanged += ParamControl_Changed;
            cbSSAA.SelectedIndexChanged += ParamControl_Changed;

            canvas.MouseWheel += Canvas_MouseWheel;
            canvas.MouseDown += Canvas_MouseDown;
            canvas.MouseMove += Canvas_MouseMove;
            canvas.MouseUp += Canvas_MouseUp;
            canvas.Paint += Canvas_Paint;
            canvas.Resize += (s, e) => { if (WindowState != FormWindowState.Minimized) ScheduleRender(); };

            _renderDebounceTimer.Tick += RenderDebounceTimer_Tick;
            _renderVisualizer.NeedsRedraw += OnVisualizerNeedsRedraw;
        }
        #endregion

        #region UI Event Handlers
        /// <summary>
        /// Обрабатывает изменение значения в элементах управления параметрами фрактала.
        /// </summary>
        private void ParamControl_Changed(object sender, EventArgs e)
        {
            if (_isHighResRendering) return;
            if (sender == nudZoom && nudZoom.Value != _zoom) _zoom = nudZoom.Value;
            ScheduleRender();
        }

        /// <summary>
        /// Открывает менеджер сохранения изображений в высоком разрешении.
        /// </summary>
        private void btnSaveHighRes_Click(object sender, EventArgs e)
        {
            if (_isHighResRendering)
            {
                MessageBox.Show("Процесс рендеринга уже запущен.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var saveManager = new SaveImageManagerForm(this))
            {
                saveManager.ShowDialog(this);
            }
        }

        /// <summary>
        /// Обрабатывает нажатие на кнопку конфигурации цветов, открывая соответствующее окно.
        /// </summary>
        private void btnConfigurePalette_Click(object sender, EventArgs e)
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
        /// Обрабатывает нажатие кнопки, открывая диалог сохранения/загрузки состояний.
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
        /// Обрабатывает событие прокрутки колеса мыши для масштабирования.
        /// </summary>
        private void Canvas_MouseWheel(object sender, MouseEventArgs e)
        {
            if (_isHighResRendering || canvas.Width <= 0 || canvas.Height <= 0) return;
            CommitAndBakePreview();
            decimal zoomFactor = e.Delta > 0 ? 1.5m : 1.0m / 1.5m;
            decimal scaleBeforeZoom = BASE_SCALE / _zoom;
            decimal mouseReal = _centerX + (e.X - canvas.Width / 2.0m) * scaleBeforeZoom / canvas.Width;
            decimal mouseImaginary = _centerY - (e.Y - canvas.Height / 2.0m) * scaleBeforeZoom / canvas.Height;
            _zoom = Math.Max((decimal)nudZoom.Minimum, Math.Min((decimal)nudZoom.Maximum, _zoom * zoomFactor));
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
        private void Canvas_Paint(object sender, PaintEventArgs e)
        {
            if (canvas.Width <= 0 || canvas.Height <= 0) { e.Graphics.Clear(Color.Black); return; }

            e.Graphics.Clear(Color.Black);
            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

            lock (_bitmapLock)
            {
                DrawTransformedBitmap(e.Graphics, _previewBitmap, _renderedCenterX, _renderedCenterY, _renderedZoom, _centerX, _centerY, _zoom);

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
        /// Планирует отложенный запуск рендеринга.
        /// </summary>
        /// <param name="force">Если true, рендеринг запускается немедленно, игнорируя таймер.</param>
        private void ScheduleRender(bool force = false)
        {
            if (_isHighResRendering || WindowState == FormWindowState.Minimized) return;
            _previewRenderCts?.Cancel();
            _renderDebounceTimer.Stop();
            if (force)
            {
                RenderDebounceTimer_Tick(null, EventArgs.Empty);
            }
            else
            {
                _renderDebounceTimer.Start();
            }
        }

        /// <summary>
        /// Обрабатывает тик таймера отложенного рендеринга.
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

            if (ssaaFactor > 1)
            {
                await StartPreviewRenderSSAA(ssaaFactor);
            }
            else
            {
                await StartPreviewRender();
            }
        }

        /// <summary>
        /// Асинхронно запускает рендеринг предпросмотра с использованием суперсэмплинга (SSAA).
        /// </summary>
        private async Task StartPreviewRenderSSAA(int ssaaFactor)
        {
            if (canvas.Width <= 0 || canvas.Height <= 0) return;

            var stopwatch = Stopwatch.StartNew();
            _isRenderingPreview = true;
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
            var renderEngineCopy = new NovaMandelbrotEngine
            {
                MaxIterations = _fractalEngine.MaxIterations,
                ThresholdSquared = _fractalEngine.ThresholdSquared,
                CenterX = _fractalEngine.CenterX,
                CenterY = _fractalEngine.CenterY,
                Scale = _fractalEngine.Scale,
                P = _fractalEngine.P,
                Z0 = _fractalEngine.Z0,
                M = _fractalEngine.M,
                UseSmoothColoring = _fractalEngine.UseSmoothColoring,
                Palette = _fractalEngine.Palette,
                SmoothPalette = _fractalEngine.SmoothPalette,
                MaxColorIterations = _fractalEngine.MaxColorIterations
            };

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

                    byte[] tileBuffer = renderEngineCopy.RenderSingleTileSSAA(tile, canvas.Width, canvas.Height, ssaaFactor, out int bytesPerPixel);

                    ct.ThrowIfCancellationRequested();

                    lock (_bitmapLock)
                    {
                        if (ct.IsCancellationRequested || _currentRenderingBitmap != newRenderingBitmap) return;

                        var bmpData = _currentRenderingBitmap.LockBits(tile.Bounds, ImageLockMode.WriteOnly, _currentRenderingBitmap.PixelFormat);
                        int tileWidthInBytes = tile.Bounds.Width * bytesPerPixel;

                        for (int y = 0; y < tile.Bounds.Height; y++)
                        {
                            IntPtr destPtr = IntPtr.Add(bmpData.Scan0, y * bmpData.Stride);
                            int srcOffset = y * tileWidthInBytes;
                            Marshal.Copy(tileBuffer, srcOffset, destPtr, tileWidthInBytes);
                        }

                        _currentRenderingBitmap.UnlockBits(bmpData);
                    }

                    _renderVisualizer?.NotifyTileRenderComplete(tile.Bounds);
                    if (ct.IsCancellationRequested) return;

                    try
                    {
                        if (canvas.IsHandleCreated && !canvas.IsDisposed)
                        {
                            canvas.Invoke((Action)(() =>
                            {
                                if (!ct.IsCancellationRequested && pbRenderProgress.IsHandleCreated && !pbRenderProgress.IsDisposed)
                                {
                                    pbRenderProgress.Value = Math.Min(pbRenderProgress.Maximum, Interlocked.Increment(ref progress));
                                }
                            }));
                        }
                    }
                    catch (ObjectDisposedException) { }

                    await Task.Yield();
                }, token);

                token.ThrowIfCancellationRequested();
                stopwatch.Stop();
                this.Text = $"{_baseTitle} - Рендер (SSAA {ssaaFactor}x): {stopwatch.Elapsed.TotalSeconds:F3} сек.";

                lock (_bitmapLock)
                {
                    if (_currentRenderingBitmap == newRenderingBitmap)
                    {
                        _previewBitmap?.Dispose();
                        _previewBitmap = _currentRenderingBitmap;
                        _currentRenderingBitmap = null;
                        _renderedCenterX = _centerX;
                        _renderedCenterY = _centerY;
                        _renderedZoom = _zoom;
                    }
                    else
                    {
                        newRenderingBitmap?.Dispose();
                    }
                }
                if (canvas.IsHandleCreated && !canvas.IsDisposed) canvas.Invalidate();
            }
            catch (OperationCanceledException) { }
            finally
            {
                _isRenderingPreview = false;
                _renderVisualizer?.NotifyRenderSessionComplete();
                if (pbRenderProgress.IsHandleCreated && !pbRenderProgress.IsDisposed)
                {
                    try
                    {
                        pbRenderProgress.Invoke((Action)(() => pbRenderProgress.Value = 0));
                    }
                    catch (ObjectDisposedException) { }
                }
            }
        }

        /// <summary>
        /// Запускает асинхронный процесс рендеринга предпросмотра фрактала.
        /// </summary>
        private async Task StartPreviewRender()
        {
            if (canvas.Width <= 0 || canvas.Height <= 0) return;

            var stopwatch = Stopwatch.StartNew();
            _isRenderingPreview = true;
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
            var renderEngineCopy = new NovaMandelbrotEngine
            {
                MaxIterations = _fractalEngine.MaxIterations,
                ThresholdSquared = _fractalEngine.ThresholdSquared,
                CenterX = _fractalEngine.CenterX,
                CenterY = _fractalEngine.CenterY,
                Scale = _fractalEngine.Scale,
                P = _fractalEngine.P,
                Z0 = _fractalEngine.Z0,
                M = _fractalEngine.M,
                UseSmoothColoring = _fractalEngine.UseSmoothColoring,
                Palette = _fractalEngine.Palette,
                SmoothPalette = _fractalEngine.SmoothPalette,
                MaxColorIterations = _fractalEngine.MaxColorIterations
            };

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

                    byte[] tileBuffer = renderEngineCopy.RenderSingleTile(tile, canvas.Width, canvas.Height, out int bytesPerPixel);

                    ct.ThrowIfCancellationRequested();
                    lock (_bitmapLock)
                    {
                        if (ct.IsCancellationRequested || _currentRenderingBitmap != newRenderingBitmap) return;
                        var bmpData = _currentRenderingBitmap.LockBits(tile.Bounds, ImageLockMode.WriteOnly, _currentRenderingBitmap.PixelFormat);
                        int tileWidthInBytes = tile.Bounds.Width * bytesPerPixel;
                        for (int y = 0; y < tile.Bounds.Height; y++)
                        {
                            IntPtr destPtr = IntPtr.Add(bmpData.Scan0, y * bmpData.Stride);
                            int srcOffset = y * tileWidthInBytes;
                            Marshal.Copy(tileBuffer, srcOffset, destPtr, tileWidthInBytes);
                        }
                        _currentRenderingBitmap.UnlockBits(bmpData);
                    }

                    _renderVisualizer?.NotifyTileRenderComplete(tile.Bounds);
                    if (ct.IsCancellationRequested) return;

                    try
                    {
                        if (canvas.IsHandleCreated && !canvas.IsDisposed)
                        {
                            canvas.Invoke((Action)(() =>
                            {
                                if (!ct.IsCancellationRequested && pbRenderProgress.IsHandleCreated && !pbRenderProgress.IsDisposed)
                                {
                                    pbRenderProgress.Value = Math.Min(pbRenderProgress.Maximum, Interlocked.Increment(ref progress));
                                }
                            }));
                        }
                    }
                    catch (ObjectDisposedException) { }

                    await Task.Yield();
                }, token);

                token.ThrowIfCancellationRequested();
                stopwatch.Stop();
                this.Text = $"{_baseTitle} - Рендер: {stopwatch.Elapsed.TotalSeconds:F3} сек.";

                lock (_bitmapLock)
                {
                    if (_currentRenderingBitmap == newRenderingBitmap)
                    {
                        _previewBitmap?.Dispose();
                        _previewBitmap = _currentRenderingBitmap;
                        _currentRenderingBitmap = null;
                        _renderedCenterX = _centerX;
                        _renderedCenterY = _centerY;
                        _renderedZoom = _zoom;
                    }
                    else
                    {
                        newRenderingBitmap?.Dispose();
                    }
                }
                if (canvas.IsHandleCreated && !canvas.IsDisposed) canvas.Invalidate();
            }
            catch (OperationCanceledException) { }
            finally
            {
                _isRenderingPreview = false;
                _renderVisualizer?.NotifyRenderSessionComplete();
                if (pbRenderProgress.IsHandleCreated && !pbRenderProgress.IsDisposed)
                {
                    try
                    {
                        pbRenderProgress.Invoke((Action)(() => pbRenderProgress.Value = 0));
                    }
                    catch (ObjectDisposedException) { }
                }
            }
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
            lock (_bitmapLock)
            {
                if (!_isRenderingPreview || _currentRenderingBitmap == null) return;
            }

            _previewRenderCts?.Cancel();

            lock (_bitmapLock)
            {
                if (_currentRenderingBitmap == null || canvas.Width <= 0 || canvas.Height <= 0) return;

                var bakedBitmap = new Bitmap(canvas.Width, canvas.Height, PixelFormat.Format24bppRgb);
                using (var g = Graphics.FromImage(bakedBitmap))
                {
                    g.Clear(Color.Black);
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;

                    if (_previewBitmap != null)
                    {
                        DrawTransformedBitmap(g, _previewBitmap, _renderedCenterX, _renderedCenterY, _renderedZoom, _centerX, _centerY, _zoom);
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
        /// Отрисовывает битмап с учетом панорамирования и масштабирования.
        /// </summary>
        private void DrawTransformedBitmap(Graphics g, Bitmap bmp, decimal srcCenterX, decimal srcCenterY, decimal srcZoom, decimal destCenterX, decimal destCenterY, decimal destZoom)
        {
            if (bmp == null || g == null || srcZoom <= 0 || destZoom <= 0) return;

            try
            {
                decimal renderedScale = BASE_SCALE / srcZoom;
                decimal currentScale = BASE_SCALE / destZoom;
                float drawScaleRatio = (float)(renderedScale / currentScale);

                float newWidth = canvas.Width * drawScaleRatio;
                float newHeight = canvas.Height * drawScaleRatio;

                decimal deltaReal = srcCenterX - destCenterX;
                decimal deltaImaginary = srcCenterY - destCenterY;

                float offsetX = (float)(deltaReal / currentScale * canvas.Width);
                float offsetY = (float)(-deltaImaginary / currentScale * canvas.Width);

                float drawX = (canvas.Width - newWidth) / 2.0f + offsetX;
                float drawY = (canvas.Height - newHeight) / 2.0f + offsetY;

                g.DrawImage(bmp, drawX, drawY, newWidth, newHeight);
            }
            catch (Exception) { }
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
            _fractalEngine.UseSmoothColoring = cbSmooth.Checked;

            _fractalEngine.P = new Resources.ComplexDecimal(nudP_Re.Value, nudP_Im.Value);
            _fractalEngine.Z0 = new Resources.ComplexDecimal(nudZ0_Re.Value, nudZ0_Im.Value);
            _fractalEngine.M = nudM.Value;

            ApplyActivePalette();
        }

        /// <summary>
        /// Генерирует список тайлов для рендеринга, отсортированных от центра к краям.
        /// </summary>
        private List<TileInfo> GenerateTiles(int width, int height)
        {
            var tiles = new List<TileInfo>();
            Point center = new Point(width / 2, height / 2);
            for (int y = 0; y < height; y += TILE_SIZE)
            {
                for (int x = 0; x < width; x += TILE_SIZE)
                {
                    int tileWidth = Math.Min(TILE_SIZE, width - x);
                    int tileHeight = Math.Min(TILE_SIZE, height - y);
                    tiles.Add(new TileInfo(x, y, tileWidth, tileHeight));
                }
            }
            return tiles.OrderBy(t => Math.Pow(t.Center.X - center.X, 2) + Math.Pow(t.Center.Y - center.Y, 2)).ToList();
        }

        /// <summary>
        /// Определяет количество потоков для рендеринга на основе выбора пользователя.
        /// </summary>
        private int GetThreadCount()
        {
            return cbThreads.SelectedItem?.ToString() == "Auto" ? Environment.ProcessorCount : Convert.ToInt32(cbThreads.SelectedItem);
        }

        /// <summary>
        /// Получает выбранный пользователем фактор суперсэмплинга (SSAA) из выпадающего списка.
        /// </summary>
        private int GetSelectedSsaaFactor()
        {
            if (cbSSAA.SelectedItem == null) return 1;
            switch (cbSSAA.SelectedItem.ToString())
            {
                case "Низкое (2x)": return 2;
                case "Высокое (4x)": return 4;
                default: return 1;
            }
        }
        #endregion

        #region Palette Management
        /// <summary>
        /// Генерирует функцию сглаженного окрашивания на основе заданной палитры.
        /// </summary>
        private Func<double, Color> GenerateSmoothPaletteFunction(Palette palette, int effectiveMaxColorIterations)
        {
            double gamma = palette.Gamma;
            var colors = new List<Color>(palette.Colors);
            int colorCount = colors.Count;

            if (palette.Name == "Стандартный серый")
            {
                return (smoothIter) =>
                {
                    if (smoothIter >= _fractalEngine.MaxIterations) return Color.White;
                    if (smoothIter < 0) smoothIter = 0;

                    double logMax = Math.Log(_fractalEngine.MaxIterations + 1);
                    if (logMax <= 0) return Color.Black;

                    double tLog = Math.Log(smoothIter + 1) / logMax;

                    int grayValue = (int)(255.0 * tLog);
                    grayValue = Math.Max(0, Math.Min(255, grayValue));

                    Color baseColor = Color.FromArgb(grayValue, grayValue, grayValue);
                    return ColorCorrection.ApplyGamma(baseColor, gamma);
                };
            }

            if (effectiveMaxColorIterations <= 0)
            {
                return (smoothIter) => Color.Black;
            }

            if (colorCount == 0) return (smoothIter) => Color.Black;
            if (colorCount == 1) return (smoothIter) => (smoothIter >= _fractalEngine.MaxIterations) ? Color.Black : ColorCorrection.ApplyGamma(colors[0], gamma);

            return (smoothIter) =>
            {
                if (smoothIter >= _fractalEngine.MaxIterations) return Color.Black;
                if (smoothIter < 0) smoothIter = 0;

                double cyclicIter = smoothIter % effectiveMaxColorIterations;

                double t = cyclicIter / (double)effectiveMaxColorIterations;
                t = Math.Max(0.0, Math.Min(1.0, t));

                double scaledT = t * (colorCount - 1);
                int index1 = (int)Math.Floor(scaledT);
                int index2 = Math.Min(index1 + 1, colorCount - 1);
                double localT = scaledT - index1;

                Color baseColor = LerpColor(colors[index1], colors[index2], localT);
                return ColorCorrection.ApplyGamma(baseColor, gamma);
            };
        }

        /// <summary>
        /// Генерирует уникальную "подпись" для палитры на основе ее параметров.
        /// </summary>
        private string GeneratePaletteSignature(Palette palette, int maxIterationsForAlignment)
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
        /// Применяет активную цветовую палитру к движку рендеринга.
        /// </summary>
        private void ApplyActivePalette()
        {
            if (_fractalEngine == null || _paletteManager.ActivePalette == null) return;

            var activePalette = _paletteManager.ActivePalette;
            int effectiveMaxColorIterations = activePalette.AlignWithRenderIterations ? _fractalEngine.MaxIterations : activePalette.MaxColorIterations;

            _fractalEngine.MaxColorIterations = effectiveMaxColorIterations;

            _fractalEngine.SmoothPalette = GenerateSmoothPaletteFunction(activePalette, effectiveMaxColorIterations);

            string newSignature = GeneratePaletteSignature(activePalette, _fractalEngine.MaxIterations);
            if (_gammaCorrectedPaletteCache == null || newSignature != _paletteCacheSignature)
            {
                _paletteCacheSignature = newSignature;
                var paletteGeneratorFunc = GenerateDiscretePaletteFunction(activePalette);
                _gammaCorrectedPaletteCache = new Color[effectiveMaxColorIterations + 1];
                for (int i = 0; i <= effectiveMaxColorIterations; i++)
                {
                    _gammaCorrectedPaletteCache[i] = paletteGeneratorFunc(i, _fractalEngine.MaxIterations, effectiveMaxColorIterations);
                }
            }

            _fractalEngine.Palette = (iter, maxIter, maxColorIter) =>
            {
                if (activePalette.Name == "Стандартный серый" && iter == maxIter) return Color.White;

                if (iter == maxIter) return Color.Black;
                int index = Math.Min(iter, _gammaCorrectedPaletteCache.Length - 1);
                return _gammaCorrectedPaletteCache[index];
            };
        }

        /// <summary>
        /// Генерирует функцию окрашивания на основе заданной палитры.
        /// </summary>
        private Func<int, int, int, Color> GenerateDiscretePaletteFunction(Palette palette)
        {
            double gamma = palette.Gamma;
            var colors = new List<Color>(palette.Colors);
            bool isGradient = palette.IsGradient;
            int colorCount = colors.Count;

            if (palette.Name == "Стандартный серый")
            {
                return (iter, maxIter, maxColorIter) =>
                {
                    if (iter == maxIter) return Color.White;

                    double logMax = Math.Log(maxColorIter + 1);
                    if (logMax == 0) return Color.Black;

                    double tLog = Math.Log(Math.Min(iter, maxColorIter) + 1) / logMax;

                    int cVal = (int)(255.0 * tLog);

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
        private Color LerpColor(Color a, Color b, double t)
        {
            t = Math.Max(0, Math.Min(1, t));
            return Color.FromArgb((int)(a.A + (b.A - a.A) * t), (int)(a.R + (b.R - a.R) * t), (int)(a.G + (b.G - a.G) * t), (int)(a.B + (b.B - a.B) * t));
        }

        /// <summary>
        /// Обрабатывает событие применения новой палитры из формы конфигурации.
        /// </summary>
        private void OnPaletteApplied(object sender, EventArgs e)
        {
            UpdateEngineParameters();
            ScheduleRender();
        }
        #endregion

        #region Form Lifecycle
        /// <summary>
        /// Обрабатывает событие загрузки формы.
        /// </summary>
        private void FractalNovaForm_Load(object sender, EventArgs e)
        {
            _baseTitle = this.Text;
            _fractalEngine = new NovaMandelbrotEngine();
            _paletteManager = new PaletteManager();
            _renderDebounceTimer = new System.Windows.Forms.Timer { Interval = 300 };
            _renderVisualizer = new RenderVisualizerComponent(TILE_SIZE);

            InitializeControls();
            InitializeEventHandlers();

            _centerX = 0.0m; _centerY = 0.0m;
            _renderedCenterX = _centerX; _renderedCenterY = _centerY;
            _renderedZoom = _zoom;

            UpdateEngineParameters();
            ScheduleRender();
        }

        /// <summary>
        /// Обрабатывает событие закрытия формы, освобождая ресурсы.
        /// </summary>
        private void FractalNovaForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            _renderDebounceTimer?.Stop();
            _renderDebounceTimer?.Dispose();
            _previewRenderCts?.Cancel();
            _previewRenderCts?.Dispose();
            lock (_bitmapLock)
            {
                _previewBitmap?.Dispose();
                _currentRenderingBitmap?.Dispose();
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

        #region IHighResRenderable Implementation
        /// <summary>
        /// Получает текущее состояние для рендеринга в высоком разрешении.
        /// </summary>
        public HighResRenderState GetRenderState()
        {
            string pReStr = nudP_Re.Value.ToString("F4", CultureInfo.InvariantCulture).Replace(".", "_");
            string pImStr = nudP_Im.Value.ToString("F4", CultureInfo.InvariantCulture).Replace(".", "_");
            string z0ReStr = nudZ0_Re.Value.ToString("F4", CultureInfo.InvariantCulture).Replace(".", "_");
            string z0ImStr = nudZ0_Im.Value.ToString("F4", CultureInfo.InvariantCulture).Replace(".", "_");
            string mStr = nudM.Value.ToString("F3", CultureInfo.InvariantCulture).Replace(".", "_");

            string fileNameDetails = $"NovaMandelbrot_p{pReStr}_{pImStr}_z{z0ReStr}_{z0ImStr}_m{mStr}";

            var state = new HighResRenderState
            {
                EngineType = "NovaMandelbrot",
                CenterX = _centerX,
                CenterY = _centerY,
                Zoom = _zoom,
                BaseScale = BASE_SCALE,
                Iterations = (int)nudIterations.Value,
                Threshold = nudThreshold.Value,
                ActivePaletteName = _paletteManager.ActivePalette?.Name ?? "Стандартный серый",
                FileNameDetails = fileNameDetails,
                UseSmoothColoring = cbSmooth.Checked,
                NovaP = new ComplexDecimal(nudP_Re.Value, nudP_Im.Value),
                NovaZ0 = new ComplexDecimal(nudZ0_Re.Value, nudZ0_Im.Value),
                NovaM = nudM.Value
            };

            return state;
        }

        /// <summary>
        /// Создает и настраивает экземпляр движка Nova на основе состояния рендеринга.
        /// </summary>
        private FractalNovaFamilyEngine CreateEngineFromState(HighResRenderState state, bool forPreview)
        {
            var engine = new NovaMandelbrotEngine();

            engine.MaxIterations = forPreview ? Math.Min(state.Iterations, 150) : state.Iterations;
            engine.ThresholdSquared = state.Threshold * state.Threshold;
            engine.CenterX = state.CenterX;
            engine.CenterY = state.CenterY;
            engine.Scale = state.BaseScale / state.Zoom;
            engine.UseSmoothColoring = state.UseSmoothColoring;

            engine.P = state.NovaP ?? new ComplexDecimal(3, 0);
            engine.Z0 = state.NovaZ0 ?? new ComplexDecimal(1, 0);
            engine.M = state.NovaM ?? 1.0m;

            var paletteForRender = _paletteManager.Palettes.FirstOrDefault(p => p.Name == state.ActivePaletteName) ?? _paletteManager.Palettes.First();
            int effectiveMaxColorIterations = paletteForRender.AlignWithRenderIterations ? engine.MaxIterations : paletteForRender.MaxColorIterations;
            engine.MaxColorIterations = effectiveMaxColorIterations;

            engine.SmoothPalette = GenerateSmoothPaletteFunction(paletteForRender, effectiveMaxColorIterations);
            engine.Palette = GenerateDiscretePaletteFunction(paletteForRender);

            return engine;
        }

        /// <summary>
        /// Асинхронно рендерит изображение в высоком разрешении.
        /// </summary>
        public async Task<Bitmap> RenderHighResolutionAsync(HighResRenderState state, int width, int height, int ssaaFactor, IProgress<RenderProgress> progress, CancellationToken cancellationToken)
        {
            _isHighResRendering = true;
            try
            {
                FractalNovaFamilyEngine renderEngine = CreateEngineFromState(state, forPreview: false);
                int threadCount = GetThreadCount();

                Action<int> progressCallback = p => progress.Report(new RenderProgress { Percentage = p, Status = "Рендеринг..." });

                Bitmap highResBitmap = await renderEngine.RenderToBitmapSSAA(
                    width, height, threadCount, progressCallback, ssaaFactor, cancellationToken);

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
        public Bitmap RenderPreview(HighResRenderState state, int previewWidth, int previewHeight)
        {
            var engine = CreateEngineFromState(state, forPreview: true);
            return engine.RenderToBitmap(previewWidth, previewHeight, 1, _ => { }, CancellationToken.None);
        }
        #endregion

        #region ISaveLoadCapableFractal Implementation
        /// <summary>
        /// Уникальный идентификатор типа фрактала.
        /// </summary>
        public string FractalTypeIdentifier => "NovaMandelbrot";

        /// <summary>
        /// Тип конкретного класса состояния сохранения для этого фрактала.
        /// </summary>
        public Type ConcreteSaveStateType => typeof(NovaMandelbrotSaveState);

        /// <summary>
        /// Класс для хранения параметров предпросмотра фрактала Нова.
        /// </summary>
        public class NovaPreviewParams
        {
            public decimal CenterX { get; set; }
            public decimal CenterY { get; set; }
            public decimal Zoom { get; set; }
            public int Iterations { get; set; }
            public string PaletteName { get; set; }
            public decimal Threshold { get; set; }
            public decimal P_Re { get; set; }
            public decimal P_Im { get; set; }
            public decimal Z0_Re { get; set; }
            public decimal Z0_Im { get; set; }
            public decimal M { get; set; }
            public bool UseSmoothColoring { get; set; }
        }

        /// <summary>
        /// Собирает текущее состояние фрактала для сохранения.
        /// </summary>
        public FractalSaveStateBase GetCurrentStateForSave(string saveName)
        {
            var state = new NovaMandelbrotSaveState(this.FractalTypeIdentifier)
            {
                SaveName = saveName,
                Timestamp = DateTime.Now,
                CenterX = _centerX,
                CenterY = _centerY,
                Zoom = _zoom,
                Threshold = nudThreshold.Value,
                Iterations = (int)nudIterations.Value,
                PaletteName = _paletteManager.ActivePalette?.Name ?? "Стандартный серый",
                P_Re = nudP_Re.Value,
                P_Im = nudP_Im.Value,
                Z0_Re = nudZ0_Re.Value,
                Z0_Im = nudZ0_Im.Value,
                M = nudM.Value
            };

            var previewParams = new NovaPreviewParams
            {
                CenterX = state.CenterX,
                CenterY = state.CenterY,
                Zoom = state.Zoom,
                Iterations = state.Iterations,
                PaletteName = state.PaletteName,
                Threshold = state.Threshold,
                P_Re = state.P_Re,
                P_Im = state.P_Im,
                Z0_Re = state.Z0_Re,
                Z0_Im = state.Z0_Im,
                M = state.M,
                UseSmoothColoring = cbSmooth.Checked
            };
            state.PreviewParametersJson = JsonSerializer.Serialize(previewParams);
            return state;
        }

        /// <summary>
        /// Загружает состояние фрактала из объекта сохранения.
        /// </summary>
        public void LoadState(FractalSaveStateBase stateBase)
        {
            if (stateBase is NovaMandelbrotSaveState state)
            {
                _isRenderingPreview = false;
                _previewRenderCts?.Cancel();
                _renderDebounceTimer.Stop();

                _centerX = state.CenterX;
                _centerY = state.CenterY;
                _zoom = state.Zoom;

                nudZoom.Value = state.Zoom;
                nudThreshold.Value = state.Threshold;
                nudIterations.Value = state.Iterations;
                nudP_Re.Value = state.P_Re;
                nudP_Im.Value = state.P_Im;
                nudZ0_Re.Value = state.Z0_Re;
                nudZ0_Im.Value = state.Z0_Im;
                nudM.Value = state.M;

                var paletteToLoad = _paletteManager.Palettes.FirstOrDefault(p => p.Name == state.PaletteName);
                if (paletteToLoad != null) _paletteManager.ActivePalette = paletteToLoad;

                lock (_bitmapLock)
                {
                    _previewBitmap?.Dispose(); _previewBitmap = null;
                    _currentRenderingBitmap?.Dispose(); _currentRenderingBitmap = null;
                }

                _renderedCenterX = _centerX;
                _renderedCenterY = _centerY;
                _renderedZoom = _zoom;

                UpdateEngineParameters();
                ScheduleRender(true);
            }
            else
            {
                MessageBox.Show("Несовместимый тип состояния для загрузки.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Асинхронно рендерит тайл предпросмотра для заданного состояния.
        /// </summary>
        public async Task<byte[]> RenderPreviewTileAsync(FractalSaveStateBase state, TileInfo tile, int totalWidth, int totalHeight, int tileSize)
        {
            return await Task.Run(() =>
            {
                if (string.IsNullOrEmpty(state.PreviewParametersJson)) return new byte[tile.Bounds.Width * tile.Bounds.Height * 4];
                NovaPreviewParams previewParams;
                try { previewParams = JsonSerializer.Deserialize<NovaPreviewParams>(state.PreviewParametersJson); }
                catch { return new byte[tile.Bounds.Width * tile.Bounds.Height * 4]; }

                var previewEngine = new NovaMandelbrotEngine
                {
                    CenterX = previewParams.CenterX,
                    CenterY = previewParams.CenterY,
                    Scale = BASE_SCALE / (previewParams.Zoom > 0 ? previewParams.Zoom : 0.001m),
                    MaxIterations = previewParams.Iterations,
                    ThresholdSquared = previewParams.Threshold * previewParams.Threshold,
                    P = new ComplexDecimal(previewParams.P_Re, previewParams.P_Im),
                    Z0 = new ComplexDecimal(previewParams.Z0_Re, previewParams.Z0_Im),
                    M = previewParams.M,
                    UseSmoothColoring = previewParams.UseSmoothColoring
                };

                var paletteForPreview = _paletteManager.Palettes.FirstOrDefault(p => p.Name == previewParams.PaletteName) ?? _paletteManager.Palettes.First();
                int effectiveMaxColorIterations = paletteForPreview.AlignWithRenderIterations ? previewEngine.MaxIterations : paletteForPreview.MaxColorIterations;
                previewEngine.MaxColorIterations = effectiveMaxColorIterations;

                if (previewEngine.UseSmoothColoring)
                {
                    previewEngine.SmoothPalette = GenerateSmoothPaletteFunction(paletteForPreview, effectiveMaxColorIterations);
                }
                else
                {
                    previewEngine.Palette = GenerateDiscretePaletteFunction(paletteForPreview);
                }

                return previewEngine.RenderSingleTile(tile, totalWidth, totalHeight, out _);
            });
        }

        /// <summary>
        /// Рендерит полное изображение предпросмотра для заданного состояния.
        /// </summary>
        public Bitmap RenderPreview(FractalSaveStateBase state, int previewWidth, int previewHeight)
        {
            if (string.IsNullOrEmpty(state.PreviewParametersJson))
            {
                var bmpError = new Bitmap(previewWidth, previewHeight);
                using (var g = Graphics.FromImage(bmpError)) { g.Clear(Color.DarkGray); TextRenderer.DrawText(g, "Нет данных", Font, new Rectangle(0, 0, previewWidth, previewHeight), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); }
                return bmpError;
            }
            NovaPreviewParams previewParams;
            try { previewParams = JsonSerializer.Deserialize<NovaPreviewParams>(state.PreviewParametersJson); }
            catch (Exception)
            {
                var bmpError = new Bitmap(previewWidth, previewHeight);
                using (var g = Graphics.FromImage(bmpError)) { g.Clear(Color.DarkRed); TextRenderer.DrawText(g, "Ошибка параметров", Font, new Rectangle(0, 0, previewWidth, previewHeight), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); }
                return bmpError;
            }

            var previewEngine = new NovaMandelbrotEngine
            {
                CenterX = previewParams.CenterX,
                CenterY = previewParams.CenterY,
                Scale = BASE_SCALE / (previewParams.Zoom > 0 ? previewParams.Zoom : 0.001m),
                MaxIterations = previewParams.Iterations,
                ThresholdSquared = previewParams.Threshold * previewParams.Threshold,
                P = new ComplexDecimal(previewParams.P_Re, previewParams.P_Im),
                Z0 = new ComplexDecimal(previewParams.Z0_Re, previewParams.Z0_Im),
                M = previewParams.M,
                UseSmoothColoring = previewParams.UseSmoothColoring
            };

            var paletteForPreview = _paletteManager.Palettes.FirstOrDefault(p => p.Name == previewParams.PaletteName) ?? _paletteManager.Palettes.First();
            int effectiveMaxColorIterations = paletteForPreview.AlignWithRenderIterations ? previewEngine.MaxIterations : paletteForPreview.MaxColorIterations;
            previewEngine.MaxColorIterations = effectiveMaxColorIterations;

            if (previewEngine.UseSmoothColoring)
            {
                previewEngine.SmoothPalette = GenerateSmoothPaletteFunction(paletteForPreview, effectiveMaxColorIterations);
            }
            else
            {
                previewEngine.Palette = GenerateDiscretePaletteFunction(paletteForPreview);
            }

            return previewEngine.RenderToBitmap(previewWidth, previewHeight, 1, progress => { }, CancellationToken.None);
        }

        /// <summary>
        /// Загружает все сохранения для данного типа фрактала.
        /// </summary>
        public List<FractalSaveStateBase> LoadAllSavesForThisType()
        {
            var specificSaves = SaveFileManager.LoadSaves<NovaMandelbrotSaveState>(this.FractalTypeIdentifier);
            return specificSaves.Cast<FractalSaveStateBase>().ToList();
        }

        /// <summary>
        /// Сохраняет все состояния для данного типа фрактала.
        /// </summary>
        public void SaveAllSavesForThisType(List<FractalSaveStateBase> saves)
        {
            var specificSaves = saves.Cast<NovaMandelbrotSaveState>().ToList();
            SaveFileManager.SaveSaves(this.FractalTypeIdentifier, specificSaves);
        }
        #endregion
    }
}