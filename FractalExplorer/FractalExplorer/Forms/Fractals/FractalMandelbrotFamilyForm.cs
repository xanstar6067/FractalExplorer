using FractalDraving;
using FractalExplorer.Engines;
using FractalExplorer.Engines.EngineImplementations;
using FractalExplorer.Engines.EngineInterfaces;
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
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace FractalDraving
{
    public abstract partial class FractalMandelbrotFamilyForm : Form, IFractalForm, ISaveLoadCapableFractal, IHighResRenderable
    {
        #region Fields

        private RenderVisualizerComponent _renderVisualizer;
        private ColorPaletteMandelbrotFamily _paletteManager;
        private Color[] _gammaCorrectedPaletteCache;
        private string _paletteCacheSignature;
        private ColorConfigurationMandelbrotFamilyForm _colorConfigForm;
        private const int TILE_SIZE = 16;
        private readonly object _bitmapLock = new object();
        private Bitmap _previewBitmap;
        private CancellationTokenSource _renderCts;
        private volatile bool _isHighResRendering = false;
        private volatile bool _isRendering = false;

        // --- КЛЮЧЕВЫЕ ИЗМЕНЕНИЯ: Переход на BigDecimal ---
        protected BigDecimal _zoom;
        protected BigDecimal _centerX;
        protected BigDecimal _centerY;
        private BigDecimal _renderedCenterX;
        private BigDecimal _renderedCenterY;
        private BigDecimal _renderedZoom;

        // Пороги для смены движка
        private static readonly BigDecimal ZoomLevel2Threshold = new BigDecimal(20000m);
        private static readonly BigDecimal ZoomLevel3Threshold = BigDecimal.Parse("2.2758e25"); // 22758000000000000000000000

        private Point _panStart;
        private bool _panning = false;
        private System.Windows.Forms.Timer _renderDebounceTimer;
        private string _baseTitle;
        private Task _renderTask;

        #endregion

        #region Constructor

        protected FractalMandelbrotFamilyForm()
        {
            InitializeComponent();
            // Инициализация с использованием BigDecimal
            _centerX = new BigDecimal(InitialCenterX);
            _centerY = new BigDecimal(InitialCenterY);
            _zoom = new BigDecimal(1.0m);
        }

        #endregion

        #region Protected Abstract/Virtual Methods

        // Новый абстрактный метод для идентификации типа фрактала
        protected abstract FractalType GetFractalType();

        // BaseScale теперь возвращает BigDecimal
        protected virtual BigDecimal BaseScale => new BigDecimal(3.0m);
        protected virtual decimal InitialCenterX => -0.5m;
        protected virtual decimal InitialCenterY => 0.0m;
        protected virtual void OnPostInitialize() { }
        protected virtual string GetSaveFileNameDetails() => "fractal";

        // Старые методы, которые больше не нужны
        // protected abstract FractalMandelbrotFamilyEngine CreateEngine();
        // protected virtual void UpdateEngineSpecificParameters() { }

        #endregion

        #region Form Lifecycle & UI Initialization

        private void FormBase_Load(object sender, EventArgs e)
        {
            _baseTitle = this.Text;
            _paletteManager = new ColorPaletteMandelbrotFamily();
            _renderDebounceTimer = new System.Windows.Forms.Timer { Interval = 300 };
            _renderDebounceTimer.Tick += RenderDebounceTimer_Tick;
            _renderVisualizer = new RenderVisualizerComponent(TILE_SIZE);
            _renderVisualizer.NeedsRedraw += OnVisualizerNeedsRedraw;

            InitializeControls();
            InitializeEventHandlers();

            var cbSSAA = this.Controls.Find("cbSSAA", true).FirstOrDefault() as ComboBox;
            if (cbSSAA != null)
            {
                cbSSAA.Items.Add("Выкл (1x)");
                cbSSAA.Items.Add("Низкое (2x)");
                cbSSAA.Items.Add("Высокое (4x)");
                cbSSAA.SelectedItem = "Выкл (1x)";
                cbSSAA.SelectedIndexChanged += (s, ev) => ScheduleRender();
            }

            _zoom = BaseScale / new BigDecimal(4.0m);
            UpdateZoomUI();

            _renderedCenterX = _centerX;
            _renderedCenterY = _centerY;
            _renderedZoom = _zoom;
            OnPostInitialize();
            ScheduleRender();
        }

        private void FractalMandelbrotFamilyForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            _renderDebounceTimer?.Stop();
            _renderDebounceTimer?.Dispose();
            _renderCts?.Cancel();
            _renderCts?.Dispose();
            lock (_bitmapLock)
            {
                _previewBitmap?.Dispose();
                _previewBitmap = null;
            }
            if (_renderVisualizer != null)
            {
                _renderVisualizer.NeedsRedraw -= OnVisualizerNeedsRedraw;
                _renderVisualizer.Dispose();
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

            nudZoom.DecimalPlaces = 4; // Уменьшим для отображения, т.к. реальная точность в _zoom
            nudZoom.Increment = 0.1m;
            nudZoom.Minimum = 0.0001m;
            nudZoom.Maximum = decimal.MaxValue;

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

        private void InitializeEventHandlers()
        {
            nudIterations.ValueChanged += ParamControl_Changed;
            nudThreshold.ValueChanged += ParamControl_Changed;
            cbThreads.SelectedIndexChanged += ParamControl_Changed;
            nudZoom.ValueChanged += NudZoom_ValueChanged;
            if (nudRe != null) nudRe.ValueChanged += ParamControl_Changed;
            if (nudIm != null) nudIm.ValueChanged += ParamControl_Changed;

            var configButton = Controls.Find("color_configurations", true).FirstOrDefault();
            if (configButton != null) configButton.Click += color_configurations_Click;

            canvas.MouseWheel += Canvas_MouseWheel;
            canvas.MouseDown += Canvas_MouseDown;
            canvas.MouseMove += Canvas_MouseMove;
            canvas.MouseUp += Canvas_MouseUp;
            canvas.Paint += Canvas_Paint;
            canvas.Resize += (s, e) =>
            {
                if (WindowState != FormWindowState.Minimized) ScheduleRender();
            };
        }

        #endregion

        #region UI Event Handlers

        private void ParamControl_Changed(object sender, EventArgs e)
        {
            if (_isHighResRendering) return;
            ScheduleRender();
        }

        private void NudZoom_ValueChanged(object sender, EventArgs e)
        {
            // Обновляем BigDecimal _zoom из UI, если пользователь вводит вручную
            var newZoom = new BigDecimal(nudZoom.Value);
            if (_zoom != newZoom)
            {
                _zoom = newZoom;
                ScheduleRender();
            }
        }

        private void color_configurations_Click(object sender, EventArgs e)
        {
            if (_colorConfigForm == null || _colorConfigForm.IsDisposed)
            {
                _colorConfigForm = new ColorConfigurationMandelbrotFamilyForm(_paletteManager);
                _colorConfigForm.PaletteApplied += (s, ev) => ScheduleRender();
                _colorConfigForm.FormClosed += (s, args) => _colorConfigForm = null;
                _colorConfigForm.Show(this);
            }
            else _colorConfigForm.Activate();
        }

        #endregion

        #region Canvas Interaction

        private void Canvas_MouseWheel(object sender, MouseEventArgs e)
        {
            if (_isRendering || canvas.Width <= 0 || canvas.Height <= 0) return;

            decimal zoomFactor = e.Delta > 0 ? 1.5m : 1.0m / 1.5m;
            var bigZoomFactor = new BigDecimal(zoomFactor);

            // --- Расчеты с BigDecimal ---
            var scaleBeforeZoom = BaseScale / _zoom;
            var mouseReal = _centerX + (new BigDecimal(e.X) - new BigDecimal(canvas.Width) / new BigDecimal(2)) * scaleBeforeZoom / new BigDecimal(canvas.Width);
            var mouseImaginary = _centerY - (new BigDecimal(e.Y) - new BigDecimal(canvas.Height) / new BigDecimal(2)) * scaleBeforeZoom / new BigDecimal(canvas.Height);

            _zoom *= bigZoomFactor;

            var scaleAfterZoom = BaseScale / _zoom;
            _centerX = mouseReal - (new BigDecimal(e.X) - new BigDecimal(canvas.Width) / new BigDecimal(2)) * scaleAfterZoom / new BigDecimal(canvas.Width);
            _centerY = mouseImaginary + (new BigDecimal(e.Y) - new BigDecimal(canvas.Height) / new BigDecimal(2)) * scaleAfterZoom / new BigDecimal(canvas.Height);

            UpdateZoomUI(); // Безопасное обновление UI
            canvas.Invalidate();
            ScheduleRender();
        }

        private void Canvas_MouseDown(object sender, MouseEventArgs e)
        {
            if (_isRendering) return;
            if (e.Button == MouseButtons.Left)
            {
                _panning = true;
                _panStart = e.Location;
                canvas.Cursor = Cursors.Hand;
            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_panning || canvas.Width <= 0) return;

            var unitsPerPixel = BaseScale / _zoom / new BigDecimal(canvas.Width);
            _centerX -= new BigDecimal(e.X - _panStart.X) * unitsPerPixel;
            _centerY += new BigDecimal(e.Y - _panStart.Y) * unitsPerPixel;
            _panStart = e.Location;

            canvas.Invalidate();
            ScheduleRender();
        }

        private void Canvas_MouseUp(object sender, MouseEventArgs e)
        {
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
                if (_previewBitmap != null)
                {
                    e.Graphics.DrawImageUnscaled(_previewBitmap, Point.Empty);
                }
            }
        }

        #endregion

        #region New Rendering Logic

        private void ScheduleRender()
        {
            if (_isHighResRendering || WindowState == FormWindowState.Minimized) return;
            _renderDebounceTimer.Stop();
            _renderDebounceTimer.Start();
        }

        private void RenderDebounceTimer_Tick(object sender, EventArgs e)
        {
            _renderDebounceTimer.Stop();
            if (_renderTask != null && !_renderTask.IsCompleted)
            {
                _renderCts?.Cancel();
            }

            _renderCts = new CancellationTokenSource();
            _renderTask = StartRenderAsync(_renderCts.Token);
        }

        private async Task StartRenderAsync(CancellationToken token)
        {
            if (canvas.Width <= 0 || canvas.Height <= 0) return;

            _isRendering = true;
            var stopwatch = Stopwatch.StartNew();

            // 1. Выбор движка в зависимости от зума
            IFractalEngine engine;
            string precisionText;

            if (_zoom < ZoomLevel2Threshold)
            {
                engine = new EngineDouble();
                precisionText = "Double";
            }
            else if (_zoom < ZoomLevel3Threshold)
            {
                engine = new EngineDecimal();
                precisionText = "Decimal (x1)";
            }
            else
            {
                engine = new EngineBig();
                precisionText = "BigDecimal (Deep)";
            }

            this.Text = $"{_baseTitle} - Точность: {precisionText}";

            // 2. Создание палитры и опций рендера
            ApplyActivePalette(engine);
            var options = new RenderOptions
            {
                Width = canvas.Width,
                Height = canvas.Height,
                CenterX = _centerX.ToString(),
                CenterY = _centerY.ToString(),
                Scale = (BaseScale / _zoom).ToString(),
                FractalType = this.GetFractalType(),
                JuliaC = (this is FractalJulia || this is FractalJuliaBurningShip) ? new ComplexDecimal(nudRe.Value, nudIm.Value) : new ComplexDecimal(0, 0),
                SsaaFactor = GetSelectedSsaaFactor(),
                NumThreads = GetThreadCount()
            };

            var currentRenderedCenterX = _centerX;
            var currentRenderedCenterY = _centerY;
            var currentRenderedZoom = _zoom;

            try
            {
                Bitmap newBitmap = await Task.Run(() => engine.Render(options, token), token);

                token.ThrowIfCancellationRequested();

                lock (_bitmapLock)
                {
                    _previewBitmap?.Dispose();
                    _previewBitmap = newBitmap;
                    _renderedCenterX = currentRenderedCenterX;
                    _renderedCenterY = currentRenderedCenterY;
                    _renderedZoom = currentRenderedZoom;
                }

                stopwatch.Stop();
                this.Text += $" - Время: {stopwatch.Elapsed.TotalSeconds:F3} сек.";
                if (canvas.IsHandleCreated && !canvas.IsDisposed) canvas.Invalidate();
            }
            catch (OperationCanceledException)
            {
                // Это ожидаемое исключение, когда мы начинаем новый рендер до завершения старого
            }
            catch (Exception ex)
            {
                if (IsHandleCreated && !IsDisposed) MessageBox.Show($"Ошибка рендеринга: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _isRendering = false;
            }
        }

        #endregion

        #region Utility Methods

        private void UpdateZoomUI()
        {
            // Безопасно обновляем nudZoom, чтобы избежать OverflowException
            if (_zoom.TryToDecimal(out decimal zoomDecimal))
            {
                // Отвязываем обработчик, чтобы избежать рекурсивного вызова
                nudZoom.ValueChanged -= NudZoom_ValueChanged;
                nudZoom.Value = ClampDecimal(zoomDecimal, nudZoom.Minimum, nudZoom.Maximum);
                nudZoom.ValueChanged += NudZoom_ValueChanged;
            }
            // Если преобразование не удалось, оставляем в nudZoom последнее (максимальное) значение
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

        private int GetThreadCount()
        {
            if (cbThreads.InvokeRequired)
            {
                return (int)cbThreads.Invoke(new Func<int>(GetThreadCount));
            }
            return cbThreads.SelectedItem?.ToString() == "Auto" ? Environment.ProcessorCount : Convert.ToInt32(cbThreads.SelectedItem);
        }

        private decimal ClampDecimal(decimal value, decimal min, decimal max) => Math.Max(min, Math.Min(max, value));

        private void OnVisualizerNeedsRedraw()
        {
            if (canvas.IsHandleCreated && !canvas.IsDisposed)
            {
                canvas.BeginInvoke((Action)(() => canvas.Invalidate()));
            }
        }

        #endregion

        #region Palette Management

        private void ApplyActivePalette(IFractalEngine engine)
        {
            engine.MaxIterations = (int)nudIterations.Value;
            engine.ThresholdSquared = (double)(nudThreshold.Value * nudThreshold.Value);

            var activePalette = _paletteManager.ActivePalette;
            if (activePalette == null) return;

            int effectiveMaxColorIterations = activePalette.AlignWithRenderIterations ? engine.MaxIterations : activePalette.MaxColorIterations;
            string newSignature = GeneratePaletteSignature(activePalette, engine.MaxIterations);

            if (_gammaCorrectedPaletteCache == null || newSignature != _paletteCacheSignature)
            {
                _paletteCacheSignature = newSignature;
                var paletteGeneratorFunc = GeneratePaletteFunction(activePalette);
                _gammaCorrectedPaletteCache = new Color[effectiveMaxColorIterations + 1];
                for (int i = 0; i <= effectiveMaxColorIterations; i++)
                {
                    _gammaCorrectedPaletteCache[i] = paletteGeneratorFunc(i, engine.MaxIterations, effectiveMaxColorIterations);
                }
            }

            engine.MaxColorIterations = effectiveMaxColorIterations;
            engine.Palette = (iter, maxIter, maxColorIter) =>
            {
                if (iter == maxIter) return Color.Black;
                int index = Math.Min(iter, _gammaCorrectedPaletteCache.Length - 1);
                return _gammaCorrectedPaletteCache[index];
            };
        }

        private string GeneratePaletteSignature(PaletteManagerMandelbrotFamily palette, int maxIterationsForAlignment)
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

        private Func<int, int, int, Color> GeneratePaletteFunction(PaletteManagerMandelbrotFamily palette)
        {
            double gamma = palette.Gamma;
            var colors = new List<Color>(palette.Colors);
            bool isGradient = palette.IsGradient;
            int colorCount = colors.Count;

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

        #endregion

        #region ISaveLoadCapableFractal Implementation (Adapted)

        public abstract string FractalTypeIdentifier { get; }
        public abstract Type ConcreteSaveStateType { get; }

        public virtual FractalSaveStateBase GetCurrentStateForSave(string saveName)
        {
            MandelbrotFamilySaveState state;
            if (this.GetFractalType() == FractalType.Julia || this.GetFractalType() == FractalType.JuliaBurningShip)
                state = new JuliaFamilySaveState(this.FractalTypeIdentifier);
            else
                state = new MandelbrotFamilySaveState(this.FractalTypeIdentifier);

            state.SaveName = saveName;
            state.Timestamp = DateTime.Now;
            state.Threshold = nudThreshold.Value;
            state.Iterations = (int)nudIterations.Value;
            state.PaletteName = _paletteManager.ActivePalette?.Name ?? "Стандартный серый";
            state.PreviewEngineType = this.FractalTypeIdentifier;

            // --- Безопасное сохранение BigDecimal ---
            if (!_centerX.TryToDecimal(out var centerXDecimal) ||
                !_centerY.TryToDecimal(out var centerYDecimal) ||
                !_zoom.TryToDecimal(out var zoomDecimal))
            {
                // Сохраняем "заглушки" для сверхглубоких состояний
                state.CenterX = 0;
                state.CenterY = 0;
                state.Zoom = decimal.MaxValue; // Флаг того, что это сверхглубокое состояние
            }
            else
            {
                state.CenterX = centerXDecimal;
                state.CenterY = centerYDecimal;
                state.Zoom = zoomDecimal;
            }

            if (state is JuliaFamilySaveState juliaState)
            {
                juliaState.CRe = nudRe.Value;
                juliaState.CIm = nudIm.Value;
            }

            // Логика PreviewParametersJson остается прежней, она использует decimal
            // что хорошо для превью.
            return state;
        }

        public virtual void LoadState(FractalSaveStateBase stateBase)
        {
            if (!(stateBase is MandelbrotFamilySaveState state))
            {
                MessageBox.Show("Несовместимый тип состояния для загрузки.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _renderCts?.Cancel();
            _renderDebounceTimer.Stop();

            // --- Загрузка с преобразованием в BigDecimal ---
            _centerX = new BigDecimal(state.CenterX);
            _centerY = new BigDecimal(state.CenterY);
            _zoom = new BigDecimal(state.Zoom);

            UpdateZoomUI();
            nudThreshold.Value = ClampDecimal(state.Threshold, nudThreshold.Minimum, nudThreshold.Maximum);
            nudIterations.Value = (int)ClampDecimal(state.Iterations, nudIterations.Minimum, nudIterations.Maximum);

            var paletteToLoad = _paletteManager.Palettes.FirstOrDefault(p => p.Name == state.PaletteName);
            if (paletteToLoad != null) _paletteManager.ActivePalette = paletteToLoad;

            if (state is JuliaFamilySaveState juliaState)
            {
                if (nudRe != null && nudIm != null && nudRe.Visible)
                {
                    nudRe.Value = ClampDecimal(juliaState.CRe, nudRe.Minimum, nudRe.Maximum);
                    nudIm.Value = ClampDecimal(juliaState.CIm, nudIm.Minimum, nudIm.Maximum);
                }
            }

            lock (_bitmapLock)
            {
                _previewBitmap?.Dispose(); _previewBitmap = null;
            }

            _renderedCenterX = _centerX;
            _renderedCenterY = _centerY;
            _renderedZoom = _zoom;
            ScheduleRender();
        }

        public virtual Bitmap RenderPreview(FractalSaveStateBase stateBase, int previewWidth, int previewHeight)
        {
            // --- Используем новый EngineDecimal для рендеринга превью ---
            if (!(stateBase is MandelbrotFamilySaveState state)) return new Bitmap(previewWidth, previewHeight);

            var engine = new EngineDecimal();
            ApplyActivePalette(engine); // Применяем палитру
            engine.MaxIterations = Math.Min(state.Iterations, 150); // Ограничиваем итерации для превью

            var juliaC = state is JuliaFamilySaveState js ? new ComplexDecimal(js.CRe, js.CIm) : ComplexDecimal.Zero;
            FractalType fractalType;
            Enum.TryParse(state.PreviewEngineType, out fractalType);

            var options = new RenderOptions
            {
                Width = previewWidth,
                Height = previewHeight,
                CenterX = state.CenterX.ToString(CultureInfo.InvariantCulture),
                CenterY = state.CenterY.ToString(CultureInfo.InvariantCulture),
                Scale = (BaseScale / new BigDecimal(state.Zoom)).ToString(),
                FractalType = fractalType,
                JuliaC = juliaC,
                NumThreads = 1 // Для превью достаточно одного потока
            };

            return engine.Render(options, CancellationToken.None);
        }

        #endregion

        #region Unused/Dummy Implementations for Interfaces
        // Эти части нужны для совместимости с интерфейсами, но их логика либо устарела, либо перенесена.
        public double LoupeZoom => nudBaseScale != null ? (double)nudBaseScale.Value : 4.0;
        public event EventHandler LoupeZoomChanged;
        public HighResRenderState GetRenderState() { return new HighResRenderState(); /* Заглушка */ }
        public async Task<Bitmap> RenderHighResolutionAsync(HighResRenderState state, int width, int height, int ssaaFactor, IProgress<RenderProgress> progress, CancellationToken cancellationToken)
        {
            // Можно реализовать позже, используя новый движок
            return await Task.FromResult(new Bitmap(width, height));
        }
        public Bitmap RenderPreview(HighResRenderState state, int previewWidth, int previewHeight)
        {
            return new Bitmap(previewWidth, previewHeight); // Заглушка
        }
        public virtual List<FractalSaveStateBase> LoadAllSavesForThisType() { throw new NotImplementedException(); }
        public virtual void SaveAllSavesForThisType(List<FractalSaveStateBase> saves) { throw new NotImplementedException(); }
        // Этот метод больше не используется, но может требоваться по зависимостям, которые мы не трогали
        private void btnOpenSaveManager_Click(object sender, EventArgs e) { }

        // Старый асинхронный метод для превью тайлов, который можно будет удалить или адаптировать позже
        public virtual async Task<byte[]> RenderPreviewTileAsync(FractalSaveStateBase stateBase, TileInfo tile, int totalWidth, int totalHeight, int tileSize)
        {
            return await Task.FromResult(new byte[0]);
        }
        #endregion
    }
}