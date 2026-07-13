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
using MediaColor = System.Windows.Media.Color;

namespace FractalExplorerWPF.Views;

public partial class MandelbrotWindow : Window
{
    private readonly MandelbrotVariantDefinition _definition;
    private readonly MandelbrotPaletteManager _paletteManager = new();
    private readonly MandelbrotSaveStore _saveStore;
    private readonly DispatcherTimer _renderTimer = new() { Interval = TimeSpan.FromMilliseconds(320) };
    private readonly DispatcherTimer _visualizationTimer = new() { Interval = TimeSpan.FromMilliseconds(33) };
    private CancellationTokenSource? _renderCts;
    private CancellationTokenSource? _juliaMapPreviewCts;
    private bool _isRendering;
    private bool _isPanning;
    private bool _isFullscreen;
    private bool _updatingControls;
    private bool _controlsVisible = true;
    private WindowStyle _previousWindowStyle;
    private WindowState _previousWindowState;
    private Point _lastPanPoint;
    private decimal _centerX;
    private decimal _centerY;
    private decimal _zoom;
    private BitmapSource? _stableBitmap;
    private decimal _renderedCenterX;
    private decimal _renderedCenterY;
    private decimal _renderedZoom;
    private int _stablePixelWidth;
    private int _stablePixelHeight;
    private RenderSession? _activeSession;

    internal string SaveManagerDisplayName => _definition.DisplayName;
    internal string SaveManagerIdentifier => _definition.Identifier;
    internal MandelbrotVariant SaveManagerVariant => _definition.Variant;

    public MandelbrotWindow(MandelbrotVariant variant, decimal? juliaReal = null, decimal? juliaImaginary = null)
    {
        _definition = MandelbrotVariantDefinition.For(variant);
        _saveStore = new MandelbrotSaveStore(variant);
        InitializeComponent();
        Title = _definition.DisplayName;
        HeaderText.Text = _definition.DisplayName;
        _renderTimer.Tick += RenderTimer_OnTick;
        _visualizationTimer.Tick += VisualizationTimer_OnTick;
        InitializeControls();
        if (_definition.HasJuliaConstant && juliaReal.HasValue && juliaImaginary.HasValue)
        {
            _updatingControls = true;
            JuliaRealBox.Text = juliaReal.Value.ToString(CultureInfo.InvariantCulture);
            JuliaImaginaryBox.Text = juliaImaginary.Value.ToString(CultureInfo.InvariantCulture);
            _updatingControls = false;
        }
        ResetView(false);
        Loaded += (_, _) =>
        {
            ScheduleRender();
            if (_definition.HasJuliaConstant) _ = RenderJuliaMapPreviewAsync();
        };
    }

    private void InitializeControls()
    {
        _updatingControls = true;
        IterationsBox.Text = "500";
        ThresholdBox.Text = "2";
        PowerBox.Text = _definition.DefaultPower.ToString(CultureInfo.InvariantCulture);
        PowerPanel.Visibility = _definition.HasPower ? Visibility.Visible : Visibility.Collapsed;
        InversionBox.Visibility = _definition.HasInversion ? Visibility.Visible : Visibility.Collapsed;
        JuliaConstantPanel.Visibility = _definition.HasJuliaConstant ? Visibility.Visible : Visibility.Collapsed;
        JuliaRealBox.Text = _definition.DefaultJuliaReal.ToString(CultureInfo.InvariantCulture);
        JuliaImaginaryBox.Text = _definition.DefaultJuliaImaginary.ToString(CultureInfo.InvariantCulture);

        for (int count = 1; count <= Environment.ProcessorCount; count++) ThreadsBox.Items.Add(count);
        ThreadsBox.Items.Add("Auto");
        ThreadsBox.SelectedItem = "Auto";
        ColoringModeBox.SelectedIndex = 1;
        SmoothBlendPowerBox.Text = "1";
        SmoothIterationOffsetBox.Text = "0";
        HistogramContrastBox.Text = "1";
        HistogramEqualizationBox.IsChecked = true;
        HistogramSmoothInputBox.IsChecked = true;
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
        PalettePhaseBox.Text = "0";
        PaletteScaleBox.Text = "1";
        PaletteWrapBox.SelectedIndex = 0;
        CustomInteriorBox.IsChecked = false;
        InteriorColorBox.Text = "#FF000000";
        _updatingControls = false;
    }

