using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using perturbation_theory.Core.Rendering;
using perturbation_theory.Infrastructure;
using perturbation_theory.Models;

namespace perturbation_theory;

// Navigation and the two-layer preview are adapted from WPF Views/MandelbrotWindow.
// Render sessions own their cancellation, queue and bitmap so old work cannot repaint a new view.
public partial class MainWindow : Window
{
    private readonly DispatcherTimer _renderTimer = new() { Interval = TimeSpan.FromMilliseconds(180) };
    private readonly DispatcherTimer _displayTimer = new() { Interval = TimeSpan.FromMilliseconds(33) };
    private bool _ready, _updatingControls, _closing, _isPanning, _isFullscreen;
    private decimal _centerX = -0.5m, _centerY, _zoom = 0.75m;
    private Point _lastPanPoint;
    private WindowStyle _previousWindowStyle;
    private WindowState _previousWindowState;
    private RenderSession? _activeSession;
    private BitmapSource? _stableBitmap;
    private decimal _renderedCenterX, _renderedCenterY, _renderedZoom;
    private double _stableAspect = 1;

    public MainWindow()
    {
        InitializeComponent();
        _updatingControls = true;
        ThreadsBox.Items.Add("Auto");
        for (int i = 1; i <= Environment.ProcessorCount; i++) ThreadsBox.Items.Add(i);
        ThreadsBox.SelectedIndex = 0;
        PaletteBox.ItemsSource = BuiltInPalette.All;
        PaletteBox.SelectedIndex = 0;
        _updatingControls = false;
        _renderTimer.Tick += (_, _) => { _renderTimer.Stop(); _ = RenderPreviewAsync(); };
        _displayTimer.Tick += (_, _) => RefreshProgress();
        Deactivated += (_, _) => EndPan();
    }

    private void Window_OnLoaded(object sender, RoutedEventArgs e)
    {
        _ready = true;
        UpdatePrecisionHint();
        _ = RenderPreviewAsync();
    }

    private void Parameter_OnChanged(object sender, EventArgs e)
    {
        if (!_ready || _updatingControls) return;
        UpdatePrecisionHint();
        ScheduleRender();
    }

