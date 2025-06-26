using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.Json;
using FractalDraving;
using FractalExplorer;
using FractalExplorer.Engines;
using FractalExplorer.Forms;
using FractalExplorer.Utilities.SaveIO.SaveStateImplementations;

namespace FractalExplorer.Utilities
{
    public static class PresetManager
    {
        #region Constants

        private const int PREVIEW_ITERATION_LIMIT_MANDELBROT_FAMILY = 175;
        private const int PREVIEW_ITERATION_LIMIT_PHOENIX = 175;
        private const int PREVIEW_ITERATION_LIMIT_NEWTON_CHAOS = 50;
        private const int PREVIEW_ITERATION_LIMIT_SERPINSKY_GEOMETRIC = 5;
        private const int PREVIEW_ITERATION_LIMIT_SERPINSKY_CHAOS = 20000;

        #endregion

        #region Public API / Dispatcher

        /// <summary>
        /// Возвращает список предустановленных точек интереса (пресетов) для указанного типа фрактала.
        /// </summary>
        /// <param name="fractalTypeIdentifier">Идентификатор типа фрактала (например, "Mandelbrot", "Julia").</param>
        /// <returns>Список пресетов или пустой список, если для данного типа пресетов нет.</returns>
        public static List<FractalSaveStateBase> GetPresetsFor(string fractalTypeIdentifier)
        {
            switch (fractalTypeIdentifier)
            {
                case "Mandelbrot":
                    return GetMandelbrotPresets();
                case "Julia":
                    return GetJuliaPresets();
                case "MandelbrotBurningShip":
                    return GetMandelbrotBurningShipPresets();
                case "JuliaBurningShip":
                    return GetJuliaBurningShipPresets();
                case "Phoenix":
                    return GetPhoenixPresets();
                case "Serpinsky":
                    return GetSerpinskyPresets();
                case "NewtonPools":
                    return GetNewtonPoolsPresets();
                default:
                    return new List<FractalSaveStateBase>();
            }
        }

        #endregion

        #region Mandelbrot-Julia Family Presets

        private static List<FractalSaveStateBase> GetMandelbrotPresets()
        {
            var presets = new List<FractalSaveStateBase>();

            // Пресет 1: Долина Морских Коньков
            var preset1 = new MandelbrotFamilySaveState("Mandelbrot")
            {
                SaveName = "Долина Морских Коньков",
                CenterX = -0.743643887037151m,
                CenterY = 0.13182590420533m,
                Zoom = 11500m,
                Iterations = 1000,
                Threshold = 2.0m,
                PaletteName = "Лёд",
                Timestamp = DateTime.MinValue
            };
            var previewParams1 = new FractalMandelbrotFamilyForm.PreviewParams
            {
                CenterX = preset1.CenterX,
                CenterY = preset1.CenterY,
                Zoom = preset1.Zoom,
                Iterations = Math.Min(preset1.Iterations, PREVIEW_ITERATION_LIMIT_MANDELBROT_FAMILY),
                PaletteName = preset1.PaletteName,
                Threshold = preset1.Threshold,
                PreviewEngineType = "Mandelbrot"
            };
            preset1.PreviewParametersJson = JsonSerializer.Serialize(previewParams1);
            presets.Add(preset1);

            // Пресет 2: Шип Миниброта
            var preset2 = new MandelbrotFamilySaveState("Mandelbrot")
            {
                SaveName = "Шип Миниброта",
                CenterX = -1.74995m,
                CenterY = 0.0m,
                Zoom = 4000m,
                Iterations = 500,
                Threshold = 2.0m,
                PaletteName = "Огонь",
                Timestamp = DateTime.MinValue
            };
            var previewParams2 = new FractalMandelbrotFamilyForm.PreviewParams
            {
                CenterX = preset2.CenterX,
                CenterY = preset2.CenterY,
                Zoom = preset2.Zoom,
                Iterations = Math.Min(preset2.Iterations, PREVIEW_ITERATION_LIMIT_MANDELBROT_FAMILY),
                PaletteName = preset2.PaletteName,
                Threshold = preset2.Threshold,
                PreviewEngineType = "Mandelbrot"
            };
            preset2.PreviewParametersJson = JsonSerializer.Serialize(previewParams2);
            presets.Add(preset2);

            // NEW PRESET: Электрическая Розетка
            var preset3 = new MandelbrotFamilySaveState("Mandelbrot")
            {
                SaveName = "Электрическая Розетка",
                CenterX = 0.27488735305156m,
                CenterY = 0.00494881779930m,
                Zoom = 6500.0m,
                Iterations = 800,
                Threshold = 2.0m,
                PaletteName = "Радуга",
                Timestamp = DateTime.MinValue
            };
            var previewParams3 = new FractalMandelbrotFamilyForm.PreviewParams
            {
                CenterX = preset3.CenterX,
                CenterY = preset3.CenterY,
                Zoom = preset3.Zoom,
                Iterations = Math.Min(preset3.Iterations, PREVIEW_ITERATION_LIMIT_MANDELBROT_FAMILY),
                PaletteName = preset3.PaletteName,
                Threshold = preset3.Threshold,
                PreviewEngineType = "Mandelbrot"
            };
            preset3.PreviewParametersJson = JsonSerializer.Serialize(previewParams3);
            presets.Add(preset3);

            // NEW PRESET: Спиральная Галактика
            var preset4 = new MandelbrotFamilySaveState("Mandelbrot")
            {
                SaveName = "Спиральная Галактика",
                CenterX = -0.16070135m,
                CenterY = 1.0375665m,
                Zoom = 3000.0m,
                Iterations = 600,
                Threshold = 2.0m,
                PaletteName = "Глубина",
                Timestamp = DateTime.MinValue
            };
            var previewParams4 = new FractalMandelbrotFamilyForm.PreviewParams
            {
                CenterX = preset4.CenterX,
                CenterY = preset4.CenterY,
                Zoom = preset4.Zoom,
                Iterations = Math.Min(preset4.Iterations, PREVIEW_ITERATION_LIMIT_MANDELBROT_FAMILY),
                PaletteName = preset4.PaletteName,
                Threshold = preset4.Threshold,
                PreviewEngineType = "Mandelbrot"
            };
            preset4.PreviewParametersJson = JsonSerializer.Serialize(previewParams4);
            presets.Add(preset4);

            return presets;
        }

