using System.Windows;
using System.Windows.Controls;
using FractalExplorerWPF.Infrastructure;
using FractalExplorerWPF.Models;

namespace FractalExplorerWPF.Views;

public partial class PhoenixSavesWindow : Window
{
    private readonly PhoenixWindow _window; private readonly PhoenixSaveStore _store; private List<PhoenixState> _states = []; private CancellationTokenSource? _previewCts;
    public PhoenixSavesWindow(PhoenixWindow window, PhoenixSaveStore store) { InitializeComponent(); _window = window; _store = store; Refresh(); Closed += (_, _) => _previewCts?.Cancel(); }
    private void Refresh(PhoenixState? select = null) { try { _states = _store.Load().OrderByDescending(s => s.Timestamp).ToList(); SavesList.ItemsSource = _states; SavesList.SelectedItem = select is null ? _states.FirstOrDefault() : _states.FirstOrDefault(s => s.SaveName == select.SaveName); } catch (Exception ex) { MessageBox.Show(this, ex.Message, "Сохранения", MessageBoxButton.OK, MessageBoxImage.Error); } }
    private void Save_OnClick(object sender, RoutedEventArgs e)
    {
        string name = SaveNameBox.Text.Trim(); if (name.Length == 0) { MessageBox.Show(this, "Введите имя сохранения.", "Сохранение", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        int index = _states.FindIndex(s => s.SaveName.Equals(name, StringComparison.OrdinalIgnoreCase)); if (index >= 0 && MessageBox.Show(this, "Перезаписать сохранение?", "Сохранение", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        PhoenixState state = _window.CaptureState(name); if (index >= 0) _states[index] = state; else _states.Add(state); _store.Save(_states); Refresh(state);
    }
    private void Delete_OnClick(object sender, RoutedEventArgs e) { if (SavesList.SelectedItem is not PhoenixState state || MessageBox.Show(this, $"Удалить «{state.SaveName}»?", "Сохранения", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return; _states.Remove(state); _store.Save(_states); Refresh(); }
    private void Load_OnClick(object sender, RoutedEventArgs e) { if (SavesList.SelectedItem is PhoenixState state) { _window.LoadState(state); DialogResult = true; } }
    private async void SavesList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _previewCts?.Cancel(); if (SavesList.SelectedItem is not PhoenixState state) { PreviewImage.Source = null; DetailsText.Text = string.Empty; return; }
        SaveNameBox.Text = state.SaveName; DetailsText.Text = $"{state.Timestamp:g}\nC1: {state.C1Real:G8} {(state.C1Imaginary < 0 ? '−' : '+')} {Math.Abs(state.C1Imaginary):G8}i\nИтерации: {state.Iterations}\nМасштаб: {state.Zoom:G8}";
        _previewCts = new CancellationTokenSource(); try { PreviewImage.Source = await _window.RenderStatePreviewAsync(state, 420, 300, _previewCts.Token); } catch (OperationCanceledException) { } catch (Exception ex) { DetailsText.Text += $"\nОшибка превью: {ex.Message}"; }
    }
}
