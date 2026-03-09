using System.Drawing;
using System.Windows.Forms;

namespace FractalExplorer.Utilities.Theme
{
    public static class ThemeManager
    {
        private static readonly ThemeCatalog Catalog = new();
        private static readonly ThemeStateService State = new(BuiltInThemeProvider.DefaultThemeId);
        private static readonly ThemeControlStyler Styler = new(() => CurrentDefinition);
        private static readonly ThemeControlSubscriptionManager SubscriptionManager = new(Styler, () => CurrentDefinition);

        public static event EventHandler? ThemeChanged
        {
            add => State.ThemeChanged += value;
            remove => State.ThemeChanged -= value;
        }

        public static event EventHandler? ThemesChanged
        {
            add => State.ThemesChanged += value;
            remove => State.ThemesChanged -= value;
        }

        public static string DefaultThemeId => Catalog.DefaultThemeId;
        public static string CurrentThemeId => State.CurrentThemeId;

        public static ThemeDefinition CurrentDefinition =>
            Catalog.TryGetTheme(CurrentThemeId, out ThemeDefinition theme)
                ? theme
                : Catalog.GetAllThemes().First();

        public static IReadOnlyList<ThemeDefinition> GetAllThemes() => Catalog.GetAllThemes();

        public static bool TryGetTheme(string id, out ThemeDefinition theme) => Catalog.TryGetTheme(id, out theme);
        public static bool TryResolveThemeId(string? rawValue, out string resolvedThemeId) => Catalog.TryResolveThemeId(rawValue, out resolvedThemeId);

        public static void SetTheme(string id)
        {
            if (!Catalog.TryGetTheme(id, out ThemeDefinition theme)) return;

            if (State.SetTheme(theme.Id))
            {
                foreach (Form form in Application.OpenForms)
                {
                    Styler.ApplyThemeToForm(form);
                }
            }
        }

        public static ThemeOperationResult AddOrUpdateCustomTheme(ThemeDefinition theme)
        {
            ThemeOperationResult result = Catalog.AddOrUpdateCustomTheme(theme);
            if (result.IsSuccess) State.NotifyThemesChanged();
            return result;
        }

        public static ThemeOperationResult<bool> RemoveCustomTheme(string id)
        {
            ThemeOperationResult<bool> result = Catalog.RemoveCustomTheme(id);
            if (!result.IsSuccess || result.Value != true) return result;

            State.NotifyThemesChanged();
            if (string.Equals(CurrentThemeId, id, StringComparison.OrdinalIgnoreCase))
            {
                SetTheme(DefaultThemeId);
            }

            return result;
        }

        public static ThemeOperationResult<ThemeDefinition> DuplicateTheme(string sourceId, string newId, string newDisplayName)
        {
            ThemeOperationResult<ThemeDefinition> result = Catalog.DuplicateTheme(sourceId, newId, newDisplayName);
            if (result.IsSuccess && result.Value is not null) State.NotifyThemesChanged();
            return result;
        }

        public static void RegisterForm(Form form)
        {
            ApplyTheme(form);
            SubscriptionManager.Attach(form);

            EventHandler handler = (_, _) => ApplyTheme(form);
            ThemeChanged += handler;
            form.Disposed += (_, _) =>
            {
                ThemeChanged -= handler;
                SubscriptionManager.Detach(form);
            };
        }

        public static void ApplyTheme(Form form) => Styler.ApplyThemeToForm(form);

        public static Color GetInteractiveStateColor(Color background, bool hovered)
        {
            (Color normal, Color hover) = ThemeAccessibilityService.GetInteractiveStateColors(CurrentDefinition, background);
            return hovered ? hover : normal;
        }

        public static Color GetInteractiveStateColor(ThemeDefinition theme, Color background, bool hovered)
        {
            (Color normal, Color hover) = ThemeAccessibilityService.GetInteractiveStateColors(theme, background);
            return hovered ? hover : normal;
        }

        public static (Color Normal, Color Hover) GetInteractiveStateColors(Color background) =>
            ThemeAccessibilityService.GetInteractiveStateColors(CurrentDefinition, background);

        public static (Color Normal, Color Hover) GetInteractiveStateColors(ThemeDefinition theme, Color background) =>
            ThemeAccessibilityService.GetInteractiveStateColors(theme, background);

        public static Color GetInteractiveBorderColor(Color background, bool hovered) => GetInteractiveStateColor(background, hovered);
        public static Color GetInteractiveBorderColor(ThemeDefinition theme, Color background, bool hovered) => GetInteractiveStateColor(theme, background, hovered);
        public static Color GetAccessibleAccentOn(Color background, double minContrast = ThemeAccessibilityService.NonTextUiContrastRatio) =>
            ThemeAccessibilityService.EnsureMinimumContrast(CurrentDefinition.AccentPrimary, background, minContrast);
    }
}
