using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using FractalExplorerWPF.Core.Rendering;
using FractalExplorerWPF.Models;
using Point = System.Windows.Point;

namespace FractalExplorerWPF.Views;

public partial class PhoenixParameterSelectorWindow : Window
{
    private const int SliceIterations = 275;
    private readonly DispatcherTimer _pTimer = new() { Interval = TimeSpan.FromMilliseconds(300) };
    private readonly DispatcherTimer _qTimer = new() { Interval = TimeSpan.FromMilliseconds(300) };
    private readonly PhoenixSliceRange _pRange = new();
    private readonly PhoenixSliceRange _qRange = new();
    private readonly PhoenixSliceRange _renderedPRange = new();
    private readonly PhoenixSliceRange _renderedQRange = new();
    private bool _hasRenderedP;
    private bool _hasRenderedQ;
    private CancellationTokenSource? _pCts;
    private CancellationTokenSource? _qCts;
    private bool _updating;
    private bool _panning;
    private bool _panPSlice;
    private Point _panStart;
    private decimal _pService;
    private decimal _qService;

    public decimal SelectedC1Real { get; private set; }
    public decimal SelectedC1Imaginary { get; private set; }
    public decimal FixedC2Real { get; }
    public decimal FixedC2Imaginary { get; }

    public PhoenixParameterSelectorWindow(decimal c1Real, decimal c1Imaginary, decimal c2Real, decimal c2Imaginary)
    {
        SelectedC1Real = c1Real; SelectedC1Imaginary = c1Imaginary; FixedC2Real = c2Real; FixedC2Imaginary = c2Imaginary;
        InitializeComponent();
        _pTimer.Tick += (_, _) => { _pTimer.Stop(); _ = RenderSliceAsync(true); };
        _qTimer.Tick += (_, _) => { _qTimer.Stop(); _ = RenderSliceAsync(false); };
        SetText();
        AdvancedBox.IsChecked = false;
        UpdateAdvancedMode();
        UpdateLabels();
        Loaded += (_, _) => { ScheduleSlice(true); ScheduleSlice(false); };
    }

    private void SetText()
    {
        _updating = true;
        PBox.Text = SelectedC1Real.ToString("G15", CultureInfo.InvariantCulture);
        QBox.Text = SelectedC1Imaginary.ToString("G15", CultureInfo.InvariantCulture);
        PServiceBox.Text = _pService.ToString("G15", CultureInfo.InvariantCulture);
        QServiceBox.Text = _qService.ToString("G15", CultureInfo.InvariantCulture);
        _updating = false;
    }

    private void ParameterText_OnChanged(object sender, TextChangedEventArgs e)
    {
        if (_updating || !IsInitialized) return;
        decimal oldP = SelectedC1Real, oldQ = SelectedC1Imaginary;
        if (TryRead(PBox.Text, out decimal p)) SelectedC1Real = Math.Clamp(p, -2, 2);
        if (TryRead(QBox.Text, out decimal q)) SelectedC1Imaginary = Math.Clamp(q, -2, 2);
        if (TryRead(PServiceBox.Text, out decimal ps)) _pService = Math.Clamp(ps, -2, 2);
        if (TryRead(QServiceBox.Text, out decimal qs)) _qService = Math.Clamp(qs, -2, 2);
        if (oldQ != SelectedC1Imaginary || sender == PServiceBox) ScheduleSlice(true);
        if (oldP != SelectedC1Real || sender == QServiceBox) ScheduleSlice(false);
        UpdateLabels(); UpdateMarkers();
    }

    private void AdvancedBox_OnChanged(object sender, RoutedEventArgs e) => UpdateAdvancedMode();
    private void UpdateAdvancedMode()
    {
        if (PAdvancedPanel is null) return;
        Visibility visibility = AdvancedBox.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        PAdvancedPanel.Visibility = visibility; QAdvancedPanel.Visibility = visibility;
    }

    private void UpdateLabels()
    {
        if (PFixedText is null) return;
        PFixedText.Text = $"Q фиксировано: {SelectedC1Imaginary:G8}";
        QFixedText.Text = $"P фиксировано: {SelectedC1Real:G8}";
        FixedC2Text.Text = $"C2 фиксировано в селекторе: {FixedC2Real:G8} {(FixedC2Imaginary < 0 ? '−' : '+')} {Math.Abs(FixedC2Imaginary):G8}i";
        StatusText.Text = $"C1 = {SelectedC1Real:G8} {(SelectedC1Imaginary < 0 ? '−' : '+')} {Math.Abs(SelectedC1Imaginary):G8}i";
    }

    private void ScheduleSlice(bool pSlice)
    {
        if (!IsLoaded) return;
        CancellationTokenSource? cts = pSlice ? _pCts : _qCts;
        cts?.Cancel();
        DispatcherTimer timer = pSlice ? _pTimer : _qTimer;
        timer.Stop(); timer.Start();
    }

