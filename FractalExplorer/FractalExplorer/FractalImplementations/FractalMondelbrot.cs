using FractalDraving;
using FractalExplorer.Engines;

namespace FractalExplorer.Projects
{
    public partial class FractalMondelbrot : FractalFormBase
    {
        public FractalMondelbrot()
        {
            Text = "Множество Мандельброта";
        }

        protected override FractalMondelbrotBaseEngine CreateEngine()
        {
            return new MandelbrotEngine();
        }

        protected override void OnPostInitialize()
        {
            // Скрываем ненужные для Мандельброта контролы
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
            return "mandelbrot";
        }
    }
}