using System.Drawing;
using System.Windows.Forms;

namespace FractalExplorer.Utilities.Theme
{
    public static class ThemeManager
    {
        public const string DefaultThemeId = "dark-modern-lab-green";

        private static readonly List<ThemeDefinition> BuiltInThemes = new()
        {
            new ThemeDefinition
            {
                Id = "dark-modern-lab-blue",
                DisplayName = "Тёмная (синяя)",
                IsBuiltIn = true,
                BaseBackground = Color.FromArgb(18, 18, 18),
                PanelBackground = Color.FromArgb(28, 30, 36),
                ControlBackground = Color.FromArgb(36, 40, 48),
                PrimaryText = Color.FromArgb(236, 240, 245),
                SecondaryText = Color.FromArgb(168, 176, 188),
                AccentPrimary = Color.FromArgb(38, 132, 255),
                AccentSecondary = Color.FromArgb(111, 86, 205),
                HoverBackground = Color.FromArgb(54, 61, 74),
                PressedBackground = Color.FromArgb(72, 80, 96),
                BorderColor = Color.FromArgb(64, 74, 92),
                InputBorderColor = Color.FromArgb(82, 95, 118)
            },
            new ThemeDefinition
            {
                Id = "dark-modern-lab-violet",
                DisplayName = "Тёмная (фиолетовая)",
                IsBuiltIn = true,
                BaseBackground = Color.FromArgb(18, 18, 18),
                PanelBackground = Color.FromArgb(30, 26, 38),
                ControlBackground = Color.FromArgb(42, 36, 52),
                PrimaryText = Color.FromArgb(236, 240, 245),
                SecondaryText = Color.FromArgb(176, 168, 188),
                AccentPrimary = Color.FromArgb(111, 86, 205),
                AccentSecondary = Color.FromArgb(38, 132, 255),
                HoverBackground = Color.FromArgb(60, 50, 76),
                PressedBackground = Color.FromArgb(78, 65, 95),
                BorderColor = Color.FromArgb(88, 74, 110),
                InputBorderColor = Color.FromArgb(105, 90, 132)
            },
            new ThemeDefinition
            {
                Id = "light",
                DisplayName = "Светлая",
                IsBuiltIn = true,
                BaseBackground = Color.FromArgb(245, 246, 248),
                PanelBackground = Color.FromArgb(233, 235, 240),
                ControlBackground = Color.FromArgb(255, 255, 255),
                PrimaryText = Color.FromArgb(34, 36, 42),
                SecondaryText = Color.FromArgb(84, 92, 106),
                AccentPrimary = Color.FromArgb(0, 102, 204),
                AccentSecondary = Color.FromArgb(102, 86, 166),
                HoverBackground = Color.FromArgb(222, 226, 234),
                PressedBackground = Color.FromArgb(205, 211, 223),
                BorderColor = Color.FromArgb(176, 185, 201),
                InputBorderColor = Color.FromArgb(158, 168, 188)
            },
            new ThemeDefinition
            {
                Id = DefaultThemeId,
                DisplayName = "Тёмная (зелёная)",
                IsBuiltIn = true,
                BaseBackground = Color.FromArgb(18, 18, 18),
                PanelBackground = Color.FromArgb(24, 34, 28),
                ControlBackground = Color.FromArgb(34, 48, 39),
                PrimaryText = Color.FromArgb(236, 242, 236),
                SecondaryText = Color.FromArgb(170, 188, 172),
                AccentPrimary = Color.FromArgb(76, 175, 80),
                AccentSecondary = Color.FromArgb(139, 195, 74),
                HoverBackground = Color.FromArgb(48, 66, 54),
                PressedBackground = Color.FromArgb(61, 83, 68),
                BorderColor = Color.FromArgb(72, 98, 79),
                InputBorderColor = Color.FromArgb(90, 118, 98)
            },
            new ThemeDefinition
            {
                Id = "light-warm",
                DisplayName = "Тёплая",
                IsBuiltIn = true,
                BaseBackground = Color.FromArgb(255, 249, 238),
                PanelBackground = Color.FromArgb(255, 242, 214),
                ControlBackground = Color.FromArgb(255, 252, 244),
                PrimaryText = Color.FromArgb(79, 57, 24),
                SecondaryText = Color.FromArgb(130, 99, 57),
                AccentPrimary = Color.FromArgb(235, 179, 52),
                AccentSecondary = Color.FromArgb(214, 142, 35),
                HoverBackground = Color.FromArgb(255, 234, 194),
                PressedBackground = Color.FromArgb(247, 214, 162),
                BorderColor = Color.FromArgb(220, 183, 122),
                InputBorderColor = Color.FromArgb(207, 165, 102)
            },
            new ThemeDefinition
            {
                Id = "light-fire",
                DisplayName = "Огненная",
                IsBuiltIn = true,
                BaseBackground = Color.FromArgb(255, 246, 238),
                PanelBackground = Color.FromArgb(255, 227, 204),
                ControlBackground = Color.FromArgb(255, 250, 243),
                PrimaryText = Color.FromArgb(88, 45, 21),
                SecondaryText = Color.FromArgb(140, 85, 56),
                AccentPrimary = Color.FromArgb(238, 120, 52),
                AccentSecondary = Color.FromArgb(214, 84, 36),
                HoverBackground = Color.FromArgb(255, 216, 184),
                PressedBackground = Color.FromArgb(248, 193, 152),
                BorderColor = Color.FromArgb(224, 157, 119),
                InputBorderColor = Color.FromArgb(212, 139, 102)
            },
            new ThemeDefinition
            {
                Id = "light-violet",
                DisplayName = "Фиолетовая",
                IsBuiltIn = true,
                BaseBackground = Color.FromArgb(248, 244, 255),
                PanelBackground = Color.FromArgb(235, 226, 250),
                ControlBackground = Color.FromArgb(252, 248, 255),
                PrimaryText = Color.FromArgb(62, 46, 96),
                SecondaryText = Color.FromArgb(102, 84, 142),
                AccentPrimary = Color.FromArgb(138, 92, 204),
                AccentSecondary = Color.FromArgb(110, 72, 176),
                HoverBackground = Color.FromArgb(224, 214, 244),
                PressedBackground = Color.FromArgb(208, 196, 233),
                BorderColor = Color.FromArgb(179, 163, 214),
                InputBorderColor = Color.FromArgb(160, 142, 199)
            }
        };