        private static List<FractalSaveStateBase> GetJuliaPresets()
        {
            var presets = new List<FractalSaveStateBase>();

            var preset1 = new JuliaFamilySaveState("Julia")
            {
                SaveName = "Классическая Спираль",
                CRe = -0.8m,
                CIm = 0.156m,
                CenterX = 0m,
                CenterY = 0m,
                Zoom = 1.0m,
                Iterations = 500,
                Threshold = 2.0m,
                PaletteName = "Психоделика",
                Timestamp = DateTime.MinValue
            };
            var previewParams1J = new FractalMandelbrotFamilyForm.PreviewParams
            {
                CenterX = preset1.CenterX,
                CenterY = preset1.CenterY,
                Zoom = preset1.Zoom,
                Iterations = Math.Min(preset1.Iterations, PREVIEW_ITERATION_LIMIT_MANDELBROT_FAMILY),
                PaletteName = preset1.PaletteName,
                Threshold = preset1.Threshold,
                CRe = preset1.CRe,
                CIm = preset1.CIm,
                PreviewEngineType = "Julia"
            };
            preset1.PreviewParametersJson = JsonSerializer.Serialize(previewParams1J);
            presets.Add(preset1);

            var preset2 = new JuliaFamilySaveState("Julia")
            {
                SaveName = "Дендрит",
                CRe = 0m,
                CIm = 1.0m,
                CenterX = 0m,
                CenterY = 0m,
                Zoom = 1.0m,
                Iterations = 300,
                Threshold = 2.0m,
                PaletteName = "Классика",
                Timestamp = DateTime.MinValue
            };
            var previewParams2J = new FractalMandelbrotFamilyForm.PreviewParams
            {
                CenterX = preset2.CenterX,
                CenterY = preset2.CenterY,
                Zoom = preset2.Zoom,
                Iterations = Math.Min(preset2.Iterations, PREVIEW_ITERATION_LIMIT_MANDELBROT_FAMILY),
                PaletteName = preset2.PaletteName,
                Threshold = preset2.Threshold,
                CRe = preset2.CRe,
                CIm = preset2.CIm,
                PreviewEngineType = "Julia"
            };
            preset2.PreviewParametersJson = JsonSerializer.Serialize(previewParams2J);
            presets.Add(preset2);

            var preset3 = new JuliaFamilySaveState("Julia")
            {
                SaveName = "Снежинка",
                CRe = -0.70176m,
                CIm = -0.3842m,
                CenterX = 0m,
                CenterY = 0m,
                Zoom = 1.0m,
                Iterations = 400,
                Threshold = 2.0m,
                PaletteName = "Лёд",
                Timestamp = DateTime.MinValue
            };
            var previewParams3J = new FractalMandelbrotFamilyForm.PreviewParams
            {
                CenterX = preset3.CenterX,
                CenterY = preset3.CenterY,
                Zoom = preset3.Zoom,
                Iterations = Math.Min(preset3.Iterations, PREVIEW_ITERATION_LIMIT_MANDELBROT_FAMILY),
                PaletteName = preset3.PaletteName,
                Threshold = preset3.Threshold,
                CRe = preset3.CRe,
                CIm = preset3.CIm,
                PreviewEngineType = "Julia"
            };
            preset3.PreviewParametersJson = JsonSerializer.Serialize(previewParams3J);
            presets.Add(preset3);

            var preset4 = new JuliaFamilySaveState("Julia")
            {
                SaveName = "Огненный Вихрь",
                CRe = 0.285m,
                CIm = 0.01m,
                CenterX = 0m,
                CenterY = 0m,
                Zoom = 1.0m,
                Iterations = 350,
                Threshold = 2.0m,
                PaletteName = "Огонь",
                Timestamp = DateTime.MinValue
            };
            var previewParams4J = new FractalMandelbrotFamilyForm.PreviewParams
            {
                CenterX = preset4.CenterX,
                CenterY = preset4.CenterY,
                Zoom = preset4.Zoom,
                Iterations = Math.Min(preset4.Iterations, PREVIEW_ITERATION_LIMIT_MANDELBROT_FAMILY),
                PaletteName = preset4.PaletteName,
                Threshold = preset4.Threshold,
                CRe = preset4.CRe,
                CIm = preset4.CIm,
                PreviewEngineType = "Julia"
            };
            preset4.PreviewParametersJson = JsonSerializer.Serialize(previewParams4J);
            presets.Add(preset4);

            return presets;
        }

