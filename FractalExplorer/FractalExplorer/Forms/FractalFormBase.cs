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
    /// <summary>
    /// Абстрактный базовый класс для форм, отображающих фракталы.
    /// Содержит общую логику UI, управление рендерингом и обработку событий.
    /// </summary>
    public abstract partial class FractalFormBase : Form, IFractalForm
    {
        #region Fields


        private const int TILE_SIZE = 32;
        /// <summary>
        /// Объект для синхронизации доступа к битмапам из разных потоков.
        /// </summary>
        private readonly object _bitmapLock = new object();

        /// <summary>
        /// Хранит последнее ПОЛНОСТЬЮ отрисованное изображение. Используется как фон при трансформациях.
        /// </summary>
        private Bitmap _previewBitmap;

        /// <summary>
        /// Временный битмап, в который рендерятся новые плитки. Рисуется поверх _previewBitmap.
        /// </summary>
        private Bitmap _currentRenderingBitmap;

        private CancellationTokenSource _previewRenderCts;
        private volatile bool _isHighResRendering = false;
        private volatile bool _isRenderingPreview = false;

        protected FractalEngineBase _fractalEngine;
        protected decimal _zoom = 1.0m;
        protected decimal _centerX = 0.0m;
        protected decimal _centerY = 0.0m;
        protected System.Windows.Forms.CheckBox[] _paletteCheckBoxes;
        protected System.Windows.Forms.CheckBox _lastSelectedPaletteCheckBox = null;

        // Параметры, с которыми был сделан последний _previewBitmap
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

            // Изначально считаем, что рендер был сделан с начальными параметрами
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
            //nudZoom.Maximum = 1_000_000_000_000_000m;
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

                if (_previewRenderCts != null)
                {
                    _previewRenderCts.Cancel();
                    System.Threading.Thread.Sleep(50); // Даем время на отмену
                    _previewRenderCts.Dispose();
                }

                // Безопасно уничтожаем оба битмапа
                lock (_bitmapLock)
                {
                    _previewBitmap?.Dispose();
                    _previewBitmap = null;
                    _currentRenderingBitmap?.Dispose();
                    _currentRenderingBitmap = null;
                }
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
            if (_isHighResRendering || _isRenderingPreview)
            {
                ScheduleRender();
                return;
            }
            await StartPreviewRender();
        }

        /// <summary>
        /// Запускает процесс плавного рендеринга с наложением.
        /// </summary>
        private async Task StartPreviewRender()
        {
            if (canvas.Width <= 0 || canvas.Height <= 0) return;

            _isRenderingPreview = true;
            _previewRenderCts?.Cancel();
            _previewRenderCts = new CancellationTokenSource();
            var token = _previewRenderCts.Token;

            // 1. Создаем новый временный битмап для отрисовки новых плиток.
            // Он должен поддерживать прозрачность.
            var newRenderingBitmap = new Bitmap(canvas.Width, canvas.Height, PixelFormat.Format32bppArgb);

            lock (_bitmapLock)
            {
                _currentRenderingBitmap?.Dispose();
                _currentRenderingBitmap = newRenderingBitmap;
            }

            // 2. Обновляем параметры движка и делаем его копию для потоков.
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

            pbRenderProgress.Value = 0;
            pbRenderProgress.Maximum = tiles.Count;
            int progress = 0;

            try
            {
                // 3. Запускаем рендеринг плиток в _currentRenderingBitmap
                await dispatcher.RenderAsync(async (tile, ct) =>
                {
                    ct.ThrowIfCancellationRequested();

                    // Рендерим плитку в ее собственный 32-битный буфер
                    var tileBuffer = renderEngineCopy.RenderSingleTile(tile, canvas.Width, canvas.Height, out int bytesPerPixel);

                    ct.ThrowIfCancellationRequested();

                    // Копируем данные из буфера плитки в _currentRenderingBitmap
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
                            int srcOffset = (y * originalTileWidthInBytes) + (tileRect.X - tile.Bounds.X) * bytesPerPixel;
                            Marshal.Copy(tileBuffer, srcOffset, destPtr, tileRect.Width * bytesPerPixel);
                        }
                        _currentRenderingBitmap.UnlockBits(bmpData);
                    }

                    // Обновляем UI
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

                    await Task.Yield();

                }, token);

                token.ThrowIfCancellationRequested();

                // 4. Рендеринг успешно завершен. Делаем _currentRenderingBitmap основным.
                lock (_bitmapLock)
                {
                    if (_currentRenderingBitmap == newRenderingBitmap)
                    {
                        _previewBitmap?.Dispose();
                        _previewBitmap = _currentRenderingBitmap; // Продвижение
                        _currentRenderingBitmap = null;

                        _renderedCenterX = currentRenderedCenterX;
                        _renderedCenterY = currentRenderedCenterY;
                        _renderedZoom = currentRenderedZoom;
                    }
                }
                canvas.Invalidate(); // Финальная перерисовка
            }
            catch (OperationCanceledException)
            {
                // Если рендер отменен, просто удаляем временный битмап
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

        /// <summary>
        /// Отрисовывает холст, совмещая старый и новый битмапы.
        /// </summary>
        private void Canvas_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.Clear(Color.Black);

            lock (_bitmapLock)
            {
                // 1. Рисуем ФОН: старый, но трансформированный _previewBitmap
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
                            decimal renderedComplexWidth = BaseScale / _renderedZoom;
                            decimal currentComplexWidth = BaseScale / _zoom;

                            if (_renderedZoom <= 0 || _zoom <= 0 || renderedComplexWidth <= 0 || currentComplexWidth <= 0)
                            {
                                e.Graphics.DrawImageUnscaled(_previewBitmap, Point.Empty);
                            }
                            else
                            {
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

                                PointF destPoint1 = new PointF((float)offsetX_pixels, (float)offsetY_pixels);
                                PointF destPoint2 = new PointF((float)(offsetX_pixels + newWidth_pixels), (float)offsetY_pixels);
                                PointF destPoint3 = new PointF((float)offsetX_pixels, (float)(offsetY_pixels + newHeight_pixels));

                                e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Bilinear;
                                e.Graphics.DrawImage(_previewBitmap, new PointF[] { destPoint1, destPoint2, destPoint3 });
                            }
                        }
                        catch (Exception) { /* Игнорируем ошибки при слишком больших трансформациях */ }
                    }
                }

                // 2. Рисуем ПЕРЕДНИЙ ПЛАН: новые, отрисованные плитки из _currentRenderingBitmap
                if (_currentRenderingBitmap != null)
                {
                    e.Graphics.DrawImageUnscaled(_currentRenderingBitmap, Point.Empty);
                }
            }
        }

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

            // >>> ИЗМЕНЕНИЕ ЗДЕСЬ <<<
            // "Запекаем" текущий вид (фон + новые плитки) в единый фон перед трансформацией.
            CommitAndBakePreview();

            // Логика зума относительно курсора
            decimal zoomFactor = e.Delta > 0 ? 1.5m : 1.0m / 1.5m;
            decimal scaleBeforeZoom = BaseScale / _zoom;

            // Вычисляем комплексные координаты точки под курсором
            decimal mouseRe = _centerX + (e.X - canvas.Width / 2.0m) * scaleBeforeZoom / canvas.Width;
            decimal mouseIm = _centerY - (e.Y - canvas.Height / 2.0m) * scaleBeforeZoom / canvas.Height;

            // Применяем новый зум
            _zoom = Math.Max(nudZoom.Minimum, Math.Min(nudZoom.Maximum, _zoom * zoomFactor));

            // Пересчитываем центр так, чтобы точка под курсором осталась на месте
            decimal scaleAfterZoom = BaseScale / _zoom;
            _centerX = mouseRe - (e.X - canvas.Width / 2.0m) * scaleAfterZoom / canvas.Width;
            _centerY = mouseIm + (e.Y - canvas.Height / 2.0m) * scaleAfterZoom / canvas.Height;

            // Немедленно перерисовываем холст, чтобы показать трансформацию
            canvas.Invalidate();

            // Обновляем UI и планируем новый детальный рендер
            if (nudZoom.Value != _zoom)
            {
                nudZoom.Value = _zoom; // Это вызовет ScheduleRender через событие ValueChanged
            }
            else
            {
                ScheduleRender(); // Если значение не изменилось (уперлись в лимит)
            }
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

            // >>> ИЗМЕНЕНИЕ ЗДЕСЬ <<<
            // "Запекаем" текущий вид (фон + новые плитки) в единый фон перед панорамированием.
            CommitAndBakePreview();

            // Единицы комплексной плоскости на пиксель для текущего вида
            decimal units_per_pixel = BaseScale / _zoom / canvas.Width;

            // Смещаем центр на дельту движения мыши
            _centerX -= (decimal)(e.X - _panStart.X) * units_per_pixel;
            _centerY += (decimal)(e.Y - _panStart.Y) * units_per_pixel;

            // Обновляем стартовую точку для следующего смещения
            _panStart = e.Location;

            // Немедленно перерисовываем холст для отображения сдвига
            canvas.Invalidate();

            // Планируем новый детальный рендер
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

        /// <summary>
        /// "Запекает" текущее состояние холста (фон + новые плитки) в основной _previewBitmap.
        /// Вызывается при начале нового действия пользователя (зум, панорамирование).
        /// </summary>
        private void CommitAndBakePreview()
        {
            // Проверяем, есть ли вообще что запекать.
            lock (_bitmapLock)
            {
                if (!_isRenderingPreview || _currentRenderingBitmap == null)
                {
                    return;
                }
            }

            // Немедленно отменяем текущий рендер.
            // Важно делать это *вне* блокировки, чтобы избежать дедлоков.
            _previewRenderCts?.Cancel();

            lock (_bitmapLock)
            {
                // После отмены еще раз проверяем, на случай если состояние изменилось.
                if (_currentRenderingBitmap == null) return;

                // Создаем новый битмап, на котором будем смешивать слои.
                // Формат 24bpp, так как прозрачность больше не нужна.
                var bakedBitmap = new Bitmap(canvas.Width, canvas.Height, PixelFormat.Format24bppRgb);
                using (var g = Graphics.FromImage(bakedBitmap))
                {
                    g.Clear(Color.Black);
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Bilinear;

                    // 1. Рисуем старый фон с его текущей трансформацией.
                    // Это в точности та же логика, что и в Canvas_Paint.
                    if (_previewBitmap != null)
                    {
                        try
                        {
                            decimal renderedComplexWidth = BaseScale / _renderedZoom;
                            decimal currentComplexWidth = BaseScale / _zoom;

                            if (!(_renderedZoom <= 0 || _zoom <= 0 || renderedComplexWidth <= 0 || currentComplexWidth <= 0))
                            {
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

                                PointF destPoint1 = new PointF((float)offsetX_pixels, (float)offsetY_pixels);
                                PointF destPoint2 = new PointF((float)(offsetX_pixels + newWidth_pixels), (float)offsetY_pixels);
                                PointF destPoint3 = new PointF((float)offsetX_pixels, (float)(offsetY_pixels + newHeight_pixels));

                                g.DrawImage(_previewBitmap, new PointF[] { destPoint1, destPoint2, destPoint3 });
                            }
                        }
                        catch (Exception) { /* Игнорируем */ }
                    }

                    // 2. Поверх рисуем новые, уже отрисованные плитки.
                    g.DrawImageUnscaled(_currentRenderingBitmap, Point.Empty);
                }

                // 3. Продвигаем "запеченный" битмап на место основного.
                _previewBitmap?.Dispose();
                _previewBitmap = bakedBitmap;

                // 4. Очищаем слой рендеринга.
                _currentRenderingBitmap.Dispose();
                _currentRenderingBitmap = null;

                // 5. КРИТИЧЕСКИ ВАЖНО: обновляем "отрендеренные" координаты.
                // Теперь наш новый фон соответствует текущему положению и зуму.
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