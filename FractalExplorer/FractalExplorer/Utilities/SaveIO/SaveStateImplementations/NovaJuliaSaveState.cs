using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FractalExplorer.Utilities.SaveIO.SaveStateImplementations
{
    /// <summary>
    /// Представляет состояние сохранения для фракталов Nova Julia.
    /// Наследует параметры Nova Mandelbrot (P, M, Z0) и добавляет константу C.
    /// </summary>
    public class NovaJuliaSaveState : NovaMandelbrotSaveState
    {
        /// <summary>
        /// Получает или задает действительную часть константы C,
        /// определяющей форму множества Жюлиа.
        /// </summary>
        public decimal C_Re { get; set; }

        /// <summary>
        /// Получает или задает мнимую часть константы C,
        /// определяющей форму множества Жюлиа.
        /// </summary>
        public decimal C_Im { get; set; }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="NovaJuliaSaveState"/>.
        /// </summary>
        public NovaJuliaSaveState()
        {
        }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="NovaJuliaSaveState"/>
        /// с указанным идентификатором типа фрактала.
        /// </summary>
        /// <param name="fractalType">Строковый идентификатор типа фрактала (например, "NovaJulia").</param>
        public NovaJuliaSaveState(string fractalType) : base(fractalType)
        {
        }
    }
}
