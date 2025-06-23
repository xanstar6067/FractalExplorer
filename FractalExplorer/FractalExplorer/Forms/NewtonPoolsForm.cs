using FractalExplorer.Engines;
using FractalExplorer.Resources;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Numerics;
using System.Threading;
using System.Drawing.Drawing2D;

namespace FractalExplorer
{
    public partial class NewtonPools : Form
    {
        // --- Компоненты ---
        private readonly NewtonFractalEngine _engine;
        private readonly System.Windows.Forms.Timer _renderDebounceTimer;
        private RenderVisualizerComponent _renderVisualizer;

        // --- Новая система палитр ---
        private NewtonPaletteManager _paletteManager;
        private color_setting_NewtonPoolsForm _colorSettingsForm;

        // --- Состояние UI и рендеринга ---
        private const double BASE_SCALE = 3.0;
        private const int TILE_SIZE = 64;
        private readonly object _bitmapLock = new object();

        private Bitmap _previewBitmap;
        private Bitmap _currentRenderingBitmap;

        private CancellationTokenSource _previewRenderCts;
        private volatile bool _isHighResRendering = false;
        private volatile bool _isRenderingPreview = false;

        private double _zoom = 1.0;
        private double _centerX = 0.0;
        private double _centerY = 0.0;

        private double _renderedCenterX;
        private double _renderedCenterY;
        private double _renderedZoom;

        private Point _panStart;
        private bool _panning = false;

        private readonly string[] presetPolynomials = {
            "z^3-1", "z^4-1", "z^5-1", "z^6-1", "z^3-2*z+2", "z^5 - z^2 + 1", "z^6 + 3*z^3 - 2",
            "z^4 - 4*z^2 + 4", "z^7 + z^4 - z + 1", "z^8 + 15*z^4 - 16", "z^4 + z^3 + z^2 + z + 1",
            "z^2 - i", "(z^2-1)*(z-2*i)", "(1+2*i)*z^2+z-1", "0.5*z^3 - 1.25*z + 2",
            "(2+i)*z^3 - (1-2*i)*z + 1", "i*z^4 + z - 1", "(1+0.5*i)*z^2 - z + (2-3*i)",
            "(0.3+1.7*i)*z^3 + (1-i)", "(2-i)*z^5 + (3+2*i)*z^2 - 1", "-2*z^3 + 0.75*z^2 - 1",
            "z^6 - 1.5*z^3 + 0.25", "-0.1*z^4 + z - 2", "(1/2)*z^3 + (3/4)*z - 1", "(2+3*i)*(z^2) - (1-i)*z + 4",
            "(z^2-1)/(z^2+1)", "(z^3-1)/(z^3+1)", "z^2 / (z-1)^2", "(z^4-1)/(z*z-2*z+1)"
        };

        public NewtonPools()
        {
            InitializeComponent();
            _engine = new NewtonFractalEngine();
            _renderDebounceTimer = new System.Windows.Forms.Timer { Interval = 300 };
            _paletteManager = new NewtonPaletteManager();
            InitializeForm();
        }

        private void InitializeForm()
        {
            _renderDebounceTimer.Tick += RenderDebounceTimer_Tick;

            if (_renderVisualizer != null)
            {
                _renderVisualizer.NeedsRedraw += OnVisualizerNeedsRedraw;
            }

            cbSelector.Items.AddRange(presetPolynomials);
            cbSelector.SelectedIndex = 0;
            richTextInput.Text = cbSelector.SelectedItem.ToString();

            int cores = Environment.ProcessorCount;
            for (int i = 1; i <= cores; i++) cbThreads.Items.Add(i);
            cbThreads.Items.Add("Auto");
            cbThreads.SelectedItem = "Auto";

            nudZoom.Minimum = 0.001M;
            nudZoom.Maximum = 1_000_000_000_000M;
            nudZoom.DecimalPlaces = 4;
            nudZoom.Value = (decimal)_zoom;

            btnConfigurePalette.Click += btnConfigurePalette_Click;

            nudIterations.ValueChanged += (s, e) => ScheduleRender();
            cbThreads.SelectedIndexChanged += (s, e) => ScheduleRender();
            nudZoom.ValueChanged += (s, e) => { _zoom = (double)nudZoom.Value; ScheduleRender(); };
            cbSelector.SelectedIndexChanged += cbSelector_SelectedIndexChanged;
            richTextInput.TextChanged += (s, e) => ScheduleRender();
            btnRender.Click += (s, e) => ScheduleRender();
            btnSave.Click += btnSave_Click;

            fractal_bitmap.MouseWheel += Canvas_MouseWheel;
            fractal_bitmap.MouseDown += Canvas_MouseDown;
            fractal_bitmap.MouseMove += Canvas_MouseMove;
            fractal_bitmap.MouseUp += Canvas_MouseUp;
            fractal_bitmap.Paint += Canvas_Paint;
            fractal_bitmap.Resize += (s, e) => { if (this.WindowState != FormWindowState.Minimized) ScheduleRender(); };

            _renderedCenterX = _centerX;
            _renderedCenterY = _centerY;
            _renderedZoom = _zoom;

            ApplyActivePalette();
            ScheduleRender();
        }

