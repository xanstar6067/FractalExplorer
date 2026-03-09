using System;
using System.Windows.Forms;

namespace FractalExplorer.Utilities.Theme
{
    public static class LegacyControlRoleResolver
    {
        public static ThemeControlRole Resolve(Control control)
        {
            if (control is Button button)
            {
                if (button.DialogResult == DialogResult.Cancel)
                {
                    return ThemeControlRole.Secondary;
                }
            }

            string text = control?.Text?.Trim() ?? string.Empty;
            if (ContainsAny(text, "отмена", "закрыть", "удалить", "cancel", "close", "delete", "remove"))
            {
                return ThemeControlRole.Secondary;
            }

            string name = control?.Name?.Trim() ?? string.Empty;
            if (ContainsAny(name, "cancel", "close", "delete", "remove"))
            {
                return ThemeControlRole.Secondary;
            }

            return ThemeControlRole.Primary;
        }

        private static bool ContainsAny(string value, params string[] keywords)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            foreach (string keyword in keywords)
            {
                if (value.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
