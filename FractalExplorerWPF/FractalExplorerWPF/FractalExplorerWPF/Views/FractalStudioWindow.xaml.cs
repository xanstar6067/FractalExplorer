using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using FractalExplorerWPF.Infrastructure;
using FractalExplorerWPF.Studio.Dsl;
using FractalExplorerWPF.Studio.Models;
using FractalExplorerWPF.Studio.Persistence;
using FractalExplorerWPF.Studio.Rendering;
using Microsoft.Win32;
using Point = System.Windows.Point;

namespace FractalExplorerWPF.Views;

public partial class FractalStudioWindow : Window
{
    private readonly DispatcherTimer _renderTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(320)
    };
    private readonly Dictionary<Guid, StudioLayerFrame> _frames = [];
    private StudioProject _project = new();
    private WriteableBitmap? _bitmap;
    private CancellationTokenSource? _renderCts;
    private bool _isRendering;
    private bool _syncing;
    private bool _isPanning;
    private bool _isFullscreen;
    private bool _scheduleAllDirty;
    private Guid? _scheduledLayerId;
    private Point _panStart;
    private decimal _panStartCenterX;
    private decimal _panStartCenterY;
    private WindowStyle _previousWindowStyle;
    private WindowState _previousWindowState;
    private string? _projectPath;

    public FractalStudioWindow()
    {
        InitializeComponent();
        _renderTimer.Tick += RenderTimer_OnTick;
        PrecisionBox.ItemsSource = Enum.GetValues<StudioPrecisionMode>();
        BlendModeBox.ItemsSource = Enum.GetValues<StudioBlendMode>();
        ThreadsBox.Items.Add("Auto");
        for (int thread = 1; thread <= Environment.ProcessorCount; thread++)
            ThreadsBox.Items.Add(thread);
        CreateNewProject();
        Loaded += (_, _) => ScheduleAllDirty();
    }

    private StudioLayer? SelectedLayer => LayersList.SelectedItem as StudioLayer;

    private void CreateNewProject()
    {
        var project = new StudioProject();
        var layer = new StudioLayer();
        layer.SynchronizeParameters(StudioOrbitRenderer.Compile(layer.FormulaSource));
        project.Layers.Add(layer);
        SetProject(project, null);
    }

    private void SetProject(StudioProject project, string? path)
    {
        DetachProject(_project);
        _renderCts?.Cancel();
        _frames.Clear();
        _bitmap = null;
        CanvasImage.Source = null;
        EmptyCanvasHint.Visibility = Visibility.Visible;
        _project = project;
        _projectPath = path;
        AttachProject(_project);

        _syncing = true;
        LayersList.ItemsSource = _project.Layers;
        AutoRenderBox.IsChecked = _project.AutoRender;
        SsaaBox.SelectedIndex = Math.Clamp(_project.PreviewSsaa - 1, 0, 3);
        ThreadsBox.SelectedItem = _project.ThreadCount <= 0
            ? "Auto"
            : _project.ThreadCount;
        _syncing = false;
        LayersList.SelectedItem = _project.Layers.FirstOrDefault();
        UpdateTitle();
        ScheduleAllDirty();
    }

    private void AttachProject(StudioProject project)
    {
        project.PropertyChanged += Project_OnPropertyChanged;
        foreach (StudioLayer layer in project.Layers)
            AttachLayer(layer);
    }

    private void DetachProject(StudioProject project)
    {
        project.PropertyChanged -= Project_OnPropertyChanged;
        foreach (StudioLayer layer in project.Layers)
            DetachLayer(layer);
    }

    private void AttachLayer(StudioLayer layer)
    {
        layer.PropertyChanged += Layer_OnPropertyChanged;
        foreach (StudioParameterValue parameter in layer.Parameters)
            parameter.PropertyChanged += FormulaParameter_OnPropertyChanged;
    }

    private void DetachLayer(StudioLayer layer)
    {
        layer.PropertyChanged -= Layer_OnPropertyChanged;
        foreach (StudioParameterValue parameter in layer.Parameters)
            parameter.PropertyChanged -= FormulaParameter_OnPropertyChanged;
    }

    private void Project_OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_syncing)
            return;
        if (e.PropertyName is nameof(StudioProject.MasterCenterX) or
            nameof(StudioProject.MasterCenterY) or
            nameof(StudioProject.MasterZoom))
        {
            foreach (StudioLayer layer in _project.Layers.Where(value => value.IsLinkedToMasterCamera))
                MarkLayerStale(layer, schedule: false);
            ScheduleAllDirty();
        }
    }

    private void Layer_OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_syncing || sender is not StudioLayer layer)
            return;
        switch (e.PropertyName)
        {
            case nameof(StudioLayer.RenderState):
            case nameof(StudioLayer.RenderStateText):
            case nameof(StudioLayer.ErrorMessage):
            case nameof(StudioLayer.Name):
                return;
            case nameof(StudioLayer.Opacity):
            case nameof(StudioLayer.BlendMode):
            case nameof(StudioLayer.IsVisible):
                ComposeWholeFrame();
                if (e.PropertyName == nameof(StudioLayer.IsVisible) && layer.IsVisible &&
                    layer.RenderState != StudioLayerRenderState.Ready)
                    MarkLayerStale(layer);
                return;
            default:
                MarkLayerStale(layer);
                return;
        }
    }

    private void FormulaParameter_OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!_syncing && e.PropertyName == nameof(StudioParameterValue.Value) && SelectedLayer is { } layer)
            MarkLayerStale(layer);
    }

    private void LayersList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        StudioLayer? layer = SelectedLayer;
        _syncing = true;
        LayerDetailsPanel.DataContext = layer;
        ParameterItems.ItemsSource = layer?.Parameters;
        if (layer is null)
        {
            FormulaEditor.Text = string.Empty;
            FormulaStructureTree.Items.Clear();
            _syncing = false;
            return;
        }

        FormulaEditor.Text = layer.FormulaSource;
        PrecisionBox.SelectedItem = layer.PrecisionMode;
        BlendModeBox.SelectedItem = layer.BlendMode;
        OpacitySlider.Value = layer.Opacity;
        SynchronizeCameraFields(layer);
        PaletteFrequencyBox.Text = layer.PaletteFrequency.ToString("G17", CultureInfo.InvariantCulture);
        PalettePhaseBox.Text = layer.PalettePhase.ToString("G17", CultureInfo.InvariantCulture);
        try
        {
            BuildFormulaTree(StudioOrbitRenderer.Compile(layer.FormulaSource).Document);
            FormulaStatusText.Text = "Формула скомпилирована";
        }
        catch (Exception ex)
        {
            FormulaStatusText.Text = ex.Message;
        }
        _syncing = false;
        UpdateRenderInfo();
    }

    private void AddLayerButton_OnClick(object sender, RoutedEventArgs e)
    {
        var layer = new StudioLayer { Name = $"Фрактальный слой {_project.Layers.Count + 1}" };
        layer.SynchronizeParameters(StudioOrbitRenderer.Compile(layer.FormulaSource));
        AttachLayer(layer);
        _project.Layers.Insert(0, layer);
        LayersList.SelectedItem = layer;
        MarkLayerStale(layer);
    }

    private void DuplicateLayerButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (SelectedLayer is not { } selected)
            return;
        StudioLayer clone = selected.Clone();
        AttachLayer(clone);
        int index = Math.Max(0, _project.Layers.IndexOf(selected));
        _project.Layers.Insert(index, clone);
        if (_frames.TryGetValue(selected.Id, out StudioLayerFrame? frame))
            _frames[clone.Id] = frame;
        clone.RenderState = selected.RenderState;
        LayersList.SelectedItem = clone;
        ComposeWholeFrame();
    }

    private void MoveLayerUpButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (SelectedLayer is not { } layer)
            return;
        int index = _project.Layers.IndexOf(layer);
        if (index <= 0)
            return;
        _project.Layers.Move(index, index - 1);
        ComposeWholeFrame();
    }

    private void DeleteLayerButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (SelectedLayer is not { } layer || _project.Layers.Count <= 1)
        {
            StatusText.Text = "В проекте должен оставаться хотя бы один слой.";
            return;
        }

        int index = _project.Layers.IndexOf(layer);
        DetachLayer(layer);
        _project.Layers.Remove(layer);
        _frames.Remove(layer.Id);
        LayersList.SelectedIndex = Math.Clamp(index, 0, _project.Layers.Count - 1);
        ComposeWholeFrame();
    }

    private void PrecisionBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_syncing && SelectedLayer is { } layer &&
            PrecisionBox.SelectedItem is StudioPrecisionMode precision)
        {
            layer.PrecisionMode = precision;
            UpdateRenderInfo();
        }
    }

    private void BlendModeBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_syncing && SelectedLayer is { } layer &&
            BlendModeBox.SelectedItem is StudioBlendMode blendMode)
            layer.BlendMode = blendMode;
    }

    private void OpacitySlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_syncing && SelectedLayer is { } layer)
            layer.Opacity = e.NewValue;
    }

    private void CameraField_OnLostFocus(object sender, RoutedEventArgs e)
    {
        if (_syncing || SelectedLayer is not { } layer)
            return;
        if (!TryDecimal(CenterXBox.Text, out decimal centerX) ||
            !TryDecimal(CenterYBox.Text, out decimal centerY) ||
            !TryDecimal(ZoomBox.Text, out decimal zoom) ||
            zoom <= 0)
        {
            StatusText.Text = "Проверьте центр и масштаб камеры.";
            SynchronizeCameraFields(layer);
            return;
        }

        SetActiveCamera(layer, centerX, centerY, zoom);
    }

    private void PaletteField_OnLostFocus(object sender, RoutedEventArgs e)
    {
        if (_syncing || SelectedLayer is not { } layer)
            return;
        if (!TryDouble(PaletteFrequencyBox.Text, out double frequency) ||
            !TryDouble(PalettePhaseBox.Text, out double phase) ||
            frequency <= 0)
        {
            StatusText.Text = "Проверьте частоту и фазу палитры.";
            PaletteFrequencyBox.Text = layer.PaletteFrequency.ToString("G17", CultureInfo.InvariantCulture);
            PalettePhaseBox.Text = layer.PalettePhase.ToString("G17", CultureInfo.InvariantCulture);
            return;
        }
        layer.PaletteFrequency = frequency;
        layer.PalettePhase = phase;
    }

    private void ApplyFormulaButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (SelectedLayer is not { } layer)
            return;
        try
        {
            StudioCompiledFormula compiled = StudioOrbitRenderer.Compile(FormulaEditor.Text);
            DetachLayer(layer);
            layer.FormulaSource = FormulaEditor.Text;
            layer.SynchronizeParameters(compiled);
            AttachLayer(layer);
            ParameterItems.ItemsSource = layer.Parameters;
            BuildFormulaTree(compiled.Document);
            FormulaStatusText.Text = "Формула проверена и скомпилирована";
            layer.ErrorMessage = null;
            MarkLayerStale(layer);
        }
        catch (Exception ex)
        {
            FormulaStatusText.Text = ex.Message;
            layer.ErrorMessage = ex.Message;
            layer.RenderState = StudioLayerRenderState.Error;
        }
    }

    private void FormulaEditor_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_syncing && SelectedLayer is not null)
            FormulaStatusText.Text = "Есть неприменённые изменения";
    }

    private void BuildFormulaTree(StudioFormulaDocument document)
    {
        FormulaStructureTree.Items.Clear();
        var parameters = new TreeViewItem { Header = $"parameters ({document.Parameters.Count})" };
        foreach (StudioFormulaParameter parameter in document.Parameters)
            parameters.Items.Add(new TreeViewItem
            {
                Header = $"{parameter.Kind.ToString().ToLowerInvariant()} {parameter.Name} = " +
                         FormatExpression(parameter.DefaultValue)
            });
        FormulaStructureTree.Items.Add(parameters);
        FormulaStructureTree.Items.Add(CreateStatementTree("init", document.Initialization));
        FormulaStructureTree.Items.Add(CreateStatementTree("iterate", document.Iteration));
        FormulaStructureTree.Items.Add(new TreeViewItem
        {
            Header = "escape",
            Items = { new TreeViewItem { Header = FormatExpression(document.EscapeCondition) } }
        });
        parameters.IsExpanded = true;
    }

    private static TreeViewItem CreateStatementTree(string name, IReadOnlyList<StudioStatement> statements)
    {
        var root = new TreeViewItem { Header = name, IsExpanded = true };
        foreach (StudioStatement statement in statements)
        {
            root.Items.Add(new TreeViewItem
            {
                Header = statement switch
                {
                    StudioVariableDeclaration declaration =>
                        $"{declaration.Kind.ToString().ToLowerInvariant()} {declaration.Name} = " +
                        FormatExpression(declaration.Initializer),
                    StudioAssignment assignment =>
                        $"{assignment.Name} = {FormatExpression(assignment.Value)}",
                    _ => statement.ToString()
                }
            });
        }
        return root;
    }

    private static string FormatExpression(StudioExpression expression) => expression switch
    {
        StudioNumberExpression number => number.Text,
        StudioIdentifierExpression identifier => identifier.Name,
        StudioUnaryExpression unary =>
            $"{TokenText(unary.Operator)}{FormatExpression(unary.Operand)}",
        StudioBinaryExpression binary =>
            $"({FormatExpression(binary.Left)} {TokenText(binary.Operator)} {FormatExpression(binary.Right)})",
        StudioCallExpression call =>
            $"{call.Name}({string.Join(", ", call.Arguments.Select(FormatExpression))})",
        _ => expression.ToString() ?? string.Empty
    };

    private static string TokenText(StudioTokenKind kind) => kind switch
    {
        StudioTokenKind.Plus => "+",
        StudioTokenKind.Minus => "-",
        StudioTokenKind.Star => "*",
        StudioTokenKind.Slash => "/",
        StudioTokenKind.Caret => "^",
        StudioTokenKind.EqualsEquals => "==",
        StudioTokenKind.BangEquals => "!=",
        StudioTokenKind.Greater => ">",
        StudioTokenKind.GreaterOrEquals => ">=",
        StudioTokenKind.Less => "<",
        StudioTokenKind.LessOrEquals => "<=",
        StudioTokenKind.AmpersandAmpersand => "&&",
        StudioTokenKind.PipePipe => "||",
        StudioTokenKind.Bang => "!",
        _ => kind.ToString()
    };

    private void AutoRenderBox_OnChanged(object sender, RoutedEventArgs e)
    {
        if (_syncing)
            return;
        _project.AutoRender = AutoRenderBox.IsChecked == true;
        if (_project.AutoRender)
            ScheduleAllDirty();
        else
            _renderTimer.Stop();
    }

    private void SsaaBox_OnChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncing || SsaaBox.SelectedItem is not ComboBoxItem item ||
            !int.TryParse(item.Tag?.ToString(), out int value))
            return;
        _project.PreviewSsaa = value;
        MarkAllLayersStale();
        ScheduleAllDirty();
        UpdateRenderInfo();
    }

    private void ThreadsBox_OnChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncing)
            return;
        _project.ThreadCount = ThreadsBox.SelectedItem?.ToString() == "Auto"
            ? 0
            : Convert.ToInt32(ThreadsBox.SelectedItem, CultureInfo.InvariantCulture);
    }

    private void RenderLayerButton_OnClick(object sender, RoutedEventArgs e) => _ = RenderSelectedLayerAsync();
    private void RenderDirtyButton_OnClick(object sender, RoutedEventArgs e) => _ = RenderDirtyLayersAsync();

    private void FullRenderButton_OnClick(object sender, RoutedEventArgs e)
    {
        _frames.Clear();
        MarkAllLayersStale();
        _ = RenderDirtyLayersAsync();
    }

    private void CancelRenderButton_OnClick(object sender, RoutedEventArgs e) => _renderCts?.Cancel();

    private void MarkLayerStale(StudioLayer layer, bool schedule = true)
    {
        if (layer.RenderState != StudioLayerRenderState.Rendering)
            layer.RenderState = StudioLayerRenderState.Stale;
        layer.ErrorMessage = null;
        if (schedule && _project.AutoRender)
            ScheduleLayer(layer);
    }

    private void MarkAllLayersStale()
    {
        foreach (StudioLayer layer in _project.Layers)
            MarkLayerStale(layer, schedule: false);
    }

    private void ScheduleLayer(StudioLayer layer)
    {
        if (!_project.AutoRender || !IsLoaded)
            return;
        if (_isRendering)
            _renderCts?.Cancel();
        _scheduledLayerId = layer.Id;
        _renderTimer.Stop();
        _renderTimer.Start();
    }

    private void ScheduleAllDirty()
    {
        if (!_project.AutoRender || !IsLoaded)
            return;
        if (_isRendering)
            _renderCts?.Cancel();
        _scheduleAllDirty = true;
        _scheduledLayerId = null;
        _renderTimer.Stop();
        _renderTimer.Start();
    }

    private void RenderTimer_OnTick(object? sender, EventArgs e)
    {
        _renderTimer.Stop();
        if (_scheduleAllDirty)
        {
            _scheduleAllDirty = false;
            _ = RenderDirtyLayersAsync();
            return;
        }

        StudioLayer? layer = _scheduledLayerId is Guid id
            ? _project.Layers.FirstOrDefault(value => value.Id == id)
            : null;
        _scheduledLayerId = null;
        if (layer is not null)
            _ = RenderLayerAsync(layer);
    }

    private Task RenderSelectedLayerAsync() =>
        SelectedLayer is { } layer ? RenderLayerAsync(layer) : Task.CompletedTask;

    private async Task RenderDirtyLayersAsync()
    {
        if (_isRendering)
            return;
        StudioLayer[] layers = _project.Layers
            .Where(layer => layer.IsVisible && layer.RenderState != StudioLayerRenderState.Ready)
            .Reverse()
            .ToArray();
        foreach (StudioLayer layer in layers)
        {
            if (!await RenderLayerAsync(layer))
                break;
        }
    }

    private async Task<bool> RenderLayerAsync(StudioLayer layer)
    {
        if (_isRendering || !layer.IsVisible)
            return false;
        (int width, int height) = MeasureSurface();
        if (width <= 0 || height <= 0)
            return false;
        EnsureBitmap(width, height);
        CanvasImage.RenderTransform = Transform.Identity;
        StudioLayerSnapshot snapshot = StudioLayerSnapshot.Capture(layer, _project);
        _renderCts = new CancellationTokenSource();
        CancellationToken token = _renderCts.Token;
        _isRendering = true;
        layer.RenderState = StudioLayerRenderState.Rendering;
        layer.ErrorMessage = null;
        CancelRenderButton.IsEnabled = true;
        StatusText.Text = $"Рендер слоя «{layer.Name}»…";
        RenderProgress.Value = 0;
        UpdateRenderInfo();

        try
        {
            var progress = new Progress<StudioRenderProgress>(value =>
            {
                _frames[layer.Id] = value.Frame;
                RenderProgress.Value = value.TotalTiles == 0
                    ? 0
                    : value.CompletedTiles * 100d / value.TotalTiles;
                ComposeTile(value.Tile);
            });
            StudioLayerFrame frame = await StudioOrbitRenderer.RenderAsync(
                snapshot,
                width,
                height,
                _project.PreviewSsaa,
                _project.ThreadCount,
                token,
                progress);
            _frames[layer.Id] = frame;
            layer.RenderState = StudioLayerRenderState.Ready;
            ComposeWholeFrame();
            StatusText.Text = $"Слой «{layer.Name}» готов.";
            return true;
        }
        catch (OperationCanceledException)
        {
            layer.RenderState = StudioLayerRenderState.Stale;
            StatusText.Text = "Рендер отменён.";
            return false;
        }
        catch (Exception ex)
        {
            layer.RenderState = StudioLayerRenderState.Error;
            layer.ErrorMessage = ex.Message;
            StatusText.Text = ex.Message;
            return false;
        }
        finally
        {
            _isRendering = false;
            CancelRenderButton.IsEnabled = false;
            RenderProgress.Value = 0;
            _renderCts.Dispose();
            _renderCts = null;
            if (_project.AutoRender && (_scheduleAllDirty || _scheduledLayerId is not null))
            {
                _renderTimer.Stop();
                _renderTimer.Start();
            }
        }
    }

    private void EnsureBitmap(int width, int height)
    {
        if (_bitmap is not null && _bitmap.PixelWidth == width && _bitmap.PixelHeight == height)
            return;
        _bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
        CanvasImage.Source = _bitmap;
        _frames.Clear();
        MarkAllLayersStale();
        EmptyCanvasHint.Visibility = Visibility.Collapsed;
    }

    private void ComposeWholeFrame()
    {
        if (_bitmap is null)
            return;
        foreach (StudioTile tile in StudioTilePlanner.Create(_bitmap.PixelWidth, _bitmap.PixelHeight, 128))
            ComposeTile(tile);
    }

    private void ComposeTile(StudioTile tile)
    {
        if (_bitmap is null)
            return;
        IReadOnlyList<(StudioLayerSnapshot Layer, StudioLayerFrame Frame)> layers = _project.Layers
            .Reverse()
            .Where(layer => _frames.ContainsKey(layer.Id))
            .Select(layer => (StudioLayerSnapshot.Capture(layer, _project), _frames[layer.Id]))
            .ToArray();
        byte[] pixels = StudioCompositor.ComposeTile(layers, tile);
        _bitmap.WritePixels(
            new Int32Rect(tile.X, tile.Y, tile.Width, tile.Height),
            pixels,
            tile.Width * 4,
            0);
        EmptyCanvasHint.Visibility = Visibility.Collapsed;
    }

    private (int Width, int Height) MeasureSurface()
    {
        DpiScale dpi = VisualTreeHelper.GetDpi(CanvasHost);
        int width = Math.Max(1, (int)Math.Round(CanvasHost.ActualWidth * dpi.DpiScaleX));
        int height = Math.Max(1, (int)Math.Round(CanvasHost.ActualHeight * dpi.DpiScaleY));
        return (width, height);
    }

    private void CanvasHost_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (!IsLoaded)
            return;
        MarkAllLayersStale();
        ScheduleAllDirty();
    }

    private void CanvasHost_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (SelectedLayer is null)
            return;
        _isPanning = true;
        _panStart = e.GetPosition(CanvasHost);
        (_panStartCenterX, _panStartCenterY, _) = GetActiveCamera(SelectedLayer);
        CanvasHost.CaptureMouse();
        Mouse.OverrideCursor = Cursors.SizeAll;
    }

    private void CanvasHost_OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isPanning)
            return;
        Point current = e.GetPosition(CanvasHost);
        CanvasImage.RenderTransform = new TranslateTransform(
            current.X - _panStart.X,
            current.Y - _panStart.Y);
    }

    private void CanvasHost_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isPanning || SelectedLayer is not { } layer)
            return;
        Point current = e.GetPosition(CanvasHost);
        double width = Math.Max(1, CanvasHost.ActualWidth);
        double height = Math.Max(1, CanvasHost.ActualHeight);
        (_, _, decimal zoom) = GetActiveCamera(layer);
        decimal viewWidth = 3m / zoom;
        decimal viewHeight = viewWidth * (decimal)height / (decimal)width;
        decimal centerX = _panStartCenterX - (decimal)(current.X - _panStart.X) / (decimal)width * viewWidth;
        decimal centerY = _panStartCenterY + (decimal)(current.Y - _panStart.Y) / (decimal)height * viewHeight;
        _isPanning = false;
        CanvasHost.ReleaseMouseCapture();
        Mouse.OverrideCursor = null;
        SetActiveCamera(layer, centerX, centerY, zoom);
    }

    private void CanvasHost_OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (SelectedLayer is not { } layer)
            return;
        Point point = e.GetPosition(CanvasHost);
        (decimal beforeX, decimal beforeY) = ScreenToWorld(layer, point);
        (decimal centerX, decimal centerY, decimal zoom) = GetActiveCamera(layer);
        decimal factor = e.Delta > 0 ? 1.5m : 1m / 1.5m;
        zoom = Math.Clamp(zoom * factor, 0.01m, 1000000000000000000000000000m);
        SetCameraValues(layer, centerX, centerY, zoom);
        (decimal afterX, decimal afterY) = ScreenToWorld(layer, point);
        centerX += beforeX - afterX;
        centerY += beforeY - afterY;
        SetCameraValues(layer, centerX, centerY, zoom);

        Matrix matrix = CanvasImage.RenderTransform.Value;
        matrix.ScaleAt((double)factor, (double)factor, point.X, point.Y);
        CanvasImage.RenderTransform = new MatrixTransform(matrix);
        SynchronizeCameraFields(layer);
        MarkCameraStale(layer);
        e.Handled = true;
    }

    private (decimal X, decimal Y) ScreenToWorld(StudioLayer layer, Point point)
    {
        (decimal centerX, decimal centerY, decimal zoom) = GetActiveCamera(layer);
        decimal width = (decimal)Math.Max(1, CanvasHost.ActualWidth);
        decimal height = (decimal)Math.Max(1, CanvasHost.ActualHeight);
        decimal viewWidth = 3m / zoom;
        decimal viewHeight = viewWidth * height / width;
        return (
            centerX + ((decimal)point.X / width - 0.5m) * viewWidth,
            centerY + (0.5m - (decimal)point.Y / height) * viewHeight);
    }

    private (decimal X, decimal Y, decimal Zoom) GetActiveCamera(StudioLayer layer) =>
        layer.IsLinkedToMasterCamera
            ? (_project.MasterCenterX, _project.MasterCenterY, _project.MasterZoom)
            : (layer.CenterX, layer.CenterY, layer.Zoom);

    private void SetActiveCamera(StudioLayer layer, decimal x, decimal y, decimal zoom)
    {
        SetCameraValues(layer, x, y, zoom);
        SynchronizeCameraFields(layer);
        CanvasImage.RenderTransform = Transform.Identity;
        MarkCameraStale(layer);
    }

    private void SetCameraValues(StudioLayer layer, decimal x, decimal y, decimal zoom)
    {
        _syncing = true;
        if (layer.IsLinkedToMasterCamera)
        {
            _project.MasterCenterX = x;
            _project.MasterCenterY = y;
            _project.MasterZoom = zoom;
        }
        else
        {
            layer.CenterX = x;
            layer.CenterY = y;
            layer.Zoom = zoom;
        }
        _syncing = false;
    }

    private void MarkCameraStale(StudioLayer layer)
    {
        if (layer.IsLinkedToMasterCamera)
        {
            foreach (StudioLayer linked in _project.Layers.Where(value => value.IsLinkedToMasterCamera))
                MarkLayerStale(linked, schedule: false);
            ScheduleAllDirty();
        }
        else
        {
            MarkLayerStale(layer);
        }
    }

    private void SynchronizeCameraFields(StudioLayer layer)
    {
        (decimal x, decimal y, decimal zoom) = GetActiveCamera(layer);
        CenterXBox.Text = x.ToString(CultureInfo.InvariantCulture);
        CenterYBox.Text = y.ToString(CultureInfo.InvariantCulture);
        ZoomBox.Text = zoom.ToString(CultureInfo.InvariantCulture);
    }

    private async void SaveProjectButton_OnClick(object sender, RoutedEventArgs e)
    {
        await SaveProjectAsync();
    }

    private async Task<bool> SaveProjectAsync()
    {
        string? path = _projectPath;
        if (string.IsNullOrWhiteSpace(path))
        {
            var dialog = new SaveFileDialog
            {
                Title = "Сохранить проект Fractal Studio",
                Filter = "Fractal Studio project|*.frstudio",
                DefaultExt = ".frstudio",
                AddExtension = true,
                FileName = SanitizeFileName(_project.Name)
            };
            if (dialog.ShowDialog(this) != true)
                return false;
            path = dialog.FileName;
        }

        try
        {
            StatusText.Text = "Сохранение проекта…";
            await StudioProjectStore.SaveAsync(path, _project, CreatePreviewPng());
            _projectPath = path;
            UpdateTitle();
            StatusText.Text = $"Проект сохранён: {Path.GetFileName(path)}";
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Fractal Studio", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusText.Text = ex.Message;
            return false;
        }
    }

    private async void OpenProjectButton_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Открыть проект Fractal Studio",
            Filter = "Fractal Studio project|*.frstudio"
        };
        if (dialog.ShowDialog(this) != true)
            return;
        try
        {
            StatusText.Text = "Открытие проекта…";
            StudioProject project = await StudioProjectStore.LoadAsync(dialog.FileName);
            SetProject(project, dialog.FileName);
            StatusText.Text = $"Открыт проект: {Path.GetFileName(dialog.FileName)}";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Fractal Studio", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusText.Text = ex.Message;
        }
    }

    private void NewProjectButton_OnClick(object sender, RoutedEventArgs e) => CreateNewProject();

    private void ExportImageButton_OnClick(object sender, RoutedEventArgs e)
    {
        (int width, int height) = MeasureSurface();
        ImageExportManagerWindow.Open(this, new ImageExportConfiguration
        {
            FileNamePrefix = "FractalStudio",
            WindowTitle = "Экспорт композиции Fractal Studio",
            InitialWidth = Math.Max(1, width),
            InitialHeight = Math.Max(1, height),
            MaxSsaaFactor = 4,
            HasNativeSsaa = true,
            ReleaseMemoryAfterExport = true,
            RenderAsync = RenderExportCompositionAsync
        });
    }

    private async Task<BitmapSource> RenderExportCompositionAsync(
        ImageExportRenderRequest request,
        CancellationToken token,
        IProgress<int> progress)
    {
        StudioLayerSnapshot[] snapshots = _project.Layers
            .Where(layer => layer.IsVisible)
            .Reverse()
            .Select(layer => StudioLayerSnapshot.Capture(layer, _project))
            .ToArray();
        if (snapshots.Length == 0)
            throw new InvalidOperationException("В композиции нет видимых слоёв.");

        var rendered = new List<(StudioLayerSnapshot Layer, StudioLayerFrame Frame)>();
        for (int index = 0; index < snapshots.Length; index++)
        {
            int layerIndex = index;
            var layerProgress = new Progress<StudioRenderProgress>(value =>
            {
                double fraction = value.TotalTiles == 0
                    ? 0
                    : (double)value.CompletedTiles / value.TotalTiles;
                progress.Report((int)Math.Round((layerIndex + fraction) * 88 / snapshots.Length));
            });
            StudioLayerFrame frame = await StudioOrbitRenderer.RenderAsync(
                snapshots[index],
                request.Width,
                request.Height,
                request.SsaaFactor,
                _project.ThreadCount,
                token,
                layerProgress);
            rendered.Add((snapshots[index], frame));
        }

        int stride = checked(request.Width * 4);
        var pixels = new byte[checked(stride * request.Height)];
        IReadOnlyList<StudioTile> tiles = StudioTilePlanner.Create(request.Width, request.Height, 128);
        for (int index = 0; index < tiles.Count; index++)
        {
            token.ThrowIfCancellationRequested();
            StudioTile tile = tiles[index];
            byte[] tilePixels = StudioCompositor.ComposeTile(rendered, tile);
            int tileStride = tile.Width * 4;
            for (int row = 0; row < tile.Height; row++)
            {
                Buffer.BlockCopy(
                    tilePixels,
                    row * tileStride,
                    pixels,
                    (tile.Y + row) * stride + tile.X * 4,
                    tileStride);
            }
            progress.Report(88 + (index + 1) * 10 / Math.Max(1, tiles.Count));
        }

        BitmapSource bitmap = BitmapSource.Create(
            request.Width,
            request.Height,
            96,
            96,
            PixelFormats.Bgra32,
            null,
            pixels,
            stride);
        bitmap.Freeze();
        progress.Report(100);
        return bitmap;
    }

    private byte[]? CreatePreviewPng()
    {
        if (_bitmap is null)
            return null;
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(_bitmap));
        using var stream = new MemoryStream();
        encoder.Save(stream);
        return stream.ToArray();
    }

    private void UpdateTitle()
    {
        Title = string.IsNullOrWhiteSpace(_projectPath)
            ? $"Fractal Studio — {_project.Name}"
            : $"Fractal Studio — {Path.GetFileNameWithoutExtension(_projectPath)}";
    }

    private void UpdateRenderInfo()
    {
        string precision = SelectedLayer?.PrecisionMode == StudioPrecisionMode.Decimal
            ? "decimal"
            : "double";
        RenderInfoText.Text = $"{precision} · SSAA {_project.PreviewSsaa}×";
    }

    private void Window_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F11)
        {
            ToggleFullscreen();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape && _isFullscreen)
        {
            ToggleFullscreen();
            e.Handled = true;
        }
        else if (e.Key == Key.F5)
        {
            _ = RenderSelectedLayerAsync();
            e.Handled = true;
        }
        else if (e.Key == Key.S && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            _ = SaveProjectAsync();
            e.Handled = true;
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

    private void Window_OnClosing(object? sender, CancelEventArgs e)
    {
        _renderTimer.Stop();
        _renderCts?.Cancel();
        DetachProject(_project);
    }

    private static bool TryDecimal(string text, out decimal value) =>
        decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) ||
        decimal.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value);

    private static bool TryDouble(string text, out double value) =>
        (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) ||
         double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value)) &&
        double.IsFinite(value);

    private static string SanitizeFileName(string value)
    {
        foreach (char invalid in Path.GetInvalidFileNameChars())
            value = value.Replace(invalid, '_');
        return string.IsNullOrWhiteSpace(value) ? "FractalStudio" : value;
    }
}
