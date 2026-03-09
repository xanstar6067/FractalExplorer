using System.Drawing;
using System.Windows.Forms;

namespace FractalExplorer.Utilities.Theme
{
    public sealed class ThemeControlSubscriptionManager
    {
        private readonly ThemeControlStyler _styler;
        private readonly Func<ThemeDefinition> _currentTheme;
        private readonly Dictionary<Control, ControlEventHandler> _controlAddedHandlers = new();
        private readonly Dictionary<Control, EventHandler> _disposedHandlers = new();
        private readonly Dictionary<ComboBox, DrawItemEventHandler> _comboBoxDrawHandlers = new();
        private readonly Dictionary<Button, EventHandler> _buttonEnabledChangedHandlers = new();
        private readonly Dictionary<CheckBox, PaintEventHandler> _checkBoxPaintHandlers = new();
        private readonly Dictionary<CheckBox, EventHandler> _checkBoxRefreshHandlers = new();

        public ThemeControlSubscriptionManager(ThemeControlStyler styler, Func<ThemeDefinition> currentTheme)
        {
            _styler = styler;
            _currentTheme = currentTheme;
        }

        public void Attach(Control root)
        {
            RegisterControl(root);
            _styler.ApplyThemeToControl(root, _currentTheme());
        }

        public void Detach(Control root)
        {
            UnregisterControlRecursive(root);
        }

        private void RegisterControl(Control control)
        {
            if (!_controlAddedHandlers.ContainsKey(control))
            {
                ControlEventHandler addedHandler = (_, args) =>
                {
                    RegisterControl(args.Control);
                    _styler.ApplyThemeToControl(args.Control, _currentTheme());
                };

                _controlAddedHandlers[control] = addedHandler;
                control.ControlAdded += addedHandler;
            }

            if (!_disposedHandlers.ContainsKey(control))
            {
                EventHandler disposedHandler = (_, _) => UnregisterControlRecursive(control);
                _disposedHandlers[control] = disposedHandler;
                control.Disposed += disposedHandler;
            }

            if (control is ComboBox comboBox)
            {
                AttachComboBoxOwnerDraw(comboBox);
            }

            if (control is Button button)
            {
                AttachButtonRefreshHandler(button);
            }

            if (control is CheckBox checkBox)
            {
                AttachDisabledCheckBoxTextRenderer(checkBox);
            }

            foreach (Control child in control.Controls)
            {
                RegisterControl(child);
            }
        }

        private void UnregisterControlRecursive(Control control)
        {
            foreach (Control child in control.Controls)
            {
                UnregisterControlRecursive(child);
            }

            if (_controlAddedHandlers.TryGetValue(control, out ControlEventHandler? addedHandler))
            {
                control.ControlAdded -= addedHandler;
                _controlAddedHandlers.Remove(control);
            }

            if (_disposedHandlers.TryGetValue(control, out EventHandler? disposedHandler))
            {
                control.Disposed -= disposedHandler;
                _disposedHandlers.Remove(control);
            }

            if (control is ComboBox comboBox)
            {
                DetachComboBoxOwnerDraw(comboBox);
            }

            if (control is Button button)
            {
                DetachButtonRefreshHandler(button);
            }

            if (control is CheckBox checkBox)
            {
                DetachDisabledCheckBoxTextRenderer(checkBox);
            }
        }

        private void AttachComboBoxOwnerDraw(ComboBox comboBox)
        {
            if (comboBox.DropDownStyle != ComboBoxStyle.DropDownList)
            {
                DetachComboBoxOwnerDraw(comboBox);
                comboBox.DrawMode = DrawMode.Normal;
                return;
            }

            comboBox.DrawMode = DrawMode.OwnerDrawFixed;

            if (_comboBoxDrawHandlers.ContainsKey(comboBox))
            {
                comboBox.Invalidate();
                return;
            }

            DrawItemEventHandler drawHandler = (_, args) => DrawComboBoxItem(comboBox, args, _currentTheme());
            _comboBoxDrawHandlers[comboBox] = drawHandler;
            comboBox.DrawItem += drawHandler;
            comboBox.Invalidate();
        }

        private void DetachComboBoxOwnerDraw(ComboBox comboBox)
        {
            if (_comboBoxDrawHandlers.TryGetValue(comboBox, out DrawItemEventHandler? drawHandler))
            {
                comboBox.DrawItem -= drawHandler;
                _comboBoxDrawHandlers.Remove(comboBox);
            }
        }

        private void DrawComboBoxItem(ComboBox comboBox, DrawItemEventArgs args, ThemeDefinition theme)
        {
            if (args.Index < 0)
            {
                return;
            }

            bool isSelected = (args.State & DrawItemState.Selected) == DrawItemState.Selected;
            Color backgroundColor = isSelected ? theme.HoverBackground : theme.ControlBackground;

            using SolidBrush backgroundBrush = new(backgroundColor);
            using SolidBrush textBrush = new(theme.PrimaryText);

            args.Graphics.FillRectangle(backgroundBrush, args.Bounds);

            string itemText = comboBox.GetItemText(comboBox.Items[args.Index]);
            args.Graphics.DrawString(itemText, args.Font ?? comboBox.Font, textBrush, args.Bounds);
            args.DrawFocusRectangle();
        }