    private async Task RenderSliceAsync(bool pSlice)
    {
        Border host = pSlice ? PSliceHost : QSliceHost;
        int width = Math.Max(1, (int)host.ActualWidth), height = Math.Max(1, (int)host.ActualHeight);
        var cts = new CancellationTokenSource();
        if (pSlice) { _pCts?.Dispose(); _pCts = cts; } else { _qCts?.Dispose(); _qCts = cts; }
        ProgressBar progress = pSlice ? PProgress : QProgress;
        try
        {
            byte[] pixels = new byte[checked(width * height * 4)];
            PhoenixSliceRange currentRange = pSlice ? _pRange : _qRange;
            var range = new PhoenixSliceRange
            {
                MinX = currentRange.MinX,
                MaxX = currentRange.MaxX,
                MinY = currentRange.MinY,
                MaxY = currentRange.MaxY
            };
            decimal fixedValue = pSlice ? SelectedC1Imaginary : SelectedC1Real;
            await Task.Run(() => PhoenixRenderer.RenderSlice(pixels, width, height, width * 4, range, pSlice, fixedValue,
                SliceIterations, 4, Environment.ProcessorCount, cts.Token, value => Dispatcher.Invoke(() => progress.Value = value)), cts.Token);
            BitmapSource bitmap = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, pixels, width * 4);
            bitmap.Freeze();
            if (pSlice)
            {
                PSliceImage.Source = bitmap;
                CopyRange(range, _renderedPRange);
                _hasRenderedP = true;
            }
            else
            {
                QSliceImage.Source = bitmap;
                CopyRange(range, _renderedQRange);
                _hasRenderedQ = true;
            }
            UpdateSliceTransform(pSlice);
            UpdateMarkers();
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { StatusText.Text = $"Ошибка среза {(pSlice ? "P" : "Q")}: {ex.Message}"; }
        finally { progress.Value = 0; }
    }

    private bool IsPSlice(object sender) => sender is FrameworkElement { Tag: "P" };

