using System.Runtime;
using System.Runtime.InteropServices;

namespace FractalExplorerWPF.Infrastructure;

internal static class MemoryPressureRelief
{
    private static readonly SemaphoreSlim Gate = new(1, 1);

    // Полное освобождение: двойная блокирующая уплотняющая сборка (вторая — Aggressive)
    // и принудительное обрезание рабочего набора процесса. Тяжёлая операция, оправдана
    // только после разовых крупных задач (экспорт больших изображений).
    public static async Task ReleaseAsync()
    {
        await Gate.WaitAsync().ConfigureAwait(false);
        try
        {
            await Task.Run(ReleaseCore).ConfigureAwait(false);
        }
        finally
        {
            Gate.Release();
        }
    }

    // Лёгкое освобождение для пауз в интерактивной работе: один блокирующий уплотняющий
    // сбор gen2 + компакция LOH. Без Aggressive и без SetProcessWorkingSetSize, поэтому
    // не провоцирует жёсткие page fault'ы при следующем взаимодействии.
    public static async Task CompactAsync()
    {
        await Gate.WaitAsync().ConfigureAwait(false);
        try
        {
            await Task.Run(CompactCore).ConfigureAwait(false);
        }
        finally
        {
            Gate.Release();
        }
    }

    private static void CompactCore()
    {
        GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
    }

    private static void ReleaseCore()
    {
        GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, blocking: true, compacting: true);

        if (OperatingSystem.IsWindows())
        {
            _ = SetProcessWorkingSetSize(GetCurrentProcess(), new IntPtr(-1), new IntPtr(-1));
        }
    }

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetCurrentProcess();

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetProcessWorkingSetSize(IntPtr process, IntPtr minimumWorkingSetSize,
        IntPtr maximumWorkingSetSize);
}
