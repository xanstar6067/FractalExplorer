using FractalDraving;
using FractalExplorer.Engines;
using FractalExplorer.Utilities.SaveIO;
using FractalExplorer.Utilities.SaveIO.SaveStateImplementations;

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

        // --- Временная реализация для запуска (позже будет дополнена) ---
        public override string FractalTypeIdentifier => "Buffalo";
        public override Type ConcreteSaveStateType => typeof(MandelbrotFamilySaveState);

        public override List<FractalSaveStateBase> LoadAllSavesForThisType()
        {
            // TODO: Будет реализовано на следующем этапе
            return new List<FractalSaveStateBase>();
        }

        public override void SaveAllSavesForThisType(List<FractalSaveStateBase> saves)
        {
            // TODO: Будет реализовано на следующем этапе
        }
    }
}