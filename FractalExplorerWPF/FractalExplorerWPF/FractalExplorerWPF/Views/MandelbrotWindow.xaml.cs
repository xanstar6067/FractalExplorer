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
using FractalExplorerWPF.Infrastructure;
using FractalExplorerWPF.Models;
using Microsoft.Win32;
using Point = System.Windows.Point;

namespace FractalExplorerWPF.Views;

public partial class MandelbrotWindow : Window
{
    private readonly MandelbrotVariantDefinition _definition;
    private readonly MandelbrotPaletteManager _paletteManager = new();
    private readonly MandelbrotSaveStore _saveStore;
    private readonly DispatcherTimer _renderTimer = new() { Interval = TimeSpan.FromMilliseconds(320) };
    private CancellationTokenSource? _renderCts;
    private bool _isRendering;
    private bool _isPanning;
    private bool _isFullscreen;
    private bool _updatingControls;
    private WindowStyle _previousWindowStyle;
    private WindowState _previousWindowState;
    private Point _lastPanPoint;
    private decimal _centerX;
    private decimal _centerY;
    private decimal _zoom;

    public MandelbrotWindow(MandelbrotVariant variant)
    {
        _definition = MandelbrotVariantDefinition.For(variant);
        _saveStore = new MandelbrotSaveStore(variant);
        InitializeComponent();
        Title = _definition.DisplayName;
        HeaderText.Text = _definition.DisplayName;
        _renderTimer.Tick += RenderTimer_OnTick;
        InitializeControls();
        ResetView(false);
        Loaded += (_, _) => ScheduleRender();
    }

    private void InitializeControls()
    {
        _updatingControls = true;
        IterationsBox.Text = "500";
        ThresholdBox.Text = "2";
        PowerBox.Text = _definition.DefaultPower.ToString(CultureInfo.InvariantCulture);
        PowerPanel.Visibility = _definition.HasPower ? Visibility.Visible : Visibility.Collapsed;
        InversionBox.Visibility = _definition.HasInversion ? Visibility.Visible : Visibility.Collapsed;

        for (int count = 1; count <= Environment.ProcessorCount; count++) ThreadsBox.Items.Add(count);
        ThreadsBox.Items.Add("Auto");
        ThreadsBox.SelectedItem = "Auto";
        ColoringModeBox.SelectedIndex = 1;
        HistogramContrastBox.Text = "1";
        OrbitStrengthBox.Text = "1";
        OrbitBiasBox.Text = "0";
        StripeFrequencyBox.Text = "3";
        StripeStrengthBox.Text = "0.5";
        StripeBiasBox.Text = "0";
        PolyABox.Text = "9";
        PolyBBox.Text = "15";
        PolyCBox.Text = "8.5";
        PolyGammaBox.Text = "1";
        PolyBlendBox.Text = "1";
        PolyBiasBox.Text = "0";
        _updatingControls = false;
    }

    public MandelbrotState CaptureState(string saveName)
    {
        int iterations = ReadInt(IterationsBox.Text, "итерации", 1, 100_000);
        decimal threshold = ReadDecimal(ThresholdBox.Text, "порог выхода", 1.0001m, 1_000m);
        decimal power = _definition.HasPower
            ? ReadDecimal(PowerBox.Text, "степень", 0.1m, 12m)
            : 2m;
        MandelbrotPalette palette = _paletteManager.ActivePalette.Clone(_paletteManager.ActivePalette.Name);

        return new MandelbrotState
        {
            SaveName = saveName,
            Timestamp = DateTime.Now,
            Variant = _definition.Variant,
            CenterX = _centerX,
            CenterY = _centerY,
            Zoom = _zoom,
            Iterations = iterations,
            Threshold = threshold,
            Threads = GetThreadCount(),
            ColoringMode = SelectedColoringMode,
            PaletteName = palette.Name,
            Palette = palette,
            Power = power,
            UseInversion = InversionBox.IsChecked == true,
            HistogramContrast = ReadDouble(HistogramContrastBox.Text, "контраст", 0.01, 100),
            OrbitTrapStrength = ReadDouble(OrbitStrengthBox.Text, "сила ловушки", 0, 100),
            OrbitTrapBias = ReadDouble(OrbitBiasBox.Text, "смещение ловушки", -10, 10),
            StripeFrequency = ReadDouble(StripeFrequencyBox.Text, "частота полос", 0, 1_000),
            StripeStrength = ReadDouble(StripeStrengthBox.Text, "сила полос", 0, 1),
            StripeBias = ReadDouble(StripeBiasBox.Text, "смещение полос", -10, 10),
            PolynomialA = ReadDouble(PolyABox.Text, "коэффициент A", -100, 100),
            PolynomialB = ReadDouble(PolyBBox.Text, "коэффициент B", -100, 100),
            PolynomialC = ReadDouble(PolyCBox.Text, "коэффициент C", -100, 100),
            PolynomialGamma = ReadDouble(PolyGammaBox.Text, "гамма полинома", 0.01, 100),
            PolynomialBlend = ReadDouble(PolyBlendBox.Text, "смешивание полинома", 0, 1),
            PolynomialBias = ReadDouble(PolyBiasBox.Text, "смещение полинома", -10, 10)
        };
    }

