using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using FractalExplorerWPF.Models;
using FractalExplorerWPF.Views;
using FractalExplorerWPF.Infrastructure;

namespace FractalExplorerWPF;

public partial class MainWindow : Window
{
    private readonly IReadOnlyList<FractalCatalogItem> _catalog = FractalCatalog.Create();
    private FractalCatalogItem? _selectedItem;
    private bool _initializingRenderPattern = true;

    public MainWindow()
    {
        InitializeComponent();
        int patternIndex = RenderPatternPreferenceStore.Load();
        RenderPatternSelector.SelectedIndex = patternIndex;
        RenderPatternSettings.SelectedPattern = (TileSchedulingStrategy)patternIndex;
        _initializingRenderPattern = false;
        PopulateCatalog();
    }

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
        if (_selectedItem?.LaunchKey == "Serpinsky")
        {
            new SerpinskyWindow { Owner = this }.Show();
            return;
        }

        if (Enum.TryParse(_selectedItem?.LaunchKey, out MandelbrotVariant variant))
        {
            new MandelbrotWindow(variant) { Owner = this }.Show();
        }
    }
}
