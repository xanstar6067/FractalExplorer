using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FractalExplorerWPF.Infrastructure.ColorPicking;
using FractalExplorerWPF.Theming;
using Color = System.Windows.Media.Color;

namespace FractalExplorerWPF.Views;

public partial class ThemeEditorWindow : Window
{
    private sealed record ColorBinding(string PropertyName, string DisplayName, Func<ThemeDefinition, Color> Getter);

    private static readonly IReadOnlyList<ColorBinding> ColorBindings =
    [
        new(nameof(ThemeDefinition.BaseBackground), "Фон окна", theme => theme.BaseBackground),
        new(nameof(ThemeDefinition.PanelBackground), "Фон панелей", theme => theme.PanelBackground),
        new(nameof(ThemeDefinition.ControlBackground), "Фон элементов управления", theme => theme.ControlBackground),
        new(nameof(ThemeDefinition.PrimaryText), "Основной текст", theme => theme.PrimaryText),
        new(nameof(ThemeDefinition.SecondaryText), "Вторичный текст", theme => theme.SecondaryText),
        new(nameof(ThemeDefinition.AccentPrimary), "Основной акцент", theme => theme.AccentPrimary),
        new(nameof(ThemeDefinition.AccentSecondary), "Дополнительный акцент", theme => theme.AccentSecondary),
        new(nameof(ThemeDefinition.HoverBackground), "Фон при наведении", theme => theme.HoverBackground),
        new(nameof(ThemeDefinition.PressedBackground), "Фон при нажатии", theme => theme.PressedBackground),
        new(nameof(ThemeDefinition.BorderColor), "Граница", theme => theme.BorderColor),
        new(nameof(ThemeDefinition.InputBorderColor), "Граница полей", theme => theme.InputBorderColor),
        new(nameof(ThemeDefinition.InteractiveBorderNormal), "Интерактивная граница", theme => IsSpecified(theme.InteractiveBorderNormal) ? theme.InteractiveBorderNormal : theme.BorderColor),
        new(nameof(ThemeDefinition.InteractiveBorderHover), "Интерактивная граница при наведении", theme => IsSpecified(theme.InteractiveBorderHover) ? theme.InteractiveBorderHover : theme.AccentPrimary),
        new(nameof(ThemeDefinition.HighVisibilityInteractiveHover), "Высококонтрастная подсветка", theme => IsSpecified(theme.HighVisibilityInteractiveHover) ? theme.HighVisibilityInteractiveHover : theme.AccentPrimary)
    ];

    private readonly Dictionary<string, Color> _colors = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Border> _swatches = new(StringComparer.Ordinal);
    private readonly List<ThemeDefinition> _themes = [];
    private readonly ColorSelectionService _colorSelectionService = ColorSelectionService.Default;
    private readonly WindowsThemeImporter _windowsImporter = new();
    private ThemeDefinition? _selectedTheme;
    private bool _updating;

    public ThemeEditorWindow()
    {
        InitializeComponent();
        BuildColorRows();
        ReloadThemes(ThemeManager.CurrentThemeId);
    }

    private void BuildColorRows()
    {
        foreach (ColorBinding binding in ColorBindings)
        {
            var row = new Grid { Margin = new Thickness(0, 0, 0, 7) };
            row.ColumnDefinitions.Add(new ColumnDefinition());
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(58) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(105) });
            row.Children.Add(new TextBlock { Text = binding.DisplayName, VerticalAlignment = VerticalAlignment.Center });

            var swatch = new Border
            {
                Height = 27, Margin = new Thickness(6, 0, 8, 0), CornerRadius = new CornerRadius(4),
                BorderThickness = new Thickness(1)
            };
            swatch.SetResourceReference(Border.BorderBrushProperty, "Theme.BorderBrush");
            ThemeContract.SetIgnoreAudit(swatch, true);
            Grid.SetColumn(swatch, 1);
            row.Children.Add(swatch);
            _swatches[binding.PropertyName] = swatch;

