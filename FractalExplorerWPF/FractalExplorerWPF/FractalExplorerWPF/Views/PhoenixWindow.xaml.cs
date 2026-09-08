using System.Diagnostics;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using FractalExplorerWPF.Core.NewtonMath;
using FractalExplorerWPF.Core.Rendering;
using FractalExplorerWPF.Controls;
using FractalExplorerWPF.Infrastructure;
using FractalExplorerWPF.Models;
using Microsoft.Win32;
using Point = System.Windows.Point;

namespace FractalExplorerWPF.Views;

public partial class PhoenixWindow : Window
{
    private const decimal BaseScale = 4m;
    private readonly DispatcherTimer _renderTimer = new() { Interval = TimeSpan.FromMilliseconds(350) };
    private readonly DispatcherTimer _visualizationTimer = new() { Interval = TimeSpan.FromMilliseconds(33) };
    private readonly MandelbrotPaletteManager _paletteManager = new();
    private readonly PhoenixSaveStore _saveStore = new();
    private CancellationTokenSource? _renderCts;
    private bool _isRendering, _panning, _isFullscreen, _controlsVisible = true;

    /// <summary>
    /// Окно само заполняет поле зума после колеса. Без этого флага обработчик изменения текста
    /// тут же прочитал бы округлённое до восьми цифр значение обратно в <see cref="_zoom"/> —
    /// уже после того, как по прежнему зуму посчитан сдвиг центра.
    /// </summary>
    private bool _updatingControls;
    private Point _lastPanPoint;
    private decimal _centerX, _centerY;
    private double _zoom = 1;
    private double _renderedZoom = 1;
    private bool _hasRenderedFrame;

    /// <summary>
    /// Центр области в произвольной точности. Ведётся начиная с
    /// <see cref="DeepZoomThreshold"/>, когда decimal (28 знаков) перестаёт различать соседние
    /// пиксели; ниже порога источником истины остаются <see cref="_centerX"/>/<see cref="_centerY"/>.
    /// </summary>
    private BigFloat _centerXExact, _centerYExact;
    private BigFloat _renderedCenterXExact, _renderedCenterYExact;
    private bool _deepZoomEngaged;

    /// <summary>
    /// Зум, начиная с которого окно ведёт центр в <see cref="BigFloat"/>. Совпадает с порогом
    /// включения пертурбационного движка в <c>PhoenixRenderer.DeepZoom</c>: смысла вести
    /// точный центр раньше нет, а после — обязательно, иначе движку неоткуда взять положение
    /// области с нужным числом знаков.
    /// </summary>
    private const double DeepZoomThreshold = 1.5e9;

    private const double MinZoom = 0.000001;

    /// <summary>
    /// Потолок зума. Прежде здесь стояло <c>decimal.MaxValue/2</c> (≈4e28) — не предел
    /// точности, а защита от переполнения: колесо умножает зум на 1.2 ДО ограничения. Картинка
    /// при этом рассыпалась уже около 1e12, то есть поле пускало заведомо дальше, чем движок
    /// мог посчитать.
    ///
    /// Теперь предел настоящий и измерен. Отклонение δ пертурбация ведёт в double, а
    /// ребазирование у Феникса почти не срабатывает: в начале орбиты <c>z₋₁ = 0</c>, поэтому
    /// перенос пары туда увеличивает вторую компоненту вместо того, чтобы уменьшить обе. Из-за
    /// этого ошибка δ копится по всей орбите без сброса. На кадре, где вся область вылетает за
    /// радиус в пределах одной-двух итераций (самый чувствительный случай — центр в точке
    /// границы), расхождение с точной BigFloat-итерацией начинается так: 1e22 — 0 пикселей,
    /// 1e24 — 1 из 1536, 1e26 — 6, 1e28 — уже 537. Потолок взят по последней глубине, где
    /// расхождение остаётся на уровне отдельных пикселей границы.
    ///
    /// Поднять его можно, начав вести δ с удвоенной разрядностью (double-double): предел
    /// сдвинется примерно на столько же порядков, на сколько прибавится значащих цифр.
    /// </summary>
    private const double MaxZoom = 1e24;
    private readonly TransformGroup _previewTransform = new();
    private readonly ScaleTransform _previewScale = new(1, 1);
    private readonly TranslateTransform _previewTranslation = new();
    private RenderSession? _activeSession;
    private WindowStyle _previousWindowStyle;
    private WindowState _previousWindowState;

