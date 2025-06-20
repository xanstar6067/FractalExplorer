using FractalDraving;
using FractalExplorer.Engines;

namespace FractalExplorer.Projects
{
    public partial class FractalMondelbrotBurningShip : FractalFormBase
    {
        public FractalMondelbrotBurningShip()
        {
            Text = "Множество Горящий Корабль";
        }

        protected override FractalEngineBase CreateEngine()
        {
            return new MandelbrotBurningShipEngine();
        }

        protected override decimal InitialCenterX => -0.25m;
        protected override decimal InitialCenterY => -0.5m;

        protected override void OnPostInitialize()
        {
            // Скрываем ненужные для этого фрактала контролы
            lblRe.Visible = false;
            nudRe.Visible = false;
            lblIm.Visible = false;
            nudIm.Visible = false;
            mandelbrotPreviewPanel.Visible = false;
            lblLoupeZoom.Visible = false;
            nudBaseScale.Visible = false;
        }
        protected override string GetSaveFileNameDetails()
        {
            return "burningship_mandelbrot";
        }
    }
}