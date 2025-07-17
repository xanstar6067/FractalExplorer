using FractalDraving;
using FractalExplorer.Engines;
using FractalExplorer.Utilities.SaveIO;
using FractalExplorer.Utilities.SaveIO.SaveStateImplementations;
using System.Text.Json;

namespace FractalExplorer.Projects
{
    /// <summary>
    /// Форма для отображения и взаимодействия с фракталом Буффало.
    /// </summary>
    public partial class FractalBuffalo : FractalMandelbrotFamilyForm
    {
        public FractalBuffalo()
        {
            Text = "Фрактал Буффало";
        }

        protected override FractalMandelbrotFamilyEngine CreateEngine()
        {
            return new BuffaloEngine();
        }

        protected override void OnPostInitialize()
        {
            // Скрываем ненужные для этого фрактала элементы управления.
            lblRe.Visible = false;
            nudRe.Visible = false;
            lblIm.Visible = false;
            nudIm.Visible = false;
            mandelbrotPreviewPanel.Visible = false;
        }

        protected override string GetSaveFileNameDetails()
        {
            return "buffalo";
        }

        #region ISaveLoadCapableFractal Implementation

        public override string FractalTypeIdentifier => "Buffalo";
        public override Type ConcreteSaveStateType => typeof(MandelbrotFamilySaveState);

        public override FractalSaveStateBase GetCurrentStateForSave(string saveName)
        {
            // Используем базовый класс состояния, так как у Buffalo нет доп. параметров
            var state = new MandelbrotFamilySaveState(this.FractalTypeIdentifier)
            {
                SaveName = saveName,
                Timestamp = DateTime.Now,
                CenterX = _centerX,
                CenterY = _centerY,
                Zoom = _zoom,
                Threshold = nudThreshold.Value,
                Iterations = (int)nudIterations.Value,
                PaletteName = _paletteManager.ActivePalette?.Name ?? "Стандартный серый",
                PreviewEngineType = this.FractalTypeIdentifier
            };

            // Параметры для рендера превью
            var previewParams = new PreviewParams
            {
                CenterX = state.CenterX,
                CenterY = state.CenterY,
                Zoom = state.Zoom,
                Iterations = state.Iterations,
                PaletteName = state.PaletteName,
                Threshold = state.Threshold,
                PreviewEngineType = state.PreviewEngineType
            };
            state.PreviewParametersJson = JsonSerializer.Serialize(previewParams, new JsonSerializerOptions());
            return state;
        }

        public override void LoadState(FractalSaveStateBase stateBase)
        {
            // Просто вызываем базовую реализацию, так как нет специфичных параметров
            base.LoadState(stateBase);
        }

        public override List<FractalSaveStateBase> LoadAllSavesForThisType()
        {
            var specificSaves = SaveFileManager.LoadSaves<MandelbrotFamilySaveState>(this.FractalTypeIdentifier);
            return specificSaves.Cast<FractalSaveStateBase>().ToList();
        }

        public override void SaveAllSavesForThisType(List<FractalSaveStateBase> saves)
        {
            var specificSaves = saves.Cast<MandelbrotFamilySaveState>().ToList();
            SaveFileManager.SaveSaves(this.FractalTypeIdentifier, specificSaves);
        }

        #endregion
    }
}