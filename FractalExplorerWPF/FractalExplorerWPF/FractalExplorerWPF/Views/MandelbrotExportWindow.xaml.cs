using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace FractalExplorerWPF.Views;

public enum MandelbrotExportFormat
{
    Png,
    Jpeg,
    Bmp
}

public enum MandelbrotExportProcessingMode
{
    Ssaa,
    Bicubic,
    Lanczos
}

public partial class MandelbrotExportWindow : Window
{
    private static readonly (string Label, double Value)[] SsaaFactors =
    [
        ("Выкл. (1×)", 1), ("Низкое (2×)", 2), ("Высокое (4×)", 4),
        ("Ультра (8×)", 8), ("Экстрим (10×)", 10)
    ];

    private static readonly (string Label, double Value)[] BicubicFactors =
    [
        ("1,1× — минимальное", 1.1), ("1,2× — очень мягкое", 1.2),
        ("1,3× — мягкое", 1.3), ("1,4× — умеренное", 1.4),
        ("1,5× — стандартное", 1.5), ("2× — сильное", 2),
        ("2,5× — экстремальное", 2.5)
    ];

    private static readonly (string Label, double Value)[] LanczosFactors =
    [
        ("4× — суперсемплинг Ultra", 4), ("2× — суперсемплинг High", 2),
        ("1,5× — суперсемплинг Medium", 1.5), ("0,75× — апскейл Quality", 0.75),
        ("0,5× — апскейл Balanced", 0.5), ("0,25× — апскейл Performance", 0.25)
    ];

    public int ExportWidth { get; set; } = 1920;
    public int ExportHeight { get; set; } = 1080;
    public int RenderWidth { get; private set; } = 1920;
    public int RenderHeight { get; private set; } = 1080;
    public int SsaaFactor { get; private set; } = 1;
    public double ProcessingFactor { get; private set; } = 1;
    public int JpegQuality { get; private set; } = 95;
    public MandelbrotExportFormat ExportFormat { get; private set; } = MandelbrotExportFormat.Png;
    public MandelbrotExportProcessingMode ProcessingMode { get; private set; } = MandelbrotExportProcessingMode.Ssaa;

    public MandelbrotExportWindow()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            WidthBox.Text = ExportWidth.ToString(CultureInfo.InvariantCulture);
            HeightBox.Text = ExportHeight.ToString(CultureInfo.InvariantCulture);
            RefreshQualityFactors();
        };
    }

    private void Preset_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string value }) return;
        string[] parts = value.Split(',');
        WidthBox.Text = parts[0];
        HeightBox.Text = parts[1];
    }

    private void Rotate_OnClick(object sender, RoutedEventArgs e) =>
        (WidthBox.Text, HeightBox.Text) = (HeightBox.Text, WidthBox.Text);

    private void FormatBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsInitialized || JpegQualityPanel is null) return;
        JpegQualityPanel.Visibility = FormatBox.SelectedIndex == 1 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void JpegQualitySlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (JpegQualityValue is not null) JpegQualityValue.Text = $"{e.NewValue:F0}%";
    }

    private void ProcessingModeBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (QualityFactorBox is not null) RefreshQualityFactors();
    }

    private void RefreshQualityFactors()
    {
        MandelbrotExportProcessingMode mode = (MandelbrotExportProcessingMode)Math.Max(0, ProcessingModeBox.SelectedIndex);
        (string Label, double Value)[] values = mode switch
        {
            MandelbrotExportProcessingMode.Bicubic => BicubicFactors,
            MandelbrotExportProcessingMode.Lanczos => LanczosFactors,
            _ => SsaaFactors
        };
        QualityFactorBox.ItemsSource = values.Select(item => item.Label).ToArray();
        QualityFactorBox.SelectedIndex = mode switch
        {
            MandelbrotExportProcessingMode.Bicubic => 4,
            MandelbrotExportProcessingMode.Lanczos => 1,
            _ => 0
        };
        QualityFactorLabel.Text = mode switch
        {
            MandelbrotExportProcessingMode.Bicubic => "Коэффициент апскейла",
            MandelbrotExportProcessingMode.Lanczos => "Коэффициент Ланцоша",
            _ => "Сглаживание (SSAA)"
        };
        QualityHint.Text = mode switch
        {
            MandelbrotExportProcessingMode.Bicubic => "Быстрый черновой рендер в меньшем размере с качественным увеличением.",
            MandelbrotExportProcessingMode.Lanczos => "Рендер в масштабе коэффициента и финальное масштабирование фильтром Ланцоша 3.",
            _ => "Рендер нескольких подвыборок на пиксель с последующим уменьшением."
        };
    }

    private void Accept_OnClick(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(WidthBox.Text, out int width) || !int.TryParse(HeightBox.Text, out int height) ||
            width is < 1 or > 16_384 || height is < 1 or > 16_384)
        {
            MessageBox.Show(this, "Размер должен быть от 1 до 16384 пикселей.", "Размер изображения",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        ProcessingMode = (MandelbrotExportProcessingMode)Math.Max(0, ProcessingModeBox.SelectedIndex);
        (string Label, double Value)[] factors = ProcessingMode switch
        {
            MandelbrotExportProcessingMode.Bicubic => BicubicFactors,
            MandelbrotExportProcessingMode.Lanczos => LanczosFactors,
            _ => SsaaFactors
        };
        int factorIndex = Math.Clamp(QualityFactorBox.SelectedIndex, 0, factors.Length - 1);
        ProcessingFactor = factors[factorIndex].Value;
        SsaaFactor = ProcessingMode == MandelbrotExportProcessingMode.Ssaa ? (int)ProcessingFactor : 1;
        RenderWidth = ProcessingMode switch
        {
            MandelbrotExportProcessingMode.Bicubic => Math.Max(1, (int)Math.Round(width / ProcessingFactor)),
            MandelbrotExportProcessingMode.Lanczos => Math.Max(1, (int)Math.Round(width * ProcessingFactor)),
            _ => width
        };
        RenderHeight = ProcessingMode switch
        {
            MandelbrotExportProcessingMode.Bicubic => Math.Max(1, (int)Math.Round(height / ProcessingFactor)),
            MandelbrotExportProcessingMode.Lanczos => Math.Max(1, (int)Math.Round(height * ProcessingFactor)),
            _ => height
        };

        long samples = checked((long)RenderWidth * RenderHeight * SsaaFactor * SsaaFactor);
        if (samples > 160_000_000)
        {
            MessageBox.Show(this,
                "Выбранные разрешение и качество требуют слишком много памяти. Уменьшите размер или коэффициент.",
                "Размер изображения", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        ExportWidth = width;
        ExportHeight = height;
        ExportFormat = FormatBox.SelectedIndex switch
        {
            1 => MandelbrotExportFormat.Jpeg,
            2 => MandelbrotExportFormat.Bmp,
            _ => MandelbrotExportFormat.Png
        };
        JpegQuality = Math.Clamp((int)Math.Round(JpegQualitySlider.Value), 1, 100);
        DialogResult = true;
    }
}
