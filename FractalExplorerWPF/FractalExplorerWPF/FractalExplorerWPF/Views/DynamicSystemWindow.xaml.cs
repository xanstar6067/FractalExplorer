using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using FractalExplorerWPF.Core.Rendering;
using FractalExplorerWPF.Infrastructure;
using FractalExplorerWPF.Infrastructure.ColorPicking;
using FractalExplorerWPF.Models;
using Microsoft.Win32;
using Color = System.Windows.Media.Color;
using Point = System.Windows.Point;

namespace FractalExplorerWPF.Views;

public partial class DynamicSystemWindow : Window
{
    private readonly DynamicSystemKind _kind;
    private readonly DynamicSystemSaveStore _saves;
    private readonly DynamicPaletteStore? _paletteStore;
    private readonly Dictionary<string, TextBox> _boxes = [];
    private readonly Dictionary<string, ComboBox> _choices = [];
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(400) };
    private DynamicSystemState _state;
    private List<DynamicPalette> _palettes = [];
    private CancellationTokenSource? _cts;
    private bool _rendering, _syncing, _panning, _controls = true, _fullscreen;
    private Point _panStart;
    private WindowStyle _oldStyle;
    private WindowState _oldState;

    public DynamicSystemWindow(DynamicSystemKind kind)
    {
        _kind = kind; _state = DynamicSystemState.CreateDefault(kind); _saves = new(kind);
        if (kind is DynamicSystemKind.Lyapunov or DynamicSystemKind.LogisticMap) _paletteStore = new(kind);
        InitializeComponent();
        Title = Heading.Text = DisplayName(kind);
        BuildParameterPanel(); LoadPalettes(); SyncControls(); UpdateSwatches();
        _timer.Tick += (_, _) => { _timer.Stop(); _ = RenderAsync(); };
        Loaded += (_, _) => Schedule();
    }

    private void BuildParameterPanel()
    {
        foreach ((string label, string key) in Fields(_kind)) AddField(label, key);
        if (_kind is DynamicSystemKind.Lorenz or DynamicSystemKind.Rossler) AddChoice("Проекция", "ProjectionMode", ["XY", "XZ", "YZ"]);
        if (_kind == DynamicSystemKind.LogisticMap) AddChoice("Режим", "VisualizationMode", ["Orbit", "Bifurcation", "Cobweb"]);
        if (_kind == DynamicSystemKind.Lyapunov) AddChoice("Сглаживание", "SsaaFactor", ["1", "2", "4"]);
        AddField("Потоки ЦП", "Threads");
        PalettePanel.Visibility = _paletteStore is null ? Visibility.Collapsed : Visibility.Visible;
        FractalColorPanel.Visibility = _kind == DynamicSystemKind.Bifurcation ? Visibility.Visible : Visibility.Collapsed;
        BackgroundColorPanel.Visibility = _kind is DynamicSystemKind.Lyapunov or DynamicSystemKind.Henon or DynamicSystemKind.Ikeda ? Visibility.Collapsed : Visibility.Visible;
    }

    private static IEnumerable<(string, string)> Fields(DynamicSystemKind kind) => kind switch
    {
        DynamicSystemKind.Lyapunov => [("Мин. A","AMin"),("Макс. A","AMax"),("Мин. B","BMin"),("Макс. B","BMax"),("Паттерн A/B","Pattern"),("Итерации","Iterations"),("Прогрев","TransientIterations")],
        DynamicSystemKind.Lorenz => [("σ","Sigma"),("ρ","Rho"),("β","Beta"),("dt","Dt"),("Шаги","Steps"),("Старт X","StartX"),("Старт Y","StartY"),("Старт Z","StartZ"),("Центр X","CenterX"),("Центр Y","CenterY"),("Масштаб","Zoom")],
        DynamicSystemKind.Rossler => [("a","A"),("b","B"),("c","C"),("dt","Dt"),("Шаги","Steps"),("Старт X","StartX"),("Старт Y","StartY"),("Старт Z","StartZ"),("Центр X","CenterX"),("Центр Y","CenterY"),("Масштаб","Zoom")],
        DynamicSystemKind.LogisticMap => [("Параметр r","R"),("Нач. x₀","X0"),("Итерации","Iterations"),("Прогрев","TransientIterations"),("Bif r min","BifurcationRMin"),("Bif r max","BifurcationRMax"),("Bif samples","BifurcationSamples"),("Bif transient","BifurcationTransient"),("Bif plotted","BifurcationPlottedPoints"),("Cobweb шаги","CobwebSteps"),("Центр X","CenterX"),("Центр Y","CenterY"),("Масштаб","Zoom")],
        DynamicSystemKind.Bifurcation => [("r min","RMin"),("r max","RMax"),("x min","XMin"),("x max","XMax"),("Прогрев","TransientIterations"),("Samples / r","SamplesPerR"),("Итерации","Iterations"),("Центр X","CenterX"),("Центр Y","CenterY"),("Масштаб","Zoom")],
        DynamicSystemKind.Henon => [("Параметр a","A"),("Параметр b","B"),("Нач. x₀","X0"),("Нач. y₀","Y0"),("Итерации","Iterations"),("Пропуск","DiscardIterations"),("Центр X","CenterX"),("Центр Y","CenterY"),("Масштаб","Zoom")],
        _ => [("Параметр u","U"),("Нач. x₀","X0"),("Нач. y₀","Y0"),("Итерации","Iterations"),("Пропуск","DiscardIterations"),("X min","RangeXMin"),("X max","RangeXMax"),("Y min","RangeYMin"),("Y max","RangeYMax"),("Центр X","CenterX"),("Центр Y","CenterY"),("Масштаб","Zoom")]
    };

    private void AddField(string label, string key)
    {
        var grid = new Grid { Margin = new Thickness(0, 3, 0, 3) };
        grid.ColumnDefinitions.Add(new ColumnDefinition()); grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(125) });
        grid.Children.Add(new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center });
        var box = new TextBox { Tag = key }; Grid.SetColumn(box, 1); grid.Children.Add(box); _boxes[key] = box;
        box.TextChanged += (_, _) => { if (!_syncing) Schedule(); };
        ParameterPanel.Children.Add(grid);
    }

    private void AddChoice(string label, string key, string[] values)
    {
        var grid = new Grid { Margin = new Thickness(0, 3, 0, 3) }; grid.ColumnDefinitions.Add(new()); grid.ColumnDefinitions.Add(new() { Width = new GridLength(125) });
        grid.Children.Add(new TextBlock { Text=label, VerticalAlignment=VerticalAlignment.Center });
        var combo = new ComboBox { Name = key + "Box", ItemsSource=values, Tag=key }; _choices[key]=combo; combo.SelectionChanged += Choice_OnChanged; Grid.SetColumn(combo,1); grid.Children.Add(combo); ParameterPanel.Children.Add(grid);
    }

    private void Choice_OnChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncing || sender is not ComboBox { Tag: string key, SelectedItem: string value }) return;
        PropertyInfo? property = typeof(DynamicSystemState).GetProperty(key); if (property is null) return;
        property.SetValue(_state, property.PropertyType == typeof(int) ? int.Parse(value, CultureInfo.InvariantCulture) : value); Schedule();
    }

    private void LoadPalettes()
    {
        if (_paletteStore is null) return; _palettes = _paletteStore.Load(); PaletteBox.ItemsSource = _palettes;
        PaletteBox.DisplayMemberPath = nameof(DynamicPalette.Name); PaletteBox.SelectedItem = ActivePalette ?? _palettes.FirstOrDefault();
    }
    private DynamicPalette? ActivePalette => _palettes.FirstOrDefault(p => p.Name == _state.PaletteName) ?? _palettes.FirstOrDefault();

    private DynamicSystemState CaptureState(string name)
    {
        foreach ((string key, TextBox box) in _boxes)
        {
            PropertyInfo p = typeof(DynamicSystemState).GetProperty(key)!;
            if (p.PropertyType == typeof(string)) { p.SetValue(_state, box.Text.Trim()); continue; }
            if (p.PropertyType == typeof(int)) { if (!int.TryParse(box.Text, out int v) || v < 0) throw new InvalidOperationException($"Некорректное значение: {key}"); p.SetValue(_state, v); }
            else { if (!TryDouble(box.Text, out double v) || !double.IsFinite(v)) throw new InvalidOperationException($"Некорректное значение: {key}"); p.SetValue(_state, v); }
        }
        if (_state.Zoom <= 0 || _state.Threads is < 1 or > 256) throw new InvalidOperationException("Масштаб должен быть положительным, число потоков — от 1 до 256.");
        if (_kind == DynamicSystemKind.Lyapunov && (_state.AMax <= _state.AMin || _state.BMax <= _state.BMin || !_state.Pattern.Any(c => c is 'A' or 'a' or 'B' or 'b'))) throw new InvalidOperationException("Проверьте диапазоны A/B и паттерн.");
        DynamicSystemState result = _state.Clone(name); result.Timestamp = DateTime.Now; result.PaletteName = ActivePalette?.Name ?? string.Empty; return result;
    }

    private void LoadState(DynamicSystemState state) { _cts?.Cancel(); _state = state.Clone(); _state.Kind = _kind; SyncControls(); UpdateSwatches(); Schedule(); }
    private void SyncControls()
    {
        _syncing=true;
        foreach ((string key, TextBox box) in _boxes) box.Text = Format(typeof(DynamicSystemState).GetProperty(key)!.GetValue(_state));
        foreach ((string key,ComboBox combo) in _choices) combo.SelectedItem = Format(typeof(DynamicSystemState).GetProperty(key)!.GetValue(_state));
        if (_paletteStore is not null) PaletteBox.SelectedItem = ActivePalette;
        _syncing=false;
    }

    private async Task RenderAsync()
    {
        if (_rendering) { Schedule(); return; }
        DynamicSystemState state; try { state = CaptureState("preview"); } catch (Exception ex) { StatusText.Text=ex.Message; return; }
        _cts?.Cancel(); _cts?.Dispose(); _cts=new(); CancellationToken token=_cts.Token; _rendering=true; CancelButton.IsEnabled=true; RenderBadge.Visibility=Visibility.Visible; var watch=Stopwatch.StartNew();
        try
        {
            DpiScale dpi=VisualTreeHelper.GetDpi(CanvasHost); int width=Math.Max(1,(int)Math.Ceiling(CanvasHost.ActualWidth*dpi.DpiScaleX)), height=Math.Max(1,(int)Math.Ceiling(CanvasHost.ActualHeight*dpi.DpiScaleY));
            WriteableBitmap? overlay=null;
            if (_kind==DynamicSystemKind.Lyapunov)
            {
                overlay=ProgressiveRenderBitmap.CreateOverlay(width*state.SsaaFactor,height*state.SsaaFactor,dpi.PixelsPerInchX,dpi.PixelsPerInchY); CurrentImage.Source=overlay;
            }
            var progress=new Progress<int>(p=>{ProgressBar.Value=p;ProgressText.Text=$"Рендер: {p}%";RenderBadgeText.Text=$"{p}%";});
            BitmapSource image=await DynamicSystemRenderer.RenderAsync(state,width,height,ActivePalette,token,progress,overlay is null?null:(tile,data)=>Dispatcher.BeginInvoke(()=>ProgressiveRenderBitmap.WriteTile(overlay,tile,data)));
            token.ThrowIfCancellationRequested(); StableImage.Source=image; CurrentImage.Source=null; ProgressBar.Value=100; ProgressText.Text="Готово"; StatusText.Text=$"Готово за {watch.Elapsed.TotalSeconds:F2} сек.";
        }
        catch(OperationCanceledException){CurrentImage.Source=null;StatusText.Text="Рендер отменён";}
        catch(Exception ex){CurrentImage.Source=null;MessageBox.Show(this,ex.Message,DisplayName(_kind),MessageBoxButton.OK,MessageBoxImage.Error);}
        finally{_rendering=false;CancelButton.IsEnabled=false;RenderBadge.Visibility=Visibility.Collapsed;}
    }

    public async Task<BitmapSource> RenderStatePreviewAsync(DynamicSystemState state,int width,int height,CancellationToken token)
    { DynamicSystemState copy=state.Clone(); copy.SsaaFactor=1; copy.Iterations=Math.Min(copy.Iterations,_kind==DynamicSystemKind.Lyapunov?220:200_000); copy.Steps=Math.Min(copy.Steps,100_000); return await DynamicSystemRenderer.RenderAsync(copy,width,height,FindPalette(copy.PaletteName),token,null,null,false); }
    private DynamicPalette? FindPalette(string name)=>_palettes.FirstOrDefault(p=>p.Name==name)??_palettes.FirstOrDefault();

    private void Schedule(){if(!IsLoaded)return;_timer.Stop();_timer.Start();}
    private void Render_OnClick(object sender,RoutedEventArgs e){_timer.Stop();_cts?.Cancel();_=RenderAsync();}
    private void Cancel_OnClick(object sender,RoutedEventArgs e)=>_cts?.Cancel();
    private void Reset_OnClick(object sender,RoutedEventArgs e){DynamicSystemState defaults=DynamicSystemState.CreateDefault(_kind);_state.CenterX=defaults.CenterX;_state.CenterY=defaults.CenterY;_state.Zoom=1;if(_kind==DynamicSystemKind.Lyapunov){_state.AMin=defaults.AMin;_state.AMax=defaults.AMax;_state.BMin=defaults.BMin;_state.BMax=defaults.BMax;}SyncControls();Schedule();}
    private void PaletteBox_OnSelectionChanged(object sender,SelectionChangedEventArgs e){if(_syncing||PaletteBox.SelectedItem is not DynamicPalette p)return;_state.PaletteName=p.Name;Schedule();}
    private void Palette_OnClick(object sender,RoutedEventArgs e){if(_paletteStore is null)return;var dialog=new DynamicPaletteWindow(_paletteStore,_palettes,ActivePalette){Owner=this};if(dialog.ShowDialog()==true){_state.PaletteName=dialog.SelectedPalette?.Name??_state.PaletteName;LoadPalettes();Schedule();}}
    private void FractalColor_OnClick(object sender,RoutedEventArgs e){if(ColorSelectionService.Default.TrySelectColor(this,_state.FractalColor,out Color c)){_state.FractalColor=c;UpdateSwatches();Schedule();}}
    private void BackgroundColor_OnClick(object sender,RoutedEventArgs e){if(ColorSelectionService.Default.TrySelectColor(this,_state.BackgroundColor,out Color c)){_state.BackgroundColor=c;UpdateSwatches();Schedule();}}
    private void UpdateSwatches(){FractalColorSwatch.Background=new SolidColorBrush(_state.FractalColor);BackgroundColorSwatch.Background=new SolidColorBrush(_state.BackgroundColor);}

    private void Saves_OnClick(object sender,RoutedEventArgs e)
    {
        IReadOnlyList<DynamicSystemState> presets=_kind==DynamicSystemKind.Lyapunov?
        [new(){Kind=_kind,SaveName="Классический AB",Timestamp=DateTime.MinValue,PointOfInterestId="classic_ab",AMin=2.5,AMax=4,BMin=2.5,BMax=4,Pattern="AB",Iterations=320,TransientIterations=80,PaletteName="Классическая Ляпунова"},new(){Kind=_kind,SaveName="ABBA-структуры",Timestamp=DateTime.MinValue,PointOfInterestId="abba",AMin=3.2,AMax=4,BMin=2.6,BMax=3.6,Pattern="ABBA",Iterations=350,TransientIterations=100,PaletteName="Классическая Ляпунова"}]:[];
        SaveManagerWindow.Open(this,new SaveManagerConfiguration<DynamicSystemState>{WindowTitle=$"Сохранение/Загрузка: {DisplayName(_kind)}",FractalIdentifier=_kind.ToString(),LoadStates=_saves.Load,SaveStates=s=>_saves.Save(s),CaptureState=CaptureState,LoadState=LoadState,RenderPreviewAsync=RenderStatePreviewAsync,GetName=s=>s.SaveName,GetTimestamp=s=>s.Timestamp,GetDetails=s=>$"{s.Timestamp:g} · {Details(s)}",PointsOfInterest=presets});
    }

    private async void Export_OnClick(object sender,RoutedEventArgs e)
    {
        int w=Math.Max(1,(int)CanvasHost.ActualWidth),h=Math.Max(1,(int)CanvasHost.ActualHeight);var options=new MandelbrotExportWindow{Owner=this,ExportWidth=w,ExportHeight=h};if(options.ShowDialog()!=true)return;
        string ext=options.ExportFormat switch{MandelbrotExportFormat.Jpeg=>"jpg",MandelbrotExportFormat.Bmp=>"bmp",_=>"png"};var dialog=new SaveFileDialog{FileName=$"{_kind}_{DateTime.Now:yyyyMMdd_HHmmss}.{ext}",Filter=ext=="png"?"PNG|*.png":ext=="jpg"?"JPEG|*.jpg":"Bitmap|*.bmp"};if(dialog.ShowDialog(this)!=true)return;
        _cts?.Cancel();_cts?.Dispose();_cts=new();try{DynamicSystemState state=CaptureState("export");state.SsaaFactor=options.SsaaFactor;BitmapSource image=await DynamicSystemRenderer.RenderAsync(state,options.RenderWidth,options.RenderHeight,ActivePalette,_cts.Token,new Progress<int>(p=>{ProgressBar.Value=p;ProgressText.Text=$"Экспорт: {p}%";}),null,false);if(image.PixelWidth!=options.ExportWidth||image.PixelHeight!=options.ExportHeight)image=options.ProcessingMode==MandelbrotExportProcessingMode.Lanczos?await Task.Run(()=>BitmapResampler.ResizeLanczos3(image,options.ExportWidth,options.ExportHeight,_cts.Token),_cts.Token):BitmapResampler.ResizeBicubic(image,options.ExportWidth,options.ExportHeight);BitmapEncoder encoder=options.ExportFormat switch{MandelbrotExportFormat.Jpeg=>new JpegBitmapEncoder{QualityLevel=options.JpegQuality},MandelbrotExportFormat.Bmp=>new BmpBitmapEncoder(),_=>new PngBitmapEncoder()};encoder.Frames.Add(BitmapFrame.Create(image));await using FileStream stream=File.Create(dialog.FileName);encoder.Save(stream);StatusText.Text=$"Сохранено: {dialog.FileName}";}catch(OperationCanceledException){StatusText.Text="Экспорт отменён";}
    }

    private void CanvasHost_OnSizeChanged(object sender,SizeChangedEventArgs e)=>Schedule();
    private void CanvasHost_OnMouseWheel(object sender,MouseWheelEventArgs e){double k=e.Delta>0?.82:1.22;Point p=e.GetPosition(CanvasHost);if(_kind==DynamicSystemKind.Lyapunov){double ax=_state.AMin+p.X/Math.Max(1,CanvasHost.ActualWidth)*(_state.AMax-_state.AMin),by=_state.BMax-p.Y/Math.Max(1,CanvasHost.ActualHeight)*(_state.BMax-_state.BMin);_state.AMin=ax+(_state.AMin-ax)*k;_state.AMax=ax+(_state.AMax-ax)*k;_state.BMin=by+(_state.BMin-by)*k;_state.BMax=by+(_state.BMax-by)*k;}else{_state.Zoom=Math.Clamp(_state.Zoom/k,.01,1_000_000);}SyncControls();Schedule();}
    private void CanvasHost_OnMouseLeftButtonDown(object sender,MouseButtonEventArgs e){_cts?.Cancel();_panning=true;_panStart=e.GetPosition(CanvasHost);CanvasHost.CaptureMouse();}
    private void CanvasHost_OnMouseMove(object sender,MouseEventArgs e){if(!_panning)return;Point p=e.GetPosition(CanvasHost);double dx=(p.X-_panStart.X)/Math.Max(1,CanvasHost.ActualWidth),dy=(p.Y-_panStart.Y)/Math.Max(1,CanvasHost.ActualHeight);if(_kind==DynamicSystemKind.Lyapunov){double aw=_state.AMax-_state.AMin,bh=_state.BMax-_state.BMin;_state.AMin-=dx*aw;_state.AMax-=dx*aw;_state.BMin+=dy*bh;_state.BMax+=dy*bh;}else{double span=BaseSpan(_state)/_state.Zoom;_state.CenterX-=dx*span;_state.CenterY+=dy*span;}_panStart=p;SyncControls();}
    private void CanvasHost_OnMouseLeftButtonUp(object sender,MouseButtonEventArgs e){if(!_panning)return;_panning=false;CanvasHost.ReleaseMouseCapture();Schedule();}
    private void Toggle_OnClick(object sender,RoutedEventArgs e){_controls=!_controls;ControlsColumn.Width=_controls?new GridLength(330):new GridLength(0);ControlsHost.Visibility=_controls?Visibility.Visible:Visibility.Collapsed;ToggleButton.Content=_controls?"✕":"☰";Schedule();}
    private void Window_OnKeyDown(object sender,KeyEventArgs e){if(e.Key==Key.F11||e.Key==Key.Escape&&_fullscreen){if(!_fullscreen){_oldStyle=WindowStyle;_oldState=WindowState;WindowStyle=WindowStyle.None;WindowState=WindowState.Maximized;}else{WindowStyle=_oldStyle;WindowState=_oldState;}_fullscreen=!_fullscreen;}}
    private void Window_OnClosing(object? sender,System.ComponentModel.CancelEventArgs e){_timer.Stop();_cts?.Cancel();_cts?.Dispose();}

    private static string DisplayName(DynamicSystemKind k)=>k switch{DynamicSystemKind.Lyapunov=>"Экспонента Ляпунова",DynamicSystemKind.Lorenz=>"Аттрактор Лоренца",DynamicSystemKind.Rossler=>"Аттрактор Рёсслера",DynamicSystemKind.LogisticMap=>"Логистическое отображение",DynamicSystemKind.Bifurcation=>"Диаграмма бифуркации",DynamicSystemKind.Henon=>"Карта Хенона",_=>"Отображение Икэды"};
    private static string Details(DynamicSystemState s)=>s.Kind==DynamicSystemKind.Lyapunov?$"{s.Pattern} · {s.Iterations} итераций · {s.PaletteName}":$"Масштаб {s.Zoom:G5} · {Math.Max(s.Iterations,s.Steps):N0} итераций";
    private static double BaseSpan(DynamicSystemState s)=>s.Kind switch{DynamicSystemKind.Lorenz or DynamicSystemKind.Rossler=>80,DynamicSystemKind.Henon=>6,DynamicSystemKind.Ikeda=>Math.Max(.0001,s.RangeXMax-s.RangeXMin),_=>1};
    private static string Format(object? value)=>value switch{double d=>d.ToString("G15",CultureInfo.InvariantCulture),float f=>f.ToString("G9",CultureInfo.InvariantCulture),null=>string.Empty,_=>Convert.ToString(value,CultureInfo.InvariantCulture)??string.Empty};
    private static bool TryDouble(string text,out double value)=>double.TryParse(text,NumberStyles.Float,CultureInfo.InvariantCulture,out value)||double.TryParse(text,NumberStyles.Float,CultureInfo.CurrentCulture,out value);
    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject root) where T:DependencyObject{for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++){DependencyObject child=VisualTreeHelper.GetChild(root,i);if(child is T match)yield return match;foreach(T nested in FindVisualChildren<T>(child))yield return nested;}}
}
