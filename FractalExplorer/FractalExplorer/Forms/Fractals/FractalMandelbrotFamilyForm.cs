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
using ExtendedNumerics;

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
        #region Fields (без изменений)

        private RenderVisualizerComponent _renderVisualizer;
        private ColorPaletteMandelbrotFamily _paletteManager;
        private Color[] _gammaCorrectedPaletteCache;
        private string _paletteCacheSignature;
        private ColorConfigurationMandelbrotFamilyForm _colorConfigForm;
        private const int TILE_SIZE = 16;
        private readonly object _bitmapLock = new object();
        private Bitmap _previewBitmap;
        private Bitmap _currentRenderingBitmap;
        private CancellationTokenSource _previewRenderCts;
        private volatile bool _isHighResRendering = false;
        private volatile bool _isRenderingPreview = false;
        protected FractalMandelbrotFamilyEngine _fractalEngine;
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

        #endregion

        #region Constructor & Abstract/Virtual Methods (без изменений)

        protected FractalMandelbrotFamilyForm()
        {
            InitializeComponent();
            _centerX = InitialCenterX;
            _centerY = InitialCenterY;
        }
        protected abstract FractalMandelbrotFamilyEngine CreateEngine();
        protected virtual decimal BaseScale => 3.0m;
        protected virtual decimal InitialCenterX => -0.5m;
        protected virtual decimal InitialCenterY => 0.0m;
        protected virtual void UpdateEngineSpecificParameters() { }
        protected virtual void OnPostInitialize() { }
        protected virtual string GetSaveFileNameDetails() => "fractal";

        #endregion

        #region UI Initialization & Event Handlers (без изменений)
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
            nudZoom.DecimalPlaces = 15;
            nudZoom.Increment = 0.1m;
            nudZoom.Minimum = 0.01m;
            nudZoom.Maximum = decimal.MaxValue;
            _zoom = BaseScale / 4.0m;
            nudZoom.Value = _zoom;
            if (nudRe != null && nudIm != null)
            {
                nudRe.Minimum = -2m; nudRe.Maximum = 2m; nudRe.DecimalPlaces = 15; nudRe.Increment = 0.001m; nudRe.Value = -0.8m;
                nudIm.Minimum = -2m; nudIm.Maximum = 2m; nudIm.DecimalPlaces = 15; nudIm.Increment = 0.001m; nudIm.Value = 0.156m;
            }
        }
        private void InitializeEventHandlers()
        {
            nudIterations.ValueChanged += ParamControl_Changed;
            nudThreshold.ValueChanged += ParamControl_Changed;
            cbThreads.SelectedIndexChanged += ParamControl_Changed;
            nudZoom.ValueChanged += ParamControl_Changed;
            if (nudRe != null) nudRe.ValueChanged += ParamControl_Changed;
            if (nudIm != null) nudIm.ValueChanged += ParamControl_Changed;
            btnRender.Click += (s, e) => ScheduleRender();
            var configButton = Controls.Find("color_configurations", true).FirstOrDefault();
            if (configButton != null) configButton.Click += color_configurations_Click;
            canvas.MouseWheel += Canvas_MouseWheel;
            canvas.MouseDown += Canvas_MouseDown;
            canvas.MouseMove += Canvas_MouseMove;
            canvas.MouseUp += Canvas_MouseUp;
            canvas.Paint += Canvas_Paint;
            canvas.Resize += (s, e) => { if (WindowState != FormWindowState.Minimized) ScheduleRender(); };
            FormClosed += FractalMandelbrotFamilyForm_FormClosed;
        }
        private void color_configurations_Click(object sender, EventArgs e)
        {
            if (_colorConfigForm == null || _colorConfigForm.IsDisposed)
            {
                _colorConfigForm = new ColorConfigurationMandelbrotFamilyForm(_paletteManager);
                _colorConfigForm.PaletteApplied += OnPaletteApplied;
                _colorConfigForm.FormClosed += (s, args) => _colorConfigForm = null;
                _colorConfigForm.Show(this);
            }
            else { _colorConfigForm.Activate(); }
        }
        private void OnPaletteApplied(object sender, EventArgs e)
        {
            UpdateEngineParameters();
            ScheduleRender();
        }
        private void ParamControl_Changed(object sender, EventArgs e)
        {
            if (_isHighResRendering) return;
            if (sender == nudZoom && nudZoom.Value != _zoom) _zoom = nudZoom.Value;
            ScheduleRender();
        }

        #endregion

        #region Canvas Interaction (ИЗМЕНЕНО ДЛЯ ВЫСОКОЙ ТОЧНОСТИ)

        /// <summary>
        /// *** НОВЫЙ МЕТОД ***
        /// Проверяет, нужно ли использовать вычисления с высокой точностью.
        /// </summary>
        private bool ShouldUseBigDecimal()
        {
            try
            {
                return BaseScale / _zoom > FractalMandelbrotFamilyEngine.ZoomThresholdForBigDecimal;
            }
            catch (OverflowException)
            {
                // Если даже расчет вызывает переполнение, точность нужна.
                return true;
            }
        }

        /// <summary>
        /// Обрабатывает масштабирование колесом мыши.
        /// *** ИЗМЕНЕНО: Добавлена ветка для вычислений с BigDecimal. ***
        /// </summary>
        private void Canvas_MouseWheel(object sender, MouseEventArgs e)
        {
            if (_isHighResRendering || canvas.Width <= 0 || canvas.Height <= 0) return;
            CommitAndBakePreview();

            if (ShouldUseBigDecimal())
            {
                // --- ВЫСОКАЯ ТОЧНОСТЬ (BigDecimal) ---
                // ИСПРАВЛЕНИЕ CS1503: Удален суффикс 'm' из строки "1.5m"
                BigDecimal zoomFactor = e.Delta > 0 ? new BigDecimal(1.5m) : BigDecimal.One / new BigDecimal(1.5m);
                BigDecimal scaleBeforeZoom = new BigDecimal(BaseScale) / new BigDecimal(_zoom);

                // Координаты мыши считаем с высокой точностью
                BigDecimal centerX_big = new BigDecimal(_centerX);
                BigDecimal centerY_big = new BigDecimal(_centerY);
                BigDecimal mouseReal = centerX_big + (e.X - canvas.Width / 2.0) * scaleBeforeZoom / canvas.Width;
                BigDecimal mouseImaginary = centerY_big - (e.Y - canvas.Height / 2.0) * scaleBeforeZoom / canvas.Height;

                BigDecimal newZoom = new BigDecimal(_zoom) * zoomFactor;

                // Проверка на выход за пределы decimal
                try
                {
                    if (newZoom > new BigDecimal(nudZoom.Maximum)) newZoom = new BigDecimal(nudZoom.Maximum);
                    if (newZoom < new BigDecimal(nudZoom.Minimum)) newZoom = new BigDecimal(nudZoom.Minimum);
                    _zoom = (decimal)newZoom;
                }
                catch (OverflowException)
                {
                    // Если новое значение зума не помещается в decimal, оставляем старое
                    newZoom = new BigDecimal(_zoom);
                }

                BigDecimal scaleAfterZoom = new BigDecimal(BaseScale) / newZoom;
                centerX_big = mouseReal - (e.X - canvas.Width / 2.0) * scaleAfterZoom / canvas.Width;
                centerY_big = mouseImaginary + (e.Y - canvas.Height / 2.0) * scaleAfterZoom / canvas.Height;

                // Сохраняем обратно в decimal
                _centerX = (decimal)centerX_big;
                _centerY = (decimal)centerY_big;
            }
            else
            {
                // --- СТАНДАРТНАЯ ТОЧНОСТЬ (decimal) ---
                decimal zoomFactor = e.Delta > 0 ? 1.5m : 1.0m / 1.5m;
                decimal scaleBeforeZoom = BaseScale / _zoom;
                decimal mouseReal = _centerX + (e.X - canvas.Width / 2.0m) * scaleBeforeZoom / canvas.Width;
                decimal mouseImaginary = _centerY - (e.Y - canvas.Height / 2.0m) * scaleBeforeZoom / canvas.Height;
                _zoom = Math.Max(nudZoom.Minimum, Math.Min(nudZoom.Maximum, _zoom * zoomFactor));
                decimal scaleAfterZoom = BaseScale / _zoom;
                _centerX = mouseReal - (e.X - canvas.Width / 2.0m) * scaleAfterZoom / canvas.Width;
                _centerY = mouseImaginary + (e.Y - canvas.Height / 2.0m) * scaleAfterZoom / canvas.Height;
            }

            canvas.Invalidate();
            // Обновляем UI и запускаем рендер
            if (nudZoom.Value != _zoom) nudZoom.Value = _zoom; else ScheduleRender();
        }

        private void Canvas_MouseDown(object sender, MouseEventArgs e)
        {
            if (_isHighResRendering) return;
            if (e.Button == MouseButtons.Left) { _panning = true; _panStart = e.Location; canvas.Cursor = Cursors.Hand; }
        }

        /// <summary>
        /// Обрабатывает панорамирование.
        /// *** ИЗМЕНЕНО: Добавлена ветка для вычислений с BigDecimal. ***
        /// </summary>
        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isHighResRendering || !_panning || canvas.Width <= 0) return;
            CommitAndBakePreview();

            if (ShouldUseBigDecimal())
            {
                // --- ВЫСОКАЯ ТОЧНОСТЬ (BigDecimal) ---
                BigDecimal unitsPerPixel = new BigDecimal(BaseScale) / new BigDecimal(_zoom) / canvas.Width;
                BigDecimal deltaX = e.X - _panStart.X;
                BigDecimal deltaY = e.Y - _panStart.Y;

                BigDecimal centerX_big = new BigDecimal(_centerX);
                BigDecimal centerY_big = new BigDecimal(_centerY);

                centerX_big -= deltaX * unitsPerPixel;
                centerY_big += deltaY * unitsPerPixel;

                _centerX = (decimal)centerX_big;
                _centerY = (decimal)centerY_big;
            }
            else
            {
                // --- СТАНДАРТНАЯ ТОЧНОСТЬ (decimal) ---
                decimal unitsPerPixel = BaseScale / _zoom / canvas.Width;
                _centerX -= (e.X - _panStart.X) * unitsPerPixel;
                _centerY += (e.Y - _panStart.Y) * unitsPerPixel;
            }

            _panStart = e.Location;
            canvas.Invalidate();
            ScheduleRender();
        }

        private void Canvas_MouseUp(object sender, MouseEventArgs e)
        {
            if (_isHighResRendering) return;
            if (e.Button == MouseButtons.Left) { _panning = false; canvas.Cursor = Cursors.Default; }
        }

        private void Canvas_Paint(object sender, PaintEventArgs e)
        {
            if (canvas.Width <= 0 || canvas.Height <= 0) { e.Graphics.Clear(Color.Black); return; }
            e.Graphics.Clear(Color.Black);
            lock (_bitmapLock)
            {
                if (_previewBitmap != null)
                {
                    if (_renderedCenterX == _centerX && _renderedCenterY == _centerY && _renderedZoom == _zoom)
                        e.Graphics.DrawImageUnscaled(_previewBitmap, Point.Empty);
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
                                PointF p1 = new PointF((float)offsetXPixels, (float)offsetYPixels);
                                PointF p2 = new PointF((float)(offsetXPixels + newWidthPixels), (float)offsetYPixels);
                                PointF p3 = new PointF((float)offsetXPixels, (float)(offsetYPixels + newHeightPixels));
                                e.Graphics.DrawImage(_previewBitmap, new PointF[] { p1, p2, p3 });
                            }
                        }
                        catch (Exception) { if (_previewBitmap != null) e.Graphics.DrawImageUnscaled(_previewBitmap, Point.Empty); }
                    }
                }
                if (_currentRenderingBitmap != null) e.Graphics.DrawImageUnscaled(_currentRenderingBitmap, Point.Empty);
            }
            if (_renderVisualizer != null && _isRenderingPreview) _renderVisualizer.DrawVisualization(e.Graphics);
        }
        #endregion

        #region Rendering Logic (ИЗМЕНЕНО ДЛЯ ВЫБОРА СТРАТЕГИИ РЕНДЕРИНГА)

        /// <summary>
        /// Асинхронно запускает рендеринг предпросмотра с SSAA.
        /// *** ИЗМЕНЕНО: Выбирает между параллельным и последовательным рендерингом плиток. ***
        /// </summary>
        private async Task StartPreviewRenderSSAA(int ssaaFactor)
        {
            if (canvas.Width <= 0 || canvas.Height <= 0) return;
            int currentWidth = canvas.Width, currentHeight = canvas.Height;
            var stopwatch = Stopwatch.StartNew();
            _isRenderingPreview = true;
            _previewRenderCts?.Cancel();
            _previewRenderCts = new CancellationTokenSource();
            var token = _previewRenderCts.Token;
            _renderVisualizer?.NotifyRenderSessionStart();
            var newRenderingBitmap = new Bitmap(currentWidth, currentHeight, PixelFormat.Format32bppArgb);
            lock (_bitmapLock) { _currentRenderingBitmap?.Dispose(); _currentRenderingBitmap = newRenderingBitmap; }
            UpdateEngineParameters();
            var currentRenderedCenterX = _centerX; var currentRenderedCenterY = _centerY; var currentRenderedZoom = _zoom;
            var renderEngineCopy = CreateEngine();
            renderEngineCopy.MaxIterations = _fractalEngine.MaxIterations;
            renderEngineCopy.ThresholdSquared = _fractalEngine.ThresholdSquared;
            renderEngineCopy.CenterX = _fractalEngine.CenterX;
            renderEngineCopy.CenterY = _fractalEngine.CenterY;
            renderEngineCopy.Scale = _fractalEngine.Scale;
            renderEngineCopy.BaseScale = _fractalEngine.BaseScale;
            renderEngineCopy.C = _fractalEngine.C;
            renderEngineCopy.Palette = _fractalEngine.Palette;
            renderEngineCopy.MaxColorIterations = _fractalEngine.MaxColorIterations;

            var tiles = GenerateTiles(currentWidth, currentHeight);
            if (pbRenderProgress.IsHandleCreated && !pbRenderProgress.IsDisposed)
                pbRenderProgress.Invoke((Action)(() => { pbRenderProgress.Value = 0; pbRenderProgress.Maximum = tiles.Count; }));

            int progress = 0;
            try
            {
                // --- ГЛАВНОЕ ИЗМЕНЕНИЕ: ВЫБОР СТРАТЕГИИ РЕНДЕРИНГА ---
                bool useHighPrecisionSequential = ShouldUseBigDecimal();

                if (useHighPrecisionSequential)
                {
                    // --- ПОСЛЕДОВАТЕЛЬНЫЙ РЕНДЕРИНГ (для BigDecimal) ---
                    // Рендерим по одной плитке, но каждая плитка использует все ядра.
                    foreach (var tile in tiles)
                    {
                        token.ThrowIfCancellationRequested();
                        _renderVisualizer?.NotifyTileRenderStart(tile.Bounds);

                        // Запускаем рендеринг одной "тяжелой" плитки в фоновом потоке
                        var tileBuffer = await Task.Run(() =>
                            renderEngineCopy.RenderSingleTileSSAA(tile, currentWidth, currentHeight, ssaaFactor, out int bpp), token);

                        CopyTileToBitmap(newRenderingBitmap, tile, tileBuffer, 4);
                        _renderVisualizer?.NotifyTileRenderComplete(tile.Bounds);
                        UpdateProgress(ref progress, token);
                    }
                }
                else
                {
                    // --- ПАРАЛЛЕЛЬНЫЙ РЕНДЕРИНГ (для Decimal) ---
                    // Старая логика: рендерим много плиток одновременно.
                    var dispatcher = new TileRenderDispatcher(tiles, GetThreadCount());
                    await dispatcher.RenderAsync(async (tile, ct) =>
                    {
                        ct.ThrowIfCancellationRequested();
                        _renderVisualizer?.NotifyTileRenderStart(tile.Bounds);
                        var tileBuffer = renderEngineCopy.RenderSingleTileSSAA(tile, currentWidth, currentHeight, ssaaFactor, out int bytesPerPixel);
                        ct.ThrowIfCancellationRequested();
                        CopyTileToBitmap(newRenderingBitmap, tile, tileBuffer, bytesPerPixel);
                        _renderVisualizer?.NotifyTileRenderComplete(tile.Bounds);
                        UpdateProgress(ref progress, ct);
                        await Task.Yield();
                    }, token);
                }

                token.ThrowIfCancellationRequested();
                stopwatch.Stop();
                this.Text = $"{_baseTitle} - Время рендера (SSAA {ssaaFactor}x): {stopwatch.Elapsed.TotalSeconds:F3} сек.";
                FinalizeRender(newRenderingBitmap, currentRenderedCenterX, currentRenderedCenterY, currentRenderedZoom);
            }
            catch (OperationCanceledException) { AbortRender(newRenderingBitmap); }
            catch (Exception ex)
            {
                AbortRender(newRenderingBitmap);
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
        /// *** ИЗМЕНЕНО: Выбирает между параллельным и последовательным рендерингом плиток. ***
        /// </summary>
        private async Task StartPreviewRender()
        {
            if (canvas.Width <= 0 || canvas.Height <= 0) return;
            int currentWidth = canvas.Width, currentHeight = canvas.Height;
            var stopwatch = Stopwatch.StartNew();
            _isRenderingPreview = true;
            _previewRenderCts?.Cancel();
            _previewRenderCts = new CancellationTokenSource();
            var token = _previewRenderCts.Token;
            _renderVisualizer?.NotifyRenderSessionStart();
            var newRenderingBitmap = new Bitmap(currentWidth, currentHeight, PixelFormat.Format32bppArgb);
            lock (_bitmapLock) { _currentRenderingBitmap?.Dispose(); _currentRenderingBitmap = newRenderingBitmap; }
            UpdateEngineParameters();
            var currentRenderedCenterX = _centerX; var currentRenderedCenterY = _centerY; var currentRenderedZoom = _zoom;
            var renderEngineCopy = CreateEngine();
            renderEngineCopy.MaxIterations = _fractalEngine.MaxIterations;
            renderEngineCopy.ThresholdSquared = _fractalEngine.ThresholdSquared;
            renderEngineCopy.CenterX = _fractalEngine.CenterX;
            renderEngineCopy.CenterY = _fractalEngine.CenterY;
            renderEngineCopy.Scale = _fractalEngine.Scale;
            renderEngineCopy.BaseScale = _fractalEngine.BaseScale;
            renderEngineCopy.C = _fractalEngine.C;
            renderEngineCopy.Palette = _fractalEngine.Palette;
            renderEngineCopy.MaxColorIterations = _fractalEngine.MaxColorIterations;

            var tiles = GenerateTiles(currentWidth, currentHeight);
            if (pbRenderProgress.IsHandleCreated && !pbRenderProgress.IsDisposed)
                pbRenderProgress.Invoke((Action)(() => { pbRenderProgress.Value = 0; pbRenderProgress.Maximum = tiles.Count; }));

            int progress = 0;
            try
            {
                bool useHighPrecisionSequential = ShouldUseBigDecimal();

                if (useHighPrecisionSequential)
                {
                    // --- ПОСЛЕДОВАТЕЛЬНЫЙ РЕНДЕРИНГ (для BigDecimal) ---
                    foreach (var tile in tiles)
                    {
                        token.ThrowIfCancellationRequested();
                        _renderVisualizer?.NotifyTileRenderStart(tile.Bounds);
                        var tileBuffer = await Task.Run(() => renderEngineCopy.RenderSingleTile(tile, currentWidth, currentHeight, out int bpp), token);
                        CopyTileToBitmap(newRenderingBitmap, tile, tileBuffer, 4);
                        _renderVisualizer?.NotifyTileRenderComplete(tile.Bounds);
                        UpdateProgress(ref progress, token);
                    }
                }
                else
                {
                    // --- ПАРАЛЛЕЛЬНЫЙ РЕНДЕРИНГ (для Decimal) ---
                    var dispatcher = new TileRenderDispatcher(tiles, GetThreadCount());
                    await dispatcher.RenderAsync(async (tile, ct) =>
                    {
                        ct.ThrowIfCancellationRequested();
                        _renderVisualizer?.NotifyTileRenderStart(tile.Bounds);
                        var tileBuffer = renderEngineCopy.RenderSingleTile(tile, currentWidth, currentHeight, out int bytesPerPixel);
                        ct.ThrowIfCancellationRequested();
                        CopyTileToBitmap(newRenderingBitmap, tile, tileBuffer, bytesPerPixel);
                        _renderVisualizer?.NotifyTileRenderComplete(tile.Bounds);
                        UpdateProgress(ref progress, ct);
                        await Task.Yield();
                    }, token);
                }

                token.ThrowIfCancellationRequested();
                stopwatch.Stop();
                this.Text = $"{_baseTitle} - Время последнего рендера: {stopwatch.Elapsed.TotalSeconds:F3} сек.";
                FinalizeRender(newRenderingBitmap, currentRenderedCenterX, currentRenderedCenterY, currentRenderedZoom);
            }
            catch (OperationCanceledException) { AbortRender(newRenderingBitmap); }
            catch (Exception ex)
            {
                AbortRender(newRenderingBitmap);
                if (IsHandleCreated && !IsDisposed) MessageBox.Show($"Ошибка рендеринга: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _isRenderingPreview = false;
                _renderVisualizer?.NotifyRenderSessionComplete();
                if (pbRenderProgress.IsHandleCreated && !pbRenderProgress.IsDisposed) pbRenderProgress.Invoke((Action)(() => pbRenderProgress.Value = 0));
            }
        }

        private async void RenderDebounceTimer_Tick(object sender, EventArgs e)
        {
            _renderDebounceTimer.Stop();
            if (_isHighResRendering || _isRenderingPreview) { ScheduleRender(); return; }
            int ssaaFactor = GetSelectedSsaaFactor();
            this.Text = $"{_baseTitle} - Качество: {ssaaFactor}x";
            if (ShouldUseBigDecimal()) this.Text += " (Высокая точность)";
            if (ssaaFactor > 1) await StartPreviewRenderSSAA(ssaaFactor);
            else await StartPreviewRender();
        }

        private void OnVisualizerNeedsRedraw()
        {
            if (canvas.IsHandleCreated && !canvas.IsDisposed) canvas.BeginInvoke((Action)(() => canvas.Invalidate()));
        }

        #endregion

        #region Utility Methods (ИЗМЕНЕНО: добавлены хелперы для рендеринга)

        /// <summary>
        /// *** НОВЫЙ МЕТОД ***
        /// Копирует отрендеренную плитку в основной битмап.
        /// </summary>
        private void CopyTileToBitmap(Bitmap targetBitmap, TileInfo tile, byte[] tileBuffer, int bytesPerPixel)
        {
            lock (_bitmapLock)
            {
                if (_previewRenderCts.IsCancellationRequested || _currentRenderingBitmap != targetBitmap) return;
                var tileRect = tile.Bounds;
                var bitmapRect = new Rectangle(0, 0, targetBitmap.Width, targetBitmap.Height);
                tileRect.Intersect(bitmapRect);
                if (tileRect.Width == 0 || tileRect.Height == 0) return;
                BitmapData bmpData = targetBitmap.LockBits(tileRect, ImageLockMode.WriteOnly, targetBitmap.PixelFormat);
                int originalTileWidthInBytes = tile.Bounds.Width * bytesPerPixel;
                for (int y = 0; y < tileRect.Height; y++)
                {
                    IntPtr destPtr = IntPtr.Add(bmpData.Scan0, y * bmpData.Stride);
                    int srcOffset = ((y + tileRect.Y) - tile.Bounds.Y) * originalTileWidthInBytes + ((tileRect.X - tile.Bounds.X) * bytesPerPixel);
                    Marshal.Copy(tileBuffer, srcOffset, destPtr, tileRect.Width * bytesPerPixel);
                }
                targetBitmap.UnlockBits(bmpData);
            }
        }

        /// <summary>
        /// *** НОВЫЙ МЕТОД ***
        /// Обновляет прогресс-бар в потокобезопасном режиме.
        /// </summary>
        // ИСПРАВЛЕНИЕ CS1628: ref-параметр не захватывается лямбдой
        private void UpdateProgress(ref int progress, CancellationToken ct)
        {
            if (ct.IsCancellationRequested || !canvas.IsHandleCreated || canvas.IsDisposed) return;

            // Сначала выполняем атомарную операцию и получаем новое значение
            int newValue = Interlocked.Increment(ref progress);

            // Затем используем это значение для обновления UI в потоке UI
            canvas.Invoke((Action)(() =>
            {
                if (!ct.IsCancellationRequested && pbRenderProgress.IsHandleCreated && !pbRenderProgress.IsDisposed)
                    pbRenderProgress.Value = Math.Min(pbRenderProgress.Maximum, newValue);
            }));
        }

        /// <summary>
        /// *** НОВЫЙ МЕТОД ***
        /// Завершает сессию рендеринга, заменяя превью на новый битмап.
        /// </summary>
        private void FinalizeRender(Bitmap newBitmap, decimal centerX, decimal centerY, decimal zoom)
        {
            lock (_bitmapLock)
            {
                if (_currentRenderingBitmap == newBitmap)
                {
                    _previewBitmap?.Dispose();
                    _previewBitmap = _currentRenderingBitmap;
                    _currentRenderingBitmap = null;
                    _renderedCenterX = centerX;
                    _renderedCenterY = centerY;
                    _renderedZoom = zoom;
                }
                else { newBitmap?.Dispose(); }
            }
            if (canvas.IsHandleCreated && !canvas.IsDisposed) canvas.Invalidate();
        }

        /// <summary>
        /// *** НОВЫЙ МЕТОД ***
        /// Прерывает сессию рендеринга и очищает ресурсы.
        /// </summary>
        private void AbortRender(Bitmap newBitmap)
        {
            lock (_bitmapLock)
            {
                if (_currentRenderingBitmap == newBitmap)
                {
                    _currentRenderingBitmap?.Dispose();
                    _currentRenderingBitmap = null;
                }
            }
            newBitmap?.Dispose();
        }


        private List<TileInfo> GenerateTiles(int width, int height)
        {
            var tiles = new List<TileInfo>();
            Point center = new Point(width / 2, height / 2);
            for (int y = 0; y < height; y += TILE_SIZE)
                for (int x = 0; x < width; x += TILE_SIZE)
                    tiles.Add(new TileInfo(x, y, TILE_SIZE, TILE_SIZE));
            return tiles.OrderBy(t => Math.Pow(t.Center.X - center.X, 2) + Math.Pow(t.Center.Y - center.Y, 2)).ToList();
        }

        private void ScheduleRender()
        {
            if (_isHighResRendering || WindowState == FormWindowState.Minimized) return;
            if (_isRenderingPreview) _previewRenderCts?.Cancel();
            _renderDebounceTimer.Stop();
            _renderDebounceTimer.Start();
        }

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
                                PointF p1 = new PointF((float)offsetXPixels, (float)offsetYPixels);
                                PointF p2 = new PointF((float)(offsetXPixels + newWidthPixels), (float)offsetYPixels);
                                PointF p3 = new PointF((float)offsetXPixels, (float)(offsetYPixels + newHeightPixels));
                                g.DrawImage(_previewBitmap, new PointF[] { p1, p2, p3 });
                            }
                        }
                        catch (Exception) { }
                    }
                    if (_currentRenderingBitmap != null) g.DrawImageUnscaled(_currentRenderingBitmap, Point.Empty);
                }
                _previewBitmap?.Dispose(); _previewBitmap = bakedBitmap;
                _currentRenderingBitmap.Dispose(); _currentRenderingBitmap = null;
                _renderedCenterX = _centerX; _renderedCenterY = _centerY; _renderedZoom = _zoom;
            }
        }
        private void btnOpenSaveManager_Click(object sender, EventArgs e)
        {
            if (_isHighResRendering) { MessageBox.Show("Процесс рендеринга уже запущен.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            using (var saveManager = new SaveImageManagerForm(this)) { saveManager.ShowDialog(this); }
        }

        private void UpdateEngineParameters()
        {
            _fractalEngine.MaxIterations = (int)nudIterations.Value;
            _fractalEngine.ThresholdSquared = nudThreshold.Value * nudThreshold.Value;
            _fractalEngine.CenterX = _centerX;
            _fractalEngine.CenterY = _centerY;
            _fractalEngine.Scale = BaseScale / _zoom;
            _fractalEngine.BaseScale = BaseScale; // Передаем BaseScale в движок
            UpdateEngineSpecificParameters();
            ApplyActivePalette();
        }

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

        private int GetThreadCount() => cbThreads.SelectedItem?.ToString() == "Auto" ? Environment.ProcessorCount : Convert.ToInt32(cbThreads.SelectedItem);
        private decimal ClampDecimal(decimal value, decimal min, decimal max) => Math.Max(min, Math.Min(max, value));
        private int ClampInt(int value, int min, int max) => Math.Max(min, Math.Min(max, value));
        public double LoupeZoom => nudBaseScale != null ? (double)nudBaseScale.Value : 4.0;
        public event EventHandler LoupeZoomChanged;

        #endregion

        #region Palette & Form Lifecycle (без изменений)
        private string GeneratePaletteSignature(PaletteManagerMandelbrotFamily palette, int maxIterationsForAlignment)
        {
            var sb = new StringBuilder();
            sb.Append(palette.Name); sb.Append(':');
            foreach (var color in palette.Colors) sb.Append(color.ToArgb().ToString("X8"));
            sb.Append(':'); sb.Append(palette.IsGradient); sb.Append(':');
            sb.Append(palette.Gamma.ToString("F2")); sb.Append(':');
            int effectiveMaxColorIterations = palette.AlignWithRenderIterations ? maxIterationsForAlignment : palette.MaxColorIterations;
            sb.Append(effectiveMaxColorIterations);
            return sb.ToString();
        }
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
                    int index1 = (int)Math.Floor(scaledT), index2 = Math.Min(index1 + 1, colorCount - 1);
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
        private Color LerpColor(Color a, Color b, double t)
        {
            t = Math.Max(0, Math.Min(1, t));
            return Color.FromArgb((int)(a.A + (b.A - a.A) * t), (int)(a.R + (b.R - a.R) * t), (int)(a.G + (b.G - a.G) * t), (int)(a.B + (b.B - a.B) * t));
        }
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
                for (int i = 0; i <= effectiveMaxColorIterations; i++) _gammaCorrectedPaletteCache[i] = paletteGeneratorFunc(i, _fractalEngine.MaxIterations, effectiveMaxColorIterations);
            }
            _fractalEngine.MaxColorIterations = effectiveMaxColorIterations;
            _fractalEngine.Palette = (iter, maxIter, maxColorIter) =>
            {
                if (iter == maxIter) return Color.Black;
                int index = Math.Min(iter, _gammaCorrectedPaletteCache.Length - 1);
                return _gammaCorrectedPaletteCache[index];
            };
        }
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
                cbSSAA.Items.Add("Выкл (1x)"); cbSSAA.Items.Add("Низкое (2x)"); cbSSAA.Items.Add("Высокое (4x)");
                cbSSAA.SelectedItem = "Выкл (1x)";
                cbSSAA.SelectedIndexChanged += (s, ev) => ScheduleRender();
            }
            _renderedCenterX = _centerX; _renderedCenterY = _centerY; _renderedZoom = _zoom;
            OnPostInitialize();
            UpdateEngineParameters();
            ScheduleRender();
        }
        private void FractalMandelbrotFamilyForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            _renderDebounceTimer?.Stop(); _renderDebounceTimer?.Dispose();
            if (_previewRenderCts != null) { _previewRenderCts.Cancel(); _previewRenderCts.Dispose(); }
            lock (_bitmapLock)
            {
                _previewBitmap?.Dispose(); _previewBitmap = null;
                _currentRenderingBitmap?.Dispose(); _currentRenderingBitmap = null;
            }
            if (_renderVisualizer != null) { _renderVisualizer.NeedsRedraw -= OnVisualizerNeedsRedraw; _renderVisualizer.Dispose(); }
        }

        #endregion

        #region ISaveLoadCapableFractal & IHighResRenderable (без изменений)

        public abstract string FractalTypeIdentifier { get; }
        public abstract Type ConcreteSaveStateType { get; }
        public class PreviewParams { public decimal CenterX { get; set; } public decimal CenterY { get; set; } public decimal Zoom { get; set; } public int Iterations { get; set; } public string PaletteName { get; set; } public decimal Threshold { get; set; } public decimal CRe { get; set; } public decimal CIm { get; set; } public string PreviewEngineType { get; set; } }
        private void btnStateManager_Click(object sender, EventArgs e) { using (var dialog = new SaveLoadDialogForm(this)) { dialog.ShowDialog(this); } }
        public virtual FractalSaveStateBase GetCurrentStateForSave(string saveName)
        {
            MandelbrotFamilySaveState state;
            if (this is FractalJulia || this is FractalJuliaBurningShip) state = new JuliaFamilySaveState(this.FractalTypeIdentifier); else state = new MandelbrotFamilySaveState(this.FractalTypeIdentifier);
            state.SaveName = saveName;
            state.Timestamp = DateTime.Now;
            state.CenterX = _centerX; state.CenterY = _centerY; state.Zoom = _zoom;
            state.Threshold = nudThreshold.Value;
            state.Iterations = (int)nudIterations.Value;
            state.PaletteName = _paletteManager.ActivePalette?.Name ?? "Стандартный серый";
            state.PreviewEngineType = this.FractalTypeIdentifier;
            var previewParams = new PreviewParams { CenterX = _centerX, CenterY = _centerY, Zoom = _zoom, Iterations = Math.Min((int)nudIterations.Value, 75), PaletteName = state.PaletteName, Threshold = state.Threshold, PreviewEngineType = state.PreviewEngineType };
            if (state is JuliaFamilySaveState juliaState)
            {
                if (nudRe != null && nudIm != null && nudRe.Visible) { juliaState.CRe = nudRe.Value; juliaState.CIm = nudIm.Value; previewParams.CRe = juliaState.CRe; previewParams.CIm = juliaState.CIm; }
            }
            state.PreviewParametersJson = JsonSerializer.Serialize(previewParams, new JsonSerializerOptions());
            return state;
        }
        public virtual void LoadState(FractalSaveStateBase stateBase)
        {
            if (!(stateBase is MandelbrotFamilySaveState state)) { MessageBox.Show("Несовместимый тип состояния для загрузки.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
            _isRenderingPreview = false; _previewRenderCts?.Cancel(); _renderDebounceTimer.Stop();
            _centerX = state.CenterX; _centerY = state.CenterY; _zoom = state.Zoom;
            nudZoom.Value = ClampDecimal(_zoom, nudZoom.Minimum, nudZoom.Maximum);
            nudThreshold.Value = ClampDecimal(state.Threshold, nudThreshold.Minimum, nudThreshold.Maximum);
            nudIterations.Value = ClampInt(state.Iterations, (int)nudIterations.Minimum, (int)nudIterations.Maximum);
            var paletteToLoad = _paletteManager.Palettes.FirstOrDefault(p => p.Name == state.PaletteName);
            if (paletteToLoad != null) _paletteManager.ActivePalette = paletteToLoad;
            if (state is JuliaFamilySaveState juliaState)
            {
                if (nudRe != null && nudIm != null && nudRe.Visible) { nudRe.Value = ClampDecimal(juliaState.CRe, nudRe.Minimum, nudRe.Maximum); nudIm.Value = ClampDecimal(juliaState.CIm, nudIm.Minimum, nudIm.Maximum); }
            }
            lock (_bitmapLock) { _previewBitmap?.Dispose(); _previewBitmap = null; _currentRenderingBitmap?.Dispose(); _currentRenderingBitmap = null; }
            _renderedCenterX = _centerX; _renderedCenterY = _centerY; _renderedZoom = _zoom;
            UpdateEngineParameters(); ScheduleRender();
        }
        public virtual async Task<byte[]> RenderPreviewTileAsync(FractalSaveStateBase stateBase, TileInfo tile, int totalWidth, int totalHeight, int tileSize)
        {
            return await Task.Run(() =>
            {
                if (string.IsNullOrEmpty(stateBase.PreviewParametersJson)) return new byte[tile.Bounds.Width * tile.Bounds.Height * 4];
                PreviewParams previewParams; try { previewParams = JsonSerializer.Deserialize<PreviewParams>(stateBase.PreviewParametersJson); } catch { return new byte[tile.Bounds.Width * tile.Bounds.Height * 4]; }
                FractalMandelbrotFamilyEngine previewEngine;
                switch (previewParams.PreviewEngineType)
                {
                    case "Mandelbrot": previewEngine = new MandelbrotEngine(); break;
                    case "Julia": previewEngine = new JuliaEngine { C = new ComplexDecimal(previewParams.CRe, previewParams.CIm) }; break;
                    case "MandelbrotBurningShip": previewEngine = new MandelbrotBurningShipEngine(); break;
                    case "JuliaBurningShip": previewEngine = new JuliaBurningShipEngine { C = new ComplexDecimal(previewParams.CRe, previewParams.CIm) }; break;
                    default: return new byte[tile.Bounds.Width * tile.Bounds.Height * 4];
                }
                previewEngine.MaxIterations = 400; previewEngine.CenterX = previewParams.CenterX; previewEngine.CenterY = previewParams.CenterY; if (previewParams.Zoom == 0) previewParams.Zoom = 0.001m;
                previewEngine.Scale = this.BaseScale / previewParams.Zoom; previewEngine.ThresholdSquared = previewParams.Threshold * previewParams.Threshold;
                var paletteForPreview = _paletteManager.Palettes.FirstOrDefault(p => p.Name == previewParams.PaletteName) ?? _paletteManager.Palettes.First();
                previewEngine.MaxColorIterations = paletteForPreview.AlignWithRenderIterations ? previewEngine.MaxIterations : paletteForPreview.MaxColorIterations;
                previewEngine.Palette = GeneratePaletteFunction(paletteForPreview);
                return previewEngine.RenderSingleTile(tile, totalWidth, totalHeight, out _);
            });
        }
        public virtual Bitmap RenderPreview(FractalSaveStateBase stateBase, int previewWidth, int previewHeight)
        {
            if (string.IsNullOrEmpty(stateBase.PreviewParametersJson)) { var bmpError = new Bitmap(previewWidth, previewHeight); using (var g = Graphics.FromImage(bmpError)) { g.Clear(Color.DarkGray); TextRenderer.DrawText(g, "Нет данных", Font, new Rectangle(0, 0, previewWidth, previewHeight), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); } return bmpError; }
            PreviewParams previewParams; try { previewParams = JsonSerializer.Deserialize<PreviewParams>(stateBase.PreviewParametersJson, new JsonSerializerOptions()); } catch (Exception) { var bmpError = new Bitmap(previewWidth, previewHeight); using (var g = Graphics.FromImage(bmpError)) { g.Clear(Color.DarkRed); TextRenderer.DrawText(g, "Ошибка параметров", Font, new Rectangle(0, 0, previewWidth, previewHeight), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); } return bmpError; }
            FractalMandelbrotFamilyEngine previewEngine;
            switch (previewParams.PreviewEngineType)
            {
                case "Mandelbrot": previewEngine = new MandelbrotEngine(); break;
                case "Julia": previewEngine = new JuliaEngine { C = new ComplexDecimal(previewParams.CRe, previewParams.CIm) }; break;
                case "MandelbrotBurningShip": previewEngine = new MandelbrotBurningShipEngine(); break;
                case "JuliaBurningShip": previewEngine = new JuliaBurningShipEngine { C = new ComplexDecimal(previewParams.CRe, previewParams.CIm) }; break;
                default: var bmpError = new Bitmap(previewWidth, previewHeight); using (var g = Graphics.FromImage(bmpError)) { g.Clear(Color.DarkOrange); TextRenderer.DrawText(g, "Неизв. тип движка", Font, new Rectangle(0, 0, previewWidth, previewHeight), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); } return bmpError;
            }
            previewEngine.CenterX = previewParams.CenterX; previewEngine.CenterY = previewParams.CenterY; if (previewParams.Zoom == 0) previewParams.Zoom = 0.001m;
            previewEngine.Scale = this.BaseScale / previewParams.Zoom; previewEngine.MaxIterations = previewParams.Iterations; previewEngine.ThresholdSquared = previewParams.Threshold * previewParams.Threshold;
            var paletteForPreview = _paletteManager.Palettes.FirstOrDefault(p => p.Name == previewParams.PaletteName) ?? _paletteManager.Palettes.First();
            previewEngine.MaxColorIterations = paletteForPreview.AlignWithRenderIterations ? previewEngine.MaxIterations : paletteForPreview.MaxColorIterations;
            previewEngine.Palette = GeneratePaletteFunction(paletteForPreview);
            return previewEngine.RenderToBitmap(previewWidth, previewHeight, 1, progress => { });
        }
        public virtual List<FractalSaveStateBase> LoadAllSavesForThisType() { throw new NotImplementedException($"Метод LoadAllSavesForThisType должен быть переопределен в классе {this.GetType().Name}, чтобы загружать состояния типа {this.ConcreteSaveStateType.Name}."); }
        public virtual void SaveAllSavesForThisType(List<FractalSaveStateBase> saves) { throw new NotImplementedException($"Метод SaveAllSavesForThisType должен быть переопределен в классе {this.GetType().Name}, чтобы сохранять состояния типа {this.ConcreteSaveStateType.Name}."); }
        public HighResRenderState GetRenderState()
        {
            var state = new HighResRenderState { EngineType = this.FractalTypeIdentifier, CenterX = _centerX, CenterY = _centerY, Zoom = _zoom, BaseScale = this.BaseScale, Iterations = (int)nudIterations.Value, Threshold = nudThreshold.Value, ActivePaletteName = _paletteManager.ActivePalette?.Name ?? "Стандартный серый", FileNameDetails = this.GetSaveFileNameDetails() };
            if (this is FractalJulia || this is FractalJuliaBurningShip) { state.JuliaC = new ComplexDecimal(nudRe.Value, nudIm.Value); }
            return state;
        }
        private FractalMandelbrotFamilyEngine CreateEngineFromState(HighResRenderState state, bool forPreview)
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
            if (forPreview) engine.MaxIterations = Math.Min(state.Iterations, 150); else engine.MaxIterations = state.Iterations;
            engine.ThresholdSquared = state.Threshold * state.Threshold;
            engine.CenterX = state.CenterX; engine.CenterY = state.CenterY; engine.Scale = state.BaseScale / state.Zoom; engine.BaseScale = state.BaseScale;
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
                FractalMandelbrotFamilyEngine renderEngine = CreateEngineFromState(state, forPreview: false);
                int threadCount = GetThreadCount();
                Bitmap highResBitmap = await Task.Run(() => renderEngine.RenderToBitmapSSAA(width, height, threadCount, p => progress.Report(new RenderProgress { Percentage = p, Status = "Рендеринг..." }), ssaaFactor, cancellationToken), cancellationToken);
                return highResBitmap;
            }
            finally { _isHighResRendering = false; }
        }
        public Bitmap RenderPreview(HighResRenderState state, int previewWidth, int previewHeight)
        {
            var engine = CreateEngineFromState(state, forPreview: true);
            return engine.RenderToBitmap(previewWidth, previewHeight, 1, _ => { }, CancellationToken.None);
        }

        #endregion
    }
}