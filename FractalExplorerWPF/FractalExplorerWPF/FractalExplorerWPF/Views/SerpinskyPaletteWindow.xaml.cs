using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FractalExplorerWPF.Infrastructure;
using FractalExplorerWPF.Models;
using Color = System.Windows.Media.Color;

namespace FractalExplorerWPF.Views;

public partial class SerpinskyPaletteWindow : Window
{
    private readonly SerpinskyPaletteManager _manager;
    private SerpinskyPalette? _selected;
    private bool _updating;

    public SerpinskyPaletteWindow(SerpinskyPaletteManager manager)
    {
        InitializeComponent();
        _manager = manager;
        RefreshList(_manager.ActivePalette);
    }

    private void RefreshList(SerpinskyPalette? select)
    {
        PaletteList.ItemsSource = null;
        PaletteList.ItemsSource = _manager.Palettes;
        PaletteList.SelectedItem = select ?? _manager.Palettes.FirstOrDefault();
    }

    private void PaletteList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PaletteList.SelectedItem is not SerpinskyPalette palette)
        {
            return;
        }

        _selected = palette;
        _updating = true;
        NameBox.Text = palette.Name;
        FractalColorBox.Text = ToHex(palette.FractalColor);
        BackgroundColorBox.Text = ToHex(palette.BackgroundColor);
        _updating = false;
        UpdateEditState();
        UpdatePreviews();
    }

    private void New_OnClick(object sender, RoutedEventArgs e)
    {
        string name = UniqueName("Новая палитра");
        var palette = new SerpinskyPalette { Name = name };
        _manager.Palettes.Add(palette);
        RefreshList(palette);
    }

    private void Copy_OnClick(object sender, RoutedEventArgs e)
    {
        if (_selected is null)
        {
            return;
        }

        SerpinskyPalette palette = _selected.Clone(UniqueName($"{_selected.Name} копия"));
        _manager.Palettes.Add(palette);
        RefreshList(palette);
    }

    private void Delete_OnClick(object sender, RoutedEventArgs e)
    {
        if (_selected is null || _selected.IsBuiltIn)
        {
            return;
        }

        if (MessageBox.Show(this, $"Удалить палитру «{_selected.Name}»?", "Палитры",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        bool wasActive = ReferenceEquals(_manager.ActivePalette, _selected);
        _manager.Palettes.Remove(_selected);
        if (wasActive)
        {
            _manager.ActivePalette = _manager.Palettes[0];
        }
        _manager.SaveCustomPalettes();
        RefreshList(_manager.ActivePalette);
    }

    private void Save_OnClick(object sender, RoutedEventArgs e)
    {
        if (!TryCommitEditor())
        {
            return;
        }

        _manager.SaveCustomPalettes();
        RefreshList(_selected);
    }

    private void Apply_OnClick(object sender, RoutedEventArgs e)
    {
        if (!TryCommitEditor() || _selected is null)
        {
            return;
        }

        _manager.ActivePalette = _selected;
        _manager.SaveCustomPalettes();
        DialogResult = true;
    }

    private bool TryCommitEditor()
    {
        if (_selected is null)
        {
            return false;
        }
        if (_selected.IsBuiltIn)
        {
            return true;
        }
        if (string.IsNullOrWhiteSpace(NameBox.Text) ||
            !TryParseColor(FractalColorBox.Text, out Color fractal) ||
            !TryParseColor(BackgroundColorBox.Text, out Color background))
        {
            MessageBox.Show(this, "Проверьте название и цвета в формате #AARRGGBB.",
                "Палитра", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        _selected.Name = NameBox.Text.Trim();
        _selected.FractalColor = fractal;
        _selected.BackgroundColor = background;
        return true;
    }

    private void ColorBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_updating)
        {
            UpdatePreviews();
        }
    }

    private void UpdateEditState()
    {
        bool editable = _selected is { IsBuiltIn: false };
        NameBox.IsEnabled = editable;
        FractalColorBox.IsEnabled = editable;
        BackgroundColorBox.IsEnabled = editable;
        EditHint.Text = editable
            ? "Пользовательскую палитру можно редактировать."
            : "Встроенную палитру можно применить или скопировать.";
    }

    private void UpdatePreviews()
    {
        if (TryParseColor(FractalColorBox.Text, out Color fractal))
        {
            FractalColorPreview.Background = new SolidColorBrush(fractal);
        }
        if (TryParseColor(BackgroundColorBox.Text, out Color background))
        {
            BackgroundColorPreview.Background = new SolidColorBrush(background);
        }
    }

    private string UniqueName(string basis)
    {
        string candidate = basis;
        int suffix = 1;
        while (_manager.Palettes.Any(p => p.Name.Equals(candidate, StringComparison.OrdinalIgnoreCase)))
        {
            candidate = $"{basis} {suffix++}";
        }
        return candidate;
    }

    private static string ToHex(Color color) =>
        $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";

    private static bool TryParseColor(string value, out Color color)
    {
        color = Colors.Transparent;
        if (value.Length != 9 || value[0] != '#')
        {
            return false;
        }
        return byte.TryParse(value.AsSpan(1, 2), NumberStyles.HexNumber, null, out byte a) &&
               byte.TryParse(value.AsSpan(3, 2), NumberStyles.HexNumber, null, out byte r) &&
               byte.TryParse(value.AsSpan(5, 2), NumberStyles.HexNumber, null, out byte g) &&
               byte.TryParse(value.AsSpan(7, 2), NumberStyles.HexNumber, null, out byte b) &&
               Assign(out color, Color.FromArgb(a, r, g, b));
    }

    private static bool Assign(out Color target, Color value)
    {
        target = value;
        return true;
    }
}
