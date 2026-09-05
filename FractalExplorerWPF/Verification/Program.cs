using System.Diagnostics;
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

        await VerifyDecimalStageRemovedAsync(Palette);
        await VerifyBlaAccelerationAsync(Palette);
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
            bool inversion = false, double relief = 1.35) => new()
        {
            ColoringMode = MandelbrotColoringMode.DistanceEstimation,
            Variant = variant,
            CenterX = cx,
            CenterY = cy,
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
        //     images: the reference samples (w+2)×(h+2) pixels and is slow.
        {
            const int w = 40, h = 28, total = w * h;
            (string Label, MandelbrotState State)[] views =
            {
                ("Mandelbrot 1e30",         De(MandelbrotVariant.Mandelbrot, mandelCentre.X, mandelCentre.Y, 1.0e30, 4000, palette())),
                // Кончик антенны: центр −2 представим точно на любой глубине, поэтому вид
                // остаётся осмысленным и на 1e50/1e120 (в отличие от 28-значного центра выше),
                // а орбита выходит за радиус за ~log₄(зум) шагов — эталон считается быстро.
                ("Mandelbrot tip 1e50",     De(MandelbrotVariant.Mandelbrot, -2m, 0m, 1.0e50, 800, palette())),
                ("Mandelbrot tip 1e120",    De(MandelbrotVariant.Mandelbrot, -2m, 0m, 1.0e120, 800, palette())),
                ("BurningShip 1e30",        De(MandelbrotVariant.BurningShip, -1.62m, 0m, 1.0e30, 3000, palette())),
                ("Tricorn 1e10",            De(MandelbrotVariant.Tricorn, -1.62m, 0m, 1.0e10, 2000, palette())),
                ("Buffalo 1e10",            De(MandelbrotVariant.Buffalo, -1.62m, 0m, 1.0e10, 2000, palette())),
                ("Celtic 1e10",             De(MandelbrotVariant.Celtic, -1.62m, 0m, 1.0e10, 2000, palette())),
                ("JuliaBurningShip 1e10",   De(MandelbrotVariant.JuliaBurningShip, 0.5m, -0.3m, 1.0e10, 2000, palette(), jr: -1.5m)),
                ("Multibrot p=3",           De(MandelbrotVariant.Generalized, -0.295455m, 0.977273m, 300.0, 2000, palette(), power: 3m)),
                ("Multibrot p=8",           De(MandelbrotVariant.Generalized, 0.66m, 0m, 300.0, 2000, palette(), power: 8m)),
                ("Simonobrot p=2",          De(MandelbrotVariant.Simonobrot, -0.03m, 0.84m, 300.0, 2000, palette(), power: 2m)),
                ("Simonobrot p=6 inv",      De(MandelbrotVariant.Simonobrot, -0.90m, 0.18m, 300.0, 2000, palette(), power: 6m, inversion: true)),
            };

            foreach ((string label, MandelbrotState state) in views)
            {
                byte[] engine = await RenderAsync(state, forceDeep: true, forceBla: null, w, h);
                byte[] exact = await Task.Run(() =>
                    MandelbrotFamilyRenderer.RenderExactReferenceForTests(state, w, h, CancellationToken.None));
                (int differing, int maxDelta) = Compare(engine, exact);
                int nonBlack = 0;
                for (int i = 0; i < total; i++)
                    if (engine[i * 4] != 0 || engine[i * 4 + 1] != 0 || engine[i * 4 + 2] != 0) nonBlack++;

                // Separates the two error sources: how much of the DE difference is already
                // present in the underlying orbit (Smooth colouring, no derivative at all)?
                state.ColoringMode = MandelbrotColoringMode.Smooth;
                byte[] smoothEngine = await RenderAsync(state, forceDeep: true, forceBla: null, w, h);
                byte[] smoothExact = await Task.Run(() =>
                    MandelbrotFamilyRenderer.RenderExactReferenceForTests(state, w, h, CancellationToken.None));
                state.ColoringMode = MandelbrotColoringMode.DistanceEstimation;
                int smoothDiffering = Compare(smoothEngine, smoothExact).Differing;

                Console.WriteLine($"[diag] DE accuracy {label}: {differing}/{total} px differ " +
                                  $"({100.0 * differing / total:F2}%), maxΔ {maxDelta}, nonblack {nonBlack}, " +
                                  $"orbit-only {smoothDiffering}/{total}");
                Check(nonBlack > total / 10, $"DE view {label} must carry structure.");
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

    // Phase 6: Simonobrot of even integer power p=2q — composition of two exact binomial
    // perturbations (zᵖ and |z|ᵖ=Mᵠ), no BLA (the leading term is a real 2×2 map, not
    // complex). Verified against the exact BigFloat reference, both with and without
    // UseInversion (which flips the sign the reference-orbit cache key must also carry).
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

            int vsExact = CountRgbDiffering(perturbation, exact);
            int maxD = 0, nonBlack = 0;
            for (int i = 0; i < total; i++)
            {
                int o = i * 4;
                maxD = Math.Max(maxD, Math.Max(Math.Abs(perturbation[o] - exact[o]),
                    Math.Max(Math.Abs(perturbation[o + 1] - exact[o + 1]), Math.Abs(perturbation[o + 2] - exact[o + 2]))));
                if (perturbation[o] != 0 || perturbation[o + 1] != 0 || perturbation[o + 2] != 0) nonBlack++;
            }
            Console.WriteLine($"[diag] Simonobrot p={power} inv={inversion}: vs exact {vsExact}/{total} (maxΔ {maxD}), nonblack {nonBlack}");
            Check(nonBlack > total / 10, $"Simonobrot p={power} inv={inversion} view must carry structure.");
            Check(vsExact * 100 <= total * 3,
                $"Simonobrot p={power} inv={inversion}: perturbation diverges from exact on {vsExact}/{total} px, maxΔ {maxD} (>3%).");
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
