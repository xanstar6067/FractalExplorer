using System.IO;
using System.Text;

namespace FractalExplorerWPF.Infrastructure;

/// <summary>
/// Минимальный потокобезопасный файловый журнал ошибок без внешних зависимостей.
/// Пишет в <see cref="AppPaths.LogsDirectory"/>/errors.log с ротацией по размеру.
/// Все операции ввода-вывода выполняются по принципу «максимум усилий»: собственные
/// сбои журналирования подавляются, чтобы обработчик исключений сам не стал источником сбоя.
/// </summary>
public static class CrashLogger
{
    private const long MaxFileBytes = 2 * 1024 * 1024;

    private static readonly object Gate = new();

    public static string LogFilePath => Path.Combine(AppPaths.LogsDirectory, "errors.log");

    /// <summary>Записывает исключение с указанием источника (имя обработчика/потока).</summary>
    public static void Log(string source, Exception exception)
    {
        var builder = new StringBuilder();
        builder.Append("Источник: ").AppendLine(source);
        builder.Append("Поток: ").Append(Environment.CurrentManagedThreadId)
            .Append(" (").Append(Thread.CurrentThread.Name ?? "без имени").AppendLine(")");
        builder.AppendLine(exception.ToString());
        Write(builder.ToString());
    }

    /// <summary>Записывает произвольное диагностическое сообщение.</summary>
    public static void Log(string message) => Write(message);

    private static void Write(string body)
    {
        try
        {
            lock (Gate)
            {
                Directory.CreateDirectory(AppPaths.LogsDirectory);
                RotateIfNeeded();

                string entry = $"===== {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} =====" +
                    Environment.NewLine + body.TrimEnd() + Environment.NewLine + Environment.NewLine;
                File.AppendAllText(LogFilePath, entry, Encoding.UTF8);
            }
        }
        catch
        {
            // Журналирование не должно приводить к каскадному сбою.
        }
    }

    private static void RotateIfNeeded()
    {
        try
        {
            var current = new FileInfo(LogFilePath);
            if (!current.Exists || current.Length < MaxFileBytes) return;

            string archive = LogFilePath + ".1";
            if (File.Exists(archive)) File.Delete(archive);
            File.Move(LogFilePath, archive);
        }
        catch
        {
            // Если ротация не удалась, продолжаем дописывать в текущий файл.
        }
    }
}
