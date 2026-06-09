using FractalExplorer.Engines;

namespace FractalExplorer.Utilities.SaveIO.SaveStateImplementations
{
    /// <summary>
    /// Представляет состояние сохранения для фрактала Серпинского.
    /// Включает специфические параметры рендеринга и цветовой схемы для фрактала Серпинского.
    /// </summary>
    public class SerpinskySaveState : FractalSaveStateBase
    {
        /// <summary>
        /// Получает или задает режим рендеринга фрактала Серпинского (например, геометрический или хаотический).
        /// </summary>
        public SerpinskyRenderMode RenderMode { get; set; }

        /// <summary>
        /// Получает или задает режим определения цвета фрактала Серпинского.
        /// </summary>
        public SerpinskyColorMode ColorMode { get; set; }

        /// <summary>
        /// Получает или задает количество итераций, используемых для генерации фрактала.
        /// </summary>
        public int Iterations { get; set; }

        /// <summary>
        /// Получает или задает коэффициент масштабирования (зум) фрактала.
        /// </summary>
        public double Zoom { get; set; }

        /// <summary>
        /// Получает или задает X-координату центра области просмотра фрактала.
        /// </summary>
        public double CenterX { get; set; }

        /// <summary>
        /// Получает или задает Y-координату центра области просмотра фрактала.
        /// </summary>
        public double CenterY { get; set; }

        /// <summary>
        /// Получает или задает основной цвет, используемый для отрисовки самого фрактала.
        /// </summary>
        public Color FractalColor { get; set; }

        /// <summary>
        /// Получает или задает цвет фона области фрактала.
        /// </summary>
        public Color BackgroundColor { get; set; }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="SerpinskySaveState"/>.
        /// Этот конструктор необходим для корректной десериализации JSON.
        /// </summary>
        public SerpinskySaveState()
        {
            // Пустой конструктор необходим для корректной десериализации JSON.
        }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="SerpinskySaveState"/>
        /// с указанным идентификатором типа фрактала.
        /// </summary>
        /// <param name="fractalType">Строковый идентификатор типа фрактала (например, "Serpinsky").</param>
        public SerpinskySaveState(string fractalType)
        {
            FractalType = fractalType;
        }
    }
}