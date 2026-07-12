using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using FractalExplorerWPF.Core.Rendering;
using FractalExplorerWPF.Infrastructure;
using FractalExplorerWPF.Models;
using Microsoft.Win32;
using Point = System.Windows.Point;

namespace FractalExplorerWPF.Views;

public partial class NovaWindow : Window
{
    private const decimal BaseScale = 4m;
    private readonly NovaVariant _variant;
    private readonly DispatcherTimer _renderTimer = new() { Interval = TimeSpan.FromMilliseconds(350) };
    private readonly DispatcherTimer _mapTimer = new() { Interval = TimeSpan.FromMilliseconds(350) };
    private readonly DispatcherTimer _visualizationTimer = new() { Interval = TimeSpan.FromMilliseconds(33) };
    private readonly MandelbrotPaletteManager _paletteManager = new();
    private readonly NovaSaveStore _saveStore;
    private readonly TransformGroup _previewTransform = new();
    private readonly ScaleTransform _previewScale = new(1, 1);
    private readonly TranslateTransform _previewTranslation = new();
    private CancellationTokenSource? _renderCts;
    private CancellationTokenSource? _mapCts;
    private RenderSession? _activeSession;
    private bool _isRendering, _panning, _isFullscreen, _controlsVisible = true, _hasRenderedFrame;
    private Point _lastPanPoint;
    private decimal _centerX, _centerY, _zoom = 1;
    private decimal _renderedCenterX, _renderedCenterY, _renderedZoom = 1;
    private WindowStyle _previousWindowStyle;
    private WindowState _previousWindowState;

    public NovaWindow(NovaVariant variant)
    {
        _variant = variant;
        _saveStore = new NovaSaveStore(variant);
        InitializeComponent();
        Title = variant == NovaVariant.Julia ? "Фрактал Nova Julia" : "Фрактал Nova Mandelbrot";
        HeaderText.Text = variant == NovaVariant.Julia ? "Параметры Nova Julia" : "Параметры Nova Mandelbrot";
        JuliaParametersPanel.Visibility = JuliaMapPanel.Visibility = variant == NovaVariant.Julia ? Visibility.Visible : Visibility.Collapsed;
        _previewTransform.Children.Add(_previewScale);
        _previewTransform.Children.Add(_previewTranslation);
        StablePreviewImage.RenderTransformOrigin = new Point(0.5, 0.5);
        StablePreviewImage.RenderTransform = _previewTransform;
        _renderTimer.Tick += (_, _) => { _renderTimer.Stop(); _ = RenderPreviewAsync(); };
        _mapTimer.Tick += (_, _) => { _mapTimer.Stop(); _ = RenderJuliaMapAsync(); };
        _visualizationTimer.Tick += (_, _) => { if (_activeSession is not null) FlushVisualizationEvents(_activeSession, false); };
        PRealBox.Text = "3"; PImaginaryBox.Text = "0"; Z0RealBox.Text = "1"; Z0ImaginaryBox.Text = "0";
        CRealBox.Text = "0"; CImaginaryBox.Text = "1"; MBox.Text = "1"; IterationsBox.Text = "100";
        ThresholdBox.Text = "10"; ZoomBox.Text = "1"; ColoringBox.SelectedIndex = 1; SsaaBox.SelectedIndex = 0;
        for (int count = 1; count <= Environment.ProcessorCount; count++) ThreadsBox.Items.Add(count);
        ThreadsBox.Items.Add("Auto"); ThreadsBox.SelectedItem = "Auto";
        Loaded += (_, _) => { ScheduleRender(); ScheduleMapRender(); };
    }

