using FractalDraving;
using FractalExplorer.Engines;

namespace FractalExplorer.Projects
{
    /// <summary>
    /// Форма для отображения и взаимодействия с множеством Мандельброта.
    /// Является конкретной реализацией базовой формы фрактала.
    /// </summary>
    public partial class FractalMondelbrot : FractalMandelbrotFamilyForm
    {
        #region Constructor

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="FractalMondelbrot"/>.
        /// Устанавливает заголовок формы.
        /// </summary>
        public FractalMondelbrot()
        {
            Text = "Множество Мандельброта";
        }

        #endregion

        #region Fractal Engine Overrides

        /// <summary>
        /// Создает и возвращает экземпляр движка для рендеринга множества Мандельброта.
        /// </summary>
        /// <returns>Экземпляр <see cref="MandelbrotEngine"/>.</returns>
        protected override FractalMandelbrotFamilyEngine CreateEngine()
        {
            return new MandelbrotEngine();
        }

        /// <summary>
        /// Вызывается после завершения инициализации формы.
        /// Скрывает элементы управления, которые не требуются для множества Мандельброта
        /// (например, поля для константы 'C' и панель предпросмотра).
        /// </summary>
        protected override void OnPostInitialize()
        {
            lblRe.Visible = false;
            nudRe.Visible = false;
            lblIm.Visible = false;
            nudIm.Visible = false;
            mandelbrotPreviewPanel.Visible = false;
        }

        /// <summary>
        /// Возвращает строку с деталями для имени файла сохранения,
        /// специфичную для множества Мандельброта.
        /// </summary>
        /// <returns>Строка, содержащая информацию о фрактале для имени файла.</returns>
        protected override string GetSaveFileNameDetails()
        {
            return "mandelbrot";
        }

        #endregion
    }
}