        private static readonly Dictionary<string, ThemeDefinition> BuiltInThemesById = BuiltInThemes.ToDictionary(
            theme => theme.Id,
            theme => theme,
            StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, ThemeDefinition> CustomThemesById = new(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, string> LegacyThemeNameMap = new(StringComparer.OrdinalIgnoreCase)
        {
            ["DarkModernLabBlue"] = "dark-modern-lab-blue",
            ["DarkModernLabViolet"] = "dark-modern-lab-violet",
            ["Light"] = "light",
            ["DarkModernLabGreen"] = DefaultThemeId,
            ["LightWarm"] = "light-warm",
            ["LightFire"] = "light-fire",
            ["LightViolet"] = "light-violet"
        };

        public static event EventHandler? ThemeChanged;
        public static event EventHandler? ThemesChanged;

        public static string CurrentThemeId { get; private set; } = DefaultThemeId;

        static ThemeManager()
        {
            try
            {
                foreach (ThemeDefinition customTheme in ThemeStorage.LoadCustomThemes())
                {
                    if (string.IsNullOrWhiteSpace(customTheme.Id) || BuiltInThemesById.ContainsKey(customTheme.Id))
                    {
                        continue;
                    }

                    CustomThemesById[customTheme.Id] = customTheme.CloneWith(customTheme.Id, customTheme.DisplayName, false);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки пользовательских тем: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static ThemeDefinition CurrentDefinition =>
            TryGetTheme(CurrentThemeId, out ThemeDefinition theme)
                ? theme
                : BuiltInThemesById[DefaultThemeId];

        public static IReadOnlyList<ThemeDefinition> GetAllThemes()
        {
            List<ThemeDefinition> result = new(BuiltInThemes.Count + CustomThemesById.Count);
            result.AddRange(BuiltInThemes);
            result.AddRange(CustomThemesById.Values.OrderBy(theme => theme.DisplayName, StringComparer.CurrentCultureIgnoreCase));
            return result;
        }

        public static bool TryGetTheme(string id, out ThemeDefinition theme)
        {
            if (!string.IsNullOrWhiteSpace(id))
            {
                if (CustomThemesById.TryGetValue(id, out theme))
                {
                    return true;
                }

                if (BuiltInThemesById.TryGetValue(id, out theme))
                {
                    return true;
                }

                if (TryMapLegacyThemeName(id, out string mappedId))
                {
                    return TryGetTheme(mappedId, out theme);
                }
            }

            theme = BuiltInThemesById[DefaultThemeId];
            return false;
        }

        public static bool TryResolveThemeId(string? rawValue, out string resolvedThemeId)
        {
            if (!string.IsNullOrWhiteSpace(rawValue) && TryGetTheme(rawValue, out ThemeDefinition theme))
            {
                resolvedThemeId = theme.Id;
                return true;
            }

            resolvedThemeId = DefaultThemeId;
            return false;
        }

        public static void SetTheme(string id)
        {
            if (!TryGetTheme(id, out ThemeDefinition theme))
            {
                return;
            }

            if (string.Equals(CurrentThemeId, theme.Id, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            CurrentThemeId = theme.Id;

            foreach (Form form in Application.OpenForms)
            {
                ApplyTheme(form);
            }

            ThemeChanged?.Invoke(null, EventArgs.Empty);
        }

        public static void AddOrUpdateCustomTheme(ThemeDefinition theme)
        {
            if (theme is null)
            {
                throw new ArgumentNullException(nameof(theme));
            }

            if (string.IsNullOrWhiteSpace(theme.Id))
            {
                throw new ArgumentException("Theme id must be provided.", nameof(theme));
            }

            if (BuiltInThemesById.ContainsKey(theme.Id))
            {
                throw new InvalidOperationException("Cannot override built-in theme.");
            }

            ThemeDefinition customTheme = theme.CloneWith(theme.Id, theme.DisplayName, false);
            CustomThemesById[customTheme.Id] = customTheme;
            SaveCustomThemes();
            ThemesChanged?.Invoke(null, EventArgs.Empty);

            if (string.Equals(CurrentThemeId, customTheme.Id, StringComparison.OrdinalIgnoreCase))
            {
                ThemeChanged?.Invoke(null, EventArgs.Empty);
            }
        }

        public static bool RemoveCustomTheme(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || !CustomThemesById.Remove(id))
            {
                return false;
            }

            SaveCustomThemes();
            ThemesChanged?.Invoke(null, EventArgs.Empty);

            if (string.Equals(CurrentThemeId, id, StringComparison.OrdinalIgnoreCase))
            {
                SetTheme(DefaultThemeId);
            }

            return true;
        }

        public static ThemeDefinition DuplicateTheme(string sourceId, string newId, string newDisplayName)
        {
            if (!TryGetTheme(sourceId, out ThemeDefinition sourceTheme))
            {
                throw new KeyNotFoundException($"Theme with id '{sourceId}' was not found.");
            }

            ThemeDefinition duplicate = sourceTheme.CloneWith(newId, newDisplayName, false);
            AddOrUpdateCustomTheme(duplicate);
            return duplicate;
        }

        public static void RegisterForm(Form form)
        {
            ApplyTheme(form);

            EventHandler handler = (_, _) => ApplyTheme(form);
            ThemeChanged += handler;

            form.Disposed += (_, _) => ThemeChanged -= handler;
            form.ControlAdded += (_, _) => ApplyTheme(form);
        }

        public static void ApplyTheme(Form form)
        {
            var theme = CurrentDefinition;
            form.SuspendLayout();
            form.BackColor = theme.BaseBackground;
            form.ForeColor = theme.PrimaryText;

            ApplyThemeToControl(form, theme);

            form.ResumeLayout(true);
            form.Invalidate(true);
        }


        private static void SaveCustomThemes()
        {
            try
            {
                ThemeStorage.SaveCustomThemes(CustomThemesById.Values);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения пользовательских тем: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static bool TryMapLegacyThemeName(string rawThemeName, out string themeId)
        {
            return LegacyThemeNameMap.TryGetValue(rawThemeName, out themeId!);
        }

        private static void ApplyThemeToControl(Control control, ThemeDefinition theme)
        {
            switch (control)
            {
                case Form themedForm:
                    themedForm.BackColor = theme.BaseBackground;
                    themedForm.ForeColor = theme.PrimaryText;
                    break;
                case Button button:
                    StyleButton(button, theme);
                    break;
                case Label label:
                    label.BackColor = Color.Transparent;
                    label.ForeColor = theme.PrimaryText;
                    break;
                case TableLayoutPanel tableLayoutPanel:
                    tableLayoutPanel.BackColor = theme.PanelBackground;
                    tableLayoutPanel.ForeColor = theme.PrimaryText;
                    break;
                case Panel panel:
                    panel.BackColor = theme.PanelBackground;
                    panel.ForeColor = theme.PrimaryText;
                    break;
                case TextBox textBox:
                    StyleTextBox(textBox, theme);
                    break;
                case RichTextBox richTextBox:
                    richTextBox.BackColor = theme.ControlBackground;
                    richTextBox.ForeColor = theme.SecondaryText;
                    richTextBox.BorderStyle = BorderStyle.FixedSingle;
                    break;
                case TreeView treeView:
                    treeView.BackColor = theme.PanelBackground;
                    treeView.ForeColor = theme.PrimaryText;
                    treeView.BorderStyle = BorderStyle.FixedSingle;
                    break;
                case ProgressBar progressBar:
                    progressBar.BackColor = theme.PanelBackground;
                    progressBar.ForeColor = theme.AccentPrimary;
                    break;
                case CheckBox checkBox:
                    checkBox.BackColor = theme.PanelBackground;
                    checkBox.ForeColor = theme.PrimaryText;
                    break;
                default:
                    if (control is not PictureBox)
                    {
                        control.BackColor = theme.PanelBackground;
                        control.ForeColor = theme.PrimaryText;
                    }
                    break;
            }

            foreach (Control child in control.Controls)
            {
                ApplyThemeToControl(child, theme);
            }
        }

        private static void StyleButton(Button button, ThemeDefinition theme)
        {
            button.UseVisualStyleBackColor = false;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = theme.BorderColor;
            button.FlatAppearance.MouseOverBackColor = theme.HoverBackground;
            button.FlatAppearance.MouseDownBackColor = theme.PressedBackground;
            button.BackColor = theme.AccentPrimary;
            button.ForeColor = theme.PrimaryText;
        }

        private static void StyleTextBox(TextBox textBox, ThemeDefinition theme)
        {
            textBox.BackColor = theme.ControlBackground;
            textBox.ForeColor = theme.PrimaryText;
            textBox.BorderStyle = BorderStyle.FixedSingle;
        }
    }
}
