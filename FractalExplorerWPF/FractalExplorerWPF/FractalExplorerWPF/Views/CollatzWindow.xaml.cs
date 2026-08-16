using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using FractalExplorerWPF.Core.Rendering;
using FractalExplorerWPF.Controls;
using FractalExplorerWPF.Infrastructure;
using FractalExplorerWPF.Models;
using Microsoft.Win32;
using Point = System.Windows.Point;

namespace FractalExplorerWPF.Views;

public partial class CollatzWindow : Window
{
    private const decimal BaseScale = 4m;
    private readonly DispatcherTimer _renderTimer = new() { Interval = TimeSpan.FromMilliseconds(350) };
    private readonly DispatcherTimer _visualizationTimer = new() { Interval = TimeSpan.FromMilliseconds(33) };
    private readonly CollatzPaletteManager _paletteManager = new();
    private readonly CollatzSaveStore _saveStore = new();
    private readonly TransformGroup _previewTransform = new();
    private readonly ScaleTransform _previewScale = new(1, 1);
    private readonly TranslateTransform _previewTranslation = new();
    private CancellationTokenSource? _renderCts;
    private RenderSession? _activeSession;
    private bool _isRendering, _panning, _isFullscreen, _controlsVisible = true, _hasRenderedFrame,
        _updatingControls;
    private Point _lastPanPoint;
    private decimal _centerX, _centerY, _zoom = 1;
    private decimal _renderedCenterX, _renderedCenterY, _renderedZoom = 1;
    private WindowStyle _previousWindowStyle;
    private WindowState _previousWindowState;

    public CollatzWindow()
    {
        InitializeComponent();
        _updatingControls = true;
        _previewTransform.Children.Add(_previewScale);
        _previewTransform.Children.Add(_previewTranslation);
        StablePreviewImage.RenderTransformOrigin = new Point(0.5, 0.5);
        StablePreviewImage.RenderTransform = _previewTransform;
        _visualizationTimer.Tick += (_, _) =>
        {
            if (_activeSession is not null) FlushVisualizationEvents(_activeSession, false);
        };
        _renderTimer.Tick += RenderTimer_OnTick;
        IterationsBox.Text = "150";
        ThresholdBox.Text = "100";
        ZoomBox.Text = "1";
        PParameterBox.Text = "3";
        QRealParameterBox.Text = "0";
        QImaginaryParameterBox.Text = "0";
        VariationBox.SelectedIndex = 0;
        ColoringModeBox.SelectedIndex = 0;
        EscapeSmoothingBox.SelectedIndex = 1;
        ArgumentCyclesBox.Text = "1";
        MagnitudeScaleBox.Text = "1";
        TrapScaleBox.Text = "4";
        CycleToleranceBox.Text = "1e-6";
        MaximumPeriodBox.Text = "32";
        DensityExposureBox.Text = "1";
        DensitySampleStepBox.Text = "2";
        DensityEscapedOnlyBox.IsChecked = true;
        SsaaBox.SelectedIndex = 0;
        for (int count = 1; count <= Environment.ProcessorCount; count++) ThreadsBox.Items.Add(count);
        ThreadsBox.Items.Add("Auto");
        ThreadsBox.SelectedItem = "Auto";
        UpdateVariationControls();
        UpdateColoringControls();
        _updatingControls = false;
        Loaded += (_, _) => ScheduleRender();
    }

