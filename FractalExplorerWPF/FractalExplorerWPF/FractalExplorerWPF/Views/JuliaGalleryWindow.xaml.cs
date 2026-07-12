using System.Globalization;
using System.IO;
using System.Windows;
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

public partial class JuliaGalleryWindow : Window
{
    private readonly MandelbrotVariant _variant;
    private readonly MandelbrotPaletteManager _paletteManager = new();
    private readonly DispatcherTimer _renderTimer = new() { Interval = TimeSpan.FromMilliseconds(420) };
    private CancellationTokenSource? _renderCts;
    private BitmapSource? _galleryBitmap;
    private List<GalleryCell> _cells = [];
    private int _canvasWidth;
    private int _canvasHeight;
    private bool _initializing = true;

    public JuliaGalleryWindow(MandelbrotVariant variant)
    {
        if (variant is not (MandelbrotVariant.Julia or MandelbrotVariant.JuliaBurningShip))
            throw new ArgumentOutOfRangeException(nameof(variant));
        _variant = variant;
        InitializeComponent();
        Title = variant == MandelbrotVariant.JuliaBurningShip
            ? "Галерея констант C — Горящий Корабль (Жюлиа)"
            : "Галерея констант C — Жюлиа";
        HeaderText.Text = Title;
        InitializeControls();
        _renderTimer.Tick += RenderTimer_OnTick;
        _initializing = false;
        Loaded += (_, _) => ScheduleRender();
    }

    private void InitializeControls()
    {
        bool burningShip = _variant == MandelbrotVariant.JuliaBurningShip;
        RealMinBox.Text = "-2";
        RealMaxBox.Text = burningShip ? "1.5" : "1";
        ImaginaryMinBox.Text = burningShip ? "-1" : "-1.2";
        ImaginaryMaxBox.Text = burningShip ? "1.5" : "1.2";
        ColumnsBox.Text = "5";
        RowsBox.Text = "4";
        TileSizeBox.Text = "180";
        IterationsBox.Text = "180";
    }

