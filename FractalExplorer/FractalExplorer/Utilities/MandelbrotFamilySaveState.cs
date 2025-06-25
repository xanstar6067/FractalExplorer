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
        public decimal Threshold { get; set; } // Сохраняем сам порог, а не квадрат
        public int Iterations { get; set; }
        public string PaletteName { get; set; }

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
