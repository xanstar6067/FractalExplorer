using System.Windows;
using System.Windows.Controls;
using FractalExplorerWPF.Infrastructure;
using FractalExplorerWPF.Models;

namespace FractalExplorerWPF.Views;

public partial class FlameSavesWindow : Window
{
    private readonly FlameWindow _window; private readonly FlameSaveStore _store; private List<FlameState> _states=[]; private CancellationTokenSource? _cts;
    public FlameSavesWindow(FlameWindow window,FlameSaveStore store){InitializeComponent();_window=window;_store=store;Refresh();Closed+=(_,_)=>_cts?.Cancel();}
    private void Refresh(FlameState? selected=null){_states=_store.Load().OrderByDescending(x=>x.Timestamp).ToList();SaveList.ItemsSource=null;SaveList.ItemsSource=_states;SaveList.SelectedItem=selected is null?_states.FirstOrDefault():_states.FirstOrDefault(x=>x.SaveName==selected.SaveName);}
    private void Save_OnClick(object sender,RoutedEventArgs e){string name=NameBox.Text.Trim();if(name.Length==0)return;FlameState state=_window.CaptureState(name);int index=_states.FindIndex(x=>x.SaveName.Equals(name,StringComparison.OrdinalIgnoreCase));if(index>=0)_states[index]=state;else _states.Add(state);_store.Save(_states);Refresh(state);}
    private void Delete_OnClick(object sender,RoutedEventArgs e){if(SaveList.SelectedItem is not FlameState state)return;_states.Remove(state);_store.Save(_states);Refresh();}
    private void Load_OnClick(object sender,RoutedEventArgs e){if(SaveList.SelectedItem is FlameState state){_window.LoadState(state.Clone());DialogResult=true;}}
    private async void SaveList_OnSelectionChanged(object sender,SelectionChangedEventArgs e){_cts?.Cancel();if(SaveList.SelectedItem is not FlameState state){Preview.Source=null;return;}NameBox.Text=state.SaveName;Details.Text=$"{state.Timestamp:g}\n{state.Samples:N0} сэмплов, {state.IterationsPerSample} итераций, прогрев {state.WarmupIterations}\nТрансформаций: {state.Transforms.Count}; экспозиция {state.Exposure:F2}; гамма {state.Gamma:F2}";_cts=new CancellationTokenSource();try{Preview.Source=await _window.RenderStatePreviewAsync(state.Clone(),450,310,_cts.Token);}catch(OperationCanceledException){}}
}
