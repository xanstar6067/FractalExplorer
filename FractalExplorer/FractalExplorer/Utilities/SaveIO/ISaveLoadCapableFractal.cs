using FractalExplorer.Resources;
using FractalExplorer.Utilities.SaveIO.SaveStateImplementations;

namespace FractalExplorer.Utilities.SaveIO
{
    /// <summary>
    /// Определяет контракт для фрактальных форм, способных сохранять и загружать свое состояние.
    /// Предоставляет методы для управления сохранениями, включая получение текущего состояния,
    /// загрузку состояния, рендеринг предварительного просмотра и управление коллекциями сохранений.
    /// </summary>
    public interface ISaveLoadCapableFractal
    {
        /// <summary>
        /// Получает строковый идентификатор типа фрактала, используемый для различения сохранений.
        /// </summary>
        string FractalTypeIdentifier { get; }

        /// <summary>
        /// Получает конкретный тип объекта состояния сохранения, используемый этим фракталом.
        /// Это необходимо для корректной десериализации сохраненных данных.
        /// </summary>
        Type ConcreteSaveStateType { get; }

        /// <summary>
        /// Получает текущее состояние фрактала для сохранения.
        /// </summary>
        /// <param name="saveName">Имя, присваиваемое этому сохранению пользователем.</param>
        /// <returns>Базовый объект состояния фрактала, содержащий все необходимые параметры.</returns>
        FractalSaveStateBase GetCurrentStateForSave(string saveName);

        /// <summary>
        /// Загружает состояние фрактала из предоставленного объекта состояния.
        /// Применяет параметры из объекта состояния к текущей форме фрактала.
        /// </summary>
        /// <param name="state">Базовый объект состояния фрактала для загрузки.</param>
        void LoadState(FractalSaveStateBase state);

        /// <summary>
        /// Загружает все сохраненные состояния для данного типа фрактала.
        /// </summary>
        /// <returns>Список базовых объектов состояний фрактала, относящихся к текущему типу.</returns>
        List<FractalSaveStateBase> LoadAllSavesForThisType();

        /// <summary>
        /// Сохраняет предоставленный список состояний фрактала для данного типа.
        /// Этот метод используется для сохранения всех текущих сохранений фрактала после изменений (например, добавления/удаления).
        /// </summary>
        /// <param name="saves">Список базовых объектов состояний фрактала для сохранения.</param>
        void SaveAllSavesForThisType(List<FractalSaveStateBase> saves);
    }
}