    public PhoenixWindow()
    {
        InitializeComponent();
        _visualizationTimer.Tick += (_, _) => { if (_activeSession is not null) FlushVisualizationEvents(_activeSession, false); };
        _previewTransform.Children.Add(_previewScale);
        _previewTransform.Children.Add(_previewTranslation);
        StablePreviewImage.RenderTransformOrigin = new Point(0.5, 0.5);
        StablePreviewImage.RenderTransform = _previewTransform;
        _renderTimer.Tick += RenderTimer_OnTick;
        C1RealBox.Text = "0.56667"; C1ImaginaryBox.Text = "0"; C2RealBox.Text = "-0.5"; C2ImaginaryBox.Text = "0";
        PrimaryPowerBox.Text = "2"; SecondaryPowerBox.Text = "0";
        InitialZRealBox.Text = "0"; InitialZImaginaryBox.Text = "0";
        PreviousRealBox.Text = "0"; PreviousImaginaryBox.Text = "0";
        IterationsBox.Text = "300"; ThresholdBox.Text = "4"; ZoomBox.Text = "1";
        OrbitTrapRadiusBox.Text = "0.5"; OrbitTrapStrengthBox.Text = "1.5";
        StripeFrequencyBox.Text = "3"; StripeStrengthBox.Text = "0.65";
        CycleToleranceBox.Text = "0.0000001"; MaximumPeriodBox.Text = "32";
        for (int count = 1; count <= Environment.ProcessorCount; count++) ThreadsBox.Items.Add(count);
        ThreadsBox.Items.Add("Auto"); ThreadsBox.SelectedItem = "Auto";
        PlaneModeBox.SelectedIndex = 0; VariantBox.SelectedIndex = 0; ColoringBox.SelectedIndex = 1;
        OrbitTrapBox.SelectedIndex = 0; SsaaBox.SelectedIndex = 0;
        UpdatePlaneUi(); UpdateColoringPanels();
        Loaded += (_, _) => ScheduleRender();
    }

    public PhoenixState CaptureState(string name)
    {
        if (!TryRead(C1RealBox.Text, out decimal c1r) || !TryRead(C1ImaginaryBox.Text, out decimal c1i) ||
            !TryRead(C2RealBox.Text, out decimal c2r) || !TryRead(C2ImaginaryBox.Text, out decimal c2i) ||
            !TryRead(InitialZRealBox.Text, out decimal initialZr) || !TryRead(InitialZImaginaryBox.Text, out decimal initialZi) ||
            !TryRead(PreviousRealBox.Text, out decimal previousZr) || !TryRead(PreviousImaginaryBox.Text, out decimal previousZi) ||
            !TryRead(ThresholdBox.Text, out decimal threshold) || threshold is < 2 or > 1000 ||
            !int.TryParse(IterationsBox.Text, out int iterations) || iterations is < 10 or > 100_000 ||
            !int.TryParse(PrimaryPowerBox.Text, out int primaryPower) || primaryPower is < 2 or > 12 ||
            !int.TryParse(SecondaryPowerBox.Text, out int secondaryPower) || secondaryPower is < 0 or > 12 ||
            !TryRead(OrbitTrapRadiusBox.Text, out decimal trapRadius) || trapRadius is < 0 or > 1000 ||
            !TryRead(OrbitTrapStrengthBox.Text, out decimal trapStrength) || trapStrength is <= 0 or > 1000 ||
            !TryRead(StripeFrequencyBox.Text, out decimal stripeFrequency) || stripeFrequency is < 0 or > 1000 ||
            !TryRead(StripeStrengthBox.Text, out decimal stripeStrength) || stripeStrength is < 0 or > 1 ||
            !TryRead(CycleToleranceBox.Text, out decimal cycleTolerance) || cycleTolerance is <= 0 or > 0.1m ||
            !int.TryParse(MaximumPeriodBox.Text, out int maximumPeriod) || maximumPeriod is < 1 or > 64)
            throw new InvalidOperationException("Проверьте C1/C2, степени a (2–12) и b (0–12), итерации, радиус выхода и параметры окраски.");
        return new PhoenixState
        {
            SaveName = name, Timestamp = DateTime.Now,
            CenterX = _deepZoomEngaged ? _centerXExact.ToDecimalClamped() : _centerX,
            CenterY = _deepZoomEngaged ? _centerYExact.ToDecimalClamped() : _centerY,
            CenterXExact = _deepZoomEngaged ? _centerXExact.ToInvariantString() : null,
            CenterYExact = _deepZoomEngaged ? _centerYExact.ToInvariantString() : null,
            Zoom = _zoom,
            Threshold = threshold, Iterations = iterations, C1Real = c1r, C1Imaginary = c1i, C2Real = c2r, C2Imaginary = c2i,
            PlaneMode = GetSelectedEnum(PlaneModeBox, PhoenixPlaneMode.Julia),
            Variant = GetSelectedEnum(VariantBox, PhoenixVariant.Classic),
            PrimaryPower = primaryPower, SecondaryPower = secondaryPower,
            InitialZReal = initialZr, InitialZImaginary = initialZi,
            InitialPreviousReal = previousZr, InitialPreviousImaginary = previousZi,
            ColoringMode = GetSelectedEnum(ColoringBox, PhoenixColoringMode.Smooth),
            OrbitTrapMode = GetSelectedEnum(OrbitTrapBox, PhoenixOrbitTrapMode.Axes),
            OrbitTrapRadius = (double)trapRadius, OrbitTrapStrength = (double)trapStrength,
            StripeFrequency = (double)stripeFrequency, StripeStrength = (double)stripeStrength,
            CycleTolerance = (double)cycleTolerance, MaximumDetectedPeriod = maximumPeriod,
            Palette = _paletteManager.ActivePalette.Clone(_paletteManager.ActivePalette.Name)
        };
    }

