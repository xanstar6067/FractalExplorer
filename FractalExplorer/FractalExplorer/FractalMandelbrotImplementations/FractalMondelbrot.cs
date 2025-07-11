using FractalDraving;
using FractalExplorer.Engines;
using FractalExplorer.Engines.EngineInterfaces;
using FractalExplorer.Utilities.SaveIO;
using FractalExplorer.Utilities.SaveIO.SaveStateImplementations;
using FractalExplorer.Utilities;

namespace FractalExplorer.Projects
{
    public partial class FractalMondelbrot : FractalMandelbrotFamilyForm
    {
        public FractalMondelbrot()
        {
            Text = "Множество Мандельброта";
        }

        // Реализуем новый абстрактный метод
        protected override FractalType GetFractalType()
        {
            return FractalType.Mandelbrot;
        }

        protected override void OnPostInitialize()
        {
            // Эта логика остается, она настраивает UI
            lblRe.Visible = false;
            nudRe.Visible = false;
            lblIm.Visible = false;
            nudIm.Visible = false;
            mandelbrotPreviewPanel.Visible = false;
        }

        protected override string GetSaveFileNameDetails()
        {
            return "mandelbrot";
        }

        #region ISaveLoadCapableFractal Overrides

        public override string FractalTypeIdentifier => "Mandelbrot";
        public override Type ConcreteSaveStateType => typeof(MandelbrotFamilySaveState);

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