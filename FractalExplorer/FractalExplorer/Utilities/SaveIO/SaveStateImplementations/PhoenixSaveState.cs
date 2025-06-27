namespace FractalExplorer.Utilities.SaveIO.SaveStateImplementations
{
    /// <summary>
    /// Представляет состояние сохранения для фракталов Феникса.
    /// Включает общие параметры, такие как центр, масштаб, итерации, пороговое значение и имя палитры,
    /// а также специфичные для фрактала Феникса комплексные константы C1 и C2.
    /// </summary>
    public class PhoenixSaveState : FractalSaveStateBase
    {
        /// <summary>
        /// Получает или задает X-координату центра фрактала в комплексной плоскости.
        /// </summary>
        public decimal CenterX { get; set; }

        /// <summary>
        /// Получает или задает Y-координату центра фрактала в комплексной плоскости.
        /// </summary>
        public decimal CenterY { get; set; }

        /// <summary>
        /// Получает или задает коэффициент масштабирования (зум) фрактала.
        /// </summary>
        public decimal Zoom { get; set; }

        /// <summary>
        /// Получает или задает пороговое значение, используемое для определения того,
        /// находится ли точка внутри множества Феникса.
        /// </summary>
        public decimal Threshold { get; set; }

        /// <summary>
        /// Получает или задает максимальное количество итераций,
        /// используемых при вычислении цвета каждой точки фрактала.
        /// </summary>
        public int Iterations { get; set; }

        /// <summary>
        /// Получает или задает имя цветовой палитры, примененной к фракталу.
        /// </summary>
        public string PaletteName { get; set; }

        /// <summary>
        /// Получает или задает действительную часть первой комплексной константы C1,
        /// специфичной для итерационной формулы фрактала Феникса.
        /// </summary>
        public decimal C1Re { get; set; }

        /// <summary>
        /// Получает или задает мнимую часть первой комплексной константы C1,
        /// специфичной для итерационной формулы фрактала Феникса.
        /// </summary>
        public decimal C1Im { get; set; }

        /// <summary>
        /// Получает или задает действительную часть второй комплексной константы C2,
        /// специфичной для итерационной формулы фрактала Феникса.
        /// </summary>
        public decimal C2Re { get; set; }

        /// <summary>
        /// Получает или задает мнимую часть второй комплексной константы C2,
        /// специфичной для итерационной формулы фрактала Феникса.
        /// </summary>
        public decimal C2Im { get; set; }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="PhoenixSaveState"/>.
        /// Этот конструктор необходим для корректной десериализации JSON.
        /// </summary>
        public PhoenixSaveState()
        {
        }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="PhoenixSaveState"/>
        /// с указанным идентификатором типа фрактала.
        /// </summary>
        /// <param name="fractalType">Строковый идентификатор типа фрактала (например, "Phoenix").</param>
        public PhoenixSaveState(string fractalType)
        {
            FractalType = fractalType;
        }
    }
}