    public void LoadState(PhoenixState state)
    {
        _renderCts?.Cancel(); _centerX = state.CenterX; _centerY = state.CenterY;
        _zoom = Math.Clamp(state.Zoom, MinZoom, MaxZoom);
        _deepZoomEngaged = false;
        if (state.CenterXExact is { Length: > 0 } exactX && state.CenterYExact is { Length: > 0 } exactY)
        {
            try
            {
                _centerXExact = BigFloat.Parse(exactX);
                _centerYExact = BigFloat.Parse(exactY);
                _deepZoomEngaged = _zoom >= DeepZoomThreshold;
                if (_deepZoomEngaged)
                {
                    _centerX = _centerXExact.ToDecimalClamped();
                    _centerY = _centerYExact.ToDecimalClamped();
                }
            }
            catch (FormatException)
            {
                // Испорченная строка точного центра — не повод не открыть сохранение:
                // decimal-поля рядом задают ту же точку с точностью до 28 знаков.
                _deepZoomEngaged = false;
            }
        }
        if (!_deepZoomEngaged) SyncDeepZoomState();
        C1RealBox.Text = Format(state.C1Real); C1ImaginaryBox.Text = Format(state.C1Imaginary); C2RealBox.Text = Format(state.C2Real); C2ImaginaryBox.Text = Format(state.C2Imaginary);
        PrimaryPowerBox.Text = state.PrimaryPower.ToString(CultureInfo.InvariantCulture); SecondaryPowerBox.Text = state.SecondaryPower.ToString(CultureInfo.InvariantCulture);
        InitialZRealBox.Text = Format(state.InitialZReal); InitialZImaginaryBox.Text = Format(state.InitialZImaginary);
        PreviousRealBox.Text = Format(state.InitialPreviousReal); PreviousImaginaryBox.Text = Format(state.InitialPreviousImaginary);
        IterationsBox.Text = state.Iterations.ToString(CultureInfo.InvariantCulture); ThresholdBox.Text = Format(state.Threshold);
        _updatingControls = true; ZoomBox.Text = FormatZoom(_zoom); _updatingControls = false;
        OrbitTrapRadiusBox.Text = state.OrbitTrapRadius.ToString("G15", CultureInfo.InvariantCulture);
        OrbitTrapStrengthBox.Text = state.OrbitTrapStrength.ToString("G15", CultureInfo.InvariantCulture);
        StripeFrequencyBox.Text = state.StripeFrequency.ToString("G15", CultureInfo.InvariantCulture);
        StripeStrengthBox.Text = state.StripeStrength.ToString("G15", CultureInfo.InvariantCulture);
        CycleToleranceBox.Text = state.CycleTolerance.ToString("G15", CultureInfo.InvariantCulture);
        MaximumPeriodBox.Text = state.MaximumDetectedPeriod.ToString(CultureInfo.InvariantCulture);
        SelectByTag(PlaneModeBox, state.PlaneMode); SelectByTag(VariantBox, state.Variant);
        SelectByTag(ColoringBox, state.ColoringMode); SelectByTag(OrbitTrapBox, state.OrbitTrapMode);
        _paletteManager.ActivePalette = state.Palette.Clone($"Загружено: {state.SaveName}");
        UpdatePlaneUi(); UpdateColoringPanels();
        UpdatePreviewTransform(); ScheduleRender();
    }

    public BitmapSource? CaptureCurrentPreview(int width, int height) =>
        SavePreviewCapture.Capture(SavePreviewLayer, CanvasHost.Background, width, height, StablePreviewImage, CanvasImage);

    public Task<BitmapSource> RenderStatePreviewAsync(
        PhoenixState state, int width, int height, CancellationToken token, IProgress<int>? progress = null) =>
        RenderBitmapAsync(state, width, height, 1, token, progress);

