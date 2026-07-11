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
using FractalExplorerWPF.Infrastructure;
using FractalExplorerWPF.Models;
using Microsoft.Win32;
using Point = System.Windows.Point;

namespace FractalExplorerWPF.Views;

public partial class NewtonPoolsWindow : Window
{
    private const double BaseScale = 3.0;
    private readonly DispatcherTimer _renderTimer = new() { Interval = TimeSpan.FromMilliseconds(350) };
    private readonly DispatcherTimer _visualizationTimer = new() { Interval = TimeSpan.FromMilliseconds(33) };
    private readonly NewtonPoolsEngine _formulaEngine = new();
    private readonly NewtonPaletteManager _paletteManager = new();
    private readonly NewtonSaveStore _saveStore = new();
    private readonly Random _random = new();
    private CancellationTokenSource? _renderCts;
    private bool _isRendering;
    private bool _isPanning;
    private bool _isFullscreen;
    private bool _controlsVisible = true;
    private WindowStyle _previousWindowStyle;
    private WindowState _previousWindowState;
    private Point _lastPanPoint;
    private double _centerX;
    private double _centerY;
    private double _zoom = 1;
    private double _renderedCenterX;
    private double _renderedCenterY;
    private double _renderedZoom = 1;
    private bool _hasRenderedFrame;
    private readonly TransformGroup _previewTransform = new();
    private readonly ScaleTransform _previewScale = new(1, 1);
    private readonly TranslateTransform _previewTranslation = new();
    private string _appliedFormula = "z^3-1";
    private RenderSession? _activeSession;

    private readonly string[] _presetFormulas =
    [
        "z^3-1", "z^4-1", "z^5-1", "z^6-1", "z^10-1", "z^3-2*z+2",
        "z^5-z^2+1", "z^6+3*z^3-2", "z^4-4*z^2+4", "z^7+z^4-z+1",
        "z^8+15*z^4-16", "z^4+z^3+z^2+z+1", "z^2-i", "(z^2-1)*(z-2*i)",
        "(1+2*i)*z^2+z-1", "0.5*z^3-1.25*z+2", "(2+i)*z^3-(1-2*i)*z+1",
        "i*z^4+z-1", "(1+0.5*i)*z^2-z+(2-3*i)", "(0.3+1.7*i)*z^3+(1-i)",
        "(2-i)*z^5+(3+2*i)*z^2-1", "-2*z^3+0.75*z^2-1", "z^6-1.5*z^3+0.25",
        "-0.1*z^4+z-2", "(1/2)*z^3+(3/4)*z-1", "(2+3*i)*(z^2)-(1-i)*z+4",
        "(z^2-1)/(z^2+1)", "(z^3-1)/(z^3+1)", "z^2/(z-1)^2",
        "(z^4-1)/(z*z-2*z+1)", "(z-1)*(z+1)*(z-0.2)*(z+0.2)",
        "(z-1)^2*(z+1)", "z^7-0.5*z^3+1", "z^5+(0.3-0.8*i)*z^2-(1.2+0.1*i)",
        "(z-1)*(z+1)*(z-0.1)*(z+0.1)*(z-0.01)", "(z-1)*(z-0.3)*(z+0.7)*(z-0.05)",
        "(z-1)^2*(z+1)*(z-0.2)"
    ];

    public NewtonPoolsWindow()
    {
        InitializeComponent();
        _previewTransform.Children.Add(_previewScale);
        _previewTransform.Children.Add(_previewTranslation);
        StablePreviewImage.RenderTransformOrigin = new Point(0.5, 0.5);
        StablePreviewImage.RenderTransform = _previewTransform;
        _visualizationTimer.Tick += (_, _) =>
        {
            if (_activeSession is not null) FlushVisualizationEvents(_activeSession, false);
        };
        _renderTimer.Tick += RenderTimer_OnTick;
        FormulaPresetBox.ItemsSource = _presetFormulas;
        FormulaPresetBox.SelectedIndex = 0;
        FormulaBox.Text = _appliedFormula;
        MethodBox.SelectedIndex = 0;
        for (int count = 1; count <= Environment.ProcessorCount; count++) ThreadsBox.Items.Add(count);
        ThreadsBox.Items.Add("Auto");
        ThreadsBox.SelectedItem = "Auto";
        ApplyFormula(showMessage: false);
        UpdateMethodControls();
        Loaded += (_, _) => ScheduleRender();
    }

