namespace FractalExplorer.Utilities.SaveIO.SaveStateImplementations
{
    /// <summary>
    /// Представляет базовый класс для сохранения состояния любого типа фрактала.
    /// Содержит общие метаданные и параметры для генерации предварительного просмотра.
    /// </summary>
    public abstract class FractalSaveStateBase
    {
        /// <summary>
        /// Получает или задает имя сохранения, выбранное пользователем.
        /// </summary>
        public string SaveName { get; set; }

        /// <summary>
        /// Получает или задает метку времени, когда было создано это состояние сохранения.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Получает строковый идентификатор типа фрактала, к которому относится это сохранение.
        /// Это свойство устанавливается в конструкторе производного класса.
        /// </summary>
        public string FractalType { get; protected set; }

        /// <summary>
        /// Получает или задает JSON-строку, содержащую параметры, необходимые для быстрого рендеринга
        /// миниатюры или предварительного просмотра фрактала без загрузки полного состояния.
        /// </summary>
        public string PreviewParametersJson { get; set; }
    }
}
