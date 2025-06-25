using FractalExplorer.Engines;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FractalExplorer.Utilities.StateBaseImplementations
{
    public class SerpinskySaveState : FractalSaveStateBase
    {
        public SerpinskyRenderMode RenderMode { get; set; }
        public SerpinskyColorMode ColorMode { get; set; }
        public int Iterations { get; set; }
        public double Zoom { get; set; }
        public double CenterX { get; set; }
        public double CenterY { get; set; }
        public Color FractalColor { get; set; }
        public Color BackgroundColor { get; set; }

        public SerpinskySaveState() { /* Для десериализации */ }

        public SerpinskySaveState(string fractalType)
        {
            FractalType = fractalType;
        }
    }
}
