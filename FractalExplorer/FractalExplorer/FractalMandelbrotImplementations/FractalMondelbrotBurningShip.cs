using FractalDraving;
using FractalExplorer.Engines;
using FractalExplorer.Utilities;
using FractalExplorer.Utilities.SaveStateImplementations;

namespace FractalExplorer.Projects
{
    /// <summary>
    /// Форма для отображения и взаимодействия с множеством "Пылающий корабль" Мандельброта.
    /// Является конкретной реализацией базовой формы фрактала.
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
        protected override decimal InitialCenterX => -0.25m;

        /// <summary>
        /// Получает начальную координату Y центра для фрактала "Пылающий корабль" Мандельброта.
        /// </summary>
        protected override decimal InitialCenterY => -0.5m;

        /// <summary>
        /// Вызывается после завершения инициализации формы.
        /// Скрывает элементы управления, которые не требуются для данного фрактала
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
        /// специфичную для множества "Пылающий корабль" Мандельброта.
        /// </summary>
        /// <returns>Строка, содержащая информацию о фрактале для имени файла.</returns>
        protected override string GetSaveFileNameDetails()
        {
            return "burningship_mandelbrot";
        }

        #endregion

        #region ISaveLoadCapableFractal Overrides

        public override string FractalTypeIdentifier => "MandelbrotBurningShip";

        public override Type ConcreteSaveStateType => typeof(MandelbrotFamilySaveState);

        public override List<FractalSaveStateBase> LoadAllSavesForThisType()
        {
            var specificSaves = SaveFileManager.LoadSaves<MandelbrotFamilySaveState>(this.FractalTypeIdentifier);
            return specificSaves.Cast<FractalSaveStateBase>().ToList();
        }

        public override void SaveAllSavesForThisType(List<FractalSaveStateBase> saves)
        {
            var specificSaves = saves.Cast<MandelbrotFamilySaveState>().ToList();
            SaveFileManager.SaveSaves(this.FractalTypeIdentifier, specificSaves);
        }

        #endregion
    }
}