    public NovaState CaptureState(string name)
    {
        if (!TryRead(PRealBox.Text, out decimal pRe) || !TryRead(PImaginaryBox.Text, out decimal pIm) || pRe is < -10 or > 10 || pIm is < -10 or > 10)
            throw new InvalidOperationException("Компоненты степени P должны быть от −10 до 10.");
        if (!TryRead(Z0RealBox.Text, out decimal zRe) || !TryRead(Z0ImaginaryBox.Text, out decimal zIm) || zRe is < -10 or > 10 || zIm is < -10 or > 10)
            throw new InvalidOperationException("Компоненты Z₀ должны быть от −10 до 10.");
        if (!TryRead(CRealBox.Text, out decimal cRe) || !TryRead(CImaginaryBox.Text, out decimal cIm))
            throw new InvalidOperationException("Введите корректную константу C.");
        if (!TryRead(MBox.Text, out decimal m) || m is < 0.1m or > 5m) throw new InvalidOperationException("Релаксация m должна быть от 0,1 до 5.");
        if (!int.TryParse(IterationsBox.Text, out int iterations) || iterations is < 10 or > 100_000) throw new InvalidOperationException("Итерации должны быть от 10 до 100000.");
        if (!TryRead(ThresholdBox.Text, out decimal threshold) || threshold is < 2 or > 1000) throw new InvalidOperationException("Порог должен быть от 2 до 1000.");
        return new NovaState
        {
            SaveName = name, Timestamp = DateTime.Now, Variant = _variant,
            FractalType = _variant == NovaVariant.Julia ? "NovaJulia" : "NovaMandelbrot",
            CenterX = _centerX, CenterY = _centerY, Zoom = _zoom, Threshold = threshold, Iterations = iterations,
            PReal = pRe, PImaginary = pIm, Z0Real = zRe, Z0Imaginary = zIm, M = m, CReal = cRe, CImaginary = cIm,
            UseSmoothColoring = ColoringBox.SelectedIndex == 1,
            Palette = _paletteManager.ActivePalette.Clone(_paletteManager.ActivePalette.Name)
        };
    }

    public void LoadState(NovaState state)
    {
        _renderCts?.Cancel(); _centerX = state.CenterX; _centerY = state.CenterY; _zoom = Math.Max(0.000000000000001m, state.Zoom);
        PRealBox.Text = Format(state.PReal); PImaginaryBox.Text = Format(state.PImaginary); Z0RealBox.Text = Format(state.Z0Real); Z0ImaginaryBox.Text = Format(state.Z0Imaginary);
        CRealBox.Text = Format(state.CReal); CImaginaryBox.Text = Format(state.CImaginary); MBox.Text = Format(state.M);
        IterationsBox.Text = state.Iterations.ToString(CultureInfo.InvariantCulture); ThresholdBox.Text = Format(state.Threshold); ZoomBox.Text = Format(_zoom);
        ColoringBox.SelectedIndex = state.UseSmoothColoring ? 1 : 0; _paletteManager.ActivePalette = state.Palette.Clone($"Загружено: {state.SaveName}");
        UpdatePreviewTransform(); ScheduleRender(); ScheduleMapRender();
    }

    public Task<BitmapSource> RenderStatePreviewAsync(NovaState state, int width, int height, CancellationToken token) => RenderBitmapAsync(state, width, height, 1, token, null);
    private void Parameter_OnChanged(object sender, EventArgs e) => ScheduleRender();
    private void MapFormulaParameter_OnChanged(object sender, EventArgs e) { ScheduleRender(); ScheduleMapRender(); }
    private void JuliaMapParameter_OnChanged(object sender, EventArgs e) { ScheduleRender(); DrawMapMarker(); }
    private void ZoomBox_OnChanged(object sender, TextChangedEventArgs e) { if (TryRead(ZoomBox.Text, out decimal z)) { _zoom = Math.Clamp(z, 0.000000000000001m, 1_000_000_000_000_000m); UpdatePreviewTransform(); ScheduleRender(); } }
    private void RenderButton_OnClick(object sender, RoutedEventArgs e) => _ = RenderPreviewAsync();
    private void CancelButton_OnClick(object sender, RoutedEventArgs e) => _renderCts?.Cancel();
    private void PaletteButton_OnClick(object sender, RoutedEventArgs e) { var dialog = new MandelbrotPaletteWindow(_paletteManager) { Owner = this }; dialog.PaletteApplied += (_, _) => ScheduleRender(); dialog.ShowDialog(); }
    private void SavesButton_OnClick(object sender, RoutedEventArgs e) => new NovaSavesWindow(this, _saveStore, _variant) { Owner = this }.ShowDialog();

