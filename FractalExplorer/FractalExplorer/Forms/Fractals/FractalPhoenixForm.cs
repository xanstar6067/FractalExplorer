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

        private const int TILE_SIZE = 16; // ИЗМЕНЕНО: Уменьшил для более плавной отрисовки
        private readonly object _bitmapLock = new object();
        private Bitmap _previewBitmap;
        private Bitmap _currentRenderingBitmap;
        private CancellationTokenSource _previewRenderCts;

        private volatile bool _isHighResRendering = false;
        private volatile bool _isRenderingPreview = false;

        protected decimal _zoom = 1.0m;
        protected decimal _centerX = 0.0m;
        protected decimal _centerY = 0.0m;

        private decimal _renderedCenterX;
        private decimal _renderedCenterY;
        private decimal _renderedZoom;

        private Point _panStart;
        private bool _panning = false;

        private System.Windows.Forms.Timer _renderDebounceTimer;
        private string _baseTitle;
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
            canvas.Resize += (s, e) =>
            {
                if (WindowState != FormWindowState.Minimized)
                {
                    ScheduleRender();
                }
            };
        }
        #endregion

        #region UI Event Handlers
        private void ParamControl_Changed(object sender, EventArgs e)
        {
            if (_isHighResRendering) return;
            if (sender == nudZoom && nudZoom.Value != _zoom)
            {
                _zoom = nudZoom.Value;
            }
            ScheduleRender();
        }

        private async void btnRender_Click(object sender, EventArgs e)
        {
            _previewRenderCts?.Cancel();
            _renderDebounceTimer.Stop();
            if (_isHighResRendering || _isRenderingPreview) return;
            await StartPreviewRender();
        }

        /// <summary>
        /// Обрабатывает нажатие кнопки "Настройки цвета".
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

        private void btnSelectPhoenixParameters_Click(object sender, EventArgs e)
        {
            ComplexDecimal currentC1 = new ComplexDecimal(nudC1Re.Value, nudC1Im.Value);
            ComplexDecimal currentC2 = new ComplexDecimal(nudC2Re.Value, nudC2Im.Value);
            if (_phoenixCSelectorWindow == null || _phoenixCSelectorWindow.IsDisposed)
            {
                _phoenixCSelectorWindow = new PhoenixCSelectorForm(this, currentC1, currentC2);
                _phoenixCSelectorWindow.ParametersSelected += (selectedC1, selectedC2) =>
                {
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
                };
                _phoenixCSelectorWindow.FormClosed += (s, args) =>
                {
                    _phoenixCSelectorWindow.Dispose();
                    _phoenixCSelectorWindow = null;
                };
                _phoenixCSelectorWindow.Show(this);
            }
            else
            {
                _phoenixCSelectorWindow.SetSelectedParameters(currentC1);
                _phoenixCSelectorWindow.Activate();
            }
        }
        #endregion

        #region Canvas Interaction
        private void Canvas_MouseWheel(object sender, MouseEventArgs e)
        {
            if (_isHighResRendering) return;
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
            decimal unitsPerPixel = BASE_SCALE / _zoom / canvas.Width;
            _centerX -= (decimal)(e.X - _panStart.X) * unitsPerPixel;
            _centerY += (decimal)(e.Y - _panStart.Y) * unitsPerPixel;
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
        private void ScheduleRender()
        {
            if (_isHighResRendering || WindowState == FormWindowState.Minimized) return;
            if (_isRenderingPreview) _previewRenderCts?.Cancel();
            _renderDebounceTimer.Stop();
            _renderDebounceTimer.Start();
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
                double elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
                this.Text = $"{_baseTitle} - Время последнего рендера: {elapsedSeconds:F3} сек.";
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
        /// Обрабатывает событие, когда визуализатору рендеринга требуется перерисовка.
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
        /// Обновляет параметры движка рендеринга фрактала, используя текущие значения из элементов управления UI.
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

        private int GetThreadCount()
        {
            return cbThreads.SelectedItem?.ToString() == "Auto" ? Environment.ProcessorCount : Convert.ToInt32(cbThreads.SelectedItem);
        }

        private async void btnSaveHighRes_Click(object sender, EventArgs e)
        {
            if (_isHighResRendering)
            {
                MessageBox.Show("Процесс сохранения уже запущен.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            int saveWidth = (int)nudSaveWidth.Value;
            int saveHeight = (int)nudSaveHeight.Value;
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
                    if (_isRenderingPreview) _previewRenderCts?.Cancel();
                    _isHighResRendering = true;
                    // Здесь предполагается, что pnlControls это панель с элементами управления
                    // Если ее нет, замените на соответствующий контрол или группу контролов
                    // pnlControls.Enabled = false; 
                    pbHighResProgress.Value = 0;
                    pbHighResProgress.Visible = true;
                    try
                    {
                        var renderEngine = new PhoenixEngine();
                        UpdateEngineParameters();
                        renderEngine.MaxIterations = _fractalEngine.MaxIterations;
                        renderEngine.ThresholdSquared = _fractalEngine.ThresholdSquared;
                        renderEngine.CenterX = _fractalEngine.CenterX;
                        renderEngine.CenterY = _fractalEngine.CenterY;
                        renderEngine.Scale = _fractalEngine.Scale;
                        renderEngine.C1 = _fractalEngine.C1;
                        renderEngine.C2 = _fractalEngine.C2;
                        renderEngine.Palette = GeneratePaletteFunction(_paletteManager.ActivePalette);
                        renderEngine.MaxColorIterations = _fractalEngine.MaxColorIterations;
                        int threadCount = GetThreadCount();
                        var stopwatch = Stopwatch.StartNew();
                        Bitmap highResBitmap = await Task.Run(() => renderEngine.RenderToBitmap(
                            saveWidth, saveHeight, threadCount,
                            progress =>
                            {
                                if (pbHighResProgress.IsHandleCreated && !pbHighResProgress.IsDisposed)
                                {
                                    pbHighResProgress.Invoke((Action)(() => pbHighResProgress.Value = Math.Min(pbHighResProgress.Maximum, progress)));
                                }
                            }
                        ));
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
                        // pnlControls.Enabled = true; // Снова предполагается pnlControls
                        if (pbHighResProgress.IsHandleCreated && !pbHighResProgress.IsDisposed)
                        {
                            pbHighResProgress.Invoke((Action)(() => { pbHighResProgress.Visible = false; pbHighResProgress.Value = 0; }));
                        }
                        ScheduleRender();
                    }
                }
            }
        }
        #endregion

        #region Palette Management
        /// <summary>
        /// Применяет активную палитру цветов из менеджера палитр к движку рендеринга.
        /// Настраивает количество итераций для раскраски в зависимости от настроек палитры.
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

            // Генерируем и присваиваем функцию-палитру движку.
            _fractalEngine.Palette = GeneratePaletteFunction(activePalette);
        }

        /// <summary>
        /// Генерирует функцию, которая определяет цвет пикселя на основе количества итераций,
        /// используя заданную палитру и ее параметры (гамма, градиент и т.д.).
        /// </summary>
        /// <param name="palette">Объект палитры, содержащий информацию о цветах и настройках.</param>
        /// <returns>Функция, преобразующая количество итераций в итоговый цвет пикселя.</returns>
        private Func<int, int, int, Color> GeneratePaletteFunction(PaletteManagerMandelbrotFamily palette)
        {
            // Получаем параметры из объекта палитры
            double gamma = palette.Gamma;
            var colors = new List<Color>(palette.Colors);
            bool isGradient = palette.IsGradient;
            int colorCount = colors.Count;

            // Специальная обработка для встроенной серой палитры
            if (palette.Name == "Стандартный серый")
            {
                // ИСПОЛЬЗУЕМ ОРИГИНАЛЬНУЮ, ПРОВЕРЕННУЮ ЛОГИКУ ДЛЯ СЕРОГО
                return (iter, maxIter, maxColorIter) =>
                {
                    if (iter == maxIter) return Color.Black;

                    // Логарифмическое сглаживание для плавного перехода
                    double tLog = Math.Log(Math.Min(iter, maxColorIter) + 1) / Math.Log(maxColorIter + 1);
                    int cVal = (int)(255.0 * (1 - tLog));

                    Color baseColor = Color.FromArgb(cVal, cVal, cVal);
                    // Применяем гамму в конце
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

                // ФИНАЛЬНЫЙ ФИКС: Безопасная нормализация. Значение плавно идет от 0 до 1, а затем остается на 1.
                // Это убирает все "галлюцинации" и "разводы".
                double normalizedIter = (double)Math.Min(iter, maxColorIter) / maxColorIter;

                Color baseColor;

                if (isGradient)
                {
                    // Для градиентов используем плавную интерполяцию
                    double scaledT = normalizedIter * (colorCount - 1);
                    int index1 = (int)Math.Floor(scaledT);
                    int index2 = Math.Min(index1 + 1, colorCount - 1);
                    double localT = scaledT - index1;

                    baseColor = LerpColor(colors[index1], colors[index2], localT);
                }
                else
                {
                    // ФИНАЛЬНЫЙ ФИКС для дискретных цветов
                    int colorIndex = (int)(normalizedIter * colorCount);
                    // Важнейший фикс: если normalizedIter равен 1.0, индекс будет равен colorCount, что вызовет ошибку.
                    // Поэтому мы его ограничиваем. Это исправляет проблему с Ч/Б и другими дискретными палитрами.
                    if (colorIndex >= colorCount)
                    {
                        colorIndex = colorCount - 1;
                    }

                    baseColor = colors[colorIndex];
                }

                // Применяем гамма-коррекцию
                return ColorCorrection.ApplyGamma(baseColor, gamma);
            };
        }

        /// <summary>
        /// Выполняет линейную интерполяцию между двумя цветами на основе коэффициента.
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

        /// <summary>
        /// Обрабатывает событие применения новой палитры цветов.
        /// </summary>
        private void OnPaletteApplied(object sender, EventArgs e)
        {
            // ИСПРАВЛЕНИЕ: Принудительно обновляем итерации движка из UI
            _fractalEngine.MaxIterations = (int)nudIterations.Value;

            // Применяем палитру и запускаем рендер
            ApplyActivePalette();
            ScheduleRender();
        }
        #endregion

        #region Form Lifecycle
        /// <summary>
        /// Обрабатывает событие загрузки формы.
        /// </summary>
        private void FractalPhoenixForm_Load(object sender, EventArgs e)
        {
            _baseTitle = this.Text;
            _paletteManager = new ColorPaletteMandelbrotFamily();
            _fractalEngine = new PhoenixEngine();
            _renderDebounceTimer = new System.Windows.Forms.Timer { Interval = 300 };
            _renderDebounceTimer.Tick += RenderDebounceTimer_Tick;
            _renderVisualizer = new RenderVisualizerComponent(TILE_SIZE);
            _renderVisualizer.NeedsRedraw += OnVisualizerNeedsRedraw;

            InitializeControls();
            InitializeEventHandlers();

            _centerX = 0.0m;
            _centerY = 0.0m;
            _renderedCenterX = _centerX;
            _renderedCenterY = _centerY;
            _renderedZoom = _zoom;

            ApplyActivePalette();
            ScheduleRender();
        }

        private void FractalPhoenixForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            _renderDebounceTimer?.Stop();
            _renderDebounceTimer?.Dispose();
            if (_previewRenderCts != null)
            {
                _previewRenderCts.Cancel();
                try { _previewRenderCts.Dispose(); } catch { }
                _previewRenderCts = null;
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
                _renderVisualizer = null;
            }
            _colorConfigForm?.Close();
            _colorConfigForm?.Dispose();
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
                if (string.IsNullOrEmpty(state.PreviewParametersJson))
                {
                    return new byte[tile.Bounds.Width * tile.Bounds.Height * 4];
                }

                PhoenixPreviewParams previewParams;
                try
                {
                    previewParams = JsonSerializer.Deserialize<PhoenixPreviewParams>(state.PreviewParametersJson);
                }
                catch
                {
                    return new byte[tile.Bounds.Width * tile.Bounds.Height * 4];
                }

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

                // ИСПРАВЛЕНИЕ: Применяем ту же логику настройки палитры, что и в основном рендере.
                if (paletteForPreview.AlignWithRenderIterations)
                {
                    previewEngine.MaxColorIterations = previewEngine.MaxIterations;
                }
                else
                {
                    previewEngine.MaxColorIterations = paletteForPreview.MaxColorIterations;
                }

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
            if (string.IsNullOrEmpty(state.PreviewParametersJson))
            {
                var bmpError = new Bitmap(previewWidth, previewHeight);
                using (var g = Graphics.FromImage(bmpError)) { g.Clear(Color.DarkGray); TextRenderer.DrawText(g, "Нет данных", Font, new Rectangle(0, 0, previewWidth, previewHeight), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); }
                return bmpError;
            }

            PhoenixPreviewParams previewParams;
            try
            {
                previewParams = JsonSerializer.Deserialize<PhoenixPreviewParams>(state.PreviewParametersJson);
            }
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

            // ИСПРАВЛЕНИЕ: Применяем ту же логику настройки палитры, что и в основном рендере.
            if (paletteForPreview.AlignWithRenderIterations)
            {
                previewEngine.MaxColorIterations = previewEngine.MaxIterations;
            }
            else
            {
                previewEngine.MaxColorIterations = paletteForPreview.MaxColorIterations;
            }

            return previewEngine.RenderToBitmap(previewWidth, previewHeight, 1, progress => { });
        }

        /// <summary>
        /// Загружает все сохраненные состояния фрактала для данного типа.
        /// </summary>
        /// <returns>Список базовых объектов состояний фрактала.</returns>
        public List<FractalSaveStateBase> LoadAllSavesForThisType()
        {
            var specificSaves = SaveFileManager.LoadSaves<PhoenixSaveState>(this.FractalTypeIdentifier);
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