    public MandelbrotState CaptureState(string saveName)
    {
        int iterations = ReadInt(IterationsBox.Text, "итерации", 50, 100_000);
        decimal threshold = ReadDecimal(ThresholdBox.Text, "порог выхода", 0.1m, 1_000m);
        decimal power = _definition.HasPower
            ? ReadDecimal(PowerBox.Text, "степень", _definition.Variant == MandelbrotVariant.Simonobrot ? -12m : 0.1m, 12m)
            : 2m;
        if (_definition.Variant == MandelbrotVariant.Simonobrot && Math.Abs(power) < 0.1m)
            throw new InvalidOperationException("Для Симоноброта модуль степени должен быть не меньше 0.1.");
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
            JuliaCReal = _definition.HasJuliaConstant
                ? ReadDecimal(JuliaRealBox.Text, "действительная часть C", -10m, 10m)
                : 0m,
            JuliaCImaginary = _definition.HasJuliaConstant
                ? ReadDecimal(JuliaImaginaryBox.Text, "мнимая часть C", -10m, 10m)
                : 0m,
            HistogramContrast = ReadDouble(HistogramContrastBox.Text, "контраст", 0.1, 4),
            HistogramEnabledEqualization = HistogramEqualizationBox.IsChecked == true,
            HistogramInputUseSmooth = HistogramSmoothInputBox.IsChecked == true,
            SmoothBlendPower = ReadDouble(SmoothBlendPowerBox.Text, "степень смешивания", 0.1, 5),
            SmoothIterationOffset = ReadDouble(SmoothIterationOffsetBox.Text, "сдвиг итерации", -100, 100),
            PalettePhaseOffset = ReadDouble(PalettePhaseBox.Text, "фаза палитры", -2, 2),
            PaletteScale = ReadDouble(PaletteScaleBox.Text, "масштаб палитры", -5, 5),
            PaletteWrapMode = PaletteWrapBox.SelectedIndex < 0
                ? MandelbrotPaletteWrapMode.Repeat
                : (MandelbrotPaletteWrapMode)PaletteWrapBox.SelectedIndex,
            UseCustomInteriorColor = CustomInteriorBox.IsChecked == true,
            InteriorColor = CustomInteriorBox.IsChecked == true
                ? ParseColor(InteriorColorBox.Text, "цвет внутренней области")
                : palette.InteriorColor,
            OrbitTrapStrength = ReadDouble(OrbitStrengthBox.Text, "сила ловушки", 0, 5),
            OrbitTrapBias = ReadDouble(OrbitBiasBox.Text, "смещение ловушки", -1, 1),
            StripeFrequency = ReadDouble(StripeFrequencyBox.Text, "частота полос", 0.1, 20),
            StripeStrength = ReadDouble(StripeStrengthBox.Text, "сила полос", 0, 1),
            StripeBias = ReadDouble(StripeBiasBox.Text, "смещение полос", -1, 1),
            PolynomialA = ReadDouble(PolyABox.Text, "коэффициент A", 0, 30),
            PolynomialB = ReadDouble(PolyBBox.Text, "коэффициент B", 0, 30),
            PolynomialC = ReadDouble(PolyCBox.Text, "коэффициент C", 0, 30),
            PolynomialGamma = ReadDouble(PolyGammaBox.Text, "гамма полинома", 0.1, 5),
            PolynomialBlend = ReadDouble(PolyBlendBox.Text, "смешивание полинома", 0, 1),
            PolynomialBias = ReadDouble(PolyBiasBox.Text, "смещение полинома", -1, 1)
        };
    }

    public void LoadState(MandelbrotState state)
    {
        if (state.Variant != _definition.Variant) return;
        _renderCts?.Cancel();
        _updatingControls = true;
        _centerX = state.CenterX;
        _centerY = state.CenterY;
        _zoom = Math.Clamp(state.Zoom, 0.01m, 1000000000000000000000000000m);
        IterationsBox.Text = state.Iterations.ToString(CultureInfo.InvariantCulture);
        ThresholdBox.Text = state.Threshold.ToString(CultureInfo.InvariantCulture);
        ZoomBox.Text = _zoom.ToString("G8", CultureInfo.InvariantCulture);
        PowerBox.Text = state.Power.ToString(CultureInfo.InvariantCulture);
        InversionBox.IsChecked = state.UseInversion;
        JuliaRealBox.Text = state.JuliaCReal.ToString(CultureInfo.InvariantCulture);
        JuliaImaginaryBox.Text = state.JuliaCImaginary.ToString(CultureInfo.InvariantCulture);
        ColoringModeBox.SelectedIndex = (int)state.ColoringMode;
        HistogramContrastBox.Text = state.HistogramContrast.ToString(CultureInfo.InvariantCulture);
        HistogramEqualizationBox.IsChecked = state.HistogramEnabledEqualization;
        HistogramSmoothInputBox.IsChecked = state.HistogramInputUseSmooth;
        SmoothBlendPowerBox.Text = state.SmoothBlendPower.ToString(CultureInfo.InvariantCulture);
        SmoothIterationOffsetBox.Text = state.SmoothIterationOffset.ToString(CultureInfo.InvariantCulture);
        PalettePhaseBox.Text = state.PalettePhaseOffset.ToString(CultureInfo.InvariantCulture);
        PaletteScaleBox.Text = state.PaletteScale.ToString(CultureInfo.InvariantCulture);
        PaletteWrapBox.SelectedIndex = (int)state.PaletteWrapMode;
        CustomInteriorBox.IsChecked = state.UseCustomInteriorColor;
        InteriorColorBox.Text = ToHex(state.InteriorColor);
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
        UpdateJuliaMapMarker();
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
        if (!_updatingControls && IsLoaded)
        {
            UpdateJuliaMapMarker();
            ScheduleRender();
        }
    }

    private void ColoringMode_OnChanged(object sender, EventArgs e) => Parameter_OnChanged(sender, e);

    private void ZoomBox_OnTextChanged(object sender, EventArgs e)
    {
        if (!_updatingControls && TryReadDecimal(ZoomBox.Text, out decimal zoom) && zoom > 0)
        {
            _zoom = Math.Clamp(zoom, 0.01m, 1000000000000000000000000000m);
            ScheduleRender();
        }
    }

    private void PaletteButton_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new MandelbrotPaletteWindow(_paletteManager) { Owner = this };
        dialog.PaletteApplied += (_, _) => ScheduleRender();
        dialog.ShowDialog();
    }

    private void JuliaConstantButton_OnClick(object sender, RoutedEventArgs e)
    {
        decimal real;
        decimal imaginary;
        try
        {
            real = ReadDecimal(JuliaRealBox.Text, "действительная часть C", -10m, 10m);
            imaginary = ReadDecimal(JuliaImaginaryBox.Text, "мнимая часть C", -10m, 10m);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Параметры", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        MandelbrotVariant sourceVariant = _definition.Variant == MandelbrotVariant.JuliaBurningShip
            ? MandelbrotVariant.BurningShip
            : MandelbrotVariant.Mandelbrot;
        var dialog = new JuliaConstantPickerWindow(sourceVariant, real, imaginary) { Owner = this };
        if (dialog.ShowDialog() != true) return;
        _updatingControls = true;
        JuliaRealBox.Text = dialog.SelectedReal.ToString(CultureInfo.InvariantCulture);
        JuliaImaginaryBox.Text = dialog.SelectedImaginary.ToString(CultureInfo.InvariantCulture);
        _updatingControls = false;
        UpdateJuliaMapMarker();
        ScheduleRender();
    }

    private void JuliaMapPreviewHost_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        JuliaConstantButton_OnClick(sender, e);
        e.Handled = true;
    }

    private void JuliaMapPreviewHost_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateJuliaMapMarker();
        if (IsLoaded && _definition.HasJuliaConstant) _ = RenderJuliaMapPreviewAsync();
    }

    private async Task RenderJuliaMapPreviewAsync()
    {
        if (!_definition.HasJuliaConstant || JuliaMapPreviewHost.ActualWidth < 1 ||
            JuliaMapPreviewHost.ActualHeight < 1) return;

        _juliaMapPreviewCts?.Cancel();
        _juliaMapPreviewCts?.Dispose();
        var cts = new CancellationTokenSource();
        _juliaMapPreviewCts = cts;
        RenderSurfaceMetrics mapSurface = RenderSurfaceMetrics.Measure(JuliaMapPreviewHost);
        int width = mapSurface.PixelWidth;
        int height = mapSurface.PixelHeight;
        (MandelbrotVariant variant, decimal centerX, decimal centerY, decimal zoom) = GetJuliaMapView();
        var state = new MandelbrotState
        {
            Variant = variant,
            CenterX = centerX,
            CenterY = centerY,
            Zoom = zoom,
            Iterations = 110,
            Threshold = 2,
            Threads = 0,
            ColoringMode = MandelbrotColoringMode.Smooth,
            Palette = new MandelbrotPalette
            {
                Name = "Карта выбора C",
                Colors =
                [
                    Colors.Black,
                    MediaColor.FromRgb(200, 50, 30),
                    Colors.White
                ],
                InteriorColor = Colors.Black,
                IsGradient = true,
                ColorPeriod = 110,
                AlignWithRenderIterations = true
            }
        };

        try
        {
            BitmapSource bitmap = await RenderBitmapAsync(state, width, height, 1, cts.Token, null,
                mapSurface.Dpi.PixelsPerInchX, mapSurface.Dpi.PixelsPerInchY);
            if (!ReferenceEquals(_juliaMapPreviewCts, cts)) return;
            JuliaMapPreviewImage.Source = bitmap;
            UpdateJuliaMapMarker();
        }
        catch (OperationCanceledException) { }
        finally
        {
            if (ReferenceEquals(_juliaMapPreviewCts, cts)) _juliaMapPreviewCts = null;
            cts.Dispose();
        }
    }

    private (MandelbrotVariant Variant, decimal CenterX, decimal CenterY, decimal Zoom) GetJuliaMapView() =>
        _definition.Variant == MandelbrotVariant.JuliaBurningShip
            ? (MandelbrotVariant.BurningShip, -0.25m, 0.25m, 3m / 3.5m)
            : (MandelbrotVariant.Mandelbrot, -0.5m, 0m, 1m);

    private void UpdateJuliaMapMarker()
    {
        if (!_definition.HasJuliaConstant || !IsInitialized ||
            !TryReadDecimal(JuliaRealBox.Text, out decimal real) ||
            !TryReadDecimal(JuliaImaginaryBox.Text, out decimal imaginary)) return;

        double width = Math.Max(1, JuliaMapPreviewHost.ActualWidth);
        double height = Math.Max(1, JuliaMapPreviewHost.ActualHeight);
        (_, decimal centerX, decimal centerY, decimal zoom) = GetJuliaMapView();
        decimal viewWidth = 3m / zoom;
        decimal viewHeight = viewWidth * (decimal)height / (decimal)width;
        double x = (double)((real - (centerX - viewWidth / 2m)) / viewWidth) * width;
        double y = (double)(((centerY + viewHeight / 2m) - imaginary) / viewHeight) * height;
        bool visible = x >= 0 && x <= width && y >= 0 && y <= height;
        JuliaMapMarkerLayer.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        if (!visible) return;
        JuliaMapHorizontalMarker.X1 = 0;
        JuliaMapHorizontalMarker.X2 = width;
        JuliaMapHorizontalMarker.Y1 = y;
        JuliaMapHorizontalMarker.Y2 = y;
        JuliaMapVerticalMarker.X1 = x;
        JuliaMapVerticalMarker.X2 = x;
        JuliaMapVerticalMarker.Y1 = 0;
        JuliaMapVerticalMarker.Y2 = height;
    }

    private void SavesButton_OnClick(object sender, RoutedEventArgs e) =>
        SaveManagerWindow.Open(this, SaveManagerConfigurations.ForMandelbrot(this, _saveStore));

    private async void ExportButton_OnClick(object sender, RoutedEventArgs e)
    {
        (int canvasPixelWidth, int canvasPixelHeight, _, _) = GetCanvasPixelSize();
        var options = new MandelbrotExportWindow
        {
            Owner = this,
            ExportWidth = canvasPixelWidth,
            ExportHeight = canvasPixelHeight
        };
        if (options.ShowDialog() != true) return;

        string extension = options.ExportFormat switch
        {
            MandelbrotExportFormat.Jpeg => ".jpg",
            MandelbrotExportFormat.Bmp => ".bmp",
            _ => ".png"
        };
        var saveDialog = new SaveFileDialog
        {
            Filter = options.ExportFormat switch
            {
                MandelbrotExportFormat.Jpeg => "JPEG image|*.jpg;*.jpeg",
                MandelbrotExportFormat.Bmp => "Bitmap image|*.bmp",
                _ => "PNG image|*.png"
            },
            DefaultExt = extension,
            AddExtension = true,
            FileName = $"{_definition.Identifier}_{DateTime.Now:yyyyMMdd_HHmmss}{extension}"
        };
        if (saveDialog.ShowDialog(this) != true) return;

        if (_activeSession is not null) CommitAndBakePreview();
        else _renderCts?.Cancel();
        using var cts = new CancellationTokenSource();
        _renderCts = cts;
        SetRenderingState(true, "Экспорт изображения...");
        try
        {
            BitmapSource bitmap = await RenderBitmapAsync(CaptureState("export"), options.RenderWidth,
                options.RenderHeight, options.SsaaFactor, cts.Token,
                new Progress<int>(value => RenderProgress.Value = value));
            if (bitmap.PixelWidth != options.ExportWidth || bitmap.PixelHeight != options.ExportHeight)
            {
                StatusText.Text = options.ProcessingMode == MandelbrotExportProcessingMode.Lanczos
                    ? "Масштабирование фильтром Ланцоша 3..."
                    : "Бикубическое масштабирование...";
                bitmap = options.ProcessingMode == MandelbrotExportProcessingMode.Lanczos
                    ? await Task.Run(() => BitmapResampler.ResizeLanczos3(bitmap,
                        options.ExportWidth, options.ExportHeight, cts.Token,
                        value => Dispatcher.Invoke(() => RenderProgress.Value = value)), cts.Token)
                    : BitmapResampler.ResizeBicubic(bitmap, options.ExportWidth, options.ExportHeight);
            }

            BitmapEncoder encoder = options.ExportFormat switch
            {
                MandelbrotExportFormat.Jpeg => new JpegBitmapEncoder { QualityLevel = options.JpegQuality },
                MandelbrotExportFormat.Bmp => new BmpBitmapEncoder(),
                _ => new PngBitmapEncoder()
            };
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            await using FileStream stream = File.Create(saveDialog.FileName);
            encoder.Save(stream);
            StatusText.Text = $"Сохранено: {saveDialog.FileName}";
        }
        catch (OperationCanceledException) { StatusText.Text = "Экспорт отменён"; }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Ошибка экспорта", MessageBoxButton.OK, MessageBoxImage.Error); }
        finally
        {
            if (ReferenceEquals(_renderCts, cts))
            {
                _renderCts = null;
                SetRenderingState(false);
            }
        }
    }

    private void RenderButton_OnClick(object sender, RoutedEventArgs e) => _ = RenderPreviewAsync();
    private void CancelButton_OnClick(object sender, RoutedEventArgs e) => _renderCts?.Cancel();
    private void ResetButton_OnClick(object sender, RoutedEventArgs e) => ResetView(true);

    private void ToggleControlsButton_OnClick(object sender, RoutedEventArgs e)
    {
        FractalControlPanel.Toggle(ref _controlsVisible, ParametersColumn, ParametersBorder,
            ToggleControlsButton, 278);
        UpdateCoarsePreviewTransform();
        ScheduleRender();
    }

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
        if (_isRendering) CommitAndBakePreview();
        else UpdateCoarsePreviewTransform();
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

        RenderSurfaceMetrics surface = RenderSurfaceMetrics.Measure(CanvasHost);
        int logicalWidth = Math.Max(1, (int)Math.Round(surface.LogicalWidth));
        int logicalHeight = Math.Max(1, (int)Math.Round(surface.LogicalHeight));
        int pixelWidth = surface.PixelWidth;
        int pixelHeight = surface.PixelHeight;
        _renderCts?.Dispose();
        var cts = new CancellationTokenSource();
        _renderCts = cts;
        CancellationToken token = cts.Token;
        var stopwatch = Stopwatch.StartNew();
        TileSchedulingStrategy strategy = RenderPatternSettings.SelectedPattern;
        SetRenderingState(true, $"Рендеринг: {GetStrategyDisplayName(strategy)}...");
        try
        {
            int factor = SelectedPreviewSsaaFactor;
            int renderWidth = checked(pixelWidth * factor);
            int renderHeight = checked(pixelHeight * factor);
            IReadOnlyList<MandelbrotRenderTile> tiles = MandelbrotTileScheduler.Create(
                renderWidth, renderHeight, 16 * factor, strategy);
            WriteableBitmap bitmap = ProgressiveRenderBitmap.CreateOverlay(renderWidth, renderHeight,
                surface.Dpi.PixelsPerInchX, surface.Dpi.PixelsPerInchY);
            var session = new RenderSession(bitmap, tiles.Count, renderWidth, renderHeight, cts);
            _activeSession = session;
            CanvasImage.Source = bitmap;
            RenderOverlay.BeginSession(renderWidth, renderHeight);
            _visualizationTimer.Start();

            await RenderTilesAsync(state, tiles, session, token);
            if (token.IsCancellationRequested)
            {
                if (_activeSession is not null) CommitAndBakePreview();
                if (ReferenceEquals(_renderCts, cts)) StatusText.Text = "Рендер отменён";
                return;
            }
            FlushVisualizationEvents(session, true);
            BitmapSource completed = session.Bitmap.Clone();
            completed.Freeze();
            SetStableBitmap(completed, state.CenterX, state.CenterY, state.Zoom, logicalWidth, logicalHeight);
            CanvasImage.Source = null;
            RenderOverlay.EndSession();
            if (ReferenceEquals(_activeSession, session)) _activeSession = null;
            stopwatch.Stop();
            StatusText.Text = $"Готово за {stopwatch.Elapsed.TotalSeconds:F3} сек. " +
                              $"Стратегия: {GetStrategyDisplayName(strategy)}. Центр: {_centerX:G6}; {_centerY:G6}";
        }
        catch (OperationCanceledException)
        {
            if (_activeSession is not null) CommitAndBakePreview();
            if (ReferenceEquals(_renderCts, cts)) StatusText.Text = "Рендер отменён";
        }
        catch (Exception ex)
        {
            if (_activeSession is not null) CommitAndBakePreview();
            if (ReferenceEquals(_renderCts, cts)) StatusText.Text = "Ошибка рендера";
            MessageBox.Show(this, ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _visualizationTimer.Stop();
            RenderOverlay.EndSession();
            if (ReferenceEquals(_renderCts, cts))
            {
                _renderCts = null;
                SetRenderingState(false);
            }
            cts.Dispose();
        }
    }

    private async Task RenderTilesAsync(
        MandelbrotState state,
        IReadOnlyList<MandelbrotRenderTile> tiles,
        RenderSession session,
        CancellationToken token)
    {
        var queue = new ConcurrentQueue<MandelbrotRenderTile>(tiles);
        int workerCount = state.Threads <= 0 ? Environment.ProcessorCount : state.Threads;
        workerCount = Math.Clamp(workerCount, 1, Environment.ProcessorCount);
        Task[] workers = Enumerable.Range(0, workerCount).Select(_ => Task.Run(() =>
        {
            while (queue.TryDequeue(out MandelbrotRenderTile tile))
            {
                if (token.IsCancellationRequested) return;
                session.Events.Enqueue(new TileRenderEvent(true, tile, null));
                byte[]? pixels = MandelbrotFamilyRenderer.RenderTile(
                    state, session.RenderWidth, session.RenderHeight, tile, token);
                if (pixels is null || token.IsCancellationRequested) return;
                session.Events.Enqueue(new TileRenderEvent(false, tile, pixels));
            }
        })).ToArray();
        await Task.WhenAll(workers);
    }

    private void VisualizationTimer_OnTick(object? sender, EventArgs e)
    {
        if (_activeSession is not null) FlushVisualizationEvents(_activeSession, false);
    }

    private void FlushVisualizationEvents(RenderSession session, bool drainAll)
    {
        int processed = 0;
        bool changed = false;
        while ((drainAll || processed < 512) && session.Events.TryDequeue(out TileRenderEvent visualEvent))
        {
            if (visualEvent.IsStart)
            {
                RenderOverlay.StartTile(visualEvent.Tile);
            }
            else if (visualEvent.Pixels is not null)
            {
                MandelbrotRenderTile tile = visualEvent.Tile;
                if (ProgressiveRenderBitmap.WriteTile(session.Bitmap, tile, visualEvent.Pixels))
                {
                    RenderOverlay.CompleteTile(tile);
                    session.CompletedTiles++;
                }
            }
            processed++;
            changed = true;
        }
        if (changed)
        {
            RenderOverlay.Refresh();
            RenderProgress.Value = session.TileCount == 0
                ? 0
                : Math.Min(100, session.CompletedTiles * 100.0 / session.TileCount);
        }
    }

    private void SetStableBitmap(
        BitmapSource bitmap,
        decimal centerX,
        decimal centerY,
        decimal zoom,
        int pixelWidth,
        int pixelHeight)
    {
        _stableBitmap = bitmap;
        _renderedCenterX = centerX;
        _renderedCenterY = centerY;
        _renderedZoom = zoom;
        _stablePixelWidth = Math.Max(1, pixelWidth);
        _stablePixelHeight = Math.Max(1, pixelHeight);
        StablePreviewImage.Source = bitmap;
        StablePreviewImage.RenderTransform = Transform.Identity;
        RenderOptions.SetBitmapScalingMode(StablePreviewImage, BitmapScalingMode.HighQuality);
    }

    private void CommitAndBakePreview()
    {
        RenderSession? session = _activeSession;
        if (session is null) return;

        session.Cancellation.Cancel();
        FlushVisualizationEvents(session, true);
        RenderSurfaceMetrics surface = RenderSurfaceMetrics.Measure(ImageLayer);
        int logicalWidth = Math.Max(1, (int)Math.Round(surface.LogicalWidth));
        int logicalHeight = Math.Max(1, (int)Math.Round(surface.LogicalHeight));
        DpiScale dpi = surface.Dpi;
        int pixelWidth = surface.PixelWidth;
        int pixelHeight = surface.PixelHeight;
        try
        {
            var baked = new RenderTargetBitmap(pixelWidth, pixelHeight, dpi.PixelsPerInchX,
                dpi.PixelsPerInchY, PixelFormats.Pbgra32);
            baked.Render(ImageLayer);
            baked.Freeze();
            SetStableBitmap(baked, _centerX, _centerY, _zoom, logicalWidth, logicalHeight);
        }
        catch (InvalidOperationException)
        {
            // Layout can briefly be unavailable during minimization or a resize transition.
        }
        CanvasImage.Source = null;
        RenderOverlay.EndSession();
        if (ReferenceEquals(_activeSession, session)) _activeSession = null;
    }

    private void UpdateCoarsePreviewTransform()
    {
        if (_stableBitmap is null || _renderedZoom <= 0 || _zoom <= 0 ||
            _stablePixelWidth <= 0 || _stablePixelHeight <= 0) return;

        decimal width = (decimal)Math.Max(1, ImageLayer.ActualWidth);
        decimal height = (decimal)Math.Max(1, ImageLayer.ActualHeight);
        decimal renderedViewWidth = 3m / _renderedZoom;
        decimal currentViewWidth = 3m / _zoom;
        decimal renderedUnitsPerPixel = renderedViewWidth / _stablePixelWidth;
        decimal currentUnitsPerPixel = currentViewWidth / width;
        if (renderedUnitsPerPixel <= 0 || currentUnitsPerPixel <= 0) return;

        decimal renderedLeft = _renderedCenterX - renderedViewWidth / 2m;
        decimal renderedTop = _renderedCenterY + _stablePixelHeight * renderedUnitsPerPixel / 2m;
        decimal currentLeft = _centerX - currentViewWidth / 2m;
        decimal currentTop = _centerY + height * currentUnitsPerPixel / 2m;
        decimal offsetX = (renderedLeft - currentLeft) / currentUnitsPerPixel;
        decimal offsetY = (currentTop - renderedTop) / currentUnitsPerPixel;
        decimal destinationWidth = _stablePixelWidth * renderedUnitsPerPixel / currentUnitsPerPixel;
        decimal destinationHeight = _stablePixelHeight * renderedUnitsPerPixel / currentUnitsPerPixel;

        StablePreviewImage.RenderTransform = new MatrixTransform(new Matrix(
            (double)(destinationWidth / width), 0,
            0, (double)(destinationHeight / height),
            (double)offsetX, (double)offsetY));
        RenderOptions.SetBitmapScalingMode(StablePreviewImage, BitmapScalingMode.LowQuality);
    }

    private static string GetStrategyDisplayName(TileSchedulingStrategy strategy) => strategy switch
    {
        TileSchedulingStrategy.Classic => "от центра",
        TileSchedulingStrategy.Linear => "построчно",
        TileSchedulingStrategy.Spiral => "спираль",
        TileSchedulingStrategy.Randomized => "случайно",
        TileSchedulingStrategy.Checkerboard => "шахматный",
        TileSchedulingStrategy.Diagonal => "по диагонали",
        TileSchedulingStrategy.EdgesInward => "от краёв",
        TileSchedulingStrategy.MortonCurve => "Z-кривая Мортона",
        _ => strategy.ToString()
    };

    private (int PixelWidth, int PixelHeight, double DpiScaleX, double DpiScaleY) GetCanvasPixelSize()
    {
        RenderSurfaceMetrics surface = RenderSurfaceMetrics.Measure(CanvasHost);
        return (surface.PixelWidth, surface.PixelHeight, surface.Dpi.DpiScaleX, surface.Dpi.DpiScaleY);
    }

    private static async Task<BitmapSource> RenderBitmapAsync(MandelbrotState state, int width, int height,
        int ssaaFactor, CancellationToken token, IProgress<int>? progress,
        double dpiX = 96, double dpiY = 96)
    {
        int factor = Math.Clamp(ssaaFactor, 1, 10);
        int renderWidth = checked(width * factor);
        int renderHeight = checked(height * factor);
        int stride = checked(renderWidth * 4);
        byte[] buffer = new byte[checked(stride * renderHeight)];
        await Task.Run(() => MandelbrotFamilyRenderer.Render(state, buffer, renderWidth, renderHeight,
            stride, token, value => progress?.Report(value)), token);
        token.ThrowIfCancellationRequested();

        if (factor == 1)
        {
            BitmapSource source = BitmapSource.Create(renderWidth, renderHeight, dpiX, dpiY,
                PixelFormats.Bgra32, null, buffer, stride);
            source.Freeze();
            return source;
        }

        byte[] downsampled = await Task.Run(() => DownsampleBox(
            buffer, renderWidth, width, height, factor, token), token);
        BitmapSource result = BitmapSource.Create(width, height, dpiX, dpiY,
            PixelFormats.Bgra32, null, downsampled, width * 4);
        result.Freeze();
        return result;
    }

    private static byte[] DownsampleBox(
        byte[] source,
        int sourceWidth,
        int targetWidth,
        int targetHeight,
        int factor,
        CancellationToken token)
    {
        var result = new byte[checked(targetWidth * targetHeight * 4)];
        int sampleCount = factor * factor;
        var options = new ParallelOptions
        {
            CancellationToken = token,
            MaxDegreeOfParallelism = Environment.ProcessorCount
        };
        Parallel.For(0, targetHeight, options, y =>
        {
            for (int x = 0; x < targetWidth; x++)
            {
                int blue = 0, green = 0, red = 0;
                for (int sampleY = 0; sampleY < factor; sampleY++)
                {
                    int sourceOffset = ((y * factor + sampleY) * sourceWidth + x * factor) * 4;
                    for (int sampleX = 0; sampleX < factor; sampleX++)
                    {
                        blue += source[sourceOffset];
                        green += source[sourceOffset + 1];
                        red += source[sourceOffset + 2];
                        sourceOffset += 4;
                    }
                }
                int targetOffset = (y * targetWidth + x) * 4;
                result[targetOffset] = (byte)(blue / sampleCount);
                result[targetOffset + 1] = (byte)(green / sampleCount);
                result[targetOffset + 2] = (byte)(red / sampleCount);
                result[targetOffset + 3] = 255;
            }
        });
        return result;
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

    private void CanvasHost_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_isRendering) CommitAndBakePreview();
        UpdateCoarsePreviewTransform();
        ScheduleRender();
    }

    private void CanvasHost_OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        CommitAndBakePreview();
        Point mouse = e.GetPosition(CanvasHost);
        (decimal X, decimal Y) before = ScreenToWorld(mouse);
        _zoom = Math.Clamp(_zoom * (e.Delta > 0 ? 1.5m : 1m / 1.5m),
            0.01m, 1000000000000000000000000000m);
        (decimal X, decimal Y) after = ScreenToWorld(mouse);
        _centerX += before.X - after.X;
        _centerY += before.Y - after.Y;
        SetZoomText();
        UpdateCoarsePreviewTransform();
        ScheduleRender();
        e.Handled = true;
    }

    private void CanvasHost_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        CommitAndBakePreview();
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
        UpdateCoarsePreviewTransform();
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
        _visualizationTimer.Stop();
        _renderCts?.Cancel();
        _renderCts?.Dispose();
        _juliaMapPreviewCts?.Cancel();
        _juliaMapPreviewCts?.Dispose();
        _activeSession = null;
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

    private static MediaColor ParseColor(string value, string name)
    {
        value = value.Trim();
        if (value.Length == 7 && value[0] == '#') value = "#FF" + value[1..];
        if (value.Length == 9 && value[0] == '#' &&
            byte.TryParse(value.AsSpan(1, 2), NumberStyles.HexNumber, null, out byte a) &&
            byte.TryParse(value.AsSpan(3, 2), NumberStyles.HexNumber, null, out byte r) &&
            byte.TryParse(value.AsSpan(5, 2), NumberStyles.HexNumber, null, out byte g) &&
            byte.TryParse(value.AsSpan(7, 2), NumberStyles.HexNumber, null, out byte b))
            return MediaColor.FromArgb(a, r, g, b);
        throw new InvalidOperationException($"Параметр «{name}» должен иметь формат #AARRGGBB.");
    }

    private static string ToHex(MediaColor color) => $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";

    private static MandelbrotState CloneState(MandelbrotState source) => new()
    {
        SaveName = source.SaveName, Timestamp = source.Timestamp, Variant = source.Variant,
        CenterX = source.CenterX, CenterY = source.CenterY, Zoom = source.Zoom,
        Iterations = source.Iterations, Threshold = source.Threshold, Threads = source.Threads,
        ColoringMode = source.ColoringMode, PaletteName = source.PaletteName,
        Palette = source.Palette.Clone(source.Palette.Name), Power = source.Power,
        UseInversion = source.UseInversion, HistogramContrast = source.HistogramContrast,
        JuliaCReal = source.JuliaCReal, JuliaCImaginary = source.JuliaCImaginary,
        HistogramEnabledEqualization = source.HistogramEnabledEqualization,
        HistogramInputUseSmooth = source.HistogramInputUseSmooth,
        SmoothBlendPower = source.SmoothBlendPower, SmoothIterationOffset = source.SmoothIterationOffset,
        PalettePhaseOffset = source.PalettePhaseOffset, PaletteScale = source.PaletteScale,
        PaletteWrapMode = source.PaletteWrapMode, UseCustomInteriorColor = source.UseCustomInteriorColor,
        InteriorColor = source.InteriorColor,
        OrbitTrapStrength = source.OrbitTrapStrength, OrbitTrapBias = source.OrbitTrapBias,
        StripeFrequency = source.StripeFrequency, StripeStrength = source.StripeStrength,
        StripeBias = source.StripeBias, PolynomialA = source.PolynomialA,
        PolynomialB = source.PolynomialB, PolynomialC = source.PolynomialC,
        PolynomialGamma = source.PolynomialGamma, PolynomialBlend = source.PolynomialBlend,
        PolynomialBias = source.PolynomialBias
    };

    private sealed class RenderSession(
        WriteableBitmap bitmap,
        int tileCount,
        int renderWidth,
        int renderHeight,
        CancellationTokenSource cancellation)
    {
        public WriteableBitmap Bitmap { get; } = bitmap;
        public int TileCount { get; } = tileCount;
        public int RenderWidth { get; } = renderWidth;
        public int RenderHeight { get; } = renderHeight;
        public CancellationTokenSource Cancellation { get; } = cancellation;
        public int CompletedTiles { get; set; }
        public ConcurrentQueue<TileRenderEvent> Events { get; } = new();
    }

    private readonly record struct TileRenderEvent(
        bool IsStart,
        MandelbrotRenderTile Tile,
        byte[]? Pixels);
}
