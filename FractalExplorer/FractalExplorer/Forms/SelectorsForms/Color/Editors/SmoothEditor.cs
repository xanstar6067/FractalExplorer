using FractalExplorer.Utilities.SaveIO;
using FractalExplorer.Utilities.SaveIO.ColorPalettes;

namespace FractalExplorer.Utilities
{
    /// <summary>
    /// Базовый редактор для плавного режима окрашивания.
    /// </summary>
    public sealed class SmoothEditor : UserControl, IColorModeEditor
    {
        private readonly Label _descriptionLabel;

        public SmoothEditor()
        {
            _descriptionLabel = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Text = "Плавный режим: дополнительных параметров пока нет."
            };

            Controls.Add(_descriptionLabel);
        }

        public void LoadFromPalette(Palette? palette)
        {
            // Зарезервировано под будущие параметры режима.
        }

        public void ApplyToPalette(Palette? palette)
        {
            // Зарезервировано под будущие параметры режима.
        }

        public void SaveChanges(PaletteManager paletteManager)
        {
            // Зарезервировано под будущие параметры режима.
        }
    }
}
