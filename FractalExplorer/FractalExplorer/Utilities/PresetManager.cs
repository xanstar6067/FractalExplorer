using System;
using System.Collections.Generic;
using System.Linq;
using FractalExplorer.Utilities.SaveIO.SaveStateImplementations;
using FractalExplorer.Engines; // Для SerpinskyRenderMode
using System.Text.Json;     // Для JsonSerializer
using FractalDraving;       // Для FractalMandelbrotFamilyForm.PreviewParams

namespace FractalExplorer.Utilities
{
    public static class PresetManager
    {
        private const int PREVIEW_ITERATION_LIMIT = 175; // Вынесем лимит в константу

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
                default:
                    return new List<FractalSaveStateBase>();
            }
        }

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
                Iterations = Math.Min(preset1.Iterations, PREVIEW_ITERATION_LIMIT),
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
                Iterations = Math.Min(preset2.Iterations, PREVIEW_ITERATION_LIMIT),
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
                Zoom = 6500.0m, // Примерный зум, можно подобрать точнее
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
                Iterations = Math.Min(preset3.Iterations, PREVIEW_ITERATION_LIMIT),
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
                Iterations = Math.Min(preset4.Iterations, PREVIEW_ITERATION_LIMIT),
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

            // Пресет 1: Классическая Спираль
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
            var previewParams1 = new FractalMandelbrotFamilyForm.PreviewParams
            {
                CenterX = preset1.CenterX,
                CenterY = preset1.CenterY,
                Zoom = preset1.Zoom,
                Iterations = Math.Min(preset1.Iterations, PREVIEW_ITERATION_LIMIT),
                PaletteName = preset1.PaletteName,
                Threshold = preset1.Threshold,
                CRe = preset1.CRe,
                CIm = preset1.CIm,
                PreviewEngineType = "Julia"
            };
            preset1.PreviewParametersJson = JsonSerializer.Serialize(previewParams1);
            presets.Add(preset1);

            // Пресет 2: Дендрит
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
            var previewParams2 = new FractalMandelbrotFamilyForm.PreviewParams
            {
                CenterX = preset2.CenterX,
                CenterY = preset2.CenterY,
                Zoom = preset2.Zoom,
                Iterations = Math.Min(preset2.Iterations, PREVIEW_ITERATION_LIMIT),
                PaletteName = preset2.PaletteName,
                Threshold = preset2.Threshold,
                CRe = preset2.CRe,
                CIm = preset2.CIm,
                PreviewEngineType = "Julia"
            };
            preset2.PreviewParametersJson = JsonSerializer.Serialize(previewParams2);
            presets.Add(preset2);

            // NEW PRESET: Снежинка
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
            var previewParams3 = new FractalMandelbrotFamilyForm.PreviewParams
            {
                CenterX = preset3.CenterX,
                CenterY = preset3.CenterY,
                Zoom = preset3.Zoom,
                Iterations = Math.Min(preset3.Iterations, PREVIEW_ITERATION_LIMIT),
                PaletteName = preset3.PaletteName,
                Threshold = preset3.Threshold,
                CRe = preset3.CRe,
                CIm = preset3.CIm,
                PreviewEngineType = "Julia"
            };
            preset3.PreviewParametersJson = JsonSerializer.Serialize(previewParams3);
            presets.Add(preset3);

            // NEW PRESET: Огненный Вихрь
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
            var previewParams4 = new FractalMandelbrotFamilyForm.PreviewParams
            {
                CenterX = preset4.CenterX,
                CenterY = preset4.CenterY,
                Zoom = preset4.Zoom,
                Iterations = Math.Min(preset4.Iterations, PREVIEW_ITERATION_LIMIT),
                PaletteName = preset4.PaletteName,
                Threshold = preset4.Threshold,
                CRe = preset4.CRe,
                CIm = preset4.CIm,
                PreviewEngineType = "Julia"
            };
            preset4.PreviewParametersJson = JsonSerializer.Serialize(previewParams4);
            presets.Add(preset4);

            return presets;
        }

        private static List<FractalSaveStateBase> GetMandelbrotBurningShipPresets()
        {
            var presets = new List<FractalSaveStateBase>();
            var preset1 = new MandelbrotFamilySaveState("MandelbrotBurningShip")
            {
                SaveName = "Центральный Корабль",
                CenterX = -0.5m, // Скорректировано для лучшего вида корабля
                CenterY = -0.5m, // Скорректировано
                Zoom = 0.8m,     // Немного отдалим, чтобы видеть больше
                Iterations = 300,
                Threshold = 2.0m,
                PaletteName = "Огонь",
                Timestamp = DateTime.MinValue
            };
            var previewParams1 = new FractalMandelbrotFamilyForm.PreviewParams
            {
                CenterX = preset1.CenterX,
                CenterY = preset1.CenterY,
                Zoom = preset1.Zoom,
                Iterations = Math.Min(preset1.Iterations, PREVIEW_ITERATION_LIMIT),
                PaletteName = preset1.PaletteName,
                Threshold = preset1.Threshold,
                PreviewEngineType = "MandelbrotBurningShip"
            };
            preset1.PreviewParametersJson = JsonSerializer.Serialize(previewParams1);
            presets.Add(preset1);

            // NEW PRESET: Глубоководный Корабль
            var preset2 = new MandelbrotFamilySaveState("MandelbrotBurningShip")
            {
                SaveName = "Глубоководный Корабль",
                CenterX = -1.768739999999000m,
                CenterY = -0.001640000000000m,
                Zoom = 1000.0m, // Примерный зум
                Iterations = 500,
                Threshold = 2.0m,
                PaletteName = "Глубина",
                Timestamp = DateTime.MinValue
            };
            var previewParams2 = new FractalMandelbrotFamilyForm.PreviewParams
            {
                CenterX = preset2.CenterX,
                CenterY = preset2.CenterY,
                Zoom = preset2.Zoom,
                Iterations = Math.Min(preset2.Iterations, PREVIEW_ITERATION_LIMIT),
                PaletteName = preset2.PaletteName,
                Threshold = preset2.Threshold,
                PreviewEngineType = "MandelbrotBurningShip"
            };
            preset2.PreviewParametersJson = JsonSerializer.Serialize(previewParams2);
            presets.Add(preset2);

            // NEW PRESET: Призрачные Паруса
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
            var previewParams3 = new FractalMandelbrotFamilyForm.PreviewParams
            {
                CenterX = preset3.CenterX,
                CenterY = preset3.CenterY,
                Zoom = preset3.Zoom,
                Iterations = Math.Min(preset3.Iterations, PREVIEW_ITERATION_LIMIT),
                PaletteName = preset3.PaletteName,
                Threshold = preset3.Threshold,
                PreviewEngineType = "MandelbrotBurningShip"
            };
            preset3.PreviewParametersJson = JsonSerializer.Serialize(previewParams3);
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
            var previewParams1 = new FractalMandelbrotFamilyForm.PreviewParams
            {
                CenterX = preset1.CenterX,
                CenterY = preset1.CenterY,
                Zoom = preset1.Zoom,
                Iterations = Math.Min(preset1.Iterations, PREVIEW_ITERATION_LIMIT),
                PaletteName = preset1.PaletteName,
                Threshold = preset1.Threshold,
                CRe = preset1.CRe,
                CIm = preset1.CIm,
                PreviewEngineType = "JuliaBurningShip"
            };
            preset1.PreviewParametersJson = JsonSerializer.Serialize(previewParams1);
            presets.Add(preset1);

            // NEW PRESET: Огненный Дракон
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
            var previewParams2 = new FractalMandelbrotFamilyForm.PreviewParams
            {
                CenterX = preset2.CenterX,
                CenterY = preset2.CenterY,
                Zoom = preset2.Zoom,
                Iterations = Math.Min(preset2.Iterations, PREVIEW_ITERATION_LIMIT),
                PaletteName = preset2.PaletteName,
                Threshold = preset2.Threshold,
                CRe = preset2.CRe,
                CIm = preset2.CIm,
                PreviewEngineType = "JuliaBurningShip"
            };
            preset2.PreviewParametersJson = JsonSerializer.Serialize(previewParams2);
            presets.Add(preset2);

            // NEW PRESET: Туманность Андромеды (стилизация)
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
            var previewParams3 = new FractalMandelbrotFamilyForm.PreviewParams
            {
                CenterX = preset3.CenterX,
                CenterY = preset3.CenterY,
                Zoom = preset3.Zoom,
                Iterations = Math.Min(preset3.Iterations, PREVIEW_ITERATION_LIMIT),
                PaletteName = preset3.PaletteName,
                Threshold = preset3.Threshold,
                CRe = preset3.CRe,
                CIm = preset3.CIm,
                PreviewEngineType = "JuliaBurningShip"
            };
            preset3.PreviewParametersJson = JsonSerializer.Serialize(previewParams3);
            presets.Add(preset3);

            return presets;
        }

        private static List<FractalSaveStateBase> GetPhoenixPresets()
        {
            var presets = new List<FractalSaveStateBase>();
            // Пресет 1
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
            // Для Phoenix пока не генерируем PreviewParametersJson,
            // так как RenderPreview в FractalMandelbrotFamilyForm его не обработает.
            // Нужна отдельная логика рендеринга превью для Phoenix.
            presets.Add(preset1);

            // NEW PRESET: Вихрь Феникса
            var preset2 = new PhoenixSaveState("Phoenix")
            {
                SaveName = "Вихрь Феникса",
                C1Re = 0.35m,
                C1Im = -0.62m,
                C2Re = -0.01m, // Небольшое значение для C2Re может дать интересные эффекты
                C2Im = 0.005m, // Небольшое значение для C2Im
                CenterX = 0.1m,
                CenterY = -0.2m,
                Zoom = 1.2m,
                Iterations = 250,
                Threshold = 4.0m,
                PaletteName = "Психоделика",
                Timestamp = DateTime.MinValue
            };
            presets.Add(preset2);

            // NEW PRESET: Хвост Павлина
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
                PaletteName = "Лёд", // Или другая яркая палитра
                Timestamp = DateTime.MinValue
            };
            presets.Add(preset3);

            return presets;
        }

        private static List<FractalSaveStateBase> GetSerpinskyPresets()
        {
            // Для Серпинского оставляем как есть, превью для них требует 
            // совершенно другой логики рендеринга, не связанной с MandelbrotFamilyEngine.
            return new List<FractalSaveStateBase>
            {
                new SerpinskySaveState("Serpinsky")
                {
                    SaveName = "Классическая Геометрия",
                    RenderMode = SerpinskyRenderMode.Geometric,
                    Iterations = 8,
                    Zoom = 1.0,
                    CenterX = 0,
                    CenterY = 0.1,
                    FractalColor = System.Drawing.Color.Black,
                    BackgroundColor = System.Drawing.Color.White,
                    Timestamp = DateTime.MinValue
                },
                new SerpinskySaveState("Serpinsky")
                {
                    SaveName = "Ночной Хаос",
                    RenderMode = SerpinskyRenderMode.Chaos,
                    Iterations = 100000,
                    Zoom = 1.0,
                    CenterX = 0,
                    CenterY = 0.1,
                    FractalColor = System.Drawing.Color.OrangeRed,
                    BackgroundColor = System.Drawing.Color.FromArgb(10, 0, 20),
                    Timestamp = DateTime.MinValue
                }
            };
        }
    }
}