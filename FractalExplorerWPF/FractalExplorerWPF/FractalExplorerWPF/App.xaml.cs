using System.Windows;
using FractalExplorerWPF.Infrastructure;
using FractalExplorerWPF.Theming;

namespace FractalExplorerWPF;

public partial class App : Application
{
    private void App_OnStartup(object sender, StartupEventArgs e)
    {
        GlobalExceptionHandler.Install(this);
        ThemeManager.Initialize(this);
        var mainWindow = new MainWindow();
        MainWindow = mainWindow;
        mainWindow.Show();

        if (!string.IsNullOrWhiteSpace(ThemeManager.InitializationWarning))
        {
            MessageBox.Show(mainWindow, ThemeManager.InitializationWarning, "Темы оформления",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
