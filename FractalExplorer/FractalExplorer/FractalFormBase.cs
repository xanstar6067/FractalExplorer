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

        // <<< НАЧАЛО ИЗМЕНЕНИЙ: Добавляем объект для блокировки >>>
        private readonly object _bitmapLock = new object();
        // <<< КОНЕЦ ИЗМЕНЕНИЙ >>>

        private const int TILE_SIZE = 32;
        private Bitmap _previewBitmap;
        private CancellationTokenSource _previewRenderCts;
        private volatile bool _isHighResRendering = false;
        private volatile bool _isRenderingPreview = false;

        protected FractalEngineBase _fractalEngine;
        protected decimal _zoom = 1.0m;
        protected decimal _centerX = 0.0m;
        protected decimal _centerY = 0.0m;
        protected System.Windows.Forms.CheckBox[] _paletteCheckBoxes;
        protected System.Windows.Forms.CheckBox _lastSelectedPaletteCheckBox = null;

        private decimal _renderedCenterX;
        private decimal _renderedCenterY;
        private decimal _renderedZoom;

        private Point _panStart;
        private bool _panning = false;
        private System.Windows.Forms.Timer _renderDebounceTimer;

        #endregion

        #region Abstract and Virtual Members

        protected abstract FractalEngineBase CreateEngine();
        protected virtual decimal BaseScale => 3.0m;
        protected virtual decimal InitialCenterX => -0.5m;
        protected virtual decimal InitialCenterY => 0.0m;
        protected virtual void UpdateEngineSpecificParameters() { }
        protected virtual void OnPostInitialize() { }

        protected virtual string GetSaveFileNameDetails()
        {
            return "fractal";
        }
        #endregion

        #region Constructor and Form Load

        protected FractalFormBase()
        {
            InitializeComponent();
            _centerX = InitialCenterX;
            _centerY = InitialCenterY;
        }

        private void FormBase_Load(object sender, EventArgs e)
        {
            _fractalEngine = CreateEngine();

            _renderDebounceTimer = new System.Windows.Forms.Timer { Interval = 300 };
            _renderDebounceTimer.Tick += RenderDebounceTimer_Tick;

            InitializePaletteCheckBoxes();
            InitializeControls();
            InitializeEventHandlers();

            _renderedCenterX = _centerX;
            _renderedCenterY = _centerY;
            _renderedZoom = _zoom;

            OnPostInitialize();

            HandlePaletteSelectionLogic();
            ScheduleRender();
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
                nudRe.Minimum = -2m;
                nudRe.Maximum = 2m;
                nudRe.DecimalPlaces = 15;
                nudRe.Increment = 0.001m;
                nudRe.Value = -0.8m;

                nudIm.Minimum = -2m;
                nudIm.Maximum = 2m;
                nudIm.DecimalPlaces = 15;
                nudIm.Increment = 0.001m;
                nudIm.Value = 0.156m;
            }
        }

        private void InitializePaletteCheckBoxes()
        {
            var allCheckBoxes = new List<System.Windows.Forms.CheckBox>
            {
                colorBox, oldRenderBW, mondelbrotClassicBox,
                checkBox1, checkBox2, checkBox3, checkBox4, checkBox5, checkBox6
            };
            _paletteCheckBoxes = allCheckBoxes.Where(cb => cb != null).ToArray();
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

            canvas.MouseWheel += Canvas_MouseWheel;
            canvas.MouseDown += Canvas_MouseDown;
            canvas.MouseMove += Canvas_MouseMove;
            canvas.MouseUp += Canvas_MouseUp;
            canvas.Paint += Canvas_Paint;
            canvas.Resize += (s, e) => { if (this.WindowState != FormWindowState.Minimized) ScheduleRender(); };

            foreach (var cb in _paletteCheckBoxes)
            {
                cb.CheckedChanged += PaletteCheckBox_CheckedChanged;
            }

            this.FormClosed += (s, e) => {
                _renderDebounceTimer?.Stop();
                _renderDebounceTimer?.Dispose();

                // <<< НАЧАЛО ИЗМЕНЕНИЙ: Безопасное уничтожение битмапа >>>
                if (_previewRenderCts != null)
                {
                    _previewRenderCts.Cancel();
                    System.Threading.Thread.Sleep(50);
                    _previewRenderCts.Dispose();
                }
                lock (_bitmapLock)
                {
                    _previewBitmap?.Dispose();
                    _previewBitmap = null;
                }
                // <<< КОНЕЦ ИЗМЕНЕНИЙ >>>
            };
        }

        #endregion

        #region Rendering Logic

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
            if (_isHighResRendering)
            {
                ScheduleRender();
                return;
            }
            if (_isRenderingPreview)
            {
                ScheduleRender();
                return;
            }
            await StartPreviewRender();
        }

        // <<< НАЧАЛО ИЗМЕНЕНИЙ: Полностью переработанный метод рендеринга >>>
        private async Task StartPreviewRender()
        {
            if (canvas.Width <= 0 || canvas.Height <= 0) return;

            _isRenderingPreview = true;
            _previewRenderCts?.Cancel();
            _previewRenderCts = new CancellationTokenSource();
            var token = _previewRenderCts.Token;

            // Подготавливаем новый битмап и параметры движка
            var newBitmap = new Bitmap(canvas.Width, canvas.Height, PixelFormat.Format24bppRgb);
            UpdateEngineParameters();

            // Сохраняем параметры, с которыми будет производиться этот рендер
            _renderedCenterX = _centerX;
            _renderedCenterY = _centerY;
            _renderedZoom = _zoom;

            // Копируем движок для потокобезопасного рендеринга
            var renderEngineCopy = CreateEngine();
            // ... (копирование всех свойств движка)
            renderEngineCopy.MaxIterations = _fractalEngine.MaxIterations;
            renderEngineCopy.ThresholdSquared = _fractalEngine.ThresholdSquared;
            renderEngineCopy.CenterX = _fractalEngine.CenterX;
            renderEngineCopy.CenterY = _fractalEngine.CenterY;
            renderEngineCopy.Scale = _fractalEngine.Scale;
            renderEngineCopy.C = _fractalEngine.C;
            renderEngineCopy.Palette = _fractalEngine.Palette;
            renderEngineCopy.MaxColorIterations = _fractalEngine.MaxColorIterations;

            // Безопасно подменяем старый битмап на новый (пустой)
            Bitmap oldBitmapToDispose = null;
            lock (_bitmapLock)
            {
                oldBitmapToDispose = _previewBitmap;
                _previewBitmap = newBitmap;
            }
            oldBitmapToDispose?.Dispose();

            // Сразу инвалидируем холст, чтобы он показал пустой фон
            canvas.Invalidate();

            var tiles = GenerateTiles(canvas.Width, canvas.Height);
            var dispatcher = new TileRenderDispatcher(tiles, GetThreadCount());

            pbRenderProgress.Value = 0;
            pbRenderProgress.Maximum = tiles.Count;
            int progress = 0;

            try
            {
                await dispatcher.RenderAsync(async (tile, ct) =>
                {
                    ct.ThrowIfCancellationRequested();

                    // 1. Отрисовываем плитку в ее собственный, локальный буфер
                    var tileBuffer = renderEngineCopy.RenderSingleTile(tile, canvas.Width, canvas.Height, out int bytesPerPixel);

                    ct.ThrowIfCancellationRequested();

                    // 2. Блокируем общий битмап и копируем в него данные плитки
                    lock (_bitmapLock)
                    {
                        // Проверяем, что битмап не был уничтожен или заменен, пока мы работали
                        if (ct.IsCancellationRequested || _previewBitmap != newBitmap) return;

                        var tileRect = tile.Bounds;
                        // Обрезаем прямоугольник, если он выходит за границы битмапа
                        var bitmapRect = new Rectangle(0, 0, _previewBitmap.Width, _previewBitmap.Height);
                        tileRect.Intersect(bitmapRect);
                        if (tileRect.Width == 0 || tileRect.Height == 0) return;

                        BitmapData bmpData = _previewBitmap.LockBits(tileRect, ImageLockMode.WriteOnly, _previewBitmap.PixelFormat);

                        // Копируем данные построчно
                        int originalTileWidthInBytes = tile.Bounds.Width * bytesPerPixel;
                        for (int y = 0; y < tileRect.Height; y++)
                        {
                            IntPtr destPtr = IntPtr.Add(bmpData.Scan0, y * bmpData.Stride);
                            int srcOffset = (y * originalTileWidthInBytes);
                            Marshal.Copy(tileBuffer, srcOffset, destPtr, tileRect.Width * bytesPerPixel);
                        }
                        _previewBitmap.UnlockBits(bmpData);
                    }

                    // 3. Из UI-потока инвалидируем только область обновленной плитки
                    if (ct.IsCancellationRequested || !canvas.IsHandleCreated || canvas.IsDisposed) return;
                    canvas.Invoke((Action)(() =>
                    {
                        if (ct.IsCancellationRequested) return;
                        canvas.Invalidate(tile.Bounds);
                        if (pbRenderProgress.IsHandleCreated && !pbRenderProgress.IsDisposed)
                        {
                            pbRenderProgress.Value = Math.Min(pbRenderProgress.Maximum, Interlocked.Increment(ref progress));
                        }
                    }));

                    await Task.Yield(); // Даем шанс другим задачам выполниться

                }, token);
            }
            catch (OperationCanceledException)
            {
                // Это ожидаемое исключение при отмене, просто выходим
            }
            catch (Exception ex)
            {
                // Показываем другие ошибки
                if (this.IsHandleCreated && !this.IsDisposed)
                {
                    MessageBox.Show($"Ошибка рендеринга: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            finally
            {
                _isRenderingPreview = false;
                if (pbRenderProgress.IsHandleCreated && !pbRenderProgress.IsDisposed)
                {
                    pbRenderProgress.Invoke((Action)(() => pbRenderProgress.Value = 0));
                }
            }
        }
        // <<< КОНЕЦ ИЗМЕНЕНИЙ >>>

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

        #region Event Handlers

        // <<< НАЧАЛО ИЗМЕНЕНИЙ: Оборачиваем Paint в lock >>>
        private void Canvas_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.Clear(Color.Black);

            lock (_bitmapLock)
            {
                if (_previewBitmap == null || canvas.Width <= 0 || canvas.Height <= 0) return;

                if (_renderedCenterX == _centerX && _renderedCenterY == _centerY && _renderedZoom == _zoom)
                {
                    e.Graphics.DrawImageUnscaled(_previewBitmap, Point.Empty);
                    return;
                }

                decimal renderedComplexWidth = BaseScale / _renderedZoom;
                decimal currentComplexWidth = BaseScale / _zoom;

                if (_renderedZoom <= 0 || _zoom <= 0 || renderedComplexWidth <= 0 || currentComplexWidth <= 0)
                {
                    e.Graphics.DrawImageUnscaled(_previewBitmap, Point.Empty);
                    return;
                }

                decimal units_per_pixel_rendered = renderedComplexWidth / _previewBitmap.Width;
                decimal units_per_pixel_current = currentComplexWidth / canvas.Width;

                decimal rendered_re_min = _renderedCenterX - (renderedComplexWidth / 2.0m);
                decimal rendered_im_max = _renderedCenterY + (_previewBitmap.Height * units_per_pixel_rendered / 2.0m);

                decimal current_re_min = _centerX - (currentComplexWidth / 2.0m);
                decimal current_im_max = _centerY + (canvas.Height * units_per_pixel_current / 2.0m);

                decimal offsetX_pixels = (rendered_re_min - current_re_min) / units_per_pixel_current;
                decimal offsetY_pixels = (current_im_max - rendered_im_max) / units_per_pixel_current;

                decimal newWidth_pixels = _previewBitmap.Width * (units_per_pixel_rendered / units_per_pixel_current);
                decimal newHeight_pixels = _previewBitmap.Height * (units_per_pixel_rendered / units_per_pixel_current);

                const decimal reasonableLimit = 7.9E+28M;
                const float floatReasonableLimit = 1E+18f;

                if (Math.Abs(offsetX_pixels) >= reasonableLimit || Math.Abs(offsetY_pixels) >= reasonableLimit ||
                    Math.Abs(newWidth_pixels) >= reasonableLimit || Math.Abs(newHeight_pixels) >= reasonableLimit)
                {
                    return;
                }

                try
                {
                    float p1_X = (float)offsetX_pixels;
                    float p1_Y = (float)offsetY_pixels;
                    float w_prime = (float)newWidth_pixels;
                    float h_prime = (float)newHeight_pixels;

                    if (!float.IsFinite(p1_X) || !float.IsFinite(p1_Y) || !float.IsFinite(w_prime) || !float.IsFinite(h_prime) ||
                        Math.Abs(p1_X) > floatReasonableLimit || Math.Abs(p1_Y) > floatReasonableLimit ||
                        Math.Abs(w_prime) > floatReasonableLimit || Math.Abs(h_prime) > floatReasonableLimit)
                    {
                        return;
                    }

                    PointF destPoint1 = new PointF(p1_X, p1_Y);
                    PointF destPoint2 = new PointF(p1_X + w_prime, p1_Y);
                    PointF destPoint3 = new PointF(p1_X, p1_Y + h_prime);

                    e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Bilinear;
                    e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

                    e.Graphics.DrawImage(_previewBitmap, new PointF[] { destPoint1, destPoint2, destPoint3 });
                }
                catch (OverflowException) { return; }
                catch (ArgumentException) { return; }
            }
        }
        // <<< КОНЕЦ ИЗМЕНЕНИЙ >>>

        private void ParamControl_Changed(object sender, EventArgs e)
        {
            if (_isHighResRendering) return;
            if (sender == nudZoom)
            {
                _zoom = nudZoom.Value;
            }
            ScheduleRender();
        }

        private void Canvas_MouseWheel(object sender, MouseEventArgs e)
        {
            if (_isHighResRendering) return;

            decimal zoomFactor = e.Delta > 0 ? 1.5m : 1.0m / 1.5m;
            decimal scaleBeforeZoom = BaseScale / _zoom;
            decimal mouseRe = _centerX + (e.X - canvas.Width / 2.0m) * scaleBeforeZoom / canvas.Width;
            decimal mouseIm = _centerY - (e.Y - canvas.Height / 2.0m) * scaleBeforeZoom / canvas.Height;

            _zoom = Math.Max(nudZoom.Minimum, Math.Min(nudZoom.Maximum, _zoom * zoomFactor));

            decimal scaleAfterZoom = BaseScale / _zoom;
            _centerX = mouseRe - (e.X - canvas.Width / 2.0m) * scaleAfterZoom / canvas.Width;
            _centerY = mouseIm + (e.Y - canvas.Height / 2.0m) * scaleAfterZoom / canvas.Height;

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
            decimal units_per_pixel = BaseScale / _zoom / canvas.Width;
            _centerX -= (decimal)(e.X - _panStart.X) * units_per_pixel;
            _centerY += (decimal)(e.Y - _panStart.Y) * units_per_pixel;
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

        private async void btnSave_Click_1(object sender, EventArgs e)
        {
            if (_isHighResRendering)
            {
                MessageBox.Show("Процесс сохранения уже запущен.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int saveWidth = (int)nudSaveWidth.Value;
            int saveHeight = (int)nudSaveHeight.Value;

            string fractalDetails = GetSaveFileNameDetails();
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string suggestedFileName = $"{fractalDetails}_{timestamp}.png";

            using (var saveDialog = new SaveFileDialog
            {
                Filter = "PNG Image|*.png",
                Title = "Сохранить фрактал (Высокое разрешение)",
                FileName = suggestedFileName
            })
            {
                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    if (_isRenderingPreview)
                    {
                        _previewRenderCts?.Cancel();
                    }

                    _isHighResRendering = true;
                    pnlControls.Enabled = false;
                    pbHighResProgress.Value = 0;
                    pbHighResProgress.Visible = true;

                    try
                    {
                        FractalEngineBase renderEngine = CreateEngine();
                        UpdateEngineParameters();
                        renderEngine.MaxIterations = _fractalEngine.MaxIterations;
                        renderEngine.ThresholdSquared = _fractalEngine.ThresholdSquared;
                        renderEngine.CenterX = _fractalEngine.CenterX;
                        renderEngine.CenterY = _fractalEngine.CenterY;
                        renderEngine.Scale = _fractalEngine.Scale;
                        if (this is FractalJulia || this is FractalJuliaBurningShip)
                        {
                            renderEngine.C = new ComplexDecimal(nudRe.Value, nudIm.Value);
                        }
                        else
                        {
                            renderEngine.C = _fractalEngine.C;
                        }

                        HandlePaletteSelectionLogic();
                        renderEngine.Palette = _fractalEngine.Palette;
                        renderEngine.MaxColorIterations = _fractalEngine.MaxColorIterations;

                        int threadCount = GetThreadCount();

                        Bitmap highResBitmap = await Task.Run(() => renderEngine.RenderToBitmap(
                            saveWidth, saveHeight, threadCount,
                            progress => {
                                if (pbHighResProgress.IsHandleCreated && !pbHighResProgress.IsDisposed)
                                {
                                    pbHighResProgress.Invoke((Action)(() => {
                                        pbHighResProgress.Value = Math.Min(pbHighResProgress.Maximum, progress);
                                    }));
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
                            pbHighResProgress.Invoke((Action)(() => {
                                pbHighResProgress.Visible = false;
                                pbHighResProgress.Value = 0;
                            }));
                        }
                        ScheduleRender();
                    }
                }
            }
        }
        #endregion

        #region Palette Logic

        private void PaletteCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            System.Windows.Forms.CheckBox currentCb = sender as System.Windows.Forms.CheckBox;
            if (currentCb == null) return;

            foreach (var cb in _paletteCheckBoxes) cb.CheckedChanged -= PaletteCheckBox_CheckedChanged;

            if (currentCb.Checked)
            {
                _lastSelectedPaletteCheckBox = currentCb;
                foreach (var cb in _paletteCheckBoxes.Where(cb => cb != currentCb))
                {
                    cb.Checked = false;
                }
            }
            else
            {
                _lastSelectedPaletteCheckBox = null;
            }

            foreach (var cb in _paletteCheckBoxes) cb.CheckedChanged += PaletteCheckBox_CheckedChanged;

            HandlePaletteSelectionLogic();
            ScheduleRender();
        }

        private void HandlePaletteSelectionLogic()
        {
            var activePaletteCheckBox = _paletteCheckBoxes.FirstOrDefault(cb => cb.Checked);

            if (activePaletteCheckBox != null)
            {
                var paletteFunc = GetPaletteFuncByName(activePaletteCheckBox.Name);
                if (_fractalEngine != null)
                {
                    _fractalEngine.Palette = paletteFunc;
                }
            }
            else
            {
                if (_fractalEngine != null)
                {
                    _fractalEngine.Palette = GetDefaultPaletteColor;
                }
            }
        }
        private Func<int, int, int, Color> GetPaletteFuncByName(string name)
        {
            switch (name)
            {
                case "colorBox": return GetPaletteColorBoxColor;
                case "oldRenderBW": return GetPaletteOldBWColor;
                case "mondelbrotClassicBox": return GetPaletteMandelbrotClassicColor;
                case "checkBox1": return GetPalette1Color;
                case "checkBox2": return GetPalette2Color;
                case "checkBox3": return GetPalette3Color;
                case "checkBox4": return GetPalette4Color;
                case "checkBox5": return GetPalette5Color;
                case "checkBox6": return GetPalette6Color;
                default: return GetDefaultPaletteColor;
            }
        }

        protected Color GetDefaultPaletteColor(int iter, int maxIter, int maxClrIter) { if (iter == maxIter) return Color.Black; double t_log = Math.Log(Math.Min(iter, maxClrIter) + 1) / Math.Log(maxClrIter + 1); int cVal = (int)(255.0 * (1 - t_log)); return Color.FromArgb(cVal, cVal, cVal); }
        protected Color GetPaletteColorBoxColor(int iter, int maxIter, int maxClrIter) { if (iter == maxIter) return Color.Black; double t_capped = (double)Math.Min(iter, maxClrIter) / maxClrIter; return ColorFromHSV(360.0 * t_capped, 0.6, 1.0); }
        protected Color GetPaletteOldBWColor(int iter, int maxIter, int maxClrIter) { if (iter == maxIter) return Color.Black; double t_capped = (double)Math.Min(iter, maxClrIter) / maxClrIter; int cVal = 255 - (int)(255.0 * t_capped); return Color.FromArgb(cVal, cVal, cVal); }
        protected Color GetPaletteMandelbrotClassicColor(int iter, int maxIter, int maxClrIter) { if (iter == maxIter) return Color.Black; double t_classic = (double)iter / maxIter; byte r, g, b; if (t_classic < 0.5) { double t = t_classic * 2; r = (byte)(t * 200); g = (byte)(t * 50); b = (byte)(t * 30); } else { double t = (t_classic - 0.5) * 2; r = (byte)(200 + t * 55); g = (byte)(50 + t * 205); b = (byte)(30 + t * 225); } return Color.FromArgb(r, g, b); }
        protected Color LerpColor(Color a, Color b, double t) { t = Math.Max(0, Math.Min(1, t)); return Color.FromArgb((int)(a.R + (b.R - a.R) * t), (int)(a.G + (b.G - a.G) * t), (int)(a.B + (b.B - a.B) * t)); }
        protected Color GetPalette1Color(int iter, int maxIter, int maxClrIter) { if (iter == maxIter) return Color.Black; double t = (double)Math.Min(iter, maxClrIter) / maxClrIter; Color c1 = Color.Black, c2 = Color.FromArgb(200, 0, 0), c3 = Color.FromArgb(255, 100, 0), c4 = Color.FromArgb(255, 255, 100), c5 = Color.White; if (t < 0.25) return LerpColor(c1, c2, t / 0.25); if (t < 0.50) return LerpColor(c2, c3, (t - 0.25) / 0.25); if (t < 0.75) return LerpColor(c3, c4, (t - 0.50) / 0.25); return LerpColor(c4, c5, (t - 0.75) / 0.25); }
        protected Color GetPalette2Color(int iter, int maxIter, int maxClrIter) { if (iter == maxIter) return Color.Black; double t = (double)Math.Min(iter, maxClrIter) / maxClrIter; Color c1 = Color.Black, c2 = Color.FromArgb(0, 0, 100), c3 = Color.FromArgb(0, 120, 200), c4 = Color.FromArgb(170, 220, 255), c5 = Color.White; if (t < 0.25) return LerpColor(c1, c2, t / 0.25); if (t < 0.50) return LerpColor(c2, c3, (t - 0.25) / 0.25); if (t < 0.75) return LerpColor(c3, c4, (t - 0.50) / 0.25); return LerpColor(c4, c5, (t - 0.75) / 0.25); }
        protected Color GetPalette3Color(int iter, int maxIter, int maxClrIter) { if (iter == maxIter) return Color.Black; double t = (double)Math.Min(iter, maxClrIter) / maxClrIter; double r = Math.Sin(t * Math.PI * 3.0 + 0.5) * 0.45 + 0.5, g = Math.Sin(t * Math.PI * 3.0 + Math.PI * 2.0 / 3.0 + 0.5) * 0.45 + 0.5, b = Math.Sin(t * Math.PI * 3.0 + Math.PI * 4.0 / 3.0 + 0.5) * 0.45 + 0.5; return Color.FromArgb((int)(r * 255), (int)(g * 255), (int)(b * 255)); }
        protected Color GetPalette4Color(int iter, int maxIter, int maxClrIter) { if (iter == maxIter) return Color.Black; double t = (double)Math.Min(iter, maxClrIter) / maxClrIter; Color c1 = Color.FromArgb(10, 0, 20), c2 = Color.FromArgb(255, 0, 255), c3 = Color.FromArgb(0, 255, 255), c4 = Color.FromArgb(230, 230, 250); if (t < 0.1) return LerpColor(c1, c2, t / 0.1); if (t < 0.4) return LerpColor(c2, c1, (t - 0.1) / 0.3); if (t < 0.5) return LerpColor(c1, c3, (t - 0.4) / 0.1); if (t < 0.8) return LerpColor(c3, c1, (t - 0.5) / 0.3); return LerpColor(c1, c4, (t - 0.8) / 0.2); }
        protected Color GetPalette5Color(int iter, int maxIter, int maxClrIter) { if (iter == maxIter) return Color.Black; double t = (double)Math.Min(iter, maxClrIter) / maxClrIter; int g = 50 + (int)(t * 150); double s = Math.Sin(t * Math.PI * 5); int f = Math.Max(0, Math.Min(255, g + (int)(s * 40))); return Color.FromArgb(f, f, Math.Min(255, f + (int)(t * 25))); }
        protected Color GetPalette6Color(int iter, int maxIter, int maxClrIter) { if (iter == maxIter) { return Color.FromArgb(50, 50, 50); } double t = (double)Math.Min(iter, maxClrIter) / maxClrIter; double h = (t * 200.0 + 180.0) % 360.0, s = Math.Max(0.2, Math.Min(0.6, 0.35 + (Math.Sin(t * Math.PI * 2) * 0.1))), v = Math.Max(0.7, Math.Min(0.95, 0.80 + (Math.Cos(t * Math.PI * 2.5) * 0.15))); return ColorFromHSV(h, s, v); }
        protected Color ColorFromHSV(double hue, double saturation, double value) { hue = (hue % 360 + 360) % 360; int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6; double f = hue / 60 - Math.Floor(hue / 60); value = Math.Max(0, Math.Min(1, value)); saturation = Math.Max(0, Math.Min(1, saturation)); int v_comp = Convert.ToInt32(value * 255); int p_comp = Convert.ToInt32(v_comp * (1 - saturation)); int q_comp = Convert.ToInt32(v_comp * (1 - f * saturation)); int t_comp = Convert.ToInt32(v_comp * (1 - (1 - f) * saturation)); switch (hi) { case 0: return Color.FromArgb(v_comp, t_comp, p_comp); case 1: return Color.FromArgb(q_comp, v_comp, p_comp); case 2: return Color.FromArgb(p_comp, v_comp, t_comp); case 3: return Color.FromArgb(p_comp, q_comp, v_comp); case 4: return Color.FromArgb(t_comp, p_comp, v_comp); default: return Color.FromArgb(v_comp, p_comp, q_comp); } }

        #endregion

        #region Helpers

        private void UpdateEngineParameters()
        {
            _fractalEngine.MaxIterations = (int)nudIterations.Value;
            _fractalEngine.ThresholdSquared = nudThreshold.Value * nudThreshold.Value;
            _fractalEngine.CenterX = _centerX;
            _fractalEngine.CenterY = _centerY;
            _fractalEngine.Scale = BaseScale / _zoom;
            UpdateEngineSpecificParameters();
        }

        private int GetThreadCount()
        {
            return cbThreads.SelectedItem?.ToString() == "Auto"
                ? Environment.ProcessorCount
                : Convert.ToInt32(cbThreads.SelectedItem);
        }

        #endregion

        #region IFractalForm Implementation

        public double LoupeZoom => nudBaseScale != null ? (double)nudBaseScale.Value : 4.0;
        public event EventHandler LoupeZoomChanged;

        #endregion
    }
}