using FractalExplorer.Engines;
using FractalExplorer.Resources;
using FractalExplorer.Utilities;
using FractalExplorer.Utilities.SaveIO.ColorPalettes;
using FractalExplorer.Utilities.SaveIO.SaveStateImplementations;
using System;
using System.Collections.Generic;
using System.Drawing;
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

        private void TestingFractalSystemForm_Load(object sender, EventArgs e)
        {
            // Можно добавить инициализацию здесь, если потребуется
        }

        private async void btnRunTests_Click(object sender, EventArgs e)
        {
            btnRunTests.Enabled = false;
            rtbLog.Clear();
            _cts = new CancellationTokenSource();

            var progress = new Progress<TestProgressReport>(report =>
            {
                if (progressBarOverall.Maximum != report.TotalTests)
                {
                    progressBarOverall.Maximum = report.TotalTests;
                }
                progressBarOverall.Value = report.CurrentTestNumber;
                lblOverallProgress.Text = $"Общий прогресс: {report.CurrentTestNumber} / {report.TotalTests}";

                lblCurrentTest.Text = $"Текущий тест: {report.CurrentTestName}";
                progressBarCurrent.Value = report.CurrentTestProgress;

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
                MessageBox.Show("Все тесты успешно завершены!", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (OperationCanceledException)
            {
                rtbLog.AppendText("--- ТЕСТИРОВАНИЕ ОТМЕНЕНО ПОЛЬЗОВАТЕЛЕМ ---");
                MessageBox.Show("Тестирование было отменено.", "Отменено", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                rtbLog.AppendText($"КРИТИЧЕСКАЯ ОШИБКА: {ex.Message}");
                MessageBox.Show($"Во время тестирования произошла ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnRunTests.Enabled = true;
                _cts.Dispose();
            }
        }
    }

    #region Test Runner and Data Structures

    /// <summary>
    /// Отчет о прогрессе выполнения тестов для обновления UI.
    /// </summary>
    public class TestProgressReport
    {
        public int TotalTests { get; set; }
        public int CurrentTestNumber { get; set; }
        public string CurrentTestName { get; set; }
        public int CurrentTestProgress { get; set; }
        public string LogMessage { get; set; }
    }

    /// <summary>
    /// Параметры для одного визуального теста.
    /// </summary>
    public class VisualTestScenario
    {
        public int TestId { get; set; }
        public string FractalType { get; set; }
        public string TestName { get; set; }
        public bool UseSmoothColoring { get; set; }
        public int SsaaFactor { get; set; }
        public FractalSaveStateBase Preset { get; set; }
        public string OverridePaletteName { get; set; } // Для тестов с разными палитрами
    }

    /// <summary>
    /// Основной класс, отвечающий за генерацию и выполнение визуальных тестов.
    /// </summary>
    public class VisualTestRunner
    {
        private const int RENDER_WIDTH = 400;
        private const int RENDER_HEIGHT = 300;
        private const int NUM_THREADS = 4;
        private static readonly string OUTPUT_DIR = Path.Combine(Application.StartupPath, "Test", "ImagesOut");

        private PaletteManager _mandelbrotPaletteManager = new PaletteManager();

        public async Task RunTestsAsync(IProgress<TestProgressReport> progress, CancellationToken token)
        {
            await Task.Run(async () =>
            {
                var scenarios = GenerateTestScenarios();
                int testCounter = 0;

                var report = new TestProgressReport
                {
                    TotalTests = scenarios.Count,
                    LogMessage = $"Начинается выполнение {scenarios.Count} тестов..."
                };
                progress.Report(report);

                foreach (var scenario in scenarios)
                {
                    token.ThrowIfCancellationRequested();
                    testCounter++;

                    report.CurrentTestNumber = testCounter;
                    report.CurrentTestName = scenario.TestName;
                    report.CurrentTestProgress = 0;
                    report.LogMessage = $"[{testCounter}/{scenarios.Count}] Запуск: {scenario.TestName}";
                    progress.Report(report);

                    try
                    {
                        var engine = CreateEngine(scenario);
                        ConfigureEngine(engine, scenario);

                        var renderProgress = new Progress<int>(p =>
                        {
                            report.CurrentTestProgress = p;
                            progress.Report(report);
                        });

                        Bitmap result = await RenderAsync(engine, renderProgress, token, scenario.SsaaFactor);
                        SaveImage(result, scenario);

                        report.LogMessage = $"Успешно: {scenario.TestName}";
                        progress.Report(report);
                    }
                    catch (Exception ex)
                    {
                        report.LogMessage = $"ОШИБКА в тесте '{scenario.TestName}': {ex.Message}";
                        progress.Report(report);
                    }
                }
                report.LogMessage = "Все тесты завершены.";
                progress.Report(report);
            }, token);
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
                    // Для Мандельброта/Жюлиа/Феникса
                    if (preset is MandelbrotFamilySaveState || preset is PhoenixSaveState)
                    {
                        // 1. Без сглаживания, без SSAA
                        scenarios.Add(new VisualTestScenario { TestId = testId++, FractalType = type, TestName = $"{preset.SaveName}_Discrete_NoSSAA", Preset = preset, UseSmoothColoring = false, SsaaFactor = 1 });
                        // 2. Со сглаживанием, без SSAA
                        scenarios.Add(new VisualTestScenario { TestId = testId++, FractalType = type, TestName = $"{preset.SaveName}_Smooth_NoSSAA", Preset = preset, UseSmoothColoring = true, SsaaFactor = 1 });
                        // 3. Без сглаживания, с SSAA 2x
                        scenarios.Add(new VisualTestScenario { TestId = testId++, FractalType = type, TestName = $"{preset.SaveName}_Discrete_SSAA_2x", Preset = preset, UseSmoothColoring = false, SsaaFactor = 2 });
                        // 4. Со сглаживанием, с SSAA 2x
                        scenarios.Add(new VisualTestScenario { TestId = testId++, FractalType = type, TestName = $"{preset.SaveName}_Smooth_SSAA_2x", Preset = preset, UseSmoothColoring = true, SsaaFactor = 2 });

                        // 5. Дополнительные тесты с другими палитрами
                        var palettesToTest = _mandelbrotPaletteManager.Palettes.Where(p => p.Name == "Огонь" || p.Name == "Лёд").ToList();
                        foreach (var palette in palettesToTest)
                        {
                            scenarios.Add(new VisualTestScenario { TestId = testId++, FractalType = type, TestName = $"{preset.SaveName}_Smooth_Palette_{palette.Name}", Preset = preset, UseSmoothColoring = true, SsaaFactor = 1, OverridePaletteName = palette.Name });
                        }
                    }
                    else // Для Ньютона и Серпинского своя логика
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
                mbEngine.Scale = 3.0m / state.Zoom;
                mbEngine.MaxIterations = state.Iterations;
                mbEngine.ThresholdSquared = state.Threshold * state.Threshold;
                mbEngine.UseSmoothColoring = scenario.UseSmoothColoring;

                var paletteName = scenario.OverridePaletteName ?? state.PaletteName;
                var palette = _mandelbrotPaletteManager.Palettes.FirstOrDefault(p => p.Name == paletteName) ?? _mandelbrotPaletteManager.Palettes.First();
                int effectiveIters = palette.AlignWithRenderIterations ? mbEngine.MaxIterations : palette.MaxColorIterations;
                mbEngine.MaxColorIterations = effectiveIters;

                // Это упрощенная версия генерации палитр, для тестов достаточно
                mbEngine.Palette = (i, m, mc) => (i == m) ? Color.Black : palette.Colors.Any() ? palette.Colors[i % palette.Colors.Count] : Color.Gray;
                mbEngine.SmoothPalette = (d) => palette.Colors.Any() ? palette.Colors[(int)d % palette.Colors.Count] : Color.Gray;

                if (engine is JuliaEngine jEngine && state is JuliaFamilySaveState jState)
                {
                    jEngine.C = new ComplexDecimal(jState.CRe, jState.CIm);
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

                phxEngine.Palette = (i, m, mc) => (i == m) ? Color.Black : palette.Colors.Any() ? palette.Colors[i % palette.Colors.Count] : Color.Gray;
                phxEngine.SmoothPalette = (d) => palette.Colors.Any() ? palette.Colors[(int)d % palette.Colors.Count] : Color.Gray;
            }
            else if (engine is FractalNewtonEngine newtonEngine)
            {
                var state = (NewtonSaveState)scenario.Preset;
                newtonEngine.SetFormula(state.Formula, out _);
                newtonEngine.CenterX = (double)state.CenterX;
                newtonEngine.CenterY = (double)state.CenterY;
                newtonEngine.Scale = (double)(4.0m / state.Zoom);
                newtonEngine.MaxIterations = state.Iterations;
                newtonEngine.UseGradient = state.PaletteSnapshot.IsGradient;
                newtonEngine.RootColors = state.PaletteSnapshot.RootColors.ToArray();
                newtonEngine.BackgroundColor = state.PaletteSnapshot.BackgroundColor;
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
            {
                return mbEngine.RenderToBitmapSSAA(RENDER_WIDTH, RENDER_HEIGHT, NUM_THREADS, reportProgress, ssaaFactor, token);
            }
            if (engine is PhoenixEngine phxEngine)
            {
                return phxEngine.RenderToBitmapSSAA(RENDER_WIDTH, RENDER_HEIGHT, NUM_THREADS, reportProgress, ssaaFactor, token);
            }
            if (engine is FractalNewtonEngine newtonEngine)
            {
                return Task.Run(() => newtonEngine.RenderToBitmapSSAA(RENDER_WIDTH, RENDER_HEIGHT, NUM_THREADS, reportProgress, ssaaFactor, token));
            }
            if (engine is FractalSerpinskyEngine serpinskyEngine)
            {
                return Task.Run(() => {
                    var bmp = new Bitmap(RENDER_WIDTH, RENDER_HEIGHT, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    var bmpData = bmp.LockBits(new Rectangle(0, 0, RENDER_WIDTH, RENDER_HEIGHT), System.Drawing.Imaging.ImageLockMode.WriteOnly, bmp.PixelFormat);
                    byte[] buffer = new byte[bmpData.Stride * bmpData.Height];
                    serpinskyEngine.RenderToBuffer(buffer, RENDER_WIDTH, RENDER_HEIGHT, bmpData.Stride, 4, NUM_THREADS, token, reportProgress);
                    System.Runtime.InteropServices.Marshal.Copy(buffer, 0, bmpData.Scan0, buffer.Length);
                    bmp.UnlockBits(bmpData);
                    return bmp;
                });
            }
            throw new NotSupportedException("Неизвестный тип движка для рендеринга.");
        }

        private void SaveImage(Bitmap bmp, VisualTestScenario scenario)
        {
            string fractalDir = Path.Combine(OUTPUT_DIR, SanitizeFileName(scenario.FractalType));
            Directory.CreateDirectory(fractalDir);

            string fileName = $"{scenario.TestId:D3}_{SanitizeFileName(scenario.TestName)}.png";
            string filePath = Path.Combine(fractalDir, fileName);

            bmp.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
            bmp.Dispose();
        }

        private string SanitizeFileName(string name)
        {
            string invalidChars = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            Regex r = new Regex(string.Format("[{0}]", Regex.Escape(invalidChars)));
            return r.Replace(name, "_");
        }
    }
    #endregion
}