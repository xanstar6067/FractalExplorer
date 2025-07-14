using FractalExplorer.Engines;
using FractalExplorer.Resources;
using FractalExplorer.Utilities;
using FractalExplorer.Utilities.SaveIO.ColorPalettes;
using FractalExplorer.Utilities.SaveIO.SaveStateImplementations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FractalExplorer.Forms.Other
{
    public partial class TestingFractalSystemForm : Form
    {
        private VisualTestRunner _testRunner;
        private CancellationTokenSource _cts;

        public TestingFractalSystemForm()
        {
            InitializeComponent();
        }

        private void TestingFractalSystemForm_Load(object sender, EventArgs e) { }

        private async void btnRunTests_Click(object sender, EventArgs e)
        {
            if (btnRunTests.Text == "Отменить")
            {
                _cts?.Cancel();
                btnRunTests.Text = "Отменяется...";
                btnRunTests.Enabled = false;
                return;
            }

            btnRunTests.Text = "Отменить";
            rtbLog.Clear();
            _cts = new CancellationTokenSource();

            var progress = new Progress<TestProgressReport>(report =>
            {
                if (progressBarOverall.Maximum != report.TotalTests)
                {
                    progressBarOverall.Maximum = report.TotalTests;
                }
                progressBarOverall.Value = Math.Min(report.CurrentTestNumber, progressBarOverall.Maximum);
                lblOverallProgress.Text = $"Общий прогресс: {report.CurrentTestNumber} / {report.TotalTests}";
                lblCurrentTest.Text = $"Текущий тест: {report.CurrentTestName}";
                progressBarCurrent.Value = Math.Min(report.CurrentTestProgress, 100);

                if (!string.IsNullOrEmpty(report.LogMessage))
                {
                    rtbLog.AppendText(report.LogMessage + Environment.NewLine);
                    rtbLog.ScrollToCaret();
                }
            });

            _testRunner = new VisualTestRunner();

            try
            {
                await _testRunner.RunTestsAsync(progress, _cts.Token);
                rtbLog.AppendText("\n--- ВСЕ ТЕСТЫ УСПЕШНО ЗАВЕРШЕНЫ ---\n");
                MessageBox.Show("Все тесты успешно завершены!", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (OperationCanceledException)
            {
                rtbLog.AppendText("\n--- ТЕСТИРОВАНИЕ ОТМЕНЕНО ПОЛЬЗОВАТЕЛЕМ ---\n");
                MessageBox.Show("Тестирование было отменено.", "Отменено", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                rtbLog.AppendText($"\n--- КРИТИЧЕСКАЯ ОШИБКА: {ex.GetType().Name} в тесте '{lblCurrentTest.Text}' - {ex.Message} ---\n");
                MessageBox.Show($"Во время тестирования произошла ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                rtbLog.AppendText("...Завершение работы...\n");
                btnRunTests.Text = "Запустить тесты";
                btnRunTests.Enabled = true;
                _cts?.Dispose();
            }
        }
    }

    #region Test Runner and Data Structures

    public class TestProgressReport
    {
        public int TotalTests { get; set; }
        public int CurrentTestNumber { get; set; }
        public string CurrentTestName { get; set; }
        public int CurrentTestProgress { get; set; }
        public string LogMessage { get; set; }
    }

    public class VisualTestScenario
    {
        public int TestId { get; set; }
        public string FractalType { get; set; }
        public string TestName { get; set; }
        public bool UseSmoothColoring { get; set; }
        public int SsaaFactor { get; set; }
        public FractalSaveStateBase Preset { get; set; }
        public string OverridePaletteName { get; set; }
    }

    public class VisualTestRunner
    {
        private const int RENDER_WIDTH = 1280;
        private const int RENDER_HEIGHT = 720;
        private const int NUM_THREADS = 4;
        private static readonly string OUTPUT_DIR = Path.Combine(Application.StartupPath, "Test", "ImagesOut");

        private readonly PaletteManager _mandelbrotPaletteManager = new PaletteManager();

        public async Task RunTestsAsync(IProgress<TestProgressReport> progress, CancellationToken token)
        {
            await Task.Run(async () =>
            {
                var scenarios = GenerateTestScenarios();
                var report = new TestProgressReport
                {
                    TotalTests = scenarios.Count,
                    LogMessage = $"Сгенерировано {scenarios.Count} тестовых сценариев..."
                };
                progress.Report(report);

                for (int i = 0; i < scenarios.Count; i++)
                {
                    var scenario = scenarios[i];
                    token.ThrowIfCancellationRequested();

                    report.CurrentTestNumber = i + 1;
                    report.CurrentTestName = scenario.TestName;
                    report.CurrentTestProgress = 0;
                    report.LogMessage = $"[{i + 1}/{scenarios.Count}] Запуск: {scenario.TestName}";
                    progress.Report(report);

                    try
                    {
                        var engine = CreateEngine(scenario);
                        ConfigureEngine(engine, scenario);

                        var stopwatch = Stopwatch.StartNew();
                        long lastReportTime = 0;
                        var renderProgress = new Progress<int>(p =>
                        {
                            if (stopwatch.ElapsedMilliseconds > lastReportTime + 50)
                            {
                                report.CurrentTestProgress = p;
                                progress.Report(report);
                                lastReportTime = stopwatch.ElapsedMilliseconds;
                            }
                        });

                        Bitmap result = await RenderAsync(engine, renderProgress, token, scenario.SsaaFactor).ConfigureAwait(false);
                        SaveImage(result, scenario);

                        report.CurrentTestProgress = 100;
                        report.LogMessage = $"Успешно: {scenario.TestName} (за {stopwatch.Elapsed.TotalSeconds:F2} с)";
                        progress.Report(report);
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex)
                    {
                        report.LogMessage = $"ОШИБКА в тесте '{scenario.TestName}': {ex.Message}";
                        progress.Report(report);
                    }
                }
                await Task.Delay(100, token);
            }, token).ConfigureAwait(false);
        }

        private List<VisualTestScenario> GenerateTestScenarios()
        {
            var scenarios = new List<VisualTestScenario>();
            int testId = 1;
            var fractalTypes = new[] { "Mandelbrot", "Julia", "MandelbrotBurningShip", "JuliaBurningShip", "Phoenix", "NewtonPools", "Serpinsky" };

            foreach (var type in fractalTypes)
            {
                var presets = PresetManager.GetPresetsFor(type);
                foreach (var preset in presets)
                {
                    if (preset is MandelbrotFamilySaveState || preset is PhoenixSaveState)
                    {
                        scenarios.Add(new VisualTestScenario { TestId = testId++, FractalType = type, TestName = $"{preset.SaveName}_Discrete_NoSSAA", Preset = preset, UseSmoothColoring = false, SsaaFactor = 1 });
                        scenarios.Add(new VisualTestScenario { TestId = testId++, FractalType = type, TestName = $"{preset.SaveName}_Smooth_NoSSAA", Preset = preset, UseSmoothColoring = true, SsaaFactor = 1 });
                        scenarios.Add(new VisualTestScenario { TestId = testId++, FractalType = type, TestName = $"{preset.SaveName}_Smooth_SSAA_2x", Preset = preset, UseSmoothColoring = true, SsaaFactor = 2 });

                        var palettesToTest = _mandelbrotPaletteManager.Palettes.Where(p => p.Name == "Огонь и лед" || p.Name == "Психоделика").ToList();
                        foreach (var palette in palettesToTest)
                        {
                            scenarios.Add(new VisualTestScenario { TestId = testId++, FractalType = type, TestName = $"{preset.SaveName}_Smooth_Palette_{palette.Name}", Preset = preset, UseSmoothColoring = true, SsaaFactor = 1, OverridePaletteName = palette.Name });
                        }
                    }
                    else
                    {
                        scenarios.Add(new VisualTestScenario { TestId = testId++, FractalType = type, TestName = $"{preset.SaveName}_Default", Preset = preset, SsaaFactor = 1 });
                        scenarios.Add(new VisualTestScenario { TestId = testId++, FractalType = type, TestName = $"{preset.SaveName}_SSAA_2x", Preset = preset, SsaaFactor = 2 });
                    }
                }
            }
            return scenarios;
        }

        private object CreateEngine(VisualTestScenario scenario)
        {
            switch (scenario.FractalType)
            {
                case "Mandelbrot": return new MandelbrotEngine();
                case "Julia": return new JuliaEngine();
                case "MandelbrotBurningShip": return new MandelbrotBurningShipEngine();
                case "JuliaBurningShip": return new JuliaBurningShipEngine();
                case "Phoenix": return new PhoenixEngine();
                case "NewtonPools": return new FractalNewtonEngine();
                case "Serpinsky": return new FractalSerpinskyEngine();
                default: throw new NotSupportedException($"Неизвестный тип фрактала: {scenario.FractalType}");
            }
        }

        private void ConfigureEngine(object engine, VisualTestScenario scenario)
        {
            if (engine is FractalMandelbrotFamilyEngine mbEngine)
            {
                var state = (MandelbrotFamilySaveState)scenario.Preset;
                mbEngine.CenterX = state.CenterX;
                mbEngine.CenterY = state.CenterY;
                mbEngine.Scale = (scenario.FractalType.Contains("BurningShip") ? 4.0m : 3.0m) / state.Zoom;
                mbEngine.MaxIterations = state.Iterations;
                mbEngine.ThresholdSquared = state.Threshold * state.Threshold;
                mbEngine.UseSmoothColoring = scenario.UseSmoothColoring;

                var paletteName = scenario.OverridePaletteName ?? state.PaletteName;
                var palette = _mandelbrotPaletteManager.Palettes.FirstOrDefault(p => p.Name == paletteName) ?? _mandelbrotPaletteManager.Palettes.First();
                int effectiveIters = palette.AlignWithRenderIterations ? mbEngine.MaxIterations : palette.MaxColorIterations;
                mbEngine.MaxColorIterations = effectiveIters;

                mbEngine.Palette = PaletteGenerator.CreateDiscrete(palette);
                mbEngine.SmoothPalette = PaletteGenerator.CreateSmooth(palette, effectiveIters, mbEngine.MaxIterations);

                if (engine is JuliaEngine jEngine && state is JuliaFamilySaveState jState)
                {
                    jEngine.C = new ComplexDecimal(jState.CRe, jState.CIm);
                }
                else if (engine is JuliaBurningShipEngine jbsEngine && state is JuliaFamilySaveState jbsState)
                {
                    jbsEngine.C = new ComplexDecimal(jbsState.CRe, jbsState.CIm);
                }
            }
            else if (engine is PhoenixEngine phxEngine)
            {
                var state = (PhoenixSaveState)scenario.Preset;
                phxEngine.CenterX = state.CenterX;
                phxEngine.CenterY = state.CenterY;
                phxEngine.Scale = 4.0m / state.Zoom;
                phxEngine.MaxIterations = state.Iterations;
                phxEngine.ThresholdSquared = state.Threshold * state.Threshold;
                phxEngine.C1 = new ComplexDecimal(state.C1Re, state.C1Im);
                phxEngine.C2 = new ComplexDecimal(state.C2Re, state.C2Im);
                phxEngine.UseSmoothColoring = scenario.UseSmoothColoring;

                var paletteName = scenario.OverridePaletteName ?? state.PaletteName;
                var palette = _mandelbrotPaletteManager.Palettes.FirstOrDefault(p => p.Name == paletteName) ?? _mandelbrotPaletteManager.Palettes.First();
                int effectiveIters = palette.AlignWithRenderIterations ? phxEngine.MaxIterations : palette.MaxColorIterations;
                phxEngine.MaxColorIterations = effectiveIters;

                phxEngine.Palette = PaletteGenerator.CreateDiscrete(palette);
                phxEngine.SmoothPalette = PaletteGenerator.CreateSmooth(palette, effectiveIters, phxEngine.MaxIterations);
            }
            else if (engine is FractalNewtonEngine newtonEngine)
            {
                var state = (NewtonSaveState)scenario.Preset;
                if (!newtonEngine.SetFormula(state.Formula, out string debugInfo))
                    throw new InvalidOperationException($"Ошибка парсинга формулы '{state.Formula}': {debugInfo}");

                newtonEngine.CenterX = (double)state.CenterX;
                newtonEngine.CenterY = (double)state.CenterY;
                newtonEngine.Scale = 3.0 / (double)state.Zoom;
                newtonEngine.MaxIterations = state.Iterations;

                var paletteSnapshot = state.PaletteSnapshot;
                newtonEngine.UseGradient = paletteSnapshot.IsGradient;
                newtonEngine.BackgroundColor = paletteSnapshot.BackgroundColor;
                newtonEngine.RootColors = (paletteSnapshot.RootColors?.Any() == true)
                    ? paletteSnapshot.RootColors.ToArray()
                    : ColorConfigurationNewtonPoolsForm.GenerateHarmonicColors(newtonEngine.Roots.Count).ToArray();
            }
            else if (engine is FractalSerpinskyEngine serpinskyEngine)
            {
                var state = (SerpinskySaveState)scenario.Preset;
                serpinskyEngine.RenderMode = state.RenderMode;
                serpinskyEngine.Iterations = state.Iterations;
                serpinskyEngine.Zoom = (double)state.Zoom;
                serpinskyEngine.CenterX = (double)state.CenterX;
                serpinskyEngine.CenterY = (double)state.CenterY;
                serpinskyEngine.ColorMode = SerpinskyColorMode.CustomColor;
                serpinskyEngine.FractalColor = state.FractalColor;
                serpinskyEngine.BackgroundColor = state.BackgroundColor;
            }
        }

        private Task<Bitmap> RenderAsync(object engine, IProgress<int> progress, CancellationToken token, int ssaaFactor)
        {
            Action<int> reportProgress = p => ((IProgress<int>)progress).Report(p);

            if (engine is FractalMandelbrotFamilyEngine mbEngine)
                return mbEngine.RenderToBitmapSSAA(RENDER_WIDTH, RENDER_HEIGHT, NUM_THREADS, reportProgress, ssaaFactor, token);

            if (engine is PhoenixEngine phxEngine)
                return phxEngine.RenderToBitmapSSAA(RENDER_WIDTH, RENDER_HEIGHT, NUM_THREADS, reportProgress, ssaaFactor, token);

            if (engine is FractalNewtonEngine newtonEngine)
                return Task.Run(() => newtonEngine.RenderToBitmapSSAA(RENDER_WIDTH, RENDER_HEIGHT, NUM_THREADS, reportProgress, ssaaFactor, token), token);

            if (engine is FractalSerpinskyEngine serpinskyEngine)
            {
                return Task.Run(() =>
                {
                    var bmp = new Bitmap(RENDER_WIDTH, RENDER_HEIGHT, PixelFormat.Format32bppArgb);
                    var bmpData = bmp.LockBits(new Rectangle(0, 0, RENDER_WIDTH, RENDER_HEIGHT), ImageLockMode.WriteOnly, bmp.PixelFormat);
                    byte[] buffer = new byte[bmpData.Stride * bmpData.Height];
                    serpinskyEngine.RenderToBuffer(buffer, RENDER_WIDTH, RENDER_HEIGHT, bmpData.Stride, 4, NUM_THREADS, token, reportProgress);
                    System.Runtime.InteropServices.Marshal.Copy(buffer, 0, bmpData.Scan0, buffer.Length);
                    bmp.UnlockBits(bmpData);
                    return bmp;
                }, token);
            }
            throw new NotSupportedException("Неизвестный тип движка для рендеринга.");
        }

        private void SaveImage(Bitmap bmp, VisualTestScenario scenario)
        {
            string fractalDir = Path.Combine(OUTPUT_DIR, SanitizeFileName(scenario.FractalType));
            Directory.CreateDirectory(fractalDir);

            string fileName = $"{scenario.TestId:D3}_{SanitizeFileName(scenario.TestName)}.png";
            string filePath = Path.Combine(fractalDir, fileName);

            bmp.Save(filePath, ImageFormat.Png);
            bmp.Dispose();
        }

        private string SanitizeFileName(string name)
        {
            string invalidChars = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            Regex r = new Regex($"[{Regex.Escape(invalidChars)}]");
            return r.Replace(name, "_");
        }
    }
    #endregion
}