using FractalExplorer.Core; // Для ColorPaletteMandelbrotFamily и ColorConfigurationMandelbrotFamilyForm
using FractalExplorer.Engines;
using FractalExplorer.Resources;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using FractalExplorer.Utilities;

// using FractalExplorer.Selectors; // Для PhoenixCSelectorForm - добавим позже

namespace FractalExplorer.Forms
{
    public partial class FractalPhoenixForm : Form //, IFractalForm // IFractalForm пока не реализуем
    {
        #region Fields
        private PhoenixEngine _fractalEngine;
        private RenderVisualizerComponent _renderVisualizer;
        private ColorPaletteMandelbrotFamily _paletteManager;
        private ColorConfigurationMandelbrotFamilyForm _colorConfigForm; // Используем тот же менеджер и форму настроек палитры

        private const int TILE_SIZE = 32;
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
        private const decimal BASE_SCALE = 4.0m; // Базовый масштаб для Феникса
        #endregion

        #region Constructor
        public FractalPhoenixForm()
        {
            InitializeComponent(); // Этот метод из Designer.cs
            Text = "Фрактал Феникс";
        }
        #endregion

        #region UI Initialization & Event Handlers from FractalMandelbrotFamilyForm
        private async void btnRender_Click(object sender, EventArgs e)
        {
            // Отменяем любой текущий рендер предпросмотра и останавливаем таймер,
            // чтобы избежать двойного запуска или конфликтов.
            _previewRenderCts?.Cancel();
            _renderDebounceTimer.Stop();

            // Если уже идет рендеринг высокого разрешения или другой рендеринг предпросмотра,
            // просто выходим (или можно добавить сообщение пользователю).
            if (_isHighResRendering || _isRenderingPreview)
            {
                // Можно показать MessageBox, если нужно
                // MessageBox.Show("Рендеринг уже выполняется.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Немедленно запускаем рендеринг предпросмотра.
            await StartPreviewRender();
        }
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

            nudIterations.Minimum = 10; // Мин. итераций для Феникса
            nudIterations.Maximum = 100000;
            nudIterations.Value = 100; // Итераций по умолчанию

            nudThreshold.Minimum = 2m;
            nudThreshold.Maximum = 1000m;
            nudThreshold.DecimalPlaces = 1;
            nudThreshold.Increment = 0.1m;
            nudThreshold.Value = 4m; // Порог (2*2) для Феникса

            nudZoom.DecimalPlaces = 15; // Увеличим точность для глубокого зума
            nudZoom.Increment = 0.1m;
            nudZoom.Minimum = 0.000000000000001m; // 1e-15m
            nudZoom.Maximum = 1000000000000000m;  // 1e15m
            _zoom = BASE_SCALE / 4.0m; // Начальный зум
            nudZoom.Value = _zoom;

            // Начальные значения для C1 (P, Q) и C2 (пока неактивны в рендере)
            nudC1Re.Value = 0.56m;   // P
            nudC1Im.Value = -0.5m; // Q  (Классический Феникс часто имеет Q < 0)
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

        private void InitializeEventHandlers()
        {
            nudC1Re.ValueChanged += ParamControl_Changed;
            nudC1Im.ValueChanged += ParamControl_Changed;
            nudC2Re.ValueChanged += ParamControl_Changed; // Пока не влияет на рендер, но для консистентности
            nudC2Im.ValueChanged += ParamControl_Changed; // Пока не влияет на рендер

            nudIterations.ValueChanged += ParamControl_Changed;
            nudThreshold.ValueChanged += ParamControl_Changed;
            cbThreads.SelectedIndexChanged += ParamControl_Changed;
            nudZoom.ValueChanged += ParamControl_Changed;

            btnRender.Click += (s, e) => ScheduleRender();
            btnSaveHighRes.Click += btnSaveHighRes_Click;
            color_configurations.Click += color_configurations_Click;
            btnSelectPhoenixParameters.Click += btnSelectPhoenixParameters_Click;

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

        private void FractalPhoenixForm_Load(object sender, EventArgs e)
        {
            _paletteManager = new ColorPaletteMandelbrotFamily();
            _fractalEngine = new PhoenixEngine(); // Создаем наш PhoenixEngine
            _renderDebounceTimer = new System.Windows.Forms.Timer { Interval = 300 };
            _renderDebounceTimer.Tick += RenderDebounceTimer_Tick;
            _renderVisualizer = new RenderVisualizerComponent(TILE_SIZE);
            _renderVisualizer.NeedsRedraw += OnVisualizerNeedsRedraw;

            InitializeControls();
            InitializeEventHandlers();

            _centerX = 0.0m; // Начальный центр для Феникса
            _centerY = 0.0m;
            _renderedCenterX = _centerX;
            _renderedCenterY = _centerY;
            _renderedZoom = _zoom;

            ApplyActivePalette();
            ScheduleRender();
        }

        private void OnVisualizerNeedsRedraw()
        {
            if (canvas.IsHandleCreated && !canvas.IsDisposed)
            {
                canvas.BeginInvoke((Action)(() => canvas.Invalidate()));
            }
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
            else
            {
                _colorConfigForm.Activate();
            }
        }

        private void OnPaletteApplied(object sender, EventArgs e)
        {
            ApplyActivePalette();
            ScheduleRender();
        }

        private void ParamControl_Changed(object sender, EventArgs e)
        {
            if (_isHighResRendering) return;

            if (sender == nudZoom)
            {
                if (nudZoom.Value != _zoom) _zoom = nudZoom.Value;
            }
            // Для C1, C2 значения напрямую читаются в UpdateEngineParameters
            ScheduleRender();
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
            _centerY += (decimal)(e.Y - _panStart.Y) * unitsPerPixel; // Y инвертирован в координатах мыши
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
                        catch (Exception) { /* Игнор ошибок интерполяции */ }
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

            // Создаем копию движка для безопасного использования в параллельных потоках
            var renderEngineCopy = new PhoenixEngine(); // Используем PhoenixEngine
            renderEngineCopy.MaxIterations = _fractalEngine.MaxIterations;
            renderEngineCopy.ThresholdSquared = _fractalEngine.ThresholdSquared;
            renderEngineCopy.CenterX = _fractalEngine.CenterX;
            renderEngineCopy.CenterY = _fractalEngine.CenterY;
            renderEngineCopy.Scale = _fractalEngine.Scale;
            renderEngineCopy.C1 = _fractalEngine.C1;
            renderEngineCopy.C2 = _fractalEngine.C2; // C2 пока не используется, но передаем
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

                lock (_bitmapLock)
                {
                    if (_currentRenderingBitmap == newRenderingBitmap)
                    {
                        _previewBitmap?.Dispose();
                        _previewBitmap = _currentRenderingBitmap; // _currentRenderingBitmap становится _previewBitmap
                        _currentRenderingBitmap = null;          // Очищаем _currentRenderingBitmap
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

                var bakedBitmap = new Bitmap(canvas.Width, canvas.Height, PixelFormat.Format24bppRgb); // Используем 24bppRgb
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
                        catch (Exception) { /* Игнор */ }
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

        private void UpdateEngineParameters()
        {
            _fractalEngine.MaxIterations = (int)nudIterations.Value;
            _fractalEngine.ThresholdSquared = nudThreshold.Value * nudThreshold.Value;
            _fractalEngine.CenterX = _centerX;
            _fractalEngine.CenterY = _centerY;
            _fractalEngine.Scale = BASE_SCALE / _zoom;
            _fractalEngine.C1 = new ComplexDecimal(nudC1Re.Value, nudC1Im.Value);
            _fractalEngine.C2 = new ComplexDecimal(nudC2Re.Value, nudC2Im.Value); // Пока не используется в рендере, но сохраняем
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
        #endregion

        #region Save High-Res
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
                        var renderEngine = new PhoenixEngine(); // Создаем новый экземпляр для сохранения
                        UpdateEngineParameters(); // Обновляем параметры текущего _fractalEngine

                        // Копируем параметры
                        renderEngine.MaxIterations = _fractalEngine.MaxIterations;
                        renderEngine.ThresholdSquared = _fractalEngine.ThresholdSquared;
                        renderEngine.CenterX = _fractalEngine.CenterX;
                        renderEngine.CenterY = _fractalEngine.CenterY;
                        renderEngine.Scale = _fractalEngine.Scale;
                        renderEngine.C1 = _fractalEngine.C1;
                        renderEngine.C2 = _fractalEngine.C2;
                        renderEngine.Palette = GeneratePaletteFunction(_paletteManager.ActivePalette); // Важно!
                        renderEngine.MaxColorIterations = _fractalEngine.MaxColorIterations;

                        int threadCount = GetThreadCount();

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
        #endregion

        #region Palette Management
        private void ApplyActivePalette()
        {
            if (_fractalEngine == null || _paletteManager.ActivePalette == null) return;
            _fractalEngine.Palette = GeneratePaletteFunction(_paletteManager.ActivePalette);
            _fractalEngine.MaxColorIterations = _paletteManager.ActivePalette.IsGradient ? _fractalEngine.MaxIterations : _paletteManager.ActivePalette.Colors.Count;

        }

        private Func<int, int, int, Color> GeneratePaletteFunction(PaletteManagerMandelbrotFamily palette)
        {
            if (palette.Name == "Стандартный серый")
            {
                return (iter, maxIter, maxClrIter) =>
                {
                    if (iter == maxIter) return Color.Black;
                    double tLog = Math.Log(Math.Min(iter, maxClrIter) + 1) / Math.Log(maxClrIter + 1);
                    int cVal = (int)(255.0 * (1 - tLog));
                    return Color.FromArgb(cVal, cVal, cVal);
                };
            }

            var colors = new List<Color>(palette.Colors);
            bool isGradient = palette.IsGradient;
            int colorCount = colors.Count;

            if (colorCount == 0) return (iter, max, clrMax) => Color.Black;
            if (colorCount == 1) return (iter, max, clrMax) => (iter == max) ? Color.Black : colors[0];

            return (iter, maxIter, maxColorIterationsParam) =>
            {
                if (iter == maxIter) return Color.Black;
                int actualMaxColorIter = isGradient ? maxColorIterationsParam : colorCount;

                if (isGradient)
                {
                    double t = (double)Math.Min(iter, actualMaxColorIter - 1) / (actualMaxColorIter - 1); // -1 для правильной интерполяции до последнего цвета
                    if (actualMaxColorIter <= 1) t = 0; // если всего один цвет в градиенте или меньше

                    double scaledT = t * (colorCount - 1);
                    int index1 = (int)Math.Floor(scaledT);
                    int index2 = Math.Min(index1 + 1, colorCount - 1);
                    double localT = scaledT - index1;
                    if (index1 < 0) index1 = 0; // Защита
                    if (index2 < 0) index2 = 0; // Защита
                    return LerpColor(colors[index1], colors[index2], localT);
                }
                else
                {
                    int index = Math.Min(iter, actualMaxColorIter - 1) % colorCount; // actualMaxColorIter здесь colorCount
                    if (index < 0) index = 0; // Защита
                    return colors[index];
                }
            };
        }

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

        #region Phoenix Specific UI
        private void btnSelectPhoenixParameters_Click(object sender, EventArgs e)
        {
            // Здесь будет код для открытия PhoenixCSelectorForm
            MessageBox.Show("Окно выбора параметров C1/C2 для Феникса будет реализовано позже.", "В разработке", MessageBoxButtons.OK, MessageBoxIcon.Information);
            // Пример, как это может выглядеть (закомментировано, так как PhoenixCSelectorForm еще не создан):
            /*
            if (_phoenixCSelectorWindow == null || _phoenixCSelectorWindow.IsDisposed)
            {
                _phoenixCSelectorWindow = new PhoenixCSelectorForm(this, (double)nudC1Re.Value, (double)nudC1Im.Value, (double)nudC2Re.Value, (double)nudC2Im.Value);
                _phoenixCSelectorWindow.ParametersSelected += (c1, c2) =>
                {
                    nudC1Re.Value = (decimal)c1.Real;
                    nudC1Im.Value = (decimal)c1.Imaginary;
                    nudC2Re.Value = (decimal)c2.Real;
                    nudC2Im.Value = (decimal)c2.Imaginary;
                    ScheduleRender(); // Перерисовать с новыми параметрами
                };
                _phoenixCSelectorWindow.FormClosed += (s, args) => _phoenixCSelectorWindow = null;
                _phoenixCSelectorWindow.Show(this);
            }
            else
            {
                _phoenixCSelectorWindow.Activate();
                _phoenixCSelectorWindow.SetSelectedParameters((double)nudC1Re.Value, (double)nudC1Im.Value, (double)nudC2Re.Value, (double)nudC2Im.Value);
            }
            */
        }
        #endregion

        #region Form Closing
        private void FractalPhoenixForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            _renderDebounceTimer?.Stop();
            _renderDebounceTimer?.Dispose();
            if (_previewRenderCts != null)
            {
                _previewRenderCts.Cancel();
                Thread.Sleep(50);
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
            _colorConfigForm?.Close(); // Закрываем форму настроек цвета, если она открыта
        }
        #endregion
    }
}