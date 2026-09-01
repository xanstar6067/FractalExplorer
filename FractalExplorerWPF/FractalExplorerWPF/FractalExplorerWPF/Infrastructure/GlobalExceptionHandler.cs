using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using FractalExplorerWPF.Views;

namespace FractalExplorerWPF.Infrastructure;

/// <summary>
/// Единая точка перехвата необработанных исключений приложения:
/// <list type="bullet">
///   <item><see cref="Application.DispatcherUnhandledException"/> — исключения UI-потока
///     (async void-обработчики, тики <c>DispatcherTimer</c>, конструкторы окон). Восстановимо:
///     журналируется, показывается диалог, пользователь выбирает «продолжить»/«закрыть».</item>
///   <item><see cref="AppDomain.UnhandledException"/> — фатальные исключения любого потока.
///     Процесс уже завершается: только журнал и финальное сообщение.</item>
///   <item><see cref="TaskScheduler.UnobservedTaskException"/> — «потерянные» исключения задач:
///     журналируются и помечаются обработанными.</item>
/// </list>
/// </summary>
public static class GlobalExceptionHandler
{
    private static readonly object Gate = new();
    private static Application? _application;
    private static bool _installed;
    private static bool _dialogVisible;
    private static bool _shuttingDown;

    public static void Install(Application application)
    {
        ArgumentNullException.ThrowIfNull(application);
        if (_installed) return;
        _installed = true;
        _application = application;

        application.DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        // OS-диалог аварии не должен появляться ни при каком исходе.
        e.Handled = true;

        CrashLogger.Log("Application.DispatcherUnhandledException", e.Exception);

        if (_shuttingDown) return;

        bool alreadyVisible;
        lock (Gate)
        {
            alreadyVisible = _dialogVisible;
            _dialogVisible = true;
        }

        if (alreadyVisible)
        {
            // Идёт показ предыдущего диалога (например, «шторм» тиков таймера) — только журнал.
            return;
        }

        try
        {
            CrashDialogChoice choice = ShowRecoverableDialog(e.Exception);
            if (choice == CrashDialogChoice.Shutdown)
            {
                _shuttingDown = true;
                _application?.Shutdown();
            }
        }
        catch (Exception dialogException)
        {
            CrashLogger.Log("GlobalExceptionHandler.ShowRecoverableDialog", dialogException);
        }
        finally
        {
            lock (Gate) _dialogVisible = false;
        }
    }

    private static void OnAppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception
            ?? new Exception($"Необработанный объект исключения: {e.ExceptionObject}");

        CrashLogger.Log(
            $"AppDomain.UnhandledException (IsTerminating={e.IsTerminating})", exception);

        lock (Gate)
        {
            if (_dialogVisible) return;
            _dialogVisible = true;
        }

        try
        {
            // Процесс завершается и поток может быть не UI-шным: используем блокирующий Win32-диалог.
            MessageBox.Show(
                "Произошла критическая ошибка, работа приложения будет завершена." +
                Environment.NewLine + Environment.NewLine +
                Describe(exception) + Environment.NewLine + Environment.NewLine +
                $"Журнал: {CrashLogger.LogFilePath}",
                "Критическая ошибка",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        catch
        {
            // На этом этапе показать что-либо уже может быть невозможно.
        }
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        CrashLogger.Log("TaskScheduler.UnobservedTaskException", e.Exception);
        e.SetObserved();
    }

    private static CrashDialogChoice ShowRecoverableDialog(Exception exception)
    {
        var dialog = new CrashDialogWindow(
            headerText: "Произошла непредвиденная ошибка.",
            summaryText: "Приложение может продолжить работу, но его состояние может быть " +
                "нестабильным. Рекомендуется сохранить важные результаты и перезапустить программу." +
                Environment.NewLine + Environment.NewLine + Describe(exception),
            details: exception.ToString(),
            logFilePath: CrashLogger.LogFilePath,
            allowContinue: true);

        try
        {
            Window? owner = ResolveOwner();
            if (owner != null && !ReferenceEquals(owner, dialog))
            {
                dialog.Owner = owner;
                dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
        }
        catch
        {
            // Не удалось привязать владельца — показываем диалог по центру экрана.
        }

        dialog.ShowDialog();
        return dialog.Choice;
    }

    private static Window? ResolveOwner()
    {
        if (_application == null) return null;

        Window? active = null;
        foreach (Window window in _application.Windows)
        {
            if (!window.IsLoaded) continue;
            if (window.IsActive) return window;
            active ??= window;
        }

        return active ?? _application.MainWindow;
    }

    private static string Describe(Exception exception) =>
        $"{exception.GetType().Name}: {exception.Message}";
}