    private void ParameterSelector_OnClick(object sender, RoutedEventArgs e)
    {
        PhoenixState state;
        try { state = CaptureState("parameter-explorer"); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Параметры Phoenix", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        var dialog = new PhoenixParameterExplorerWindow(state) { Owner = this };
        if (dialog.ShowDialog() != true) return;
        C1RealBox.Text = Format(dialog.SelectedC1Real); C1ImaginaryBox.Text = Format(dialog.SelectedC1Imaginary);
        C2RealBox.Text = Format(dialog.SelectedC2Real); C2ImaginaryBox.Text = Format(dialog.SelectedC2Imaginary);
        if (dialog.OpenC1AsJulia)
        {
            SelectByTag(PlaneModeBox, PhoenixPlaneMode.Julia);
            ResetView(); UpdatePlaneUi();
        }
        ScheduleRender();
    }

    private void PlaneModeBox_OnChanged(object sender, SelectionChangedEventArgs e) { UpdatePlaneUi(); ScheduleRender(); }
    private void ColoringBox_OnChanged(object sender, SelectionChangedEventArgs e) { UpdateColoringPanels(); ScheduleRender(); }
    private void UpdatePlaneUi()
    {
        if (NavigationHint is null) return;
        bool parameterPlane = GetSelectedEnum(PlaneModeBox, PhoenixPlaneMode.Julia) == PhoenixPlaneMode.ParameterC1;
        NavigationHint.Text = parameterPlane
            ? "Колесо: масштаб. ЛКМ: перемещение. Двойной щелчок открывает выбранный C1 как динамическую плоскость. F11: полный экран." +
              (UsesAutomaticParameterStartFromInputs()
                  ? " При b > 0 и нулевых z₀/z₋₁ для невырожденной карты автоматически используется z₀ = 1."
                  : string.Empty)
            : "Колесо: масштаб. ЛКМ: перемещение. F11: полноэкранный режим.";
    }

    private bool UsesAutomaticParameterStartFromInputs() =>
        int.TryParse(SecondaryPowerBox.Text, out int secondaryPower) && secondaryPower > 0 &&
        TryRead(InitialZRealBox.Text, out decimal initialZr) && initialZr == 0 &&
        TryRead(InitialZImaginaryBox.Text, out decimal initialZi) && initialZi == 0 &&
        TryRead(PreviousRealBox.Text, out decimal previousZr) && previousZr == 0 &&
        TryRead(PreviousImaginaryBox.Text, out decimal previousZi) && previousZi == 0;

    private void UpdateColoringPanels()
    {
        if (OrbitTrapPanel is null) return;
        PhoenixColoringMode mode = GetSelectedEnum(ColoringBox, PhoenixColoringMode.Smooth);
        OrbitTrapPanel.Visibility = mode == PhoenixColoringMode.OrbitTrap ? Visibility.Visible : Visibility.Collapsed;
        StripePanel.Visibility = mode == PhoenixColoringMode.StripeAverage ? Visibility.Visible : Visibility.Collapsed;
        PeriodPanel.Visibility = mode == PhoenixColoringMode.Period ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Parameter_OnChanged(object sender, EventArgs e) { UpdatePlaneUi(); ScheduleRender(); }
    private void ZoomBox_OnChanged(object sender, TextChangedEventArgs e)
    {
        // Выход до всего остального: поле заполняет само окно после колеса, и обратное чтение
        // округлило бы зум уже после того, как по прежнему значению посчитан сдвиг центра.
        if (_updatingControls) return;
        if (TryReadDouble(ZoomBox.Text, out double zoom))
        {
            _zoom = Math.Clamp(zoom, MinZoom, MaxZoom);
            SyncDeepZoomState();
            UpdatePreviewTransform();
            ScheduleRender();
        }
    }
    private void RenderButton_OnClick(object sender, RoutedEventArgs e) => _ = RenderPreviewAsync();
    private void CancelButton_OnClick(object sender, RoutedEventArgs e) => _renderCts?.Cancel();

    private void PaletteButton_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new MandelbrotPaletteWindow(_paletteManager) { Owner = this };
        dialog.PaletteApplied += (_, _) => ScheduleRender(); dialog.ShowDialog();
    }

    private void SavesButton_OnClick(object sender, RoutedEventArgs e) =>
        SaveManagerWindow.Open(this, SaveManagerConfigurations.ForPhoenix(this, _saveStore));

    private void ExportButton_OnClick(object sender, RoutedEventArgs e)
    {
        RenderSurfaceMetrics surface = RenderSurfaceMetrics.Measure(CanvasHost);
        _renderCts?.Cancel();
        PhoenixState state;
        try { state = CaptureState("export"); }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Параметры экспорта", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        ImageExportManagerWindow.Open(this, new ImageExportConfiguration
        {
            FileNamePrefix = "phoenix",
            InitialWidth = surface.PixelWidth,
            InitialHeight = surface.PixelHeight,
            MaxSsaaFactor = 4,
            RenderAsync = (request, token, progress) => RenderBitmapAsync(state, request.Width,
                request.Height, request.SsaaFactor, token, progress)
        });
    }

