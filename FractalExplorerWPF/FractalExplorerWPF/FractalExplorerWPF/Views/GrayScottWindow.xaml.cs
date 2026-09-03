using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using FractalExplorerWPF.Controls;
using FractalExplorerWPF.Core.Rendering;
using FractalExplorerWPF.Infrastructure;
using FractalExplorerWPF.Models;
using Color = System.Windows.Media.Color;
using MediaBrushes = System.Windows.Media.Brushes;
using Point = System.Windows.Point;

namespace FractalExplorerWPF.Views;

public partial class GrayScottWindow : Window
{
    private readonly DispatcherTimer _frameTimer = new();
    private readonly GrayScottPaletteManager _paletteManager = new();
    private readonly GrayScottSaveStore _saveStore = new();
    private readonly ConcurrentQueue<(double X, double Y)> _injections = new();
    private readonly Stopwatch _fpsWatch = Stopwatch.StartNew();
    private readonly Stopwatch _scheduleWatch = Stopwatch.StartNew();
    private GrayScottSimulation? _simulation;
    private GrayScottState? _activeState;
    private WriteableBitmap? _bitmap;
    private CancellationTokenSource? _simulationCts;
    private Task _frameIdleTask = Task.CompletedTask;
    private int _presentedFrames;
    private double _measuredFps;
    private double _nextFrameAtMilliseconds;
    private bool _frameBusy;
    private bool _running = true;
    private bool _syncing;
    private bool _painting;
    private bool _controlsVisible = true;
    private bool _fullScreen;
    private WindowStyle _previousWindowStyle;
    private WindowState _previousWindowState;

    public GrayScottWindow()
    {
        InitializeComponent();
        PresetBox.ItemsSource = GrayScottPresets.All;
        _frameTimer.Tick += FrameTimer_OnTick;
        ApplyState(GrayScottPresets.All[0].State.Clone());
        PresetBox.SelectedItem = GrayScottPresets.All[0];
        Loaded += async (_, _) => await ResetSimulationAsync(startAfterReset: true);
    }

    public GrayScottState CaptureState(string name)
    {
        if (TryCaptureState(name, out GrayScottState state, out string error)) return state;
        throw new InvalidOperationException(error);
    }

    public void LoadState(GrayScottState state)
    {
        ApplyState(state.Clone());
        _ = ResetSimulationAsync(startAfterReset: true);
    }

    public BitmapSource? CaptureCurrentPreview(int width, int height) =>
        SavePreviewCapture.Capture(SavePreviewLayer, CanvasHost.Background, width, height, FrameImage);

    public Task<BitmapSource> RenderStatePreviewAsync(
        GrayScottState state, int width, int height, CancellationToken token, IProgress<int>? progress = null) =>
        GrayScottRenderer.RenderPreviewAsync(state.Clone(), width, height, token, progress);

