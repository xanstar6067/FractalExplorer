using System.Windows;
using System.Windows.Controls;

namespace FractalExplorerWPF.Views;

public partial class SerpinskyExportWindow : Window
{
    public int ExportWidth { get; set; } = 1920;
    public int ExportHeight { get; set; } = 1080;
    public int SsaaFactor { get; private set; } = 1;

    public SerpinskyExportWindow()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            WidthBox.Text = ExportWidth.ToString();
            HeightBox.Text = ExportHeight.ToString();
        };
    }

    private void Accept_OnClick(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(WidthBox.Text, out int width) ||
            !int.TryParse(HeightBox.Text, out int height) ||
            width is < 1 or > 16_384 ||
            height is < 1 or > 16_384)
        {
            MessageBox.Show(this, "Размер должен быть от 1 до 16384 пикселей.", "Размер изображения",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        ExportWidth = width;
        ExportHeight = height;
        SsaaFactor = int.Parse(((ComboBoxItem)SsaaBox.SelectedItem).Content.ToString()!);
        long renderPixels = (long)width * SsaaFactor * height * SsaaFactor;
        if (renderPixels > 100_000_000)
        {
            MessageBox.Show(this,
                "Выбранные размер и SSAA требуют слишком много памяти. Уменьшите разрешение или SSAA.",
                "Размер изображения", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        DialogResult = true;
    }
}