    private void ScheduleRender() { if (!IsLoaded) return; if (_isRendering) CommitAndBakePreview(); else _renderCts?.Cancel(); _renderTimer.Stop(); _renderTimer.Start(); }

    /// <summary>
    /// Останавливает текущий рендер и «запекает» то, что уже видно на холсте (сдвинутый
    /// прежний кадр плюс успевшие лечь тайлы), в стабильный предпросмотр. Снимок после
    /// этого считается отрисованным для текущего вида, поэтому дальнейшие зум и
    /// перетаскивание двигают именно его.
    ///
    /// Без этого при зуме терялся уже посчитанный кадр, а при перетаскивании рендер
    /// продолжал идти поверх уезжающего фона.
    /// </summary>
    private void CommitAndBakePreview()
    {
        RenderSession? session = _activeSession;
        _renderCts?.Cancel();
        if (session is null) return;

        FlushVisualizationEvents(session, true);
        RenderSurfaceMetrics surface = RenderSurfaceMetrics.Measure(SavePreviewLayer);
        try
        {
            var baked = new RenderTargetBitmap(surface.PixelWidth, surface.PixelHeight,
                surface.Dpi.PixelsPerInchX, surface.Dpi.PixelsPerInchY, PixelFormats.Pbgra32);
            baked.Render(SavePreviewLayer);
            baked.Freeze();
            StablePreviewImage.Source = baked;
            _renderedCenterXExact = _deepZoomEngaged ? _centerXExact : BigFloat.FromDecimal(_centerX);
            _renderedCenterYExact = _deepZoomEngaged ? _centerYExact : BigFloat.FromDecimal(_centerY);
            _renderedZoom = _zoom;
            _hasRenderedFrame = true;
            UpdatePreviewTransform();
        }
        catch (InvalidOperationException)
        {
            // Разметка бывает недоступна на свёртывании окна и в момент изменения размера.
        }
        CanvasImage.Source = null;
        RenderOverlay.EndSession();
        if (ReferenceEquals(_activeSession, session)) _activeSession = null;
    }
    private void RenderTimer_OnTick(object? sender, EventArgs e) { _renderTimer.Stop(); _ = RenderPreviewAsync(); }

    private async Task RenderPreviewAsync()
    {
        if (_isRendering) { ScheduleRender(); return; }
        PhoenixState state; try { state = CaptureState("preview"); } catch (Exception ex) { StatusText.Text = ex.Message; return; }
        _renderCts?.Dispose(); _renderCts = new CancellationTokenSource(); CancellationToken token = _renderCts.Token;
        var watch = Stopwatch.StartNew();
        string planeName = state.PlaneMode == PhoenixPlaneMode.Julia ? "динамической плоскости" : "параметрической плоскости C1";
        SetRendering(true, $"Рендеринг {planeName}...");
        try
        {
            int factor = SsaaBox.SelectedItem is ComboBoxItem item ? Convert.ToInt32(item.Tag, CultureInfo.InvariantCulture) : 1;
            RenderSurfaceMetrics surface = RenderSurfaceMetrics.Measure(CanvasHost);
            DpiScale dpi = surface.Dpi;
            int pixelWidth = surface.PixelWidth;
            int pixelHeight = surface.PixelHeight;
            int renderWidth = checked(pixelWidth * factor);
            int renderHeight = checked(pixelHeight * factor);
            TileSchedulingStrategy strategy = RenderPatternSettings.SelectedPattern;
            IReadOnlyList<MandelbrotRenderTile> tiles = MandelbrotTileScheduler.Create(renderWidth, renderHeight, 16 * factor, strategy);
            WriteableBitmap bitmap = ProgressiveRenderBitmap.CreateOverlay(renderWidth, renderHeight, dpi.PixelsPerInchX, dpi.PixelsPerInchY);
            var session = new RenderSession(bitmap, tiles.Count, renderWidth, renderHeight);
            _activeSession = session;
            CanvasImage.Source = bitmap;
            RenderOverlay.BeginSession(renderWidth, renderHeight);
            _visualizationTimer.Start();
            await RenderTilesAsync(state, tiles, session, GetThreadCount(), token);
            if (token.IsCancellationRequested) { CanvasImage.Source = null; StatusText.Text = "Рендер отменён"; return; }
            FlushVisualizationEvents(session, true);
            BitmapSource completed = session.Bitmap.Clone();
            completed.Freeze();
            StablePreviewImage.Source = completed;
            CanvasImage.Source = null;
            // Центр берём из состояния, которым кадр посчитан, а не из текущего: пока шёл
            // рендер, пользователь мог уже сдвинуть вид.
            _renderedCenterXExact = state.CenterXExact is { Length: > 0 } renderedX
                ? BigFloat.Parse(renderedX)
                : BigFloat.FromDecimal(state.CenterX);
            _renderedCenterYExact = state.CenterYExact is { Length: > 0 } renderedY
                ? BigFloat.Parse(renderedY)
                : BigFloat.FromDecimal(state.CenterY);
            _renderedZoom = state.Zoom;
            _hasRenderedFrame = true;
            UpdatePreviewTransform();
            RenderOverlay.EndSession();
            _activeSession = null;
            StatusText.Text = $"Готово за {watch.Elapsed.TotalSeconds:F3} сек. {state.PlaneMode}, {state.Variant}, a={state.PrimaryPower}, b={state.SecondaryPower}. Стратегия: {strategy}.";
        }
        catch (OperationCanceledException) { CanvasImage.Source = null; StatusText.Text = "Рендер отменён"; }
        catch (Exception ex) { StatusText.Text = "Ошибка рендера"; MessageBox.Show(this, ex.Message, "Phoenix", MessageBoxButton.OK, MessageBoxImage.Error); }
        finally { _visualizationTimer.Stop(); RenderOverlay.EndSession(); _activeSession = null; SetRendering(false); }
    }

