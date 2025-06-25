using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FractalExplorer.Utilities
{
    public class MandelbrotFamilySaveState : FractalSaveStateBase
    {
        public decimal CenterX { get; set; }
        public decimal CenterY { get; set; }
        public decimal Zoom { get; set; }
        public decimal Threshold { get; set; }
        public int Iterations { get; set; }
        public string PaletteName { get; set; }
        // Дополнительные поля, если нужны для рендера превью,
        // но они также могут быть в PreviewParametersJson
        public string PreviewEngineType { get; set; } // "Mandelbrot" или "Julia" или "BurningShipMandelbrot" и т.д.

        public MandelbrotFamilySaveState() { /* Для десериализации */ }

        public MandelbrotFamilySaveState(string fractalType)
        {
            FractalType = fractalType;
        }
    }

    public class JuliaFamilySaveState : MandelbrotFamilySaveState
    {
        public decimal CRe { get; set; }
        public decimal CIm { get; set; }

        public JuliaFamilySaveState() : base() { /* Для десериализации */ }

        public JuliaFamilySaveState(string fractalType) : base(fractalType) { }
    }
}
