using FractalExplorer.Utilities.Theme;
using FractalExplorer.Utilities.ColorPicking;

namespace FractalExplorer.Forms.Other
{
    public partial class ThemeEditorForm : Form
    {
        private sealed class ThemeColorBinding
        {
            public required string PropertyName { get; init; }
            public required string DisplayName { get; init; }
            public required Func<ThemeDefinition, Color> Getter { get; init; }
        }

        private readonly List<ThemeColorBinding> _colorBindings = new()
        {
            new ThemeColorBinding { PropertyName = nameof(ThemeDefinition.BaseBackground), DisplayName = "Фон формы", Getter = theme => theme.BaseBackground },
            new ThemeColorBinding { PropertyName = nameof(ThemeDefinition.PanelBackground), DisplayName = "Фон панелей", Getter = theme => theme.PanelBackground },
            new ThemeColorBinding { PropertyName = nameof(ThemeDefinition.ControlBackground), DisplayName = "Фон элементов управления", Getter = theme => theme.ControlBackground },
            new ThemeColorBinding { PropertyName = nameof(ThemeDefinition.PrimaryText), DisplayName = "Текст (основной)", Getter = theme => theme.PrimaryText },
            new ThemeColorBinding { PropertyName = nameof(ThemeDefinition.SecondaryText), DisplayName = "Текст (вторичный)", Getter = theme => theme.SecondaryText },
            new ThemeColorBinding { PropertyName = nameof(ThemeDefinition.AccentPrimary), DisplayName = "Акцент (основной)", Getter = theme => theme.AccentPrimary },
            new ThemeColorBinding { PropertyName = nameof(ThemeDefinition.AccentSecondary), DisplayName = "Акцент (дополнительный)", Getter = theme => theme.AccentSecondary },
            new ThemeColorBinding { PropertyName = nameof(ThemeDefinition.HoverBackground), DisplayName = "Фон при наведении", Getter = theme => theme.HoverBackground },
            new ThemeColorBinding { PropertyName = nameof(ThemeDefinition.PressedBackground), DisplayName = "Фон при нажатии", Getter = theme => theme.PressedBackground },
            new ThemeColorBinding { PropertyName = nameof(ThemeDefinition.BorderColor), DisplayName = "Граница", Getter = theme => theme.BorderColor },
            new ThemeColorBinding { PropertyName = nameof(ThemeDefinition.InteractiveBorderNormal), DisplayName = "Интерактивная граница (обыч.)", Getter = theme => theme.InteractiveBorderNormal.IsEmpty ? theme.BorderColor : theme.InteractiveBorderNormal },
            new ThemeColorBinding { PropertyName = nameof(ThemeDefinition.InteractiveBorderHover), DisplayName = "Интерактивная граница (hover)", Getter = theme => theme.InteractiveBorderHover.IsEmpty ? theme.AccentPrimary : theme.InteractiveBorderHover }
        };

        private readonly Dictionary<string, Panel> _colorPreviewPanels = new(StringComparer.Ordinal);
        private readonly Dictionary<string, Button> _colorEditButtons = new(StringComparer.Ordinal);
        private readonly Dictionary<string, Color> _currentColors = new(StringComparer.Ordinal);

        private readonly WindowsThemeImporter _windowsThemeImporter = new();
        private readonly ColorSelectionService _colorSelectionService = ColorSelectionService.Default;

        private List<ThemeDefinition> _themes = new();
        private ThemeDefinition? _selectedTheme;
        private readonly CheckBox _chkHighVisibilityInteractiveStates = new()
        {
            AutoSize = true,
            Text = "Усиленная интерактивная подсветка",
            Margin = new Padding(0, 0, 0, 10)
        };

        public ThemeEditorForm()
        {
            InitializeComponent();
            ThemeManager.RegisterForm(this);

            _chkHighVisibilityInteractiveStates.CheckedChanged += (_, _) => ApplyPreviewTheme();
            rightPanel.RowCount += 1;
            rightPanel.RowStyles.Insert(2, new RowStyle());
            rightPanel.Controls.Add(_chkHighVisibilityInteractiveStates, 0, 2);
            rightPanel.SetColumnSpan(_chkHighVisibilityInteractiveStates, 1);
            rightPanel.Controls.SetChildIndex(grpColors, 3);
            rightPanel.Controls.SetChildIndex(grpPreview, 4);
            rightPanel.Controls.SetChildIndex(buttonsPanel, 5);

            BuildColorEditors();
        }

        private void ThemeEditorForm_Load(object sender, EventArgs e)
        {
            ReloadThemes(ThemeManager.CurrentThemeId);
        }

