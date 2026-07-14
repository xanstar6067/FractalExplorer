using System.Runtime;
using System.Runtime.InteropServices;

namespace FractalExplorerWPF.Infrastructure;

internal static class MemoryPressureRelief
{
    private static readonly SemaphoreSlim Gate = new(1, 1);

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
