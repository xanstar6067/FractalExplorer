using FractalExplorer.Engines;
using FractalExplorer.Projects;
using FractalExplorer.Resources;
using FractalExplorer.Utilities.SaveIO.ColorPalettes;
using FractalExplorer.Utilities.SaveIO.SaveStateImplementations;
using System.Text.Json;

namespace FractalExplorer.Engines
{
    /// <summary>
    /// Фабрика для создания и настройки экземпляров движков фракталов на основе состояния.
    /// </summary>
    public static class FractalEngineFactory
    {
        /// <summary>
        /// Создает и настраивает движок фрактала на основе предоставленного состояния.
        /// </summary>
        /// <param name="stateBase">Состояние, содержащее все параметры для рендеринга.</param>
        /// <param name="paletteManager">Менеджер палитр для получения нужной палитры.</param>
        /// <param name="baseScale">Базовый масштаб формы, для расчета масштаба движка.</param>
        /// <returns>Полностью настроенный экземпляр движка фрактала.</returns>
        public static FractalMandelbrotFamilyEngine CreateEngine(FractalSaveStateBase stateBase, PaletteManager paletteManager, decimal baseScale)
        {
            if (string.IsNullOrEmpty(stateBase.PreviewParametersJson))
            {
                throw new ArgumentException("PreviewParametersJson не может быть пустым.", nameof(stateBase));
            }

            // Десериализуем базовые параметры, которые есть у всех
            var baseParams = JsonSerializer.Deserialize<MandelbrotFamilySaveState>(stateBase.PreviewParametersJson);

            FractalMandelbrotFamilyEngine engine;

            // Единственный switch теперь здесь, в изолированной фабрике.
            switch (baseParams.PreviewEngineType)
            {
                case "Mandelbrot":
                    engine = new MandelbrotEngine();
                    break;
                case "Julia":
                    var juliaParams = JsonSerializer.Deserialize<JuliaFamilySaveState>(stateBase.PreviewParametersJson);
                    engine = new JuliaEngine { C = new ComplexDecimal(juliaParams.CRe, juliaParams.CIm) };
                    break;
                case "MandelbrotBurningShip":
                    engine = new MandelbrotBurningShipEngine();
                    break;
                case "JuliaBurningShip":
                    var juliaBsParams = JsonSerializer.Deserialize<JuliaFamilySaveState>(stateBase.PreviewParametersJson);
                    engine = new JuliaBurningShipEngine { C = new ComplexDecimal(juliaBsParams.CRe, juliaBsParams.CIm) };
                    break;
                case "GeneralizedMandelbrot":
                    var genParams = JsonSerializer.Deserialize<GeneralizedMandelbrotSaveState>(stateBase.PreviewParametersJson);
                    engine = new GeneralizedMandelbrotEngine { Power = genParams.Power };
                    break;
                case "Buffalo":
                    engine = new BuffaloEngine();
                    break;
                case "Simonobrot":
                    var simonParams = JsonSerializer.Deserialize<GeneralizedMandelbrotSaveState>(stateBase.PreviewParametersJson);
                    engine = new SimonobrotEngine { Power = simonParams.Power, UseInversion = simonParams.UseInversion };
                    break;
                default:
                    throw new NotSupportedException($"Тип движка '{baseParams.PreviewEngineType}' не поддерживается фабрикой.");
            }

            // Настраиваем общие параметры движка
            engine.MaxIterations = baseParams.Iterations;
            engine.CenterX = baseParams.CenterX;
            engine.CenterY = baseParams.CenterY;
            engine.Scale = baseScale / (baseParams.Zoom == 0 ? 0.001m : baseParams.Zoom);
            engine.ThresholdSquared = baseParams.Threshold * baseParams.Threshold;

            var paletteForPreview = paletteManager.Palettes.FirstOrDefault(p => p.Name == baseParams.PaletteName) ?? paletteManager.Palettes.First();
            int effectiveMaxColorIterations = paletteForPreview.AlignWithRenderIterations ? engine.MaxIterations : paletteForPreview.MaxColorIterations;

            // Настраиваем палитры. Для превью всегда используем дискретную палитру.
            engine.UseSmoothColoring = false;
            engine.MaxColorIterations = effectiveMaxColorIterations;

            // Мы не можем передать сюда функцию, поэтому создаем временную базовую форму для доступа к генераторам палитр
            // Это небольшой компромисс, чтобы не делать генераторы палитр статическими
            var tempForm = new FractalMondelbrot(); // Любой наследник FractalMandelbrotFamilyForm
            engine.Palette = tempForm.GenerateDiscretePaletteFunction(paletteForPreview);
            tempForm.Dispose();

            return engine;
        }
    }
}