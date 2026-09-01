using System.IO;

namespace FractalExplorerWPF.Infrastructure;

public static class AppPaths
{
    public static string SavesDirectory => Path.Combine(AppContext.BaseDirectory, "Saves");

    public static string LogsDirectory => Path.Combine(AppContext.BaseDirectory, "Logs");

    public static string EnsureSavesDirectory()
    {
        Directory.CreateDirectory(SavesDirectory);
        return SavesDirectory;
    }

    public static string EnsureLogsDirectory()
    {
        Directory.CreateDirectory(LogsDirectory);
        return LogsDirectory;
    }
}
