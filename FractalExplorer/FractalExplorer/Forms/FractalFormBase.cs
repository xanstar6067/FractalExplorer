using FractalExplorer.Core;
using FractalExplorer.Engines;
using FractalExplorer.Projects;
using FractalExplorer.Resources;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FractalDraving
{
    public abstract partial class FractalFormBase : Form, IFractalForm
    {
        #region Fields

        private RenderVisualizerComponent _renderVisualizer;
        private PaletteManager _paletteManager;
        // ИЗМЕНЕНИЕ: Ссылка на экземпляр формы настроек
        private ColorConfigurationForm _colorConfigForm;

        private const int TILE_SIZE = 32;
        private readonly object _bitmapLock = new object();
        private Bitmap _previewBitmap;
        private Bitmap _currentRenderingBitmap;
        private CancellationTokenSource _previewRenderCts;
        private volatile bool _isHighResRendering = false;
        private volatile bool _isRenderingPreview = false;
        protected FractalMondelbrotBaseEngine _fractalEngine;
        protected decimal _zoom = 1.0m;
        protected decimal _centerX = 0.0m;
        protected decimal _centerY = 0.0m;
        private decimal _renderedCenterX;
        private decimal _renderedCenterY;
        private decimal _renderedZoom;
        private Point _panStart;
        private bool _panning = false;
        private System.Windows.Forms.Timer _renderDebounceTimer;

        #endregion

        // ... (конструктор и другие регионы остаются без изменений) ...

        #region Rendering Logic and Event Handlers

        // ИЗМЕНЕНИЕ: Логика вызова окна настроек полностью переработана
        private void color_configurations_Click(object sender, EventArgs e)
        {
            // Если форма еще не открыта или была закрыта
            if (_colorConfigForm == null || _colorConfigForm.IsDisposed)
            {
                _colorConfigForm = new ColorConfigurationForm(_paletteManager);
                // Подписываемся на событие "Палитра применена"
                _colorConfigForm.PaletteApplied += OnPaletteApplied;
                // Подписываемся на закрытие формы, чтобы обнулить ссылку
                _colorConfigForm.FormClosed += (s, args) => _colorConfigForm = null;
                // Показываем форму как немодальную, указывая родителя
                _colorConfigForm.Show(this);
            }
            else
            {
                // Если форма уже открыта, просто выводим ее на передний план
                _colorConfigForm.Activate();
            }
        }

        // НОВЫЙ МЕТОД: Обработчик события от формы настроек
        private void OnPaletteApplied(object sender, EventArgs e)
        {
            // Получив сигнал, применяем палитру и запускаем перерисовку
            ApplyActivePalette();
            ScheduleRender();
        }

        private void ApplyActivePalette()
        {
            if (_fractalEngine == null || _paletteManager.ActivePalette == null) return;
            _fractalEngine.Palette = GeneratePaletteFunction(_paletteManager.ActivePalette);
        }

        // ... (Остальные методы в этом регионе остаются без изменений) ...

        #endregion

        // --- Весь остальной код в файле остается БЕЗ ИЗМЕНЕНИЙ ---
        // (Я не буду его повторять для краткости, он идентичен предыдущему ответу)
        #region Unchanged_Code
        protected abstract FractalMondelbrotBaseEngine CreateEngine();
        protected virtual decimal BaseScale => 3.0m;
        protected virtual decimal InitialCenterX => -0.5m;
        protected virtual decimal InitialCenterY => 0.0m;
        protected virtual void UpdateEngineSpecificParameters() { }
        protected virtual void OnPostInitialize() { }
        protected virtual string GetSaveFileNameDetails() => "fractal";

        protected FractalFormBase()
        {
            InitializeComponent();
            _centerX = InitialCenterX;
            _centerY = InitialCenterY;
        }

        private void FormBase_Load(object sender, EventArgs e)
        {
            _paletteManager = new PaletteManager();
            _fractalEngine = CreateEngine();
            _renderDebounceTimer = new System.Windows.Forms.Timer { Interval = 300 };
            _renderDebounceTimer.Tick += RenderDebounceTimer_Tick;
            _renderVisualizer = new RenderVisualizerComponent(TILE_SIZE);
            _renderVisualizer.NeedsRedraw += OnVisualizerNeedsRedraw;

            HideOldPaletteControls();
            InitializeControls();
            InitializeEventHandlers();

            _renderedCenterX = _centerX;
            _renderedCenterY = _centerY;
            _renderedZoom = _zoom;
            OnPostInitialize();

            ApplyActivePalette();
            ScheduleRender();
        }

        private void HideOldPaletteControls()
        {
            var controlsToHide = new Control[] {
                colorBox, oldRenderBW, mondelbrotClassicBox,
                checkBox1, checkBox2, checkBox3, checkBox4, checkBox5, checkBox6
            };
            foreach (var control in controlsToHide)
            {
                if (control != null) control.Visible = false;
            }
        }

        private void OnVisualizerNeedsRedraw()
        {
            if (canvas.IsHandleCreated && !canvas.IsDisposed)
            {
                canvas.BeginInvoke((Action)(() => canvas.Invalidate()));
            }
        }

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
            nudZoom.DecimalPlaces = 4;
            nudZoom.Increment = 0.1m;
            nudZoom.Minimum = 0.001m;
            nudZoom.Maximum = 1_000_000_000_000_000m;
            _zoom = BaseScale / 3.0m;
            nudZoom.Value = _zoom;
            if (nudRe != null && nudIm != null)
            {
                nudRe.Minimum = -2m; nudRe.Maximum = 2m;
                nudRe.DecimalPlaces = 15; nudRe.Increment = 0.001m;
                nudRe.Value = -0.8m;
                nudIm.Minimum = -2m; nudIm.Maximum = 2m;
                nudIm.DecimalPlaces = 15; nudIm.Increment = 0.001m;
                nudIm.Value = 0.156m;
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
            btnSaveHighRes.Click += btnSave_Click_1;

            var configButton = this.Controls.Find("color_configurations", true).FirstOrDefault();
            if (configButton != null)
            {
                configButton.Click += color_configurations_Click;
            }

            canvas.MouseWheel += Canvas_MouseWheel;
            canvas.MouseDown += Canvas_MouseDown;
            canvas.MouseMove += Canvas_MouseMove;
            canvas.MouseUp += Canvas_MouseUp;
            canvas.Paint += Canvas_Paint;
            canvas.Resize += (s, e) => { if (this.WindowState != FormWindowState.Minimized) ScheduleRender(); };

            this.FormClosed += (s, e) => {
                _renderDebounceTimer?.Stop(); _renderDebounceTimer?.Dispose();
                if (_previewRenderCts != null) { _previewRenderCts.Cancel(); System.Threading.Thread.Sleep(50); _previewRenderCts.Dispose(); }
                lock (_bitmapLock) { _previewBitmap?.Dispose(); _previewBitmap = null; _currentRenderingBitmap?.Dispose(); _currentRenderingBitmap = null; }
                if (_renderVisualizer != null) { _renderVisualizer.NeedsRedraw -= OnVisualizerNeedsRedraw; _renderVisualizer.Dispose(); }
            };
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
            lock (_bitmapLock) { _currentRenderingBitmap?.Dispose(); _currentRenderingBitmap = newRenderingBitmap; }
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
            var tiles = GenerateTiles(canvas.Width, canvas.Height);
            var dispatcher = new TileRenderDispatcher(tiles, GetThreadCount());
            if (pbRenderProgress.IsHandleCreated && !pbRenderProgress.IsDisposed) { pbRenderProgress.Invoke((Action)(() => { pbRenderProgress.Value = 0; pbRenderProgress.Maximum = tiles.Count; })); }
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
                    canvas.Invoke((Action)(() => { if (!ct.IsCancellationRequested && pbRenderProgress.IsHandleCreated && !pbRenderProgress.IsDisposed) { pbRenderProgress.Value = Math.Min(pbRenderProgress.Maximum, Interlocked.Increment(ref progress)); } }));
                    await Task.Yield();
                }, token);
                token.ThrowIfCancellationRequested();
                lock (_bitmapLock)
                {
                    if (_currentRenderingBitmap == newRenderingBitmap) { _previewBitmap?.Dispose(); _previewBitmap = _currentRenderingBitmap; _currentRenderingBitmap = null; _renderedCenterX = currentRenderedCenterX; _renderedCenterY = currentRenderedCenterY; _renderedZoom = currentRenderedZoom; }
                    else { newRenderingBitmap?.Dispose(); }
                }
                if (canvas.IsHandleCreated && !canvas.IsDisposed) canvas.Invalidate();
            }
            catch (OperationCanceledException) { lock (_bitmapLock) { if (_currentRenderingBitmap == newRenderingBitmap) { _currentRenderingBitmap?.Dispose(); _currentRenderingBitmap = null; } } newRenderingBitmap?.Dispose(); }
            catch (Exception ex) { newRenderingBitmap?.Dispose(); if (this.IsHandleCreated && !this.IsDisposed) { MessageBox.Show($"Ошибка рендеринга: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
            finally { _isRenderingPreview = false; _renderVisualizer?.NotifyRenderSessionComplete(); if (pbRenderProgress.IsHandleCreated && !pbRenderProgress.IsDisposed) { pbRenderProgress.Invoke((Action)(() => pbRenderProgress.Value = 0)); } }
        }

        private List<TileInfo> GenerateTiles(int width, int height)
        {
            var tiles = new List<TileInfo>();
            Point center = new Point(width / 2, height / 2);
            for (int y = 0; y < height; y += TILE_SIZE) { for (int x = 0; x < width; x += TILE_SIZE) { tiles.Add(new TileInfo(x, y, TILE_SIZE, TILE_SIZE)); } }
            return tiles.OrderBy(t => Math.Pow(t.Center.X - center.X, 2) + Math.Pow(t.Center.Y - center.Y, 2)).ToList();
        }

        private void Canvas_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.Clear(Color.Black);
            lock (_bitmapLock)
            {
                if (_previewBitmap != null && canvas.Width > 0 && canvas.Height > 0)
                {
                    if (_renderedCenterX == _centerX && _renderedCenterY == _centerY && _renderedZoom == _zoom) { e.Graphics.DrawImageUnscaled(_previewBitmap, Point.Empty); }
                    else
                    {
                        try
                        {
                            decimal renderedComplexWidth = BaseScale / _renderedZoom; decimal currentComplexWidth = BaseScale / _zoom;
                            if (!(_renderedZoom <= 0 || _zoom <= 0 || renderedComplexWidth <= 0 || currentComplexWidth <= 0))
                            {
                                decimal units_per_pixel_rendered = renderedComplexWidth / _previewBitmap.Width; decimal units_per_pixel_current = currentComplexWidth / canvas.Width;
                                decimal rendered_re_min = _renderedCenterX - (renderedComplexWidth / 2.0m); decimal rendered_im_max = _renderedCenterY + (_previewBitmap.Height * units_per_pixel_rendered / 2.0m);
                                decimal current_re_min = _centerX - (currentComplexWidth / 2.0m); decimal current_im_max = _centerY + (canvas.Height * units_per_pixel_current / 2.0m);
                                decimal offsetX_pixels = (rendered_re_min - current_re_min) / units_per_pixel_current; decimal offsetY_pixels = (current_im_max - rendered_im_max) / units_per_pixel_current;
                                decimal newWidth_pixels = _previewBitmap.Width * (units_per_pixel_rendered / units_per_pixel_current); decimal newHeight_pixels = _previewBitmap.Height * (units_per_pixel_rendered / units_per_pixel_current);
                                PointF destPoint1 = new PointF((float)offsetX_pixels, (float)offsetY_pixels); PointF destPoint2 = new PointF((float)(offsetX_pixels + newWidth_pixels), (float)offsetY_pixels); PointF destPoint3 = new PointF((float)offsetX_pixels, (float)(offsetY_pixels + newHeight_pixels));
                                e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Bilinear;
                                e.Graphics.DrawImage(_previewBitmap, new PointF[] { destPoint1, destPoint2, destPoint3 });
                            }
                        }
                        catch (Exception) { /* Игнорируем */ }
                    }
                }
                if (_currentRenderingBitmap != null) { e.Graphics.DrawImageUnscaled(_currentRenderingBitmap, Point.Empty); }
            }
            if (_renderVisualizer != null && _isRenderingPreview) { _renderVisualizer.DrawVisualization(e.Graphics); }
        }
        private void ScheduleRender() { if (_isHighResRendering || this.WindowState == FormWindowState.Minimized) return; if (_isRenderingPreview) { _previewRenderCts?.Cancel(); } _renderDebounceTimer.Stop(); _renderDebounceTimer.Start(); }
        private async void RenderDebounceTimer_Tick(object sender, EventArgs e) { _renderDebounceTimer.Stop(); if (_isHighResRendering || _isRenderingPreview) { ScheduleRender(); return; } await StartPreviewRender(); }
        private void ParamControl_Changed(object sender, EventArgs e) { if (_isHighResRendering) return; if (sender == nudZoom) { _zoom = nudZoom.Value; } ScheduleRender(); }
        private void Canvas_MouseWheel(object sender, MouseEventArgs e) { if (_isHighResRendering) return; CommitAndBakePreview(); decimal zoomFactor = e.Delta > 0 ? 1.5m : 1.0m / 1.5m; decimal scaleBeforeZoom = BaseScale / _zoom; decimal mouseRe = _centerX + (e.X - canvas.Width / 2.0m) * scaleBeforeZoom / canvas.Width; decimal mouseIm = _centerY - (e.Y - canvas.Height / 2.0m) * scaleBeforeZoom / canvas.Height; _zoom = Math.Max(nudZoom.Minimum, Math.Min(nudZoom.Maximum, _zoom * zoomFactor)); decimal scaleAfterZoom = BaseScale / _zoom; _centerX = mouseRe - (e.X - canvas.Width / 2.0m) * scaleAfterZoom / canvas.Width; _centerY = mouseIm + (e.Y - canvas.Height / 2.0m) * scaleAfterZoom / canvas.Height; canvas.Invalidate(); if (nudZoom.Value != _zoom) { nudZoom.Value = _zoom; } else { ScheduleRender(); } }
        private void Canvas_MouseDown(object sender, MouseEventArgs e) { if (_isHighResRendering) return; if (e.Button == MouseButtons.Left) { _panning = true; _panStart = e.Location; canvas.Cursor = Cursors.Hand; } }
        private void Canvas_MouseMove(object sender, MouseEventArgs e) { if (_isHighResRendering || !_panning) return; CommitAndBakePreview(); decimal units_per_pixel = BaseScale / _zoom / canvas.Width; _centerX -= (decimal)(e.X - _panStart.X) * units_per_pixel; _centerY += (decimal)(e.Y - _panStart.Y) * units_per_pixel; _panStart = e.Location; canvas.Invalidate(); ScheduleRender(); }
        private void Canvas_MouseUp(object sender, MouseEventArgs e) { if (_isHighResRendering) return; if (e.Button == MouseButtons.Left) { _panning = false; canvas.Cursor = Cursors.Default; } }

        #region New Palette Logic

        private Func<int, int, int, Color> GeneratePaletteFunction(FractalExplorer.Core.ColorPalette palette)
        {
            if (palette.Name == "Стандартный серый")
            {
                return (iter, maxIter, maxClrIter) =>
                {
                    if (iter == maxIter) return Color.Black;
                    double t_log = Math.Log(Math.Min(iter, maxClrIter) + 1) / Math.Log(maxClrIter + 1);
                    int cVal = (int)(255.0 * (1 - t_log));
                    return Color.FromArgb(cVal, cVal, cVal);
                };
            }

            var colors = new List<Color>(palette.Colors);
            bool isGradient = palette.IsGradient;
            int colorCount = colors.Count;

            if (colorCount == 0) return (iter, max, clrMax) => Color.Black;
            if (colorCount == 1) return (iter, max, clrMax) => (iter == max) ? Color.Black : colors[0];

            return (iter, maxIter, maxClrIter) =>
            {
                if (iter == maxIter) return Color.Black;

                if (isGradient)
                {
                    double t = (double)Math.Min(iter, maxClrIter) / maxClrIter;
                    double scaled_t = t * (colorCount - 1);
                    int index1 = (int)Math.Floor(scaled_t);
                    int index2 = Math.Min(index1 + 1, colorCount - 1);
                    double local_t = scaled_t - index1;
                    return LerpColor(colors[index1], colors[index2], local_t);
                }
                else
                {
                    int index = Math.Min(iter, maxClrIter) % colorCount;
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

        #region Helpers

        private void CommitAndBakePreview()
        {
            lock (_bitmapLock) { if (!_isRenderingPreview || _currentRenderingBitmap == null) { return; } }
            _previewRenderCts?.Cancel();
            lock (_bitmapLock)
            {
                if (_currentRenderingBitmap == null) return;
                var bakedBitmap = new Bitmap(canvas.Width, canvas.Height, PixelFormat.Format24bppRgb);
                using (var g = Graphics.FromImage(bakedBitmap))
                {
                    g.Clear(Color.Black); g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Bilinear;
                    if (_previewBitmap != null)
                    {
                        try
                        {
                            decimal renderedComplexWidth = BaseScale / _renderedZoom; decimal currentComplexWidth = BaseScale / _zoom;
                            if (!(_renderedZoom <= 0 || _zoom <= 0 || renderedComplexWidth <= 0 || currentComplexWidth <= 0))
                            {
                                decimal units_per_pixel_rendered = renderedComplexWidth / _previewBitmap.Width; decimal units_per_pixel_current = currentComplexWidth / canvas.Width;
                                decimal rendered_re_min = _renderedCenterX - (renderedComplexWidth / 2.0m); decimal rendered_im_max = _renderedCenterY + (_previewBitmap.Height * units_per_pixel_rendered / 2.0m);
                                decimal current_re_min = _centerX - (currentComplexWidth / 2.0m); decimal current_im_max = _centerY + (canvas.Height * units_per_pixel_current / 2.0m);
                                decimal offsetX_pixels = (rendered_re_min - current_re_min) / units_per_pixel_current; decimal offsetY_pixels = (current_im_max - rendered_im_max) / units_per_pixel_current;
                                decimal newWidth_pixels = _previewBitmap.Width * (units_per_pixel_rendered / units_per_pixel_current); decimal newHeight_pixels = _previewBitmap.Height * (units_per_pixel_rendered / units_per_pixel_current);
                                PointF destPoint1 = new PointF((float)offsetX_pixels, (float)offsetY_pixels); PointF destPoint2 = new PointF((float)(offsetX_pixels + newWidth_pixels), (float)offsetY_pixels); PointF destPoint3 = new PointF((float)offsetX_pixels, (float)(offsetY_pixels + newHeight_pixels));
                                g.DrawImage(_previewBitmap, new PointF[] { destPoint1, destPoint2, destPoint3 });
                            }
                        }
                        catch (Exception) { /* Игнорируем */ }
                    }
                    g.DrawImageUnscaled(_currentRenderingBitmap, Point.Empty);
                }
                _previewBitmap?.Dispose(); _previewBitmap = bakedBitmap;
                _currentRenderingBitmap.Dispose(); _currentRenderingBitmap = null;
                _renderedCenterX = _centerX; _renderedCenterY = _centerY; _renderedZoom = _zoom;
            }
        }
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

        private int GetThreadCount() { return cbThreads.SelectedItem?.ToString() == "Auto" ? Environment.ProcessorCount : Convert.ToInt32(cbThreads.SelectedItem); }

        private async void btnSave_Click_1(object sender, EventArgs e)
        {
            if (_isHighResRendering) { MessageBox.Show("Процесс сохранения уже запущен.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            int saveWidth = (int)nudSaveWidth.Value; int saveHeight = (int)nudSaveHeight.Value;
            string fractalDetails = GetSaveFileNameDetails(); string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string suggestedFileName = $"{fractalDetails}_{timestamp}.png";
            using (var saveDialog = new SaveFileDialog { Filter = "PNG Image|*.png", Title = "Сохранить фрактал (Высокое разрешение)", FileName = suggestedFileName })
            {
                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    if (_isRenderingPreview) { _previewRenderCts?.Cancel(); }
                    _isHighResRendering = true; pnlControls.Enabled = false;
                    pbHighResProgress.Value = 0; pbHighResProgress.Visible = true;
                    try
                    {
                        FractalMondelbrotBaseEngine renderEngine = CreateEngine();
                        UpdateEngineParameters();
                        renderEngine.MaxIterations = _fractalEngine.MaxIterations;
                        renderEngine.ThresholdSquared = _fractalEngine.ThresholdSquared;
                        renderEngine.CenterX = _fractalEngine.CenterX;
                        renderEngine.CenterY = _fractalEngine.CenterY;
                        renderEngine.Scale = _fractalEngine.Scale;
                        if (this is FractalJulia || this is FractalJuliaBurningShip) { renderEngine.C = new ComplexDecimal(nudRe.Value, nudIm.Value); }
                        else { renderEngine.C = _fractalEngine.C; }

                        renderEngine.Palette = GeneratePaletteFunction(_paletteManager.ActivePalette);
                        renderEngine.MaxColorIterations = _fractalEngine.MaxColorIterations;
                        int threadCount = GetThreadCount();
                        Bitmap highResBitmap = await Task.Run(() => renderEngine.RenderToBitmap(
                            saveWidth, saveHeight, threadCount,
                            progress => { if (pbHighResProgress.IsHandleCreated && !pbHighResProgress.IsDisposed) { pbHighResProgress.Invoke((Action)(() => { pbHighResProgress.Value = Math.Min(pbHighResProgress.Maximum, progress); })); } }
                        ));
                        highResBitmap.Save(saveDialog.FileName, ImageFormat.Png);
                        highResBitmap.Dispose();
                        MessageBox.Show("Изображение успешно сохранено!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex) { MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                    finally { _isHighResRendering = false; pnlControls.Enabled = true; if (pbHighResProgress.IsHandleCreated && !pbHighResProgress.IsDisposed) { pbHighResProgress.Invoke((Action)(() => { pbHighResProgress.Visible = false; pbHighResProgress.Value = 0; })); } ScheduleRender(); }
                }
            }
        }
        #endregion

        #region IFractalForm Implementation
        public double LoupeZoom => nudBaseScale != null ? (double)nudBaseScale.Value : 4.0;
        public event EventHandler LoupeZoomChanged;
        #endregion
        #endregion
    }
}