    private bool TryCaptureState(string name, out GrayScottState state, out string error)
    {
        state = new GrayScottState();
        if (!ReadFiniteDouble(DiffusionUBox.Text, out double diffusionU) || diffusionU is <= 0 or > 1 ||
            !ReadFiniteDouble(DiffusionVBox.Text, out double diffusionV) || diffusionV is <= 0 or > 1)
        {
            error = "Коэффициенты диффузии должны быть больше 0 и не больше 1.";
            return false;
        }
        if (!ReadFiniteDouble(FeedBox.Text, out double feed) || feed is < 0 or > 0.2 ||
            !ReadFiniteDouble(KillBox.Text, out double kill) || kill is < 0 or > 0.2)
        {
            error = "Параметры F и K должны лежать в диапазоне 0–0.2.";
            return false;
        }
        if (!ReadFiniteDouble(DeltaTimeBox.Text, out double deltaTime) || deltaTime is < 0.05 or > 1.5)
        {
            error = "Шаг времени должен быть от 0.05 до 1.5.";
            return false;
        }
        if (!int.TryParse(GridSizeBox.Text, out int gridSize) || gridSize is < 96 or > 768)
        {
            error = "Размер сетки должен быть от 96 до 768.";
            return false;
        }
        if (!int.TryParse(StepsPerFrameBox.Text, out int stepsPerFrame) || stepsPerFrame is < 1 or > 64)
        {
            error = "Число шагов на кадр должно быть от 1 до 64.";
            return false;
        }
        if (!int.TryParse(RandomSeedBox.Text, out int randomSeed) ||
            !int.TryParse(SeedCountBox.Text, out int seedCount) || seedCount is < 1 or > 500 ||
            !int.TryParse(SeedRadiusBox.Text, out int seedRadius) || seedRadius is < 1 or > 128 ||
            !int.TryParse(BrushRadiusBox.Text, out int brushRadius) || brushRadius is < 1 or > 128)
        {
            error = "Проверьте seed, число и радиусы затравок (радиусы 1–128).";
            return false;
        }
        if (!ReadFiniteDouble(RangeMinimumBox.Text, out double rangeMinimum) ||
            !ReadFiniteDouble(RangeMaximumBox.Text, out double rangeMaximum) || rangeMaximum <= rangeMinimum)
        {
            error = "Максимум цветового диапазона должен быть больше минимума.";
            return false;
        }

        int targetFps = TargetFpsBox.SelectedIndex == 1 ? 60 : 30;
        state = new GrayScottState
        {
            SaveName = name,
            Timestamp = DateTime.Now,
            PresetId = (PresetBox.SelectedItem as GrayScottPreset)?.Id,
            DiffusionU = diffusionU,
            DiffusionV = diffusionV,
            Feed = feed,
            Kill = kill,
            DeltaTime = deltaTime,
            GridSize = gridSize,
            StepsPerFrame = stepsPerFrame,
            TargetFps = targetFps,
            RandomSeed = randomSeed,
            SeedMode = (GrayScottSeedMode)Math.Clamp(SeedModeBox.SelectedIndex, 0, 3),
            SeedCount = seedCount,
            SeedRadius = seedRadius,
            BrushRadius = brushRadius,
            FieldMode = (GrayScottFieldMode)Math.Clamp(FieldModeBox.SelectedIndex, 0, 2),
            RangeMinimum = rangeMinimum,
            RangeMaximum = rangeMaximum,
            ReversePalette = ReversePaletteBox.IsChecked == true,
            Palette = _paletteManager.ActivePalette.Clone()
        };
        error = string.Empty;
        return true;
    }

    private void ApplyState(GrayScottState state)
    {
        _syncing = true;
        try
        {
            DiffusionUBox.Text = Format(state.DiffusionU);
            DiffusionVBox.Text = Format(state.DiffusionV);
            FeedBox.Text = Format(state.Feed);
            KillBox.Text = Format(state.Kill);
            DeltaTimeBox.Text = Format(state.DeltaTime);
            GridSizeBox.Text = state.GridSize.ToString(CultureInfo.InvariantCulture);
            StepsPerFrameBox.Text = state.StepsPerFrame.ToString(CultureInfo.InvariantCulture);
            TargetFpsBox.SelectedIndex = state.TargetFps >= 60 ? 1 : 0;
            RandomSeedBox.Text = state.RandomSeed.ToString(CultureInfo.InvariantCulture);
            SeedModeBox.SelectedIndex = (int)state.SeedMode;
            SeedCountBox.Text = state.SeedCount.ToString(CultureInfo.InvariantCulture);
            SeedRadiusBox.Text = state.SeedRadius.ToString(CultureInfo.InvariantCulture);
            BrushRadiusBox.Text = state.BrushRadius.ToString(CultureInfo.InvariantCulture);
            FieldModeBox.SelectedIndex = (int)state.FieldMode;
            RangeMinimumBox.Text = Format(state.RangeMinimum);
            RangeMaximumBox.Text = Format(state.RangeMaximum);
            ReversePaletteBox.IsChecked = state.ReversePalette;
            GrayScottPalette? palette = _paletteManager.Palettes.FirstOrDefault(item =>
                item.Name.Equals(state.Palette.Name, StringComparison.OrdinalIgnoreCase));
            if (palette is null)
            {
                palette = state.Palette.Clone();
                _paletteManager.Palettes.Add(palette);
            }
            _paletteManager.ActivePalette = palette;
            PresetBox.SelectedItem = GrayScottPresets.All.FirstOrDefault(item => item.Id == state.PresetId);
        }
        finally
        {
            _syncing = false;
        }
        SetTimerInterval(state.TargetFps);
        UpdatePalettePreview();
        PendingText.Visibility = Visibility.Collapsed;
    }

