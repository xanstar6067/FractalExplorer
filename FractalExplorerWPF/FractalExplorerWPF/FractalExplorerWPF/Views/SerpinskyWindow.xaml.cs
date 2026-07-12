using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using FractalExplorer.Engines;
using FractalExplorerWPF.Infrastructure;
using FractalExplorerWPF.Models;
using Microsoft.Win32;
using DrawingColor = System.Drawing.Color;
using MediaColor = System.Windows.Media.Color;
using Point = System.Windows.Point;

namespace FractalExplorerWPF.Views;

public partial class SerpinskyWindow : Window
{
    private readonly DispatcherTimer _renderTimer = new() { Interval = TimeSpan.FromMilliseconds(300) };
    private readonly SerpinskyPaletteManager _paletteManager = new();
    private readonly SerpinskySaveStore _saveStore = new();
    private CancellationTokenSource? _renderCts;
    private bool _isRendering;
    private bool _isPanning;
    private bool _isFullscreen;
    private WindowStyle _previousWindowStyle;
    private WindowState _previousWindowState;
    private Point _lastPanPoint;
    private double _centerX;
    private double _centerY;
    private double _zoom = 1.0;

    public SerpinskyWindow()
    {
        InitializeComponent();
        _renderTimer.Tick += RenderTimer_OnTick;
        RenderModeBox.SelectedIndex = 0;
        IterationsBox.Text = "8";
        ZoomBox.Text = "1";

        for (int threadCount = 1; threadCount <= Environment.ProcessorCount; threadCount++)
        {
            ThreadsBox.Items.Add(threadCount);
        }
        ThreadsBox.Items.Add("Auto");
        ThreadsBox.SelectedItem = "Auto";

        Loaded += (_, _) => ScheduleRender();
    }

    public SerpinskySaveState CaptureState(string saveName) =>
        new()
        {
            SaveName = saveName,
            Timestamp = DateTime.Now,
            RenderMode = SelectedRenderMode,
            Iterations = ReadIterations(),
            Zoom = _zoom,
            CenterX = _centerX,
            CenterY = _centerY,
            FractalColor = _paletteManager.ActivePalette.FractalColor,
            BackgroundColor = _paletteManager.ActivePalette.BackgroundColor
        };

    public void LoadState(SerpinskySaveState state)
    {
        _renderCts?.Cancel();
        RenderModeBox.SelectedIndex = state.RenderMode == SerpinskyRenderMode.Geometric ? 0 : 1;
        IterationsBox.Text = state.Iterations.ToString(CultureInfo.InvariantCulture);
        _zoom = Math.Clamp(state.Zoom, 0.01, 10_000_000);
        _centerX = state.CenterX;
        _centerY = state.CenterY;
        ZoomBox.Text = _zoom.ToString("0.####", CultureInfo.InvariantCulture);

        _paletteManager.ActivePalette = new SerpinskyPalette
        {
            Name = $"Загружено: {state.SaveName}",
            FractalColor = state.FractalColor,
            BackgroundColor = state.BackgroundColor
        };
        ScheduleRender();
    }

    public async Task<BitmapSource> RenderStatePreviewAsync(
        SerpinskySaveState state,
        int width,
        int height,
        CancellationToken cancellationToken)
    {
        int iterations = state.RenderMode == SerpinskyRenderMode.Geometric
            ? Math.Min(state.Iterations, 6)
            : Math.Min(state.Iterations, 20_000);
        return await RenderBitmapAsync(state, width, height, iterations, 1, cancellationToken, null);
    }

    private SerpinskyRenderMode SelectedRenderMode =>
        RenderModeBox.SelectedIndex == 1
            ? SerpinskyRenderMode.Chaos
            : SerpinskyRenderMode.Geometric;

    private void Parameter_OnChanged(object sender, EventArgs e)
    {
        if (!IsLoaded)
        {
            return;
        }

        if (sender == RenderModeBox)
        {
            IterationsBox.Text = SelectedRenderMode == SerpinskyRenderMode.Geometric ? "8" : "50000";
        }
        ScheduleRender();
    }

    private void ZoomBox_OnTextChanged(object sender, EventArgs e)
    {
        if (TryReadDouble(ZoomBox.Text, out double zoom))
        {
            _zoom = Math.Clamp(zoom, 0.01, 10_000_000);
            ScheduleRender();
        }
    }

    private void RenderButton_OnClick(object sender, RoutedEventArgs e) => _ = RenderPreviewAsync();

    private void CancelButton_OnClick(object sender, RoutedEventArgs e) => _renderCts?.Cancel();

