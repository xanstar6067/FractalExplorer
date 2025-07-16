using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FractalExplorer.Utilities.SaveIO.SaveStateImplementations
{
    /// <summary>
    /// Представляет состояние сохранения для Обобщенного множества Мандельброта,
    /// которое расширяет базовое семейство фракталов Мандельброта, 
    /// добавляя настраиваемую степень 'p' в итерационную формулу.
    /// </summary>
    public class GeneralizedMandelbrotSaveState : MandelbrotFamilySaveState
    {
        /// <summary>
        /// Получает или задает степень 'p' в итерационной формуле z -> z^p + c.
        /// </summary>
        public decimal Power { get; set; }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="GeneralizedMandelbrotSaveState"/>
        /// с использованием значений по умолчанию.
        /// </summary>
        public GeneralizedMandelbrotSaveState() : base()
        {
        }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="GeneralizedMandelbrotSaveState"/>
        /// с указанием типа фрактала.
        /// </summary>
        /// <param name="fractalType">Строка, идентифицирующая тип фрактала.</param>
        public GeneralizedMandelbrotSaveState(string fractalType) : base(fractalType)
        {
        }
    }
}