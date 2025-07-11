using FractalDraving;
using FractalExplorer.Engines;
using FractalExplorer.Engines.EngineInterfaces;
using FractalExplorer.Utilities.SaveIO;
using FractalExplorer.Utilities.SaveIO.SaveStateImplementations;
using FractalExplorer.Utilities;

namespace FractalExplorer.Projects
{
    public partial class FractalMondelbrotBurningShip : FractalMandelbrotFamilyForm
    {
        public FractalMondelbrotBurningShip()
        {
            Text = "Множество Горящий Корабль";
        }

        protected override FractalType GetFractalType()
        {
            return FractalType.MandelbrotBurningShip;
        }

        protected override decimal InitialCenterX => -1.75m;
        protected override decimal InitialCenterY => -0.05m;

        protected override void OnPostInitialize()
        {
            lblRe.Visible = false;
            nudRe.Visible = false;
            lblIm.Visible = false;
            nudIm.Visible = false;
            mandelbrotPreviewPanel.Visible = false;
        }

        protected override string GetSaveFileNameDetails()
        {
            return "burningship_mandelbrot";
        }

        #region ISaveLoadCapableFractal Overrides

        public override string FractalTypeIdentifier => "MandelbrotBurningShip";
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