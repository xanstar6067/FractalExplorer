using FractalExplorer.Engines;
using FractalExplorer.Forms.Other;
using FractalExplorer.Resources;
using FractalExplorer.Utilities;
using FractalExplorer.Utilities.RenderUtilities;
using FractalExplorer.Utilities.SaveIO.ColorPalettes;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
// using FractalExplorer.Utilities.SaveIO; // Пока не используется
// using FractalExplorer.Utilities.SaveIO.SaveStateImplementations; // Пока не используется
using System.Runtime.InteropServices;

namespace FractalExplorer.Forms.Fractals
{
    // Адаптировано: Форма теперь наследуется от базового класса Form.
    // Интерфейсы для сохранения будут добавлены позже.
    public partial class FractalCollatzForm : Form
    {
        #region Fields

        // Изменено: Тип движка теперь базовый, а инициализируется конкретным CollatzEngine.
        private FractalMandelbrotFamilyEngine _fractalEngine;

        // Поля, скопированные из FractalPhoenixForm, которые не требуют изменений:
        private RenderVisualizerComponent _renderVisualizer;
        private PaletteManager _paletteManager;
        private Color[] _gammaCorrectedPaletteCache;
        private string _paletteCacheSignature;
        private ColorConfigurationForm _colorConfigForm;
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
        public FractalCollatzForm()
        {
            InitializeComponent();
            // Изменено: Устанавливаем заголовок для фрактала Коллатца
            Text = "Фрактал Коллатца";
        }
        #endregion

        #region UI Initialization
        /// <summary>
        /// Инициализирует значения по умолчанию и ограничения для элементов управления UI.
        /// </summary>
        private void InitializeControls()
        {
            // Эта логика полностью скопирована из FractalPhoenixForm,
            // так как элементы управления (потоки, итерации, масштаб) идентичны.
            int cores = Environment.ProcessorCount;
            cbThreads.Items.Clear();
            for (int i = 1; i <= cores; i++) cbThreads.Items.Add(i);
            cbThreads.Items.Add("Auto");
            cbThreads.SelectedItem = "Auto";

            nudIterations.Minimum = 10;
            nudIterations.Maximum = 100000;
            nudIterations.Value = 150; // Немного больше для красоты Коллатца

            nudThreshold.Minimum = 2m;
            nudThreshold.Maximum = 10000m; // Для Коллатца может понадобиться больший порог
            nudThreshold.DecimalPlaces = 1;
            nudThreshold.Increment = 1m;
            nudThreshold.Value = 100m; // Начальный порог для Коллатца

            nudZoom.DecimalPlaces = 15;
            nudZoom.Increment = 0.1m;
            nudZoom.Minimum = 0.001m;
            nudZoom.Maximum = decimal.MaxValue;
            _zoom = BASE_SCALE / 4.0m;
            nudZoom.Value = _zoom;

            cbSSAA.Items.Add("Выкл (1x)");
            cbSSAA.Items.Add("Низкое (2x)");
            cbSSAA.Items.Add("Высокое (4x)");
            cbSSAA.SelectedItem = "Выкл (1x)";
        }

        /// <summary>
        /// Инициализирует обработчики событий для различных элементов управления UI.
        /// </summary>
        private void InitializeEventHandlers()
        {
            // Удалены обработчики для nudC1Re, nudC1Im и т.д.
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
        }
        #endregion

        #region UI Event Handlers
        // Все обработчики событий UI (нажатия кнопок, панорамирование, масштабирование)
        // полностью скопированы из FractalPhoenixForm. Они являются универсальными.
        // Я приведу только те, где есть минимальные изменения, или которые важны.

        private void ParamControl_Changed(object sender, EventArgs e)
        {
            if (_isHighResRendering) return;
            if (sender == nudZoom && nudZoom.Value != _zoom) _zoom = nudZoom.Value;
            ScheduleRender();
        }

