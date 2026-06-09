using System.Windows;
using System.Windows.Controls;
using FractalExplorerWPF.Infrastructure;
using FractalExplorerWPF.Models;

namespace FractalExplorerWPF.Views;

public partial class SerpinskySavesWindow : Window
{
    private readonly SerpinskyWindow _fractalWindow;
    private readonly SerpinskySaveStore _store;
    private List<SerpinskySaveState> _states = [];
    private CancellationTokenSource? _previewCts;

    public SerpinskySavesWindow(SerpinskyWindow fractalWindow, SerpinskySaveStore store)
    {
        InitializeComponent();
        _fractalWindow = fractalWindow;
        _store = store;
        RefreshStates();
        Closed += (_, _) => _previewCts?.Cancel();
    }

    private void RefreshStates(SerpinskySaveState? select = null)
    {
        try
        {
            _states = _store.Load().OrderByDescending(state => state.Timestamp).ToList();
            SavesList.ItemsSource = _states;
            SavesList.SelectedItem = select is null
                ? _states.FirstOrDefault()
                : _states.FirstOrDefault(state => state.SaveName == select.SaveName);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Ошибка загрузки сохранений",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Save_OnClick(object sender, RoutedEventArgs e)
    {
        string name = SaveNameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show(this, "Введите имя сохранения.", "Сохранение",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        int existingIndex = _states.FindIndex(state =>
            state.SaveName.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (existingIndex >= 0 &&
            MessageBox.Show(this, "Перезаписать существующее сохранение?", "Сохранение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        SerpinskySaveState state = _fractalWindow.CaptureState(name);
        if (existingIndex >= 0)
        {
            _states[existingIndex] = state;
        }
        else
        {
            _states.Add(state);
        }
        _store.Save(_states);
        RefreshStates(state);
    }

    private void Delete_OnClick(object sender, RoutedEventArgs e)
    {
        if (SavesList.SelectedItem is not SerpinskySaveState state)
        {
            return;
        }
        if (MessageBox.Show(this, $"Удалить «{state.SaveName}»?", "Сохранения",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        _states.Remove(state);
        _store.Save(_states);
        RefreshStates();
    }

    private void Load_OnClick(object sender, RoutedEventArgs e)
    {
        if (SavesList.SelectedItem is not SerpinskySaveState state)
        {
            return;
        }
        _fractalWindow.LoadState(state);
        DialogResult = true;
    }

    private async void SavesList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _previewCts?.Cancel();
        if (SavesList.SelectedItem is not SerpinskySaveState state)
        {
            PreviewImage.Source = null;
            DetailsText.Text = string.Empty;
            return;
        }

        SaveNameBox.Text = state.SaveName;
        DetailsText.Text =
            $"{state.Timestamp:g}\nРежим: {state.RenderMode}\nИтерации: {state.Iterations}\nМасштаб: {state.Zoom:0.####}";
        _previewCts = new CancellationTokenSource();
        try
        {
            PreviewImage.Source = await _fractalWindow.RenderStatePreviewAsync(
                state, 400, 300, _previewCts.Token);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            DetailsText.Text += $"\nОшибка превью: {ex.Message}";
        }
    }
}
