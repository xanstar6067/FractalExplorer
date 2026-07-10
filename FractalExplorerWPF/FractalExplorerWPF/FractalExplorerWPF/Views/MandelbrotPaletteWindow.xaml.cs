using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FractalExplorerWPF.Infrastructure;
using FractalExplorerWPF.Models;
using Color = System.Windows.Media.Color;
using MediaBrushes = System.Windows.Media.Brushes;
using Point = System.Windows.Point;

namespace FractalExplorerWPF.Views;

public partial class MandelbrotPaletteWindow : Window
{
    private readonly MandelbrotPaletteManager _manager;
    private MandelbrotPalette? _selected;
    private bool _updating;

    public MandelbrotPaletteWindow(MandelbrotPaletteManager manager)
    {
        InitializeComponent();
        _manager = manager;
        RefreshList(_manager.ActivePalette);
    }

    private void RefreshList(MandelbrotPalette? select)
    {
        PaletteList.ItemsSource = null;
        PaletteList.ItemsSource = select is not null && !_manager.Palettes.Contains(select)
            ? new[] { select }.Concat(_manager.Palettes).ToList()
            : _manager.Palettes;
        PaletteList.SelectedItem = select ?? _manager.Palettes.FirstOrDefault();
    }

    private void PaletteList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PaletteList.SelectedItem is not MandelbrotPalette palette) return;
        _selected = palette;
        _updating = true;
        NameBox.Text = palette.Name;
        ColorsBox.Text = string.Join("; ", palette.Colors.Select(ToHex));
        InteriorColorBox.Text = ToHex(palette.InteriorColor);
        GradientBox.IsChecked = palette.IsGradient;
        GammaBox.Text = palette.Gamma.ToString(CultureInfo.InvariantCulture);
        PeriodBox.Text = palette.ColorPeriod.ToString(CultureInfo.InvariantCulture);
        _updating = false;
        UpdateEditState();
        UpdatePreview();
    }

    private void New_OnClick(object sender, RoutedEventArgs e)
    {
        var palette = new MandelbrotPalette { Name = UniqueName("Новая палитра") };
        _manager.Palettes.Add(palette);
        RefreshList(palette);
    }

    private void Copy_OnClick(object sender, RoutedEventArgs e)
    {
        if (_selected is null) return;
        MandelbrotPalette copy = _selected.Clone(UniqueName($"{_selected.Name} копия"));
        _manager.Palettes.Add(copy);
        RefreshList(copy);
    }

    private void Delete_OnClick(object sender, RoutedEventArgs e)
    {
        if (_selected is null || _selected.IsBuiltIn) return;
        if (MessageBox.Show(this, $"Удалить «{_selected.Name}»?", "Палитра",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        bool active = ReferenceEquals(_selected, _manager.ActivePalette);
        _manager.Palettes.Remove(_selected);
        if (active) _manager.ActivePalette = _manager.Palettes[0];
        _manager.SaveCustomPalettes();
        RefreshList(_manager.ActivePalette);
    }

    private void Save_OnClick(object sender, RoutedEventArgs e)
    {
        if (!ApplyEdits()) return;
        if (_selected is not null && !_manager.Palettes.Contains(_selected))
        {
            if (_manager.Palettes.Any(palette => palette.Name.Equals(_selected.Name, StringComparison.OrdinalIgnoreCase)))
                _selected.Name = UniqueName($"{_selected.Name} копия");
            _manager.Palettes.Add(_selected);
        }
        _manager.SaveCustomPalettes();
        RefreshList(_selected);
    }

    private void Apply_OnClick(object sender, RoutedEventArgs e)
    {
        if (_selected is null) return;
        if (!_selected.IsBuiltIn && !ApplyEdits()) return;
        _manager.ActivePalette = _selected;
        _manager.SaveCustomPalettes();
        DialogResult = true;
    }

    private bool ApplyEdits()
    {
        if (_selected is null || _selected.IsBuiltIn) return _selected is not null;
        List<Color> colors = ParseColors(ColorsBox.Text);
        if (string.IsNullOrWhiteSpace(NameBox.Text) || colors.Count == 0 ||
            !TryParseColor(InteriorColorBox.Text.Trim(), out Color interior) ||
            !double.TryParse(GammaBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double gamma) || gamma is < 0.01 or > 100 ||
            !int.TryParse(PeriodBox.Text, out int period) || period is < 1 or > 100_000)
        {
            MessageBox.Show(this, "Проверьте имя, цвета, гамму и период палитры.", "Палитра",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
        _selected.Name = NameBox.Text.Trim();
        _selected.Colors = colors;
        _selected.InteriorColor = interior;
        _selected.IsGradient = GradientBox.IsChecked == true;
        _selected.Gamma = gamma;
        _selected.ColorPeriod = period;
        return true;
    }

    private void ColorsBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_updating) UpdatePreview();
    }

    private void UpdatePreview()
    {
        List<Color> colors = ParseColors(ColorsBox.Text);
        if (colors.Count == 0) { GradientPreview.Background = MediaBrushes.Transparent; return; }
        if (colors.Count == 1) { GradientPreview.Background = new SolidColorBrush(colors[0]); return; }
        var gradient = new LinearGradientBrush { StartPoint = new Point(0, 0.5), EndPoint = new Point(1, 0.5) };
        for (int i = 0; i < colors.Count; i++) gradient.GradientStops.Add(new GradientStop(colors[i], (double)i / (colors.Count - 1)));
        GradientPreview.Background = gradient;
    }

    private void UpdateEditState()
    {
        bool editable = _selected is { IsBuiltIn: false };
        NameBox.IsEnabled = editable; ColorsBox.IsEnabled = editable; InteriorColorBox.IsEnabled = editable;
        GradientBox.IsEnabled = editable; GammaBox.IsEnabled = editable; PeriodBox.IsEnabled = editable;
        EditHint.Text = editable ? "Пользовательскую палитру можно редактировать."
            : "Встроенную палитру можно применить или скопировать.";
    }

    private List<Color> ParseColors(string text) => text.Split([';', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
        .Select(value => value.Trim()).Select(value => TryParseColor(value, out Color color) ? color : (Color?)null)
        .Where(color => color.HasValue).Select(color => color!.Value).ToList();

    private string UniqueName(string basis)
    {
        string candidate = basis; int suffix = 1;
        while (_manager.Palettes.Any(p => p.Name.Equals(candidate, StringComparison.OrdinalIgnoreCase))) candidate = $"{basis} {suffix++}";
        return candidate;
    }

    private static string ToHex(Color color) => $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";

    private static bool TryParseColor(string value, out Color color)
    {
        color = Colors.Transparent;
        if (value.Length == 7) value = "#FF" + value[1..];
        if (value.Length != 9 || value[0] != '#') return false;
        return byte.TryParse(value.AsSpan(1, 2), NumberStyles.HexNumber, null, out byte a) &&
               byte.TryParse(value.AsSpan(3, 2), NumberStyles.HexNumber, null, out byte r) &&
               byte.TryParse(value.AsSpan(5, 2), NumberStyles.HexNumber, null, out byte g) &&
               byte.TryParse(value.AsSpan(7, 2), NumberStyles.HexNumber, null, out byte b) &&
               Assign(out color, Color.FromArgb(a, r, g, b));
    }

    private static bool Assign(out Color target, Color value) { target = value; return true; }
}
