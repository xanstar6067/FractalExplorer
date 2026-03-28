using FractalExplorer.Properties;
using FractalExplorer.Utilities.Theme;

namespace FractalExplorer.Main
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

            string startupThemeId;
            if (!ThemeManager.TryResolveThemeId(Settings.Default.UiTheme, out startupThemeId))
            {
                Settings.Default.UiTheme = startupThemeId;
                Settings.Default.Save();
            }

            ThemeManager.SetTheme(startupThemeId);
            Application.Run(new LauncherHubForm());
        }
    }
}