        private void BuildColorEditors()
        {
            flpColorProperties.SuspendLayout();
            flpColorProperties.Controls.Clear();

            foreach (ThemeColorBinding binding in _colorBindings)
            {
                Panel rowPanel = new()
                {
                    Width = flpColorProperties.ClientSize.Width - 24,
                    Height = 34,
                    Margin = new Padding(0, 0, 0, 6)
                };

                Label label = new()
                {
                    AutoSize = false,
                    Text = binding.DisplayName,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Location = new Point(0, 6),
                    Size = new Size(220, 22)
                };

                Panel colorPanel = new()
                {
                    BorderStyle = BorderStyle.FixedSingle,
                    Location = new Point(228, 6),
                    Size = new Size(48, 22)
                };

                Button button = new()
                {
                    Text = "Изменить",
                    Location = new Point(284, 4),
                    Size = new Size(90, 26),
                    Tag = binding.PropertyName
                };
                button.Click += EditColorButton_Click;

                rowPanel.Controls.Add(label);
                rowPanel.Controls.Add(colorPanel);
                rowPanel.Controls.Add(button);

                _colorPreviewPanels[binding.PropertyName] = colorPanel;
                _colorEditButtons[binding.PropertyName] = button;
                flpColorProperties.Controls.Add(rowPanel);
            }

            flpColorProperties.ResumeLayout();
        }

        private void ReloadThemes(string? preferredThemeId = null)
        {
            lbThemes.SelectedIndexChanged -= lbThemes_SelectedIndexChanged;

            _themes = ThemeManager.GetAllThemes().ToList();
            lbThemes.Items.Clear();
            foreach (ThemeDefinition theme in _themes)
            {
                lbThemes.Items.Add(GetThemeDisplayName(theme));
            }

            string themeIdToSelect = preferredThemeId ?? ThemeManager.CurrentThemeId;
            int selectedIndex = _themes.FindIndex(theme => string.Equals(theme.Id, themeIdToSelect, StringComparison.OrdinalIgnoreCase));
            if (selectedIndex < 0 && _themes.Count > 0)
            {
                selectedIndex = 0;
            }

            lbThemes.SelectedIndex = selectedIndex;
            lbThemes.SelectedIndexChanged += lbThemes_SelectedIndexChanged;

            if (selectedIndex >= 0)
            {
                LoadTheme(_themes[selectedIndex]);
            }
            else
            {
                _selectedTheme = null;
                _currentColors.Clear();
                txtThemeName.Text = string.Empty;
                UpdateControlsState();
            }
        }

        private static string GetThemeDisplayName(ThemeDefinition theme)
        {
            return theme.IsBuiltIn ? $"{theme.DisplayName} [Встроенная]" : theme.DisplayName;
        }

        private void lbThemes_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbThemes.SelectedIndex < 0 || lbThemes.SelectedIndex >= _themes.Count)
            {
                _selectedTheme = null;
                return;
            }

