using System.Diagnostics;
using System.IO;
using System.Windows;

namespace FractalExplorerWPF.Views;

public enum CrashDialogChoice
{
    Continue,
    Shutdown
}

/// <summary>
/// Диалог восстановления после необработанного исключения: краткое описание,
/// разворачиваемые подробности, копирование, открытие журнала и выбор
/// «продолжить работу» / «закрыть приложение».
/// </summary>
public partial class CrashDialogWindow : Window
{
    private readonly string _details;
    private readonly string _logFilePath;

    public CrashDialogWindow(string headerText, string summaryText, string details,
        string logFilePath, bool allowContinue)
    {
        InitializeComponent();

        _details = details ?? string.Empty;
        _logFilePath = logFilePath ?? string.Empty;

        HeaderText.Text = headerText;
        SummaryText.Text = summaryText;
        DetailsText.Text = _details;
        LogPathText.Text = string.IsNullOrWhiteSpace(_logFilePath)
            ? string.Empty
            : $"Журнал: {_logFilePath}";

        Choice = allowContinue ? CrashDialogChoice.Continue : CrashDialogChoice.Shutdown;

        if (!allowContinue)
        {
            ContinueButton.Visibility = Visibility.Collapsed;
            ShutdownButton.IsDefault = true;
            ShutdownButton.Content = "Закрыть";
        }
    }

    public CrashDialogChoice Choice { get; private set; }

    private void CopyButton_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            Clipboard.SetText(_details);
        }
        catch
        {
            // Буфер обмена может быть временно занят другим процессом — не критично.
        }
    }

    private void OpenLogButton_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            string target = File.Exists(_logFilePath)
                ? _logFilePath
                : Path.GetDirectoryName(_logFilePath) ?? _logFilePath;

            Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
        }
        catch
        {
            MessageBox.Show(this, "Не удалось открыть журнал.", Title,
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void ContinueButton_OnClick(object sender, RoutedEventArgs e)
    {
        Choice = CrashDialogChoice.Continue;
        DialogResult = true;
    }

    private void ShutdownButton_OnClick(object sender, RoutedEventArgs e)
    {
        Choice = CrashDialogChoice.Shutdown;
        DialogResult = true;
    }
}