        #region New Palette Logic

        private void btnConfigurePalette_Click(object sender, EventArgs e)
        {
            if (!_engine.SetFormula(richTextInput.Text, out string _))
            {
                MessageBox.Show("Сначала введите корректную формулу, чтобы определить количество корней.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_colorSettingsForm == null || _colorSettingsForm.IsDisposed)
            {
                _colorSettingsForm = new color_setting_NewtonPoolsForm(_paletteManager);
                _colorSettingsForm.PaletteChanged += (s, palette) => {
                    _paletteManager.ActivePalette = palette;
                    ApplyActivePalette();
                    ScheduleRender();
                };
            }
            _colorSettingsForm.ShowWithRootCount(_engine.Roots.Count);
        }

        private void ApplyActivePalette()
        {
            var palette = _paletteManager.ActivePalette;
            if (palette == null) return;

            if (palette.RootColors == null || palette.RootColors.Count == 0)
            {
                _engine.RootColors = color_setting_NewtonPoolsForm.GenerateHarmonicColors(_engine.Roots.Count).ToArray();
            }
            else
            {
                _engine.RootColors = palette.RootColors.ToArray();
            }

            _engine.BackgroundColor = palette.BackgroundColor;
            _engine.UseGradient = palette.IsGradient;
        }

        #endregion

        #region Tiled Rendering Logic & Event Handlers (Unchanged)
        private void OnVisualizerNeedsRedraw()
        {
            if (fractal_bitmap.IsHandleCreated && !fractal_bitmap.IsDisposed)
            {
                fractal_bitmap.BeginInvoke((Action)(() => fractal_bitmap.Invalidate()));
            }
        }
        private void ScheduleRender()
        {
            if (_isHighResRendering || this.WindowState == FormWindowState.Minimized) return;
            if (_isRenderingPreview)
            {
                _previewRenderCts?.Cancel();
            }
            _renderDebounceTimer.Stop();
            _renderDebounceTimer.Start();
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
        private async Task StartPreviewRender()
        {
            if (fractal_bitmap.Width <= 0 || fractal_bitmap.Height <= 0) return;
            _isRenderingPreview = true;
            _previewRenderCts?.Cancel();
            _previewRenderCts = new CancellationTokenSource();
            var token = _previewRenderCts.Token;
            _renderVisualizer?.NotifyRenderSessionStart();
            if (!_engine.SetFormula(richTextInput.Text, out string debugInfo))
            {
                richTextDebugOutput.Text = debugInfo;
                lock (_bitmapLock)
                {
                    _previewBitmap?.Dispose(); _previewBitmap = null;
                    _currentRenderingBitmap?.Dispose(); _currentRenderingBitmap = null;
                }
                if (fractal_bitmap.IsHandleCreated && !fractal_bitmap.IsDisposed) fractal_bitmap.Invalidate();
                _isRenderingPreview = false;
                _renderVisualizer?.NotifyRenderSessionComplete();
                return;
            }
            richTextDebugOutput.Text = debugInfo;
            var newRenderingBitmap = new Bitmap(fractal_bitmap.Width, fractal_bitmap.Height, PixelFormat.Format32bppArgb);
            lock (_bitmapLock)
            {
                _currentRenderingBitmap?.Dispose();
                _currentRenderingBitmap = newRenderingBitmap;
            }
            UpdateEngineParameters();
            double currentRenderedCenterX = _centerX;
            double currentRenderedCenterY = _centerY;
            double currentRenderedZoom = _zoom;
            var tiles = GenerateTiles(fractal_bitmap.Width, fractal_bitmap.Height);
            var dispatcher = new TileRenderDispatcher(tiles, GetThreadCount());
            if (progressBar.IsHandleCreated && !progressBar.IsDisposed)
            {
                progressBar.Invoke((Action)(() => {
                    progressBar.Value = 0;
                    progressBar.Maximum = tiles.Count;
                }));
            }
            int progress = 0;
            try
            {
                await dispatcher.RenderAsync(async (tile, ct) =>
                {
                    ct.ThrowIfCancellationRequested();
                    _renderVisualizer?.NotifyTileRenderStart(tile.Bounds);
                    var tileBuffer = _engine.RenderSingleTile(tile, fractal_bitmap.Width, fractal_bitmap.Height, out int bytesPerPixel);
                    ct.ThrowIfCancellationRequested();
                    lock (_bitmapLock)
                    {
                        if (ct.IsCancellationRequested || _currentRenderingBitmap != newRenderingBitmap) return;
                        BitmapData bmpData = _currentRenderingBitmap.LockBits(tile.Bounds, ImageLockMode.WriteOnly, _currentRenderingBitmap.PixelFormat);
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
                    if (ct.IsCancellationRequested || !fractal_bitmap.IsHandleCreated || fractal_bitmap.IsDisposed) return;
                    fractal_bitmap.Invoke((Action)(() =>
                    {
                        if (!ct.IsCancellationRequested && progressBar.IsHandleCreated && !progressBar.IsDisposed)
                            progressBar.Value = Math.Min(progressBar.Maximum, Interlocked.Increment(ref progress));
                    }));
                    await Task.Yield();
                }, token);
                token.ThrowIfCancellationRequested();
                lock (_bitmapLock)
                {
                    if (_currentRenderingBitmap == newRenderingBitmap)
                    {
                        _previewBitmap?.Dispose();
                        _previewBitmap = new Bitmap(_currentRenderingBitmap);
                        _currentRenderingBitmap.Dispose();
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
                if (fractal_bitmap.IsHandleCreated && !fractal_bitmap.IsDisposed) fractal_bitmap.Invalidate();
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
                if (this.IsHandleCreated && !this.IsDisposed)
                    MessageBox.Show($"Ошибка рендеринга: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _isRenderingPreview = false;
                _renderVisualizer?.NotifyRenderSessionComplete();
                if (progressBar.IsHandleCreated && !progressBar.IsDisposed)
                    progressBar.Invoke((Action)(() => progressBar.Value = 0));
            }
        }
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
        private void Canvas_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.Clear(Color.Black);
            e.Graphics.InterpolationMode = InterpolationMode.Bilinear;
            lock (_bitmapLock)
            {
                if (_previewBitmap != null && fractal_bitmap.Width > 0 && fractal_bitmap.Height > 0)
                {
                    try
                    {
                        double renderedScale = BASE_SCALE / _renderedZoom;
                        double currentScale = BASE_SCALE / _zoom;
                        float drawScaleRatio = (float)(renderedScale / currentScale);
                        float newWidth = fractal_bitmap.Width * drawScaleRatio;
                        float newHeight = fractal_bitmap.Height * drawScaleRatio;
                        double deltaRe = _renderedCenterX - _centerX;
                        double deltaIm = _renderedCenterY - _centerY;
                        float offsetX = (float)(deltaRe / currentScale * fractal_bitmap.Width);
                        float offsetY = (float)(deltaIm / currentScale * fractal_bitmap.Width);
                        float drawX = (fractal_bitmap.Width - newWidth) / 2.0f + offsetX;
                        float drawY = (fractal_bitmap.Height - newHeight) / 2.0f + offsetY;
                        var destRect = new RectangleF(drawX, drawY, newWidth, newHeight);
                        e.Graphics.DrawImage(_previewBitmap, destRect);
                    }
                    catch { /* Игнорируем ошибки */ }
                }
                if (_currentRenderingBitmap != null)
                {
                    e.Graphics.DrawImageUnscaled(_currentRenderingBitmap, Point.Empty);
                }
                if (_renderVisualizer != null && _isRenderingPreview)
                {
                    _renderVisualizer.DrawVisualization(e.Graphics);
                }
            }
        }
        private void Canvas_MouseWheel(object sender, MouseEventArgs e)
        {
            if (_isHighResRendering) return;
            CommitAndBakePreview();
            double zoomFactor = e.Delta > 0 ? 1.5 : 1.0 / 1.5;
            double scaleBefore = BASE_SCALE / _zoom / fractal_bitmap.Width;
            double mouseRe = _centerX + (e.X - fractal_bitmap.Width / 2.0) * scaleBefore;
            double mouseIm = _centerY + (e.Y - fractal_bitmap.Height / 2.0) * scaleBefore;
            _zoom = Math.Max((double)nudZoom.Minimum, Math.Min((double)nudZoom.Maximum, _zoom * zoomFactor));
            double scaleAfter = BASE_SCALE / _zoom / fractal_bitmap.Width;
            _centerX = mouseRe - (e.X - fractal_bitmap.Width / 2.0) * scaleAfter;
            _centerY = mouseIm - (e.Y - fractal_bitmap.Height / 2.0) * scaleAfter;
            fractal_bitmap.Invalidate();
            if (nudZoom.Value != (decimal)_zoom) nudZoom.Value = (decimal)_zoom;
            else ScheduleRender();
        }
        private void Canvas_MouseDown(object sender, MouseEventArgs e)
        {
            if (_isHighResRendering) return;
            if (e.Button == MouseButtons.Left) { _panning = true; _panStart = e.Location; }
        }
        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isHighResRendering || !_panning) return;
            CommitAndBakePreview();
            double scale = BASE_SCALE / _zoom / fractal_bitmap.Width;
            _centerX -= (e.X - _panStart.X) * scale;
            _centerY -= (e.Y - _panStart.Y) * scale;
            _panStart = e.Location;
            fractal_bitmap.Invalidate();
            ScheduleRender();
        }
        private void Canvas_MouseUp(object sender, MouseEventArgs e)
        {
            if (_isHighResRendering) return;
            if (e.Button == MouseButtons.Left) { _panning = false; }
        }
        private void cbSelector_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbSelector.SelectedIndex >= 0)
            {
                richTextInput.Text = cbSelector.SelectedItem.ToString();
            }
        }
        #endregion

        #region Helpers and Save Logic
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
                var bakedBitmap = new Bitmap(fractal_bitmap.Width, fractal_bitmap.Height, PixelFormat.Format24bppRgb);
                using (var g = Graphics.FromImage(bakedBitmap))
                {
                    var currentRect = fractal_bitmap.ClientRectangle;
                    var paintArgs = new PaintEventArgs(g, currentRect);
                    Canvas_Paint(this, paintArgs);
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
            _engine.MaxIterations = (int)nudIterations.Value;
            _engine.CenterX = _centerX;
            _engine.CenterY = _centerY;
            _engine.Scale = BASE_SCALE / _zoom;
            ApplyActivePalette();
        }
        private int GetThreadCount() => cbThreads.SelectedItem?.ToString() == "Auto" ? Environment.ProcessorCount : Convert.ToInt32(cbThreads.SelectedItem);
        private async void btnSave_Click(object sender, EventArgs e)
        {
            if (_isHighResRendering) return;
            _isHighResRendering = true;
            SetMainControlsEnabled(false);
            int saveWidth = (int)nudW.Value;
            int saveHeight = (int)nudH.Value;
            using (var saveDialog = new SaveFileDialog { Filter = "PNG Image|*.png", FileName = $"newton_pools_{DateTime.Now:yyyyMMdd_HHmmss}.png" })
            {
                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    progressPNG.Value = 0;
                    progressPNG.Visible = true;
                    var saveEngine = new NewtonFractalEngine();
                    if (!saveEngine.SetFormula(richTextInput.Text, out _))
                    {
                        MessageBox.Show("Ошибка в формуле, сохранение отменено.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        FinalizeSave();
                        return;
                    }
                    int threadCount = GetThreadCount();
                    saveEngine.MaxIterations = (int)nudIterations.Value;
                    saveEngine.CenterX = _centerX;
                    saveEngine.CenterY = _centerY;
                    saveEngine.Scale = BASE_SCALE / _zoom;
                    ApplyActivePalette();
                    saveEngine.RootColors = _engine.RootColors;
                    saveEngine.BackgroundColor = _engine.BackgroundColor;
                    saveEngine.UseGradient = _engine.UseGradient;
                    try
                    {
                        Bitmap highResBitmap = await Task.Run(() => saveEngine.RenderToBitmap(
                            saveWidth, saveHeight,
                            threadCount,
                            progress =>
                            {
                                if (progressPNG.IsHandleCreated && !progressPNG.IsDisposed)
                                    progressPNG.Invoke((Action)(() => progressPNG.Value = Math.Min(100, progress)));
                            }
                        ));
                        highResBitmap.Save(saveDialog.FileName, ImageFormat.Png);
                        highResBitmap.Dispose();
                        MessageBox.Show("Изображение сохранено!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            FinalizeSave();
        }
        private void FinalizeSave()
        {
            _isHighResRendering = false;
            SetMainControlsEnabled(true);
            if (progressPNG.IsHandleCreated && !progressPNG.IsDisposed)
                progressPNG.Invoke((Action)(() => { progressPNG.Visible = false; progressPNG.Value = 0; }));
        }
        private void SetMainControlsEnabled(bool enabled)
        {
            Action action = () =>
            {
                panel1.Enabled = enabled;
                btnSave.Enabled = enabled;
            };
            if (this.InvokeRequired) this.Invoke(action); else action();
        }
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            _renderDebounceTimer?.Stop();
            _previewRenderCts?.Cancel();
            _previewRenderCts?.Dispose();
            _renderDebounceTimer?.Dispose();
            _colorSettingsForm?.Close();
            _colorSettingsForm?.Dispose();
            if (_renderVisualizer != null)
            {
                _renderVisualizer.NeedsRedraw -= OnVisualizerNeedsRedraw;
                _renderVisualizer.Dispose();
            }
        }
        private void NewtonPools_Load(object sender, EventArgs e)
        {
            _renderVisualizer = new RenderVisualizerComponent(TILE_SIZE);
            _renderVisualizer.NeedsRedraw += OnVisualizerNeedsRedraw;
        }
        #endregion
    }
}