            LoadTheme(_themes[lbThemes.SelectedIndex]);
        }

        private void LoadTheme(ThemeDefinition theme)
        {
            _selectedTheme = theme;
            txtThemeName.Text = theme.DisplayName;
            _chkHighVisibilityInteractiveStates.Checked = theme.HighVisibilityInteractiveStates;

            _currentColors.Clear();
            foreach (ThemeColorBinding binding in _colorBindings)
            {
                _currentColors[binding.PropertyName] = binding.Getter(theme);
            }

            RefreshColorPreviews();
            ApplyPreviewTheme();
            UpdateControlsState();
        }

        private void RefreshColorPreviews()
        {
            foreach (ThemeColorBinding binding in _colorBindings)
            {
                if (_colorPreviewPanels.TryGetValue(binding.PropertyName, out Panel? panel) &&
                    _currentColors.TryGetValue(binding.PropertyName, out Color color))
                {
                    panel.BackColor = color;
                }
            }
        }

        private void ApplyPreviewTheme()
        {
            if (_selectedTheme is null || _currentColors.Count == 0)
            {
                return;
            }

            ThemeDefinition previewTheme = BuildThemeFromCurrentValues(_selectedTheme.Id, txtThemeName.Text, _selectedTheme.IsBuiltIn);

            pnlPreview.BackColor = previewTheme.PanelBackground;
            lblPreviewTitle.ForeColor = previewTheme.PrimaryText;
            lblPreviewSubText.ForeColor = previewTheme.SecondaryText;

            btnPreviewAction.UseVisualStyleBackColor = false;
            btnPreviewAction.FlatStyle = FlatStyle.Flat;
            btnPreviewAction.FlatAppearance.BorderSize = 1;
            btnPreviewAction.FlatAppearance.BorderColor = previewTheme.BorderColor;
            btnPreviewAction.BackColor = previewTheme.AccentPrimary;
            btnPreviewAction.ForeColor = ResolvePreviewButtonTextColor(previewTheme, previewTheme.AccentPrimary);

            txtPreviewInput.BackColor = previewTheme.ControlBackground;
            txtPreviewInput.ForeColor = previewTheme.PrimaryText;
        }

        private static Color ResolvePreviewButtonTextColor(ThemeDefinition theme, Color buttonBackground)
        {
            if (!theme.Id.StartsWith("windows-system", StringComparison.OrdinalIgnoreCase))
            {
                return theme.PrimaryText;
            }

            const double minimumTextContrast = 4.5d;
            if (CalculateContrastRatio(theme.PrimaryText, buttonBackground) >= minimumTextContrast)
            {
                return theme.PrimaryText;
            }

            double whiteContrast = CalculateContrastRatio(Color.White, buttonBackground);
            double blackContrast = CalculateContrastRatio(Color.Black, buttonBackground);
            return whiteContrast >= blackContrast ? Color.White : Color.Black;
        }

        private static double CalculateContrastRatio(Color first, Color second)
        {
            double firstLuminance = CalculateRelativeLuminance(first);
            double secondLuminance = CalculateRelativeLuminance(second);
            double lighter = Math.Max(firstLuminance, secondLuminance);
            double darker = Math.Min(firstLuminance, secondLuminance);
            return (lighter + 0.05d) / (darker + 0.05d);
        }

        private static double CalculateRelativeLuminance(Color color)
        {
            static double ConvertChannel(byte channel)
            {
                double normalized = channel / 255d;
                return normalized <= 0.03928d
                    ? normalized / 12.92d
                    : Math.Pow((normalized + 0.055d) / 1.055d, 2.4d);
            }

            double red = ConvertChannel(color.R);
            double green = ConvertChannel(color.G);
            double blue = ConvertChannel(color.B);
            return 0.2126d * red + 0.7152d * green + 0.0722d * blue;
        }

        private void UpdateControlsState()
        {
            bool hasSelection = _selectedTheme != null;
            bool isBuiltIn = _selectedTheme?.IsBuiltIn == true;

            txtThemeName.ReadOnly = !hasSelection || isBuiltIn;
            btnDelete.Enabled = hasSelection && !isBuiltIn;
            btnSaveApply.Enabled = hasSelection && !isBuiltIn;
            btnCopy.Enabled = hasSelection;
            btnCopyWindowsTheme.Enabled = IsWindowsThemeImportSupported();
            _chkHighVisibilityInteractiveStates.Enabled = hasSelection && !isBuiltIn;

            foreach (Button button in _colorEditButtons.Values)
            {
                button.Enabled = hasSelection && !isBuiltIn;
            }
        }

        private static bool IsWindowsThemeImportSupported()
        {
            return OperatingSystem.IsWindowsVersionAtLeast(10);
        }

        private void EditColorButton_Click(object? sender, EventArgs e)
        {
            if (_selectedTheme is null || _selectedTheme.IsBuiltIn)
            {
                return;
            }

            if (sender is not Button button || button.Tag is not string propertyName || !_currentColors.TryGetValue(propertyName, out Color currentColor))
            {
                return;
            }

            if (!_colorSelectionService.TrySelectColor(this, currentColor, out Color selectedColor))
            {
                return;
            }

            _currentColors[propertyName] = selectedColor;
            RefreshColorPreviews();
            ApplyPreviewTheme();
        }

        private void txtThemeName_TextChanged(object sender, EventArgs e)
        {
            ApplyPreviewTheme();
        }

        private void btnNew_Click(object sender, EventArgs e)
        {
            ThemeDefinition source = _selectedTheme ?? ThemeManager.CurrentDefinition;
            string newId = GenerateUniqueThemeId("custom-theme");
            string newName = GenerateUniqueThemeName("Новая тема");

            ThemeDefinition newTheme = BuildThemeFromCurrentValues(source.Id, source.DisplayName, source.IsBuiltIn).CloneWith(newId, newName, false);
            ThemeManager.AddOrUpdateCustomTheme(newTheme);
            ThemeManager.SetTheme(newTheme.Id);
            ReloadThemes(newTheme.Id);
        }

        private void btnCopy_Click(object sender, EventArgs e)
        {
            if (_selectedTheme is null)
            {
                return;
            }

            string newId = GenerateUniqueThemeId($"{_selectedTheme.Id}-copy");
            string newName = GenerateUniqueThemeName($"{_selectedTheme.DisplayName} (копия)");
            ThemeDefinition duplicate = ThemeManager.DuplicateTheme(_selectedTheme.Id, newId, newName);
            ThemeManager.SetTheme(duplicate.Id);
            ReloadThemes(duplicate.Id);
        }


        private void btnCopyWindowsTheme_Click(object sender, EventArgs e)
        {
            if (!IsWindowsThemeImportSupported())
            {
                MessageBox.Show(
                    "Импорт темы Windows поддерживается только в Windows 10 и новее.",
                    "Импорт недоступен",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (!_windowsThemeImporter.TryBuildThemeFromWindows(out ThemeDefinition windowsTheme, out string error))
            {
                MessageBox.Show(
                    string.IsNullOrWhiteSpace(error) ? "Не удалось создать тему из настроек Windows." : error,
                    "Ошибка импорта",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            string newThemeId = GenerateUniqueThemeId(windowsTheme.Id);
            string newThemeName = GenerateUniqueThemeName(windowsTheme.DisplayName);
            ThemeDefinition newTheme = windowsTheme.CloneWith(newThemeId, newThemeName, false);

            ThemeManager.AddOrUpdateCustomTheme(newTheme);
            ThemeManager.SetTheme(newTheme.Id);
            ReloadThemes(newTheme.Id);
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (_selectedTheme is null || _selectedTheme.IsBuiltIn)
            {
                return;
            }

            DialogResult result = MessageBox.Show(
                $"Удалить тему '{_selectedTheme.DisplayName}'?",
                "Подтверждение",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes)
            {
                return;
            }

            string deletedId = _selectedTheme.Id;
            bool removed = ThemeManager.RemoveCustomTheme(deletedId);
            if (removed)
            {
                ReloadThemes(ThemeManager.CurrentThemeId);
            }
        }

        private void btnSaveApply_Click(object sender, EventArgs e)
        {
            if (_selectedTheme is null || _selectedTheme.IsBuiltIn)
            {
                return;
            }

            string displayName = txtThemeName.Text.Trim();
            if (string.IsNullOrWhiteSpace(displayName))
            {
                MessageBox.Show("Имя темы не может быть пустым.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            ThemeDefinition updatedTheme = BuildThemeFromCurrentValues(_selectedTheme.Id, displayName, false);
            ThemeManager.AddOrUpdateCustomTheme(updatedTheme);
            ThemeManager.SetTheme(updatedTheme.Id);
            ReloadThemes(updatedTheme.Id);
        }

        private ThemeDefinition BuildThemeFromCurrentValues(string id, string displayName, bool isBuiltIn)
        {
            string normalizedName = string.IsNullOrWhiteSpace(displayName) ? "Новая тема" : displayName.Trim();
            return new ThemeDefinition
            {
                Id = id,
                DisplayName = normalizedName,
                IsBuiltIn = isBuiltIn,
                BaseBackground = _currentColors[nameof(ThemeDefinition.BaseBackground)],
                PanelBackground = _currentColors[nameof(ThemeDefinition.PanelBackground)],
                ControlBackground = _currentColors[nameof(ThemeDefinition.ControlBackground)],
                PrimaryText = _currentColors[nameof(ThemeDefinition.PrimaryText)],
                SecondaryText = _currentColors[nameof(ThemeDefinition.SecondaryText)],
                AccentPrimary = _currentColors[nameof(ThemeDefinition.AccentPrimary)],
                AccentSecondary = _currentColors[nameof(ThemeDefinition.AccentSecondary)],
                HoverBackground = _currentColors[nameof(ThemeDefinition.HoverBackground)],
                PressedBackground = _currentColors[nameof(ThemeDefinition.PressedBackground)],
                BorderColor = _currentColors[nameof(ThemeDefinition.BorderColor)],
                InputBorderColor = _selectedTheme?.InputBorderColor ?? _currentColors[nameof(ThemeDefinition.BorderColor)],
                InteractiveBorderNormal = _currentColors[nameof(ThemeDefinition.InteractiveBorderNormal)],
                InteractiveBorderHover = _currentColors[nameof(ThemeDefinition.InteractiveBorderHover)],
                HighVisibilityInteractiveStates = _chkHighVisibilityInteractiveStates.Checked
            };
        }

        private string GenerateUniqueThemeName(string baseName)
        {
            string candidate = baseName;
            int counter = 2;
            HashSet<string> names = ThemeManager.GetAllThemes()
                .Select(theme => theme.DisplayName)
                .ToHashSet(StringComparer.CurrentCultureIgnoreCase);

            while (names.Contains(candidate))
            {
                candidate = $"{baseName} {counter++}";
            }

            return candidate;
        }

        private string GenerateUniqueThemeId(string baseId)
        {
            string normalizedBase = baseId.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(normalizedBase))
            {
                normalizedBase = "custom-theme";
            }

            string candidate = normalizedBase;
            int counter = 2;
            HashSet<string> ids = ThemeManager.GetAllThemes()
                .Select(theme => theme.Id)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            while (ids.Contains(candidate))
            {
                candidate = $"{normalizedBase}-{counter++}";
            }

            return candidate;
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