    private void Palette_OnChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_ready || _updatingControls) return;
        _updatingControls = true;
        PeriodBox.Text = ((BuiltInPalette)PaletteBox.SelectedItem).Period.ToString(CultureInfo.InvariantCulture);
        _updatingControls = false;
        ScheduleRender();
    }

    private void View_OnChanged(object sender, TextChangedEventArgs e)
    {
        if (!_ready || _updatingControls) return;
        CommitAndBakePreview();
        if (TryDecimal(CenterXBox.Text, out decimal x) && x is >= -1000m and <= 1000m &&
            TryDecimal(CenterYBox.Text, out decimal y) && y is >= -1000m and <= 1000m &&
            TryDecimal(ZoomBox.Text, out decimal zoom) &&
            zoom >= MandelbrotSettings.MinZoom && zoom <= MandelbrotSettings.MaxZoom)
        {
            (_centerX, _centerY, _zoom) = (x, y, zoom);
            UpdateCoarsePreviewTransform();
            UpdatePrecisionHint();
        }
        ScheduleRender();
    }

    private void ScheduleRender()
    {
        if (!_ready || _closing) return;
        CommitAndBakePreview();
        _renderTimer.Stop();
        _renderTimer.Start();
        StatusText.Text = "Ожидание рендера…";
        StatisticsText.Text = "";
        CancelButton.IsEnabled = true;
    }

    private MandelbrotSettings ReadSettings()
    {
        var settings = new MandelbrotSettings
        {
            CenterX = ReadDecimal(CenterXBox.Text, "Центр Re"),
            CenterY = ReadDecimal(CenterYBox.Text, "Центр Im"),
            Zoom = ReadDecimal(ZoomBox.Text, "Приближение"),
            Iterations = ReadInt(IterationsBox.Text, "Итерации"),
            EscapeRadius = ReadDecimal(ThresholdBox.Text, "Порог выхода"),
            Threads = ThreadsBox.SelectedItem is int count ? count : 0,
            Engine = (RenderEngine)EngineBox.SelectedIndex,
            Precision = (PrecisionMode)PrecisionBox.SelectedIndex,
            Coloring = (ColoringMode)ColoringBox.SelectedIndex,
            Palette = (BuiltInPalette)PaletteBox.SelectedItem,
            ColorPeriod = ReadInt(PeriodBox.Text, "Период палитры")
        };
        settings.Validate();
        return settings;
    }

    private async Task RenderPreviewAsync()
    {
        if (!_ready || _closing || _isPanning || CanvasHost.ActualWidth < 1 || CanvasHost.ActualHeight < 1) return;
        _renderTimer.Stop();
        CommitAndBakePreview();
        RenderSession? session = null;
        try
        {
            MandelbrotSettings settings = ReadSettings();
            RenderSurfaceMetrics surface = RenderSurfaceMetrics.Measure(CanvasHost);
            settings.ValidateSurface(surface.PixelWidth, surface.PixelHeight);
            (_centerX, _centerY, _zoom) = (settings.CenterX, settings.CenterY, settings.Zoom);
            UpdateCoarsePreviewTransform();
            var bitmap = new WriteableBitmap(surface.PixelWidth, surface.PixelHeight,
                surface.Dpi.PixelsPerInchX, surface.Dpi.PixelsPerInchY, PixelFormats.Bgra32, null);
            session = new RenderSession(settings, surface, bitmap);
            _activeSession = session;
            CanvasImage.Source = bitmap;
            RenderProgress.Value = 0;
            CancelButton.IsEnabled = true;
            StatusText.Text = settings.Engine == RenderEngine.Classic
                ? "Классический рендер…" : "Вычисление опорной орбиты…";
            StatisticsText.Text = "";
            UpdateCanvasStatus();
            _displayTimer.Start();

            RenderSession current = session;
            RenderStatistics stats = await Task.Run(() =>
            {
                if (settings.Engine == RenderEngine.Classic)
                {
                    var classic = new ClassicMandelbrotRenderer(settings);
                    Volatile.Write(ref current.ReferenceReady, 1);
                    return classic.Render(surface.PixelWidth, surface.PixelHeight, current.Cancellation.Token,
                        tile => current.Tiles.Enqueue(tile),
                        progress => Interlocked.Exchange(ref current.Progress, progress));
                }
                var renderer = new PerturbationRenderer(settings, current.Cancellation.Token);
                Volatile.Write(ref current.ReferenceReady, 1);
                return renderer.Render(surface.PixelWidth, surface.PixelHeight, current.Cancellation.Token,
                    tile => current.Tiles.Enqueue(tile),
                    progress => Interlocked.Exchange(ref current.Progress, progress));
            }, current.Cancellation.Token);

            if (_closing || !ReferenceEquals(_activeSession, current)) return;
            FlushTiles(current, true);
            bitmap.Freeze();
            SetStableBitmap(bitmap, settings.CenterX, settings.CenterY, settings.Zoom,
                surface.LogicalHeight / surface.LogicalWidth);
            CanvasImage.Source = null;
            _activeSession = null;
            _displayTimer.Stop();
            RenderProgress.Value = 100;
            StatusText.Text = $"Готово · {stats.Elapsed.TotalSeconds:N2} с · {surface.PixelWidth} × {surface.PixelHeight}";
            string engineDescription = DescribeEngine(settings.Engine, settings.Zoom, settings.Precision);
            StatisticsText.Text = settings.Engine == RenderEngine.Classic
                ? $"{engineDescription}\nПрямой расчёт: {stats.Pixels:N0} пикс."
                : $"{engineDescription}\nОпорная орбита: {stats.ReferenceIterations:N0} ит., {stats.ReferenceTime.TotalMilliseconds:N1} мс\n" +
                  $"Переустановок базы: {stats.Rebases:N0}\nПрямой пересчёт: {stats.FallbackPixels:N0} / {stats.Pixels:N0} пикс.";
            CancelButton.IsEnabled = false;
        }
        catch (OperationCanceledException)
        {
            if (!_closing && ReferenceEquals(_activeSession, session))
            {
                CommitAndBakePreview();
                StatusText.Text = "Рендер отменён";
            }
        }
        catch (Exception ex)
        {
            if (!_closing && (session is null || ReferenceEquals(_activeSession, session)))
            {
                CommitAndBakePreview();
                StatusText.Text = $"Ошибка: {ex.Message}";
                StatisticsText.Text = "";
                CancelButton.IsEnabled = false;
            }
        }
        finally
        {
            // Dispose only after the worker and its Parallel.ForEach have stopped.
            session?.Cancellation.Dispose();
        }
    }

    private void RefreshProgress()
    {
        if (_activeSession is not { } session) return;
        FlushTiles(session, false);
        RenderProgress.Value = Math.Max(RenderProgress.Value, Volatile.Read(ref session.Progress));
        if (Volatile.Read(ref session.ReferenceReady) != 0)
            StatusText.Text = $"Рендер · {RenderProgress.Value:0}%";
    }

    private static void WriteTile(RenderSession session, RenderedTile tile) =>
        session.Bitmap.WritePixels(new Int32Rect(tile.X, tile.Y, tile.Width, tile.Height),
            tile.Pixels, tile.Width * 4, 0);

    private void FlushTiles(RenderSession session, bool drainAll)
    {
        var clock = Stopwatch.StartNew();
        while ((drainAll || clock.ElapsedMilliseconds < 8) && session.Tiles.TryDequeue(out RenderedTile? tile))
            WriteTile(session, tile);
    }

    private void SetStableBitmap(BitmapSource bitmap, decimal x, decimal y, decimal zoom, double aspect)
    {
        _stableBitmap = bitmap;
        (_renderedCenterX, _renderedCenterY, _renderedZoom) = (x, y, zoom);
        _stableAspect = aspect;
        StablePreviewImage.Source = bitmap;
        StablePreviewImage.Visibility = Visibility.Visible;
        StablePreviewImage.RenderTransform = Transform.Identity;
        RenderOptions.SetBitmapScalingMode(StablePreviewImage, BitmapScalingMode.HighQuality);
    }

    private void CommitAndBakePreview(bool bake = true)
    {
        if (_activeSession is not { } session) return;
        session.Cancellation.Cancel();
        _activeSession = null;
        _displayTimer.Stop();
        // After resize, the old overlay has a different world aspect. Keep the stable image.
        bool sameSize = Math.Abs(CanvasHost.ActualWidth - session.Surface.LogicalWidth) < 0.5 &&
                        Math.Abs(CanvasHost.ActualHeight - session.Surface.LogicalHeight) < 0.5;
        if (bake && sameSize && !_closing)
        {
            try
            {
                FlushTiles(session, true);
                var baked = new RenderTargetBitmap(session.Surface.PixelWidth, session.Surface.PixelHeight,
                    session.Surface.Dpi.PixelsPerInchX, session.Surface.Dpi.PixelsPerInchY, PixelFormats.Pbgra32);
                baked.Render(ImageLayer);
                baked.Freeze();
                SetStableBitmap(baked, session.Settings.CenterX, session.Settings.CenterY, session.Settings.Zoom,
                    session.Surface.LogicalHeight / session.Surface.LogicalWidth);
            }
            catch (InvalidOperationException)
            {
                // Layout may briefly be unavailable while the window is being minimized.
            }
        }
        CanvasImage.Source = null;
        CancelButton.IsEnabled = false;
    }

    private void UpdateCoarsePreviewTransform()
    {
        if (_stableBitmap is null || _renderedZoom <= 0) return;
        double width = Math.Max(1, CanvasHost.ActualWidth), height = Math.Max(1, CanvasHost.ActualHeight);
        double scale = (double)(_zoom / _renderedZoom);
        double unitsPerPixel = (double)(3m / _zoom) / width;
        // Subtract centers in decimal before converting their small difference to double.
        double offsetX = width * (1 - scale) / 2 + (double)(_renderedCenterX - _centerX) / unitsPerPixel;
        double destinationHeight = width * _stableAspect * scale;
        double offsetY = (height - destinationHeight) / 2 + (double)(_centerY - _renderedCenterY) / unitsPerPixel;
        // An old image is not informative after a huge typed jump, and huge WPF transforms
        // can exceed the compositor's useful range.
        if (scale is < 1e-6 or > 1e6 || Math.Abs(offsetX) > 1e8 || Math.Abs(offsetY) > 1e8)
        {
            StablePreviewImage.Visibility = Visibility.Hidden;
            return;
        }
        StablePreviewImage.Visibility = Visibility.Visible;
        StablePreviewImage.RenderTransform = new MatrixTransform(new Matrix(scale, 0, 0,
            destinationHeight / height, offsetX, offsetY));
        RenderOptions.SetBitmapScalingMode(StablePreviewImage, BitmapScalingMode.LowQuality);
    }

    private void CanvasHost_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (!_ready || _closing) return;
        CommitAndBakePreview(false);
        UpdateCoarsePreviewTransform();
        ScheduleRender();
    }

    private void CanvasHost_OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (!_ready) return;
        EndPan();
        CommitAndBakePreview();
        Point mouse = e.GetPosition(CanvasHost);
        decimal width = (decimal)Math.Max(1, CanvasHost.ActualWidth);
        decimal height = (decimal)Math.Max(1, CanvasHost.ActualHeight);
        int pixelWidth = RenderSurfaceMetrics.Measure(CanvasHost).PixelWidth;
        decimal maxZoom = Math.Min(MandelbrotSettings.MaxZoom, 3m / MandelbrotSettings.MinPixelStep / pixelWidth);
        decimal nextZoom = Math.Clamp(_zoom * (e.Delta > 0 ? 1.5m : 1m / 1.5m),
            MandelbrotSettings.MinZoom, maxZoom);
        decimal widthDifference = 3m / _zoom - 3m / nextZoom;
        // Work with offsets rather than subtracting two large absolute world positions.
        _centerX += ((decimal)mouse.X / width - 0.5m) * widthDifference;
        _centerY += (0.5m - (decimal)mouse.Y / height) * widthDifference * height / width;
        _centerX = Math.Clamp(_centerX, -1000m, 1000m);
        _centerY = Math.Clamp(_centerY, -1000m, 1000m);
        _zoom = nextZoom;
        SyncViewControls();
        UpdateCoarsePreviewTransform();
        ScheduleRender();
        e.Handled = true;
    }

    private void CanvasHost_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        CanvasHost.Focus();
        _renderTimer.Stop();
        CommitAndBakePreview();
        _isPanning = true;
        _lastPanPoint = e.GetPosition(CanvasHost);
        CanvasHost.CaptureMouse();
        CanvasHost.Cursor = Cursors.SizeAll;
        e.Handled = true;
    }

    private void CanvasHost_OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isPanning) return;
        if (e.LeftButton != MouseButtonState.Pressed) { EndPan(); return; }
        Point current = e.GetPosition(CanvasHost);
        decimal unitsPerPixel = (3m / _zoom) / (decimal)Math.Max(1, CanvasHost.ActualWidth);
        _centerX = Math.Clamp(_centerX + (decimal)(_lastPanPoint.X - current.X) * unitsPerPixel, -1000m, 1000m);
        _centerY = Math.Clamp(_centerY + (decimal)(current.Y - _lastPanPoint.Y) * unitsPerPixel, -1000m, 1000m);
        _lastPanPoint = current;
        SyncViewControls();
        UpdateCoarsePreviewTransform();
    }

    private void CanvasHost_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e) => EndPan();
    private void CanvasHost_OnLostMouseCapture(object sender, MouseEventArgs e) => EndPan();

    private void EndPan()
    {
        if (!_isPanning) return;
        _isPanning = false;
        CanvasHost.ReleaseMouseCapture();
        CanvasHost.Cursor = null;
        ScheduleRender();
    }

    private void SyncViewControls()
    {
        _updatingControls = true;
        CenterXBox.Text = _centerX.ToString("G29", CultureInfo.InvariantCulture);
        CenterYBox.Text = _centerY.ToString("G29", CultureInfo.InvariantCulture);
        ZoomBox.Text = _zoom.ToString("G29", CultureInfo.InvariantCulture);
        _updatingControls = false;
        UpdateCanvasStatus();
        UpdatePrecisionHint();
    }

    private void UpdateCanvasStatus() =>
        CanvasStatusText.Text = $"×{_zoom:G5} · {DescribeEngine((RenderEngine)EngineBox.SelectedIndex, _zoom, (PrecisionMode)PrecisionBox.SelectedIndex)}";

    private static string DescribeEngine(RenderEngine engine, decimal zoom, PrecisionMode precision) =>
        engine == RenderEngine.Classic
            ? $"Классический · {(ClassicMandelbrotRenderer.UsesDecimal(zoom) ? "decimal" : "double")} (авто)"
            : $"Perturbation · {precision switch
            {
                PrecisionMode.Double => "double → double",
                PrecisionMode.DecimalReference => "decimal → double",
                _ => "decimal → decimal"
            }}";

    private void UpdatePrecisionHint()
    {
        bool classic = EngineBox.SelectedIndex == (int)RenderEngine.Classic;
        PrecisionBox.Visibility = classic ? Visibility.Collapsed : Visibility.Visible;
        PrecisionLabel.Text = classic
            ? $"Точность: {(ClassicMandelbrotRenderer.UsesDecimal(_zoom) ? "decimal" : "double")} (авто)"
            : "Опорная орбита → отклонения";
        if (classic)
        {
            PrecisionHint.Text = "Как в оригинале: double до зума 1,5×10⁹, decimal — выше.";
            return;
        }
        PrecisionHint.Text = PrecisionBox.SelectedIndex switch
        {
            0 => "Быстрая опорная орбита. Точность ограничена double.",
            1 => "Точная опорная орбита decimal, быстрые отклонения double.",
            _ => "Эксперимент: все итерации decimal. Обычно медленнее гибридного режима."
        };
    }

    private void RenderButton_OnClick(object sender, RoutedEventArgs e) => _ = RenderPreviewAsync();
    private void CancelButton_OnClick(object sender, RoutedEventArgs e)
    {
        _renderTimer.Stop();
        CommitAndBakePreview();
        CancelButton.IsEnabled = false;
        StatusText.Text = "Рендер отменён";
    }

    private void ResetButton_OnClick(object sender, RoutedEventArgs e)
    {
        CommitAndBakePreview();
        (_centerX, _centerY, _zoom) = (-0.5m, 0m, 0.75m);
        SyncViewControls();
        UpdateCoarsePreviewTransform();
        ScheduleRender();
    }

    private void DeepZoomButton_OnClick(object sender, RoutedEventArgs e)
    {
        CommitAndBakePreview();
        (_centerX, _centerY, _zoom) = (-0.743643887037151m, 0.131825904205330m, 1_000_000_000_000m);
        _updatingControls = true;
        IterationsBox.Text = "4000";
        PrecisionBox.SelectedIndex = 1;
        PaletteBox.SelectedIndex = 2;
        PeriodBox.Text = "400";
        _updatingControls = false;
        SyncViewControls();
        UpdatePrecisionHint();
        UpdateCoarsePreviewTransform();
        ScheduleRender();
    }

    private void ToggleControlsButton_OnClick(object sender, RoutedEventArgs e)
    {
        bool visible = ParametersBorder.Visibility == Visibility.Visible;
        ParametersBorder.Visibility = visible ? Visibility.Collapsed : Visibility.Visible;
        ParametersColumn.Width = new GridLength(visible ? 0 : 310);
        ToggleControlsButton.Content = visible ? "☰" : "✕";
        ToggleControlsButton.ToolTip = visible ? "Показать панель параметров" : "Скрыть панель параметров";
    }

    private void Window_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F11 || (e.Key == Key.Escape && _isFullscreen))
        {
            ToggleFullscreen();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            EndPan();
            CancelButton_OnClick(sender, e);
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            _ = RenderPreviewAsync();
            e.Handled = true;
        }
    }

    private void ToggleFullscreen()
    {
        EndPan();
        if (!_isFullscreen)
        {
            _previousWindowStyle = WindowStyle;
            _previousWindowState = WindowState;
            WindowState = WindowState.Normal;
            WindowStyle = WindowStyle.None;
            WindowState = WindowState.Maximized;
        }
        else
        {
            WindowState = WindowState.Normal;
            WindowStyle = _previousWindowStyle;
            WindowState = _previousWindowState;
        }
        _isFullscreen = !_isFullscreen;
    }

    private void Window_OnClosing(object? sender, CancelEventArgs e)
    {
        _closing = true;
        EndPan();
        _renderTimer.Stop();
        _displayTimer.Stop();
        CommitAndBakePreview(false);
    }

    private static bool TryDecimal(string text, out decimal value) =>
        decimal.TryParse(text.Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out value);

    private static decimal ReadDecimal(string text, string name) =>
        TryDecimal(text, out decimal value) ? value : throw new ArgumentException($"«{name}»: введите число.");

    private static int ReadInt(string text, string name) =>
        int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
            ? value : throw new ArgumentException($"«{name}»: введите целое число.");

    private sealed class RenderSession(MandelbrotSettings settings, RenderSurfaceMetrics surface, WriteableBitmap bitmap)
    {
        public MandelbrotSettings Settings { get; } = settings;
        public RenderSurfaceMetrics Surface { get; } = surface;
        public WriteableBitmap Bitmap { get; } = bitmap;
        public CancellationTokenSource Cancellation { get; } = new();
        public ConcurrentQueue<RenderedTile> Tiles { get; } = new();
        public int ReferenceReady;
        public int Progress;
    }
}
