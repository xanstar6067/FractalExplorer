using System;
using System.Windows.Forms;

namespace FractalExplorer.Utilities.Theme
{
    public enum ThemeControlRole
    {
        Primary,
        Secondary
    }

    public static class ThemeControlRoleResolver
    {
        private const string RolePrefix = "theme-role:";

        public static bool TryResolve(Control control, out ThemeControlRole role)
        {
            role = ThemeControlRole.Primary;

            if (control?.Tag is not string tag || string.IsNullOrWhiteSpace(tag))
            {
                return false;
            }

            string[] tokens = tag.Split(new[] { ';', ',', '|' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string token in tokens)
            {
                string normalized = token.Trim();
                if (!normalized.StartsWith(RolePrefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string roleName = normalized.Substring(RolePrefix.Length).Trim();
                if (string.Equals(roleName, "secondary", StringComparison.OrdinalIgnoreCase))
                {
                    role = ThemeControlRole.Secondary;
                    return true;
                }

                if (string.Equals(roleName, "primary", StringComparison.OrdinalIgnoreCase))
                {
                    role = ThemeControlRole.Primary;
                    return true;
                }
            }

            return false;
        }
    }
}
