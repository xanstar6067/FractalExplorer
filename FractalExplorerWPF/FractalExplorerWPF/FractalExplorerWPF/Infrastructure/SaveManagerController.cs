using System.Globalization;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using FractalExplorerWPF.Controls;

namespace FractalExplorerWPF.Infrastructure;

public sealed class SaveManagerConfiguration<TState> where TState : class
{
    public required string WindowTitle { get; init; }
    public required string FractalIdentifier { get; init; }
    public required Func<List<TState>> LoadStates { get; init; }
    public required Action<IReadOnlyCollection<TState>> SaveStates { get; init; }
    public required Func<string, TState> CaptureState { get; init; }
    public required Func<int, int, BitmapSource?> CapturePreview { get; init; }
    public required Action<TState> LoadState { get; init; }
    public required Func<TState, int, int, CancellationToken, IProgress<int>?, Task<BitmapSource>> RenderPreviewAsync { get; init; }
    public required Func<TState, string> GetName { get; init; }
    public required Func<TState, DateTime> GetTimestamp { get; init; }
    public required Func<TState, string> GetDetails { get; init; }
    public IReadOnlyList<TState> PointsOfInterest { get; init; } = [];
    public int PreviewWidth { get; init; } = 480;
    public int PreviewHeight { get; init; } = 320;
}

public sealed record SaveManagerEntry<TState>(TState State, string DisplayName, bool IsPointOfInterest)
    where TState : class;

public sealed class SaveManagerController<TState> : IDisposable where TState : class
{
    private readonly Window _window;
    private readonly SaveManagerControl _view;
    private readonly SaveManagerConfiguration<TState> _configuration;
    private List<TState> _states = [];
    private List<SaveManagerEntry<TState>> _entries = [];
    private CancellationTokenSource? _previewCts;
    private bool _isRendering;
    private bool _disposed;

    public SaveManagerController(Window window, SaveManagerControl view, SaveManagerConfiguration<TState> configuration)
    {
        _window = window;
        _view = view;
        _configuration = configuration;
        _window.Title = configuration.WindowTitle;

        _view.SelectionChanged += View_OnSelectionChanged;
        _view.ItemDoubleClicked += View_OnItemDoubleClicked;
        _view.SaveRequested += View_OnSaveRequested;
        _view.DeleteRequested += View_OnDeleteRequested;
        _view.LoadRequested += View_OnLoadRequested;
        _view.RenderPreviewRequested += View_OnRenderPreviewRequested;
        _view.CancelPreviewRequested += View_OnCancelPreviewRequested;
        _view.PointsOfInterestModeChanged += View_OnPointsOfInterestModeChanged;
        _view.CloseRequested += View_OnCloseRequested;
        _view.SetPointsOfInterestAvailable(configuration.PointsOfInterest.Count > 0);
        RefreshStates();
    }

    private void RefreshStates(string? selectName = null)
    {
        try
        {
            _states = _configuration.LoadStates()
                .OrderByDescending(_configuration.GetTimestamp)
                .ToList();
            PopulateEntries(selectName);
        }
        catch (Exception ex)
        {
            MessageBox.Show(_window, ex.Message, "Ошибка загрузки сохранений", MessageBoxButton.OK, MessageBoxImage.Error);
            _states = [];
            PopulateEntries();
        }
    }

    private void PopulateEntries(string? selectName = null)
    {
        bool pointsMode = _view.IsPointsOfInterestMode;
        IEnumerable<TState> source = pointsMode
            ? _configuration.PointsOfInterest.OrderBy(_configuration.GetName)
            : _states;

        _entries = source.Select(state => new SaveManagerEntry<TState>(
            state,
            pointsMode
                ? _configuration.GetName(state)
                : $"{_configuration.GetName(state)} ({_configuration.GetTimestamp(state):yyyy-MM-dd HH:mm:ss})",
            pointsMode)).ToList();

        _view.SetItems(_entries);
        _view.SelectedItem = selectName is null
            ? _entries.FirstOrDefault()
            : _entries.FirstOrDefault(entry => _configuration.GetName(entry.State).Equals(selectName, StringComparison.OrdinalIgnoreCase));

        if (_entries.Count == 0) ClearSelection();
        UpdateButtonStates();
    }

    private void View_OnSelectionChanged(object? sender, EventArgs e)
    {
        bool wasRendering = _isRendering;
        CancelPreview();
        if (SelectedEntry is not { } entry)
        {
            ClearSelection();
            UpdateButtonStates();
            return;
        }

        _view.SaveName = _configuration.GetName(entry.State);
        _view.SetDetails(_configuration.GetDetails(entry.State));
        _view.SetStatus(wasRendering ? "Рендер отменён при смене сохранения." : string.Empty);
        UpdateButtonStates();

        if (TryLoadCachedPreview(entry, out BitmapSource? cached))
        {
            _view.SetPreview(cached);
            return;
        }

        _view.SetPreview(null, "Превью отсутствует. Нажмите «Рендер превью».");
    }