    public NewtonState CaptureState(string saveName)
    {
        if (!int.TryParse(IterationsBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int iterations) || iterations is < 1 or > 100_000)
            throw new InvalidOperationException("Итерации должны быть целым числом от 1 до 100000.");
        int order = ReadHouseholderOrder();
        NewtonColorPalette palette = _paletteManager.ActivePalette.Clone(_paletteManager.ActivePalette.Name);
        return new NewtonState
        {
            SaveName = saveName,
            Timestamp = DateTime.Now,
            Formula = _appliedFormula,
            MaxIterations = iterations,
            Zoom = _zoom,
            CenterX = _centerX,
            CenterY = _centerY,
            IterationMethod = SelectedMethod,
            HouseholderOrder = order,
            Palette = palette
        };
    }

    public void LoadState(NewtonState state)
    {
        _renderCts?.Cancel();
        FormulaPresetBox.SelectedIndex = -1;
        FormulaBox.Text = state.Formula;
        IterationsBox.Text = state.MaxIterations.ToString(CultureInfo.InvariantCulture);
        _zoom = Math.Clamp(state.Zoom, 0.001, 1_000_000_000_000);
        _centerX = state.CenterX;
        _centerY = state.CenterY;
        ZoomBox.Text = _zoom.ToString("0.####", CultureInfo.InvariantCulture);
        MethodBox.SelectedIndex = (int)state.IterationMethod;
        HouseholderOrderBox.Text = Math.Clamp(state.HouseholderOrder, 2, 12).ToString(CultureInfo.InvariantCulture);
        _paletteManager.ActivePalette = state.Palette.Clone($"Загружено: {state.SaveName}");
        UpdatePreviewTransform();
        if (ApplyFormula(showMessage: true)) ScheduleRender();
    }

    public Task<BitmapSource> RenderStatePreviewAsync(NewtonState state, int width, int height, CancellationToken token) =>
        RenderBitmapAsync(state, width, height, 1, token, null);

    private NewtonIterationMethod SelectedMethod => MethodBox.SelectedIndex switch
    {
        1 => NewtonIterationMethod.Halley,
        2 => NewtonIterationMethod.Householder,
        _ => NewtonIterationMethod.Newton
    };