    private void ScheduleRender() { if (!IsLoaded) return; _renderCts?.Cancel(); _renderTimer.Stop(); _renderTimer.Start(); }
    private void ScheduleMapRender() { if (!IsLoaded || _variant != NovaVariant.Julia) return; _mapCts?.Cancel(); _mapTimer.Stop(); _mapTimer.Start(); }
    private void JuliaMapHost_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        DrawMapMarker();
        if (e.NewSize.Width > 1 && e.NewSize.Height > 1) ScheduleMapRender();
    }

    private async Task RenderPreviewAsync()
    {
        if (_isRendering) { ScheduleRender(); return; }
        NovaState state; try { state = CaptureState("preview"); } catch (Exception ex) { StatusText.Text = ex.Message; return; }
        _renderCts?.Dispose(); _renderCts = new CancellationTokenSource(); CancellationToken token = _renderCts.Token;
        var watch = Stopwatch.StartNew(); SetRendering(true, "Рендеринг Nova...");
        try
        {
            int factor = SsaaBox.SelectedItem is ComboBoxItem item ? Convert.ToInt32(item.Tag, CultureInfo.InvariantCulture) : 1;
            DpiScale dpi = VisualTreeHelper.GetDpi(CanvasHost);
            int width = checked(Math.Max(1, (int)Math.Ceiling(CanvasHost.ActualWidth * dpi.DpiScaleX)) * factor);
            int height = checked(Math.Max(1, (int)Math.Ceiling(CanvasHost.ActualHeight * dpi.DpiScaleY)) * factor);
            TileSchedulingStrategy strategy = RenderPatternSettings.SelectedPattern;
            IReadOnlyList<MandelbrotRenderTile> tiles = MandelbrotTileScheduler.Create(width, height, 16 * factor, strategy);
            var bitmap = new WriteableBitmap(width, height, dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Bgra32, null);
            var session = new RenderSession(bitmap, tiles.Count, width, height); _activeSession = session; CanvasImage.Source = bitmap;
            RenderOverlay.BeginSession(width, height); _visualizationTimer.Start();
            await RenderTilesAsync(state, tiles, session, GetThreadCount(), token);
            token.ThrowIfCancellationRequested(); FlushVisualizationEvents(session, true);
            BitmapSource completed = session.Bitmap.Clone(); completed.Freeze(); StablePreviewImage.Source = completed; CanvasImage.Source = null;
            _renderedCenterX = state.CenterX; _renderedCenterY = state.CenterY; _renderedZoom = state.Zoom; _hasRenderedFrame = true; UpdatePreviewTransform();
            StatusText.Text = $"Готово за {watch.Elapsed.TotalSeconds:F3} сек. Стратегия: {strategy}.";
        }
        catch (OperationCanceledException) { CanvasImage.Source = null; StatusText.Text = "Рендер отменён"; }
        catch (Exception ex) { CanvasImage.Source = null; MessageBox.Show(this, ex.Message, Title, MessageBoxButton.OK, MessageBoxImage.Error); }
        finally { _visualizationTimer.Stop(); RenderOverlay.EndSession(); _activeSession = null; SetRendering(false); }
    }

    private static async Task RenderTilesAsync(NovaState state, IReadOnlyList<MandelbrotRenderTile> tiles, RenderSession session, int threads, CancellationToken token)
    {
        var queue = new ConcurrentQueue<MandelbrotRenderTile>(tiles);
        Task[] workers = Enumerable.Range(0, Math.Clamp(threads, 1, Environment.ProcessorCount)).Select(_ => Task.Run(() =>
        {
            while (queue.TryDequeue(out MandelbrotRenderTile tile))
            {
                token.ThrowIfCancellationRequested(); session.Events.Enqueue(new TileEvent(true, tile, null));
                byte[] pixels = NovaRenderer.RenderTile(state, session.Width, session.Height, tile, token);
                session.Events.Enqueue(new TileEvent(false, tile, pixels));
            }
        }, token)).ToArray();
        await Task.WhenAll(workers);
    }

    private void FlushVisualizationEvents(RenderSession session, bool drain)
    {
        int processed = 0; bool changed = false;
        while ((drain || processed < 512) && session.Events.TryDequeue(out TileEvent entry))
        {
            if (entry.Start) RenderOverlay.StartTile(entry.Tile);
            else if (entry.Pixels is not null)
            {
                session.Bitmap.WritePixels(new Int32Rect(entry.Tile.X, entry.Tile.Y, entry.Tile.Width, entry.Tile.Height), entry.Pixels, entry.Tile.Width * 4, 0);
                RenderOverlay.CompleteTile(entry.Tile); session.Completed++;
            }
            processed++; changed = true;
        }
        if (!changed) return; RenderOverlay.Refresh(); RenderProgress.Value = session.Count == 0 ? 0 : session.Completed * 100d / session.Count;
    }

    private async Task RenderJuliaMapAsync()
    {
        if (_variant != NovaVariant.Julia || JuliaMapHost.ActualWidth <= 2 || JuliaMapHost.ActualHeight <= 2) return;
        NovaState state; try { state = CaptureState("map"); } catch { return; }
        state.Variant = NovaVariant.Mandelbrot; state.CenterX = 0; state.CenterY = 0; state.Zoom = 1; state.Iterations = 100;
        _mapCts?.Dispose(); _mapCts = new CancellationTokenSource(); CancellationToken token = _mapCts.Token;
        DpiScale dpi = VisualTreeHelper.GetDpi(JuliaMapHost);
        int width = Math.Max(160, (int)Math.Ceiling((JuliaMapHost.ActualWidth - 2) * dpi.DpiScaleX));
        int height = Math.Max(100, (int)Math.Ceiling((JuliaMapHost.ActualHeight - 2) * dpi.DpiScaleY));
        int stride = width * 4; byte[] pixels = new byte[stride * height];
        try
        {
            await Task.Run(() =>
            {
                var tile = new MandelbrotRenderTile(0, 0, width, height, 0, 0);
                byte[] rendered = NovaRenderer.RenderTile(state, width, height, tile, token, true);
                Buffer.BlockCopy(rendered, 0, pixels, 0, pixels.Length);
            }, token);
            BitmapSource bitmap = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, pixels, stride); bitmap.Freeze();
            JuliaMapPreviewImage.Source = bitmap; DrawMapMarker();
        }
        catch (OperationCanceledException) { }
    }

    private void JuliaMapPreview_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        NovaState state; try { state = CaptureState("selector"); } catch (Exception ex) { StatusText.Text = ex.Message; return; }
        var selector = new NovaParameterSelectorWindow(state) { Owner = this };
        selector.CoordinatesSelected += (real, imaginary) => { CRealBox.Text = Format(real); CImaginaryBox.Text = Format(imaginary); DrawMapMarker(); ScheduleRender(); };
        selector.ShowDialog();
    }

    private void DrawMapMarker()
    {
        JuliaMapMarker.Children.Clear(); if (!TryRead(CRealBox.Text, out decimal real) || !TryRead(CImaginaryBox.Text, out decimal imaginary)) return;
        double width = JuliaMapMarker.ActualWidth; double height = JuliaMapMarker.ActualHeight; if (width <= 0 || height <= 0) return;
        double x = ((double)real + 2) / 4 * width; double y = (2 - (double)imaginary) / 4 * height;
        var brush = new SolidColorBrush(Colors.Lime); brush.Freeze();
        JuliaMapMarker.Children.Add(new Line { X1 = x - 7, X2 = x + 7, Y1 = y, Y2 = y, Stroke = brush, StrokeThickness = 2 });
        JuliaMapMarker.Children.Add(new Line { X1 = x, X2 = x, Y1 = y - 7, Y2 = y + 7, Stroke = brush, StrokeThickness = 2 });
    }

    private async void ExportButton_OnClick(object sender, RoutedEventArgs e)
    {
        DpiScale dpi = VisualTreeHelper.GetDpi(CanvasHost);
        var options = new MandelbrotExportWindow { Owner = this, ExportWidth = Math.Max(1, (int)Math.Ceiling(CanvasHost.ActualWidth * dpi.DpiScaleX)), ExportHeight = Math.Max(1, (int)Math.Ceiling(CanvasHost.ActualHeight * dpi.DpiScaleY)) };
        if (options.ShowDialog() != true) return;
        string extension = options.ExportFormat switch { MandelbrotExportFormat.Jpeg => "jpg", MandelbrotExportFormat.Bmp => "bmp", _ => "png" };
        var save = new SaveFileDialog { Filter = options.ExportFormat switch { MandelbrotExportFormat.Jpeg => "JPEG image|*.jpg", MandelbrotExportFormat.Bmp => "Bitmap image|*.bmp", _ => "PNG image|*.png" }, FileName = $"{(_variant == NovaVariant.Julia ? "nova_julia" : "nova_mandelbrot")}_{DateTime.Now:yyyyMMdd_HHmmss}.{extension}" };
        if (save.ShowDialog(this) != true) return;
        _renderCts?.Cancel(); _renderCts?.Dispose(); _renderCts = new CancellationTokenSource(); CancellationToken token = _renderCts.Token; SetRendering(true, "Экспорт...");
        try
        {
            NovaState state = CaptureState("export"); BitmapSource bitmap;
            if (options.ProcessingMode == MandelbrotExportProcessingMode.Ssaa) bitmap = await RenderBitmapAsync(state, options.ExportWidth, options.ExportHeight, options.SsaaFactor, token, new Progress<int>(v => RenderProgress.Value = v));
            else
            {
                BitmapSource raw = await RenderBitmapAsync(state, options.RenderWidth, options.RenderHeight, 1, token, null);
                bitmap = options.ProcessingMode == MandelbrotExportProcessingMode.Lanczos
                    ? await Task.Run(() => BitmapResampler.ResizeLanczos3(raw, options.ExportWidth, options.ExportHeight, token), token)
                    : BitmapResampler.ResizeBicubic(raw, options.ExportWidth, options.ExportHeight);
            }
            BitmapEncoder encoder = options.ExportFormat switch { MandelbrotExportFormat.Jpeg => new JpegBitmapEncoder { QualityLevel = options.JpegQuality }, MandelbrotExportFormat.Bmp => new BmpBitmapEncoder(), _ => new PngBitmapEncoder() };
            encoder.Frames.Add(BitmapFrame.Create(bitmap)); await using FileStream stream = File.Create(save.FileName); encoder.Save(stream); StatusText.Text = $"Сохранено: {save.FileName}";
        }
        catch (OperationCanceledException) { StatusText.Text = "Экспорт отменён"; }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Ошибка экспорта", MessageBoxButton.OK, MessageBoxImage.Error); }
        finally { SetRendering(false); }
    }

    private async Task<BitmapSource> RenderBitmapAsync(NovaState state, int width, int height, int ssaa, CancellationToken token, IProgress<int>? progress)
    {
        int factor = Math.Clamp(ssaa, 1, 4), rw = checked(width * factor), rh = checked(height * factor), stride = checked(rw * 4), threads = GetThreadCount();
        byte[] pixels = new byte[checked(stride * rh)]; await Task.Run(() => NovaRenderer.Render(state, pixels, rw, rh, stride, threads, token, v => progress?.Report(factor == 1 ? v : v * 90 / 100)), token);
        BitmapSource source = BitmapSource.Create(rw, rh, 96, 96, PixelFormats.Bgra32, null, pixels, stride); source.Freeze();
        return factor == 1 ? source : await Task.Run(() => BitmapResampler.ResizeLanczos3(source, width, height, token, v => progress?.Report(v)), token);
    }

    private int GetThreadCount() => ThreadsBox.SelectedItem?.ToString() == "Auto" ? Environment.ProcessorCount : Math.Max(1, Convert.ToInt32(ThreadsBox.SelectedItem, CultureInfo.InvariantCulture));
    private void SetRendering(bool value, string? status = null) { _isRendering = value; CancelButton.IsEnabled = value; if (!value) RenderProgress.Value = 0; if (status is not null) StatusText.Text = status; }
    private void CanvasHost_OnSizeChanged(object sender, SizeChangedEventArgs e) { UpdatePreviewTransform(); ScheduleRender(); }
    private void CanvasHost_OnMouseWheel(object sender, MouseWheelEventArgs e) { Point mouse = e.GetPosition(CanvasHost); var before = ScreenToWorld(mouse); _zoom = Math.Clamp(_zoom * (e.Delta > 0 ? 1.2m : 1m / 1.2m), 0.000000000000001m, 1_000_000_000_000_000m); var after = ScreenToWorld(mouse); _centerX += before.X - after.X; _centerY += before.Y - after.Y; UpdatePreviewTransform(); ZoomBox.Text = Format(_zoom); ScheduleRender(); }
    private void CanvasHost_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e) { _panning = true; _lastPanPoint = e.GetPosition(CanvasHost); CanvasHost.CaptureMouse(); Mouse.OverrideCursor = Cursors.SizeAll; }
    private void CanvasHost_OnMouseMove(object sender, MouseEventArgs e) { if (!_panning) return; Point current = e.GetPosition(CanvasHost); var before = ScreenToWorld(_lastPanPoint); var after = ScreenToWorld(current); _centerX += before.X - after.X; _centerY += before.Y - after.Y; _lastPanPoint = current; UpdatePreviewTransform(); }
    private void CanvasHost_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e) { if (!_panning) return; _panning = false; CanvasHost.ReleaseMouseCapture(); Mouse.OverrideCursor = null; ScheduleRender(); }
    private (decimal X, decimal Y) ScreenToWorld(Point p) { decimal width = (decimal)Math.Max(1, CanvasHost.ActualWidth), scale = BaseScale / _zoom; return (_centerX + ((decimal)p.X - width / 2) * scale / width, _centerY + ((decimal)Math.Max(1, CanvasHost.ActualHeight) / 2 - (decimal)p.Y) * scale / width); }
    private void UpdatePreviewTransform() { if (!_hasRenderedFrame || _renderedZoom <= 0 || _zoom <= 0 || CanvasHost.ActualWidth <= 0) return; double scale = (double)(_zoom / _renderedZoom), width = CanvasHost.ActualWidth; decimal currentScale = BaseScale / _zoom; _previewScale.ScaleX = _previewScale.ScaleY = scale; _previewTranslation.X = (double)((_renderedCenterX - _centerX) / currentScale) * width; _previewTranslation.Y = (double)((_centerY - _renderedCenterY) / currentScale) * width; }
    private void ToggleControlsButton_OnClick(object sender, RoutedEventArgs e) { _controlsVisible = !_controlsVisible; ControlsColumn.Width = _controlsVisible ? new GridLength(300) : new GridLength(0); ControlsHost.Visibility = _controlsVisible ? Visibility.Visible : Visibility.Collapsed; ToggleControlsButton.Content = _controlsVisible ? "✕" : "☰"; ScheduleRender(); }
    private void Window_OnKeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.F11 || e.Key == Key.Escape && _isFullscreen) ToggleFullscreen(); }
    private void ToggleFullscreen() { if (!_isFullscreen) { _previousWindowStyle = WindowStyle; _previousWindowState = WindowState; WindowStyle = WindowStyle.None; WindowState = WindowState.Maximized; } else { WindowStyle = _previousWindowStyle; WindowState = _previousWindowState; } _isFullscreen = !_isFullscreen; }
    private void Window_OnClosing(object? sender, System.ComponentModel.CancelEventArgs e) { _renderTimer.Stop(); _mapTimer.Stop(); _visualizationTimer.Stop(); _renderCts?.Cancel(); _mapCts?.Cancel(); _renderCts?.Dispose(); _mapCts?.Dispose(); }
    private static bool TryRead(string text, out decimal value) => decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) || decimal.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value);
    private static string Format(decimal value) => value.ToString("G15", CultureInfo.InvariantCulture);
    private sealed class RenderSession(WriteableBitmap bitmap, int count, int width, int height) { public WriteableBitmap Bitmap { get; } = bitmap; public int Count { get; } = count; public int Width { get; } = width; public int Height { get; } = height; public int Completed { get; set; } public ConcurrentQueue<TileEvent> Events { get; } = new(); }
    private readonly record struct TileEvent(bool Start, MandelbrotRenderTile Tile, byte[]? Pixels);
}