        private void AttachButtonRefreshHandler(Button button)
        {
            if (_buttonEnabledChangedHandlers.ContainsKey(button))
            {
                return;
            }

            EventHandler refreshHandler = (_, _) => button.Invalidate();
            _buttonEnabledChangedHandlers[button] = refreshHandler;
            button.EnabledChanged += refreshHandler;
        }

        private void DetachButtonRefreshHandler(Button button)
        {
            if (_buttonEnabledChangedHandlers.TryGetValue(button, out EventHandler? refreshHandler))
            {
                button.EnabledChanged -= refreshHandler;
                _buttonEnabledChangedHandlers.Remove(button);
            }
        }

        private void AttachDisabledCheckBoxTextRenderer(CheckBox checkBox)
        {
            if (!_checkBoxPaintHandlers.ContainsKey(checkBox))
            {
                PaintEventHandler paintHandler = (_, args) => DrawCheckBoxDisabledTextOverride(checkBox, args, _currentTheme());
                _checkBoxPaintHandlers[checkBox] = paintHandler;
                checkBox.Paint += paintHandler;
            }

            if (_checkBoxRefreshHandlers.ContainsKey(checkBox))
            {
                return;
            }

            EventHandler refreshHandler = (_, _) => checkBox.Invalidate();
            _checkBoxRefreshHandlers[checkBox] = refreshHandler;
            checkBox.EnabledChanged += refreshHandler;
            checkBox.TextChanged += refreshHandler;
            checkBox.AppearanceChanged += refreshHandler;
            checkBox.SizeChanged += refreshHandler;
            checkBox.RightToLeftChanged += refreshHandler;
            checkBox.FontChanged += refreshHandler;
        }

        private void DetachDisabledCheckBoxTextRenderer(CheckBox checkBox)
        {
            if (_checkBoxPaintHandlers.TryGetValue(checkBox, out PaintEventHandler? paintHandler))
            {
                checkBox.Paint -= paintHandler;
                _checkBoxPaintHandlers.Remove(checkBox);
            }

            if (_checkBoxRefreshHandlers.TryGetValue(checkBox, out EventHandler? refreshHandler))
            {
                checkBox.EnabledChanged -= refreshHandler;
                checkBox.TextChanged -= refreshHandler;
                checkBox.AppearanceChanged -= refreshHandler;
                checkBox.SizeChanged -= refreshHandler;
                checkBox.RightToLeftChanged -= refreshHandler;
                checkBox.FontChanged -= refreshHandler;
                _checkBoxRefreshHandlers.Remove(checkBox);
            }
        }

        private static void DrawCheckBoxDisabledTextOverride(CheckBox checkBox, PaintEventArgs args, ThemeDefinition theme)
        {
            if (checkBox.Enabled || string.IsNullOrEmpty(checkBox.Text))
            {
                return;
            }

            Color disabledTextColor = MixColors(theme.PrimaryText, theme.PanelBackground, 0.58f);
            Rectangle textBounds = GetCheckBoxTextBounds(checkBox, args.Graphics);
            if (textBounds.Width <= 0 || textBounds.Height <= 0)
            {
                return;
            }

            using SolidBrush backgroundBrush = new(checkBox.BackColor);
            args.Graphics.FillRectangle(backgroundBrush, textBounds);

            TextFormatFlags flags = TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix;
            if (checkBox.RightToLeft == RightToLeft.Yes)
            {
                flags |= TextFormatFlags.RightToLeft;
            }

            flags |= IsCheckGlyphOnRight(checkBox.CheckAlign)
                ? TextFormatFlags.Right
                : TextFormatFlags.Left;

            TextRenderer.DrawText(args.Graphics, checkBox.Text, checkBox.Font, textBounds, disabledTextColor, flags);
        }

        private static Rectangle GetCheckBoxTextBounds(CheckBox checkBox, Graphics graphics)
        {
            Size glyphSize = CheckBoxRenderer.GetGlyphSize(graphics, System.Windows.Forms.VisualStyles.CheckBoxState.UncheckedNormal);
            int spacing = 4;
            Rectangle client = checkBox.ClientRectangle;

            if (IsCheckGlyphOnRight(checkBox.CheckAlign))
            {
                return new Rectangle(client.Left, client.Top, Math.Max(0, client.Width - glyphSize.Width - spacing), client.Height);
            }

            return new Rectangle(client.Left + glyphSize.Width + spacing, client.Top, Math.Max(0, client.Width - glyphSize.Width - spacing), client.Height);
        }

        private static bool IsCheckGlyphOnRight(ContentAlignment checkAlign)
        {
            return checkAlign == ContentAlignment.TopRight
                   || checkAlign == ContentAlignment.MiddleRight
                   || checkAlign == ContentAlignment.BottomRight;
        }

        private static Color MixColors(Color source, Color target, float targetWeight)
        {
            float clampedWeight = targetWeight < 0f ? 0f : targetWeight > 1f ? 1f : targetWeight;
            float sourceWeight = 1f - clampedWeight;

            return Color.FromArgb(
                (int)Math.Round(source.R * sourceWeight + target.R * clampedWeight),
                (int)Math.Round(source.G * sourceWeight + target.G * clampedWeight),
                (int)Math.Round(source.B * sourceWeight + target.B * clampedWeight));
        }
    }
}
