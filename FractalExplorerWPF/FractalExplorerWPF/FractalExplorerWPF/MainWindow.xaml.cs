using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using FractalExplorerWPF.Models;
using FractalExplorerWPF.Views;
using FractalExplorerWPF.Infrastructure;
using FractalExplorerWPF.Theming;

namespace FractalExplorerWPF;

public partial class MainWindow : Window
{
    private readonly IReadOnlyList<FractalCatalogItem> _catalog = FractalCatalog.Create();
    private FractalCatalogItem? _selectedItem;
    private bool _initializingRenderPattern = true;
    private bool _updatingThemes;

    public MainWindow()
    {
        InitializeComponent();
        int patternIndex = RenderPatternPreferenceStore.Load();
        RenderPatternSelector.SelectedIndex = patternIndex;
        RenderPatternSettings.SelectedPattern = (TileSchedulingStrategy)patternIndex;
        _initializingRenderPattern = false;
        ThemeManager.ThemeChanged += ThemeManager_OnThemeChanged;
        ThemeManager.ThemesChanged += ThemeManager_OnThemesChanged;
        Closed += (_, _) =>
        {
            ThemeManager.ThemeChanged -= ThemeManager_OnThemeChanged;
            ThemeManager.ThemesChanged -= ThemeManager_OnThemesChanged;
        };
        ReloadThemeSelector();
        PopulateCatalog();
    }

    private void ReloadThemeSelector()
    {
        _updatingThemes = true;
        IReadOnlyList<ThemeDefinition> themes = ThemeManager.GetAllThemes();
        ThemeSelector.ItemsSource = themes;
        ThemeSelector.SelectedItem = themes.FirstOrDefault(theme =>
            string.Equals(theme.Id, ThemeManager.CurrentThemeId, StringComparison.OrdinalIgnoreCase));
        _updatingThemes = false;
    }

    private void ThemeSelector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_updatingThemes && ThemeSelector.SelectedItem is ThemeDefinition theme)
            ThemeManager.SetTheme(theme.Id);
    }

    private void ThemeEditor_OnClick(object sender, RoutedEventArgs e)
    {
        new ThemeEditorWindow { Owner = this }.ShowDialog();
        ReloadThemeSelector();
    }

    private void ThemeManager_OnThemeChanged(object? sender, EventArgs e) => ReloadThemeSelector();
    private void ThemeManager_OnThemesChanged(object? sender, EventArgs e) => ReloadThemeSelector();

    private void PopulateCatalog()
    {
        foreach (IGrouping<string, FractalCatalogItem> family in _catalog.GroupBy(item => item.Family))
        {
            var familyNode = new TreeViewItem
            {
                Header = family.Key,
                IsExpanded = FractalTree.Items.Count == 0
            };

            foreach (FractalCatalogItem item in family)
            {
                familyNode.Items.Add(new TreeViewItem
                {
                    Header = item.DisplayName,
                    Tag = item
                });
            }

            FractalTree.Items.Add(familyNode);
        }

        if (FractalTree.Items[0] is TreeViewItem firstFamily &&
            firstFamily.Items[0] is TreeViewItem firstFractal)
        {
            firstFractal.IsSelected = true;
        }
    }

    private void FractalTree_OnSelectedItemChanged(
        object sender,
        RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is not TreeViewItem { Tag: FractalCatalogItem item })
        {
            return;
        }

        FractalName.Text = item.DisplayName;
        FractalDescription.Text = item.Description;
        _selectedItem = item;
        LaunchButton.IsEnabled = item.LaunchKey is not null;
        LaunchButton.Content = item.LaunchKey is not null
            ? "Запустить"
            : "Запуск будет перенесён позже";
        FractalPreview.Source = new BitmapImage(
            new Uri($"pack://application:,,,/{item.PreviewResourcePath}", UriKind.Absolute));
    }

    private void LaunchButton_OnClick(object sender, RoutedEventArgs e) => LaunchSelected();

    private void RenderPatternSelector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (RenderPatternSelector.SelectedIndex is >= 0 and <= 7)
        {
            RenderPatternSettings.SelectedPattern =
                (TileSchedulingStrategy)RenderPatternSelector.SelectedIndex;
            if (!_initializingRenderPattern)
                RenderPatternPreferenceStore.Save(RenderPatternSelector.SelectedIndex);
        }
    }

    private void FractalTree_OnMouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (_selectedItem?.LaunchKey is not null)
        {
            LaunchSelected();
            e.Handled = true;
        }
    }

    private void LaunchSelected()
    {
        if (_selectedItem?.LaunchKey == "JuliaGallery")
        {
            new JuliaGalleryWindow(MandelbrotVariant.Julia) { Owner = this }.Show();
            return;
        }

        if (_selectedItem?.LaunchKey == "JuliaBurningShipGallery")
        {
            new JuliaGalleryWindow(MandelbrotVariant.JuliaBurningShip) { Owner = this }.Show();
            return;
        }

        if (_selectedItem?.LaunchKey == "Serpinsky")
        {
            new SerpinskyWindow { Owner = this }.Show();
            return;
        }

        if (_selectedItem?.LaunchKey == "NewtonPools")
        {
            new NewtonPoolsWindow { Owner = this }.Show();
            return;
        }

        if (Enum.TryParse(_selectedItem?.LaunchKey, out MandelbrotVariant variant))
        {
            new MandelbrotWindow(variant) { Owner = this }.Show();
        }
    }
}
