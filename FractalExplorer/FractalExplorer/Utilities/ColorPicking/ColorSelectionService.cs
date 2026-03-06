using FractalExplorer.Forms.Common;
using System.Windows.Forms;

namespace FractalExplorer.Utilities.ColorPicking
{
    public sealed class ColorSelectionService
    {
        public static ColorSelectionService Default { get; } = new();

        public bool TrySelectColor(IWin32Window owner, Color initial, out Color selected)
        {
            using ColorPickerPanelForm colorPickerForm = new(initial);
            if (colorPickerForm.ShowDialog(owner) != DialogResult.OK)
            {
                selected = initial;
                return false;
            }

            selected = colorPickerForm.SelectedColor;
            return true;
        }
    }
}