    private void PaletteButton_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new SerpinskyPaletteWindow(_paletteManager) { Owner = this };
        if (dialog.ShowDialog() == true)
        {
            ScheduleRender();
        }
    }

    private void SavesButton_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new SerpinskySavesWindow(this, _saveStore) { Owner = this };
        dialog.ShowDialog();
    }

    private async void ExportButton_OnClick(object sender, RoutedEventArgs e)
    {
        DpiScale dpi = VisualTreeHelper.GetDpi(CanvasHost);
        var options = new SerpinskyExportWindow
        {
            Owner = this,
            ExportWidth = Math.Max(1, (int)Math.Ceiling(CanvasHost.ActualWidth * dpi.DpiScaleX)),
            ExportHeight = Math.Max(1, (int)Math.Ceiling(CanvasHost.ActualHeight * dpi.DpiScaleY))
        };
        if (options.ShowDialog() != true)
        {
            return;
        }

        var saveDialog = new SaveFileDialog
        {
            Filter = "PNG image|*.png",
            FileName = $"serpinski_{DateTime.Now:yyyyMMdd_HHmmss}.png"
        };
        if (saveDialog.ShowDialog(this) != true)
        {
            return;
        }

        _renderCts?.Cancel();
        using var exportCts = new CancellationTokenSource();
        _renderCts = exportCts;
        SetRenderingState(true, "Экспорт изображения...");
        try
        {
            SerpinskySaveState state = CaptureState("export");
            BitmapSource bitmap = await RenderBitmapAsync(
                state,
                options.ExportWidth,
                options.ExportHeight,
                state.Iterations,
                options.SsaaFactor,
                exportCts.Token,
                new Progress<int>(value => RenderProgress.Value = value));

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            await using FileStream stream = File.Create(saveDialog.FileName);
            encoder.Save(stream);
            StatusText.Text = $"Сохранено: {saveDialog.FileName}";
        }
        catch (OperationCanceledException)
        {
            StatusText.Text = "Экспорт отменён";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Ошибка экспорта", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            SetRenderingState(false);
        }
    }

    private void ScheduleRender()
    {
        if (!IsLoaded)
        {
            return;
        }

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
        if (_isRendering)
        {
            ScheduleRender();
            return;
        }

        DpiScale dpi = VisualTreeHelper.GetDpi(CanvasHost);
        int width = Math.Max(1, (int)Math.Ceiling(CanvasHost.ActualWidth * dpi.DpiScaleX));
        int height = Math.Max(1, (int)Math.Ceiling(CanvasHost.ActualHeight * dpi.DpiScaleY));
        SerpinskySaveState state;
        try
        {
            state = CaptureState("preview");
        }
        catch (Exception ex)
        {
            StatusText.Text = ex.Message;
            return;
        }

        _renderCts?.Dispose();
        _renderCts = new CancellationTokenSource();
        CancellationToken token = _renderCts.Token;
        var stopwatch = Stopwatch.StartNew();
        SetRenderingState(true, "Рендеринг...");

        try
        {
            BitmapSource bitmap = await RenderBitmapAsync(
                state,
                width,
                height,
                state.Iterations,
                1,
                token,
                new Progress<int>(value => RenderProgress.Value = value));
            token.ThrowIfCancellationRequested();
            CanvasImage.Source = bitmap;
            stopwatch.Stop();
            StatusText.Text = $"Готово за {stopwatch.Elapsed.TotalSeconds:F3} сек.";
        }
        catch (OperationCanceledException)
        {
            StatusText.Text = "Рендер отменён";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Ошибка рендера";
            MessageBox.Show(this, ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            SetRenderingState(false);
        }
    }

    private async Task<BitmapSource> RenderBitmapAsync(
        SerpinskySaveState state,
        int width,
        int height,
        int iterations,
        int ssaaFactor,
        CancellationToken token,
        IProgress<int>? progress)
    {
        int factor = state.RenderMode == SerpinskyRenderMode.Geometric
            ? Math.Clamp(ssaaFactor, 1, 4)
            : 1;
        int renderWidth = checked(width * factor);
        int renderHeight = checked(height * factor);
        int stride = checked(renderWidth * 4);
        byte[] buffer = new byte[checked(stride * renderHeight)];
        var engine = CreateEngine(state, iterations);
        int threadCount = GetThreadCount();

        await Task.Run(
            () => engine.RenderToBuffer(
                buffer,
                renderWidth,
                renderHeight,
                stride,
                4,
                threadCount,
                token,
                value => progress?.Report(value)),
            token);
        token.ThrowIfCancellationRequested();

        BitmapSource source = BitmapSource.Create(
            renderWidth,
            renderHeight,
            96,
            96,
            PixelFormats.Bgra32,
            null,
            buffer,
            stride);
        source.Freeze();

        if (factor == 1)
        {
            return source;
        }

        var scaled = new TransformedBitmap(source, new ScaleTransform(1.0 / factor, 1.0 / factor));
        scaled.Freeze();
        return scaled;
    }

    private static FractalSerpinskyEngine CreateEngine(SerpinskySaveState state, int iterations) =>
        new()
        {
            RenderMode = state.RenderMode,
            Iterations = iterations,
            Zoom = state.Zoom,
            CenterX = state.CenterX,
            CenterY = state.CenterY,
            ColorMode = SerpinskyColorMode.CustomColor,
            FractalColor = ToDrawingColor(state.FractalColor),
            BackgroundColor = ToDrawingColor(state.BackgroundColor)
        };

    private static DrawingColor ToDrawingColor(MediaColor color) =>
        DrawingColor.FromArgb(color.A, color.R, color.G, color.B);

    private int ReadIterations()
    {
        if (!int.TryParse(IterationsBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
        {
            throw new InvalidOperationException("Введите целое количество итераций.");
        }

        int minimum = SelectedRenderMode == SerpinskyRenderMode.Geometric ? 0 : 1_000;
        int maximum = SelectedRenderMode == SerpinskyRenderMode.Geometric ? 20 : int.MaxValue;
        return Math.Clamp(value, minimum, maximum);
    }

    private int GetThreadCount() =>
        ThreadsBox.SelectedItem?.ToString() == "Auto"
            ? Environment.ProcessorCount
            : Math.Max(1, Convert.ToInt32(ThreadsBox.SelectedItem, CultureInfo.InvariantCulture));

    private void SetRenderingState(bool rendering, string? status = null)
    {
        _isRendering = rendering;
        CancelButton.IsEnabled = rendering;
        if (!rendering)
        {
            RenderProgress.Value = 0;
        }
        if (status is not null)
        {
            StatusText.Text = status;
        }
    }

    private void CanvasHost_OnSizeChanged(object sender, SizeChangedEventArgs e) => ScheduleRender();

    private void CanvasHost_OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        Point mouse = e.GetPosition(CanvasHost);
        Point worldBefore = ScreenToWorld(mouse);
        _zoom = Math.Clamp(_zoom * (e.Delta > 0 ? 1.2 : 1.0 / 1.2), 0.01, 10_000_000);
        Point worldAfter = ScreenToWorld(mouse);
        _centerX += worldBefore.X - worldAfter.X;
        _centerY += worldBefore.Y - worldAfter.Y;
        ZoomBox.Text = _zoom.ToString("0.####", CultureInfo.InvariantCulture);
        ScheduleRender();
    }

    private void CanvasHost_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isPanning = true;
        _lastPanPoint = e.GetPosition(CanvasHost);
        CanvasHost.CaptureMouse();
        Mouse.OverrideCursor = Cursors.SizeAll;
    }

    private void CanvasHost_OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isPanning)
        {
            return;
        }

        Point current = e.GetPosition(CanvasHost);
        Point worldBefore = ScreenToWorld(_lastPanPoint);
        Point worldAfter = ScreenToWorld(current);
        _centerX += worldBefore.X - worldAfter.X;
        _centerY += worldBefore.Y - worldAfter.Y;
        _lastPanPoint = current;
    }

    private void CanvasHost_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isPanning)
        {
            return;
        }

        _isPanning = false;
        CanvasHost.ReleaseMouseCapture();
        Mouse.OverrideCursor = null;
        ScheduleRender();
    }

    private Point ScreenToWorld(Point point)
    {
        double width = Math.Max(1, CanvasHost.ActualWidth);
        double height = Math.Max(1, CanvasHost.ActualHeight);
        double viewHeight = 1.0 / _zoom;
        double viewWidth = viewHeight * width / height;
        return new Point(
            _centerX - viewWidth / 2.0 + point.X / width * viewWidth,
            _centerY + viewHeight / 2.0 - point.Y / height * viewHeight);
    }

    private static bool TryReadDouble(string text, out double value) =>
        double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) ||
        double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value);

    private void Window_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F11)
        {
            ToggleFullscreen();
        }
        else if (e.Key == Key.Escape && _isFullscreen)
        {
            ToggleFullscreen();
        }
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
        _renderCts?.Cancel();
        _renderCts?.Dispose();
    }
}
