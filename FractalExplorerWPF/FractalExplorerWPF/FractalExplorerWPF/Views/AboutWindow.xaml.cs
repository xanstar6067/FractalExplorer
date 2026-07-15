using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

namespace FractalExplorerWPF.Views;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
    }

    private void RepositoryLink_OnRequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri)
            {
                UseShellExecute = true
            });
        }
        catch
        {
            MessageBox.Show(
                this,
                "Не удалось открыть ссылку в браузере.",
                "О программе",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        e.Handled = true;
    }
}