    private void View_OnItemDoubleClicked(object? sender, EventArgs e) => LoadSelected();

    private void View_OnSaveRequested(object? sender, EventArgs e)
    {
        if (_view.IsPointsOfInterestMode) return;
        string name = _view.SaveName.Trim();
        if (name.Length == 0)
        {
            MessageBox.Show(_window, "Введите имя сохранения.", "Сохранение", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        int existingIndex = _states.FindIndex(state =>
            _configuration.GetName(state).Equals(name, StringComparison.OrdinalIgnoreCase));
        if (existingIndex >= 0 && MessageBox.Show(_window,
                $"Сохранение с именем «{name}» уже существует. Перезаписать?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            CancelPreview();
            UpdateButtonStates();
            TState state = _configuration.CaptureState(name);
            BitmapSource? snapshot = null;
            string? captureError = null;
            try
            {
                snapshot = _configuration.CapturePreview(_configuration.PreviewWidth, _configuration.PreviewHeight);
            }
            catch (Exception ex)
            {
                captureError = ex.Message;
            }

            TState? previous = existingIndex >= 0 ? _states[existingIndex] : null;
            var updated = new List<TState>(_states);
            if (existingIndex >= 0) updated[existingIndex] = state;
            else updated.Add(state);
            _configuration.SaveStates(updated);
            _states = updated;
            var savedEntry = new SaveManagerEntry<TState>(state, name, false);
            bool previewSaved = snapshot is not null && SaveCachedPreview(savedEntry, snapshot);
            if (previous is not null && GetPreviewPath(new(previous, name, false)) != GetPreviewPath(savedEntry))
                DeleteCachedPreview(new(previous, name, false));
            // A failed capture must not leave an unrelated image when overwriting the same key.
            if (!previewSaved) DeleteCachedPreview(savedEntry);
            RefreshStates(name);
            if (snapshot is not null) _view.SetPreview(snapshot);
            _view.SetStatus(previewSaved ? "Сохранено с текущим кадром."
                : captureError is not null ? $"Сохранено без превью: {captureError}"
                : snapshot is null ? "Сохранено без превью: на полотне нет кадра."
                : "Состояние сохранено, но PNG-превью записать не удалось.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(_window, ex.Message, "Ошибка сохранения", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void View_OnDeleteRequested(object? sender, EventArgs e)
    {
        if (SelectedEntry is not { IsPointOfInterest: false } entry) return;
        string name = _configuration.GetName(entry.State);
        if (MessageBox.Show(_window, $"Вы уверены, что хотите удалить сохранение «{name}»?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            CancelPreview();
            UpdateButtonStates();
            var updated = _states.Where(state => !ReferenceEquals(state, entry.State)).ToList();
            _configuration.SaveStates(updated);
            _states = updated;
            DeleteCachedPreview(entry);
            RefreshStates();
        }
        catch (Exception ex)
        {
            MessageBox.Show(_window, ex.Message, "Ошибка удаления", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void View_OnLoadRequested(object? sender, EventArgs e) => LoadSelected();

    private void LoadSelected()
    {
        if (SelectedEntry is not { } entry) return;
        try
        {
            _configuration.LoadState(entry.State);
            _window.DialogResult = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(_window, ex.Message, "Ошибка загрузки", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void View_OnRenderPreviewRequested(object? sender, EventArgs e) =>
        await RenderSelectedPreviewAsync();

    private void View_OnCancelPreviewRequested(object? sender, EventArgs e)
    {
        if (_previewCts is not { } cts || cts.IsCancellationRequested) return;
        cts.Cancel();
        _view.SetCancelling();
        _view.SetStatus("Отмена рендера...");
    }

    private void View_OnPointsOfInterestModeChanged(object? sender, EventArgs e)
    {
        PopulateEntries();
    }

    private void View_OnCloseRequested(object? sender, EventArgs e) => _window.Close();

    private async Task RenderSelectedPreviewAsync()
    {
        if (_disposed || _isRendering || SelectedEntry is not { } entry) return;

        CancelPreview();
        var cts = new CancellationTokenSource();
        _previewCts = cts;
        _isRendering = true;
        _view.SetBusy(true);
        _view.SetStatus("Рендер превью...");
        UpdateButtonStates();
        var stopwatch = Stopwatch.StartNew();
        int? percent = null;
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        timer.Tick += (_, _) =>
        {
            if (ReferenceEquals(_previewCts, cts)) _view.SetRenderProgress(percent, stopwatch.Elapsed);
        };
        var progress = new Progress<int>(value =>
        {
            if (!ReferenceEquals(_previewCts, cts) || cts.IsCancellationRequested) return;
            percent = Math.Max(percent ?? 0, Math.Clamp(value, 0, 100));
            _view.SetRenderProgress(percent, stopwatch.Elapsed);
        });
        timer.Start();

        try
        {
            BitmapSource preview = await _configuration.RenderPreviewAsync(
                entry.State, _configuration.PreviewWidth, _configuration.PreviewHeight, cts.Token, progress);
            cts.Token.ThrowIfCancellationRequested();
            if (!ReferenceEquals(_previewCts, cts)) return;

            if (!preview.IsFrozen && preview.CanFreeze) preview.Freeze();
            _view.SetPreview(preview);
            bool cacheSaved = SaveCachedPreview(entry, preview);
            _view.SetStatus(cacheSaved
                ? $"Превью обновлено за {stopwatch.Elapsed.TotalSeconds:F1} сек."
                : "Превью показано, но PNG записать не удалось.");
        }
        catch (OperationCanceledException)
        {
            if (ReferenceEquals(_previewCts, cts)) _view.SetStatus("Рендер отменён. Превью не изменено.");
        }
        catch (Exception ex)
        {
            if (ReferenceEquals(_previewCts, cts))
            {
                _view.SetStatus($"Ошибка рендера превью: {ex.Message}. Прежнее превью сохранено.");
            }
        }
        finally
        {
            timer.Stop();
            stopwatch.Stop();
            if (ReferenceEquals(_previewCts, cts))
            {
                _previewCts = null;
                _isRendering = false;
                _view.SetBusy(false);
                UpdateButtonStates();
            }
            cts.Dispose();
        }
    }

    private SaveManagerEntry<TState>? SelectedEntry => _view.SelectedItem as SaveManagerEntry<TState>;

    private void UpdateButtonStates()
    {
        bool hasSelection = SelectedEntry is not null;
        _view.SetButtonStates(hasSelection, !_view.IsPointsOfInterestMode, _isRendering);
    }

    private void ClearSelection()
    {
        _view.SaveName = string.Empty;
        _view.SetPreview(null);
        _view.SetDetails(string.Empty);
        _view.SetStatus(string.Empty);
    }

    private void CancelPreview()
    {
        CancellationTokenSource? cts = _previewCts;
        _previewCts = null;
        if (cts is null) return;
        try { cts.Cancel(); }
        catch (ObjectDisposedException) { }
        _isRendering = false;
        _view.SetBusy(false);
    }

    private string GetPreviewPath(SaveManagerEntry<TState> entry)
    {
        TState state = entry.State;
        string directory = Path.Combine(AppPaths.SavesDirectory, "SavePrevData", MakeSafeFileName(_configuration.FractalIdentifier));
        if (entry.IsPointOfInterest) directory = Path.Combine(directory, "PointsOfInterest");
        string name = MakeSafeFileName(_configuration.GetName(state));
        string timestamp = _configuration.GetTimestamp(state).ToString("yyyyMMdd_HHmmss_fffffff", CultureInfo.InvariantCulture);
        return Path.Combine(directory, $"{name}_{timestamp}.png");
    }

    private bool TryLoadCachedPreview(SaveManagerEntry<TState> entry, out BitmapSource? preview)
    {
        preview = null;
        string path = GetPreviewPath(entry);
        if (!File.Exists(path)) return false;

        try
        {
            using FileStream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();
            preview = image;
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private bool SaveCachedPreview(SaveManagerEntry<TState> entry, BitmapSource preview)
    {
        string path = GetPreviewPath(entry);
        string? directory = Path.GetDirectoryName(path);
        if (directory is null) return false;

        try
        {
            Directory.CreateDirectory(directory);
            string temporaryPath = path + $".tmp_{Guid.NewGuid():N}";
            try
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(preview));
                using (FileStream stream = File.Create(temporaryPath)) encoder.Save(stream);
                File.Move(temporaryPath, path, true);
            }
            finally
            {
                if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
            }
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private void DeleteCachedPreview(SaveManagerEntry<TState> entry)
    {
        try
        {
            string path = GetPreviewPath(entry);
            if (File.Exists(path)) File.Delete(path);
        }
        catch (Exception)
        {
            // Ошибка очистки превью не должна мешать сохранению или удалению состояния.
        }
    }

    private static string MakeSafeFileName(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "Save";
        char[] invalid = Path.GetInvalidFileNameChars();
        string safe = new(value.Select(character => invalid.Contains(character) ? '_' : character).ToArray());
        return string.IsNullOrWhiteSpace(safe) ? "Save" : safe.Trim();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        CancelPreview();
        _view.SelectionChanged -= View_OnSelectionChanged;
        _view.ItemDoubleClicked -= View_OnItemDoubleClicked;
        _view.SaveRequested -= View_OnSaveRequested;
        _view.DeleteRequested -= View_OnDeleteRequested;
        _view.LoadRequested -= View_OnLoadRequested;
        _view.RenderPreviewRequested -= View_OnRenderPreviewRequested;
        _view.CancelPreviewRequested -= View_OnCancelPreviewRequested;
        _view.PointsOfInterestModeChanged -= View_OnPointsOfInterestModeChanged;
        _view.CloseRequested -= View_OnCloseRequested;
    }
}
