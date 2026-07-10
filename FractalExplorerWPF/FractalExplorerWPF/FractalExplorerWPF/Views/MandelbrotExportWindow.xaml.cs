using System.Windows;
using System.Windows.Controls;

namespace FractalExplorerWPF.Views;

public partial class MandelbrotExportWindow : Window
{
    public int ExportWidth { get; set; } = 1920;
    public int ExportHeight { get; set; } = 1080;
    public int SsaaFactor { get; private set; } = 1;

    public MandelbrotExportWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => { WidthBox.Text = ExportWidth.ToString(); HeightBox.Text = ExportHeight.ToString(); };
    }

    private void Accept_OnClick(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(WidthBox.Text, out int width) || !int.TryParse(HeightBox.Text, out int height) ||
            width is < 1 or > 16_384 || height is < 1 or > 16_384)
        {
            MessageBox.Show(this, "Размер должен быть от 1 до 16384 пикселей.", "Размер изображения",
                MessageBoxButton.OK, MessageBoxImage.Warning); return;
        }
        int factor = int.Parse(((ComboBoxItem)SsaaBox.SelectedItem).Content.ToString()!);
        long pixels = (long)width * factor * height * factor;
        if (pixels > 80_000_000)
        {
            MessageBox.Show(this, "Размер и SSAA требуют слишком много памяти. Уменьшите разрешение или сглаживание.",
                "Размер изображения", MessageBoxButton.OK, MessageBoxImage.Warning); return;
        }
        ExportWidth = width; ExportHeight = height; SsaaFactor = factor; DialogResult = true;
    }
}
