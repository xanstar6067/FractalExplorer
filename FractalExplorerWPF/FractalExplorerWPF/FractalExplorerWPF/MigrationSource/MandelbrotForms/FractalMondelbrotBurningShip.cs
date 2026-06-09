using FractalDraving;
using FractalExplorer.Engines;
using FractalExplorer.Utilities.SaveIO;
using FractalExplorer.Utilities.SaveIO.SaveStateImplementations;

namespace FractalExplorer.Projects
{
    /// <summary>
    /// Форма для отображения и взаимодействия с множеством "Пылающий корабль" Мандельброта.
    /// Является конкретной реализацией базовой формы фрактала,
    /// предназначенной для отображения и управления параметрами фрактала "Пылающий корабль".
    /// </summary>
    public partial class FractalMondelbrotBurningShip : FractalMandelbrotFamilyForm
    {
        #region Constructor

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="FractalMondelbrotBurningShip"/>.
        /// Устанавливает заголовок формы.
        /// </summary>
        public FractalMondelbrotBurningShip()
        {
            Text = "Множество Горящий Корабль";
        }

        #endregion

        #region Fractal Engine Overrides

        /// <summary>
        /// Создает и возвращает экземпляр движка для рендеринга множества "Пылающий корабль" Мандельброта.
        /// </summary>
        /// <returns>Экземпляр <see cref="MandelbrotBurningShipEngine"/>.</returns>
        protected override FractalMandelbrotFamilyEngine CreateEngine()
        {
            return new MandelbrotBurningShipEngine();
        }

        /// <summary>
        /// Получает начальную координату X центра для фрактала "Пылающий корабль" Мандельброта.
        /// </summary>
        // Это значение было выбрано как хороший центр для начального отображения фрактала "Пылающий корабль".
        protected override decimal InitialCenterX => 0.0m;

        /// <summary>
        /// Получает начальную координату Y центра для фрактала "Пылающий корабль" Мандельброта.
        /// </summary>
        // Это значение было выбрано как хороший центр для начального отображения фрактала "Пылающий корабль".
        protected override decimal InitialCenterY => 0.5m;

        /// <summary>
        /// Вызывается после завершения инициализации формы.
        /// Скрывает элементы управления, которые не требуются для данного фрактала,
        /// такие как поля для константы 'C' и панель предпросмотра, поскольку
        /// они специфичны для фракталов Жюлиа.
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
        /// специфичную для множества "Пылающий корабль" Мандельброта.
        /// </summary>
        /// <returns>Строка, содержащая информацию о фрактале для имени файла.</returns>
        protected override string GetSaveFileNameDetails()
        {
            return "burningship_mandelbrot";
        }

        #endregion

        #region ISaveLoadCapableFractal Overrides

        /// <summary>
        /// Получает строковый идентификатор типа фрактала.
        /// </summary>
        public override string FractalTypeIdentifier => "MandelbrotBurningShip";

        /// <summary>
        /// Получает конкретный тип состояния сохранения, используемый для множества "Пылающий корабль" Мандельброта.
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