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
    private Point _lastPanPoint;
    private decimal _centerX, _centerY, _zoom = 1;
    private decimal _renderedCenterX, _renderedCenterY, _renderedZoom = 1;
    private bool _hasRenderedFrame;
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
        C1RealBox.Text = "0.566666666666667"; C1ImaginaryBox.Text = "-0.5"; C2RealBox.Text = "0"; C2ImaginaryBox.Text = "0";
        IterationsBox.Text = "100"; ThresholdBox.Text = "4"; ZoomBox.Text = "1";
        for (int count = 1; count <= Environment.ProcessorCount; count++) ThreadsBox.Items.Add(count);
        ThreadsBox.Items.Add("Auto"); ThreadsBox.SelectedItem = "Auto";
        SsaaBox.SelectedIndex = 0; ColoringBox.SelectedIndex = 1;
        Loaded += (_, _) => ScheduleRender();
    }

    public PhoenixState CaptureState(string name)
    {
        if (!TryRead(C1RealBox.Text, out decimal c1r) || !TryRead(C1ImaginaryBox.Text, out decimal c1i) ||
            !TryRead(C2RealBox.Text, out decimal c2r) || !TryRead(C2ImaginaryBox.Text, out decimal c2i) ||
            !TryRead(ThresholdBox.Text, out decimal threshold) || threshold is < 2 or > 1000 ||
            !int.TryParse(IterationsBox.Text, out int iterations) || iterations is < 10 or > 100_000)
            throw new InvalidOperationException("Проверьте C1/C2, итерации (10–100000) и порог выхода (2–1000).");
        return new PhoenixState
        {
            SaveName = name, Timestamp = DateTime.Now, CenterX = _centerX, CenterY = _centerY, Zoom = _zoom,
            Threshold = threshold, Iterations = iterations, C1Real = c1r, C1Imaginary = c1i, C2Real = c2r, C2Imaginary = c2i,
            UseSmoothColoring = ColoringBox.SelectedIndex == 1, Palette = _paletteManager.ActivePalette.Clone(_paletteManager.ActivePalette.Name)
        };
    }

    public void LoadState(PhoenixState state)
    {
        _renderCts?.Cancel(); _centerX = state.CenterX; _centerY = state.CenterY; _zoom = Math.Max(0.000001m, state.Zoom);
        C1RealBox.Text = Format(state.C1Real); C1ImaginaryBox.Text = Format(state.C1Imaginary); C2RealBox.Text = Format(state.C2Real); C2ImaginaryBox.Text = Format(state.C2Imaginary);
        IterationsBox.Text = state.Iterations.ToString(CultureInfo.InvariantCulture); ThresholdBox.Text = Format(state.Threshold); ZoomBox.Text = Format(_zoom);
        ColoringBox.SelectedIndex = state.UseSmoothColoring ? 1 : 0; _paletteManager.ActivePalette = state.Palette.Clone($"Загружено: {state.SaveName}");
        UpdatePreviewTransform(); ScheduleRender();
    }

    public Task<BitmapSource> RenderStatePreviewAsync(PhoenixState state, int width, int height, CancellationToken token) => RenderBitmapAsync(state, width, height, 1, token, null);

    private void ParameterSelector_OnClick(object sender, RoutedEventArgs e)
    {
        if (!TryRead(C1RealBox.Text, out decimal c1r) || !TryRead(C1ImaginaryBox.Text, out decimal c1i) || !TryRead(C2RealBox.Text, out decimal c2r) || !TryRead(C2ImaginaryBox.Text, out decimal c2i))
        { MessageBox.Show(this, "Сначала введите корректные C1 и C2.", "Параметры Phoenix", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        var dialog = new PhoenixParameterSelectorWindow(c1r, c1i, c2r, c2i) { Owner = this };
        if (dialog.ShowDialog() != true) return;
        C1RealBox.Text = Format(dialog.SelectedC1Real); C1ImaginaryBox.Text = Format(dialog.SelectedC1Imaginary);
        C2RealBox.Text = Format(dialog.FixedC2Real); C2ImaginaryBox.Text = Format(dialog.FixedC2Imaginary); ScheduleRender();
    }

    private void Parameter_OnChanged(object sender, EventArgs e) => ScheduleRender();
    private void ZoomBox_OnChanged(object sender, TextChangedEventArgs e)
    {
        if (TryRead(ZoomBox.Text, out decimal zoom))
        {
            _zoom = Math.Clamp(zoom, 0.000001m, decimal.MaxValue);
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

    private void ScheduleRender() { if (!IsLoaded) return; _renderCts?.Cancel(); _renderTimer.Stop(); _renderTimer.Start(); }
    private void RenderTimer_OnTick(object? sender, EventArgs e) { _renderTimer.Stop(); _ = RenderPreviewAsync(); }

    private async Task RenderPreviewAsync()
    {
        if (_isRendering) { ScheduleRender(); return; }
        PhoenixState state; try { state = CaptureState("preview"); } catch (Exception ex) { StatusText.Text = ex.Message; return; }
        _renderCts?.Dispose(); _renderCts = new CancellationTokenSource(); CancellationToken token = _renderCts.Token;
        var watch = Stopwatch.StartNew(); SetRendering(true, "Рендеринг Феникса...");
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
            _renderedCenterX = state.CenterX;
            _renderedCenterY = state.CenterY;
            _renderedZoom = state.Zoom;
            _hasRenderedFrame = true;
            UpdatePreviewTransform();
            RenderOverlay.EndSession();
            _activeSession = null;
            StatusText.Text = $"Готово за {watch.Elapsed.TotalSeconds:F3} сек. Стратегия: {strategy}.";
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
        Point mouse = e.GetPosition(CanvasHost); (decimal X, decimal Y) before = ScreenToWorld(mouse);
        _zoom = Math.Clamp(_zoom * (e.Delta > 0 ? 1.2m : 1m / 1.2m), 0.000001m, decimal.MaxValue);
        (decimal X, decimal Y) after = ScreenToWorld(mouse); _centerX += before.X - after.X; _centerY += before.Y - after.Y;
        UpdatePreviewTransform(); ZoomBox.Text = Format(_zoom); ScheduleRender();
    }
    private void CanvasHost_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e) { _panning = true; _lastPanPoint = e.GetPosition(CanvasHost); CanvasHost.CaptureMouse(); Mouse.OverrideCursor = Cursors.SizeAll; }
    private void CanvasHost_OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_panning) return; Point current = e.GetPosition(CanvasHost); (decimal X, decimal Y) before = ScreenToWorld(_lastPanPoint); (decimal X, decimal Y) after = ScreenToWorld(current);
        _centerX += before.X - after.X; _centerY += before.Y - after.Y; _lastPanPoint = current; UpdatePreviewTransform();
    }
    private void CanvasHost_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e) { if (!_panning) return; _panning = false; CanvasHost.ReleaseMouseCapture(); Mouse.OverrideCursor = null; ScheduleRender(); }
    private (decimal X, decimal Y) ScreenToWorld(Point point)
    {
        decimal width = (decimal)Math.Max(1, CanvasHost.ActualWidth); decimal scale = BaseScale / _zoom;
        return (_centerX + ((decimal)point.X - width / 2) * scale / width, _centerY + ((decimal)Math.Max(1, CanvasHost.ActualHeight) / 2 - (decimal)point.Y) * scale / width);
    }
    private void UpdatePreviewTransform()
    {
        if (!_hasRenderedFrame || _renderedZoom <= 0 || _zoom <= 0 || CanvasHost.ActualWidth <= 0) return;
        double scale = (double)(_zoom / _renderedZoom);
        decimal currentScale = BaseScale / _zoom;
        double width = CanvasHost.ActualWidth;
        double dx = (double)((_renderedCenterX - _centerX) / currentScale) * width;
        double dy = (double)((_centerY - _renderedCenterY) / currentScale) * width;
        _previewScale.ScaleX = scale;
        _previewScale.ScaleY = scale;
        _previewTranslation.X = dx;
        _previewTranslation.Y = dy;
    }
    private void ToggleControlsButton_OnClick(object sender, RoutedEventArgs e) => FractalControlPanel.Toggle(ref _controlsVisible, ControlsColumn, ControlsHost, ToggleControlsButton, 280, ScheduleRender);
    private void Window_OnKeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.F11 || e.Key == Key.Escape && _isFullscreen) ToggleFullscreen(); }
    private void ToggleFullscreen() { if (!_isFullscreen) { _previousWindowStyle = WindowStyle; _previousWindowState = WindowState; WindowStyle = WindowStyle.None; WindowState = WindowState.Maximized; } else { WindowStyle = _previousWindowStyle; WindowState = _previousWindowState; } _isFullscreen = !_isFullscreen; }
    private void Window_OnClosing(object? sender, System.ComponentModel.CancelEventArgs e) { _renderTimer.Stop(); _visualizationTimer.Stop(); _renderCts?.Cancel(); _renderCts?.Dispose(); }
    private static bool TryRead(string text, out decimal value) => decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) || decimal.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value);
    private static string Format(decimal value) => value.ToString("G15", CultureInfo.InvariantCulture);

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
