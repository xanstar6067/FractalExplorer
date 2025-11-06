using FractalExplorer.Engines;
using FractalExplorer.Forms.Other;
using FractalExplorer.Resources;
using FractalExplorer.Utilities;
using FractalExplorer.Utilities.RenderUtilities;
using FractalExplorer.Utilities.SaveIO.ColorPalettes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FractalExplorer.Forms
{
    /// <summary>
    /// Представляет основную форму для отображения и взаимодействия с фракталом Nova.
    /// </summary>
    public partial class FractalNovaForm : Form
    {
        #region Fields
        private FractalNovaEngine _fractalEngine;
        private RenderVisualizerComponent _renderVisualizer;

        // --- Добавлено для управления палитрами ---
        private PaletteManager _paletteManager;
        private Color[] _gammaCorrectedPaletteCache;
        private string _paletteCacheSignature;
        private ColorConfigurationForm _colorConfigForm;
        // --- Конец добавленного кода ---

        private const int TILE_SIZE = 16;
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
        public FractalNovaForm()
        {
            InitializeComponent();
            this.Load += FractalNovaForm_Load;
            this.FormClosed += FractalNovaForm_FormClosed;
        }
        #endregion

        #region Form Lifecycle
        private void FractalNovaForm_Load(object sender, EventArgs e)
        {
            _baseTitle = this.Text;
            _fractalEngine = new FractalNovaEngine();
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

        #region UI Initialization
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

        private void InitializeEventHandlers()
        {
            btnSaveHighRes.Click += btnSaveHighRes_Click;
            btnConfigurePalette.Click += btnConfigurePalette_Click;
            btnRender.Click += (s, e) => ScheduleRender(true);
            btnStateManager.Click += btnStateManager_Click;

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
        private void ParamControl_Changed(object sender, EventArgs e)
        {
            if (_isHighResRendering) return;
            if (sender == nudZoom && nudZoom.Value != _zoom) _zoom = nudZoom.Value;
            ScheduleRender();
        }

        private void btnSaveHighRes_Click(object sender, EventArgs e) { Console.WriteLine("Button 'Save Image' clicked."); }

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

        private void btnStateManager_Click(object sender, EventArgs e) { Console.WriteLine("Button 'State Manager' clicked."); }
        #endregion

        #region Canvas Interaction
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
            if (_isHighResRendering || !_panning || canvas.Width <= 0) return;
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

            await StartPreviewRender(ssaaFactor);
        }

        private async Task StartPreviewRender(int ssaaFactor)
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
            var renderEngineCopy = new FractalNovaEngine
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

                    if (canvas.IsHandleCreated && !canvas.IsDisposed)
                    {
                        canvas.Invoke((Action)(() => {
                            if (!ct.IsCancellationRequested && pbRenderProgress.IsHandleCreated && !pbRenderProgress.IsDisposed)
                            {
                                pbRenderProgress.Value = Math.Min(pbRenderProgress.Maximum, Interlocked.Increment(ref progress));
                            }
                        }));
                    }
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
            catch (OperationCanceledException)
            {
            }
            finally
            {
                _isRenderingPreview = false;
                _renderVisualizer?.NotifyRenderSessionComplete();
                if (pbRenderProgress.IsHandleCreated && !pbRenderProgress.IsDisposed) pbRenderProgress.Invoke((Action)(() => pbRenderProgress.Value = 0));
            }
        }

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
            catch (Exception) { /* Игнорируем ошибки интерполяции */ }
        }

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

        #region Palette Management

        /// <summary>
        /// Генерирует функцию сглаженного окрашивания на основе заданной палитры.
        /// </summary>
        private Func<double, Color> GenerateSmoothPaletteFunction(Palette palette, int effectiveMaxColorIterations)
        {
            double gamma = palette.Gamma;
            var colors = new List<Color>(palette.Colors);
            int colorCount = colors.Count;

            // --- ИЗМЕНЕНО: Логика для серой палитры возвращена к вашему варианту ---
            if (palette.Name == "Стандартный серый")
            {
                return (smoothIter) =>
                {
                    // 1. Точки внутри множества теперь БЕЛЫЕ
                    if (smoothIter >= _fractalEngine.MaxIterations) return Color.White;
                    if (smoothIter < 0) smoothIter = 0;

                    double logMax = Math.Log(_fractalEngine.MaxIterations + 1);
                    if (logMax <= 0) return Color.Black;

                    double tLog = Math.Log(smoothIter + 1) / logMax;

                    // 2. Градиент инвертирован: быстрый выход (далеко) -> черный цвет
                    int grayValue = (int)(255.0 * tLog);
                    grayValue = Math.Max(0, Math.Min(255, grayValue));

                    Color baseColor = Color.FromArgb(grayValue, grayValue, grayValue);
                    return ColorCorrection.ApplyGamma(baseColor, gamma);
                };
            }
            // --- Конец измененного кода ---

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
                // --- ИЗМЕНЕНО: Для серой палитры цвет фрактала должен быть белым, а не браться из кеша ---
                if (activePalette.Name == "Стандартный серый" && iter == maxIter) return Color.White;
                // --- Конец измененного кода ---

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

            // --- ИЗМЕНЕНО: Логика для серой палитры возвращена к вашему варианту ---
            if (palette.Name == "Стандартный серый")
            {
                return (iter, maxIter, maxColorIter) =>
                {
                    if (iter == maxIter) return Color.White; // Точки внутри - белые

                    double logMax = Math.Log(maxColorIter + 1);
                    if (logMax == 0) return Color.Black;

                    double tLog = Math.Log(Math.Min(iter, maxColorIter) + 1) / logMax;

                    int cVal = (int)(255.0 * tLog); // Градиент от черного к белому

                    return ColorCorrection.ApplyGamma(Color.FromArgb(cVal, cVal, cVal), gamma);
                };
            }
            // --- Конец измененного кода ---

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

        private int GetThreadCount()
        {
            return cbThreads.SelectedItem?.ToString() == "Auto" ? Environment.ProcessorCount : Convert.ToInt32(cbThreads.SelectedItem);
        }

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
    }
}