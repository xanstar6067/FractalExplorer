using System.Windows;
using System.Windows.Media;
using Color = System.Windows.Media.Color;

namespace FractalExplorerWPF.Views;

public partial class ColorPickerWindow : Window
{
    public Color SelectedColor => Picker.SelectedColor;

    public ColorPickerWindow(Color initialColor)
    {
        InitializeComponent();
        Picker.Initialize(initialColor);
    }

    private void Ok_OnClick(object sender, RoutedEventArgs e) => DialogResult = true;
}