        private static List<FractalSaveStateBase> GetMandelbrotBurningShipPresets()
        {
            var presets = new List<FractalSaveStateBase>();
            var preset1 = new MandelbrotFamilySaveState("MandelbrotBurningShip")
            {
                SaveName = "Центральный Корабль",
                CenterX = -0.5m,
                CenterY = -0.5m,
                Zoom = 0.8m,
                Iterations = 300,
                Threshold = 2.0m,
                PaletteName = "Огонь",
                Timestamp = DateTime.MinValue
            };
            var previewParams1BS = new FractalMandelbrotFamilyForm.PreviewParams
            {
                CenterX = preset1.CenterX,
                CenterY = preset1.CenterY,
                Zoom = preset1.Zoom,
                Iterations = Math.Min(preset1.Iterations, PREVIEW_ITERATION_LIMIT_MANDELBROT_FAMILY),
                PaletteName = preset1.PaletteName,
                Threshold = preset1.Threshold,
                PreviewEngineType = "MandelbrotBurningShip"
            };
            preset1.PreviewParametersJson = JsonSerializer.Serialize(previewParams1BS);
            presets.Add(preset1);

            var preset2 = new MandelbrotFamilySaveState("MandelbrotBurningShip")
            {
                SaveName = "Глубоководный Корабль",
                CenterX = -1.768739999999000m,
                CenterY = -0.001640000000000m,
                Zoom = 1000.0m,
                Iterations = 500,
                Threshold = 2.0m,
                PaletteName = "Глубина",
                Timestamp = DateTime.MinValue
            };
            var previewParams2BS = new FractalMandelbrotFamilyForm.PreviewParams
            {
                CenterX = preset2.CenterX,
                CenterY = preset2.CenterY,
                Zoom = preset2.Zoom,
                Iterations = Math.Min(preset2.Iterations, PREVIEW_ITERATION_LIMIT_MANDELBROT_FAMILY),
                PaletteName = preset2.PaletteName,
                Threshold = preset2.Threshold,
                PreviewEngineType = "MandelbrotBurningShip"
            };
            preset2.PreviewParametersJson = JsonSerializer.Serialize(previewParams2BS);
            presets.Add(preset2);

            var preset3 = new MandelbrotFamilySaveState("MandelbrotBurningShip")
            {
                SaveName = "Призрачные Паруса",
                CenterX = 0.352m,
                CenterY = -1.71m,
                Zoom = 70.0m,
                Iterations = 400,
                Threshold = 2.0m,
                PaletteName = "Лёд",
                Timestamp = DateTime.MinValue
            };
            var previewParams3BS = new FractalMandelbrotFamilyForm.PreviewParams
            {
                CenterX = preset3.CenterX,
                CenterY = preset3.CenterY,
                Zoom = preset3.Zoom,
                Iterations = Math.Min(preset3.Iterations, PREVIEW_ITERATION_LIMIT_MANDELBROT_FAMILY),
                PaletteName = preset3.PaletteName,
                Threshold = preset3.Threshold,
                PreviewEngineType = "MandelbrotBurningShip"
            };
            preset3.PreviewParametersJson = JsonSerializer.Serialize(previewParams3BS);
            presets.Add(preset3);

            return presets;
        }