    public void LoadState(MandelbrotState state)
    {
        if (state.Variant != _definition.Variant) return;
        _renderCts?.Cancel();
        _updatingControls = true;
        _centerX = state.CenterX;
        _centerY = state.CenterY;
        _zoom = Math.Clamp(state.Zoom, 0.000000000001m, 1000000000000000000000000000m);
        IterationsBox.Text = state.Iterations.ToString(CultureInfo.InvariantCulture);
        ThresholdBox.Text = state.Threshold.ToString(CultureInfo.InvariantCulture);
        ZoomBox.Text = _zoom.ToString("G8", CultureInfo.InvariantCulture);
        PowerBox.Text = state.Power.ToString(CultureInfo.InvariantCulture);
        InversionBox.IsChecked = state.UseInversion;
        ColoringModeBox.SelectedIndex = (int)state.ColoringMode;
        HistogramContrastBox.Text = state.HistogramContrast.ToString(CultureInfo.InvariantCulture);
        OrbitStrengthBox.Text = state.OrbitTrapStrength.ToString(CultureInfo.InvariantCulture);
        OrbitBiasBox.Text = state.OrbitTrapBias.ToString(CultureInfo.InvariantCulture);
        StripeFrequencyBox.Text = state.StripeFrequency.ToString(CultureInfo.InvariantCulture);
        StripeStrengthBox.Text = state.StripeStrength.ToString(CultureInfo.InvariantCulture);
        StripeBiasBox.Text = state.StripeBias.ToString(CultureInfo.InvariantCulture);
        PolyABox.Text = state.PolynomialA.ToString(CultureInfo.InvariantCulture);
        PolyBBox.Text = state.PolynomialB.ToString(CultureInfo.InvariantCulture);
        PolyCBox.Text = state.PolynomialC.ToString(CultureInfo.InvariantCulture);
        PolyGammaBox.Text = state.PolynomialGamma.ToString(CultureInfo.InvariantCulture);
        PolyBlendBox.Text = state.PolynomialBlend.ToString(CultureInfo.InvariantCulture);
        PolyBiasBox.Text = state.PolynomialBias.ToString(CultureInfo.InvariantCulture);
        MandelbrotPalette loadedPalette = state.Palette;
        if (!string.IsNullOrWhiteSpace(state.PaletteName) &&
            (loadedPalette.Colors.Count == 0 || loadedPalette.Name == "Новая палитра"))
        {
            loadedPalette = _paletteManager.Palettes.FirstOrDefault(palette =>
                palette.Name.Equals(state.PaletteName, StringComparison.OrdinalIgnoreCase)) ?? loadedPalette;
        }
        _paletteManager.ActivePalette = loadedPalette.Clone(
            string.IsNullOrWhiteSpace(state.PaletteName) ? loadedPalette.Name : state.PaletteName);
        _updatingControls = false;
        ScheduleRender();
    }

    public Task<BitmapSource> RenderStatePreviewAsync(MandelbrotState state, int width, int height, CancellationToken token)
    {
        MandelbrotState preview = CloneState(state);
        preview.Iterations = Math.Min(preview.Iterations, 600);
        preview.Threads = 0;
        return RenderBitmapAsync(preview, width, height, 1, token, null);
    }

    private MandelbrotColoringMode SelectedColoringMode =>
        ColoringModeBox.SelectedIndex < 0 ? MandelbrotColoringMode.Smooth : (MandelbrotColoringMode)ColoringModeBox.SelectedIndex;

    private void Parameter_OnChanged(object sender, EventArgs e)
    {
        if (!_updatingControls && IsLoaded) ScheduleRender();
    }

    private void ColoringMode_OnChanged(object sender, EventArgs e) => Parameter_OnChanged(sender, e);

    private void ZoomBox_OnTextChanged(object sender, EventArgs e)
    {
        if (!_updatingControls && TryReadDecimal(ZoomBox.Text, out decimal zoom) && zoom > 0)
        {
            _zoom = Math.Clamp(zoom, 0.000000000001m, 1000000000000000000000000000m);
            ScheduleRender();
        }
    }

