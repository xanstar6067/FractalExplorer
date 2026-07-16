using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FractalExplorerWPF.Infrastructure.ColorPicking;
using Color = System.Windows.Media.Color;

namespace FractalExplorerWPF.Controls;

public partial class ColorSelectorControl : UserControl
{
    public static readonly DependencyProperty SelectedColorProperty = DependencyProperty.Register(
        nameof(SelectedColor), typeof(Color), typeof(ColorSelectorControl),
        new FrameworkPropertyMetadata(Colors.Black,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, SelectedColor_OnChanged));

    public ColorSelectorControl()
    {
        InitializeComponent();
        RefreshColor();
    }

    public Color SelectedColor
    {
        get => (Color)GetValue(SelectedColorProperty);
        set => SetValue(SelectedColorProperty, value);
    }

    public event EventHandler? SelectedColorChanged;

    private static void SelectedColor_OnChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var selector = (ColorSelectorControl)sender;
        selector.RefreshColor();
        selector.SelectedColorChanged?.Invoke(selector, EventArgs.Empty);
    }

    private void PickerButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (Window.GetWindow(this) is not { } owner ||
            !ColorSelectionService.Default.TrySelectColor(owner, SelectedColor, out Color selected)) return;

        SelectedColor = selected;
    }

    private void RefreshColor()
    {
        if (ColorSwatch is null || HexText is null) return;
        ColorSwatch.Background = new SolidColorBrush(SelectedColor);
        HexText.Text = $"#{SelectedColor.A:X2}{SelectedColor.R:X2}{SelectedColor.G:X2}{SelectedColor.B:X2}";
    }
}