    public CollatzState CaptureState(string name)
    {
        if (!int.TryParse(IterationsBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int iterations) || iterations is < 10 or > 100_000)
            throw new InvalidOperationException("Итерации должны быть целым числом от 10 до 100000.");
        if (!TryRead(ThresholdBox.Text, out decimal threshold) || threshold is < 2 or > 10_000)
            throw new InvalidOperationException("Порог выхода должен быть от 2 до 10000.");
        if (!TryRead(PParameterBox.Text, out decimal p) || p is < -100 or > 100)
            throw new InvalidOperationException("Параметр p должен быть от −100 до 100.");
        if (!TryRead(QRealParameterBox.Text, out decimal qReal) || qReal is < -100 or > 100)
            throw new InvalidOperationException("Re(q) должна быть от −100 до 100.");
        if (!TryRead(QImaginaryParameterBox.Text, out decimal qImaginary) || qImaginary is < -100 or > 100)
            throw new InvalidOperationException("Im(q) должна быть от −100 до 100.");
        double argumentCycles = ReadDouble(ArgumentCyclesBox.Text, "обороты аргумента", 0.1, 20);
        double magnitudeScale = ReadDouble(MagnitudeScaleBox.Text, "масштаб модуля", 0.01, 20);
        double trapScale = ReadDouble(TrapScaleBox.Text, "чувствительность ловушки", 0.01, 100);
        double cycleTolerance = ReadDouble(CycleToleranceBox.Text, "допуск цикла", 1e-12, 0.1);
        int maximumPeriod = ReadInt(MaximumPeriodBox.Text, "максимальный период", 1, 64);
        double densityExposure = ReadDouble(DensityExposureBox.Text, "экспозиция плотности", 0.1, 10);
        int densitySampleStep = ReadInt(DensitySampleStepBox.Text, "шаг начальных точек", 1, 8);
        return new CollatzState
        {
            SaveName = name,
            Timestamp = DateTime.Now,
            CenterX = _centerX,
            CenterY = _centerY,
            Zoom = _zoom,
            Threshold = threshold,
            Iterations = iterations,
            Variation = (CollatzVariation)Math.Clamp(VariationBox.SelectedIndex, 0,
                (int)CollatzVariation.GeneralizedPQ),
            PParameter = p,
            QRealParameter = qReal,
            QImaginaryParameter = qImaginary,
            ColoringMode = (CollatzColoringMode)Math.Clamp(ColoringModeBox.SelectedIndex, 0,
                (int)CollatzColoringMode.PeriodDetection),
            UseSmoothColoring = EscapeSmoothingBox.SelectedIndex == 1,
            ArgumentCycles = argumentCycles,
            MagnitudeScale = magnitudeScale,
            TrapScale = trapScale,
            CycleTolerance = cycleTolerance,
            MaximumDetectedPeriod = maximumPeriod,
            OrbitDensityExposure = densityExposure,
            OrbitDensitySampleStep = densitySampleStep,
            OrbitDensityEscapedOnly = DensityEscapedOnlyBox.IsChecked == true,
            Palette = _paletteManager.ActivePalette.Clone(_paletteManager.ActivePalette.Name)
        };
    }

    public void LoadState(CollatzState state)
    {
        _renderCts?.Cancel();
        _updatingControls = true;
        _centerX = state.CenterX;
        _centerY = state.CenterY;
        _zoom = Math.Max(0.000000000000001m, state.Zoom);
        IterationsBox.Text = state.Iterations.ToString(CultureInfo.InvariantCulture);
        ThresholdBox.Text = Format(state.Threshold);
        ZoomBox.Text = Format(_zoom);
        PParameterBox.Text = Format(state.PParameter);
        QRealParameterBox.Text = Format(state.QRealParameter);
        QImaginaryParameterBox.Text = Format(state.QImaginaryParameter);
        VariationBox.SelectedIndex = Math.Clamp((int)state.Variation, 0,
            (int)CollatzVariation.GeneralizedPQ);
        ColoringModeBox.SelectedIndex = Math.Clamp((int)state.ColoringMode, 0,
            (int)CollatzColoringMode.PeriodDetection);
        EscapeSmoothingBox.SelectedIndex = state.UseSmoothColoring ? 1 : 0;
        ArgumentCyclesBox.Text = Format(state.ArgumentCycles);
        MagnitudeScaleBox.Text = Format(state.MagnitudeScale);
        TrapScaleBox.Text = Format(state.TrapScale);
        CycleToleranceBox.Text = state.CycleTolerance.ToString("G8", CultureInfo.InvariantCulture);
        MaximumPeriodBox.Text = state.MaximumDetectedPeriod.ToString(CultureInfo.InvariantCulture);
        DensityExposureBox.Text = Format(state.OrbitDensityExposure);
        DensitySampleStepBox.Text = state.OrbitDensitySampleStep.ToString(CultureInfo.InvariantCulture);
        DensityEscapedOnlyBox.IsChecked = state.OrbitDensityEscapedOnly;
        _paletteManager.ActivePalette = state.Palette.Clone($"Загружено: {state.SaveName}");
        UpdateVariationControls();
        UpdateColoringControls();
        _updatingControls = false;
        UpdatePreviewTransform();
        ScheduleRender();
    }