        private static List<FractalSaveStateBase> GetJuliaBurningShipPresets()
        {
            var presets = new List<FractalSaveStateBase>();
            var preset1 = new JuliaFamilySaveState("JuliaBurningShip")
            {
                SaveName = "Космический Цветок",
                CRe = -1.7551867961883m,
                CIm = 0.01068m,
                CenterX = 0m,
                CenterY = 0m,
                Zoom = 1.0m,
                Iterations = 500,
                Threshold = 2.0m,
                PaletteName = "Лёд",
                Timestamp = DateTime.MinValue
            };
            var previewParams1JBS = new FractalMandelbrotFamilyForm.PreviewParams
            {
                CenterX = preset1.CenterX,
                CenterY = preset1.CenterY,
                Zoom = preset1.Zoom,
                Iterations = Math.Min(preset1.Iterations, PREVIEW_ITERATION_LIMIT_MANDELBROT_FAMILY),
                PaletteName = preset1.PaletteName,
                Threshold = preset1.Threshold,
                CRe = preset1.CRe,
                CIm = preset1.CIm,
                PreviewEngineType = "JuliaBurningShip"
            };
            preset1.PreviewParametersJson = JsonSerializer.Serialize(previewParams1JBS);
            presets.Add(preset1);

            var preset2 = new JuliaFamilySaveState("JuliaBurningShip")
            {
                SaveName = "Огненный Дракон",
                CRe = -0.4m,
                CIm = -1.6m,
                CenterX = 0m,
                CenterY = 0m,
                Zoom = 1.0m,
                Iterations = 350,
                Threshold = 2.0m,
                PaletteName = "Огонь",
                Timestamp = DateTime.MinValue
            };
            var previewParams2JBS = new FractalMandelbrotFamilyForm.PreviewParams
            {
                CenterX = preset2.CenterX,
                CenterY = preset2.CenterY,
                Zoom = preset2.Zoom,
                Iterations = Math.Min(preset2.Iterations, PREVIEW_ITERATION_LIMIT_MANDELBROT_FAMILY),
                PaletteName = preset2.PaletteName,
                Threshold = preset2.Threshold,
                CRe = preset2.CRe,
                CIm = preset2.CIm,
                PreviewEngineType = "JuliaBurningShip"
            };
            preset2.PreviewParametersJson = JsonSerializer.Serialize(previewParams2JBS);
            presets.Add(preset2);

            var preset3 = new JuliaFamilySaveState("JuliaBurningShip")
            {
                SaveName = "Туманность Андромеды (стил.)",
                CRe = -1.1m,
                CIm = -0.23m,
                CenterX = 0m,
                CenterY = 0m,
                Zoom = 1.0m,
                Iterations = 400,
                Threshold = 2.0m,
                PaletteName = "Глубина",
                Timestamp = DateTime.MinValue
            };
            var previewParams3JBS = new FractalMandelbrotFamilyForm.PreviewParams
            {
                CenterX = preset3.CenterX,
                CenterY = preset3.CenterY,
                Zoom = preset3.Zoom,
                Iterations = Math.Min(preset3.Iterations, PREVIEW_ITERATION_LIMIT_MANDELBROT_FAMILY),
                PaletteName = preset3.PaletteName,
                Threshold = preset3.Threshold,
                CRe = preset3.CRe,
                CIm = preset3.CIm,
                PreviewEngineType = "JuliaBurningShip"
            };
            preset3.PreviewParametersJson = JsonSerializer.Serialize(previewParams3JBS);
            presets.Add(preset3);

            return presets;
        }

