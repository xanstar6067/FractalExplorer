using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FractalExplorer.Utilities.SaveIO.SaveStateImplementations
{
    public class PhoenixSaveState : FractalSaveStateBase
    {
        public decimal CenterX { get; set; }
        public decimal CenterY { get; set; }
        public decimal Zoom { get; set; }
        public decimal Threshold { get; set; }
        public int Iterations { get; set; }
        public string PaletteName { get; set; }

        // Специфичные для Феникса параметры
        public decimal C1Re { get; set; }
        public decimal C1Im { get; set; }
        public decimal C2Re { get; set; }
        public decimal C2Im { get; set; }

        public PhoenixSaveState() { /* Для десериализации */ }

        public PhoenixSaveState(string fractalType)
        {
            FractalType = fractalType;
        }
    }
}
