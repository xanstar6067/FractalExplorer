using System.Windows;
using System.Windows.Controls;
using FractalExplorerWPF.Infrastructure;
using FractalExplorerWPF.Models;

namespace FractalExplorerWPF.Views;

public partial class MandelbrotSavesWindow : Window
{
    private readonly MandelbrotWindow _fractalWindow;
    private readonly MandelbrotSaveStore _store;
    private List<MandelbrotState> _states = [];
    private CancellationTokenSource? _previewCts;

    public MandelbrotSavesWindow(MandelbrotWindow fractalWindow, MandelbrotSaveStore store)
    {
        InitializeComponent();
        _fractalWindow = fractalWindow;
        _store = store;
        RefreshStates();
        Closed += (_, _) => _previewCts?.Cancel();
    }

    private void RefreshStates(MandelbrotState? select = null)
    {
        try
        {
            _states = _store.Load().OrderByDescending(state => state.Timestamp).ToList();
            SavesList.ItemsSource = _states;
            SavesList.SelectedItem = select is null ? _states.FirstOrDefault()
                : _states.FirstOrDefault(state => state.SaveName == select.SaveName);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Ошибка загрузки сохранений", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Save_OnClick(object sender, RoutedEventArgs e)
    {
        string name = SaveNameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show(this, "Введите имя сохранения.", "Сохранение", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        int index = _states.FindIndex(state => state.SaveName.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (index >= 0 && MessageBox.Show(this, "Перезаписать существующее сохранение?", "Сохранение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        MandelbrotState state;
        try { state = _fractalWindow.CaptureState(name); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Параметры", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        if (index >= 0) _states[index] = state; else _states.Add(state);
        _store.Save(_states);
        RefreshStates(state);
    }

    private void Delete_OnClick(object sender, RoutedEventArgs e)
    {
        if (SavesList.SelectedItem is not MandelbrotState state) return;
        if (MessageBox.Show(this, $"Удалить «{state.SaveName}»?", "Сохранение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        _states.Remove(state); _store.Save(_states); RefreshStates();
    }

    private void Load_OnClick(object sender, RoutedEventArgs e)
    {
        if (SavesList.SelectedItem is not MandelbrotState state) return;
        _fractalWindow.LoadState(state); DialogResult = true;
    }

    private async void SavesList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _previewCts?.Cancel();
        if (SavesList.SelectedItem is not MandelbrotState state)
        {
            PreviewImage.Source = null; DetailsText.Text = string.Empty; return;
        }
        SaveNameBox.Text = state.SaveName;
        DetailsText.Text = $"{state.Timestamp:g}\nИтерации: {state.Iterations}\nМасштаб: {state.Zoom:G6}\n" +
                           $"Центр: {state.CenterX:G8}; {state.CenterY:G8}\nПалитра: {state.PaletteName}\nОкрашивание: {state.ColoringMode}";
        _previewCts = new CancellationTokenSource();
        try { PreviewImage.Source = await _fractalWindow.RenderStatePreviewAsync(state, 480, 320, _previewCts.Token); }
        catch (OperationCanceledException) { }
        catch (Exception ex) { DetailsText.Text += $"\nОшибка превью: {ex.Message}"; }
    }
}
