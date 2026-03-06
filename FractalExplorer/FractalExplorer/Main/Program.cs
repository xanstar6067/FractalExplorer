using FractalExplorer.Resources;
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

            AppTheme startupTheme;
            if (!ThemeManager.TryGetThemeByName(Settings.Default.UiTheme, out startupTheme))
            {
                startupTheme = AppTheme.DarkModernLabGreen;
                Settings.Default.UiTheme = startupTheme.ToString();
                Settings.Default.Save();
            }

            ThemeManager.SetTheme(startupTheme);
            Application.Run(new LauncherHubForm());
        }
    }
}