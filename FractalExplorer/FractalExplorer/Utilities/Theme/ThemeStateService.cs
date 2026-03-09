namespace FractalExplorer.Utilities.Theme
{
    public sealed class ThemeStateService
    {
        public event EventHandler? ThemeChanged;
        public event EventHandler? ThemesChanged;

        public string CurrentThemeId { get; private set; }

        public ThemeStateService(string defaultThemeId)
        {
            CurrentThemeId = defaultThemeId;
        }

        public bool SetTheme(string id)
        {
            if (string.Equals(CurrentThemeId, id, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            CurrentThemeId = id;
            ThemeChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }

        public void NotifyThemesChanged() => ThemesChanged?.Invoke(this, EventArgs.Empty);
    }
}
