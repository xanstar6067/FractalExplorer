using System.Windows;
using System.Windows.Controls;
using FractalExplorerWPF.Infrastructure;
using FractalExplorerWPF.Models;

namespace FractalExplorerWPF.Views;

public partial class NewtonSavesWindow : Window
{
    private readonly NewtonPoolsWindow _fractalWindow;
    private readonly NewtonSaveStore _store;
    private List<NewtonState> _states = [];
    private CancellationTokenSource? _previewCts;

    public NewtonSavesWindow(NewtonPoolsWindow fractalWindow, NewtonSaveStore store)
    {
        InitializeComponent();
        _fractalWindow = fractalWindow;
        _store = store;
        RefreshStates();
        Closed += (_, _) => _previewCts?.Cancel();
    }

    private void RefreshStates(NewtonState? select = null)
    {
        try
        {
            _states = _store.Load().OrderByDescending(state => state.Timestamp).ToList();
            SavesList.ItemsSource = _states;
            SavesList.SelectedItem = select is null ? _states.FirstOrDefault() : _states.FirstOrDefault(state => state.SaveName == select.SaveName);
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
        int existing = _states.FindIndex(state => state.SaveName.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (existing >= 0 && MessageBox.Show(this, "Перезаписать существующее сохранение?", "Сохранение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        NewtonState state = _fractalWindow.CaptureState(name);
        if (existing >= 0) _states[existing] = state; else _states.Add(state);
        _store.Save(_states);
        RefreshStates(state);
    }

    private void Delete_OnClick(object sender, RoutedEventArgs e)
    {
        if (SavesList.SelectedItem is not NewtonState state || MessageBox.Show(this, $"Удалить «{state.SaveName}»?", "Сохранения",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        _states.Remove(state);
        _store.Save(_states);
        RefreshStates();
    }

    private void Load_OnClick(object sender, RoutedEventArgs e)
    {
        if (SavesList.SelectedItem is not NewtonState state) return;
        _fractalWindow.LoadState(state);
        DialogResult = true;
    }

    private async void SavesList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _previewCts?.Cancel();
        if (SavesList.SelectedItem is not NewtonState state)
        {
            PreviewImage.Source = null;
            DetailsText.Text = string.Empty;
            return;
        }
        SaveNameBox.Text = state.SaveName;
        DetailsText.Text = $"{state.Timestamp:g}\nМетод: {state.IterationMethod}\nФормула: {state.Formula}\nИтерации: {state.MaxIterations}\nМасштаб: {state.Zoom:0.####}";
        _previewCts = new CancellationTokenSource();
        try { PreviewImage.Source = await _fractalWindow.RenderStatePreviewAsync(state, 420, 300, _previewCts.Token); }
        catch (OperationCanceledException) { }
        catch (Exception ex) { DetailsText.Text += $"\nОшибка превью: {ex.Message}"; }
    }
}