    public Task<BitmapSource> RenderStatePreviewAsync(CollatzState state, int width, int height, CancellationToken token) =>
        RenderBitmapAsync(state, width, height, 1, token, null);

    private void VariationBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateVariationControls();
        if (!_updatingControls) ScheduleRender();
    }

    private void UpdateVariationControls()
    {
        CollatzVariation variation = (CollatzVariation)Math.Clamp(VariationBox.SelectedIndex, 0,
            (int)CollatzVariation.GeneralizedPQ);
        if (PParameterPanel is not null)
            PParameterPanel.Visibility = variation is CollatzVariation.ParityBranchVariation or
                CollatzVariation.GeneralizedP or CollatzVariation.GeneralizedPQ
                ? Visibility.Visible : Visibility.Collapsed;
        if (QParameterPanel is not null)
            QParameterPanel.Visibility = variation == CollatzVariation.GeneralizedPQ
                ? Visibility.Visible : Visibility.Collapsed;
        if (VariationHintText is not null)
            VariationHintText.Text = VariationDescription(variation);
    }

    private void ColoringMode_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateColoringControls();
        if (!_updatingControls) ScheduleRender();
    }

    private void UpdateColoringControls()
    {
        CollatzColoringMode mode = (CollatzColoringMode)Math.Clamp(ColoringModeBox.SelectedIndex, 0,
            (int)CollatzColoringMode.PeriodDetection);
        if (EscapeColoringPanel is not null)
            EscapeColoringPanel.Visibility = mode == CollatzColoringMode.EscapeTime
                ? Visibility.Visible : Visibility.Collapsed;
        if (ArgumentColoringPanel is not null)
            ArgumentColoringPanel.Visibility = mode == CollatzColoringMode.FinalArgument
                ? Visibility.Visible : Visibility.Collapsed;
        if (MagnitudeColoringPanel is not null)
            MagnitudeColoringPanel.Visibility = mode == CollatzColoringMode.FinalMagnitude
                ? Visibility.Visible : Visibility.Collapsed;
        if (TrapColoringPanel is not null)
            TrapColoringPanel.Visibility = mode is CollatzColoringMode.IntegerTrap or
                CollatzColoringMode.RealAxisTrap ? Visibility.Visible : Visibility.Collapsed;
        if (CycleColoringPanel is not null)
            CycleColoringPanel.Visibility = mode is CollatzColoringMode.CycleBasins or
                CollatzColoringMode.PeriodDetection ? Visibility.Visible : Visibility.Collapsed;
        if (DensityColoringPanel is not null)
            DensityColoringPanel.Visibility = mode == CollatzColoringMode.OrbitDensity
                ? Visibility.Visible : Visibility.Collapsed;
        if (ColoringDescriptionText is not null)
            ColoringDescriptionText.Text = ColoringModeDescription(mode);
    }

    private void Parameter_OnChanged(object sender, EventArgs e)
    {
        if (!_updatingControls) ScheduleRender();
    }

    private void ZoomBox_OnChanged(object sender, TextChangedEventArgs e)
    {
        if (!TryRead(ZoomBox.Text, out decimal zoom)) return;
        _zoom = Math.Clamp(zoom, 0.000000000000001m, 1_000_000_000_000_000m);
        UpdatePreviewTransform();
        if (!_updatingControls) ScheduleRender();
    }

    private void RenderButton_OnClick(object sender, RoutedEventArgs e) => _ = RenderPreviewAsync();
    private void CancelButton_OnClick(object sender, RoutedEventArgs e) => _renderCts?.Cancel();

    private void PaletteButton_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new MandelbrotPaletteWindow(_paletteManager) { Owner = this };
        dialog.PaletteApplied += (_, _) => ScheduleRender();
        dialog.ShowDialog();
    }

    private void SavesButton_OnClick(object sender, RoutedEventArgs e) =>
        SaveManagerWindow.Open(this, SaveManagerConfigurations.ForCollatz(this, _saveStore));

    private void ExportButton_OnClick(object sender, RoutedEventArgs e)
    {
        RenderSurfaceMetrics surface = RenderSurfaceMetrics.Measure(CanvasHost);
        _renderCts?.Cancel();
        CollatzState state;
        try { state = CaptureState("export"); }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Параметры экспорта", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        ImageExportManagerWindow.Open(this, new ImageExportConfiguration
        {
            FileNamePrefix = "collatz",
            InitialWidth = surface.PixelWidth,
            InitialHeight = surface.PixelHeight,
            MaxSsaaFactor = 4,
            RenderAsync = (request, token, progress) => RenderBitmapAsync(state, request.Width,
                request.Height, request.SsaaFactor, token, progress)
        });
    }

    private void ScheduleRender()
    {
        if (!IsLoaded) return;
        _renderCts?.Cancel();
        _renderTimer.Stop();
        _renderTimer.Start();
    }

    private void RenderTimer_OnTick(object? sender, EventArgs e)
    {
        _renderTimer.Stop();
        _ = RenderPreviewAsync();
    }

    private async Task RenderPreviewAsync()
    {
        if (_isRendering) { ScheduleRender(); return; }
        CollatzState state;
        try { state = CaptureState("preview"); }
        catch (Exception ex) { StatusText.Text = ex.Message; return; }

        _renderCts?.Dispose();
        _renderCts = new CancellationTokenSource();
        CancellationToken token = _renderCts.Token;
        var watch = Stopwatch.StartNew();
        SetRendering(true, $"Рендеринг: {VariationDisplayName(state.Variation)} · " +
                           $"{ColoringModeDisplayName(state.ColoringMode)}...");
        try
        {
            int factor = SsaaBox.SelectedItem is ComboBoxItem item ? Convert.ToInt32(item.Tag, CultureInfo.InvariantCulture) : 1;
            RenderSurfaceMetrics surface = RenderSurfaceMetrics.Measure(CanvasHost);
            DpiScale dpi = surface.Dpi;
            int pixelWidth = surface.PixelWidth;
            int pixelHeight = surface.PixelHeight;
            int renderWidth = checked(pixelWidth * factor);
            int renderHeight = checked(pixelHeight * factor);
            BitmapSource completed;
            string renderDetails;
            if (state.ColoringMode == CollatzColoringMode.OrbitDensity)
            {
                CanvasImage.Source = null;
                completed = await RenderBitmapAsync(state, pixelWidth, pixelHeight, factor, token,
                    new Progress<int>(value => RenderProgress.Value = value));
                renderDetails = "глобальное накопление орбит";
            }
            else
            {
                TileSchedulingStrategy strategy = RenderPatternSettings.SelectedPattern;
                IReadOnlyList<MandelbrotRenderTile> tiles = MandelbrotTileScheduler.Create(
                    renderWidth, renderHeight, 16 * factor, strategy);
                WriteableBitmap bitmap = ProgressiveRenderBitmap.CreateOverlay(
                    renderWidth, renderHeight, dpi.PixelsPerInchX, dpi.PixelsPerInchY);
                var session = new RenderSession(bitmap, tiles.Count, renderWidth, renderHeight);
                _activeSession = session;
                CanvasImage.Source = bitmap;
                RenderOverlay.BeginSession(renderWidth, renderHeight);
                _visualizationTimer.Start();
                await RenderTilesAsync(state, tiles, session, GetThreadCount(), token);
                FlushVisualizationEvents(session, true);
                completed = session.Bitmap.Clone();
                completed.Freeze();
                renderDetails = $"стратегия: {strategy}";
            }

            if (token.IsCancellationRequested)
            {
                CanvasImage.Source = null;
                StatusText.Text = "Рендер отменён";
                return;
            }
            StablePreviewImage.Source = completed;
            CanvasImage.Source = null;
            _renderedCenterX = state.CenterX;
            _renderedCenterY = state.CenterY;
            _renderedZoom = state.Zoom;
            _hasRenderedFrame = true;
            UpdatePreviewTransform();
            StatusText.Text = $"Готово за {watch.Elapsed.TotalSeconds:F3} сек.; {renderDetails}.";
        }
        catch (OperationCanceledException) { CanvasImage.Source = null; StatusText.Text = "Рендер отменён"; }
        catch (Exception ex) { StatusText.Text = "Ошибка рендера"; MessageBox.Show(this, ex.Message, "Коллатц", MessageBoxButton.OK, MessageBoxImage.Error); }
        finally
        {
            _visualizationTimer.Stop();
            RenderOverlay.EndSession();
            _activeSession = null;
            SetRendering(false);
        }
    }

    private static async Task RenderTilesAsync(CollatzState state, IReadOnlyList<MandelbrotRenderTile> tiles,
        RenderSession session, int threadCount, CancellationToken token)
    {
        var queue = new ConcurrentQueue<MandelbrotRenderTile>(tiles);
        Task[] workers = Enumerable.Range(0, Math.Clamp(threadCount, 1, Environment.ProcessorCount)).Select(_ => Task.Run(() =>
        {
            while (queue.TryDequeue(out MandelbrotRenderTile tile))
            {
                if (token.IsCancellationRequested) return;
                session.Events.Enqueue(new TileRenderEvent(true, tile, null));
                byte[]? pixels = CollatzRenderer.RenderTile(state, session.RenderWidth, session.RenderHeight, tile, token);
                if (pixels is null || token.IsCancellationRequested) return;
                session.Events.Enqueue(new TileRenderEvent(false, tile, pixels));
            }
        })).ToArray();
        await Task.WhenAll(workers);
    }

    private void FlushVisualizationEvents(RenderSession session, bool drainAll)
    {
        int processed = 0;
        bool changed = false;
        while ((drainAll || processed < 512) && session.Events.TryDequeue(out TileRenderEvent entry))
        {
            if (entry.IsStart) RenderOverlay.StartTile(entry.Tile);
            else if (entry.Pixels is not null)
            {
                if (ProgressiveRenderBitmap.WriteTile(session.Bitmap, entry.Tile, entry.Pixels))
                {
                    RenderOverlay.CompleteTile(entry.Tile);
                    session.CompletedTiles++;
                }
            }
            processed++;
            changed = true;
        }
        if (!changed) return;
        RenderOverlay.Refresh();
        RenderProgress.Value = session.TileCount == 0 ? 0 : session.CompletedTiles * 100.0 / session.TileCount;
    }

    private async Task<BitmapSource> RenderBitmapAsync(CollatzState state, int width, int height, int ssaa,
        CancellationToken token, IProgress<int>? progress)
    {
        int factor = Math.Clamp(ssaa, 1, 4);
        int renderWidth = checked(width * factor);
        int renderHeight = checked(height * factor);
        int stride = checked(renderWidth * 4);
        byte[] pixels = new byte[checked(stride * renderHeight)];
        int threads = GetThreadCount();
        await Task.Run(() => CollatzRenderer.Render(state, pixels, renderWidth, renderHeight, stride, threads, token,
            value => progress?.Report(factor == 1 ? value : value * 90 / 100)));
        BitmapSource source = BitmapSource.Create(renderWidth, renderHeight, 96, 96, PixelFormats.Bgra32, null, pixels, stride);
        source.Freeze();
        return factor == 1 || token.IsCancellationRequested ? source : await Task.Run(() => BitmapResampler.ResizeLanczos3(source, width, height, token,
            value => progress?.Report(value)));
    }

    private int GetThreadCount() => ThreadsBox.SelectedItem?.ToString() == "Auto"
        ? Environment.ProcessorCount
        : Math.Max(1, Convert.ToInt32(ThreadsBox.SelectedItem, CultureInfo.InvariantCulture));

    private void SetRendering(bool value, string? status = null)
    {
        _isRendering = value;
        CancelButton.IsEnabled = value;
        if (!value) RenderProgress.Value = 0;
        if (status is not null) StatusText.Text = status;
    }

    private void CanvasHost_OnSizeChanged(object sender, SizeChangedEventArgs e) { UpdatePreviewTransform(); ScheduleRender(); }

    private void CanvasHost_OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        Point mouse = e.GetPosition(CanvasHost);
        (decimal X, decimal Y) before = ScreenToWorld(mouse);
        _zoom = Math.Clamp(_zoom * (e.Delta > 0 ? 1.2m : 1m / 1.2m), 0.000000000000001m, 1_000_000_000_000_000m);
        (decimal X, decimal Y) after = ScreenToWorld(mouse);
        _centerX += before.X - after.X;
        _centerY += before.Y - after.Y;
        UpdatePreviewTransform();
        ZoomBox.Text = Format(_zoom);
        ScheduleRender();
    }

    private void CanvasHost_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _panning = true;
        _lastPanPoint = e.GetPosition(CanvasHost);
        CanvasHost.CaptureMouse();
        Mouse.OverrideCursor = Cursors.SizeAll;
    }

    private void CanvasHost_OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_panning) return;
        Point current = e.GetPosition(CanvasHost);
        (decimal X, decimal Y) before = ScreenToWorld(_lastPanPoint);
        (decimal X, decimal Y) after = ScreenToWorld(current);
        _centerX += before.X - after.X;
        _centerY += before.Y - after.Y;
        _lastPanPoint = current;
        UpdatePreviewTransform();
    }

    private void CanvasHost_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_panning) return;
        _panning = false;
        CanvasHost.ReleaseMouseCapture();
        Mouse.OverrideCursor = null;
        ScheduleRender();
    }

    private (decimal X, decimal Y) ScreenToWorld(Point point)
    {
        decimal width = (decimal)Math.Max(1, CanvasHost.ActualWidth);
        decimal scale = BaseScale / _zoom;
        return (_centerX + ((decimal)point.X - width / 2) * scale / width,
            _centerY + ((decimal)Math.Max(1, CanvasHost.ActualHeight) / 2 - (decimal)point.Y) * scale / width);
    }

    private void UpdatePreviewTransform()
    {
        if (!_hasRenderedFrame || _renderedZoom <= 0 || _zoom <= 0 || CanvasHost.ActualWidth <= 0) return;
        double scale = (double)(_zoom / _renderedZoom);
        decimal currentScale = BaseScale / _zoom;
        double width = CanvasHost.ActualWidth;
        _previewScale.ScaleX = scale;
        _previewScale.ScaleY = scale;
        _previewTranslation.X = (double)((_renderedCenterX - _centerX) / currentScale) * width;
        _previewTranslation.Y = (double)((_centerY - _renderedCenterY) / currentScale) * width;
    }

    private void ToggleControlsButton_OnClick(object sender, RoutedEventArgs e)
    {
        FractalControlPanel.Toggle(ref _controlsVisible, ControlsColumn, ControlsHost,
            ToggleControlsButton, 290);
        ScheduleRender();
    }

    private void Window_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F11 || e.Key == Key.Escape && _isFullscreen) ToggleFullscreen();
    }

    private void ToggleFullscreen()
    {
        if (!_isFullscreen)
        {
            _previousWindowStyle = WindowStyle;
            _previousWindowState = WindowState;
            WindowStyle = WindowStyle.None;
            WindowState = WindowState.Maximized;
        }
        else
        {
            WindowStyle = _previousWindowStyle;
            WindowState = _previousWindowState;
        }
        _isFullscreen = !_isFullscreen;
    }

    private void Window_OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _renderTimer.Stop();
        _visualizationTimer.Stop();
        _renderCts?.Cancel();
        _renderCts?.Dispose();
    }

    private static string VariationDisplayName(CollatzVariation variation) => variation switch
    {
        CollatzVariation.SineVariation => "синусная арт-вариация",
        CollatzVariation.ParityBranchVariation => "арт-вариация с ветвями 1 / (p−1)n",
        CollatzVariation.GeneralizedP => "обобщённая Cₚ",
        CollatzVariation.GeneralizedPQ => "семейство C(p,q)",
        _ => "стандартная вариация"
    };

    private static string VariationDescription(CollatzVariation variation) => variation switch
    {
        CollatzVariation.SineVariation => "Художественная вариация; не сохраняет отображение Коллатца на целых.",
        CollatzVariation.ParityBranchVariation => "Прежний режим Generalized P: чётное n → 1, нечётное n → (p−1)n.",
        CollatzVariation.GeneralizedP => "Аналитическое продолжение: чётное n → n/2, нечётное n → pn+1.",
        CollatzVariation.GeneralizedPQ => "То же отображение на целых; комплексный q меняет только продолжение между ними.",
        _ => "Классическое продолжение Коллатца (p = 3)."
    };

    private static string ColoringModeDisplayName(CollatzColoringMode mode) => mode switch
    {
        CollatzColoringMode.FinalArgument => "Final Argument",
        CollatzColoringMode.FinalMagnitude => "Final Magnitude",
        CollatzColoringMode.CycleBasins => "Cycle Basins",
        CollatzColoringMode.IntegerTrap => "Integer Trap",
        CollatzColoringMode.RealAxisTrap => "Real Axis Trap",
        CollatzColoringMode.OrbitDensity => "Orbit Density",
        CollatzColoringMode.PeriodDetection => "Period Detection",
        _ => "Escape Time"
    };

    private static string ColoringModeDescription(CollatzColoringMode mode) => mode switch
    {
        CollatzColoringMode.FinalArgument => "Цвет определяется аргументом последнего значения z.",
        CollatzColoringMode.FinalMagnitude => "Цвет определяется логарифмом модуля последнего значения z.",
        CollatzColoringMode.CycleBasins => "Одинаковым найденным циклам назначается одинаковый цвет.",
        CollatzColoringMode.IntegerTrap => "Яркость показывает минимальное расстояние орбиты до вещественного целого.",
        CollatzColoringMode.RealAxisTrap => "Яркость показывает минимальное расстояние орбиты до вещественной оси.",
        CollatzColoringMode.OrbitDensity => "Накопление посещений пикселей всеми траекториями, как в Buddhabrot.",
        CollatzColoringMode.PeriodDetection => "Цвет определяется найденным периодом орбиты.",
        _ => "Текущий режим: цвет по числу итераций до выхода."
    };

    private static bool TryRead(string text, out decimal value) =>
        decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) ||
        decimal.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value);

    private static double ReadDouble(string text, string parameterName, double minimum, double maximum)
    {
        bool parsed = double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double value) ||
                      double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value);
        if (!parsed || !double.IsFinite(value) || value < minimum || value > maximum)
            throw new InvalidOperationException($"Параметр «{parameterName}» должен быть от {minimum:G8} до {maximum:G8}.");
        return value;
    }

    private static int ReadInt(string text, string parameterName, int minimum, int maximum)
    {
        if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ||
            value < minimum || value > maximum)
            throw new InvalidOperationException($"Параметр «{parameterName}» должен быть целым числом от {minimum} до {maximum}.");
        return value;
    }

    private static string Format(decimal value) => value.ToString("G15", CultureInfo.InvariantCulture);
    private static string Format(double value) => value.ToString("G15", CultureInfo.InvariantCulture);

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
