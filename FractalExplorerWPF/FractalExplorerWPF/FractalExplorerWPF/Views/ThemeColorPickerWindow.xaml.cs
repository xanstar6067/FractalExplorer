using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Color = System.Windows.Media.Color;

namespace FractalExplorerWPF.Views;

public partial class ThemeColorPickerWindow : Window
{
    private bool _updating;

    public Color SelectedColor { get; private set; }

    public ThemeColorPickerWindow(Color initialColor)
    {
        InitializeComponent();
        SetColor(initialColor);
    }

    public static bool TryPick(Window owner, Color initialColor, out Color selectedColor)
    {
        var dialog = new ThemeColorPickerWindow(initialColor) { Owner = owner };
        bool accepted = dialog.ShowDialog() == true;
        selectedColor = accepted ? dialog.SelectedColor : initialColor;
        return accepted;
    }

    private void SetColor(Color color)
    {
        _updating = true;
        SelectedColor = color;
        AlphaSlider.Value = color.A; RedSlider.Value = color.R; GreenSlider.Value = color.G; BlueSlider.Value = color.B;
        HexBox.Text = ToHex(color);
        RefreshPreview();
        _updating = false;
    }

    private void ChannelSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_updating || !IsLoaded) return;
        SelectedColor = Color.FromArgb((byte)AlphaSlider.Value, (byte)RedSlider.Value, (byte)GreenSlider.Value, (byte)BlueSlider.Value);
        _updating = true;
        HexBox.Text = ToHex(SelectedColor);
        RefreshPreview();
        _updating = false;
    }

    private void HexBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_updating) return;
        if (!TryParse(HexBox.Text.Trim(), out Color color))
        {
            ValidationText.Text = "Введите цвет в формате #AARRGGBB.";
            return;
        }

        ValidationText.Text = string.Empty;
        _updating = true;
        SelectedColor = color;
        AlphaSlider.Value = color.A; RedSlider.Value = color.R; GreenSlider.Value = color.G; BlueSlider.Value = color.B;
        RefreshPreview();
        _updating = false;
    }

    private void RefreshPreview()
    {
        Preview.Background = new SolidColorBrush(SelectedColor);
        AlphaValue.Text = SelectedColor.A.ToString(); RedValue.Text = SelectedColor.R.ToString();
        GreenValue.Text = SelectedColor.G.ToString(); BlueValue.Text = SelectedColor.B.ToString();
    }

    private void Accept_OnClick(object sender, RoutedEventArgs e)
    {
        if (!TryParse(HexBox.Text.Trim(), out Color color)) return;
        SelectedColor = color;
        DialogResult = true;
    }

    private static string ToHex(Color color) => $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";

    private static bool TryParse(string value, out Color color)
    {
        color = default;
        if (value.Length == 7 && value[0] == '#') value = "#FF" + value[1..];
        return value.Length == 9 && value[0] == '#' &&
               byte.TryParse(value.AsSpan(1, 2), NumberStyles.HexNumber, null, out byte a) &&
               byte.TryParse(value.AsSpan(3, 2), NumberStyles.HexNumber, null, out byte r) &&
               byte.TryParse(value.AsSpan(5, 2), NumberStyles.HexNumber, null, out byte g) &&
               byte.TryParse(value.AsSpan(7, 2), NumberStyles.HexNumber, null, out byte b) &&
               Assign(out color, Color.FromArgb(a, r, g, b));
    }

    private static bool Assign(out Color target, Color value) { target = value; return true; }
}