        private async void btnRender_Click(object sender, EventArgs e)
        {
            _renderDebounceTimer.Stop();
            _previewRenderCts?.Cancel();
            if (_isHighResRendering || _isRenderingPreview) return;

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

        // Заглушки для функционала, который будет реализован позже.
        private void btnOpenSaveManager_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Функция сохранения изображений будет добавлена на следующем шаге.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnStateManager_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Функция сохранения/загрузки состояний будет добавлена на следующем шаге.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        #endregion

        #region Canvas Interaction
        // Вся секция Canvas Interaction полностью скопирована из FractalPhoenixForm
        // без каких-либо изменений.
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
            if (canvas.Width <= 0 || canvas.Height <= 0)
            {
                e.Graphics.Clear(Color.Black);
                return;
            }

            e.Graphics.Clear(Color.Black);

            lock (_bitmapLock)
            {
                // Безопасное рисование preview bitmap
                if (_previewBitmap != null)
                {
                    try
                    {
                        // Проверяем, что bitmap не был disposed
                        if (_previewBitmap.Width > 0 && _previewBitmap.Height > 0)
                        {
                            if (_renderedCenterX == _centerX && _renderedCenterY == _centerY && _renderedZoom == _zoom)
                            {
                                // Простое рисование без трансформации
                                e.Graphics.DrawImageUnscaled(_previewBitmap, Point.Empty);
                            }
                            else
                            {
                                // Рисование с трансформацией
                                DrawTransformedPreview(e.Graphics);
                            }
                        }
                    }
                    catch (ArgumentException)
                    {
                        // Bitmap был disposed или поврежден - игнорируем
                    }
                    catch (InvalidOperationException)
                    {
                        // Bitmap находится в недействительном состоянии
                    }
                    catch (ExternalException)
                    {
                        // Ошибка GDI+ - игнорируем
                    }
                }

                // Безопасное рисование current rendering bitmap
                if (_currentRenderingBitmap != null)
                {
                    try
                    {
                        // Проверяем, что bitmap не был disposed
                        if (_currentRenderingBitmap.Width > 0 && _currentRenderingBitmap.Height > 0)
                        {
                            e.Graphics.DrawImageUnscaled(_currentRenderingBitmap, Point.Empty);
                        }
                    }
                    catch (ArgumentException)
                    {
                        // Bitmap был disposed или поврежден - игнорируем
                    }
                    catch (InvalidOperationException)
                    {
                        // Bitmap находится в недействительном состоянии
                    }
                    catch (ExternalException)
                    {
                        // Ошибка GDI+ - игнорируем
                    }
                }
            }

            // Рисование визуализатора рендеринга
            if (_renderVisualizer != null && _isRenderingPreview)
            {
                try
                {
                    _renderVisualizer.DrawVisualization(e.Graphics);
                }
                catch (Exception)
                {
                    // Игнорируем ошибки визуализатора
                }
            }
        }

