using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FractalExplorerWPF.Controls;

public partial class SaveManagerControl : UserControl
{
    public event EventHandler? SelectionChanged;
    public event EventHandler? ItemDoubleClicked;
    public event EventHandler? SaveRequested;
    public event EventHandler? DeleteRequested;
    public event EventHandler? LoadRequested;
    public event EventHandler? RenderPreviewRequested;
    public event EventHandler? PointsOfInterestModeChanged;
    public event EventHandler? CloseRequested;

    public SaveManagerControl()
    {
        InitializeComponent();
    }

    public object? SelectedItem
    {
        get => SavesList.SelectedItem;
        set => SavesList.SelectedItem = value;
    }

    public string SaveName
    {
        get => SaveNameBox.Text;
        set => SaveNameBox.Text = value;
    }

    public bool IsPointsOfInterestMode => PointsOfInterestCheckBox.IsChecked == true;

    public void SetItems(IEnumerable items)
    {
        SavesList.ItemsSource = null;
        SavesList.ItemsSource = items;
    }

    public void SetPointsOfInterestAvailable(bool available)
    {
        PointsOfInterestCheckBox.Visibility = available ? Visibility.Visible : Visibility.Collapsed;
        if (!available) PointsOfInterestCheckBox.IsChecked = false;
    }

    public void SetPreview(ImageSource? image)
    {
        PreviewImage.Source = image;
        EmptyPreviewText.Visibility = image is null ? Visibility.Visible : Visibility.Collapsed;
    }

    public void SetDetails(string text) => DetailsText.Text = text;

    public void SetStatus(string text) => StatusText.Text = text;

    public void SetBusy(bool busy)
    {
        PreviewProgress.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
    }

    public void SetButtonStates(bool hasSelection, bool canEdit, bool isRendering)
    {
        LoadButton.IsEnabled = hasSelection;
        DeleteButton.IsEnabled = hasSelection && canEdit;
        SaveButton.IsEnabled = canEdit;
        SaveNameBox.IsEnabled = canEdit;
        RenderPreviewButton.IsEnabled = hasSelection && canEdit && !isRendering;
    }

    private void SavesList_OnSelectionChanged(object sender, SelectionChangedEventArgs e) =>
        SelectionChanged?.Invoke(this, EventArgs.Empty);

    private void SavesList_OnMouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) =>
        ItemDoubleClicked?.Invoke(this, EventArgs.Empty);

    private void SaveButton_OnClick(object sender, RoutedEventArgs e) => SaveRequested?.Invoke(this, EventArgs.Empty);
    private void DeleteButton_OnClick(object sender, RoutedEventArgs e) => DeleteRequested?.Invoke(this, EventArgs.Empty);
    private void LoadButton_OnClick(object sender, RoutedEventArgs e) => LoadRequested?.Invoke(this, EventArgs.Empty);
    private void RenderPreviewButton_OnClick(object sender, RoutedEventArgs e) => RenderPreviewRequested?.Invoke(this, EventArgs.Empty);
    private void PointsOfInterestCheckBox_OnChanged(object sender, RoutedEventArgs e) => PointsOfInterestModeChanged?.Invoke(this, EventArgs.Empty);
    private void CloseButton_OnClick(object sender, RoutedEventArgs e) => CloseRequested?.Invoke(this, EventArgs.Empty);
}