    private void Parameter_OnChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (!_initializing && IsLoaded) ScheduleRender();
    }

    private void ScheduleRender()
    {
        _renderTimer.Stop();
        _renderTimer.Start();
        StatusText.Text = "Параметры изменены. Ожидание автоперерендера...";
    }

    private async void RenderTimer_OnTick(object? sender, EventArgs e)
    {
        _renderTimer.Stop();
        await RenderGalleryAsync();
    }

    private async Task RenderGalleryAsync()
    {
        GalleryParameters parameters;
        try
        {
            parameters = ReadParameters();
        }
        catch (Exception ex)
        {
            StatusText.Text = ex.Message;
            return;
        }

        _renderCts?.Cancel();
        _renderCts?.Dispose();
        var cts = new CancellationTokenSource();
        _renderCts = cts;
        CancelButton.IsEnabled = true;
        RenderProgress.Value = 0;
        EmptyHint.Visibility = Visibility.Visible;
        EmptyHint.Text = "Рендер галереи...";
        StatusText.Text = "Рендер галереи...";

        try
        {
            int width = checked(parameters.Columns * parameters.TileSize);
            int height = checked(parameters.Rows * parameters.TileSize);
            List<GalleryCell> cells = OrderCellsForRendering(BuildCells(parameters), width, height, parameters.TileSize);
            long bytesRequired = (long)width * height * 4;
            if (bytesRequired > 512L * 1024 * 1024)
                throw new InvalidOperationException("Размер галереи слишком велик. Уменьшите сетку или размер ячейки.");

            byte[] output = new byte[checked((int)bytesRequired)];
            MandelbrotPalette palette = _paletteManager.ActivePalette.Clone(_paletteManager.ActivePalette.Name);
            int completed = 0;
            WriteableBitmap liveBitmap = ProgressiveRenderBitmap.CreateSeededOrOpaque(
                width, height, 96, 96, GalleryImage.Source as BitmapSource);
            GalleryImage.Source = liveBitmap;
            EmptyHint.Visibility = Visibility.Collapsed;
            IProgress<GalleryTileResult> progress = new Progress<GalleryTileResult>(result =>
            {
                var tile = new MandelbrotRenderTile(
                    result.Cell.Column * parameters.TileSize,
                    result.Cell.Row * parameters.TileSize,
                    parameters.TileSize,
                    parameters.TileSize,
                    result.Cell.Column,
                    result.Cell.Row);
                ProgressiveRenderBitmap.WriteTile(liveBitmap, tile, result.Pixels);
                RenderProgress.Value = result.Completed * 100.0 / cells.Count;
                StatusText.Text = $"Отрисовано {result.Completed}/{cells.Count} ячеек";
            });

            await Task.Run(async () =>
            {
                var options = new ParallelOptions
                {
                    CancellationToken = cts.Token,
                    MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1)
                };
                await Parallel.ForEachAsync(cells, options, (cell, token) =>
                {
                    var state = new MandelbrotState
                    {
                        Variant = _variant,
                        CenterX = 0,
                        CenterY = 0,
                        Zoom = 0.75m,
                        JuliaCReal = cell.Real,
                        JuliaCImaginary = cell.Imaginary,
                        Iterations = parameters.Iterations,
                        Threshold = 2,
                        Threads = 1,
                        ColoringMode = MandelbrotColoringMode.Smooth,
                        PaletteName = palette.Name,
                        Palette = palette
                    };
                    int tileStride = parameters.TileSize * 4;
                    byte[] tile = new byte[checked(tileStride * parameters.TileSize)];
                    MandelbrotFamilyRenderer.Render(state, tile, parameters.TileSize, parameters.TileSize,
                        tileStride, token);
                    for (int y = 0; y < parameters.TileSize; y++)
                    {
                        int sourceOffset = y * tileStride;
                        int targetOffset = ((cell.Row * parameters.TileSize + y) * width +
                                            cell.Column * parameters.TileSize) * 4;
                        Buffer.BlockCopy(tile, sourceOffset, output, targetOffset, tileStride);
                    }
                    progress.Report(new GalleryTileResult(cell, tile, Interlocked.Increment(ref completed)));
                    return ValueTask.CompletedTask;
                });
            }, cts.Token);

            cts.Token.ThrowIfCancellationRequested();
            BitmapSource bitmap = BitmapSource.Create(width, height, 96, 96,
                PixelFormats.Bgra32, null, output, width * 4);
            bitmap.Freeze();
            _cells = cells;
            _canvasWidth = width;
            _canvasHeight = height;
            _galleryBitmap = bitmap;
            GalleryImage.Source = bitmap;
            EmptyHint.Visibility = Visibility.Collapsed;
            StatusText.Text = $"Готово. Полотно {width}×{height}px, ячеек: {cells.Count}.";
        }
        catch (OperationCanceledException)
        {
            StatusText.Text = "Рендер отменён";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Ошибка рендера";
            MessageBox.Show(this, ex.Message, "Галерея Жюлиа", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            if (ReferenceEquals(_renderCts, cts)) _renderCts = null;
            CancelButton.IsEnabled = false;
            cts.Dispose();
        }
    }

    private GalleryParameters ReadParameters()
    {
        decimal realMin = ReadDecimal(RealMinBox.Text, "минимум Re(C)", -10, 10);
        decimal realMax = ReadDecimal(RealMaxBox.Text, "максимум Re(C)", -10, 10);
        decimal imaginaryMin = ReadDecimal(ImaginaryMinBox.Text, "минимум Im(C)", -10, 10);
        decimal imaginaryMax = ReadDecimal(ImaginaryMaxBox.Text, "максимум Im(C)", -10, 10);
        if (realMin >= realMax || imaginaryMin >= imaginaryMax)
            throw new InvalidOperationException("Минимальные границы должны быть меньше максимальных.");
        return new GalleryParameters(
            realMin, realMax, imaginaryMin, imaginaryMax,
            ReadInt(ColumnsBox.Text, "столбцы", 1, 20),
            ReadInt(RowsBox.Text, "строки", 1, 20),
            ReadInt(TileSizeBox.Text, "размер ячейки", 48, 640),
            ReadInt(IterationsBox.Text, "итерации", 30, 5000));
    }

    private static List<GalleryCell> BuildCells(GalleryParameters parameters)
    {
        decimal realStep = (parameters.RealMax - parameters.RealMin) / parameters.Columns;
        decimal imaginaryStep = (parameters.ImaginaryMax - parameters.ImaginaryMin) / parameters.Rows;
        var cells = new List<GalleryCell>(parameters.Columns * parameters.Rows);
        for (int row = 0; row < parameters.Rows; row++)
        for (int column = 0; column < parameters.Columns; column++)
            cells.Add(new GalleryCell(row, column,
                parameters.RealMin + (column + 0.5m) * realStep,
                parameters.ImaginaryMax - (row + 0.5m) * imaginaryStep));
        return cells;
    }

    private static List<GalleryCell> OrderCellsForRendering(
        List<GalleryCell> cells, int width, int height, int tileSize)
    {
        Dictionary<(int Column, int Row), GalleryCell> lookup =
            cells.ToDictionary(cell => (cell.Column, cell.Row));
        return MandelbrotTileScheduler.Create(width, height, tileSize, RenderPatternSettings.SelectedPattern)
            .Select(tile => lookup[(tile.Column, tile.Row)])
            .ToList();
    }

    private void GalleryImage_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (OpenOnClickBox.IsChecked != true || _galleryBitmap is null || _cells.Count == 0) return;
        Point point = e.GetPosition(GalleryImage);
        double controlWidth = Math.Max(1, GalleryImage.ActualWidth);
        double controlHeight = Math.Max(1, GalleryImage.ActualHeight);
        double scale = Math.Min(controlWidth / _canvasWidth, controlHeight / _canvasHeight);
        double drawnWidth = _canvasWidth * scale;
        double drawnHeight = _canvasHeight * scale;
        double left = (controlWidth - drawnWidth) / 2;
        double top = (controlHeight - drawnHeight) / 2;
        if (point.X < left || point.X >= left + drawnWidth || point.Y < top || point.Y >= top + drawnHeight) return;
        int x = Math.Clamp((int)((point.X - left) / scale), 0, _canvasWidth - 1);
        int y = Math.Clamp((int)((point.Y - top) / scale), 0, _canvasHeight - 1);
        int tileSize = _canvasWidth / Math.Max(1, _cells.Max(cell => cell.Column) + 1);
        int column = x / tileSize;
        int row = y / tileSize;
        GalleryCell? cell = _cells.FirstOrDefault(candidate => candidate.Row == row && candidate.Column == column);
        if (cell is null) return;
        new MandelbrotWindow(_variant, cell.Real, cell.Imaginary) { Owner = this }.Show();
    }

    private void Palette_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new MandelbrotPaletteWindow(_paletteManager) { Owner = this };
        dialog.PaletteApplied += (_, _) => ScheduleRender();
        dialog.ShowDialog();
    }

    private void Export_OnClick(object sender, RoutedEventArgs e)
    {
        if (_galleryBitmap is null)
        {
            MessageBox.Show(this, "Сначала постройте галерею.", "Экспорт", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var dialog = new SaveFileDialog
        {
            Filter = "PNG image|*.png|JPEG image|*.jpg;*.jpeg|Bitmap image|*.bmp",
            DefaultExt = ".png",
            AddExtension = true,
            FileName = $"{(_variant == MandelbrotVariant.JuliaBurningShip ? "julia_burningship" : "julia")}_gallery_{DateTime.Now:yyyyMMdd_HHmmss}.png"
        };
        if (dialog.ShowDialog(this) != true) return;
        BitmapEncoder encoder = Path.GetExtension(dialog.FileName).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => new JpegBitmapEncoder { QualityLevel = 95 },
            ".bmp" => new BmpBitmapEncoder(),
            _ => new PngBitmapEncoder()
        };
        encoder.Frames.Add(BitmapFrame.Create(_galleryBitmap));
        using FileStream stream = File.Create(dialog.FileName);
        encoder.Save(stream);
        StatusText.Text = $"Экспортировано: {dialog.FileName}";
    }

    private void Render_OnClick(object sender, RoutedEventArgs e)
    {
        _renderTimer.Stop();
        _ = RenderGalleryAsync();
    }
    private void Cancel_OnClick(object sender, RoutedEventArgs e) => _renderCts?.Cancel();

    private static int ReadInt(string text, string name, int minimum, int maximum)
    {
        if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.CurrentCulture, out int value) || value < minimum || value > maximum)
            throw new InvalidOperationException($"Параметр «{name}» должен быть от {minimum} до {maximum}.");
        return value;
    }

    private static decimal ReadDecimal(string text, string name, decimal minimum, decimal maximum)
    {
        if ((!decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal value) &&
             !decimal.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value)) ||
            value < minimum || value > maximum)
            throw new InvalidOperationException($"Параметр «{name}» должен быть от {minimum} до {maximum}.");
        return value;
    }

    private void Window_OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _renderTimer.Stop();
        _renderCts?.Cancel();
    }

    private sealed record GalleryParameters(decimal RealMin, decimal RealMax, decimal ImaginaryMin,
        decimal ImaginaryMax, int Columns, int Rows, int TileSize, int Iterations);
    private sealed record GalleryCell(int Row, int Column, decimal Real, decimal Imaginary);
    private sealed record GalleryTileResult(GalleryCell Cell, byte[] Pixels, int Completed);
}