    private void PaletteButton_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new MandelbrotPaletteWindow(_paletteManager) { Owner = this };
        if (dialog.ShowDialog() == true) ScheduleRender();
    }

    private void SavesButton_OnClick(object sender, RoutedEventArgs e) =>
        new MandelbrotSavesWindow(this, _saveStore) { Owner = this }.ShowDialog();

    private async void ExportButton_OnClick(object sender, RoutedEventArgs e)
    {
        var options = new MandelbrotExportWindow
        {
            Owner = this,
            ExportWidth = Math.Max(1, (int)CanvasHost.ActualWidth),
            ExportHeight = Math.Max(1, (int)CanvasHost.ActualHeight)
        };
        if (options.ShowDialog() != true) return;

        var saveDialog = new SaveFileDialog
        {
            Filter = "PNG image|*.png",
            FileName = $"{_definition.Identifier}_{DateTime.Now:yyyyMMdd_HHmmss}.png"
        };
        if (saveDialog.ShowDialog(this) != true) return;

        _renderCts?.Cancel();
        using var cts = new CancellationTokenSource();
        _renderCts = cts;
        SetRenderingState(true, "Экспорт изображения...");
        try
        {
            BitmapSource bitmap = await RenderBitmapAsync(CaptureState("export"), options.ExportWidth,
                options.ExportHeight, options.SsaaFactor, cts.Token,
                new Progress<int>(value => RenderProgress.Value = value));
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            await using FileStream stream = File.Create(saveDialog.FileName);
            encoder.Save(stream);
            StatusText.Text = $"Сохранено: {saveDialog.FileName}";
        }
        catch (OperationCanceledException) { StatusText.Text = "Экспорт отменён"; }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Ошибка экспорта", MessageBoxButton.OK, MessageBoxImage.Error); }
        finally { SetRenderingState(false); }
    }

    private void RenderButton_OnClick(object sender, RoutedEventArgs e) => _ = RenderPreviewAsync();
    private void CancelButton_OnClick(object sender, RoutedEventArgs e) => _renderCts?.Cancel();
    private void ResetButton_OnClick(object sender, RoutedEventArgs e) => ResetView(true);

    private void ResetView(bool render)
    {
        _centerX = _definition.InitialCenterX;
        _centerY = _definition.InitialCenterY;
        _zoom = _definition.InitialZoom;
        _updatingControls = true;
        ZoomBox.Text = _zoom.ToString(CultureInfo.InvariantCulture);
        _updatingControls = false;
        if (render) ScheduleRender();
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
        MandelbrotState state;
        try { state = CaptureState("preview"); }
        catch (Exception ex) { StatusText.Text = ex.Message; return; }

        int width = Math.Max(1, (int)CanvasHost.ActualWidth);
        int height = Math.Max(1, (int)CanvasHost.ActualHeight);
        _renderCts?.Dispose();
        _renderCts = new CancellationTokenSource();
        CancellationToken token = _renderCts.Token;
        var stopwatch = Stopwatch.StartNew();
        SetRenderingState(true, "Рендеринг...");
        try
        {
            BitmapSource bitmap = await RenderBitmapAsync(state, width, height, SelectedPreviewSsaaFactor, token,
                new Progress<int>(value => RenderProgress.Value = value));
            token.ThrowIfCancellationRequested();
            CanvasImage.Source = bitmap;
            stopwatch.Stop();
            StatusText.Text = $"Готово за {stopwatch.Elapsed.TotalSeconds:F3} сек. Центр: {_centerX:G6}; {_centerY:G6}";
        }
        catch (OperationCanceledException) { StatusText.Text = "Рендер отменён"; }
        catch (Exception ex)
        {
            StatusText.Text = "Ошибка рендера";
            MessageBox.Show(this, ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally { SetRenderingState(false); }
    }

    private static async Task<BitmapSource> RenderBitmapAsync(MandelbrotState state, int width, int height,
        int ssaaFactor, CancellationToken token, IProgress<int>? progress)
    {
        int factor = Math.Clamp(ssaaFactor, 1, 4);
        int renderWidth = checked(width * factor);
        int renderHeight = checked(height * factor);
        int stride = checked(renderWidth * 4);
        byte[] buffer = new byte[checked(stride * renderHeight)];
        await Task.Run(() => MandelbrotFamilyRenderer.Render(state, buffer, renderWidth, renderHeight,
            stride, token, value => progress?.Report(value)), token);
        token.ThrowIfCancellationRequested();

        BitmapSource source = BitmapSource.Create(renderWidth, renderHeight, 96, 96,
            PixelFormats.Bgra32, null, buffer, stride);
        source.Freeze();
        if (factor == 1) return source;
        var scaled = new TransformedBitmap(source, new ScaleTransform(1.0 / factor, 1.0 / factor));
        scaled.Freeze();
        return scaled;
    }

    private void SetRenderingState(bool rendering, string? status = null)
    {
        _isRendering = rendering;
        CancelButton.IsEnabled = rendering;
        if (!rendering) RenderProgress.Value = 0;
        if (status is not null) StatusText.Text = status;
    }

    private int GetThreadCount() => ThreadsBox.SelectedItem?.ToString() == "Auto"
        ? 0
        : Math.Max(1, Convert.ToInt32(ThreadsBox.SelectedItem, CultureInfo.InvariantCulture));

    private int SelectedPreviewSsaaFactor => PreviewSsaaBox.SelectedItem is ComboBoxItem item &&
                                             int.TryParse(item.Tag?.ToString(), out int factor)
        ? factor
        : 1;

    private void CanvasHost_OnSizeChanged(object sender, SizeChangedEventArgs e) => ScheduleRender();

    private void CanvasHost_OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        Point mouse = e.GetPosition(CanvasHost);
        (decimal X, decimal Y) before = ScreenToWorld(mouse);
        _zoom = Math.Clamp(_zoom * (e.Delta > 0 ? 1.35m : 1m / 1.35m),
            0.000000000001m, 1000000000000000000000000000m);
        (decimal X, decimal Y) after = ScreenToWorld(mouse);
        _centerX += before.X - after.X;
        _centerY += before.Y - after.Y;
        SetZoomText();
        ScheduleRender();
        e.Handled = true;
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
        if (!_isPanning) return;
        Point current = e.GetPosition(CanvasHost);
        (decimal X, decimal Y) before = ScreenToWorld(_lastPanPoint);
        (decimal X, decimal Y) after = ScreenToWorld(current);
        _centerX += before.X - after.X;
        _centerY += before.Y - after.Y;
        _lastPanPoint = current;
    }

    private void CanvasHost_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isPanning) return;
        _isPanning = false;
        CanvasHost.ReleaseMouseCapture();
        Mouse.OverrideCursor = null;
        ScheduleRender();
    }

    private (decimal X, decimal Y) ScreenToWorld(Point point)
    {
        decimal width = (decimal)Math.Max(1, CanvasHost.ActualWidth);
        decimal height = (decimal)Math.Max(1, CanvasHost.ActualHeight);
        decimal viewWidth = 3m / _zoom;
        decimal viewHeight = viewWidth * height / width;
        return (_centerX + ((decimal)point.X / width - 0.5m) * viewWidth,
            _centerY + (0.5m - (decimal)point.Y / height) * viewHeight);
    }

    private void SetZoomText()
    {
        _updatingControls = true;
        ZoomBox.Text = _zoom.ToString("G8", CultureInfo.InvariantCulture);
        _updatingControls = false;
    }

    private void Window_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F11) ToggleFullscreen();
        else if (e.Key == Key.Escape && _isFullscreen) ToggleFullscreen();
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

    private static int ReadInt(string text, string name, int minimum, int maximum)
    {
        if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.CurrentCulture, out int value) || value < minimum || value > maximum)
            throw new InvalidOperationException($"Параметр «{name}» должен быть от {minimum} до {maximum}.");
        return value;
    }

    private static double ReadDouble(string text, string name, double minimum, double maximum)
    {
        if (!TryReadDouble(text, out double value) || !double.IsFinite(value) || value < minimum || value > maximum)
            throw new InvalidOperationException($"Параметр «{name}» должен быть от {minimum} до {maximum}.");
        return value;
    }

    private static bool TryReadDouble(string text, out double value) =>
        double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) ||
        double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value);

    private static decimal ReadDecimal(string text, string name, decimal minimum, decimal maximum)
    {
        if (!TryReadDecimal(text, out decimal value) || value < minimum || value > maximum)
            throw new InvalidOperationException($"Параметр «{name}» должен быть от {minimum} до {maximum}.");
        return value;
    }

    private static bool TryReadDecimal(string text, out decimal value) =>
        decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) ||
        decimal.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value);

    private static MandelbrotState CloneState(MandelbrotState source) => new()
    {
        SaveName = source.SaveName, Timestamp = source.Timestamp, Variant = source.Variant,
        CenterX = source.CenterX, CenterY = source.CenterY, Zoom = source.Zoom,
        Iterations = source.Iterations, Threshold = source.Threshold, Threads = source.Threads,
        ColoringMode = source.ColoringMode, PaletteName = source.PaletteName,
        Palette = source.Palette.Clone(source.Palette.Name), Power = source.Power,
        UseInversion = source.UseInversion, HistogramContrast = source.HistogramContrast,
        OrbitTrapStrength = source.OrbitTrapStrength, OrbitTrapBias = source.OrbitTrapBias,
        StripeFrequency = source.StripeFrequency, StripeStrength = source.StripeStrength,
        StripeBias = source.StripeBias, PolynomialA = source.PolynomialA,
        PolynomialB = source.PolynomialB, PolynomialC = source.PolynomialC,
        PolynomialGamma = source.PolynomialGamma, PolynomialBlend = source.PolynomialBlend,
        PolynomialBias = source.PolynomialBias
    };
}