        #endregion

        #region Phoenix Family Presets

        private static List<FractalSaveStateBase> GetPhoenixPresets()
        {
            var presets = new List<FractalSaveStateBase>();

            var preset1 = new PhoenixSaveState("Phoenix")
            {
                SaveName = "Стандартный Феникс",
                C1Re = 0.56m,
                C1Im = -0.5m,
                C2Re = 0m,
                C2Im = 0m,
                CenterX = 0m,
                CenterY = 0m,
                Zoom = 1m,
                Iterations = 200,
                Threshold = 4.0m,
                PaletteName = "Радуга",
                Timestamp = DateTime.MinValue
            };
            var previewParams1Ph = new FractalPhoenixForm.PhoenixPreviewParams
            {
                CenterX = preset1.CenterX,
                CenterY = preset1.CenterY,
                Zoom = preset1.Zoom,
                Iterations = Math.Min(preset1.Iterations, PREVIEW_ITERATION_LIMIT_PHOENIX),
                PaletteName = preset1.PaletteName,
                Threshold = preset1.Threshold,
                C1Re = preset1.C1Re,
                C1Im = preset1.C1Im,
                C2Re = preset1.C2Re,
                C2Im = preset1.C2Im
            };
            preset1.PreviewParametersJson = JsonSerializer.Serialize(previewParams1Ph);
            presets.Add(preset1);

            var preset2 = new PhoenixSaveState("Phoenix")
            {
                SaveName = "Вихрь Феникса",
                C1Re = 0.35m,
                C1Im = -0.62m,
                C2Re = -0.01m,
                C2Im = 0.005m,
                CenterX = 0.1m,
                CenterY = -0.2m,
                Zoom = 1.2m,
                Iterations = 250,
                Threshold = 4.0m,
                PaletteName = "Психоделика",
                Timestamp = DateTime.MinValue
            };
            var previewParams2Ph = new FractalPhoenixForm.PhoenixPreviewParams
            {
                CenterX = preset2.CenterX,
                CenterY = preset2.CenterY,
                Zoom = preset2.Zoom,
                Iterations = Math.Min(preset2.Iterations, PREVIEW_ITERATION_LIMIT_PHOENIX),
                PaletteName = preset2.PaletteName,
                Threshold = preset2.Threshold,
                C1Re = preset2.C1Re,
                C1Im = preset2.C1Im,
                C2Re = preset2.C2Re,
                C2Im = preset2.C2Im
            };
            preset2.PreviewParametersJson = JsonSerializer.Serialize(previewParams2Ph);
            presets.Add(preset2);

            var preset3 = new PhoenixSaveState("Phoenix")
            {
                SaveName = "Хвост Павлина",
                C1Re = 0.56667m,
                C1Im = -0.5m,
                C2Re = 0.001m,
                C2Im = 0.001m,
                CenterX = 0m,
                CenterY = 0m,
                Zoom = 0.8m,
                Iterations = 300,
                Threshold = 4.0m,
                PaletteName = "Лёд",
                Timestamp = DateTime.MinValue
            };
            var previewParams3Ph = new FractalPhoenixForm.PhoenixPreviewParams
            {
                CenterX = preset3.CenterX,
                CenterY = preset3.CenterY,
                Zoom = preset3.Zoom,
                Iterations = Math.Min(preset3.Iterations, PREVIEW_ITERATION_LIMIT_PHOENIX),
                PaletteName = preset3.PaletteName,
                Threshold = preset3.Threshold,
                C1Re = preset3.C1Re,
                C1Im = preset3.C1Im,
                C2Re = preset3.C2Re,
                C2Im = preset3.C2Im
            };
            preset3.PreviewParametersJson = JsonSerializer.Serialize(previewParams3Ph);
            presets.Add(preset3);

            return presets;
        }

        #endregion

        #region Serpinsky Fractal Presets

