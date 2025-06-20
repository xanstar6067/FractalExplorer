using FractalExplorer.Engines;
using FractalExplorer.Resources;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
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
        private color_setting_NewtonPoolsForm _colorSettingsForm;

        // --- Состояние UI и рендеринга ---
        private const double BASE_SCALE = 3.0;
        private const int TILE_SIZE = 64;
        private readonly object _bitmapLock = new object();

        private Bitmap _previewBitmap; // Последний полностью отрисованный
        private Bitmap _currentRenderingBitmap; // Для новых плиток

        private CancellationTokenSource _previewRenderCts;
        private volatile bool _isHighResRendering = false;
        private volatile bool _isRenderingPreview = false;

        // --- Параметры вида ---
        private double _zoom = 1.0;
        private double _centerX = 0.0;
        private double _centerY = 0.0;

        // Параметры, с которыми был сделан _previewBitmap
        private double _renderedCenterX;
        private double _renderedCenterY;
        private double _renderedZoom;

        private Point _panStart;
        private bool _panning = false;

        private List<Color> _userDefinedRootColors = new List<Color>();
        private Color _userDefinedBackgroundColor = Color.Black;
        private bool _useCustomPalette = false;

        private readonly string[] presetPolynomials = {
     // --- Классические полиномы ---
     "z^3-1",
     "z^4-1",
     "z^5-1",
     "z^6-1",
     "z^3-2*z+2",
     "z^5 - z^2 + 1",
     "z^6 + 3*z^3 - 2",
     "z^4 - 4*z^2 + 4", // (z^2-2)^2
     "z^7 + z^4 - z + 1",
     "z^8 + 15*z^4 - 16", // (z^4+16)(z^4-1)
     "z^4 + z^3 + z^2 + z + 1",

     // --- С комплексными и дробными коэффициентами ---
     "z^2 - i",
     "(z^2-1)*(z-2*i)",
     "(1+2*i)*z^2+z-1",
     "(0.5-0.3*i)*z^3+2",
     "0.5*z^3 - 1.25*z + 2",
     "(2+i)*z^3 - (1-2*i)*z + 1",
     "i*z^4 + z - 1",
     "(1+0.5*i)*z^2 - z + (2-3*i)",
     "(0.3+1.7*i)*z^3 + (1-i)",
     "(2-i)*z^5 + (3+2*i)*z^2 - 1",
     "-2*z^3 + 0.75*z^2 - 1",
     "z^6 - 1.5*z^3 + 0.25",
     "-0.1*z^4 + z - 2",
     "(1/2)*z^3 + (3/4)*z - 1",
     "(2+3*i)*(z^2) - (1-i)*z + 4",

     // --- Дробно-рациональные функции (для теста деления) ---
     "(z^2-1)/(z^2+1)",
     "(z^3-1)/(z^3+1)",
     "z^2 / (z-1)^2",
     "(z^4-1)/(z*z-2*z+1)" // (z^4-1)/(z-1)^2
 };
        public NewtonPools()
        {
            InitializeComponent();
            _engine = new NewtonFractalEngine();
            _renderDebounceTimer = new System.Windows.Forms.Timer { Interval = 300 };
            InitializeForm();
        }

        private void InitializeForm()
        {
            _renderDebounceTimer.Tick += RenderDebounceTimer_Tick;

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

            // Подписки
            nudIterations.ValueChanged += (s, e) => ScheduleRender();
            cbThreads.SelectedIndexChanged += (s, e) => ScheduleRender();
            nudZoom.ValueChanged += (s, e) => { _zoom = (double)nudZoom.Value; ScheduleRender(); };
            cbSelector.SelectedIndexChanged += cbSelector_SelectedIndexChanged;
            richTextInput.TextChanged += (s, e) => ScheduleRender();
            btnRender.Click += (s, e) => ScheduleRender();
            btnSave.Click += btnSave_Click;

            // Цвета
            var colorCheckboxes = new[] { oldRenderBW, colorBox0, colorBox1, colorBox2, colorBox3, colorBox4, colorCustom };
            foreach (var cb in colorCheckboxes) cb.CheckedChanged += ColorBox_Changed;
            custom_color.Click += custom_color_Click;

            // Мышь и холст
            fractal_bitmap.MouseWheel += Canvas_MouseWheel;
            fractal_bitmap.MouseDown += Canvas_MouseDown;
            fractal_bitmap.MouseMove += Canvas_MouseMove;
            fractal_bitmap.MouseUp += Canvas_MouseUp;
            fractal_bitmap.Paint += Canvas_Paint;
            fractal_bitmap.Resize += (s, e) => { if (this.WindowState != FormWindowState.Minimized) ScheduleRender(); };

            _renderedCenterX = _centerX;
            _renderedCenterY = _centerY;
            _renderedZoom = _zoom;

            ScheduleRender();
        }

        #region Tiled Rendering Logic

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
                ScheduleRender(); // Если уже идет рендер, перезапустим таймер
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

            // 1. Обрабатываем формулу и находим корни
            if (!_engine.SetFormula(richTextInput.Text, out string debugInfo))
            {
                richTextDebugOutput.Text = debugInfo;
                lock (_bitmapLock)
                {
                    _previewBitmap?.Dispose();
                    _previewBitmap = null;
                    _currentRenderingBitmap?.Dispose();
                    _currentRenderingBitmap = null;
                }
                fractal_bitmap.Invalidate();
                _isRenderingPreview = false;
                return;
            }
            richTextDebugOutput.Text = debugInfo;

            // 2. Создаем новый временный битмап для отрисовки плиток (прозрачный)
            var newRenderingBitmap = new Bitmap(fractal_bitmap.Width, fractal_bitmap.Height, PixelFormat.Format32bppArgb);
            lock (_bitmapLock)
            {
                _currentRenderingBitmap?.Dispose();
                _currentRenderingBitmap = newRenderingBitmap;
            }

            // 3. Обновляем параметры движка
            UpdateEngineParameters();
            double currentRenderedCenterX = _centerX;
            double currentRenderedCenterY = _centerY;
            double currentRenderedZoom = _zoom;

            var tiles = GenerateTiles(fractal_bitmap.Width, fractal_bitmap.Height);
            var dispatcher = new TileRenderDispatcher(tiles, GetThreadCount());

            progressBar.Value = 0;
            progressBar.Maximum = tiles.Count;
            int progress = 0;

            try
            {
                await dispatcher.RenderAsync(async (tile, ct) =>
                {
                    ct.ThrowIfCancellationRequested();
                    var tileBuffer = new byte[tile.Bounds.Width * tile.Bounds.Height * 4];
                    _engine.RenderTile(tileBuffer, tile.Bounds.Width * 4, 4, tile, fractal_bitmap.Width, fractal_bitmap.Height);
                    ct.ThrowIfCancellationRequested();

                    lock (_bitmapLock)
                    {
                        if (ct.IsCancellationRequested || _currentRenderingBitmap != newRenderingBitmap) return;
                        BitmapData bmpData = _currentRenderingBitmap.LockBits(tile.Bounds, ImageLockMode.WriteOnly, _currentRenderingBitmap.PixelFormat);
                        Marshal.Copy(tileBuffer, 0, bmpData.Scan0, tileBuffer.Length);
                        _currentRenderingBitmap.UnlockBits(bmpData);
                    }

                    if (ct.IsCancellationRequested || !fractal_bitmap.IsHandleCreated || fractal_bitmap.IsDisposed) return;
                    fractal_bitmap.Invoke((Action)(() =>
                    {
                        if (ct.IsCancellationRequested) return;
                        fractal_bitmap.Invalidate(tile.Bounds);
                        if (progressBar.IsHandleCreated && !progressBar.IsDisposed)
                            progressBar.Value = Math.Min(progressBar.Maximum, Interlocked.Increment(ref progress));
                    }));
                    await Task.Yield();
                }, token);

                token.ThrowIfCancellationRequested();

                // 4. Рендеринг успешно завершен. Делаем _currentRenderingBitmap основным.
                lock (_bitmapLock)
                {
                    if (_currentRenderingBitmap == newRenderingBitmap)
                    {
                        _previewBitmap?.Dispose();
                        _previewBitmap = new Bitmap(_currentRenderingBitmap); // Копируем, чтобы избавиться от альфа-канала
                        _currentRenderingBitmap.Dispose();
                        _currentRenderingBitmap = null;

                        _renderedCenterX = currentRenderedCenterX;
                        _renderedCenterY = currentRenderedCenterY;
                        _renderedZoom = currentRenderedZoom;
                    }
                }
                fractal_bitmap.Invalidate();
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
            }
            catch (Exception ex)
            {
                if (this.IsHandleCreated && !this.IsDisposed)
                    MessageBox.Show($"Ошибка рендеринга: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _isRenderingPreview = false;
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
                    tiles.Add(new TileInfo(x, y, TILE_SIZE, TILE_SIZE));
                }
            }
            return tiles.OrderBy(t => Math.Pow(t.Center.X - center.X, 2) + Math.Pow(t.Center.Y - center.Y, 2)).ToList();
        }

        #endregion

        #region Event Handlers (UI, Mouse, etc.)

        private void Canvas_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.Clear(Color.Black);
            e.Graphics.InterpolationMode = InterpolationMode.Bilinear;

            lock (_bitmapLock)
            {
                // 1. Рисуем фон: старый, но трансформированный _previewBitmap
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
                        float offsetY = (float)(deltaIm / currentScale * fractal_bitmap.Width); // Используем Width для сохранения пропорций

                        float drawX = (fractal_bitmap.Width - newWidth) / 2.0f + offsetX;
                        float drawY = (fractal_bitmap.Height - newHeight) / 2.0f + offsetY;

                        var destRect = new RectangleF(drawX, drawY, newWidth, newHeight);
                        e.Graphics.DrawImage(_previewBitmap, destRect);
                    }
                    catch { /* Игнорируем ошибки при слишком больших трансформациях */ }
                }

                // 2. Рисуем передний план: новые плитки из _currentRenderingBitmap
                if (_currentRenderingBitmap != null)
                {
                    e.Graphics.DrawImageUnscaled(_currentRenderingBitmap, Point.Empty);
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
            _centerY -= (e.Y - _panStart.Y) * scale; // Знак минус, т.к. Y в UI инвертирован
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
                // ScheduleRender() будет вызван событием TextChanged
            }
        }

        private void ColorBox_Changed(object sender, EventArgs e)
        {
            if (_isHighResRendering) return;
            if (sender is CheckBox currentCb && currentCb.Checked)
            {
                _useCustomPalette = (currentCb == colorCustom);
                var allCheckBoxes = new[] { oldRenderBW, colorBox0, colorBox1, colorBox2, colorBox3, colorBox4, colorCustom };
                foreach (var cb in allCheckBoxes)
                {
                    if (cb != currentCb) cb.Checked = false;
                }
            }
            ScheduleRender();
        }

        private void custom_color_Click(object sender, EventArgs e)
        {
            // Процесс обработки формулы уже есть в StartPreviewRender, вызовем его для актуализации корней
            if (!_engine.SetFormula(richTextInput.Text, out string _))
            {
                MessageBox.Show("Сначала введите корректную формулу.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (_colorSettingsForm == null || _colorSettingsForm.IsDisposed)
            {
                _colorSettingsForm = new color_setting_NewtonPoolsForm();
                _colorSettingsForm.ColorsChanged += (s, palette) => {
                    _userDefinedRootColors = palette.RootColors;
                    _userDefinedBackgroundColor = palette.BackgroundColor;
                    if (_useCustomPalette) ScheduleRender();
                };
            }
            _colorSettingsForm.PopulateColorPickers(_userDefinedRootColors, _userDefinedBackgroundColor, _engine.Roots.Count);
            _colorSettingsForm.Show();
            _colorSettingsForm.Activate();
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
                    // Рисуем на нем текущее состояние холста (трансформированный фон + новые плитки)
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

            UpdatePalette();
        }

        private void UpdatePalette()
        {
            if (_useCustomPalette)
            {
                _engine.RootColors = _userDefinedRootColors.Take(_engine.Roots.Count).ToArray();
                _engine.BackgroundColor = _userDefinedBackgroundColor;
                _engine.UseGradient = false;
            }
            else
            {
                // Логика выбора стандартных палитр
                var rootColors = new Color[_engine.Roots.Count];
                bool useBlackWhite = oldRenderBW.Checked;
                bool useGradient = colorBox0.Checked; // Gradient
                bool usePastel = colorBox1.Checked;
                bool useContrast = colorBox2.Checked;
                bool useFire = colorBox3.Checked;
                bool useContrasting = colorBox4.Checked;

                _engine.UseGradient = useGradient;

                // Эта логика будет работать, только если UseGradient = false
                if (usePastel) { Color[] p = { Color.FromArgb(255, 182, 193), Color.FromArgb(173, 216, 230), Color.FromArgb(189, 252, 201) }; for (int i = 0; i < rootColors.Length; i++) rootColors[i] = p[i % p.Length]; }
                else if (useContrast) { Color[] p = { Color.Red, Color.Yellow, Color.Blue }; for (int i = 0; i < rootColors.Length; i++) rootColors[i] = p[i % p.Length]; }
                else if (useFire) { Color[] p = { Color.FromArgb(200, 0, 0), Color.FromArgb(255, 100, 0), Color.FromArgb(255, 255, 100) }; for (int i = 0; i < rootColors.Length; i++) rootColors[i] = p[i % p.Length]; }
                else if (useContrasting) { Color[] p = { Color.FromArgb(10, 0, 20), Color.Magenta, Color.Cyan }; for (int i = 0; i < rootColors.Length; i++) rootColors[i] = p[i % p.Length]; }
                else if (useBlackWhite) { for (int i = 0; i < rootColors.Length; i++) rootColors[i] = Color.White; }
                else { for (int i = 0; i < rootColors.Length; i++) { int shade = 255 * (i + 1) / (rootColors.Length + 1); rootColors[i] = Color.FromArgb(shade, shade, shade); } }

                _engine.RootColors = rootColors;
                _engine.BackgroundColor = usePastel ? Color.FromArgb(50, 50, 50) : Color.Black;
            }
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

                    // Копируем параметры в новый движок
                    saveEngine.MaxIterations = (int)nudIterations.Value;
                    saveEngine.CenterX = _centerX;
                    saveEngine.CenterY = _centerY;
                    saveEngine.Scale = BASE_SCALE / _zoom;
                    // Палитру нужно скопировать так же, как мы делаем для рендера
                    _useCustomPalette = colorCustom.Checked;
                    UpdatePalette(); // Обновит палитру основного движка
                    saveEngine.RootColors = _engine.RootColors; // Копируем ее
                    saveEngine.BackgroundColor = _engine.BackgroundColor;
                    saveEngine.UseGradient = _engine.UseGradient;

                    try
                    {
                        Bitmap highResBitmap = await Task.Run(() => saveEngine.RenderToBitmap(
                            saveWidth, saveHeight, GetThreadCount(),
                            progress => {
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
            Action action = () => {
                panel1.Enabled = enabled; // Отключаем всю панель
                btnSave.Enabled = enabled; // Кнопку сохранения управляем отдельно
            };
            if (this.InvokeRequired) this.Invoke(action); else action();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _renderDebounceTimer?.Stop();
            _previewRenderCts?.Cancel();
            _previewRenderCts?.Dispose();
            _renderDebounceTimer?.Dispose();
            _colorSettingsForm?.Close();
            base.OnFormClosed(e);
        }
        #endregion
    }
}