            var edit = new Button { Content = "Изменить", Tag = binding.PropertyName, Padding = new Thickness(8, 3, 8, 3) };
            edit.Click += EditColor_OnClick;
            Grid.SetColumn(edit, 2);
            row.Children.Add(edit);
            ColorPropertiesPanel.Children.Add(row);
        }
    }

    private void ReloadThemes(string? preferredId)
    {
        _updating = true;
        _themes.Clear();
        _themes.AddRange(ThemeManager.GetAllThemes());
        ThemeList.Items.Clear();
        foreach (ThemeDefinition theme in _themes)
            ThemeList.Items.Add(new ListBoxItem { Content = theme.IsBuiltIn ? $"{theme.DisplayName}  · встроенная" : theme.DisplayName, Tag = theme });

        int index = _themes.FindIndex(theme => string.Equals(theme.Id, preferredId, StringComparison.OrdinalIgnoreCase));
        ThemeList.SelectedIndex = index >= 0 ? index : 0;
        _updating = false;
        if (ThemeList.SelectedItem is ListBoxItem { Tag: ThemeDefinition selected }) LoadTheme(selected);
    }

    private void ThemeList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_updating || ThemeList.SelectedItem is not ListBoxItem { Tag: ThemeDefinition theme }) return;
        LoadTheme(theme);
    }

    private void LoadTheme(ThemeDefinition theme)
    {
        _updating = true;
        _selectedTheme = theme;
        ThemeNameBox.Text = theme.DisplayName;
        HighVisibilityBox.IsChecked = theme.HighVisibilityInteractiveStates;
        _colors.Clear();
        foreach (ColorBinding binding in ColorBindings) _colors[binding.PropertyName] = binding.Getter(theme);
        _updating = false;
        RefreshSwatches();
        RefreshPreview();
        RefreshEnabledState();
    }

    private void RefreshSwatches()
    {
        foreach ((string name, Border swatch) in _swatches)
            if (_colors.TryGetValue(name, out Color color)) swatch.Background = new SolidColorBrush(color);
    }

    private void EditColor_OnClick(object sender, RoutedEventArgs e)
    {
        if (_selectedTheme is not { IsBuiltIn: false } || sender is not Button { Tag: string property } || !_colors.TryGetValue(property, out Color color)) return;
        if (!_colorSelectionService.TrySelectColor(this, color, out Color selected)) return;
        _colors[property] = selected;
        RefreshSwatches();
        RefreshPreview();
        RefreshEnabledState();
    }

    private void ThemeNameBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_updating) return;
        RefreshPreview();
        RefreshEnabledState();
    }

    private void HighVisibilityBox_OnChanged(object sender, RoutedEventArgs e)
    {
        if (_updating) return;
        RefreshPreview();
        RefreshEnabledState();
    }

    private void RefreshPreview()
    {
        if (!TryBuildTheme(out ThemeDefinition? theme) || theme is null) return;
        ThemeManager.ApplyPreviewResources(PreviewRoot.Resources, theme);
    }

    private void RefreshEnabledState()
    {
        bool editable = _selectedTheme is { IsBuiltIn: false };
        ThemeNameBox.IsReadOnly = !editable;
        HighVisibilityBox.IsEnabled = editable;
        DeleteButton.IsEnabled = editable;
        SaveButton.IsEnabled = editable && HasChanges();
        ApplyButton.IsEnabled = _selectedTheme is not null;
        foreach (Button button in ColorPropertiesPanel.Children.OfType<Grid>().SelectMany(row => row.Children.OfType<Button>()))
            button.IsEnabled = editable;
    }

    private void New_OnClick(object sender, RoutedEventArgs e)
    {
        ThemeManager.TryGetTheme("light", out ThemeDefinition source);
        ThemeDefinition theme = source.CloneWith(UniqueId("custom-theme"), UniqueName("Новая тема"), false);
        ThemeManager.AddOrUpdateCustomTheme(theme);
        ReloadThemes(theme.Id);
    }

    private void Copy_OnClick(object sender, RoutedEventArgs e)
    {
        if (_selectedTheme is null) return;
        ThemeDefinition copy = ThemeManager.DuplicateTheme(_selectedTheme.Id, UniqueId($"{_selectedTheme.Id}-copy"), UniqueName($"{_selectedTheme.DisplayName} (копия)"));
        ReloadThemes(copy.Id);
    }

    private void Delete_OnClick(object sender, RoutedEventArgs e)
    {
        if (_selectedTheme is not { IsBuiltIn: false } theme) return;
        if (MessageBox.Show(this, $"Удалить тему «{theme.DisplayName}»?", "Темы оформления",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        ThemeManager.RemoveCustomTheme(theme.Id);
        ReloadThemes(ThemeManager.CurrentThemeId);
    }

    private void Save_OnClick(object sender, RoutedEventArgs e)
    {
        if (!TrySave(out ThemeDefinition? saved)) return;
        ReloadThemes(saved!.Id);
    }

    private void Apply_OnClick(object sender, RoutedEventArgs e)
    {
        if (_selectedTheme is null) return;
        ThemeDefinition theme = _selectedTheme;
        if (!theme.IsBuiltIn && (!TrySave(out ThemeDefinition? saved) || saved is null)) return;
        else if (!theme.IsBuiltIn) theme = ThemeManager.GetAllThemes().First(item => item.Id == theme.Id);
        ThemeManager.SetTheme(theme.Id);
        ReloadThemes(theme.Id);
    }

    private void ImportWindows_OnClick(object sender, RoutedEventArgs e)
    {
        if (!_windowsImporter.TryBuildThemeFromWindows(out ThemeDefinition imported, out string error))
        {
            MessageBox.Show(this, string.IsNullOrWhiteSpace(error) ? "Не удалось импортировать тему Windows." : error,
                "Темы оформления", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        ThemeDefinition theme = imported.CloneWith(UniqueId(imported.Id), UniqueName(imported.DisplayName), false);
        ThemeManager.AddOrUpdateCustomTheme(theme);
        ReloadThemes(theme.Id);
        if (!string.IsNullOrWhiteSpace(error))
            MessageBox.Show(this, error, "Тема создана с резервными значениями", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private bool TrySave(out ThemeDefinition? saved)
    {
        saved = null;
        if (_selectedTheme is not { IsBuiltIn: false }) return false;
        if (string.IsNullOrWhiteSpace(ThemeNameBox.Text))
        {
            MessageBox.Show(this, "Название темы не может быть пустым.", "Темы оформления", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
        if (!TryBuildTheme(out saved) || saved is null) return false;
        ThemeManager.AddOrUpdateCustomTheme(saved);
        return true;
    }

    private bool TryBuildTheme(out ThemeDefinition? theme)
    {
        theme = null;
        if (_selectedTheme is null || ColorBindings.Any(binding => !_colors.ContainsKey(binding.PropertyName))) return false;
        theme = new ThemeDefinition
        {
            Id = _selectedTheme.Id,
            DisplayName = string.IsNullOrWhiteSpace(ThemeNameBox.Text) ? _selectedTheme.DisplayName : ThemeNameBox.Text.Trim(),
            IsBuiltIn = _selectedTheme.IsBuiltIn,
            BaseBackground = Get(nameof(ThemeDefinition.BaseBackground)), PanelBackground = Get(nameof(ThemeDefinition.PanelBackground)),
            ControlBackground = Get(nameof(ThemeDefinition.ControlBackground)), PrimaryText = Get(nameof(ThemeDefinition.PrimaryText)),
            SecondaryText = Get(nameof(ThemeDefinition.SecondaryText)), AccentPrimary = Get(nameof(ThemeDefinition.AccentPrimary)),
            AccentSecondary = Get(nameof(ThemeDefinition.AccentSecondary)), HoverBackground = Get(nameof(ThemeDefinition.HoverBackground)),
            PressedBackground = Get(nameof(ThemeDefinition.PressedBackground)), BorderColor = Get(nameof(ThemeDefinition.BorderColor)),
            InputBorderColor = Get(nameof(ThemeDefinition.InputBorderColor)), InteractiveBorderNormal = Get(nameof(ThemeDefinition.InteractiveBorderNormal)),
            InteractiveBorderHover = Get(nameof(ThemeDefinition.InteractiveBorderHover)), HighVisibilityInteractiveHover = Get(nameof(ThemeDefinition.HighVisibilityInteractiveHover)),
            HighVisibilityInteractiveStates = HighVisibilityBox.IsChecked == true
        };
        return true;
    }

    private bool HasChanges()
    {
        if (_selectedTheme is not { IsBuiltIn: false } theme || !TryBuildTheme(out ThemeDefinition? edited) || edited is null) return false;
        return !string.Equals(theme.DisplayName, edited.DisplayName, StringComparison.CurrentCulture) ||
               theme.HighVisibilityInteractiveStates != edited.HighVisibilityInteractiveStates ||
               ColorBindings.Any(binding => binding.Getter(theme) != binding.Getter(edited));
    }

    private string UniqueName(string basis)
    {
        HashSet<string> names = ThemeManager.GetAllThemes().Select(theme => theme.DisplayName).ToHashSet(StringComparer.CurrentCultureIgnoreCase);
        string candidate = basis; int suffix = 2;
        while (names.Contains(candidate)) candidate = $"{basis} {suffix++}";
        return candidate;
    }

    private string UniqueId(string basis)
    {
        string normalized = string.IsNullOrWhiteSpace(basis) ? "custom-theme" : basis.Trim().ToLowerInvariant();
        HashSet<string> ids = ThemeManager.GetAllThemes().Select(theme => theme.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        string candidate = normalized; int suffix = 2;
        while (ids.Contains(candidate)) candidate = $"{normalized}-{suffix++}";
        return candidate;
    }

    private Color Get(string property) => _colors[property];
    private static bool IsSpecified(Color color) => color.A != 0 || color.R != 0 || color.G != 0 || color.B != 0;
}
