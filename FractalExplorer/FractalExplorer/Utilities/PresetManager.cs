using System;
using System.Collections.Generic;
using System.Linq;
using FractalExplorer.Utilities.SaveIO.SaveStateImplementations;
using FractalExplorer.Engines; // Для SerpinskyRenderMode

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
            return new List<FractalSaveStateBase>
            {
                new MandelbrotFamilySaveState("Mandelbrot")
                {
                    SaveName = "Долина Морских Коньков",
                    CenterX = -0.743643887037151m,
                    CenterY = 0.13182590420533m,
                    Zoom = 11500m,
                    Iterations = 1000,
                    Threshold = 2.0m,
                    PaletteName = "Лёд",
                    Timestamp = DateTime.MinValue // Используем для идентификации пресетов
                },
                new MandelbrotFamilySaveState("Mandelbrot")
                {
                    SaveName = "Шип Миниброта",
                    CenterX = -1.74995m,
                    CenterY = 0.0m,
                    Zoom = 4000m,
                    Iterations = 500,
                    Threshold = 2.0m,
                    PaletteName = "Огонь",
                    Timestamp = DateTime.MinValue
                }
            };
        }

        private static List<FractalSaveStateBase> GetJuliaPresets()
        {
            return new List<FractalSaveStateBase>
            {
                new JuliaFamilySaveState("Julia")
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
                },
                new JuliaFamilySaveState("Julia")
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
                }
            };
        }

        private static List<FractalSaveStateBase> GetMandelbrotBurningShipPresets()
        {
            return new List<FractalSaveStateBase>
            {
                new MandelbrotFamilySaveState("MandelbrotBurningShip")
                {
                    SaveName = "Центральный Корабль",
                    CenterX = -0.5m,
                    CenterY = -0.5m,
                    Zoom = 1.0m,
                    Iterations = 300,
                    Threshold = 2.0m,
                    PaletteName = "Огонь",
                    Timestamp = DateTime.MinValue
                }
            };
        }

        private static List<FractalSaveStateBase> GetJuliaBurningShipPresets()
        {
            return new List<FractalSaveStateBase>
            {
                new JuliaFamilySaveState("JuliaBurningShip")
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
                }
            };
        }

        private static List<FractalSaveStateBase> GetPhoenixPresets()
        {
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