    private static async Task RenderTilesAsync(PhoenixState state, IReadOnlyList<MandelbrotRenderTile> tiles,
        RenderSession session, int threadCount, CancellationToken token)
    {
        var queue = new ConcurrentQueue<MandelbrotRenderTile>(tiles);
        Task[] workers = Enumerable.Range(0, Math.Clamp(threadCount, 1, Environment.ProcessorCount)).Select(_ => Task.Run(() =>
        {
            while (queue.TryDequeue(out MandelbrotRenderTile tile))
            {
                if (token.IsCancellationRequested) return;
                session.Events.Enqueue(new TileRenderEvent(true, tile, null));
                byte[]? pixels = PhoenixRenderer.RenderTile(state, session.RenderWidth, session.RenderHeight, tile, token);
                if (pixels is null || token.IsCancellationRequested) return;
                session.Events.Enqueue(new TileRenderEvent(false, tile, pixels));
            }
        })).ToArray();
        await Task.WhenAll(workers);
    }

    private void FlushVisualizationEvents(RenderSession session, bool drainAll)
    {
        int processed = 0; bool changed = false;
        while ((drainAll || processed < 512) && session.Events.TryDequeue(out TileRenderEvent entry))
        {
            if (entry.IsStart) RenderOverlay.StartTile(entry.Tile);
            else if (entry.Pixels is not null)
            {
                if (ProgressiveRenderBitmap.WriteTile(session.Bitmap, entry.Tile, entry.Pixels))
                {
                    RenderOverlay.CompleteTile(entry.Tile); session.CompletedTiles++;
                }
            }
            processed++; changed = true;
        }
        if (!changed) return;
        RenderOverlay.Refresh();
        RenderProgress.Value = session.TileCount == 0 ? 0 : session.CompletedTiles * 100.0 / session.TileCount;
    }

    private async Task<BitmapSource> RenderBitmapAsync(PhoenixState state, int width, int height, int ssaa, CancellationToken token, IProgress<int>? progress)
    {
        int factor = Math.Clamp(ssaa, 1, 4), rw = checked(width * factor), rh = checked(height * factor), stride = checked(rw * 4);
        // WPF controls belong to the dispatcher thread. Snapshot the selected value
        // before Task.Run so the renderer never touches ThreadsBox/ComboBoxItem.
        int threadCount = GetThreadCount();
        byte[] pixels = new byte[checked(stride * rh)];
        await Task.Run(() => PhoenixRenderer.Render(state, pixels, rw, rh, stride, threadCount, token, v => progress?.Report(factor == 1 ? v : v * 90 / 100)));
        BitmapSource source = BitmapSource.Create(rw, rh, 96, 96, PixelFormats.Bgra32, null, pixels, stride); source.Freeze();
        return factor == 1 || token.IsCancellationRequested ? source : await Task.Run(() => BitmapResampler.ResizeLanczos3(source, width, height, token, v => progress?.Report(v)));
    }

    private int GetThreadCount() => ThreadsBox.SelectedItem?.ToString() == "Auto" ? Environment.ProcessorCount : Math.Max(1, Convert.ToInt32(ThreadsBox.SelectedItem, CultureInfo.InvariantCulture));
    private void SetRendering(bool value, string? status = null) { _isRendering = value; CancelButton.IsEnabled = value; if (!value) RenderProgress.Value = 0; if (status is not null) StatusText.Text = status; }
    private void CanvasHost_OnSizeChanged(object sender, SizeChangedEventArgs e) { UpdatePreviewTransform(); ScheduleRender(); }

    private void CanvasHost_OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        // Запекаем до изменения зума: снимок должен соответствовать прежнему виду.
        CommitAndBakePreview();
        Point mouse = e.GetPosition(CanvasHost);
        double width = Math.Max(1, CanvasHost.ActualWidth);
        double height = Math.Max(1, CanvasHost.ActualHeight);
        double fractionX = mouse.X / width - 0.5;
        double fractionY = height / 2 - mouse.Y;

