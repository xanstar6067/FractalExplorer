using FractalDraving;
using FractalExplorer.Engines;
using FractalExplorer.Utilities.SaveIO;
using FractalExplorer.Utilities.SaveIO.SaveStateImplementations;

namespace FractalExplorer.Projects
{
    /// <summary>
    /// Форма для отображения и взаимодействия с множеством Мандельброта.
    /// Является конкретной реализацией базовой формы фрактала для отображения
    /// и управления параметрами множества Мандельброта.
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
        /// Скрывает элементы управления, которые не требуются для множества Мандельброта,
        /// поскольку для него не нужна константа 'C' и связанная с ней панель предпросмотра.
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

        #region ISaveLoadCapableFractal Overrides

        /// <summary>
        /// Получает строковый идентификатор типа фрактала.
        /// </summary>
        public override string FractalTypeIdentifier => "Mandelbrot";

        /// <summary>
        /// Получает конкретный тип состояния сохранения, используемый для множества Мандельброта.
        /// </summary>
        public override Type ConcreteSaveStateType => typeof(MandelbrotFamilySaveState);

        /// <summary>
        /// Загружает все сохраненные состояния фрактала для данного типа.
        /// </summary>
        /// <returns>Список базовых объектов состояний фрактала.</returns>
        public override List<FractalSaveStateBase> LoadAllSavesForThisType()
        {
            var specificSaves = SaveFileManager.LoadSaves<MandelbrotFamilySaveState>(this.FractalTypeIdentifier);
            // Преобразуем список конкретного типа в список базового типа для соответствия интерфейсу.
            return specificSaves.Cast<FractalSaveStateBase>().ToList();
        }

        /// <summary>
        /// Сохраняет предоставленный список состояний фрактала для данного типа.
        /// </summary>
        /// <param name="saves">Список базовых объектов состояний фрактала для сохранения.</param>
        public override void SaveAllSavesForThisType(List<FractalSaveStateBase> saves)
        {
            // Преобразуем список базового типа обратно в список конкретного типа перед сохранением,
            // так как SaveFileManager ожидает конкретный тип для сериализации.
            var specificSaves = saves.Cast<MandelbrotFamilySaveState>().ToList();
            SaveFileManager.SaveSaves(this.FractalTypeIdentifier, specificSaves);
        }

        #endregion
    }
}