using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using FractalExplorerWPF.Controls;
using FractalExplorerWPF.Core.NewtonMath;
using FractalExplorerWPF.Core.Rendering;
using FractalExplorerWPF.Infrastructure;
using FractalExplorerWPF.Models;

// No visible windows or screen capture. The snapshot callback supplies synthetic pixels.
internal static class Program
{
    [STAThread]
    private static int Main()
    {
        int result = 0;
        _ = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext());
        Dispatcher.CurrentDispatcher.InvokeAsync(async () =>
        {
            try
            {
                await VerifyManagerAsync();
                await VerifyDeepZoomAsync();
                Console.WriteLine("PASS: preview selection, snapshot persistence, progress, cancellation, stale results, errors, presets and deep zoom.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                result = 1;
            }
            finally { Dispatcher.CurrentDispatcher.BeginInvokeShutdown(DispatcherPriority.Background); }
        });
        Dispatcher.Run();
        return result;
    }

    private static async Task VerifyManagerAsync()
    {
        string id = "Verification_" + Guid.NewGuid().ToString("N");
        string directory = Path.Combine(AppPaths.SavesDirectory, "SavePrevData", id);
        var a = new State("A", new DateTime(2026, 1, 2));
        var b = new State("B", new DateTime(2026, 1, 1));
        var preset = new State("Preset", DateTime.MinValue);
        List<State> saved = [a, b];
        List<PendingRender> jobs = [];
        int captures = 0;
        BitmapSource? snapshot = Pixel(17);
        var configuration = new SaveManagerConfiguration<State>
        {
            WindowTitle = "Verification", FractalIdentifier = id,
            LoadStates = () => saved.ToList(), SaveStates = states => saved = states.ToList(),
            CaptureState = name => new State(name, new DateTime(2026, 1, 3)),
            CapturePreview = (_, _) => { captures++; return snapshot; },
            LoadState = _ => { }, GetName = state => state.Name,
            GetTimestamp = state => state.Timestamp, GetDetails = state => state.Name,
            PointsOfInterest = [preset],
            RenderPreviewAsync = (state, _, _, token, progress) =>
            {
                var job = new PendingRender(state, token, progress!);
                jobs.Add(job);
                return job.Completion.Task;
            }
        };
        var view = new SaveManagerControl();
        var window = new Window { Content = view };
        using var controller = new SaveManagerController<State>(window, view, configuration);
        try
        {
            Select(view, "B"); Select(view, "A");
            Check(jobs.Count == 0 && captures == 0, "Selection must neither render nor capture.");
            Check(Image(view) is null, "Missing preview must be empty.");

            view.SaveName = "Snapshot";
            Click(view, "SaveButton");
            Check(captures == 1 && jobs.Count == 0, "Saving must copy the frame without rendering.");
            State captured = saved.Single(state => state.Name == "Snapshot");
            string snapshotPath = PreviewPath(directory, captured);
            byte[] originalPng = File.ReadAllBytes(snapshotPath);
            Select(view, "B");
            Check(Image(view) is null, "Switching to an uncached entry must clear the old image.");
            Select(view, "Snapshot");
            Check(ReadPixel(Image(view)!) == 17 && jobs.Count == 0, "Cached snapshot must survive selection.");

            Click(view, "RenderPreviewButton");
            PendingRender oldJob = jobs[^1];
            oldJob.Progress.Report(60); await DrainAsync();
            var progressBar = (ProgressBar)view.FindName("PreviewProgress");
            Check(!progressBar.IsIndeterminate && progressBar.Value == 60, "Renderer progress must reach the UI.");
            oldJob.Progress.Report(30); await DrainAsync();
            Check(progressBar.Value == 60, "Out-of-order progress must not go backwards.");
            Select(view, "B");
            Check(oldJob.Token.IsCancellationRequested && jobs.Count == 1, "Switching cancels without starting another render.");
            Click(view, "RenderPreviewButton");
            PendingRender newJob = jobs[^1];
            oldJob.Progress.Report(95);
            oldJob.Completion.SetResult(Pixel(99));
            await DrainAsync();
            Check(Image(view) is null && progressBar.IsIndeterminate, "Stale image and progress must be discarded.");
            Check(File.ReadAllBytes(snapshotPath).SequenceEqual(originalPng), "Stale render must not rewrite the original PNG.");
            newJob.Completion.SetResult(Pixel(42)); await DrainAsync();
            Check(ReadPixel(Image(view)!) == 42 && File.Exists(PreviewPath(directory, b)), "Manual render must update its own entry.");

            byte[] beforeCancel = File.ReadAllBytes(PreviewPath(directory, b));
            Click(view, "RenderPreviewButton");
            PendingRender cancelled = jobs[^1];
            Click(view, "CancelPreviewButton");
            Check(cancelled.Token.IsCancellationRequested, "Cancel button must signal cancellation.");
            cancelled.Completion.SetResult(Pixel(70)); await DrainAsync();
            Check(ReadPixel(Image(view)!) == 42, "Cancelled render must preserve the image.");
            Check(File.ReadAllBytes(PreviewPath(directory, b)).SequenceEqual(beforeCancel), "Cancelled render must preserve the PNG.");
            Click(view, "RenderPreviewButton");
            jobs[^1].Completion.SetException(new InvalidOperationException("test render failure"));
            await DrainAsync();
            Check(((TextBlock)view.FindName("StatusText")).Text.Contains("test render failure"), "Render error must be visible.");
            Check(File.ReadAllBytes(PreviewPath(directory, b)).SequenceEqual(beforeCancel), "Failed render must preserve the PNG.");

            File.WriteAllText(PreviewPath(directory, a), "invalid png");
            int beforeSelection = jobs.Count;
            Select(view, "A");
            Check(Image(view) is null && jobs.Count == beforeSelection, "Corrupt PNG must not start a render.");
            snapshot = null;
            view.SaveName = "NoFrame"; Click(view, "SaveButton");
            Check(saved.Any(state => state.Name == "NoFrame") && Image(view) is null,
                "A missing frame must not prevent saving the state.");
            Check(jobs.Count == beforeSelection, "Missing frame must not trigger rendering.");

            var points = (CheckBox)view.FindName("PointsOfInterestCheckBox");
            points.IsChecked = true;
            Check(jobs.Count == beforeSelection, "Selecting a preset must not render.");
            Check(((Button)view.FindName("RenderPreviewButton")).IsEnabled, "Presets must support manual rendering.");
            Click(view, "RenderPreviewButton"); jobs[^1].Completion.SetResult(Pixel(55)); await DrainAsync();
            points.IsChecked = false; points.IsChecked = true;
            Check(ReadPixel(Image(view)!) == 55 && jobs.Count == beforeSelection + 1, "Preset preview must be cached.");

            var reopenedView = new SaveManagerControl();
            var reopenedWindow = new Window { Content = reopenedView };
            using (var reopened = new SaveManagerController<State>(reopenedWindow, reopenedView, configuration))
            {
                Select(reopenedView, "Snapshot");
                Check(ReadPixel(Image(reopenedView)!) == 17, "Reopening must load the saved PNG.");
            }
            Click(view, "RenderPreviewButton");
            PendingRender closing = jobs[^1];
            controller.Dispose();
            Check(closing.Token.IsCancellationRequested, "Closing must cancel the render.");
            closing.Progress.Report(80); closing.Completion.SetResult(Pixel(88)); await DrainAsync();
            Check(ReadPixel(Image(view)!) == 55, "Closed manager must ignore pending results.");
        }
        finally
        {
            // Only delete this run's generated fixtures, never the application's saves.
            string fullPath = Path.GetFullPath(directory);
            string expectedRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "Saves", "SavePrevData")) + Path.DirectorySeparatorChar;
            if (!fullPath.StartsWith(expectedRoot, StringComparison.OrdinalIgnoreCase) || Path.GetFileName(fullPath) != id)
                throw new InvalidOperationException("Unexpected verification directory.");
            if (Directory.Exists(fullPath)) Directory.Delete(fullPath, true);
        }
    }

    private static async Task VerifyDeepZoomAsync()
    {
        var state = new MandelbrotState
        {
            CenterX = -1.2628848671045503000020782246m,
            CenterY = 0.0409687601493310685285376264m,
            Zoom = 5.7607143988620999e25,
            Iterations = 4500, Threads = 2,
            Palette = new MandelbrotPalette { Colors = [Colors.White, Colors.White], InteriorColor = Colors.Black }
        };
        const int width = 12, height = 8;
        byte[] pixels = new byte[width * height * 4];
        int progress = 0;
        await Task.Run(() => MandelbrotFamilyRenderer.Render(state, pixels, width, height, width * 4,
            CancellationToken.None, value => Interlocked.Exchange(ref progress, value)));
        Check(pixels.Where((_, index) => index % 4 != 3).Any(value => value != 0), "4500 iterations must resolve deep-zoom detail.");
        Check(progress == 100, "Deep-zoom render must complete with 100% progress.");
        state.Iterations = 600;
        await Task.Run(() => MandelbrotFamilyRenderer.Render(state, pixels, width, height, width * 4, CancellationToken.None));
        Check(pixels.Where((_, index) => index % 4 != 3).All(value => value == 0), "Regression fixture must reproduce the old black preview at 600 iterations.");

        await VerifyUnifiedDeepZoomEngineAsync();

        using var cancellation = new CancellationTokenSource();
        state.Iterations = 100_000;
        byte[] cancelledPixels = new byte[480 * 320 * 4];
        Task render = Task.Run(() => MandelbrotFamilyRenderer.Render(state, cancelledPixels, 480, 320, 480 * 4, cancellation.Token));
        cancellation.CancelAfter(30);
        await render.WaitAsync(TimeSpan.FromSeconds(10));
        Check(cancelledPixels.Where((_, index) => index % 4 == 3).Any(value => value == 0), "Cancellation must stop unfinished work.");
    }

    // Phase 1 of the unified deep-zoom engine: adaptive reference-orbit precision plus a
    // FloatExp representation of the per-pixel δ, both behind the existing 1e25 gate.
    // The byte-identical band (zoom <= 1e50) is proven by an external git-stash A/B hash
    // run; here we check the two new mechanisms in isolation.
    private static async Task VerifyUnifiedDeepZoomEngineAsync()
    {
        MandelbrotPalette Palette() =>
            new() { Colors = [Colors.White, Colors.Black], InteriorColor = Colors.Black };

        // 1. The FloatExp-δ kernel must track the trusted double-δ kernel where both are
        //    valid. A moderately deep view with a bounded iteration budget keeps most
        //    pixels off the chaotically sensitive boundary (1 ULP can flip one there).
        var overlap = new MandelbrotState
        {
            CenterX = -1.2628848671045503000020782246m,
            CenterY = 0.0409687601493310685285376264m,
            Zoom = 5.7607143988620999e25,
            Iterations = 2200,
            Threads = 2,
            Palette = Palette()
        };
        const int ow = 110, oh = 72;
        byte[] doubleDelta = new byte[ow * oh * 4];
        byte[] floatExpDelta = new byte[ow * oh * 4];

        MandelbrotFamilyRenderer.ForceFloatExpDeltaForTests = false;
        await Task.Run(() => MandelbrotFamilyRenderer.Render(overlap, doubleDelta, ow, oh, ow * 4, CancellationToken.None));
        MandelbrotFamilyRenderer.ForceFloatExpDeltaForTests = true;
        await Task.Run(() => MandelbrotFamilyRenderer.Render(overlap, floatExpDelta, ow, oh, ow * 4, CancellationToken.None));
        MandelbrotFamilyRenderer.ForceFloatExpDeltaForTests = null;

        Check(floatExpDelta.Where((_, index) => index % 4 != 3).Any(value => value != 0),
            "FloatExp δ kernel must produce an image.");
        Check(doubleDelta.Where((_, index) => index % 4 != 3).Any(value => value != 0),
            "Overlap fixture must have visible structure for the kernel comparison.");
        int differing = 0;
        for (int pixel = 0; pixel < ow * oh; pixel++)
        {
            int b = pixel * 4;
            if (doubleDelta[b] != floatExpDelta[b] ||
                doubleDelta[b + 1] != floatExpDelta[b + 1] ||
                doubleDelta[b + 2] != floatExpDelta[b + 2])
                differing++;
        }
        // At this depth δ stays inside normal double range, so the FloatExp recurrence
        // rounds bit-for-bit like the double one on this fixed fixture. A drift here is a
        // real kernel regression, not boundary chaos (both kernels share the exact same
        // double reference orbit and δc).
        Check(differing == 0,
            $"FloatExp δ kernel diverges from the double δ kernel on {differing}/{ow * oh} pixels.");

        // 2. A view deep enough to switch δ to FloatExp automatically and to lift the
        //    reference precision above the 384-bit floor. No pre-change baseline exists
        //    (old MaxZoom was 1e50); the checks are that the new paths run, complete and
        //    leave the calling thread's working precision restored. Rendered synchronously
        //    so the PrecisionScope opens and closes on *this* thread.
        Check(BigFloat.WorkingPrecisionBits == BigFloat.MinimumPrecisionBits,
            "Working precision must start at the minimum.");
        var deep = new MandelbrotState
        {
            CenterX = -1.2628848671045503000020782246m,
            CenterY = 0.0409687601493310685285376264m,
            Zoom = 1.0e120,
            Iterations = 2600,
            Threads = 2,
            Palette = Palette()
        };
        int deepProgress = 0;
        byte[] deepPixels = new byte[80 * 56 * 4];
        MandelbrotFamilyRenderer.Render(deep, deepPixels, 80, 56, 80 * 4,
            CancellationToken.None, value => deepProgress = value);
        Check(deepProgress == 100, "Deep FloatExp render must complete with 100% progress.");
        Check(deepPixels.Where((_, index) => index % 4 == 3).All(value => value == 255),
            "Deep FloatExp render must fill every pixel.");
        Check(BigFloat.WorkingPrecisionBits == BigFloat.MinimumPrecisionBits,
            "Deep-zoom render must restore the calling thread's working precision.");
        await Task.CompletedTask;
    }

    private static async Task DrainAsync()
    {
        for (int i = 0; i < 3; i++) await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
    }
    private static void Select(SaveManagerControl view, string name) => view.SelectedItem =
        ((ListBox)view.FindName("SavesList")).Items.Cast<SaveManagerEntry<State>>().Single(entry => entry.State.Name == name);
    private static void Click(SaveManagerControl view, string name) =>
        ((Button)view.FindName(name)).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    private static BitmapSource? Image(SaveManagerControl view) =>
        ((Image)view.FindName("PreviewImage")).Source as BitmapSource;
    private static BitmapSource Pixel(byte value)
    {
        BitmapSource bitmap = BitmapSource.Create(1, 1, 96, 96, PixelFormats.Bgra32, null, new byte[] { value, value, value, 255 }, 4);
        bitmap.Freeze(); return bitmap;
    }
    private static byte ReadPixel(BitmapSource bitmap)
    {
        byte[] pixels = new byte[4]; bitmap.CopyPixels(pixels, 4, 0); return pixels[0];
    }
    private static string PreviewPath(string directory, State state) =>
        Path.Combine(directory, $"{state.Name}_{state.Timestamp:yyyyMMdd_HHmmss_fffffff}.png");
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
    private sealed record State(string Name, DateTime Timestamp);
    private sealed record PendingRender(State State, CancellationToken Token, IProgress<int> Progress)
    {
        public TaskCompletionSource<BitmapSource> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