        double previousZoom = _zoom;
        _zoom = Math.Clamp(_zoom * (e.Delta > 0 ? 1.2 : 1 / 1.2), MinZoom, MaxZoom);

        // Точка под курсором остаётся на месте. Прежняя формула «мир до минус мир после»,
        // записанная через разность ширин области: сам сдвиг мал и укладывается в double, а
        // ApplyCenterShift кладёт его в BigFloat-центр на глубине и в decimal на мелком зуме.
        double viewWidthDelta = (double)BaseScale / previousZoom - (double)BaseScale / _zoom;
        double shiftX = fractionX * viewWidthDelta;
        double shiftY = fractionY / width * viewWidthDelta;

        SyncDeepZoomState();
        ApplyCenterShift(shiftX, shiftY);

        UpdatePreviewTransform();
        _updatingControls = true; ZoomBox.Text = FormatZoom(_zoom); _updatingControls = false;
        ScheduleRender();
    }
    private void CanvasHost_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        CommitAndBakePreview();
        Point point = e.GetPosition(CanvasHost);
        if (e.ClickCount >= 2 && GetSelectedEnum(PlaneModeBox, PhoenixPlaneMode.Julia) == PhoenixPlaneMode.ParameterC1)
        {
            (decimal X, decimal Y) selected = ScreenToWorld(point);
            C1RealBox.Text = Format(selected.X); C1ImaginaryBox.Text = Format(selected.Y);
            SelectByTag(PlaneModeBox, PhoenixPlaneMode.Julia);
            ResetView();
            UpdatePlaneUi(); UpdatePreviewTransform(); ScheduleRender(); e.Handled = true;
            return;
        }
        _panning = true; _lastPanPoint = point; CanvasHost.CaptureMouse(); Mouse.OverrideCursor = Cursors.SizeAll;
    }
    private void CanvasHost_OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_panning) return;
        Point current = e.GetPosition(CanvasHost);
        double width = Math.Max(1, CanvasHost.ActualWidth);
        double viewWidth = (double)BaseScale / _zoom;
        ApplyCenterShift((_lastPanPoint.X - current.X) / width * viewWidth,
            (current.Y - _lastPanPoint.Y) / width * viewWidth);
        _lastPanPoint = current;
        UpdatePreviewTransform();
    }
    private void CanvasHost_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e) { if (!_panning) return; _panning = false; CanvasHost.ReleaseMouseCapture(); Mouse.OverrideCursor = null; ScheduleRender(); }

    /// <summary>
    /// Экранная точка в мировые координаты. Нужна только для выбора константы C1 двойным
    /// щелчком, а C1 хранится в decimal, поэтому результат тоже decimal: на глубине разность
    /// от центра считается в double, а к центру прибавляется уже в BigFloat.
    /// </summary>
    private (decimal X, decimal Y) ScreenToWorld(Point point)
    {
        double width = Math.Max(1, CanvasHost.ActualWidth);
        double viewWidth = (double)BaseScale / _zoom;
        double offsetX = (point.X - width / 2) * viewWidth / width;
        double offsetY = (Math.Max(1, CanvasHost.ActualHeight) / 2 - point.Y) * viewWidth / width;
        if (!_deepZoomEngaged) return (_centerX + (decimal)offsetX, _centerY + (decimal)offsetY);
        return ((_centerXExact + BigFloat.FromDouble(offsetX)).ToDecimalClamped(),
            (_centerYExact + BigFloat.FromDouble(offsetY)).ToDecimalClamped());
    }

    /// <summary>
    /// Прибавляет к центру небольшой сдвиг в мировых координатах. На глубине сдвиг уходит в
    /// BigFloat-центр (decimal-приближение обновляется следом), на мелком зуме — в decimal.
    /// </summary>
    private void ApplyCenterShift(double shiftX, double shiftY)
    {
        if (_deepZoomEngaged)
        {
            _centerXExact += BigFloat.FromDouble(shiftX);
            _centerYExact += BigFloat.FromDouble(shiftY);
            _centerX = _centerXExact.ToDecimalClamped();
            _centerY = _centerYExact.ToDecimalClamped();
        }
        else
        {
            _centerX += (decimal)shiftX;
            _centerY += (decimal)shiftY;
        }
    }

    /// <summary>
    /// Заводит или глушит ведение центра в BigFloat по текущему зуму. Вверх через порог центр
    /// переносится из decimal, вниз decimal снова становится источником истины.
    /// </summary>
    private void SyncDeepZoomState()
    {
        bool shouldEngage = _zoom >= DeepZoomThreshold;
        if (shouldEngage && !_deepZoomEngaged)
        {
            _centerXExact = BigFloat.FromDecimal(_centerX);
            _centerYExact = BigFloat.FromDecimal(_centerY);
            _deepZoomEngaged = true;
        }
        else if (!shouldEngage && _deepZoomEngaged)
        {
            _centerX = _centerXExact.ToDecimalClamped();
            _centerY = _centerYExact.ToDecimalClamped();
            _deepZoomEngaged = false;
        }
    }

    private void UpdatePreviewTransform()
    {
        if (!_hasRenderedFrame || _renderedZoom <= 0 || _zoom <= 0 || CanvasHost.ActualWidth <= 0) return;
        double scale = _zoom / _renderedZoom;
        double currentScale = (double)BaseScale / _zoom;
        double width = CanvasHost.ActualWidth;
        BigFloat currentCenterX = _deepZoomEngaged ? _centerXExact : BigFloat.FromDecimal(_centerX);
        BigFloat currentCenterY = _deepZoomEngaged ? _centerYExact : BigFloat.FromDecimal(_centerY);
        _previewScale.ScaleX = scale;
        _previewScale.ScaleY = scale;
        _previewTranslation.X = (_renderedCenterXExact - currentCenterX).ToDouble() / currentScale * width;
        _previewTranslation.Y = (currentCenterY - _renderedCenterYExact).ToDouble() / currentScale * width;
    }
    private void ToggleControlsButton_OnClick(object sender, RoutedEventArgs e) => FractalControlPanel.Toggle(ref _controlsVisible, ControlsColumn, ControlsHost, ToggleControlsButton, 310, ScheduleRender);
    private void Window_OnKeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.F11 || e.Key == Key.Escape && _isFullscreen) ToggleFullscreen(); }
    private void ToggleFullscreen() { if (!_isFullscreen) { _previousWindowStyle = WindowStyle; _previousWindowState = WindowState; WindowStyle = WindowStyle.None; WindowState = WindowState.Maximized; } else { WindowStyle = _previousWindowStyle; WindowState = _previousWindowState; } _isFullscreen = !_isFullscreen; }
    private void Window_OnClosing(object? sender, System.ComponentModel.CancelEventArgs e) { _renderTimer.Stop(); _visualizationTimer.Stop(); _renderCts?.Cancel(); _renderCts?.Dispose(); }
    /// <summary>
    /// Возврат к исходному виду: центр в нуле, зум 1.
    ///
    /// Обнуляются обе пары полей сразу, и ведение центра в BigFloat гасится напрямую, а не
    /// через <see cref="SyncDeepZoomState"/>: тот при спуске с глубины восстанавливает decimal
    /// из BigFloat-полей и вернул бы прежний центр поверх только что обнулённого.
    /// </summary>
    private void ResetView()
    {
        _centerX = 0; _centerY = 0; _zoom = 1;
        _centerXExact = BigFloat.Zero; _centerYExact = BigFloat.Zero;
        _deepZoomEngaged = false;
        _updatingControls = true; ZoomBox.Text = "1"; _updatingControls = false;
    }

    private static bool TryRead(string text, out decimal value) => decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) || decimal.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value);
    private static bool TryReadDouble(string text, out double value) => double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) || double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value);
    private static string Format(decimal value) => value.ToString("G15", CultureInfo.InvariantCulture);

    /// <summary>
    /// Зум показывается восемью значащими цифрами: большое значение уходит в
    /// экспоненциальную запись (8.1707708E+09) и помещается в поле целиком.
    /// </summary>
    private static string FormatZoom(double value) => value.ToString("G8", CultureInfo.InvariantCulture);

    private static TEnum GetSelectedEnum<TEnum>(ComboBox comboBox, TEnum fallback) where TEnum : struct, Enum
    {
        string? tag = (comboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
        return Enum.TryParse(tag, out TEnum result) ? result : fallback;
    }

    private static void SelectByTag<TEnum>(ComboBox comboBox, TEnum value) where TEnum : struct, Enum
    {
        string expected = value.ToString();
        foreach (object item in comboBox.Items)
        {
            if (item is ComboBoxItem comboItem && string.Equals(comboItem.Tag?.ToString(), expected, StringComparison.Ordinal))
            {
                comboBox.SelectedItem = comboItem;
                return;
            }
        }
    }

    private sealed class RenderSession(WriteableBitmap bitmap, int tileCount, int renderWidth, int renderHeight)
    {
        public WriteableBitmap Bitmap { get; } = bitmap;
        public int TileCount { get; } = tileCount;
        public int RenderWidth { get; } = renderWidth;
        public int RenderHeight { get; } = renderHeight;
        public int CompletedTiles { get; set; }
        public ConcurrentQueue<TileRenderEvent> Events { get; } = new();
    }
    private readonly record struct TileRenderEvent(bool IsStart, MandelbrotRenderTile Tile, byte[]? Pixels);
}
