namespace FractalExplorer.Utilities.SaveIO.SaveStateImplementations
{
    /// <summary>
    /// Представляет состояние сохранения для фрактала, основанного на гипотезе Коллатца.
    /// Наследует все основные параметры от MandelbrotFamilySaveState.
    /// </summary>
    public class CollatzSaveState : MandelbrotFamilySaveState
    {
        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="CollatzSaveState"/>.
        /// Этот конструктор предназначен для использования десериализатором JSON.
        /// </summary>
        public CollatzSaveState() : base()
        {
        }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="CollatzSaveState"/>
        /// с указанным идентификатором типа фрактала.
        /// </summary>
        /// <param name="fractalType">Строковый идентификатор типа фрактала (например, "Collatz").</param>
        public CollatzSaveState(string fractalType) : base(fractalType)
        {
        }
    }
}