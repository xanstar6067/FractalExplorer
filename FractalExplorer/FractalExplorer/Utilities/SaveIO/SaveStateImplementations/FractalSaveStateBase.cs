using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FractalExplorer.Utilities.SaveIO.SaveStateImplementations
{
    public abstract class FractalSaveStateBase
    {
        public string SaveName { get; set; }
        public DateTime Timestamp { get; set; }
        public string FractalType { get; protected set; } // Устанавливается в конструкторе наследника
        public string PreviewParametersJson { get; set; } // JSON с параметрами для превью
    }
}
