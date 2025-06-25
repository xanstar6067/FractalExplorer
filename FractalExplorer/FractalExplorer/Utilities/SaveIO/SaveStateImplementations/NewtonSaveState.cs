using FractalExplorer.Utilities.SaveIO.ColorPalettes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FractalExplorer.Utilities.SaveIO.SaveStateImplementations
{
    public class NewtonSaveState : FractalSaveStateBase
    {
        public string Formula { get; set; }
        public decimal CenterX { get; set; } // decimal, чтобы соответствовать другим формам
        public decimal CenterY { get; set; }
        public decimal Zoom { get; set; }
        public int Iterations { get; set; }
        public NewtonColorPalette PaletteSnapshot { get; set; } // Сохраняем всю палитру

        public NewtonSaveState() { /* Для десериализации */ }

        public NewtonSaveState(string fractalType)
        {
            FractalType = fractalType;
        }
    }
}