        private static List<FractalSaveStateBase> GetSerpinskyPresets()
        {
            var presets = new List<FractalSaveStateBase>();
            var jsonOptions = new JsonSerializerOptions();
            jsonOptions.Converters.Add(new JsonConverters.JsonColorConverter()); // Для сериализации Color

            var preset1 = new SerpinskySaveState("Serpinsky")
            {
                SaveName = "Классическая Геометрия",
                RenderMode = SerpinskyRenderMode.Geometric,
                Iterations = 8,
                Zoom = 1.0,
                CenterX = 0,
                CenterY = 0.1,
                FractalColor = Color.Black,
                BackgroundColor = Color.White,
                Timestamp = DateTime.MinValue
            };
            var previewParams1S = new FractalSerpinsky.SerpinskyPreviewParams
            {
                RenderMode = preset1.RenderMode,
                Iterations = Math.Min(preset1.Iterations, PREVIEW_ITERATION_LIMIT_SERPINSKY_GEOMETRIC),
                Zoom = preset1.Zoom,
                CenterX = preset1.CenterX,
                CenterY = preset1.CenterY,
                FractalColor = preset1.FractalColor,
                BackgroundColor = preset1.BackgroundColor
            };
            preset1.PreviewParametersJson = JsonSerializer.Serialize(previewParams1S, jsonOptions);
            presets.Add(preset1);

            var preset2 = new SerpinskySaveState("Serpinsky")
            {
                SaveName = "Ночной Хаос",
                RenderMode = SerpinskyRenderMode.Chaos,
                Iterations = 100000,
                Zoom = 1.0,
                CenterX = 0,
                CenterY = 0.1,
                FractalColor = Color.OrangeRed,
                BackgroundColor = Color.FromArgb(10, 0, 20),
                Timestamp = DateTime.MinValue
            };
            var previewParams2S = new FractalSerpinsky.SerpinskyPreviewParams
            {
                RenderMode = preset2.RenderMode,
                Iterations = Math.Min(preset2.Iterations, PREVIEW_ITERATION_LIMIT_SERPINSKY_CHAOS),
                Zoom = preset2.Zoom,
                CenterX = preset2.CenterX,
                CenterY = preset2.CenterY,
                FractalColor = preset2.FractalColor,
                BackgroundColor = preset2.BackgroundColor
            };
            preset2.PreviewParametersJson = JsonSerializer.Serialize(previewParams2S, jsonOptions);
            presets.Add(preset2);

            return presets;
        }

        #endregion

        #region Newton Pool Presets

