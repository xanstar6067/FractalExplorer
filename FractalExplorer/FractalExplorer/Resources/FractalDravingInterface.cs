using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FractalExplorer.Resources
{
    public interface IFractalForm
    {
        // Свойство для получения масштаба лупы
        double LoupeZoom { get; }
        // Событие, на которое подпишется селектор
        event EventHandler LoupeZoomChanged;
    }
}