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

        #region UI Initialization & Event Handlers
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

        // ... (Все остальные обработчики событий UI остаются без изменений)
        #region Unchanged UI Event Handlers
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
        #endregion

        #endregion

        #region Rendering Logic
        // ... (Код Rendering Logic: ScheduleRender, StartPreviewRender, CommitAndBakePreview, etc. остается без изменений)
        #region Unchanged Rendering Logic
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
        #endregion

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

        // ... (Код GenerateTiles и GetThreadCount остается без изменений)
        #region Unchanged Helper Methods
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
        #endregion

        #endregion

        #region Palette Management (НОВАЯ, ИСПРАВЛЕННАЯ ЛОГИКА)

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
            // Если в палитре стоит галочка "AlignWithRenderIterations", то для раскраски
            // используется общее количество итераций рендера.
            // В противном случае используется значение, заданное в самой палитре.
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
            double gamma = palette.Gamma;
            var colors = new List<Color>(palette.Colors);
            bool isGradient = palette.IsGradient;
            int colorCount = colors.Count;

            // Специальная обработка для встроенной серой палитры.
            if (palette.Name == "Стандартный серый")
            {
                return (iter, maxIter, maxColorIter) =>
                {
                    if (iter == maxIter) return Color.Black;

                    double tLog = Math.Log(Math.Min(iter, maxColorIter) + 1) / Math.Log(maxColorIter + 1);
                    int cVal = (int)(255.0 * (1 - tLog));

                    Color baseColor = Color.FromArgb(cVal, cVal, cVal);
                    return ColorCorrection.ApplyGamma(baseColor, gamma);
                };
            }

            // Обработка крайних случаев.
            if (colorCount == 0) return (i, m, mc) => Color.Black;
            if (colorCount == 1)
            {
                return (iter, max, clrMax) =>
                {
                    Color baseColor = (iter == max) ? Color.Black : colors[0];
                    return ColorCorrection.ApplyGamma(baseColor, gamma);
                };
            }

            // Основная логика генерации функции палитры.
            return (iter, maxIter, maxColorIter) =>
            {
                if (iter == maxIter) return Color.Black;

                int colorDomainIter = Math.Min(iter, maxColorIter);

                Color baseColor;
                if (isGradient)
                {
                    double t = (double)colorDomainIter / maxColorIter;
                    double scaledT = t * (colorCount - 1);
                    int index1 = (int)Math.Floor(scaledT);
                    int index2 = Math.Min(index1 + 1, colorCount - 1);
                    double localT = scaledT - index1;
                    baseColor = LerpColor(colors[index1], colors[index2], localT);
                }
                else
                {
                    int index = iter % colorCount;
                    baseColor = colors[index];
                }

                // Применяем гамма-коррекцию в самом конце.
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

        // ... (Остальной код формы: Save High-Res, Phoenix Specific UI, Form Closing, ISaveLoadCapableFractal Implementation - без изменений)
        #region Unchanged Code Sections (Save, UI, Closing, Interfaces)
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
                    pnlControls.Enabled = false;
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
                        pnlControls.Enabled = true;
                        if (pbHighResProgress.IsHandleCreated && !pbHighResProgress.IsDisposed)
                        {
                            pbHighResProgress.Invoke((Action)(() => { pbHighResProgress.Visible = false; pbHighResProgress.Value = 0; }));
                        }
                        ScheduleRender();
                    }
                }
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
        private void btnStateManager_Click(object sender, EventArgs e)
        {
            using (var dialog = new SaveLoadDialogForm(this))
            {
                dialog.ShowDialog(this);
            }
        }
        public string FractalTypeIdentifier => "Phoenix";
        public Type ConcreteSaveStateType => typeof(PhoenixSaveState);
        public class PhoenixPreviewParams
        {
            public decimal CenterX { get; set; }
            public decimal CenterY { get; set; }
            public decimal Zoom { get; set; }
            public int Iterations { get; set; }
            public string PaletteName { get; set; }
            public decimal Threshold { get; set; }
            public decimal C1Re { get; set; }
            public decimal C1Im { get; set; }
            public decimal C2Re { get; set; }
            public decimal C2Im { get; set; }
        }
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
        public void LoadState(FractalSaveStateBase stateBase)
        {
            if (stateBase is PhoenixSaveState state)
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
                nudC1Re.Value = state.C1Re;
                nudC1Im.Value = state.C1Im;
                nudC2Re.Value = state.C2Re;
                nudC2Im.Value = state.C2Im;
                var paletteToLoad = _paletteManager.Palettes.FirstOrDefault(p => p.Name == state.PaletteName);
                if (paletteToLoad != null)
                {
                    _paletteManager.ActivePalette = paletteToLoad;
                    ApplyActivePalette();
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
            else
            {
                MessageBox.Show("Несовместимый тип состояния для загрузки.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        public async Task<byte[]> RenderPreviewTileAsync(FractalSaveStateBase state, TileInfo tile, int totalWidth, int totalHeight, int tileSize)
        {
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
        public Bitmap RenderPreview(FractalSaveStateBase state, int previewWidth, int previewHeight)
        {
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
                previewParams = JsonSerializer.Deserialize<PhoenixPreviewParams>(state.PreviewParametersJson);
            }
            catch (Exception)
            {
                var bmpError = new Bitmap(previewWidth, previewHeight);
                using (var g = Graphics.FromImage(bmpError))
                {
                    g.Clear(Color.DarkRed);
                    TextRenderer.DrawText(g, "Ошибка параметров", Font, new Rectangle(0, 0, previewWidth, previewHeight), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
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
        public List<FractalSaveStateBase> LoadAllSavesForThisType()
        {
            var specificSaves = SaveFileManager.LoadSaves<PhoenixSaveState>(this.FractalTypeIdentifier);
            return specificSaves.Cast<FractalSaveStateBase>().ToList();
        }
        public void SaveAllSavesForThisType(List<FractalSaveStateBase> saves)
        {
            var specificSaves = saves.Cast<PhoenixSaveState>().ToList();
            SaveFileManager.SaveSaves(this.FractalTypeIdentifier, specificSaves);
        }
        #endregion
    }
}