    private void FormulaPresetBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded || FormulaPresetBox.SelectedItem is not string formula) return;
        FormulaBox.Text = formula;
        ApplyFormula(showMessage: true);
    }

    private void ApplyFormulaButton_OnClick(object sender, RoutedEventArgs e) => ApplyFormula(showMessage: true);

    private bool ApplyFormula(bool showMessage)
    {
        _formulaEngine.HouseholderOrder = ReadHouseholderOrder();
        if (!_formulaEngine.SetFormula(FormulaBox.Text.Trim(), out string debug))
        {
            DebugOutput.Text = debug;
            RootCountText.Text = "Найдено корней: 0";
            StatusText.Text = "Ошибка формулы";
            if (showMessage) MessageBox.Show(this, "Проверьте формулу: есть синтаксическая ошибка.", "Ошибка формулы", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
        _appliedFormula = FormulaBox.Text.Trim();
        DebugOutput.Text = debug;
        RootCountText.Text = $"Найдено корней: {_formulaEngine.Roots.Count}";
        StatusText.Text = _formulaEngine.Roots.Count == 0 ? "Формула корректна, но корни не найдены." : "Формула применена";
        ScheduleRender();
        return true;
    }

    private void RandomFormulaButton_OnClick(object sender, RoutedEventArgs e)
    {
        FormulaPresetBox.SelectedIndex = -1;
        FormulaBox.Text = GenerateValidRandomFormula();
        ApplyFormula(showMessage: true);
    }

    private string GenerateValidRandomFormula()
    {
        for (int attempt = 0; attempt < 20; attempt++)
        {
            string formula = GenerateRandomFormula();
            var engine = new NewtonPoolsEngine();
            if (engine.SetFormula(formula, out _)) return formula;
        }
        return _presetFormulas[_random.Next(_presetFormulas.Length)];
    }

    private string GenerateRandomFormula() => _random.Next(4) switch
    {
        0 => GenerateCyclotomicFormula(), 1 => GenerateSparseFormula(), 2 => GenerateFactoredFormula(), _ => GeneratePerturbedFormula()
    };

    private string GenerateCyclotomicFormula()
    {
        int degree = _random.Next(3, 11);
        string coefficient = RandomComplexCoefficient();
        return coefficient == "1" ? $"z^{degree} {RandomSignedConstant()}" : $"{coefficient}*z^{degree} {RandomSignedConstant()}";
    }

    private string GenerateSparseFormula()
    {
        int high = _random.Next(4, 9);
        int low = _random.Next(1, high - 1);
        return $"z^{high} {RandomSign()} {RandomRealCoefficient(false)}*z^{low} {RandomSignedConstant()}";
    }

    private string GenerateFactoredFormula()
    {
        string[] offsets = ["-1", "+1", "-0.5", "+0.5", "-0.25", "+0.25", "-i", "+i", "-0.5*i", "+0.5*i", "-(1+i)", "+(1-i)"];
        var terms = Enumerable.Range(0, _random.Next(3, 6)).Select(_ => $"(z{offsets[_random.Next(offsets.Length)]})").ToList();
        if (_random.NextDouble() < 0.35) terms.Add(terms[_random.Next(terms.Count)]);
        return string.Join("*", terms);
    }

    private string GeneratePerturbedFormula()
    {
        int degree = _random.Next(3, 8);
        return $"z^{degree} {RandomSign()} {RandomComplexCoefficient()}*z^{_random.Next(1, degree)} {RandomSignedConstant()}";
    }

    private string RandomComplexCoefficient()
    {
        if (_random.NextDouble() < 0.45) return RandomRealCoefficient(true);
        return $"({RandomRealCoefficient(false)}{(_random.Next(2) == 0 ? "+" : "-")}{RandomRealCoefficient(false)}*i)";
    }

    private string RandomRealCoefficient(bool allowOne)
    {
        string[] values = allowOne
            ? ["1", "0.25", "0.5", "0.75", "1.25", "1.5", "2", "3"]
            : ["0.25", "0.5", "0.75", "1.25", "1.5", "2", "3"];
        return values[_random.Next(values.Length)];
    }

    private string RandomSignedConstant()
    {
        string[] values = ["- 1", "+ 1", "- 2", "+ 2", "- 0.5", "+ 0.5", "- i", "+ i", "- (1+i)", "+ (1-i)"];
        return values[_random.Next(values.Length)];
    }

    private string RandomSign() => _random.Next(2) == 0 ? "+" : "-";

    private void MethodBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateMethodControls();
        ScheduleRender();
    }

    private void UpdateMethodControls()
    {
        if (HouseholderOrderPanel is not null) HouseholderOrderPanel.IsEnabled = SelectedMethod == NewtonIterationMethod.Householder;
    }

    private void Parameter_OnChanged(object sender, EventArgs e) => ScheduleRender();

    private void ZoomBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (TryReadDouble(ZoomBox.Text, out double zoom))
        {
            _zoom = Math.Clamp(zoom, 0.001, 1_000_000_000_000);
            UpdatePreviewTransform();
            ScheduleRender();
        }
    }

    private void RenderButton_OnClick(object sender, RoutedEventArgs e) => _ = RenderPreviewAsync();
    private void CancelButton_OnClick(object sender, RoutedEventArgs e) => _renderCts?.Cancel();

    private void PaletteButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (!_formulaEngine.SetFormula(_appliedFormula, out _))
        {
            MessageBox.Show(this, "Сначала примените корректную формулу.", "Палитра", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var dialog = new NewtonPaletteWindow(_paletteManager, _formulaEngine.Roots) { Owner = this };
        dialog.PaletteApplied += (_, _) => ScheduleRender();
        dialog.ShowDialog();
    }

    private void SavesButton_OnClick(object sender, RoutedEventArgs e) =>
        new NewtonSavesWindow(this, _saveStore) { Owner = this }.ShowDialog();

    private async void ExportButton_OnClick(object sender, RoutedEventArgs e)
    {
        DpiScale dpi = VisualTreeHelper.GetDpi(CanvasHost);
        var options = new NewtonExportWindow
        {
            Owner = this,
            ExportWidth = Math.Max(1, (int)Math.Ceiling(CanvasHost.ActualWidth * dpi.DpiScaleX)),
            ExportHeight = Math.Max(1, (int)Math.Ceiling(CanvasHost.ActualHeight * dpi.DpiScaleY))
        };
        if (options.ShowDialog() != true) return;
        var saveDialog = new SaveFileDialog { Filter = "PNG image|*.png", FileName = $"newton_pools_{DateTime.Now:yyyyMMdd_HHmmss}.png" };
        if (saveDialog.ShowDialog(this) != true) return;

        _renderCts?.Cancel();
        _renderCts?.Dispose();
        _renderCts = new CancellationTokenSource();
        CancellationToken token = _renderCts.Token;
        SetRenderingState(true, "Экспорт изображения...");
        try
        {
            NewtonState state = CaptureState("export");
            BitmapSource bitmap = await RenderBitmapAsync(state, options.ExportWidth, options.ExportHeight, options.SsaaFactor, token,
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
        if (_isRendering)
        {
            ScheduleRender();
            return;
        }

        NewtonState state;
        try { state = CaptureState("preview"); }
        catch (Exception ex) { StatusText.Text = ex.Message; return; }

        _renderCts?.Dispose();
        _renderCts = new CancellationTokenSource();
        CancellationToken token = _renderCts.Token;
        var stopwatch = Stopwatch.StartNew();
        SetRenderingState(true, $"Рендеринг методом {state.IterationMethod}...");
        try
        {
            DpiScale dpi = VisualTreeHelper.GetDpi(CanvasHost);
            int renderWidth = Math.Max(1, (int)Math.Ceiling(CanvasHost.ActualWidth * dpi.DpiScaleX));
            int renderHeight = Math.Max(1, (int)Math.Ceiling(CanvasHost.ActualHeight * dpi.DpiScaleY));
            TileSchedulingStrategy strategy = RenderPatternSettings.SelectedPattern;
            IReadOnlyList<MandelbrotRenderTile> tiles = MandelbrotTileScheduler.Create(renderWidth, renderHeight, 16, strategy);
            var bitmap = new WriteableBitmap(renderWidth, renderHeight, dpi.PixelsPerInchX, dpi.PixelsPerInchY,
                PixelFormats.Bgra32, null);
            NewtonPoolsEngine engine = CreateEngine(state);
            var session = new RenderSession(bitmap, tiles.Count, renderWidth, renderHeight);
            _activeSession = session;
            CanvasImage.Source = bitmap;
            RenderOverlay.BeginSession(renderWidth, renderHeight);
            _visualizationTimer.Start();

            await RenderTilesAsync(engine, tiles, session, GetThreadCount(), token);
            token.ThrowIfCancellationRequested();
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
            StatusText.Text = $"Готово за {stopwatch.Elapsed.TotalSeconds:F3} сек. Корней: {_formulaEngine.Roots.Count}. Стратегия: {strategy}";
        }
        catch (OperationCanceledException)
        {
            CanvasImage.Source = null;
            StatusText.Text = "Рендер отменён";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Ошибка рендера";
            MessageBox.Show(this, ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _visualizationTimer.Stop();
            RenderOverlay.EndSession();
            _activeSession = null;
            SetRenderingState(false);
        }
    }

    private static async Task RenderTilesAsync(NewtonPoolsEngine engine, IReadOnlyList<MandelbrotRenderTile> tiles,
        RenderSession session, int threadCount, CancellationToken token)
    {
        var queue = new ConcurrentQueue<MandelbrotRenderTile>(tiles);
        Task[] workers = Enumerable.Range(0, Math.Clamp(threadCount, 1, Environment.ProcessorCount)).Select(_ => Task.Run(() =>
        {
            while (queue.TryDequeue(out MandelbrotRenderTile tile))
            {
                token.ThrowIfCancellationRequested();
                session.Events.Enqueue(new TileRenderEvent(true, tile, null));
                byte[] pixels = engine.RenderTile(tile, session.RenderWidth, session.RenderHeight, token);
                session.Events.Enqueue(new TileRenderEvent(false, tile, pixels));
            }
        }, token)).ToArray();
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
                session.Bitmap.WritePixels(new Int32Rect(entry.Tile.X, entry.Tile.Y, entry.Tile.Width, entry.Tile.Height),
                    entry.Pixels, entry.Tile.Width * 4, 0);
                RenderOverlay.CompleteTile(entry.Tile);
                session.CompletedTiles++;
            }
            processed++;
            changed = true;
        }
        if (!changed) return;
        RenderOverlay.Refresh();
        RenderProgress.Value = session.TileCount == 0 ? 0 : session.CompletedTiles * 100.0 / session.TileCount;
    }

    private async Task<BitmapSource> RenderBitmapAsync(NewtonState state, int width, int height, int ssaaFactor,
        CancellationToken token, IProgress<int>? progress)
    {
        int factor = Math.Clamp(ssaaFactor, 1, 4);
        int renderWidth = checked(width * factor);
        int renderHeight = checked(height * factor);
        int stride = checked(renderWidth * 4);
        byte[] buffer = new byte[checked(stride * renderHeight)];
        NewtonPoolsEngine engine = CreateEngine(state);
        int threads = GetThreadCount();
        await Task.Run(() => engine.RenderToBuffer(buffer, renderWidth, renderHeight, stride, threads, token,
            value => progress?.Report(factor == 1 ? value : value * 90 / 100)), token);
        token.ThrowIfCancellationRequested();

        BitmapSource source = BitmapSource.Create(renderWidth, renderHeight, 96, 96, PixelFormats.Bgra32, null, buffer, stride);
        source.Freeze();
        if (factor == 1) return source;
        return await Task.Run(() => BitmapResampler.ResizeLanczos3(source, width, height, token, value => progress?.Report(value)), token);
    }

    private static NewtonPoolsEngine CreateEngine(NewtonState state)
    {
        var engine = new NewtonPoolsEngine
        {
            MaxIterations = state.MaxIterations,
            CenterX = state.CenterX,
            CenterY = state.CenterY,
            Scale = BaseScale / Math.Max(0.001, state.Zoom),
            IterationMethod = state.IterationMethod,
            HouseholderOrder = state.HouseholderOrder,
            BackgroundColor = state.Palette.BackgroundColor,
            UseGradient = state.Palette.IsGradient
        };
        if (!engine.SetFormula(state.Formula, out string debug)) throw new InvalidOperationException(debug);
        engine.RootColors = NewtonPaletteManager.AdjustColors(state.Palette, engine.Roots.Count).ToArray();
        return engine;
    }

    private int ReadHouseholderOrder() => int.TryParse(HouseholderOrderBox.Text, out int order) ? Math.Clamp(order, 2, 12) : 3;
    private int GetThreadCount() => ThreadsBox.SelectedItem?.ToString() == "Auto" ? Environment.ProcessorCount : Math.Max(1, Convert.ToInt32(ThreadsBox.SelectedItem, CultureInfo.InvariantCulture));

    private void SetRenderingState(bool rendering, string? status = null)
    {
        _isRendering = rendering;
        CancelButton.IsEnabled = rendering;
        if (!rendering) RenderProgress.Value = 0;
        if (status is not null) StatusText.Text = status;
    }

    private void CanvasHost_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdatePreviewTransform();
        ScheduleRender();
    }

    private void CanvasHost_OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        Point mouse = e.GetPosition(CanvasHost);
        Point before = ScreenToWorld(mouse);
        _zoom = Math.Clamp(_zoom * (e.Delta > 0 ? 1.2 : 1 / 1.2), 0.001, 1_000_000_000_000);
        Point after = ScreenToWorld(mouse);
        _centerX += before.X - after.X;
        _centerY += before.Y - after.Y;
        UpdatePreviewTransform();
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
        if (!_isPanning) return;
        Point current = e.GetPosition(CanvasHost);
        Point before = ScreenToWorld(_lastPanPoint);
        Point after = ScreenToWorld(current);
        _centerX += before.X - after.X;
        _centerY += before.Y - after.Y;
        _lastPanPoint = current;
        UpdatePreviewTransform();
    }

    private void CanvasHost_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isPanning) return;
        _isPanning = false;
        CanvasHost.ReleaseMouseCapture();
        Mouse.OverrideCursor = null;
        ScheduleRender();
    }

    private Point ScreenToWorld(Point point)
    {
        double width = Math.Max(1, CanvasHost.ActualWidth);
        double scale = BaseScale / _zoom;
        return new Point(_centerX + (point.X - width / 2) * scale / width,
            _centerY + (point.Y - Math.Max(1, CanvasHost.ActualHeight) / 2) * scale / width);
    }

    private void UpdatePreviewTransform()
    {
        if (!_hasRenderedFrame || _renderedZoom <= 0 || _zoom <= 0 || CanvasHost.ActualWidth <= 0) return;
        double scale = _zoom / _renderedZoom;
        double currentScale = BaseScale / _zoom;
        double width = CanvasHost.ActualWidth;
        _previewScale.ScaleX = scale;
        _previewScale.ScaleY = scale;
        _previewTranslation.X = (_renderedCenterX - _centerX) / currentScale * width;
        _previewTranslation.Y = (_centerY - _renderedCenterY) / currentScale * width;
    }

    private void ToggleControlsButton_OnClick(object sender, RoutedEventArgs e)
    {
        _controlsVisible = !_controlsVisible;
        ControlsColumn.Width = _controlsVisible ? new GridLength(290) : new GridLength(0);
        ControlsHost.Visibility = _controlsVisible ? Visibility.Visible : Visibility.Collapsed;
        ToggleControlsButton.Content = _controlsVisible ? "✕" : "☰";
        ScheduleRender();
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
        _visualizationTimer.Stop();
        _renderCts?.Cancel();
        _renderCts?.Dispose();
    }

    private static bool TryReadDouble(string text, out double value) =>
        double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) ||
        double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value);

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
