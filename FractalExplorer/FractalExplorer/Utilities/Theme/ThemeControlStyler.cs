using System.Drawing;
using System.Windows.Forms;

namespace FractalExplorer.Utilities.Theme
{
    public sealed class ThemeControlStyler
    {
        private readonly Func<ThemeDefinition> _currentTheme;

        public ThemeControlStyler(Func<ThemeDefinition> currentTheme)
        {
            _currentTheme = currentTheme;
        }

        public void ApplyThemeToForm(Form form)
        {
            ThemeDefinition theme = _currentTheme();
            form.SuspendLayout();
            form.BackColor = theme.BaseBackground;
            form.ForeColor = theme.PrimaryText;
            ApplyThemeToControl(form, theme);
            form.ResumeLayout(true);
        }

        public void ApplyThemeToControl(Control control, ThemeDefinition theme)
        {
            switch (control)
            {
                case Panel panel: StylePanel(panel, theme); break;
                case Label label: StyleLabel(label, theme); break;
                case TextBox textBox: StyleTextBox(textBox, theme); break;
                case ComboBox comboBox: StyleComboBox(comboBox, theme); break;
                case Button button: StyleButton(button, theme); break;
                case CheckBox checkBox: StyleCheckBox(checkBox, theme); break;
                default:
                    control.BackColor = theme.PanelBackground;
                    control.ForeColor = theme.PrimaryText;
                    break;
            }

            foreach (Control child in control.Controls)
            {
                ApplyThemeToControl(child, theme);
            }
        }

        public void StyleButton(Button button, ThemeDefinition theme)
        {
            ThemeControlRole role = ThemeControlRoleResolver.TryResolve(button, out ThemeControlRole explicitRole)
                ? explicitRole
                : LegacyControlRoleResolver.Resolve(button);

            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = theme.BorderColor;
            button.BackColor = role == ThemeControlRole.Secondary ? theme.AccentSecondary : theme.AccentPrimary;
            button.ForeColor = theme.PrimaryText;
        }

        public void StyleLabel(Label label, ThemeDefinition theme)
        {
            label.BackColor = Color.Transparent;
            label.ForeColor = theme.SecondaryText;
        }

        public void StylePanel(Panel panel, ThemeDefinition theme)
        {
            panel.BackColor = theme.PanelBackground;
            panel.ForeColor = theme.PrimaryText;
        }

        public void StyleTextBox(TextBox textBox, ThemeDefinition theme)
        {
            textBox.BackColor = theme.ControlBackground;
            textBox.ForeColor = theme.PrimaryText;
            textBox.BorderStyle = BorderStyle.FixedSingle;
        }

        public void StyleComboBox(ComboBox comboBox, ThemeDefinition theme)
        {
            comboBox.BackColor = theme.ControlBackground;
            comboBox.ForeColor = theme.PrimaryText;
            comboBox.FlatStyle = FlatStyle.Flat;
        }

        public void StyleCheckBox(CheckBox checkBox, ThemeDefinition theme)
        {
            checkBox.BackColor = theme.PanelBackground;
            checkBox.ForeColor = theme.PrimaryText;
        }
    }
}