    private async void Preset_OnChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncing || PresetBox.SelectedItem is not GrayScottPreset preset) return;
        try
        {
            ApplyState(preset.State.Clone());
            PresetBox.SelectedItem = preset;
            await ResetSimulationAsync(startAfterReset: true);
        }
        catch (Exception exception)
        {
            StatusText.Text = $"Не удалось применить пресет: {exception.Message}";
        }
    }

    private void Parameter_OnChanged(object sender, EventArgs e)
    {
        if (!_syncing) PendingText.Visibility = Visibility.Visible;
    }

    private void TargetFps_OnChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncing) return;
        SetTimerInterval(TargetFpsBox.SelectedIndex == 1 ? 60 : 30);
        PendingText.Visibility = Visibility.Visible;
    }

    private async void PaletteMapping_OnChanged(object sender, RoutedEventArgs e)
    {
        if (_syncing) return;
        await RenderCurrentFieldAsync();
    }

    private async void Reset_OnClick(object sender, RoutedEventArgs e) =>
        await ResetSimulationAsync(startAfterReset: _running);

    private async Task ResetSimulationAsync(bool startAfterReset)
    {
        _frameTimer.Stop();
        _running = false;
        UpdateRunState();
        try
        {
            CancellationTokenSource? previousCts = _simulationCts;
            await _frameIdleTask;
            previousCts?.Cancel();
            previousCts?.Dispose();

            if (!TryCaptureState("preview", out GrayScottState state, out string error))
            {
                StatusText.Text = error;
                return;
            }

            _activeState = state;
            _simulation = new GrayScottSimulation(state);
            _simulationCts = new CancellationTokenSource();
            _bitmap = new WriteableBitmap(state.GridSize, state.GridSize, 96, 96, PixelFormats.Bgra32, null);
            FrameImage.Source = _bitmap;
            while (_injections.TryDequeue(out _)) { }
            await RenderCurrentFieldAsync();
            PendingText.Visibility = Visibility.Collapsed;
            StatusText.Text = $"Сетка {state.GridSize}×{state.GridSize} · F={state.Feed:G5} · K={state.Kill:G5}";
            SetRunning(startAfterReset);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            SetRunning(false);
            StatusText.Text = $"Не удалось пересоздать симуляцию: {exception.Message}";
        }
    }

    private void RunPause_OnClick(object sender, RoutedEventArgs e) => SetRunning(!_running);

    private void SetRunning(bool running)
    {
        _running = running;
        if (running)
        {
            _nextFrameAtMilliseconds = _scheduleWatch.Elapsed.TotalMilliseconds;
            _frameTimer.Start();
        }
        else
        {
            _frameTimer.Stop();
        }
        UpdateRunState();
    }

    private void UpdateRunState()
    {
        RunButton.Content = _running ? "Пауза" : "Продолжить";
        StepButton.IsEnabled = !_running;
    }

    private async void Step_OnClick(object sender, RoutedEventArgs e)
    {
        if (_running) return;
        await ProduceFrameAsync();
    }

    private async void FrameTimer_OnTick(object? sender, EventArgs e)
    {
        if (!_running || _frameBusy) return;
        int targetFps = _activeState?.TargetFps ?? (TargetFpsBox.SelectedIndex == 1 ? 60 : 30);
        double now = _scheduleWatch.Elapsed.TotalMilliseconds;
        if (now < _nextFrameAtMilliseconds) return;
        double period = 1000d / Math.Clamp(targetFps, 1, 120);
        _nextFrameAtMilliseconds = Math.Max(_nextFrameAtMilliseconds + period, now);
        await ProduceFrameAsync();
    }

    private async Task ProduceFrameAsync(int? stepOverride = null)
    {
        if (_frameBusy || _simulation is null || _activeState is null || _simulationCts is null) return;
        _frameBusy = true;
        var frameIdle = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _frameIdleTask = frameIdle.Task;
        GrayScottSimulation simulation = _simulation;
        GrayScottState state = _activeState;
        state.Palette = _paletteManager.ActivePalette.Clone();
        state.ReversePalette = ReversePaletteBox.IsChecked == true;
        int steps = stepOverride ?? state.StepsPerFrame;
        CancellationToken token = _simulationCts.Token;
        var pendingInjections = new List<(double X, double Y)>();
        while (_injections.TryDequeue(out (double X, double Y) point)) pendingInjections.Add(point);

        try
        {
            Task<byte[]> workerTask = Task.Run(() =>
            {
                foreach ((double x, double y) in pendingInjections)
                    simulation.Inject(x, y, state.BrushRadius);
                if (steps > 0) simulation.Advance(steps, token);
                GrayScottSnapshot snapshot = simulation.CurrentView();
                return GrayScottRenderer.RenderFrame(snapshot, state, token);
            }, token);
            byte[] pixels = await workerTask;
            if (!ReferenceEquals(simulation, _simulation) || token.IsCancellationRequested) return;
            PresentFrame(pixels, simulation.StepCount);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            SetRunning(false);
            StatusText.Text = exception.Message;
        }
        finally
        {
            _frameBusy = false;
            frameIdle.TrySetResult(true);
        }
    }

    private async Task RenderCurrentFieldAsync()
    {
        if (_simulation is null || _activeState is null || _simulationCts is null) return;
        try
        {
            await _frameIdleTask;
            _activeState.Palette = _paletteManager.ActivePalette.Clone();
            _activeState.ReversePalette = ReversePaletteBox.IsChecked == true;
            await ProduceFrameAsync(0);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            SetRunning(false);
            StatusText.Text = $"Не удалось обновить изображение: {exception.Message}";
        }
    }

    private void PresentFrame(byte[] pixels, long stepCount)
    {
        if (_bitmap is null || _activeState is null) return;
        _bitmap.WritePixels(new Int32Rect(0, 0, _bitmap.PixelWidth, _bitmap.PixelHeight),
            pixels, _bitmap.PixelWidth * 4, 0);
        _presentedFrames++;
        if (_fpsWatch.Elapsed.TotalSeconds >= 0.75)
        {
            _measuredFps = _presentedFrames / _fpsWatch.Elapsed.TotalSeconds;
            _presentedFrames = 0;
            _fpsWatch.Restart();
        }
        double simulatedTime = stepCount * _activeState.DeltaTime;
        FrameBadgeText.Text = $"{_measuredFps:F1} FPS · шаг {stepCount:N0}";
        StatusText.Text = $"Время модели: {simulatedTime:N1} · шагов/кадр: {_activeState.StepsPerFrame} · " +
                          $"фактически {_measuredFps:F1} FPS";
    }

    private async void Randomize_OnClick(object sender, RoutedEventArgs e)
    {
        RandomSeedBox.Text = Random.Shared.Next().ToString(CultureInfo.InvariantCulture);
        await ResetSimulationAsync(startAfterReset: true);
    }

    private void Palette_OnClick(object sender, RoutedEventArgs e)
    {
        var window = new GrayScottPaletteWindow(_paletteManager) { Owner = this };
        window.PaletteApplied += async (_, _) =>
        {
            UpdatePalettePreview();
            await RenderCurrentFieldAsync();
        };
        window.ShowDialog();
    }

    private void UpdatePalettePreview()
    {
        List<Color> colors = _paletteManager.ActivePalette.Colors;
        if (colors.Count == 0)
        {
            PalettePreview.Background = MediaBrushes.Transparent;
            return;
        }
        if (colors.Count == 1)
        {
            PalettePreview.Background = new SolidColorBrush(colors[0]);
            return;
        }
        var brush = new LinearGradientBrush { StartPoint = new Point(0, 0.5), EndPoint = new Point(1, 0.5) };
        for (int index = 0; index < colors.Count; index++)
            brush.GradientStops.Add(new GradientStop(colors[index], index / (double)(colors.Count - 1)));
        PalettePreview.Background = brush;
    }

    private void Saves_OnClick(object sender, RoutedEventArgs e) =>
        SaveManagerWindow.Open(this, SaveManagerConfigurations.ForGrayScott(this, _saveStore));

    private async void Export_OnClick(object sender, RoutedEventArgs e)
    {
        bool resume = _running;
        SetRunning(false);
        await _frameIdleTask;
        if (_simulation is null)
        {
            MessageBox.Show(this, "Симуляция ещё не инициализирована.", "Gray–Scott",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            SetRunning(resume);
            return;
        }
        if (!TryCaptureState("export", out GrayScottState state, out string error))
        {
            MessageBox.Show(this, error, "Gray–Scott", MessageBoxButton.OK, MessageBoxImage.Warning);
            SetRunning(resume);
            return;
        }
        GrayScottSnapshot snapshot = _simulation.Snapshot();
        RenderSurfaceMetrics surface = RenderSurfaceMetrics.Measure(CanvasHost);
        ImageExportManagerWindow.Open(this, new ImageExportConfiguration
        {
            FileNamePrefix = "gray_scott",
            WindowTitle = "Экспорт текущего кадра Gray–Scott",
            InitialWidth = surface.PixelWidth,
            InitialHeight = surface.PixelHeight,
            HasNativeSsaa = false,
            MaxSsaaFactor = 4,
            RenderAsync = (request, token, progress) => GrayScottRenderer.RenderSnapshotAsync(
                snapshot, state, request.Width, request.Height, token, progress)
        });
        SetRunning(resume);
    }

    private void CanvasHost_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _painting = true;
        CanvasHost.CaptureMouse();
        QueueInjection(e.GetPosition(CanvasHost));
        e.Handled = true;
    }

    private void CanvasHost_OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_painting || e.LeftButton != MouseButtonState.Pressed) return;
        QueueInjection(e.GetPosition(CanvasHost));
    }

    private async void CanvasHost_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_painting) return;
        _painting = false;
        CanvasHost.ReleaseMouseCapture();
        if (!_running) await ProduceFrameAsync(0);
    }

    private void QueueInjection(Point point)
    {
        if (_simulation is null) return;
        double hostWidth = Math.Max(1, CanvasHost.ActualWidth);
        double hostHeight = Math.Max(1, CanvasHost.ActualHeight);
        double side = Math.Min(hostWidth, hostHeight);
        double left = (hostWidth - side) * 0.5;
        double top = (hostHeight - side) * 0.5;
        double x = (point.X - left) / side;
        double y = (point.Y - top) / side;
        if (x is < 0 or > 1 || y is < 0 or > 1) return;
        _injections.Enqueue((x, y));
    }

    private void SetTimerInterval(int targetFps)
    {
        _frameTimer.Interval = TimeSpan.FromMilliseconds(4);
        _nextFrameAtMilliseconds = _scheduleWatch.Elapsed.TotalMilliseconds;
    }

    private void Toggle_OnClick(object sender, RoutedEventArgs e) =>
        FractalControlPanel.Toggle(ref _controlsVisible, ControlsColumn, ControlsHost, ToggleButton, 330);

    private void Window_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space)
        {
            SetRunning(!_running);
            e.Handled = true;
        }
        else if (e.Key == Key.F11 || e.Key == Key.Escape && _fullScreen)
        {
            ToggleFullScreen();
        }
    }

    private void ToggleFullScreen()
    {
        if (!_fullScreen)
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
        _fullScreen = !_fullScreen;
    }

    private void Window_OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _frameTimer.Stop();
        _simulationCts?.Cancel();
        _simulationCts?.Dispose();
    }

    private static bool ReadFiniteDouble(string text, out double value)
    {
        bool parsed = double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) ||
                      double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value);
        return parsed && double.IsFinite(value);
    }

    private static string Format(double value) => value.ToString("G15", CultureInfo.InvariantCulture);
}
