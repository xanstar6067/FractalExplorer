using System.Diagnostics;
using System.Globalization;
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

        VerifyBigFloatSqrt();
        VerifyBigFloatTranscendental();
        await VerifyCollatzDeepZoomAsync();
        await VerifyDecimalStageRemovedAsync(Palette);
        await VerifyBlaAccelerationAsync(Palette);
        await VerifyRealBlaAccelerationAsync(Palette);
        await VerifyReflectedVariantsAsync(Palette);
        await VerifyMultibrotDeepZoomAsync(Palette);
        await VerifySimonobrotDeepZoomAsync(Palette);
        await VerifyHistogramDeepZoomAsync(Palette);
        await VerifyDistanceEstimationDeepZoomAsync(Palette);
        await VerifyEngineAccuracyAsync(Palette);
    }

    // Phase 7: Histogram coloring moved onto the deep engine (RenderDeepZoomHistogram) — the
    // decimal stage it used to require unconditionally is now only a degenerate-orbit
    // fallback. Same two-pass CDF pipeline as the decimal version, fed by the already-proven
    // deep kernels (Iterations/Smooth are unconditional in every kernel, so BLA/FloatExp/
    // reflection/power formulas all carry through unchanged).
    private static async Task VerifyHistogramDeepZoomAsync(Func<MandelbrotPalette> palette)
    {
        static int RgbDelta(byte[] a, byte[] b, int pixel)
        {
            int o = pixel * 4;
            return Math.Max(Math.Abs(a[o] - b[o]),
                   Math.Max(Math.Abs(a[o + 1] - b[o + 1]), Math.Abs(a[o + 2] - b[o + 2])));
        }

        static int CountDiffering(byte[] a, byte[] b)
        {
            int n = 0;
            for (int pixel = 0; pixel * 4 < a.Length; pixel++)
                if (RgbDelta(a, b, pixel) != 0) n++;
            return n;
        }

        async Task<byte[]> RenderAsync(MandelbrotState state, bool? forceDeep, int w, int h)
        {
            byte[] pixels = new byte[w * h * 4];
            MandelbrotFamilyRenderer.ForceDeepZoomForTests = forceDeep;
            try
            {
                await Task.Run(() => MandelbrotFamilyRenderer.Render(state, pixels, w, h, w * 4, CancellationToken.None));
            }
            finally { MandelbrotFamilyRenderer.ForceDeepZoomForTests = null; }
            return pixels;
        }

        const int w = 112, h = 74, total = w * h;
        MandelbrotState HistogramState(double zoom, int iterations, bool equalize, bool useSmooth) => new()
        {
            ColoringMode = MandelbrotColoringMode.Histogram,
            CenterX = -1.2628848671045503000020782246m,
            CenterY = 0.0409687601493310685285376264m,
            Zoom = zoom,
            Iterations = iterations,
            HistogramEnabledEqualization = equalize,
            HistogramInputUseSmooth = useSmooth,
            Threads = 2,
            Palette = palette()
        };

        // (a) Where decimal was still trustworthy, the deep two-pass pipeline (binning + CDF
        //     + colouring) must reproduce it up to boundary chaos — across every combination
        //     of equalization and smooth/iteration binning.
        foreach ((bool equalize, bool useSmooth) in new[] { (true, true), (true, false), (false, true), (false, false) })
        {
            MandelbrotState state = HistogramState(1.0e12, 6000, equalize, useSmooth);
            byte[] decimalPixels = await RenderAsync(state, forceDeep: false, w, h);
            byte[] deepPixels = await RenderAsync(state, forceDeep: true, w, h);
            int differing = CountDiffering(decimalPixels, deepPixels);
            Console.WriteLine($"[diag] Histogram equalize={equalize} smooth={useSmooth}: decimal vs deep {differing}/{total} px differ");
            Check(deepPixels.Where((_, index) => index % 4 != 3).Any(value => value != 0),
                $"Histogram (equalize={equalize}, smooth={useSmooth}) must resolve structure.");
            Check(differing * 100 <= total * 5,
                $"Histogram (equalize={equalize}, smooth={useSmooth}): decimal vs deep diverges on {differing}/{total} px (>5%).");
        }

        // (b) Determinism: the two-pass parallel binning must not depend on thread scheduling.
        {
            MandelbrotState state = HistogramState(1.0e18, 4000, equalize: true, useSmooth: true);
            byte[] a = await RenderAsync(state, forceDeep: true, w, h);
            byte[] b = await RenderAsync(state, forceDeep: true, w, h);
            Check(CountDiffering(a, b) == 0, "Deep Histogram must be deterministic across runs.");
        }

        // (c) Degenerate reference orbit + Histogram must still fall back cleanly (decimal
        //     two-pass render, no crash) instead of silently mis-colouring with normalized=0.
        {
            var degenerate = new MandelbrotState
            {
                ColoringMode = MandelbrotColoringMode.Histogram,
                CenterX = 1000m,
                CenterY = 1000m,
                Zoom = 1.0e30,
                Iterations = 500,
                Threads = 2,
                Palette = palette()
            };
            int progress = 0;
            byte[] pixels = new byte[w * h * 4];
            MandelbrotFamilyRenderer.Render(degenerate, pixels, w, h, w * 4,
                CancellationToken.None, value => progress = value);
            Check(progress == 100, "Degenerate-orbit Histogram fallback must complete with 100% progress.");
            Check(pixels.Where((_, index) => index % 4 == 3).All(value => value == 255),
                "Degenerate-orbit Histogram fallback must fill every pixel.");
        }

        // (d) Tile-mode preview path (local normalization, not the full-frame CDF) must not
        //     crash and must resolve structure for a deep-eligible state.
        {
            MandelbrotState state = HistogramState(1.0e12, 3000, equalize: true, useSmooth: true);
            var tile = new MandelbrotRenderTile(0, 0, w, h, 0, 0);
            byte[]? tilePixels = MandelbrotFamilyRenderer.RenderTile(state, w, h, tile, CancellationToken.None);
            Check(tilePixels is not null, "Histogram tile render must not be cancelled.");
            Check(tilePixels!.Where((_, index) => index % 4 != 3).Any(value => value != 0),
                "Histogram tile render must resolve structure.");
        }
    }

    // Phase 8: Distance Estimation moved onto the deep engine
    // (RenderDeepZoomDistanceEstimation). No new per-formula perturbation math was needed:
    // the derivative recurrence D ← J(z)·D + ∂f/∂c depends on z alone, and every kernel
    // already assembles z = Z + δ in double for orbit-trap/stripe — so DE arrives for all
    // supported variants at once, reusing GetIterationJacobian from the flat stage verbatim.
    // BLA is disabled in this mode (skipping iterations would skip derivative steps).
    // Distances are stored normalized to the pixel size; float would flush the absolute
    // deep-zoom values (~1e-43 already at zoom 1e40) straight to zero and kill the relief.
    private static async Task VerifyDistanceEstimationDeepZoomAsync(Func<MandelbrotPalette> palette)
    {
        static (int Differing, int MaxDelta) Compare(byte[] a, byte[] b)
        {
            int differing = 0, maxDelta = 0;
            for (int pixel = 0; pixel * 4 < a.Length; pixel++)
            {
                int o = pixel * 4;
                int d = Math.Max(Math.Abs(a[o] - b[o]),
                    Math.Max(Math.Abs(a[o + 1] - b[o + 1]), Math.Abs(a[o + 2] - b[o + 2])));
                if (d != 0) differing++;
                maxDelta = Math.Max(maxDelta, d);
            }
            return (differing, maxDelta);
        }

        async Task<byte[]> RenderAsync(MandelbrotState state, bool? forceDeep, bool? forceBla, int w, int h)
        {
            byte[] pixels = new byte[w * h * 4];
            MandelbrotFamilyRenderer.ForceDeepZoomForTests = forceDeep;
            MandelbrotFamilyRenderer.ForceBlaForTests = forceBla;
            try
            {
                await Task.Run(() => MandelbrotFamilyRenderer.Render(state, pixels, w, h, w * 4, CancellationToken.None));
            }
            finally
            {
                MandelbrotFamilyRenderer.ForceDeepZoomForTests = null;
                MandelbrotFamilyRenderer.ForceBlaForTests = null;
            }
            return pixels;
        }

        static MandelbrotState De(
            MandelbrotVariant variant, decimal cx, decimal cy, double zoom, int iterations,
            MandelbrotPalette pal, decimal power = 2m, decimal jr = 0m, decimal ji = 0m,
            bool inversion = false, double relief = 1.35,
            string? exactX = null, string? exactY = null) => new()
        {
            ColoringMode = MandelbrotColoringMode.DistanceEstimation,
            Variant = variant,
            CenterX = cx,
            CenterY = cy,
            CenterXExact = exactX,
            CenterYExact = exactY,
            Power = power,
            JuliaCReal = jr,
            JuliaCImaginary = ji,
            UseInversion = inversion,
            Zoom = zoom,
            Iterations = iterations,
            DistanceReliefStrength = relief,
            Threads = 2,
            Palette = pal
        };

        var mandelCentre = (X: -1.2628848671045503000020782246m, Y: 0.0409687601493310685285376264m);

        // (a) Where the flat double stage is still trustworthy, the perturbation kernels must
        //     reproduce its relief. Both stages run the identical Jacobian/EstimateDistance
        //     code; the only difference is where z comes from.
        {
            const int w = 96, h = 64, total = w * h;
            foreach (double zoom in new[] { 1.0e6, 1.0e8 })
            {
                MandelbrotState state = De(MandelbrotVariant.Mandelbrot,
                    mandelCentre.X, mandelCentre.Y, zoom, 3000, palette());
                byte[] flat = await RenderAsync(state, forceDeep: false, forceBla: null, w, h);
                byte[] deep = await RenderAsync(state, forceDeep: true, forceBla: null, w, h);
                (int differing, int maxDelta) = Compare(flat, deep);
                Console.WriteLine($"[diag] DE zoom {zoom:E0}: flat vs deep {differing}/{total} px differ (maxΔ {maxDelta})");
                Check(deep.Where((_, index) => index % 4 != 3).Any(value => value != 0),
                    $"Deep DE must resolve structure at zoom {zoom:E0}.");
                Check(differing * 100 <= total * 8,
                    $"Deep DE diverges from the flat stage on {differing}/{total} px at zoom {zoom:E0} (>8%).");
            }
        }

        // (b) Accuracy against the near-exact BigFloat reference, which drives the very same
        //     derivative recurrence from a directly iterated arbitrary-precision orbit. Small
        //     images: the reference samples (w+2)x(h+2) pixels and is slow.
        //
        //     The headline assertion is the DE *excess*: the same view is also rendered in
        //     Smooth mode (no derivative at all) and compared to the reference, so we can tell
        //     the error Distance Estimation adds from the error the underlying orbit already
        //     carries. On a chaotic view the second number is large for reasons that predate
        //     this phase, and only the excess is meaningful.
        {
            const int w = 40, h = 28, total = w * h;

            // -2 - 10^-offset: an exactly-representable centre just outside the antenna tip,
            // at any depth. The whole frame then sits in the smooth escaping region, where
            // the orbit is not chaotic and an exact comparison is actually meaningful.
            static string OutsideTip(int offsetDigits) => "-2." + new string('0', offsetDigits - 1) + "1";

            (string Label, bool OrbitChaotic, MandelbrotState State)[] views =
            {
                ("Mandelbrot 1e30",         false, De(MandelbrotVariant.Mandelbrot, mandelCentre.X, mandelCentre.Y, 1.0e30, 4000, palette())),
                ("Mandelbrot outside-tip 1e50",  false, De(MandelbrotVariant.Mandelbrot, -2m, 0m, 1.0e50, 800, palette(), exactX: OutsideTip(46), exactY: "0")),
                ("Mandelbrot outside-tip 1e120", false, De(MandelbrotVariant.Mandelbrot, -2m, 0m, 1.0e120, 800, palette(), exactX: OutsideTip(118), exactY: "0")),
                // Unit-circle Julia (c = 0): centre 1 is exact at any depth and the dynamics
                // z <- z^2 are perfectly smooth, so this isolates deep-zoom numerics from
                // boundary chaos. Also the only fixture here that starts the derivative at I.
                ("Julia c=0 circle 1e50",   false, De(MandelbrotVariant.Julia, 1m, 0m, 1.0e50, 600, palette())),
                // The antenna tip itself: half the frame (c > -2) is the chaotic region of the
                // real quadratic map, so the orbit alone diverges on ~50% of pixels. Kept
                // deliberately - it is the case where only the DE excess can be asserted.
                ("Mandelbrot tip 1e50",     true,  De(MandelbrotVariant.Mandelbrot, -2m, 0m, 1.0e50, 800, palette())),
                ("BurningShip 1e30",        false, De(MandelbrotVariant.BurningShip, -1.62m, 0m, 1.0e30, 3000, palette())),
                ("Tricorn 1e10",            false, De(MandelbrotVariant.Tricorn, -1.62m, 0m, 1.0e10, 2000, palette())),
                ("Buffalo 1e10",            false, De(MandelbrotVariant.Buffalo, -1.62m, 0m, 1.0e10, 2000, palette())),
                ("Celtic 1e10",             false, De(MandelbrotVariant.Celtic, -1.62m, 0m, 1.0e10, 2000, palette())),
                ("JuliaBurningShip 1e10",   false, De(MandelbrotVariant.JuliaBurningShip, 0.5m, -0.3m, 1.0e10, 2000, palette(), jr: -1.5m)),
                ("Multibrot p=3",           false, De(MandelbrotVariant.Generalized, -0.295455m, 0.977273m, 300.0, 2000, palette(), power: 3m)),
                ("Multibrot p=8",           false, De(MandelbrotVariant.Generalized, 0.66m, 0m, 300.0, 2000, palette(), power: 8m)),
                ("Simonobrot p=2",          false, De(MandelbrotVariant.Simonobrot, -0.03m, 0.84m, 300.0, 2000, palette(), power: 2m)),
                ("Simonobrot p=6 inv",      false, De(MandelbrotVariant.Simonobrot, -0.90m, 0.18m, 300.0, 2000, palette(), power: 6m, inversion: true)),
            };

            foreach ((string label, bool chaotic, MandelbrotState state) in views)
            {
                byte[] engine = await RenderAsync(state, forceDeep: true, forceBla: null, w, h);
                byte[] exact = await Task.Run(() =>
                    MandelbrotFamilyRenderer.RenderExactReferenceForTests(state, w, h, CancellationToken.None));
                (int differing, int maxDelta) = Compare(engine, exact);
                int nonBlack = 0;
                for (int i = 0; i < total; i++)
                    if (engine[i * 4] != 0 || engine[i * 4 + 1] != 0 || engine[i * 4 + 2] != 0) nonBlack++;

                // Same view without any derivative: how much of the difference is the orbit's?
                state.ColoringMode = MandelbrotColoringMode.Smooth;
                byte[] smoothEngine = await RenderAsync(state, forceDeep: true, forceBla: null, w, h);
                byte[] smoothExact = await Task.Run(() =>
                    MandelbrotFamilyRenderer.RenderExactReferenceForTests(state, w, h, CancellationToken.None));
                state.ColoringMode = MandelbrotColoringMode.DistanceEstimation;
                int orbitOnly = Compare(smoothEngine, smoothExact).Differing;
                int excess = Math.Abs(differing - orbitOnly);

                Console.WriteLine($"[diag] DE accuracy {label}: {differing}/{total} px differ " +
                                  $"({100.0 * differing / total:F2}%), maxD {maxDelta}, nonblack {nonBlack}, " +
                                  $"orbit-only {orbitOnly}, DE excess {excess}");
                Check(nonBlack > total / 10, $"DE view {label} must carry structure.");
                Check(excess * 100 <= total * 3,
                    $"DE {label}: the derivative adds {excess}/{total} px of error over the orbit itself (>3%).");
                if (!chaotic)
                    Check(differing * 100 <= total * 8,
                        $"DE {label}: deep engine diverges from the exact reference on {differing}/{total} px (>8%).");
            }
        }
        // (c) The relief must survive the depth. If the normalized distance field had
        //     underflowed to zero, ApplyDistanceLighting would early-return the unshaded base
        //     colour for every pixel and the relief strength would stop mattering — so a
        //     relief-on vs relief-off render being identical is exactly the failure mode.
        foreach ((decimal cx, decimal cy, double zoom, int iterations, string label) in new[]
        {
            (mandelCentre.X, mandelCentre.Y, 1.0e30, 4000, "centre 1e30"),
            (-2m, 0m, 1.0e50, 800, "tip 1e50"),
            (-2m, 0m, 1.0e120, 800, "tip 1e120"),
        })
        {
            const int w = 64, h = 44, total = w * h;
            MandelbrotState lit = De(MandelbrotVariant.Mandelbrot, cx, cy, zoom, iterations, palette());
            MandelbrotState flatLit = De(MandelbrotVariant.Mandelbrot, cx, cy, zoom, iterations, palette(), relief: 0);
            byte[] withRelief = await RenderAsync(lit, forceDeep: true, forceBla: null, w, h);
            byte[] withoutRelief = await RenderAsync(flatLit, forceDeep: true, forceBla: null, w, h);
            (int differing, int maxDelta) = Compare(withRelief, withoutRelief);
            Console.WriteLine($"[diag] DE relief alive, {label}: {differing}/{total} px react to relief (maxΔ {maxDelta})");
            Check(differing * 4 > total,
                $"DE distance field collapsed at {label}: only {differing}/{total} px react to relief.");
        }

        // (d) BLA must be inert in this mode — the pyramid skips iterations, which would skip
        //     derivative steps. Forcing it on and off must give bit-identical output.
        {
            const int w = 64, h = 44;
            MandelbrotState state = De(MandelbrotVariant.Mandelbrot, mandelCentre.X, mandelCentre.Y, 1.0e30, 3500, palette());
            byte[] blaOn = await RenderAsync(state, forceDeep: true, forceBla: true, w, h);
            byte[] blaOff = await RenderAsync(state, forceDeep: true, forceBla: false, w, h);
            Check(Compare(blaOn, blaOff).Differing == 0,
                "BLA must be disabled for Distance Estimation (output changed with BLA forced on).");
        }

        // (e) Degenerate reference orbit + DE must fall back to the decimal two-pass render.
        {
            const int w = 64, h = 44;
            MandelbrotState degenerate = De(MandelbrotVariant.Mandelbrot, 1000m, 1000m, 1.0e30, 500, palette());
            int progress = 0;
            byte[] pixels = new byte[w * h * 4];
            MandelbrotFamilyRenderer.Render(degenerate, pixels, w, h, w * 4,
                CancellationToken.None, value => progress = value);
            Check(progress == 100, "Degenerate-orbit DE fallback must complete with 100% progress.");
            Check(pixels.Where((_, index) => index % 4 == 3).All(value => value == 255),
                "Degenerate-orbit DE fallback must fill every pixel.");
        }

        // (f) Tile path (used by the preview scheduler) must resolve the same relief.
        {
            const int w = 64, h = 44;
            MandelbrotState state = De(MandelbrotVariant.Mandelbrot, mandelCentre.X, mandelCentre.Y, 1.0e30, 3000, palette());
            MandelbrotFamilyRenderer.ForceDeepZoomForTests = true;
            byte[]? tilePixels;
            byte[] fullPixels = new byte[w * h * 4];
            try
            {
                var tile = new MandelbrotRenderTile(0, 0, w, h, 0, 0);
                tilePixels = MandelbrotFamilyRenderer.RenderTile(state, w, h, tile, CancellationToken.None);
                MandelbrotFamilyRenderer.Render(state, fullPixels, w, h, w * 4, CancellationToken.None);
            }
            finally { MandelbrotFamilyRenderer.ForceDeepZoomForTests = null; }
            Check(tilePixels is not null, "Deep DE tile render must not be cancelled.");
            Check(tilePixels!.Where((_, index) => index % 4 != 3).Any(value => value != 0),
                "Deep DE tile render must resolve structure.");
            // A whole-canvas tile samples exactly the same grid as the full-frame pass.
            Check(Compare(tilePixels!, fullPixels).Differing == 0,
                "Deep DE tile render must match the full-frame render on a full-canvas tile.");
        }
    }

    // Phase 5: Generalized/Multibrot of integer power p on the deep engine — exact binomial
    // perturbation (Z+δ)ᵖ−Zᵖ, BLA with a p-dependent table. Verified against the exact
    // BigFloat reference (repeated-multiplication zᵖ, no perturbation).
    private static async Task VerifyMultibrotDeepZoomAsync(Func<MandelbrotPalette> palette)
    {
        static int CountRgbDiffering(byte[] a, byte[] b)
        {
            int n = 0;
            for (int pixel = 0; pixel * 4 < a.Length; pixel++)
            {
                int o = pixel * 4;
                if (a[o] != b[o] || a[o + 1] != b[o + 1] || a[o + 2] != b[o + 2]) n++;
            }
            return n;
        }

        async Task<byte[]> RenderAsync(MandelbrotState state, bool? forceDeep, bool? forceBla, int w, int h)
        {
            byte[] pixels = new byte[w * h * 4];
            MandelbrotFamilyRenderer.ForceDeepZoomForTests = forceDeep;
            MandelbrotFamilyRenderer.ForceBlaForTests = forceBla;
            try
            {
                await Task.Run(() => MandelbrotFamilyRenderer.Render(state, pixels, w, h, w * 4, CancellationToken.None));
            }
            finally
            {
                MandelbrotFamilyRenderer.ForceDeepZoomForTests = null;
                MandelbrotFamilyRenderer.ForceBlaForTests = null;
            }
            return pixels;
        }

        const int w = 56, h = 40, total = w * h;

        // Structured boundary views per power; the perturbation engine is forced on so the
        // kernel is exercised on real detail and compared to the exact BigFloat reference
        // (repeated-multiplication zᵖ, no perturbation). Small image / modest iterations —
        // the exact BigFloat renderer is slow.
        (int Power, decimal Cx, decimal Cy)[] cases =
        {
            (3, -0.295455m, 0.977273m),
            (5, -0.540000m, 0.600000m),
            (8, 0.660000m, 0.000000m),
            (12, 0.750000m, 0.000000m),
        };

        foreach ((int power, decimal cx, decimal cy) in cases)
        {
            var state = new MandelbrotState
            {
                Variant = MandelbrotVariant.Generalized,
                Power = power,
                CenterX = cx,
                CenterY = cy,
                Zoom = 300.0,
                Iterations = 2000,
                Threads = 2,
                Palette = palette()
            };
            byte[] perturbation = await RenderAsync(state, forceDeep: true, forceBla: null, w, h);
            byte[] exact = await Task.Run(() =>
                MandelbrotFamilyRenderer.RenderExactReferenceForTests(state, w, h, CancellationToken.None));
            byte[] blaOff = await RenderAsync(state, forceDeep: true, forceBla: false, w, h);

            int vsExact = CountRgbDiffering(perturbation, exact);
            int vsBlaOff = CountRgbDiffering(perturbation, blaOff);
            int maxD = 0, nonBlack = 0;
            for (int i = 0; i < total; i++)
            {
                int o = i * 4;
                maxD = Math.Max(maxD, Math.Max(Math.Abs(perturbation[o] - exact[o]),
                    Math.Max(Math.Abs(perturbation[o + 1] - exact[o + 1]), Math.Abs(perturbation[o + 2] - exact[o + 2]))));
                if (perturbation[o] != 0 || perturbation[o + 1] != 0 || perturbation[o + 2] != 0) nonBlack++;
            }
            Console.WriteLine($"[diag] Multibrot p={power}: vs exact {vsExact}/{total} (maxΔ {maxD}), BLA on/off {vsBlaOff}/{total}, nonblack {nonBlack}");
            Check(nonBlack > total / 10, $"Multibrot p={power} view must carry structure.");
            Check(vsExact * 100 <= total * 3,
                $"Multibrot p={power}: perturbation diverges from exact on {vsExact}/{total} px, maxΔ {maxD} (>3%).");
            Check(vsBlaOff * 100 <= total * 3,
                $"Multibrot p={power}: BLA changes {vsBlaOff}/{total} px vs non-BLA (>3%).");
        }

        // Mandelbrot must be untouched by the Multibrot path.
        var mandel = new MandelbrotState
        {
            CenterX = -1.2628848671045503000020782246m,
            CenterY = 0.0409687601493310685285376264m,
            Zoom = 5.0e25,
            Iterations = 4000,
            Threads = 2,
            Palette = palette()
        };
        Check(CountRgbDiffering(
                await RenderAsync(mandel, forceDeep: true, forceBla: null, w, h),
                await RenderAsync(mandel, forceDeep: true, forceBla: null, w, h)) == 0,
            "Mandelbrot deep render must stay deterministic after Phase 5.");
    }

    // Phase 10: BigFloat.Sqrt — the one operation odd-power Simonobrot needs that the type
    // did not have (|z|ᵖ = M^(p/2) = Mᵠ·√M). Checked three ways: against the published
    // decimal expansion of √2, by round-tripping (√x)² back to x at several magnitudes, and
    // by demanding exactness on perfect squares (where the integer Newton iteration must
    // land on the root itself, not one ULP below it).
    // Phase 11: transcendental functions over BigFloat (π, exp, sin/cos, sh/ch), built for
    // the Collatz deep-zoom stage. Nothing in the Mandelbrot engine calls them, so this is
    // the only place that pins them down. Three independent kinds of oracle:
    //   • published digits — catches a wrong algorithm outright;
    //   • identities (sin²+cos²=1, ch²−sh²=1, doubling formulas) — hold at every precision
    //     and catch guard-bit shortfalls the digit checks would miss at a single precision;
    //   • agreement with the double library on ordinary arguments — catches a wrong branch
    //     in the argument reduction, which the identities alone would not (they survive a
    //     consistent shift of both sin and cos).
    private static void VerifyBigFloatTranscendental()
    {
        // First 100 digits of each constant (truncated, not rounded — the checks compare
        // a prefix of the produced digit string).
        const string PiDigits =
            "3.141592653589793238462643383279502884197169399375105820974944592307816406286208998628034825342117067";
        const string EDigits =
            "2.718281828459045235360287471352662497757247093699959574966967627724076630353547594571382178525166427";
        const string Sin1Digits =
            "0.841470984807896506652502321630298999622563060798371065672751709991910404391239668948639743543052695";
        const string Cos1Digits =
            "0.540302305868139717400936607442976603732310420617922227670097255381100394774471764517951856087183089";
        const string Sinh1Digits =
            "1.175201193643801456882381850595600815155717981334095870229565413013307567304323895607117452089623391";
        const string Cosh1Digits =
            "1.543080634815243778477905620757061682601529112365863704737402214710769063049223698964264726435543035";
        const string SinPiTenthDigits =
            "0.309016994374947424102293417182819058860154589902881431067724311352630231409451224853603602094695568";
        const string CosPiTenthDigits =
            "0.951056516295153572116439333379382143405698634125750222447305644430153170085193501718792810970811381";
        const string ExpMinusFiveDigits =
            "0.006737946999085467096636048423148424248849585027355085430305531572683522515604062281449138844208361";

        static void CheckDigits(string label, BigFloat value, string expected)
        {
            string produced = value.ToInvariantString(expected.Length + 20);
            int common = 0;
            while (common < produced.Length && common < expected.Length && produced[common] == expected[common])
                common++;
            Check(common >= expected.Length,
                $"{label} matches only {common} of {expected.Length} published characters: {produced}");
        }

        // 384 bits ≈ 115 decimal digits, so all 100 published ones must come out right.
        using (new BigFloat.PrecisionScope(BigFloat.MinimumPrecisionBits))
        {
            CheckDigits("π", BigFloatMath.Pi, PiDigits);
            CheckDigits("exp(1)", BigFloatMath.Exp(BigFloat.One), EDigits);
            CheckDigits("exp(-5)", BigFloatMath.Exp(BigFloat.FromInt(-5)), ExpMinusFiveDigits);
            Check(BigFloatMath.Exp(BigFloat.Zero).Equals(BigFloat.One), "exp(0) must be exactly 1.");

            BigFloatMath.SinCos(BigFloat.One, out BigFloat sine, out BigFloat cosine);
            CheckDigits("sin(1)", sine, Sin1Digits);
            CheckDigits("cos(1)", cosine, Cos1Digits);

            BigFloatMath.SinCosPi(BigFloat.One / 10, out BigFloat sinePi, out BigFloat cosinePi);
            CheckDigits("sin(π/10)", sinePi, SinPiTenthDigits);
            CheckDigits("cos(π/10)", cosinePi, CosPiTenthDigits);

            BigFloatMath.SinhCosh(BigFloat.One, out BigFloat hyperbolicSine, out BigFloat hyperbolicCosine);
            CheckDigits("sh(1)", hyperbolicSine, Sinh1Digits);
            CheckDigits("ch(1)", hyperbolicCosine, Cosh1Digits);
        }

        // π at a precision far above and far below the 384-bit default: the Machin series
        // must be recomputed per precision, not reused from a cache keyed by nothing.
        using (new BigFloat.PrecisionScope(1024)) CheckDigits("π at 1024 bits", BigFloatMath.Pi, PiDigits);
        using (new BigFloat.PrecisionScope(128))
        {
            string produced = BigFloatMath.Pi.ToInvariantString(60);
            int common = 0;
            while (common < produced.Length && produced[common] == PiDigits[common]) common++;
            // 128 bits ≈ 38 decimal digits; ask for 34 to stay clear of the rounding digit.
            Check(common >= 36, $"π at 128 bits matches only {common} characters: {produced}");
        }

        // Identities at several precisions, over arguments that exercise every branch of
        // the reduction (both signs, every quadrant, several periods away from zero).
        foreach (int bits in new[] { 128, BigFloat.MinimumPrecisionBits, 512 })
        {
            using var precision = new BigFloat.PrecisionScope(bits);
            BigFloat tolerance = BigFloat.FromDouble(System.Math.ScaleB(1.0, -(bits - 12)));
            for (int index = -260; index <= 260; index += 7)
            {
                BigFloat turns = BigFloat.FromInt(index) / 37;
                BigFloatMath.SinCosPi(turns, out BigFloat sine, out BigFloat cosine);
                BigFloat residual = BigFloat.Abs(sine * sine + cosine * cosine - BigFloat.One);
                Check(residual.CompareTo(tolerance) <= 0,
                    $"sin²+cos² deviates from 1 by {residual.ToInvariantString(20)} at {index}/37 turns, {bits} bits.");

                // sin(2πx) = 2 sin(πx) cos(πx) ties the reduced branches to each other:
                // a quadrant mix-up survives sin²+cos²=1 but not this.
                BigFloatMath.SinCosPi(BigFloat.ScaleByPowerOfTwo(turns, 1), out BigFloat doubleSine, out _);
                BigFloat doublingError = BigFloat.Abs(
                    doubleSine - BigFloat.ScaleByPowerOfTwo(sine * cosine, 1));
                Check(doublingError.CompareTo(tolerance) <= 0,
                    $"sin(2πx) ≠ 2·sin(πx)·cos(πx) by {doublingError.ToInvariantString(20)} at {index}/37 turns.");

                BigFloatMath.SinhCosh(turns, out BigFloat hyperbolicSine, out BigFloat hyperbolicCosine);
                BigFloat hyperbolicResidual = BigFloat.Abs(
                    hyperbolicCosine * hyperbolicCosine - hyperbolicSine * hyperbolicSine - BigFloat.One);
                // ch grows like e^|x|, so the absolute residual is allowed to grow with it.
                BigFloat hyperbolicTolerance = tolerance * hyperbolicCosine * hyperbolicCosine;
                Check(hyperbolicResidual.CompareTo(hyperbolicTolerance) <= 0,
                    $"ch²−sh² deviates from 1 by {hyperbolicResidual.ToInvariantString(20)} at {index}/37, {bits} bits.");
            }
        }

        // Reduction by period is exact because it is done on the argument of sin(πx), not by
        // dividing by an approximate 2π. Far from zero the double library visibly loses this
        // (Math.PI * 12345.75 is already rounded), so the reference here is the exact value:
        // 12345.75 mod 2 = 1.75, hence sin = −√2/2 and cos = +√2/2.
        using (new BigFloat.PrecisionScope(BigFloat.MinimumPrecisionBits))
        {
            BigFloat half = BigFloat.ScaleByPowerOfTwo(BigFloat.Sqrt(BigFloat.FromInt(2)), -1);
            BigFloat tolerance = BigFloat.FromDouble(System.Math.ScaleB(1.0, -360));
            // Every argument here is an exact multiple of 1/4 turn, so |sin| = |cos| = √2/2
            // to the last bit; the signs come from the (small, exactly representable)
            // reduced argument, where the double library is still reliable.
            foreach (double turns in new[] { 12345.75, -87.25, 1e6 + 1.75, 0.75 })
            {
                BigFloatMath.SinCosPi(BigFloat.FromDouble(turns), out BigFloat sine, out BigFloat cosine);
                double reduced = turns - 2 * System.Math.Round(turns / 2);
                BigFloat expectedSine = System.Math.Sin(System.Math.PI * reduced) < 0 ? -half : half;
                BigFloat expectedCosine = System.Math.Cos(System.Math.PI * reduced) < 0 ? -half : half;
                Check(BigFloat.Abs(sine - expectedSine).CompareTo(tolerance) <= 0 &&
                      BigFloat.Abs(cosine - expectedCosine).CompareTo(tolerance) <= 0,
                    $"sin/cos(π·{turns}) lost the exact ±√2/2: {sine.ToInvariantString(25)}, {cosine.ToInvariantString(25)}");
            }
        }

        // Agreement with the double library on ordinary arguments — an oracle that shares
        // no code with BigFloat at all.
        using (new BigFloat.PrecisionScope(256))
        {
            var random = new Random(20260908);
            double worstTrig = 0, worstExp = 0, worstHyperbolic = 0;
            for (int index = 0; index < 3000; index++)
            {
                double turns = (random.NextDouble() - 0.5) * 8;
                BigFloatMath.SinCosPi(BigFloat.FromDouble(turns), out BigFloat sine, out BigFloat cosine);
                worstTrig = System.Math.Max(worstTrig,
                    System.Math.Abs(sine.ToDouble() - System.Math.Sin(System.Math.PI * turns)));
                worstTrig = System.Math.Max(worstTrig,
                    System.Math.Abs(cosine.ToDouble() - System.Math.Cos(System.Math.PI * turns)));

                double argument = (random.NextDouble() - 0.5) * 60;
                double exponential = System.Math.Exp(argument);
                worstExp = System.Math.Max(worstExp,
                    System.Math.Abs(BigFloatMath.Exp(BigFloat.FromDouble(argument)).ToDouble() - exponential) / exponential);

                BigFloatMath.SinhCosh(BigFloat.FromDouble(argument),
                    out BigFloat hyperbolicSine, out BigFloat hyperbolicCosine);
                worstHyperbolic = System.Math.Max(worstHyperbolic,
                    System.Math.Abs(hyperbolicSine.ToDouble() - System.Math.Sinh(argument)) /
                    System.Math.Abs(System.Math.Sinh(argument)));
                worstHyperbolic = System.Math.Max(worstHyperbolic,
                    System.Math.Abs(hyperbolicCosine.ToDouble() - System.Math.Cosh(argument)) /
                    System.Math.Cosh(argument));
            }
            Console.WriteLine($"[diag] BigFloat vs double: trig {worstTrig:E2} abs, exp {worstExp:E2} rel, " +
                              $"hyperbolic {worstHyperbolic:E2} rel");
            Check(worstTrig < 1e-13 && worstExp < 1e-13 && worstHyperbolic < 1e-13,
                "BigFloat transcendentals disagree with the double library beyond double's own rounding.");
        }

        Check(BigFloat.WorkingPrecisionBits == BigFloat.MinimumPrecisionBits,
            "Transcendental checks must leave the working precision restored.");
    }

    // Centres found by descending on edge density (a frame far outside the set comes out
    // uniformly non-black and would pass a "has content" check while showing nothing).
    private static readonly (double Zoom, string CenterX, string CenterY)[] DeepCollatzCentres =
    [
        (1.1e12, "-0.869177864622138448380772548135348733365049368",
                 "0.003351110447198153027105397611632949120554176"),
        (1.1e15, "-0.869177864620624139702320622587697311553355236",
                 "0.003351110446321172760920851431301963941719519"),
        (1.1e18, "-0.869177864620622627578561083124457777168012231",
                 "0.00335111044632155045879382890961167857526658"),
    ];

    // Phase 11: Collatz gained a third precision stage — direct iteration in BigFloat.
    // Unlike the Mandelbrot family this is not perturbation: the formula is transcendental
    // (cos πz), its derivative is tens per step, so δ from a reference orbit reaches the
    // size of the orbit within a couple of dozen iterations and there is nothing to rebase
    // onto. The stage engages above zoom 1e10, which is where the old ladder actually broke
    // — both the double and the decimal path computed cos/sin in double, so decimal only
    // ever raised the precision of the coordinates, never of the formula.
    private static async Task VerifyCollatzDeepZoomAsync()
    {
        static MandelbrotPalette Palette() => new()
        {
            Colors = [Colors.White, Colors.Black],
            InteriorColor = Colors.Black,
            IsGradient = true
        };

        static CollatzState View(double centerX, double centerY, double zoom,
            CollatzVariation variation, CollatzColoringMode coloring, int iterations = 150) => new()
        {
            CenterX = (decimal)centerX,
            CenterY = (decimal)centerY,
            Zoom = zoom,
            Iterations = iterations,
            Threshold = 100m,
            Variation = variation,
            ColoringMode = coloring,
            PParameter = 3m,
            QRealParameter = 0.2m,
            QImaginaryParameter = -0.1m,
            UseSmoothColoring = true,
            OrbitDensitySampleStep = 2,
            Palette = Palette()
        };

        static CollatzState Exact(double zoom, string centerX, string centerY,
            CollatzVariation variation = CollatzVariation.Standard,
            CollatzColoringMode coloring = CollatzColoringMode.EscapeTime)
        {
            CollatzState state = View(0, 0, zoom, variation, coloring);
            state.CenterXExact = centerX;
            state.CenterYExact = centerY;
            state.CenterX = BigFloat.Parse(centerX).ToDecimalClamped();
            state.CenterY = BigFloat.Parse(centerY).ToDecimalClamped();
            return state;
        }

        static int CountDiffering(byte[] a, byte[] b)
        {
            int differing = 0;
            for (int pixel = 0; pixel * 4 < a.Length; pixel++)
            {
                int offset = pixel * 4;
                if (a[offset] != b[offset] || a[offset + 1] != b[offset + 1] ||
                    a[offset + 2] != b[offset + 2]) differing++;
            }
            return differing;
        }

        // Neighbouring pixels that differ — the frame really shows structure rather than a
        // uniform fill. Counting non-black pixels is not enough: the verification palette is
        // white→black with a black interior, so a frame far outside the set comes out fully
        // non-black and tells nothing (the lesson from the Simonobrot fixtures).
        static int CountEdges(byte[] pixels, int width)
        {
            int edges = 0;
            int rows = pixels.Length / 4 / width;
            for (int y = 0; y < rows; y++)
            for (int x = 1; x < width; x++)
            {
                int offset = (y * width + x) * 4;
                if (pixels[offset] != pixels[offset - 4] || pixels[offset + 1] != pixels[offset - 3] ||
                    pixels[offset + 2] != pixels[offset - 2]) edges++;
            }
            return edges;
        }

        async Task<byte[]> RenderAsync(CollatzState state, bool? forceBigFloat, int width, int height,
            int? forcePrecisionBits = null)
        {
            byte[] pixels = new byte[width * height * 4];
            CollatzRenderer.ForceBigFloatForTests = forceBigFloat;
            CollatzRenderer.ForcePrecisionBitsForTests = forcePrecisionBits;
            try
            {
                await Task.Run(() => CollatzRenderer.Render(state, pixels, width, height, width * 4, 4,
                    CancellationToken.None));
            }
            finally
            {
                CollatzRenderer.ForceBigFloatForTests = null;
                CollatzRenderer.ForcePrecisionBitsForTests = null;
            }
            return pixels;
        }

        const int w = 64, h = 44, total = w * h;

        // 1. Where double is still trustworthy, the BigFloat stage must reproduce it. Run
        //    every variation against every coloring mode: each mode reads a different set of
        //    orbit metrics, and each variation a different branch of the formula. This is
        //    the check that the formula, the escape tests and every metric were transcribed
        //    correctly — the double path shares no code with the BigFloat one.
        var variations = new[]
        {
            CollatzVariation.Standard, CollatzVariation.SineVariation,
            CollatzVariation.ParityBranchVariation, CollatzVariation.GeneralizedP,
            CollatzVariation.GeneralizedPQ
        };
        var colorings = new[]
        {
            CollatzColoringMode.EscapeTime, CollatzColoringMode.FinalArgument,
            CollatzColoringMode.FinalMagnitude, CollatzColoringMode.CycleBasins,
            CollatzColoringMode.IntegerTrap, CollatzColoringMode.RealAxisTrap,
            CollatzColoringMode.OrbitDensity, CollatzColoringMode.PeriodDetection
        };
        int worstDiffering = 0;
        string worstLabel = "";
        foreach (CollatzVariation variation in variations)
        foreach (CollatzColoringMode coloring in colorings)
        {
            CollatzState state = View(0.5623, 0, 5000, variation, coloring,
                coloring == CollatzColoringMode.OrbitDensity ? 60 : 150);
            byte[] shallow = await RenderAsync(state, false, w, h);
            byte[] deep = await RenderAsync(state, true, w, h);
            int differing = CountDiffering(shallow, deep);
            if (differing > worstDiffering)
            {
                worstDiffering = differing;
                worstLabel = $"{variation}/{coloring}";
            }
            Check(differing * 100 <= total * 6,
                $"BigFloat stage diverges from the double stage on {differing}/{total} px " +
                $"for {variation}/{coloring} (>6%).");
        }
        Console.WriteLine($"[diag] Collatz BigFloat vs double @zoom 5e3: worst {worstDiffering}/{total} " +
                          $"({100.0 * worstDiffering / total:F2}%) at {worstLabel}");

        // 2. The same at a zoom where double is near its limit. A larger drift is expected
        //    here, and it is double's: the BigFloat stage carries ~50 spare bits there.
        CollatzState nearLimit = View(0.5623, 0, 1e8, CollatzVariation.Standard,
            CollatzColoringMode.EscapeTime);
        int nearLimitDiffering = CountDiffering(await RenderAsync(nearLimit, false, w, h),
            await RenderAsync(nearLimit, true, w, h));
        Console.WriteLine($"[diag] Collatz BigFloat vs double @zoom 1e8: {nearLimitDiffering}/{total} " +
                          $"({100.0 * nearLimitDiffering / total:F2}%)");
        Check(nearLimitDiffering * 100 <= total * 25,
            $"BigFloat stage diverges from double at 1e8 on {nearLimitDiffering}/{total} px (>25%).");

        // 3. Deep frames must complete, fill every pixel, still show structure, and leave the
        //    calling thread's working precision alone. And — the only check of the precision
        //    plan itself that needs no external oracle — the planned precision must give the
        //    same frame as a deliberately excessive one.
        Check(BigFloat.WorkingPrecisionBits == BigFloat.MinimumPrecisionBits,
            "Working precision must start at the minimum.");
        foreach ((double zoom, string centerX, string centerY) in DeepCollatzCentres)
        {
            CollatzState deep = Exact(zoom, centerX, centerY);
            var watch = Stopwatch.StartNew();
            byte[] planned = await RenderAsync(deep, null, w, h);
            watch.Stop();
            int plannedBits = CollatzRenderer.PlanPrecisionBits(deep);
            byte[] generous = await RenderAsync(deep, null, w, h, plannedBits + 256);
            int edges = CountEdges(planned, w);
            int drift = CountDiffering(planned, generous);
            Console.WriteLine($"[diag] Collatz deep {zoom:E1}: {plannedBits} bits, " +
                              $"{watch.Elapsed.TotalMilliseconds:F0} ms for {w}×{h}, edges {edges}, " +
                              $"drift vs +256 bits {drift}/{total}");
            Check(planned.Where((_, index) => index % 4 == 3).All(value => value == 255),
                $"Deep Collatz render at {zoom:E1} left pixels unfilled.");
            Check(edges >= 40, $"Deep Collatz render at {zoom:E1} shows no structure (edges {edges}).");
            Check(drift * 100 <= total * 2,
                $"The precision plan is short at {zoom:E1}: {drift}/{total} px change when given 256 more bits.");
        }

        // 4. Past the deepest centre we have, structure is not guaranteed — but the stage
        //    must still run to completion at the zoom ceiling the window allows.
        foreach (double zoom in new[] { 1e30, 1e50 })
        {
            CollatzState extreme = Exact(zoom, DeepCollatzCentres[^1].CenterX, DeepCollatzCentres[^1].CenterY);
            var watch = Stopwatch.StartNew();
            byte[] pixels = await RenderAsync(extreme, null, w, h);
            watch.Stop();
            Console.WriteLine($"[diag] Collatz extreme {zoom:E0}: " +
                              $"{CollatzRenderer.PlanPrecisionBits(extreme)} bits, " +
                              $"{watch.Elapsed.TotalMilliseconds:F0} ms for {w}×{h}");
            Check(pixels.Where((_, index) => index % 4 == 3).All(value => value == 255),
                $"Collatz render at {zoom:E0} left pixels unfilled.");
        }
        Check(BigFloat.WorkingPrecisionBits == BigFloat.MinimumPrecisionBits,
            "Deep Collatz render must restore the calling thread's working precision.");

        // 5. The tile path and the full-frame path must agree pixel for pixel: the window
        //    renders tiles, the exporter renders whole frames.
        CollatzState tiled = Exact(DeepCollatzCentres[^1].Zoom, DeepCollatzCentres[^1].CenterX,
            DeepCollatzCentres[^1].CenterY);
        byte[] full = await RenderAsync(tiled, null, w, h);
        var tile = new MandelbrotRenderTile(16, 12, 32, 20, 1, 1);
        byte[]? tilePixels = await Task.Run(() =>
            CollatzRenderer.RenderTile(tiled, w, h, tile, CancellationToken.None));
        Check(tilePixels is not null, "Deep Collatz tile render returned null without cancellation.");
        int tileDiffering = 0;
        for (int y = 0; y < tile.Height; y++)
        for (int x = 0; x < tile.Width; x++)
        {
            int tileOffset = (y * tile.Width + x) * 4;
            int frameOffset = ((tile.Y + y) * w + tile.X + x) * 4;
            if (tilePixels![tileOffset] != full[frameOffset] ||
                tilePixels[tileOffset + 1] != full[frameOffset + 1] ||
                tilePixels[tileOffset + 2] != full[frameOffset + 2]) tileDiffering++;
        }
        Check(tileDiffering == 0,
            $"Deep Collatz tile disagrees with the full frame on {tileDiffering} px.");

        // 6. The exact centre must actually reach the renderer: a shift of a tenth of a pixel
        //    at 1e18 is far below what the decimal centre can hold, so if the exact strings
        //    were being ignored the frame would not move at all. And an exact centre equal to
        //    the decimal one must change nothing.
        CollatzState shifted = Exact(DeepCollatzCentres[^1].Zoom,
            ShiftCentre(DeepCollatzCentres[^1].CenterX, DeepCollatzCentres[^1].Zoom),
            DeepCollatzCentres[^1].CenterY);
        Check(CountDiffering(full, await RenderAsync(shifted, null, w, h)) > 0,
            "A sub-decimal shift of the exact centre changed nothing — the exact centre is ignored.");

        CollatzState plain = View(0.5623, 0, 1e12, CollatzVariation.Standard, CollatzColoringMode.EscapeTime);
        CollatzState mirrored = View(0.5623, 0, 1e12, CollatzVariation.Standard, CollatzColoringMode.EscapeTime);
        mirrored.CenterXExact = "0.5623";
        mirrored.CenterYExact = "0";
        Check(CountDiffering(await RenderAsync(plain, null, w, h),
                  await RenderAsync(mirrored, null, w, h)) == 0,
            "An exact centre equal to the decimal centre must render identically.");

        // 7. Determinism: the stage is parallel over rows and keeps per-thread state (the
        //    orbit history buffer and the working precision).
        CollatzState repeat = Exact(DeepCollatzCentres[0].Zoom, DeepCollatzCentres[0].CenterX,
            DeepCollatzCentres[0].CenterY, CollatzVariation.GeneralizedPQ,
            CollatzColoringMode.CycleBasins);
        Check(CountDiffering(await RenderAsync(repeat, null, w, h),
                  await RenderAsync(repeat, null, w, h)) == 0,
            "Two identical deep Collatz renders differ — the stage is not deterministic.");

        // 8. The precision plan must grow with depth and never drop below the floor.
        int previousBits = 0;
        foreach (double zoom in new[] { 1e10, 1e15, 1e20, 1e30, 1e40, 1e50 })
        {
            int bits = CollatzRenderer.PlanPrecisionBits(View(0, 0, zoom, CollatzVariation.Standard,
                CollatzColoringMode.EscapeTime));
            Check(bits >= 128 && bits >= previousBits,
                $"Precision plan is not monotonic: {bits} bits at zoom {zoom:E0} after {previousBits}.");
            previousBits = bits;
        }
    }

    // Moves an exact centre by about a tenth of a pixel at the given zoom — a difference the
    // decimal centre cannot represent at these depths.
    private static string ShiftCentre(string centre, double zoom)
    {
        using var precision = new BigFloat.PrecisionScope(1024);
        BigFloat step = BigFloat.FromInt(4) / BigFloat.FromDouble(zoom) / 640;
        return (BigFloat.Parse(centre) + step).ToInvariantString();
    }

    private static void VerifyBigFloatSqrt()
    {
        // First 100 digits of √2.
        const string Root2 =
            "1.414213562373095048801688724209698078569671875376948073176679737990732478462107038850387534327641572";

        using (new BigFloat.PrecisionScope(BigFloat.MinimumPrecisionBits))
        {
            string produced = BigFloat.Sqrt(BigFloat.FromInt(2)).ToInvariantString(120);
            int common = 0;
            while (common < produced.Length && common < Root2.Length && produced[common] == Root2[common]) common++;
            // 384 bits ≈ 115 decimal digits, so all 100 published ones must come out right.
            Check(common >= Root2.Length,
                $"BigFloat.Sqrt(2) matches only {common} of {Root2.Length} published characters: {produced}");
        }

        foreach (int bits in new[] { BigFloat.MinimumPrecisionBits, 512, 1024 })
        {
            using var precision = new BigFloat.PrecisionScope(bits);
            foreach (string text in new[] { "2", "3", "0.9999999999999", "1e-120", "1e40", "1e-300" })
            {
                BigFloat value = BigFloat.Parse(text);
                BigFloat root = BigFloat.Sqrt(value);
                BigFloat error = root * root - value;
                if (error.Sign < 0) error = -error;
                // Two roundings (the root and the squaring) at `bits` significant bits. The
                // bound is scaled in BigFloat, not in double: at 1e-300 a relative ratio
                // taken through ToDouble would underflow to zero and pass vacuously.
                BigFloat tolerance = value * BigFloat.FromDouble(System.Math.ScaleB(1.0, -(bits - 4)));
                Check(error.CompareTo(tolerance) <= 0,
                    $"BigFloat.Sqrt({text}) at {bits} bits: (√x)² is off by {error.ToInvariantString(20)}.");
            }

            foreach (string text in new[] { "4", "0.25", "1", "1e-100", "1e100", "12345678901234567890" })
            {
                BigFloat value = BigFloat.Parse(text);
                Check((BigFloat.Sqrt(value * value) - value).IsZero,
                    $"BigFloat.Sqrt of the perfect square ({text})² must return exactly {text} at {bits} bits.");
            }

            Check(BigFloat.Sqrt(BigFloat.Zero).IsZero, "BigFloat.Sqrt(0) must be zero.");
        }

        Check(BigFloat.WorkingPrecisionBits == BigFloat.MinimumPrecisionBits,
            "BigFloat.Sqrt checks must leave the working precision restored.");
    }

    // Phase 6: Simonobrot of even integer power p=2q — composition of two exact binomial
    // perturbations (zᵖ and |z|ᵖ=Mᵠ). Phase 10 added odd p=2q+1, where the modulus factor
    // carries a square root and is perturbed by the exact identity
    // δ√M = δm/(√(M+δm)+√M). Verified against the exact BigFloat reference, both with and
    // without UseInversion (which flips the sign the reference-orbit cache key must also
    // carry) — and, for the odd powers, against the plain-double Iterate path as well:
    // that one goes through Complex.Pow/Math.Pow, so it is the only oracle that does not
    // share StepSimonobrotReference with the engine under test.
    private static async Task VerifySimonobrotDeepZoomAsync(Func<MandelbrotPalette> palette)
    {
        static int CountRgbDiffering(byte[] a, byte[] b)
        {
            int n = 0;
            for (int pixel = 0; pixel * 4 < a.Length; pixel++)
            {
                int o = pixel * 4;
                if (a[o] != b[o] || a[o + 1] != b[o + 1] || a[o + 2] != b[o + 2]) n++;
            }
            return n;
        }

        async Task<byte[]> RenderAsync(MandelbrotState state, bool? forceDeep, int w, int h)
        {
            byte[] pixels = new byte[w * h * 4];
            MandelbrotFamilyRenderer.ForceDeepZoomForTests = forceDeep;
            try
            {
                await Task.Run(() => MandelbrotFamilyRenderer.Render(state, pixels, w, h, w * 4, CancellationToken.None));
            }
            finally { MandelbrotFamilyRenderer.ForceDeepZoomForTests = null; }
            return pixels;
        }

        const int w = 56, h = 40, total = w * h;

        // Structured boundary views per power/inversion combo (auto-located).
        (int Power, bool Inversion, decimal Cx, decimal Cy)[] cases =
        {
            (2, false, -0.03m, 0.84m),
            (2, true,   0.03m, 0.84m),
            (6, false, -0.90m, 0.18m),
            (8, true,  -0.33m, 0.87m),
            (12, false, -0.12m, 0.84m),
            // Phase 10 — odd powers, where the modulus factor is Mᵠ·√M. Centres picked for
            // edge density (the detail the kernel has to reproduce), not just for non-black.
            (3, false, -0.70m, -0.35m),
            (5, false,  0.55m, -0.85m),
            (7, true,   0.30m, 0.85m),
            (9, false,  0.95m, 0.25m),
            (11, false, -0.10m, -0.95m),
        };

        foreach ((int power, bool inversion, decimal cx, decimal cy) in cases)
        {
            var state = new MandelbrotState
            {
                Variant = MandelbrotVariant.Simonobrot,
                Power = power,
                UseInversion = inversion,
                CenterX = cx,
                CenterY = cy,
                Zoom = 300.0,
                Iterations = 2000,
                Threads = 2,
                Palette = palette()
            };
            byte[] perturbation = await RenderAsync(state, forceDeep: true, w, h);
            byte[] exact = await Task.Run(() =>
                MandelbrotFamilyRenderer.RenderExactReferenceForTests(state, w, h, CancellationToken.None));
            // The exact reference and the reference orbit share StepSimonobrotReference, so
            // it cannot catch an error in the formula itself — only in the perturbation on
            // top of it. The plain-double path can: it computes zᵖ·|z|ᵖ through
            // Complex.Pow and Math.Pow(√M, p), independently of everything above. At zoom
            // 300 double is exact enough to be that oracle.
            byte[] flat = await RenderAsync(state, forceDeep: false, w, h);

            int vsExact = CountRgbDiffering(perturbation, exact);
            int vsFlat = CountRgbDiffering(perturbation, flat);
            int maxD = 0, nonBlack = 0;
            for (int i = 0; i < total; i++)
            {
                int o = i * 4;
                maxD = Math.Max(maxD, Math.Max(Math.Abs(perturbation[o] - exact[o]),
                    Math.Max(Math.Abs(perturbation[o + 1] - exact[o + 1]), Math.Abs(perturbation[o + 2] - exact[o + 2]))));
                if (perturbation[o] != 0 || perturbation[o + 1] != 0 || perturbation[o + 2] != 0) nonBlack++;
            }
            Console.WriteLine($"[diag] Simonobrot p={power} inv={inversion}: vs exact {vsExact}/{total} (maxΔ {maxD}), " +
                $"vs plain double {vsFlat}/{total}, nonblack {nonBlack}");
            Check(nonBlack > total / 10, $"Simonobrot p={power} inv={inversion} view must carry structure.");
            Check(vsExact * 100 <= total * 3,
                $"Simonobrot p={power} inv={inversion}: perturbation diverges from exact on {vsExact}/{total} px, maxΔ {maxD} (>3%).");
            Check(vsFlat * 100 <= total * 3,
                $"Simonobrot p={power} inv={inversion}: perturbation diverges from the plain-double formula on {vsFlat}/{total} px (>3%).");
        }

        // UseInversion must be part of the reference-orbit cache key: two renders that
        // differ only by it, at the same centre/zoom, must not collide.
        {
            var baseState = new MandelbrotState
            {
                Variant = MandelbrotVariant.Simonobrot,
                Power = 2,
                CenterX = -0.03m,
                CenterY = 0.84m,
                Zoom = 300.0,
                Iterations = 2000,
                Threads = 2,
                Palette = palette()
            };
            var inverted = new MandelbrotState
            {
                Variant = MandelbrotVariant.Simonobrot,
                Power = 2,
                UseInversion = true,
                CenterX = -0.03m,
                CenterY = 0.84m,
                Zoom = 300.0,
                Iterations = 2000,
                Threads = 2,
                Palette = palette()
            };
            byte[] a = await RenderAsync(baseState, forceDeep: true, w, h);
            byte[] b = await RenderAsync(inverted, forceDeep: true, w, h);
            Check(CountRgbDiffering(a, b) > total / 4,
                "UseInversion must be part of the orbit cache key (renders collided).");
        }
    }

    // How significant is the quality loss of the production engine (perturbation + BLA +
    // FloatExp) versus a near-exact BigFloat direct-iteration reference? Reports the
    // fraction of differing pixels and the worst per-channel delta per view.
    private static async Task VerifyEngineAccuracyAsync(Func<MandelbrotPalette> palette)
    {
        static (int Differing, int MaxDelta) Compare(byte[] a, byte[] b)
        {
            int differing = 0, maxDelta = 0;
            for (int pixel = 0; pixel * 4 < a.Length; pixel++)
            {
                int o = pixel * 4;
                int d = Math.Max(Math.Abs(a[o] - b[o]),
                    Math.Max(Math.Abs(a[o + 1] - b[o + 1]), Math.Abs(a[o + 2] - b[o + 2])));
                if (d != 0) differing++;
                maxDelta = Math.Max(maxDelta, d);
            }
            return (differing, maxDelta);
        }

        MandelbrotPalette Pal() => palette();

        MandelbrotState Deep(MandelbrotVariant variant, decimal cx, decimal cy, double zoom, int iterations,
            decimal power = 2m, decimal jr = 0m, decimal ji = 0m) => new()
        {
            Variant = variant,
            CenterX = cx,
            CenterY = cy,
            Power = power,
            JuliaCReal = jr,
            JuliaCImaginary = ji,
            Zoom = zoom,
            Iterations = iterations,
            Threads = 2,
            Palette = Pal()
        };

        const int w = 48, h = 32, total = w * h;
        var mandelCentre = (X: -1.2628848671045503000020782246m, Y: 0.0409687601493310685285376264m);

        (string Label, MandelbrotState State)[] views =
        {
            ("Mandelbrot 1e30",        Deep(MandelbrotVariant.Mandelbrot, mandelCentre.X, mandelCentre.Y, 1.0e30, 4500)),
            ("Mandelbrot 1e120 (fexp)",Deep(MandelbrotVariant.Mandelbrot, mandelCentre.X, mandelCentre.Y, 1.0e120, 4500)),
            ("BurningShip 1e30",       Deep(MandelbrotVariant.BurningShip, -1.62m, 0m, 1.0e30, 3000)),
        };

        foreach ((string label, MandelbrotState state) in views)
        {
            byte[] engine = new byte[w * h * 4];
            await Task.Run(() => MandelbrotFamilyRenderer.Render(state, engine, w, h, w * 4, CancellationToken.None));
            byte[] exact = await Task.Run(() =>
                MandelbrotFamilyRenderer.RenderExactReferenceForTests(state, w, h, CancellationToken.None));
            (int differing, int maxDelta) = Compare(engine, exact);
            int nonBlack = 0;
            for (int i = 0; i < total; i++)
                if (engine[i * 4] != 0 || engine[i * 4 + 1] != 0 || engine[i * 4 + 2] != 0) nonBlack++;
            double percent = 100.0 * differing / total;
            Console.WriteLine($"[diag] accuracy {label}: {differing}/{total} px differ ({percent:F2}%), max Δ {maxDelta}, nonblack {nonBlack}");
            Check(differing * 100 <= total * 5,
                $"Engine diverges from the exact reference on {differing}/{total} px for {label} (>5%).");
        }
    }

    // Phase 4: reflected/conjugate variants (Burning Ship, Julia Burning Ship, Tricorn,
    // Buffalo, Celtic) on the perturbation engine via a sign-folded δ recurrence (no BLA).
    // Where decimal is still trustworthy the perturbation render must reproduce it up to
    // boundary chaos; Mandelbrot/Julia must be untouched.
    private static async Task VerifyReflectedVariantsAsync(Func<MandelbrotPalette> palette)
    {
        static int CountRgbDiffering(byte[] a, byte[] b)
        {
            int n = 0;
            for (int pixel = 0; pixel * 4 < a.Length; pixel++)
            {
                int o = pixel * 4;
                if (a[o] != b[o] || a[o + 1] != b[o + 1] || a[o + 2] != b[o + 2]) n++;
            }
            return n;
        }

        async Task<byte[]> RenderAsync(MandelbrotState state, bool? forceDeep, int w, int h)
        {
            byte[] pixels = new byte[w * h * 4];
            MandelbrotFamilyRenderer.ForceDeepZoomForTests = forceDeep;
            try
            {
                await Task.Run(() => MandelbrotFamilyRenderer.Render(state, pixels, w, h, w * 4, CancellationToken.None));
            }
            finally { MandelbrotFamilyRenderer.ForceDeepZoomForTests = null; }
            return pixels;
        }

        const int w = 120, h = 80, total = w * h;

        (MandelbrotVariant variant, decimal cx, decimal cy, decimal jr, decimal ji, string label)[] cases =
        {
            (MandelbrotVariant.BurningShip,      -1.62m, 0m,     0m,     0m,   "BurningShip"),
            (MandelbrotVariant.Tricorn,          -1.62m, 0m,     0m,     0m,   "Tricorn"),
            (MandelbrotVariant.Buffalo,          -1.62m, 0m,     0m,     0m,   "Buffalo"),
            (MandelbrotVariant.Celtic,           -1.62m, 0m,     0m,     0m,   "Celtic"),
            (MandelbrotVariant.JuliaBurningShip,  0.5m, -0.3m, -1.5m,   0m,   "JuliaBurningShip"),
        };

        foreach ((MandelbrotVariant variant, decimal cx, decimal cy, decimal jr, decimal ji, string label) in cases)
        {
            foreach (double zoom in new[] { 1.0e10, 1.0e16 })
            {
                var state = new MandelbrotState
                {
                    Variant = variant,
                    CenterX = cx,
                    CenterY = cy,
                    JuliaCReal = jr,
                    JuliaCImaginary = ji,
                    Zoom = zoom,
                    Iterations = 3000,
                    Threshold = 2m,
                    Threads = 2,
                    Palette = palette()
                };
                byte[] decimalPixels = await RenderAsync(state, forceDeep: false, w, h);
                byte[] perturbationPixels = await RenderAsync(state, forceDeep: true, w, h);
                int differing = CountRgbDiffering(decimalPixels, perturbationPixels);
                Console.WriteLine($"[diag] {label} zoom {zoom:E0}: decimal vs perturbation {differing}/{total} px differ");
                Check(perturbationPixels.Where((_, index) => index % 4 != 3).Any(value => value != 0),
                    $"{label} perturbation must resolve structure at zoom {zoom:E0}.");
                Check(differing * 100 <= total * 8,
                    $"{label}: perturbation diverges from decimal on {differing}/{total} px at zoom {zoom:E0} (>8%).");
            }
        }

        // Regression guard: the reflected code path must not touch Mandelbrot.
        var mandel = new MandelbrotState
        {
            CenterX = -1.2628848671045503000020782246m,
            CenterY = 0.0409687601493310685285376264m,
            Zoom = 5.0e25,
            Iterations = 4000,
            Threads = 2,
            Palette = palette()
        };
        byte[] a1 = await RenderAsync(mandel, forceDeep: true, w, h);
        byte[] a2 = await RenderAsync(mandel, forceDeep: true, w, h);
        Check(CountRgbDiffering(a1, a2) == 0, "Mandelbrot deep render must stay deterministic after Phase 4.");
    }

    // Phase 3: BLA (Zhuoran) skips runs of iterations where δ stays small. It is a pure
    // accelerator over the Phase 2 perturbation engine — the output must match BLA-off up
    // to a few boundary pixels (composite BLAs carry their own rounding order), and the
    // OrbitTrap/StripeAverage modes must be untouched (BLA disabled there).
    private static async Task VerifyBlaAccelerationAsync(Func<MandelbrotPalette> palette)
    {
        static int CountRgbDiffering(byte[] a, byte[] b)
        {
            int n = 0;
            for (int pixel = 0; pixel * 4 < a.Length; pixel++)
            {
                int o = pixel * 4;
                if (a[o] != b[o] || a[o + 1] != b[o + 1] || a[o + 2] != b[o + 2]) n++;
            }
            return n;
        }

        async Task<byte[]> RenderAsync(MandelbrotState state, bool bla, int w, int h)
        {
            byte[] pixels = new byte[w * h * 4];
            MandelbrotFamilyRenderer.ForceBlaForTests = bla;
            try
            {
                await Task.Run(() => MandelbrotFamilyRenderer.Render(state, pixels, w, h, w * 4, CancellationToken.None));
            }
            finally { MandelbrotFamilyRenderer.ForceBlaForTests = null; }
            return pixels;
        }

        MandelbrotState Deep(double zoom, int iterations, MandelbrotColoringMode mode = MandelbrotColoringMode.Smooth) => new()
        {
            CenterX = -1.2628848671045503000020782246m,
            CenterY = 0.0409687601493310685285376264m,
            Zoom = zoom,
            Iterations = iterations,
            Threads = 2,
            ColoringMode = mode,
            Palette = palette()
        };

        const int w = 120, h = 80, total = w * h;

        // (a) BLA on vs off across the engine's regimes (double-δ, deeper double-δ, FloatExp-δ).
        foreach ((double zoom, int iterations, string label) in new[]
        {
            (5.0e25, 4000, "double-δ 5e25"),
            (1.0e40, 5000, "double-δ 1e40"),
            (1.0e120, 3500, "FloatExp-δ 1e120"),
        })
        {
            MandelbrotState state = Deep(zoom, iterations);
            byte[] off = await RenderAsync(state, bla: false, w, h);
            byte[] on = await RenderAsync(state, bla: true, w, h);
            int differing = CountRgbDiffering(off, on);
            Console.WriteLine($"[diag] BLA {label}: on vs off {differing}/{total} px differ");
            Check(on.Where((_, index) => index % 4 != 3).Any(value => value != 0),
                $"BLA render must resolve structure ({label}).");
            Check(differing * 100 <= total * 2,
                $"BLA changes {differing}/{total} px vs non-BLA ({label}, >2%).");
        }

        // (b) Julia deep (B = 0 in the BLA table): on vs off must still match.
        {
            var julia = new MandelbrotState
            {
                Variant = MandelbrotVariant.Julia,
                JuliaCReal = -0.8m,
                JuliaCImaginary = 0.156m,
                CenterX = 0.15m,
                CenterY = 0.30m,
                Zoom = 5.0e25,
                Iterations = 4000,
                Threads = 2,
                Palette = palette()
            };
            int differing = CountRgbDiffering(
                await RenderAsync(julia, bla: false, w, h),
                await RenderAsync(julia, bla: true, w, h));
            Console.WriteLine($"[diag] BLA Julia 5e25: on vs off {differing}/{total} px differ");
            Check(differing * 100 <= total * 2, $"BLA changes Julia {differing}/{total} px (>2%).");
        }

        // (c) OrbitTrap coloring keeps every iteration ⇒ BLA is disabled ⇒ byte-identical.
        {
            MandelbrotState trap = Deep(5.0e25, 4000, MandelbrotColoringMode.OrbitTrap);
            int differing = CountRgbDiffering(
                await RenderAsync(trap, bla: false, w, h),
                await RenderAsync(trap, bla: true, w, h));
            Check(differing == 0, $"BLA must be inert for OrbitTrap coloring, changed {differing}/{total} px.");
        }

        // (d) Speed: a high-iteration deep view. BLA must not be slower; report the ratio.
        {
            MandelbrotState heavy = Deep(1.0e55, 24000);
            const int hw = 160, hh = 108;
            byte[] warm = new byte[hw * hh * 4];
            MandelbrotFamilyRenderer.ForceBlaForTests = false;
            MandelbrotFamilyRenderer.Render(heavy, warm, hw, hh, hw * 4, CancellationToken.None);

            var clock = Stopwatch.StartNew();
            MandelbrotFamilyRenderer.ForceBlaForTests = false;
            MandelbrotFamilyRenderer.Render(heavy, warm, hw, hh, hw * 4, CancellationToken.None);
            double offMs = clock.Elapsed.TotalMilliseconds;

            clock.Restart();
            MandelbrotFamilyRenderer.ForceBlaForTests = true;
            MandelbrotFamilyRenderer.Render(heavy, warm, hw, hh, hw * 4, CancellationToken.None);
            double onMs = clock.Elapsed.TotalMilliseconds;
            MandelbrotFamilyRenderer.ForceBlaForTests = null;

            Console.WriteLine($"[diag] BLA speed 1e55 i24000: off {offMs:F0}ms, on {onMs:F0}ms, ×{offMs / onMs:F2}");
            Check(onMs <= offMs * 1.25, $"BLA slower than non-BLA: {onMs:F0}ms vs {offMs:F0}ms.");
        }

        await Task.CompletedTask;
    }

    // Phase 9: BLA with a REAL 2x2 linear part. The five reflected/conjugate variants
    // (Burning Ship, Julia Burning Ship, Tricorn, Buffalo, Celtic) and even-power Simonobrot
    // advance delta through a real linear map, not a complex multiplication, so they get their
    // own pyramid (RealBlaTable); the complex BlaTable is left untouched and still serves
    // Mandelbrot/Julia/Multibrot. Like the complex one this is a pure accelerator, hence:
    //   (a) it must actually engage - the skip counter guards against a vacuous pass;
    //   (b) it must cost the picture nothing. Views deep enough to engage BLA sit in
    //       boundary-chaotic territory, where the engine already differs from an exact BigFloat
    //       render on a few percent of pixels. The honest metric is therefore the EXCESS of
    //       that difference over the same render with BLA off, not the raw diff;
    //   (c) coloring modes that consume every single iteration (orbit trap, stripe average,
    //       distance estimation) must keep BLA off and stay byte-identical.
    // The centres are exact decimal strings found by walking into each variant's boundary, so
    // the frames stay structured at these depths instead of degenerating into a flat field.
    private static async Task VerifyRealBlaAccelerationAsync(Func<MandelbrotPalette> palette)
    {
        static int CountRgbDiffering(byte[] a, byte[] b)
        {
            int n = 0;
            for (int pixel = 0; pixel * 4 < a.Length; pixel++)
            {
                int o = pixel * 4;
                if (a[o] != b[o] || a[o + 1] != b[o + 1] || a[o + 2] != b[o + 2]) n++;
            }
            return n;
        }

        MandelbrotState State(
            MandelbrotVariant variant, string centreX, string centreY, double zoom,
            MandelbrotColoringMode mode = MandelbrotColoringMode.Smooth,
            decimal power = 2m, decimal juliaReal = 0m, decimal juliaImaginary = 0m,
            bool inversion = false) => new()
            {
                Variant = variant,
                ColoringMode = mode,
                // decimal only carries ~28 digits; the deep engine reads the exact strings.
                CenterX = decimal.Parse(centreX[..System.Math.Min(centreX.Length, 20)], CultureInfo.InvariantCulture),
                CenterY = decimal.Parse(centreY[..System.Math.Min(centreY.Length, 20)], CultureInfo.InvariantCulture),
                CenterXExact = centreX,
                CenterYExact = centreY,
                Power = power,
                JuliaCReal = juliaReal,
                JuliaCImaginary = juliaImaginary,
                UseInversion = inversion,
                Zoom = zoom,
                Iterations = 6000,
                Threshold = 2m,
                Threads = 2,
                Palette = palette(),
            };

        async Task<byte[]> RenderAsync(MandelbrotState state, bool bla, int w, int h)
        {
            byte[] pixels = new byte[w * h * 4];
            MandelbrotFamilyRenderer.ForceBlaForTests = bla;
            MandelbrotFamilyRenderer.ForceDeepZoomForTests = true;
            try
            {
                await Task.Run(() => MandelbrotFamilyRenderer.Render(state, pixels, w, h, w * 4, CancellationToken.None));
            }
            finally
            {
                MandelbrotFamilyRenderer.ForceBlaForTests = null;
                MandelbrotFamilyRenderer.ForceDeepZoomForTests = null;
            }
            return pixels;
        }

        async Task<long> CountSkipsAsync(MandelbrotState state, int w, int h)
        {
            MandelbrotFamilyRenderer.RealBlaSkippedIterationsForTests = 0;
            MandelbrotFamilyRenderer.CountRealBlaSkipsForTests = true;
            try { await RenderAsync(state, bla: true, w, h); }
            finally { MandelbrotFamilyRenderer.CountRealBlaSkipsForTests = false; }
            return MandelbrotFamilyRenderer.RealBlaSkippedIterationsForTests;
        }

        const string burningShipX = "-0.81350985269959441502640438927923405594718016208354671578096";
        const string burningShipY = "1.15385056973366263716386423762505110184996148623201569967787";
        const string celticX = "-0.891865646507391491113368732535744974340110083129727811184488";
        const string celticY = "1.544758206384140070271947748309466179897357707661851201063699";
        const string simonobrot4X = "0.279452570816264365821172574207784238378988855076446292869889";
        const string simonobrot4Y = "1.018073395776969286854111772196867045308653990469452818298968";

        (string Label, MandelbrotState State)[] fixtures =
        {
            ("BurningShip 3e30", State(MandelbrotVariant.BurningShip,
                "-0.813509852699594415026404389279137158659901740416441180706343",
                "1.153850569733662637163864237624756879942846369129799170139332", 3.1622776601683795e30)),
            ("BurningShip 1e45", State(MandelbrotVariant.BurningShip, burningShipX, burningShipY, 1.0e45)),
            ("Tricorn 3e30", State(MandelbrotVariant.Tricorn,
                "0.740107894676546596166278068634689383680928471433837845616988",
                "1.284164879491910489407619722838421840742204004581206006914469", 3.1622776601683795e30)),
            ("Buffalo 1e45", State(MandelbrotVariant.Buffalo,
                "0.251515542826420576827659826958312420927687487022709942977638",
                "0.454127991517863684724990589158969466705687426119938699326094", 1.0e45)),
            ("Celtic 3e30", State(MandelbrotVariant.Celtic, celticX, celticY, 3.1622776601683795e30)),
            ("JuliaBurningShip 1e45", State(MandelbrotVariant.JuliaBurningShip,
                "-0.024227550790277459831804555192054182024312705553405370339065",
                "0.56875673781379166740393234420623920098438359700265085138842", 1.0e45,
                juliaReal: -1.5m, juliaImaginary: 0.02m)),
            ("Simonobrot p2 1e28", State(MandelbrotVariant.Simonobrot,
                "-0.233085135590328323057688239020593618626185475481456854709886",
                "0.956730152389579347325401450437689559915308904251176295871525", 1.0e28, power: 2m)),
            ("Simonobrot p4 1e28", State(MandelbrotVariant.Simonobrot, simonobrot4X, simonobrot4Y, 1.0e28, power: 4m)),
            ("Simonobrot p6 inv 1e28", State(MandelbrotVariant.Simonobrot,
                "0.227591276012239845382720882305167929274131349145291924124711",
                "1.029105663003342779985661616216266280026506157420936047716223", 1.0e28,
                power: 6m, inversion: true)),
            ("Simonobrot p12 1e28", State(MandelbrotVariant.Simonobrot,
                "0.122209692688143080456363858471452783327760583215973003556001",
                "1.021769700244342585739196861366295324922134500631004946262376", 1.0e28, power: 12m)),
            // Phase 10 — odd powers: the same real 2×2 table, with both half-integer powers
            // of the modulus (M^(p/2) and M^(p/2−1)) carrying a √M factor.
            ("Simonobrot p3 1e28", State(MandelbrotVariant.Simonobrot,
                "-0.202832656913406719584951309136145088020207829768900793575531",
                "1.112011809221549716846392243745997167999832561088051678883473", 1.0e28, power: 3m)),
            ("Simonobrot p5 1e28", State(MandelbrotVariant.Simonobrot,
                "-0.548667511943908696630758431962511649547657017247242149297411",
                "0.872765118321406809316583683587250477176667101144019039749011", 1.0e28, power: 5m)),
        };

        const int w = 120, h = 80, total = w * h;
        const int ew = 36, eh = 24, etotal = ew * eh;

        foreach ((string label, MandelbrotState state) in fixtures)
        {
            long skips = await CountSkipsAsync(state, w, h);
            byte[] off = await RenderAsync(state, bla: false, w, h);
            byte[] on = await RenderAsync(state, bla: true, w, h);
            int differing = CountRgbDiffering(off, on);

            Check(on.Where((_, index) => index % 4 != 3).Any(value => value != 0),
                $"Real BLA render must resolve structure ({label}).");
            Check(skips > 0, $"Real BLA never engaged on {label}: the fixture proves nothing.");

            byte[] exact = await Task.Run(() =>
                MandelbrotFamilyRenderer.RenderExactReferenceForTests(state, ew, eh, CancellationToken.None));
            int exactVersusOff = CountRgbDiffering(exact, await RenderAsync(state, bla: false, ew, eh));
            int exactVersusOn = CountRgbDiffering(exact, await RenderAsync(state, bla: true, ew, eh));
            int excess = exactVersusOn - exactVersusOff;

            Console.WriteLine($"[diag] real BLA {label}: skipped {skips:N0} iterations, on vs off " +
                $"{differing}/{total} px, vs exact {exactVersusOff} -> {exactVersusOn}/{etotal} (excess {excess})");

            // Pure accelerator: it may add no error of its own beyond the boundary chaos the
            // engine already carries. 2% of the sampled pixels is the whole budget.
            Check(excess * 50 <= etotal,
                $"Real BLA adds {excess}/{etotal} px of error over non-BLA on {label} (>2%).");
        }

        // Odd-power Simonobrot must reach the deep engine through the production gate, not
        // only through the test seam: without ForceDeepZoomForTests the render still has to
        // engage the real BLA, which only the perturbation kernels ever consult.
        {
            MandelbrotState state = fixtures[^2].State;   // Simonobrot p3 1e28
            byte[] pixels = new byte[w * h * 4];
            MandelbrotFamilyRenderer.RealBlaSkippedIterationsForTests = 0;
            MandelbrotFamilyRenderer.CountRealBlaSkipsForTests = true;
            try
            {
                await Task.Run(() => MandelbrotFamilyRenderer.Render(state, pixels, w, h, w * 4, CancellationToken.None));
            }
            finally { MandelbrotFamilyRenderer.CountRealBlaSkipsForTests = false; }

            Check(MandelbrotFamilyRenderer.RealBlaSkippedIterationsForTests > 0,
                "Odd-power Simonobrot must select the deep engine at 1e28 without the test seam.");
            Check(CountRgbDiffering(pixels, await RenderAsync(state, bla: true, w, h)) == 0,
                "The production gate must produce exactly the forced-deep render for odd-power Simonobrot.");
        }

        // Coloring modes that consume every iteration keep BLA off, so they stay byte-identical.
        foreach (MandelbrotColoringMode mode in new[]
        {
            MandelbrotColoringMode.OrbitTrap,
            MandelbrotColoringMode.StripeAverage,
            MandelbrotColoringMode.DistanceEstimation,
        })
        {
            foreach ((string label, MandelbrotState state) in new (string, MandelbrotState)[]
            {
                ("BurningShip", State(MandelbrotVariant.BurningShip, burningShipX, burningShipY, 1.0e45, mode)),
                ("Celtic", State(MandelbrotVariant.Celtic, celticX, celticY, 3.1622776601683795e30, mode)),
                ("Simonobrot p4", State(MandelbrotVariant.Simonobrot, simonobrot4X, simonobrot4Y, 1.0e28, mode, power: 4m)),
            })
            {
                int differing = CountRgbDiffering(
                    await RenderAsync(state, bla: false, w, h),
                    await RenderAsync(state, bla: true, w, h));
                Check(differing == 0,
                    $"Real BLA must be inert for {mode} ({label}), changed {differing}/{total} px.");
            }
        }

        // Speed - the whole point of the phase. The reflected step is cheap, so skipping runs of
        // it pays off the most; Simonobrot's own step is heavy enough that the win is smaller,
        // and there the requirement is only that the lookup must not cost more than it saves.
        foreach ((string label, MandelbrotState state, double minimumRatio) in
            new (string, MandelbrotState, double)[]
            {
                ("Tricorn 3e30", fixtures[2].State, 2.0),
                ("Simonobrot p4 1e28", fixtures[7].State, 0.8),
            })
        {
            const int sw = 160, sh = 108;
            byte[] scratch = new byte[sw * sh * 4];
            MandelbrotFamilyRenderer.ForceDeepZoomForTests = true;
            double offMs = double.MaxValue, onMs = double.MaxValue;
            try
            {
                MandelbrotFamilyRenderer.ForceBlaForTests = false;
                MandelbrotFamilyRenderer.Render(state, scratch, sw, sh, sw * 4, CancellationToken.None);
                var clock = new Stopwatch();
                for (int trial = 0; trial < 3; trial++)
                {
                    clock.Restart();
                    MandelbrotFamilyRenderer.ForceBlaForTests = false;
                    MandelbrotFamilyRenderer.Render(state, scratch, sw, sh, sw * 4, CancellationToken.None);
                    offMs = System.Math.Min(offMs, clock.Elapsed.TotalMilliseconds);

                    clock.Restart();
                    MandelbrotFamilyRenderer.ForceBlaForTests = true;
                    MandelbrotFamilyRenderer.Render(state, scratch, sw, sh, sw * 4, CancellationToken.None);
                    onMs = System.Math.Min(onMs, clock.Elapsed.TotalMilliseconds);
                }
            }
            finally
            {
                MandelbrotFamilyRenderer.ForceBlaForTests = null;
                MandelbrotFamilyRenderer.ForceDeepZoomForTests = null;
            }

            Console.WriteLine($"[diag] real BLA speed {label}: off {offMs:F0}ms, on {onMs:F0}ms, x{offMs / onMs:F2}");
            Check(offMs / onMs >= minimumRatio,
                $"Real BLA speed on {label}: x{offMs / onMs:F2}, expected at least x{minimumRatio:F2}.");
        }
    }

    // Phase 2: the decimal stage is gone from the Mandelbrot/Julia ladder — perturbation
    // now takes over wherever plain double stops being trusted (~1.5e9). Byte-identity for
    // zoom < 1.5e9 and zoom >= 1e25 is proven by the external git-stash A/B; here we check
    // the band that changed hands (was decimal brute-force, now perturbation).
    private static async Task VerifyDecimalStageRemovedAsync(Func<MandelbrotPalette> palette)
    {
        static int RgbDelta(byte[] a, byte[] b, int pixel)
        {
            int o = pixel * 4;
            return Math.Max(Math.Abs(a[o] - b[o]),
                   Math.Max(Math.Abs(a[o + 1] - b[o + 1]), Math.Abs(a[o + 2] - b[o + 2])));
        }

        static int CountDiffering(byte[] a, byte[] b)
        {
            int n = 0;
            for (int pixel = 0; pixel * 4 < a.Length; pixel++)
                if (RgbDelta(a, b, pixel) != 0) n++;
            return n;
        }

        async Task<byte[]> RenderAsync(MandelbrotState state, bool? forceDeep, bool? forceFloatExp, int w, int h)
        {
            byte[] pixels = new byte[w * h * 4];
            MandelbrotFamilyRenderer.ForceDeepZoomForTests = forceDeep;
            MandelbrotFamilyRenderer.ForceFloatExpDeltaForTests = forceFloatExp;
            try
            {
                await Task.Run(() => MandelbrotFamilyRenderer.Render(state, pixels, w, h, w * 4, CancellationToken.None));
            }
            finally
            {
                MandelbrotFamilyRenderer.ForceDeepZoomForTests = null;
                MandelbrotFamilyRenderer.ForceFloatExpDeltaForTests = null;
            }
            return pixels;
        }

        MandelbrotState DeepState(double zoom, int iterations) => new()
        {
            CenterX = -1.2628848671045503000020782246m,
            CenterY = 0.0409687601493310685285376264m,
            Zoom = zoom,
            Iterations = iterations,
            Threads = 2,
            Palette = palette()
        };

        const int w = 112, h = 74, total = w * h;

        // (a) Where decimal was still trustworthy (<= ~1e18), perturbation must reproduce
        //     it up to boundary chaos (a 1-ULP coordinate difference can flip a boundary
        //     pixel by a whole colour band — see the project memory).
        foreach (double zoom in new[] { 1.0e12, 1.0e18 })
        {
            MandelbrotState state = DeepState(zoom, 6000);
            byte[] decimalPixels = await RenderAsync(state, forceDeep: false, forceFloatExp: null, w, h);
            byte[] perturbationPixels = await RenderAsync(state, forceDeep: true, forceFloatExp: null, w, h);
            int differing = CountDiffering(decimalPixels, perturbationPixels);
            Console.WriteLine($"[diag] zoom {zoom:E0}: decimal vs perturbation {differing}/{total} px differ");
            Check(perturbationPixels.Where((_, index) => index % 4 != 3).Any(value => value != 0),
                $"Perturbation must resolve structure at zoom {zoom:E0}.");
            Check(differing * 100 <= total * 5,
                $"decimal→perturbation diverges on {differing}/{total} px at zoom {zoom:E0} (>5%).");
        }

        // (b) Past ~1e20 decimal itself runs out of digits, so it is no longer the oracle.
        //     Instead check the perturbation engine is internally converged: swapping the
        //     δ representation (double ↔ FloatExp) over the same reference orbit must not
        //     move the picture. If it doesn't, the divergence from decimal up here is
        //     decimal's error, not the engine's.
        foreach (double zoom in new[] { 1.0e18, 1.0e23 })
        {
            MandelbrotState state = DeepState(zoom, 6000);
            byte[] doubleDelta = await RenderAsync(state, forceDeep: true, forceFloatExp: false, w, h);
            byte[] floatExpDelta = await RenderAsync(state, forceDeep: true, forceFloatExp: true, w, h);
            int differing = CountDiffering(doubleDelta, floatExpDelta);
            Console.WriteLine($"[diag] zoom {zoom:E0}: perturbation double-δ vs FloatExp-δ {differing}/{total} px differ");
            Check(differing * 200 <= total,
                $"Perturbation not δ-representation-stable at zoom {zoom:E0}: {differing}/{total} px (>0.5%).");
        }

        // (c) Julia perturbation with the rebasing fix (δ = z − Z₀; Julia's Z₀ = centre is
        //     non-zero, and the old code used δ = z, which glitched wherever rebasing
        //     fired). A dendrite view forces heavy rebasing; at this shallow zoom plain
        //     double is an exact oracle, so agreement means the fix is right, not just
        //     "doesn't crash".
        {
            var julia = new MandelbrotState
            {
                Variant = MandelbrotVariant.Julia,
                JuliaCReal = -0.8m,
                JuliaCImaginary = 0.156m,
                CenterX = 0.15m,
                CenterY = 0.30m,
                Zoom = 100.0,
                Iterations = 3000,
                Threads = 2,
                Palette = palette()
            };
            byte[] doublePixels = await RenderAsync(julia, forceDeep: false, forceFloatExp: null, w, h);
            byte[] perturbationPixels = await RenderAsync(julia, forceDeep: true, forceFloatExp: null, w, h);
            int differing = CountDiffering(doublePixels, perturbationPixels);
            int structuredPixels = 0;
            for (int pixel = 0; pixel < total; pixel++)
                if (perturbationPixels[pixel * 4] != perturbationPixels[0] ||
                    perturbationPixels[pixel * 4 + 1] != perturbationPixels[1] ||
                    perturbationPixels[pixel * 4 + 2] != perturbationPixels[2])
                    structuredPixels++;
            Console.WriteLine($"[diag] Julia dendrite zoom 100: double vs perturbation {differing}/{total} px differ; structured {structuredPixels}/{total}");
            Check(structuredPixels > total / 4, "Julia perturbation must be structured, not a flat fill.");
            Check(differing * 100 <= total * 10,
                $"Julia: perturbation strays from the double oracle on {differing}/{total} px (>10%) — rebase fix suspect.");
        }

        // (d) Degenerate reference orbit (centre deep in the fast-escaping exterior): the
        //     fallback must run in double (no decimal), fill the frame and stay uniform.
        var degenerate = new MandelbrotState
        {
            CenterX = 1000m,
            CenterY = 1000m,
            Zoom = 1.0e30,
            Iterations = 500,
            Threads = 2,
            Palette = palette()
        };
        int degenerateProgress = 0;
        byte[] degeneratePixels = new byte[w * h * 4];
        MandelbrotFamilyRenderer.Render(degenerate, degeneratePixels, w, h, w * 4,
            CancellationToken.None, value => degenerateProgress = value);
        Check(degenerateProgress == 100, "Degenerate-orbit fallback must complete with 100% progress.");
        Check(degeneratePixels.Where((_, index) => index % 4 == 3).All(value => value == 255),
            "Degenerate-orbit fallback must fill every pixel.");
        bool uniform = true;
        for (int pixel = 1; pixel < w * h && uniform; pixel++)
            uniform = degeneratePixels[pixel * 4] == degeneratePixels[0]
                   && degeneratePixels[pixel * 4 + 1] == degeneratePixels[1]
                   && degeneratePixels[pixel * 4 + 2] == degeneratePixels[2];
        Check(uniform, "Degenerate-orbit fallback must produce a uniform frame.");
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