    private void SliceHost_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_panning) return;
        bool pSlice = IsPSlice(sender);
        Border host = (Border)sender;
        PhoenixSliceRange range = pSlice ? _pRange : _qRange;
        Point point = e.GetPosition(host);
        decimal x = (decimal)(range.MinX + point.X / Math.Max(1, host.ActualWidth) * (range.MaxX - range.MinX));
        decimal y = (decimal)(range.MaxY - point.Y / Math.Max(1, host.ActualHeight) * (range.MaxY - range.MinY));
        if (pSlice) { SelectedC1Real = Math.Clamp(x, -2, 2); _pService = Math.Clamp(y, -2, 2); ScheduleSlice(false); }
        else { SelectedC1Imaginary = Math.Clamp(x, -2, 2); _qService = Math.Clamp(y, -2, 2); ScheduleSlice(true); }
        SetText(); UpdateLabels(); UpdateMarkers(); e.Handled = true;
    }

    private void SliceHost_OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        bool pSlice = IsPSlice(sender); Border host = (Border)sender; PhoenixSliceRange range = pSlice ? _pRange : _qRange;
        Point point = e.GetPosition(host); double fx = point.X / Math.Max(1, host.ActualWidth), fy = point.Y / Math.Max(1, host.ActualHeight);
        double mouseX = range.MinX + fx * (range.MaxX - range.MinX), mouseY = range.MaxY - fy * (range.MaxY - range.MinY);
        double factor = e.Delta > 0 ? 1.25 : 1 / 1.25;
        double width = (range.MaxX - range.MinX) / factor, height = (range.MaxY - range.MinY) / factor;
        if (width is < 1e-12 or > 1e4 || height is < 1e-12 or > 1e4) return;
        range.MinX = mouseX - fx * width; range.MaxX = range.MinX + width;
        range.MinY = mouseY - (1 - fy) * height; range.MaxY = range.MinY + height;
        UpdateMarkers(); UpdateSliceTransform(pSlice); ScheduleSlice(pSlice); e.Handled = true;
    }

    private void SliceHost_OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Middle) return;
        _panning = true; _panPSlice = IsPSlice(sender); _panStart = e.GetPosition((Border)sender);
        ((Border)sender).CaptureMouse(); Mouse.OverrideCursor = Cursors.SizeAll; e.Handled = true;
    }

    private void SliceHost_OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_panning || e.MiddleButton != MouseButtonState.Pressed || _panPSlice != IsPSlice(sender)) return;
        Border host = (Border)sender; Point current = e.GetPosition(host); PhoenixSliceRange range = _panPSlice ? _pRange : _qRange;
        double dx = (current.X - _panStart.X) * (range.MaxX - range.MinX) / Math.Max(1, host.ActualWidth);
        double dy = (current.Y - _panStart.Y) * (range.MaxY - range.MinY) / Math.Max(1, host.ActualHeight);
        range.MinX -= dx; range.MaxX -= dx; range.MinY += dy; range.MaxY += dy; _panStart = current;
        UpdateMarkers(); UpdateSliceTransform(_panPSlice); e.Handled = true;
    }

    private void SliceHost_OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Middle || !_panning) return;
        _panning = false; ((Border)sender).ReleaseMouseCapture(); Mouse.OverrideCursor = null; ScheduleSlice(IsPSlice(sender)); e.Handled = true;
    }

    private void SliceHost_OnSizeChanged(object sender, SizeChangedEventArgs e) { bool pSlice = IsPSlice(sender); UpdateMarkers(); UpdateSliceTransform(pSlice); ScheduleSlice(pSlice); }

    private void UpdateMarkers()
    {
        if (!IsInitialized) return;
        UpdateMarker(PSliceHost, PMarkerLayer, PMarkerHorizontal, PMarkerVertical, _pRange, (double)SelectedC1Real, (double)_pService);
        UpdateMarker(QSliceHost, QMarkerLayer, QMarkerHorizontal, QMarkerVertical, _qRange, (double)SelectedC1Imaginary, (double)_qService);
    }

    private static void UpdateMarker(Border host, Canvas layer, Line horizontal, Line vertical, PhoenixSliceRange range, double xValue, double yValue)
    {
        double width = Math.Max(1, host.ActualWidth), height = Math.Max(1, host.ActualHeight);
        double x = (xValue - range.MinX) / (range.MaxX - range.MinX) * width;
        double y = (range.MaxY - yValue) / (range.MaxY - range.MinY) * height;
        bool visible = x >= 0 && x <= width && y >= 0 && y <= height;
        layer.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        if (!visible) return;
        horizontal.X1 = 0; horizontal.X2 = width; horizontal.Y1 = y; horizontal.Y2 = y;
        vertical.X1 = x; vertical.X2 = x; vertical.Y1 = 0; vertical.Y2 = height;
    }

    private void Reset_OnClick(object sender, RoutedEventArgs e)
    {
        _pRange.Reset(); _qRange.Reset(); UpdateMarkers();
        UpdateSliceTransform(true); UpdateSliceTransform(false);
        ScheduleSlice(true); ScheduleSlice(false);
    }
    private void UpdateSliceTransform(bool pSlice)
    {
        bool hasFrame = pSlice ? _hasRenderedP : _hasRenderedQ;
        if (!hasFrame) return;
        System.Windows.Controls.Image image = pSlice ? PSliceImage : QSliceImage;
        Border host = pSlice ? PSliceHost : QSliceHost;
        PhoenixSliceRange rendered = pSlice ? _renderedPRange : _renderedQRange;
        PhoenixSliceRange current = pSlice ? _pRange : _qRange;
        double currentWidth = current.MaxX - current.MinX;
        double currentHeight = current.MaxY - current.MinY;
        if (currentWidth <= 0 || currentHeight <= 0 || host.ActualWidth <= 0 || host.ActualHeight <= 0) return;
        double scaleX = (rendered.MaxX - rendered.MinX) / currentWidth;
        double scaleY = (rendered.MaxY - rendered.MinY) / currentHeight;
        double offsetX = (rendered.MinX - current.MinX) / currentWidth * host.ActualWidth;
        double offsetY = (current.MaxY - rendered.MaxY) / currentHeight * host.ActualHeight;
        image.RenderTransformOrigin = new Point(0, 0);
        image.RenderTransform = new MatrixTransform(scaleX, 0, 0, scaleY, offsetX, offsetY);
    }
    private static void CopyRange(PhoenixSliceRange source, PhoenixSliceRange target)
    {
        target.MinX = source.MinX; target.MaxX = source.MaxX;
        target.MinY = source.MinY; target.MaxY = source.MaxY;
    }
    private void Apply_OnClick(object sender, RoutedEventArgs e)
    {
        if (!TryRead(PBox.Text, out decimal p) || !TryRead(QBox.Text, out decimal q) || p is < -2 or > 2 || q is < -2 or > 2)
        { MessageBox.Show(this, "P и Q должны находиться в диапазоне от −2 до 2.", "Параметры Phoenix", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        SelectedC1Real = p; SelectedC1Imaginary = q; DialogResult = true;
    }
    private static bool TryRead(string text, out decimal value) => decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) || decimal.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value);
    private void Window_OnClosing(object? sender, System.ComponentModel.CancelEventArgs e) { _pTimer.Stop(); _qTimer.Stop(); _pCts?.Cancel(); _qCts?.Cancel(); }
}
