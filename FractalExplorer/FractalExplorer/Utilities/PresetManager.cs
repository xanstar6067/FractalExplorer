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
                // Добавьте сюда другие фракталы по мере необходимости
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
                Timestamp = DateTime.MinValue // Используем для идентификации пресетов
            };
            var previewParams1 = new FractalMandelbrotFamilyForm.PreviewParams
            {
                CenterX = preset1.CenterX,
                CenterY = preset1.CenterY,
                Zoom = preset1.Zoom,
                Iterations = Math.Min(preset1.Iterations, 75), // Итерации для превью
                PaletteName = preset1.PaletteName,
                Threshold = preset1.Threshold,
                PreviewEngineType = "Mandelbrot" // Тип движка для этого пресета
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
                Iterations = Math.Min(preset2.Iterations, 75), // Итерации для превью
                PaletteName = preset2.PaletteName,
                Threshold = preset2.Threshold,
                PreviewEngineType = "Mandelbrot"
            };
            preset2.PreviewParametersJson = JsonSerializer.Serialize(previewParams2);
            presets.Add(preset2);

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
                Iterations = Math.Min(preset1.Iterations, 75),
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
                Iterations = Math.Min(preset2.Iterations, 75),
                PaletteName = preset2.PaletteName,
                Threshold = preset2.Threshold,
                CRe = preset2.CRe,
                CIm = preset2.CIm,
                PreviewEngineType = "Julia"
            };
            preset2.PreviewParametersJson = JsonSerializer.Serialize(previewParams2);
            presets.Add(preset2);

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
                Zoom = 1.0m,
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
                Iterations = Math.Min(preset1.Iterations, 75),
                PaletteName = preset1.PaletteName,
                Threshold = preset1.Threshold,
                PreviewEngineType = "MandelbrotBurningShip"
            };
            preset1.PreviewParametersJson = JsonSerializer.Serialize(previewParams1);
            presets.Add(preset1);
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
                Iterations = Math.Min(preset1.Iterations, 75),
                PaletteName = preset1.PaletteName,
                Threshold = preset1.Threshold,
                CRe = preset1.CRe,
                CIm = preset1.CIm,
                PreviewEngineType = "JuliaBurningShip"
            };
            preset1.PreviewParametersJson = JsonSerializer.Serialize(previewParams1);
            presets.Add(preset1);
            return presets;
        }

        private static List<FractalSaveStateBase> GetPhoenixPresets()
        {
            // Для PhoenixSaveState и SerpinskySaveState:
            // Текущая реализация RenderPreview в FractalMandelbrotFamilyForm не умеет
            // обрабатывать эти типы фракталов через PreviewEngineType "Phoenix" или "Serpinsky"
            // без дополнительных доработок в RenderPreview.
            // Поэтому PreviewParametersJson здесь пока не заполняется.
            // Если вы добавите соответствующую логику рендеринга для них,
            // можно будет аналогично заполнять PreviewParametersJson.
            return new List<FractalSaveStateBase>
            {
                new PhoenixSaveState("Phoenix")
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
                    // PreviewParametersJson = ... ; // Если будет реализован рендер превью
                }
            };
        }

        private static List<FractalSaveStateBase> GetSerpinskyPresets()
        {
            return new List<FractalSaveStateBase>
            {
                new SerpinskySaveState("Serpinsky")
                {
                    SaveName = "Классическая Геометрия",
                    RenderMode = SerpinskyRenderMode.Geometric,
                    Iterations = 8,
                    Zoom = 1.0,
                    CenterX = 0,
                    CenterY = 0.1, // Немного смещаем центр для лучшего вида
                    FractalColor = System.Drawing.Color.Black,
                    BackgroundColor = System.Drawing.Color.White,
                    Timestamp = DateTime.MinValue
                    // PreviewParametersJson = ... ; // Если будет реализован рендер превью
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
                    // PreviewParametersJson = ... ; // Если будет реализован рендер превью
                }
            };
        }
    }
}