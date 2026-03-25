using FractalExplorer.Utilities.SaveIO.ColorPalettes;

namespace FractalExplorer.Utilities.SaveIO
{
    /// <summary>
    /// Контракт редактора расширенных параметров конкретного цветового режима.
    /// </summary>
    public interface IColorModeEditor
    {
        /// <summary>
        /// Загружает состояние редактора из выбранной палитры.
        /// </summary>
        /// <param name="palette">Текущая выбранная палитра.</param>
        void LoadFromPalette(Palette? palette);

        /// <summary>
        /// Применяет изменения редактора к выбранной палитре перед кнопкой Apply.
        /// </summary>
        /// <param name="palette">Текущая выбранная палитра.</param>
        void ApplyToPalette(Palette? palette);

        /// <summary>
        /// Сохраняет изменения редактора перед сохранением пользовательских палитр.
        /// </summary>
        /// <param name="paletteManager">Менеджер палитр.</param>
        void SaveChanges(PaletteManager paletteManager);
    }
}
