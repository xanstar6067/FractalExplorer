using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FractalExplorer.Utilities.SaveIO.SaveStateImplementations
{
    /// <summary>
    /// Представляет состояние сохранения для Обобщенного множества Мандельброта,
    /// добавляя к базовым параметрам настраиваемую степень 'p'.
    /// </summary>
    public class GeneralizedMandelbrotSaveState : MandelbrotFamilySaveState
    {
        /// <summary>
        /// Получает или задает степень 'p' в итерационной формуле z -> z^p + c.
        /// </summary>
        public decimal Power { get; set; }

        public GeneralizedMandelbrotSaveState() : base() { }

        public GeneralizedMandelbrotSaveState(string fractalType) : base(fractalType) { }
    }
}