        private void DrawTransformedPreview(Graphics graphics)
        {
            try
            {
                decimal renderedComplexWidth = BASE_SCALE / _renderedZoom;
                decimal currentComplexWidth = BASE_SCALE / _zoom;

                // Дополнительные проверки на валидность данных
                if (_renderedZoom <= 0 || _zoom <= 0 || renderedComplexWidth <= 0 || currentComplexWidth <= 0)
                    return;

                // Проверяем, что bitmap всё еще валиден
                if (_previewBitmap == null || _previewBitmap.Width <= 0 || _previewBitmap.Height <= 0)
                    return;

                decimal unitsPerPixelRendered = renderedComplexWidth / _previewBitmap.Width;
                decimal unitsPerPixelCurrent = currentComplexWidth / canvas.Width;

                // Проверяем на деление на ноль
                if (unitsPerPixelCurrent == 0)
                    return;

                decimal renderedReMin = _renderedCenterX - (renderedComplexWidth / 2.0m);
                decimal renderedImMax = _renderedCenterY + (_previewBitmap.Height * unitsPerPixelRendered / 2.0m);
                decimal currentReMin = _centerX - (currentComplexWidth / 2.0m);
                decimal currentImMax = _centerY + (canvas.Height * unitsPerPixelCurrent / 2.0m);

                decimal offsetXPixels = (renderedReMin - currentReMin) / unitsPerPixelCurrent;
                decimal offsetYPixels = (currentImMax - renderedImMax) / unitsPerPixelCurrent;
                decimal newWidthPixels = _previewBitmap.Width * (unitsPerPixelRendered / unitsPerPixelCurrent);
                decimal newHeightPixels = _previewBitmap.Height * (unitsPerPixelRendered / unitsPerPixelCurrent);

                // Проверяем разумность размеров
                if (Math.Abs(newWidthPixels) > 50000 || Math.Abs(newHeightPixels) > 50000)
                    return;

                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

                PointF destPoint1 = new PointF((float)offsetXPixels, (float)offsetYPixels);
                PointF destPoint2 = new PointF((float)(offsetXPixels + newWidthPixels), (float)offsetYPixels);
                PointF destPoint3 = new PointF((float)offsetXPixels, (float)(offsetYPixels + newHeightPixels));

                graphics.DrawImage(_previewBitmap, new PointF[] { destPoint1, destPoint2, destPoint3 });
            }
            catch (Exception)
            {
                // Любая ошибка при трансформации - игнорируем
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

        // Адаптировано: Вспомогательный метод для создания копии движка рендеринга.
        // Вместо создания PhoenixEngine, создается CollatzEngine и копируются ОБЩИЕ параметры
        // из базового класса FractalMandelbrotFamilyEngine.
        private FractalMandelbrotFamilyEngine CreateEngineCopy()
        {
            var engineCopy = new FractalCollatzEngine();
            // Копируем все общие параметры
            engineCopy.MaxIterations = _fractalEngine.MaxIterations;
            engineCopy.ThresholdSquared = _fractalEngine.ThresholdSquared;
            engineCopy.CenterX = _fractalEngine.CenterX;
            engineCopy.CenterY = _fractalEngine.CenterY;
            engineCopy.Scale = _fractalEngine.Scale;
            engineCopy.UseSmoothColoring = _fractalEngine.UseSmoothColoring;
            engineCopy.Palette = _fractalEngine.Palette;
            engineCopy.SmoothPalette = _fractalEngine.SmoothPalette;
            engineCopy.MaxColorIterations = _fractalEngine.MaxColorIterations;

            // Специфичные для CollatzEngine параметры не требуются,
            // но если бы они были, их нужно было бы скопировать здесь.
            // engineCopy.CopySpecificParametersFrom(_fractalEngine); // Вызов на будущее

            return engineCopy;
        }

        private async Task StartPreviewRenderSSAA(int ssaaFactor)
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

            // Изменено: Создаем копию движка CollatzEngine
            var renderEngineCopy = CreateEngineCopy();

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

                    // Изменено: Используется CollatzEngine
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
                        int tileWidthInBytes = tile.Bounds.Width * bytesPerPixel;
                        for (int y = 0; y < tileRect.Height; y++)
                        {
                            IntPtr destPtr = IntPtr.Add(bmpData.Scan0, y * bmpData.Stride);
                            int srcOffset = y * tileWidthInBytes;
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

            // Изменено: Создаем копию движка CollatzEngine
            var renderEngineCopy = CreateEngineCopy();

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

                    // Изменено: Используется CollatzEngine
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

        private void OnVisualizerNeedsRedraw()
        {
            if (canvas.IsHandleCreated && !canvas.IsDisposed)
            {
                canvas.BeginInvoke((Action)(() => canvas.Invalidate()));
            }
        }
        #endregion

        #region Utility Methods
        // Вся секция Utility Methods полностью скопирована из FractalPhoenixForm
        // без каких-либо изменений.
        private void CommitAndBakePreview()
        {
            lock (_bitmapLock)
            {
                if (!_isRenderingPreview || _currentRenderingBitmap == null)
                    return;
            }

            _previewRenderCts?.Cancel();

            lock (_bitmapLock)
            {
                if (_currentRenderingBitmap == null || canvas.Width <= 0 || canvas.Height <= 0)
                    return;

                try
                {
                    // Проверяем, что current bitmap валиден
                    if (_currentRenderingBitmap.Width <= 0 || _currentRenderingBitmap.Height <= 0)
                        return;

                    var bakedBitmap = new Bitmap(canvas.Width, canvas.Height, PixelFormat.Format24bppRgb);

                    try
                    {
                        using (var g = Graphics.FromImage(bakedBitmap))
                        {
                            g.Clear(Color.Black);
                            g.InterpolationMode = InterpolationMode.Bilinear;

                            if (_previewBitmap != null)
                            {
                                try
                                {
                                    // Проверяем валидность preview bitmap
                                    if (_previewBitmap.Width > 0 && _previewBitmap.Height > 0)
                                    {
                                        DrawTransformedPreviewToBitmap(g);
                                    }
                                }
                                catch (ArgumentException)
                                {
                                    // _previewBitmap поврежден - игнорируем
                                }
                                catch (InvalidOperationException)
                                {
                                    // _previewBitmap в недействительном состоянии
                                }
                                catch (ExternalException)
                                {
                                    // Ошибка GDI+ - игнорируем
                                }
                            }

                            try
                            {
                                // Проверяем валидность current rendering bitmap еще раз
                                if (_currentRenderingBitmap.Width > 0 && _currentRenderingBitmap.Height > 0)
                                {
                                    g.DrawImageUnscaled(_currentRenderingBitmap, Point.Empty);
                                }
                            }
                            catch (ArgumentException)
                            {
                                // _currentRenderingBitmap поврежден - игнорируем
                            }
                            catch (InvalidOperationException)
                            {
                                // _currentRenderingBitmap в недействительном состоянии
                            }
                            catch (ExternalException)
                            {
                                // Ошибка GDI+ - игнорируем
                            }
                        }

                        // Успешно создали bakedBitmap, теперь заменяем старые
                        _previewBitmap?.Dispose();
                        _previewBitmap = bakedBitmap;
                        _currentRenderingBitmap?.Dispose();
                        _currentRenderingBitmap = null;
                        _renderedCenterX = _centerX;
                        _renderedCenterY = _centerY;
                        _renderedZoom = _zoom;
                    }
                    catch (Exception)
                    {
                        // Если что-то пошло не так, освобождаем bakedBitmap
                        bakedBitmap?.Dispose();
                        throw;
                    }
                }
                catch (Exception)
                {
                    // Любая ошибка при создании bakedBitmap - игнорируем
                    // Оставляем существующие bitmap'ы как есть
                }
            }
        }

        private void DrawTransformedPreviewToBitmap(Graphics g)
        {
            try
            {
                decimal renderedComplexWidth = BASE_SCALE / _renderedZoom;
                decimal currentComplexWidth = BASE_SCALE / _zoom;

                if (_renderedZoom <= 0 || _zoom <= 0 || renderedComplexWidth <= 0 || currentComplexWidth <= 0)
                    return;

                if (_previewBitmap == null || _previewBitmap.Width <= 0 || _previewBitmap.Height <= 0)
                    return;

                decimal unitsPerPixelRendered = renderedComplexWidth / _previewBitmap.Width;
                decimal unitsPerPixelCurrent = currentComplexWidth / canvas.Width;

                if (unitsPerPixelCurrent == 0)
                    return;

                decimal renderedReMin = _renderedCenterX - (renderedComplexWidth / 2.0m);
                decimal renderedImMax = _renderedCenterY + (_previewBitmap.Height * unitsPerPixelRendered / 2.0m);
                decimal currentReMin = _centerX - (currentComplexWidth / 2.0m);
                decimal currentImMax = _centerY + (canvas.Height * unitsPerPixelCurrent / 2.0m);

                decimal offsetXPixels = (renderedReMin - currentReMin) / unitsPerPixelCurrent;
                decimal offsetYPixels = (currentImMax - renderedImMax) / unitsPerPixelCurrent;
                decimal newWidthPixels = _previewBitmap.Width * (unitsPerPixelRendered / unitsPerPixelCurrent);
                decimal newHeightPixels = _previewBitmap.Height * (unitsPerPixelRendered / unitsPerPixelCurrent);

                // Проверяем разумность размеров
                if (Math.Abs(newWidthPixels) > 50000 || Math.Abs(newHeightPixels) > 50000)
                    return;

                PointF destPoint1 = new PointF((float)offsetXPixels, (float)offsetYPixels);
                PointF destPoint2 = new PointF((float)(offsetXPixels + newWidthPixels), (float)offsetYPixels);
                PointF destPoint3 = new PointF((float)offsetXPixels, (float)(offsetYPixels + newHeightPixels));

                g.DrawImage(_previewBitmap, new PointF[] { destPoint1, destPoint2, destPoint3 });
            }
            catch (Exception)
            {
                // Любая ошибка при трансформации - игнорируем
            }
        }

        // Адаптировано: Метод обновляет параметры движка.
        // Удалена установка параметров C1 и C2, которые были специфичны для Феникса.
        private void UpdateEngineParameters()
        {
            _fractalEngine.MaxIterations = (int)nudIterations.Value;
            _fractalEngine.ThresholdSquared = nudThreshold.Value * nudThreshold.Value;
            _fractalEngine.CenterX = _centerX;
            _fractalEngine.CenterY = _centerY;
            _fractalEngine.Scale = BASE_SCALE / _zoom;
            _fractalEngine.UseSmoothColoring = cbSmooth.Checked;

            // Эта часть универсальна и остается
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

        private int GetSelectedSsaaFactor()
        {
            if (cbSSAA.InvokeRequired)
            {
                return (int)cbSSAA.Invoke(new Func<int>(GetSelectedSsaaFactor));
            }
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
        // Вся секция Palette Management полностью скопирована из FractalPhoenixForm
        // без каких-либо изменений, так как она работает с общими параметрами
        // и не зависит от конкретной формулы фрактала.
        private Func<double, Color> GenerateSmoothPaletteFunction(Palette palette, int effectiveMaxColorIterations)
        {
            double gamma = palette.Gamma;
            var colors = new List<Color>(palette.Colors);
            int colorCount = colors.Count;

            if (palette.Name == "Стандартный серый")
            {
                return (smoothIter) =>
                {
                    if (smoothIter >= _fractalEngine.MaxIterations) return Color.Black;
                    if (smoothIter < 0) smoothIter = 0;

                    double logMax = Math.Log(_fractalEngine.MaxIterations + 1);
                    if (logMax <= 0) return Color.Black;

                    double tLog = Math.Log(smoothIter + 1) / logMax;

                    int gray_level = (int)(255.0 * (1.0 - tLog));
                    gray_level = Math.Max(0, Math.Min(255, gray_level));

                    Color baseColor = Color.FromArgb(gray_level, gray_level, gray_level);
                    return ColorCorrection.ApplyGamma(baseColor, gamma);
                };
            }

            if (effectiveMaxColorIterations <= 0) return (smoothIter) => Color.Black;
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

        private string GeneratePaletteSignature(Palette palette, int maxIterationsForAlignment)
        {
            var sb = new System.Text.StringBuilder();
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
                if (iter == maxIter) return Color.Black;
                int index = Math.Min(iter, _gammaCorrectedPaletteCache.Length - 1);
                return _gammaCorrectedPaletteCache[index];
            };
        }

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

        private Color LerpColor(Color a, Color b, double t)
        {
            t = Math.Max(0, Math.Min(1, t));
            return Color.FromArgb((int)(a.A + (b.A - a.A) * t), (int)(a.R + (b.R - a.R) * t), (int)(a.G + (b.G - a.G) * t), (int)(a.B + (b.B - a.B) * t));
        }

        private void OnPaletteApplied(object sender, EventArgs e)
        {
            UpdateEngineParameters();
            ScheduleRender();
        }
        #endregion

        #region Form Lifecycle
        private void FractalCollatzForm_Load(object sender, EventArgs e)
        {
            _baseTitle = this.Text;
            _paletteManager = new PaletteManager();

            // Изменено: Создаем экземпляр CollatzEngine.
            _fractalEngine = new FractalCollatzEngine();

            _renderDebounceTimer = new System.Windows.Forms.Timer { Interval = 300 };
            _renderDebounceTimer.Tick += RenderDebounceTimer_Tick;
            _renderVisualizer = new RenderVisualizerComponent(TILE_SIZE);
            _renderVisualizer.NeedsRedraw += OnVisualizerNeedsRedraw;

            InitializeControls();
            InitializeEventHandlers();

            // Установим начальные координаты и масштаб
            _centerX = 0.0m;
            _centerY = 0.0m;
            _renderedCenterX = _centerX;
            _renderedCenterY = _centerY;
            _renderedZoom = _zoom;

            UpdateEngineParameters();
            ScheduleRender();
        }

        private void FractalCollatzForm_FormClosed(object sender, FormClosedEventArgs e)
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
    }
}