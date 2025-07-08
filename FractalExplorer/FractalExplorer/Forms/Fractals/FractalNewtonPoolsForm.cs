using FractalExplorer.Engines;
using FractalExplorer.Forms.Other;
using FractalExplorer.Resources;
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
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FractalExplorer
{
    /// <summary>
    /// Форма для отображения и взаимодействия с фракталами Ньютона.
    /// Позволяет задавать комплексные функции, находить их корни и визуализировать
    /// бассейны притяжения этих корней с различными настройками палитры.
    /// </summary>
    public partial class NewtonPools : Form, ISaveLoadCapableFractal, IHighResRenderable
    {
        #region Fields

        /// <summary>
        /// Экземпляр движка для рендеринга фрактала Ньютона.
        /// </summary>
        private readonly FractalNewtonEngine _engine;

        /// <summary>
        /// Таймер для отложенного запуска рендеринга, чтобы избежать частых перерисовок.
        /// </summary>
        private readonly System.Windows.Forms.Timer _renderDebounceTimer;

        /// <summary>
        /// Компонент для визуализации процесса рендеринга плиток.
        /// </summary>
        private RenderVisualizerComponent _renderVisualizer;

        /// <summary>
        /// Менеджер палитр, специфичный для фракталов Ньютона.
        /// </summary>
        private NewtonPaletteManager _paletteManager;

        /// <summary>
        /// Форма для настройки цветовой палитры фрактала Ньютона.
        /// </summary>
        private ColorConfigurationNewtonPoolsForm _colorSettingsForm;

        /// <summary>
        /// Базовый масштаб для преобразования мировых координат в экранные.
        /// </summary>
        private const double BASE_SCALE = 3.0;

        /// <summary>
        /// Размер одной плитки (тайла) для пошагового рендеринга.
        /// </summary>
        private const int TILE_SIZE = 32;

        /// <summary>
        /// Объект для блокировки доступа к битмапам во время рендеринга.
        /// </summary>
        private readonly object _bitmapLock = new object();

        /// <summary>
        /// Битмап, содержащий отрисованное изображение для предпросмотра.
        /// </summary>
        private Bitmap _previewBitmap;

        /// <summary>
        /// Битмап, в который текущий момент происходит рендеринг плиток.
        /// </summary>
        private Bitmap _currentRenderingBitmap;

        /// <summary>
        /// Токен отмены для операций рендеринга предпросмотра.
        /// </summary>
        private CancellationTokenSource _previewRenderCts;

        /// <summary>
        /// Флаг, указывающий, выполняется ли сейчас рендеринг в высоком разрешении.
        /// </summary>
        private volatile bool _isHighResRendering = false;

        /// <summary>
        /// Флаг, указывающий, выполняется ли сейчас рендеринг предпросмотра.
        /// </summary>
        private volatile bool _isRenderingPreview = false;

        /// <summary>
        /// Текущий коэффициент масштабирования фрактала.
        /// </summary>
        private double _zoom = 1.0;

        /// <summary>
        /// Текущая координата X центра видимой области фрактала.
        /// </summary>
        private double _centerX = 0.0;

        /// <summary>
        /// Текущая координата Y центра видимой области фрактала.
        /// </summary>
        private double _centerY = 0.0;

        /// <summary>
        /// Координата X центра, по которой был отрисован _previewBitmap.
        /// Используется для интерполяции при панорамировании/масштабировании.
        /// </summary>
        private double _renderedCenterX;

        /// <summary>
        /// Координата Y центра, по которой был отрисован _previewBitmap.
        /// Используется для интерполяции при панорамировании/масштабировании.
        /// </summary>
        private double _renderedCenterY;

        /// <summary>
        /// Коэффициент масштабирования, по которому был отрисован _previewBitmap.
        /// Используется для интерполяции при панорамировании/масштабировании.
        /// </summary>
        private double _renderedZoom;

        /// <summary>
        /// Начальная позиция курсора мыши при панорамировании.
        /// </summary>
        private Point _panStart;

        /// <summary>
        /// Флаг, указывающий, находится ли пользователь в режиме панорамирования.
        /// </summary>
        private bool _panning = false;

        private string _baseTitle;
        /// <summary>
        /// Массив предустановленных полиномиальных формул для выбора.
        /// </summary>
        private readonly string[] presetPolynomials =
        {
            "z^3-1", "z^4-1", "z^5-1", "z^6-1", "z^3-2*z+2", "z^5 - z^2 + 1",
            "z^6 + 3*z^3 - 2", "z^4 - 4*z^2 + 4", "z^7 + z^4 - z + 1", "z^8 + 15*z^4 - 16",
            "z^4 + z^3 + z^2 + z + 1", "z^2 - i", "(z^2-1)*(z-2*i)", "(1+2*i)*z^2+z-1",
            "0.5*z^3 - 1.25*z + 2", "(2+i)*z^3 - (1-2*i)*z + 1", "i*z^4 + z - 1",
            "(1+0.5*i)*z^2 - z + (2-3*i)", "(0.3+1.7*i)*z^3 + (1-i)",
            "(2-i)*z^5 + (3+2*i)*z^2 - 1", "-2*z^3 + 0.75*z^2 - 1", "z^6 - 1.5*z^3 + 0.25",
            "-0.1*z^4 + z - 2", "(1/2)*z^3 + (3/4)*z - 1", "(2+3*i)*(z^2) - (1-i)*z + 4",
            "(z^2-1)/(z^2+1)", "(z^3-1)/(z^3+1)", "z^2 / (z-1)^2", "(z^4-1)/(z*z-2*z+1)"
        };

        #endregion

        #region Constructor

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="NewtonPools"/>.
        /// </summary>
        public NewtonPools()
        {
            InitializeComponent();
            _engine = new FractalNewtonEngine();
            _renderDebounceTimer = new System.Windows.Forms.Timer { Interval = 300 };
            _paletteManager = new NewtonPaletteManager();
            InitializeForm();
        }

        #endregion

        #region UI Initialization

        /// <summary>
        /// Выполняет инициализацию элементов управления формы и подписку на события.
        /// </summary>
        private void InitializeForm()
        {
            _baseTitle = this.Text;
            _renderDebounceTimer.Tick += RenderDebounceTimer_Tick;

            _renderVisualizer = new RenderVisualizerComponent(TILE_SIZE);
            _renderVisualizer.NeedsRedraw += OnVisualizerNeedsRedraw;

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

            // TODO: Закомментируйте или удалите старую кнопку сохранения и ее обработчик в дизайнере.
            // btnSave.Click += btnSave_Click; 

            // TODO: Добавьте новую кнопку "Менеджер сохранения" в дизайнере и привяжите к ней этот обработчик.
            // this.btnOpenSaveManager.Click += new System.EventHandler(this.btnOpenSaveManager_Click);


            fractal_bitmap.MouseWheel += Canvas_MouseWheel;
            fractal_bitmap.MouseDown += Canvas_MouseDown;
            fractal_bitmap.MouseMove += Canvas_MouseMove;
            fractal_bitmap.MouseUp += Canvas_MouseUp;
            fractal_bitmap.Paint += Canvas_Paint;
            fractal_bitmap.Resize += (s, e) => { if (WindowState != FormWindowState.Minimized) ScheduleRender(); };

            _renderedCenterX = _centerX;
            _renderedCenterY = _centerY;
            _renderedZoom = _zoom;

            ApplyActivePalette();
            ScheduleRender();
        }

        #endregion

        #region Palette Management

        private void btnConfigurePalette_Click(object sender, EventArgs e)
        {
            if (!_engine.SetFormula(richTextInput.Text, out string _))
            {
                MessageBox.Show("Сначала введите корректную формулу, чтобы определить количество корней.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_colorSettingsForm == null || _colorSettingsForm.IsDisposed)
            {
                _colorSettingsForm = new ColorConfigurationNewtonPoolsForm(_paletteManager);
                _colorSettingsForm.PaletteChanged += (s, palette) =>
                {
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
                _engine.RootColors = ColorConfigurationNewtonPoolsForm.GenerateHarmonicColors(_engine.Roots.Count).ToArray();
            }
            else
            {
                _engine.RootColors = palette.RootColors.ToArray();
            }

            _engine.BackgroundColor = palette.BackgroundColor;
            _engine.UseGradient = palette.IsGradient;
        }

        #endregion

        #region Rendering Logic

        private void OnVisualizerNeedsRedraw()
        {
            if (fractal_bitmap.IsHandleCreated && !fractal_bitmap.IsDisposed)
            {
                fractal_bitmap.BeginInvoke((Action)(() => fractal_bitmap.Invalidate()));
            }
        }

        private void ScheduleRender()
        {
            if (_isHighResRendering || WindowState == FormWindowState.Minimized) return;
            if (_isRenderingPreview) _previewRenderCts?.Cancel();
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

            var stopwatch = Stopwatch.StartNew();
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
                progressBar.Invoke((Action)(() => { progressBar.Value = 0; progressBar.Maximum = tiles.Count; }));
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
                        BitmapData bitmapData = _currentRenderingBitmap.LockBits(tile.Bounds, ImageLockMode.WriteOnly, _currentRenderingBitmap.PixelFormat);
                        int tileWidthInBytes = tile.Bounds.Width * bytesPerPixel;
                        for (int y = 0; y < tile.Bounds.Height; y++)
                        {
                            IntPtr destinationPointer = IntPtr.Add(bitmapData.Scan0, y * bitmapData.Stride);
                            int sourceOffset = y * tileWidthInBytes;
                            Marshal.Copy(tileBuffer, sourceOffset, destinationPointer, tileWidthInBytes);
                        }
                        _currentRenderingBitmap.UnlockBits(bitmapData);
                    }

                    _renderVisualizer?.NotifyTileRenderComplete(tile.Bounds);
                    if (ct.IsCancellationRequested || !fractal_bitmap.IsHandleCreated || fractal_bitmap.IsDisposed) return;

                    fractal_bitmap.Invoke((Action)(() => { if (!ct.IsCancellationRequested && progressBar.IsHandleCreated && !progressBar.IsDisposed) progressBar.Value = Math.Min(progressBar.Maximum, Interlocked.Increment(ref progress)); }));
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
                if (progressBar.IsHandleCreated && !progressBar.IsDisposed) progressBar.Invoke((Action)(() => progressBar.Value = 0));
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

        #endregion

        #region Canvas Interaction

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

                        double deltaReal = _renderedCenterX - _centerX;
                        double deltaImaginary = _renderedCenterY - _centerY;

                        float offsetX = (float)(deltaReal / currentScale * fractal_bitmap.Width);
                        float offsetY = (float)(deltaImaginary / currentScale * fractal_bitmap.Width);

                        float drawX = (fractal_bitmap.Width - newWidth) / 2.0f + offsetX;
                        float drawY = (fractal_bitmap.Height - newHeight) / 2.0f + offsetY;

                        var destinationRectangle = new RectangleF(drawX, drawY, newWidth, newHeight);
                        e.Graphics.DrawImage(_previewBitmap, destinationRectangle);
                    }
                    catch (Exception) { }
                }

                if (_currentRenderingBitmap != null) e.Graphics.DrawImageUnscaled(_currentRenderingBitmap, Point.Empty);

                if (_renderVisualizer != null && _isRenderingPreview) _renderVisualizer.DrawVisualization(e.Graphics);
            }
        }

        private void Canvas_MouseWheel(object sender, MouseEventArgs e)
        {
            if (_isHighResRendering) return;

            CommitAndBakePreview();

            double zoomFactor = e.Delta > 0 ? 1.5 : 1.0 / 1.5;
            double scaleBefore = BASE_SCALE / _zoom / fractal_bitmap.Width;

            double mouseReal = _centerX + (e.X - fractal_bitmap.Width / 2.0) * scaleBefore;
            double mouseImaginary = _centerY + (e.Y - fractal_bitmap.Height / 2.0) * scaleBefore;

            _zoom = Math.Max((double)nudZoom.Minimum, Math.Min((double)nudZoom.Maximum, _zoom * zoomFactor));

            double scaleAfter = BASE_SCALE / _zoom / fractal_bitmap.Width;
            _centerX = mouseReal - (e.X - fractal_bitmap.Width / 2.0) * scaleAfter;
            _centerY = mouseImaginary - (e.Y - fractal_bitmap.Height / 2.0) * scaleAfter;

            fractal_bitmap.Invalidate();

            if (nudZoom.Value != (decimal)_zoom) nudZoom.Value = (decimal)_zoom;
            else ScheduleRender();
        }

        private void Canvas_MouseDown(object sender, MouseEventArgs e)
        {
            if (_isHighResRendering) return;
            if (e.Button == MouseButtons.Left)
            {
                _panning = true;
                _panStart = e.Location;
            }
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
            if (e.Button == MouseButtons.Left) _panning = false;
        }

        private void cbSelector_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbSelector.SelectedIndex >= 0) richTextInput.Text = cbSelector.SelectedItem.ToString();
        }

        #endregion

        #region Utility Methods

        private void CommitAndBakePreview()
        {
            lock (_bitmapLock) { if (!_isRenderingPreview || _currentRenderingBitmap == null) return; }

            _previewRenderCts?.Cancel();

            lock (_bitmapLock)
            {
                if (_currentRenderingBitmap == null) return;

                var bakedBitmap = new Bitmap(fractal_bitmap.Width, fractal_bitmap.Height, PixelFormat.Format24bppRgb);
                using (var graphics = Graphics.FromImage(bakedBitmap))
                {
                    graphics.Clear(Color.Black);
                    graphics.InterpolationMode = InterpolationMode.Bilinear;

                    if (_previewBitmap != null)
                    {
                        try
                        {
                            double renderedScale = BASE_SCALE / _renderedZoom;
                            double currentScale = BASE_SCALE / _zoom;
                            float drawScaleRatio = (float)(renderedScale / currentScale);
                            float newWidth = fractal_bitmap.Width * drawScaleRatio;
                            float newHeight = fractal_bitmap.Height * drawScaleRatio;
                            double deltaReal = _renderedCenterX - _centerX;
                            double deltaImaginary = _renderedCenterY - _centerY;
                            float offsetX = (float)(deltaReal / currentScale * fractal_bitmap.Width);
                            float offsetY = (float)(deltaImaginary / currentScale * fractal_bitmap.Width);
                            float drawX = (fractal_bitmap.Width - newWidth) / 2.0f + offsetX;
                            float drawY = (fractal_bitmap.Height - newHeight) / 2.0f + offsetY;
                            var destinationRectangle = new RectangleF(drawX, drawY, newWidth, newHeight);
                            graphics.DrawImage(_previewBitmap, destinationRectangle);
                        }
                        catch (Exception) { }
                    }

                    if (_currentRenderingBitmap != null) graphics.DrawImageUnscaled(_currentRenderingBitmap, Point.Empty);
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

        private int GetThreadCount()
        {
            return cbThreads.SelectedItem?.ToString() == "Auto" ? Environment.ProcessorCount : Convert.ToInt32(cbThreads.SelectedItem);
        }

        /// <summary>
        /// Открывает менеджер сохранения изображений.
        /// TODO: Привязать этот обработчик к новой кнопке в дизайнере.
        /// </summary>
        private void btnOpenSaveManager_Click(object sender, EventArgs e)
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

        /*
        // Старый метод сохранения, заменен на менеджер изображений.
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
                    progressBar.Value = 0;
                    progressBar.Visible = true;

                    var saveEngine = new FractalNewtonEngine();
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
                        var stopwatch = Stopwatch.StartNew();
                        Bitmap highResBitmap = await Task.Run(() => saveEngine.RenderToBitmap(
                            saveWidth, saveHeight,
                            threadCount,
                            progress => {
                                if (progressPNG.IsHandleCreated && !progressPNG.IsDisposed) {
                                    progressPNG.Invoke((Action)(() => progressPNG.Value = Math.Min(100, progress)));
                                }
                            }
                        ));
                        stopwatch.Stop(); 

                        highResBitmap.Save(saveDialog.FileName, ImageFormat.Png);
                        highResBitmap.Dispose();
                        double elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
                        MessageBox.Show($"Изображение сохранено!\nВремя рендеринга: {elapsedSeconds:F3} сек.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            {
                progressPNG.Invoke((Action)(() => { progressPNG.Visible = false; progressPNG.Value = 0; }));
            }
        }
        */

        private void SetMainControlsEnabled(bool enabled)
        {
            Action action = () =>
            {
                panel1.Enabled = enabled;
                // btnSave.Enabled = enabled; // Управляется отдельно
            };
            if (InvokeRequired) Invoke(action);
            else action();
        }

        #endregion

        #region Form Lifecycle

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

        #endregion

        #region ISaveLoadCapableFractal Implementation

        public string FractalTypeIdentifier => "NewtonPools";

        public Type ConcreteSaveStateType => typeof(NewtonSaveState);

        public class NewtonPreviewParams
        {
            public string Formula { get; set; }
            public decimal CenterX { get; set; }
            public decimal CenterY { get; set; }
            public decimal Zoom { get; set; }
            public int Iterations { get; set; }
            public NewtonColorPalette PaletteSnapshot { get; set; }
        }

        public FractalSaveStateBase GetCurrentStateForSave(string saveName)
        {
            _paletteManager.ActivePalette.BackgroundColor = _engine.BackgroundColor;
            _paletteManager.ActivePalette.IsGradient = _engine.UseGradient;
            _paletteManager.ActivePalette.RootColors = new List<Color>(_engine.RootColors);

            var state = new NewtonSaveState(this.FractalTypeIdentifier)
            {
                SaveName = saveName,
                Timestamp = DateTime.Now,
                Formula = richTextInput.Text,
                CenterX = (decimal)_centerX,
                CenterY = (decimal)_centerY,
                Zoom = (decimal)_zoom,
                Iterations = (int)nudIterations.Value,
                PaletteSnapshot = _paletteManager.ActivePalette
            };

            var previewParams = new NewtonPreviewParams
            {
                Formula = state.Formula,
                CenterX = state.CenterX,
                CenterY = state.CenterY,
                Zoom = state.Zoom,
                Iterations = Math.Min(state.Iterations, 50),
                PaletteSnapshot = state.PaletteSnapshot
            };

            var jsonOptions = new JsonSerializerOptions();
            jsonOptions.Converters.Add(new Utilities.JsonConverters.JsonColorConverter());
            state.PreviewParametersJson = JsonSerializer.Serialize(previewParams, jsonOptions);

            return state;
        }

        public void LoadState(FractalSaveStateBase stateBase)
        {
            if (stateBase is NewtonSaveState state)
            {
                _isRenderingPreview = false;
                _previewRenderCts?.Cancel();
                _renderDebounceTimer.Stop();

                _centerX = (double)state.CenterX;
                _centerY = (double)state.CenterY;
                _zoom = (double)state.Zoom;

                richTextInput.Text = state.Formula;
                nudZoom.Value = (decimal)_zoom;
                nudIterations.Value = state.Iterations;

                _paletteManager.ActivePalette = state.PaletteSnapshot;

                UpdateEngineParameters();

                lock (_bitmapLock)
                {
                    _previewBitmap?.Dispose(); _previewBitmap = null;
                    _currentRenderingBitmap?.Dispose(); _currentRenderingBitmap = null;
                }

                _renderedCenterX = _centerX;
                _renderedCenterY = _centerY;
                _renderedZoom = _zoom;

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
                if (string.IsNullOrEmpty(state.PreviewParametersJson)) return new byte[tile.Bounds.Width * tile.Bounds.Height * 4];

                NewtonPreviewParams previewParams;
                try
                {
                    var jsonOptions = new JsonSerializerOptions();
                    jsonOptions.Converters.Add(new Utilities.JsonConverters.JsonColorConverter());
                    previewParams = JsonSerializer.Deserialize<NewtonPreviewParams>(state.PreviewParametersJson, jsonOptions);
                }
                catch { return new byte[tile.Bounds.Width * tile.Bounds.Height * 4]; }

                var previewEngine = new FractalNewtonEngine();
                if (!previewEngine.SetFormula(previewParams.Formula, out _)) return new byte[tile.Bounds.Width * tile.Bounds.Height * 4];

                previewEngine.CenterX = (double)previewParams.CenterX;
                previewEngine.CenterY = (double)previewParams.CenterY;
                previewEngine.Scale = 3.0 / (double)previewParams.Zoom;
                previewEngine.MaxIterations = 150;

                var palette = previewParams.PaletteSnapshot;
                previewEngine.BackgroundColor = palette.BackgroundColor;
                previewEngine.UseGradient = palette.IsGradient;
                previewEngine.RootColors = (palette.RootColors != null && palette.RootColors.Any()) ? palette.RootColors.ToArray() : ColorConfigurationNewtonPoolsForm.GenerateHarmonicColors(previewEngine.Roots.Count).ToArray();

                return previewEngine.RenderSingleTile(tile, totalWidth, totalHeight, out _);
            });
        }

        public Bitmap RenderPreview(FractalSaveStateBase state, int previewWidth, int previewHeight)
        {
            if (string.IsNullOrEmpty(state.PreviewParametersJson))
            {
                var bmpError = new Bitmap(previewWidth, previewHeight);
                using (var g = Graphics.FromImage(bmpError)) { g.Clear(Color.DarkGray); TextRenderer.DrawText(g, "Нет данных", Font, new Rectangle(0, 0, previewWidth, previewHeight), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); }
                return bmpError;
            }

            NewtonPreviewParams previewParams;
            try
            {
                var jsonOptions = new JsonSerializerOptions();
                jsonOptions.Converters.Add(new Utilities.JsonConverters.JsonColorConverter());
                previewParams = JsonSerializer.Deserialize<NewtonPreviewParams>(state.PreviewParametersJson, jsonOptions);
            }
            catch (Exception)
            {
                var bmpError = new Bitmap(previewWidth, previewHeight);
                using (var g = Graphics.FromImage(bmpError)) { g.Clear(Color.DarkRed); TextRenderer.DrawText(g, "Ошибка параметров", Font, new Rectangle(0, 0, previewWidth, previewHeight), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); }
                return bmpError;
            }

            var previewEngine = new FractalNewtonEngine();
            if (!previewEngine.SetFormula(previewParams.Formula, out _))
            {
                var bmpError = new Bitmap(previewWidth, previewHeight);
                using (var g = Graphics.FromImage(bmpError)) { g.Clear(Color.DarkRed); TextRenderer.DrawText(g, "Ошибка формулы", Font, new Rectangle(0, 0, previewWidth, previewHeight), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); }
                return bmpError;
            }

            previewEngine.CenterX = (double)previewParams.CenterX;
            previewEngine.CenterY = (double)previewParams.CenterY;
            if (previewParams.Zoom == 0) previewParams.Zoom = 0.001m;
            previewEngine.Scale = 3.0 / (double)previewParams.Zoom;
            previewEngine.MaxIterations = previewParams.Iterations;

            var palette = previewParams.PaletteSnapshot;
            previewEngine.BackgroundColor = palette.BackgroundColor;
            previewEngine.UseGradient = palette.IsGradient;
            if (palette.RootColors != null && palette.RootColors.Count > 0)
            {
                previewEngine.RootColors = palette.RootColors.ToArray();
            }
            else
            {
                previewEngine.RootColors = ColorConfigurationNewtonPoolsForm.GenerateHarmonicColors(previewEngine.Roots.Count).ToArray();
            }

            return previewEngine.RenderToBitmap(previewWidth, previewHeight, 1, progress => { }, CancellationToken.None);
        }

        public List<FractalSaveStateBase> LoadAllSavesForThisType()
        {
            var specificSaves = SaveFileManager.LoadSaves<NewtonSaveState>(this.FractalTypeIdentifier);
            return specificSaves.Cast<FractalSaveStateBase>().ToList();
        }

        public void SaveAllSavesForThisType(List<FractalSaveStateBase> saves)
        {
            var specificSaves = saves.Cast<NewtonSaveState>().ToList();
            SaveFileManager.SaveSaves(this.FractalTypeIdentifier, specificSaves);
        }

        private void btnStateManager_Click(object sender, EventArgs e)
        {
            using (var dialog = new Forms.SaveLoadDialogForm(this))
            {
                dialog.ShowDialog(this);
            }
        }

        #endregion

        #region IHighResRenderable Implementation

        public HighResRenderState GetRenderState()
        {
            var state = new HighResRenderState
            {
                EngineType = this.FractalTypeIdentifier,
                CenterX = (decimal)_centerX,
                CenterY = (decimal)_centerY,
                Zoom = (decimal)_zoom,
                BaseScale = (decimal)BASE_SCALE,
                Iterations = (int)nudIterations.Value,
                FileNameDetails = "newton_fractal", // Простое имя по умолчанию
                // Параметры палитры будут переданы через CreateEngineFromState
            };
            return state;
        }

        private FractalNewtonEngine CreateEngineFromState(HighResRenderState state, bool forPreview)
        {
            var engine = new FractalNewtonEngine();

            // Установка формулы является критическим шагом
            if (!engine.SetFormula(richTextInput.Text, out _))
            {
                throw new InvalidOperationException("Не удалось обработать формулу для рендеринга.");
            }

            engine.CenterX = (double)state.CenterX;
            engine.CenterY = (double)state.CenterY;
            engine.Scale = (double)state.BaseScale / (double)state.Zoom;

            if (forPreview)
            {
                engine.MaxIterations = Math.Min(state.Iterations, 150);
            }
            else
            {
                engine.MaxIterations = state.Iterations;
            }

            // Применяем текущую активную палитру из UI
            var currentPalette = _paletteManager.ActivePalette;
            engine.BackgroundColor = currentPalette.BackgroundColor;
            engine.UseGradient = currentPalette.IsGradient;
            engine.RootColors = (currentPalette.RootColors != null && currentPalette.RootColors.Any())
                ? currentPalette.RootColors.ToArray()
                : ColorConfigurationNewtonPoolsForm.GenerateHarmonicColors(engine.Roots.Count).ToArray();

            return engine;
        }

        public async Task<Bitmap> RenderHighResolutionAsync(HighResRenderState state, int width, int height, int ssaaFactor, IProgress<RenderProgress> progress, CancellationToken cancellationToken)
        {
            _isHighResRendering = true;
            try
            {
                FractalNewtonEngine renderEngine = CreateEngineFromState(state, forPreview: false);
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

        public Bitmap RenderPreview(HighResRenderState state, int previewWidth, int previewHeight)
        {
            var engine = CreateEngineFromState(state, forPreview: true);
            return engine.RenderToBitmap(previewWidth, previewHeight, 1, _ => { }, CancellationToken.None);
        }

        #endregion
    }
}