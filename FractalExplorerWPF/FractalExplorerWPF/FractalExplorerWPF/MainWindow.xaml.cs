using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using FractalExplorerWPF.Models;

namespace FractalExplorerWPF;

public partial class MainWindow : Window
{
    private readonly IReadOnlyList<FractalCatalogItem> _catalog = FractalCatalog.Create();

    public MainWindow()
    {
        InitializeComponent();
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
        FractalPreview.Source = new BitmapImage(
            new Uri($"pack://application:,,,/{item.PreviewResourcePath}", UriKind.Absolute));
    }
}