        private static List<FractalSaveStateBase> GetNewtonPoolsPresets()
        {
            var presets = new List<FractalSaveStateBase>();
            var jsonOptions = new JsonSerializerOptions();
            jsonOptions.Converters.Add(new JsonConverters.JsonColorConverter()); // Для сериализации Color внутри PaletteSnapshot

            // --- Пресет 1: Классический z^3 - 1 ---
            var palette1 = new Utilities.SaveIO.ColorPalettes.NewtonColorPalette
            {
                Name = "NewtonPreset1_Classic",
                RootColors = new List<Color> { Color.FromArgb(255, 100, 100), Color.FromArgb(100, 255, 100), Color.FromArgb(100, 100, 255) },
                BackgroundColor = Color.FromArgb(20, 0, 0),
                IsGradient = false
            };
            var preset1 = new NewtonSaveState("NewtonPools")
            {
                SaveName = "Ньютон: z^3 - 1 (Классика)",
                Formula = "z^3-1",
                CenterX = 0m,
                CenterY = 0m,
                Zoom = 1.0m,
                Iterations = 100,
                PaletteSnapshot = palette1,
                Timestamp = DateTime.MinValue
            };
            var previewParams1N = new NewtonPools.NewtonPreviewParams
            {
                Formula = preset1.Formula,
                CenterX = preset1.CenterX,
                CenterY = preset1.CenterY,
                Zoom = preset1.Zoom,
                Iterations = Math.Min(preset1.Iterations, PREVIEW_ITERATION_LIMIT_NEWTON_CHAOS),
                PaletteSnapshot = preset1.PaletteSnapshot
            };
            preset1.PreviewParametersJson = JsonSerializer.Serialize(previewParams1N, jsonOptions);
            presets.Add(preset1);

            // --- Пресет 2: z^4 - 1 с градиентом ---
            var palette2 = new Utilities.SaveIO.ColorPalettes.NewtonColorPalette
            {
                Name = "NewtonPreset2_Gradient",
                RootColors = new List<Color> { Color.Cyan, Color.Magenta, Color.Yellow, Color.Lime },
                BackgroundColor = Color.Black,
                IsGradient = true
            };
            var preset2 = new NewtonSaveState("NewtonPools")
            {
                SaveName = "Ньютон: z^4 - 1 (Градиент)",
                Formula = "z^4-1",
                CenterX = 0m,
                CenterY = 0m,
                Zoom = 1.2m,
                Iterations = 80,
                PaletteSnapshot = palette2,
                Timestamp = DateTime.MinValue
            };
            var previewParams2N = new NewtonPools.NewtonPreviewParams
            {
                Formula = preset2.Formula,
                CenterX = preset2.CenterX,
                CenterY = preset2.CenterY,
                Zoom = preset2.Zoom,
                Iterations = Math.Min(preset2.Iterations, PREVIEW_ITERATION_LIMIT_NEWTON_CHAOS),
                PaletteSnapshot = preset2.PaletteSnapshot
            };
            preset2.PreviewParametersJson = JsonSerializer.Serialize(previewParams2N, jsonOptions);
            presets.Add(preset2);

            // --- Пресет 3: Более сложная функция z^5 - z^2 + 1 ---
            var palette3 = new Utilities.SaveIO.ColorPalettes.NewtonColorPalette
            {
                Name = "NewtonPreset3_Complex",
                RootColors = new List<Color> { Color.Orange, Color.Purple, Color.GreenYellow, Color.SkyBlue, Color.HotPink },
                BackgroundColor = Color.FromArgb(10, 10, 30),
                IsGradient = false
            };
            var preset3 = new NewtonSaveState("NewtonPools")
            {
                SaveName = "Ньютон: z^5 - z^2 + 1",
                Formula = "z^5 - z^2 + 1",
                CenterX = 0m,
                CenterY = 0m,
                Zoom = 1.5m,
                Iterations = 120,
                PaletteSnapshot = palette3,
                Timestamp = DateTime.MinValue
            };
            var previewParams3N = new NewtonPools.NewtonPreviewParams
            {
                Formula = preset3.Formula,
                CenterX = preset3.CenterX,
                CenterY = preset3.CenterY,
                Zoom = preset3.Zoom,
                Iterations = Math.Min(preset3.Iterations, PREVIEW_ITERATION_LIMIT_NEWTON_CHAOS),
                PaletteSnapshot = preset3.PaletteSnapshot
            };
            preset3.PreviewParametersJson = JsonSerializer.Serialize(previewParams3N, jsonOptions);
            presets.Add(preset3);

            // --- Пресет 4: z^3 - 2*z + 2 (из вашего списка) с градиентом и другим центром ---
            var palette4 = new Utilities.SaveIO.ColorPalettes.NewtonColorPalette
            {
                Name = "NewtonPreset4_Shifted",
                RootColors = new List<Color> { Color.Teal, Color.Gold, Color.Crimson },
                BackgroundColor = Color.FromArgb(5, 5, 5),
                IsGradient = true
            };
            var preset4 = new NewtonSaveState("NewtonPools")
            {
                SaveName = "Ньютон: z^3-2*z+2 (Сдвиг)",
                Formula = "z^3-2*z+2",
                CenterX = 0.5m,
                CenterY = -0.3m,
                Zoom = 2.0m,
                Iterations = 150,
                PaletteSnapshot = palette4,
                Timestamp = DateTime.MinValue
            };
            var previewParams4N = new NewtonPools.NewtonPreviewParams
            {
                Formula = preset4.Formula,
                CenterX = preset4.CenterX,
                CenterY = preset4.CenterY,
                Zoom = preset4.Zoom,
                Iterations = Math.Min(preset4.Iterations, PREVIEW_ITERATION_LIMIT_NEWTON_CHAOS),
                PaletteSnapshot = preset4.PaletteSnapshot
            };
            preset4.PreviewParametersJson = JsonSerializer.Serialize(previewParams4N, jsonOptions);
            presets.Add(preset4);

            return presets;
        }

        #endregion
    }
}