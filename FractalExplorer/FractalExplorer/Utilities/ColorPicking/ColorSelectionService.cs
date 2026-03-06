using System.Windows.Forms;

namespace FractalExplorer.Utilities.ColorPicking
{
    public enum ColorSelectionSource
    {
        StandardDialog,
        Eyedropper,
        Cancel
    }

    public interface IColorSelectionSourceSelector
    {
        ColorSelectionSource SelectSource(IWin32Window owner, Color initialColor);
    }

    public interface IColorSelectionHandler
    {
        bool TrySelectColor(IWin32Window owner, Color initialColor, out Color selectedColor);
    }

    public sealed class ColorSelectionService
    {
        private readonly IColorSelectionSourceSelector _sourceSelector;
        private readonly IReadOnlyDictionary<ColorSelectionSource, IColorSelectionHandler> _handlers;

        public static ColorSelectionService Default { get; } = new ColorSelectionService(
            new DefaultColorSelectionSourceSelector(),
            new Dictionary<ColorSelectionSource, IColorSelectionHandler>
            {
                { ColorSelectionSource.StandardDialog, new StandardDialogColorSelectionHandler() },
                { ColorSelectionSource.Eyedropper, new EyedropperColorSelectionHandler() }
            });

        public ColorSelectionService(
            IColorSelectionSourceSelector sourceSelector,
            IReadOnlyDictionary<ColorSelectionSource, IColorSelectionHandler> handlers)
        {
            _sourceSelector = sourceSelector;
            _handlers = handlers;
        }

        public bool TrySelectColor(IWin32Window owner, Color initial, out Color selected)
        {
            selected = initial;

            ColorSelectionSource source = _sourceSelector.SelectSource(owner, initial);
            if (source == ColorSelectionSource.Cancel)
            {
                return false;
            }

            if (!_handlers.TryGetValue(source, out IColorSelectionHandler? handler))
            {
                return false;
            }

            return handler.TrySelectColor(owner, initial, out selected);
        }
    }

    internal sealed class DefaultColorSelectionSourceSelector : IColorSelectionSourceSelector
    {
        public ColorSelectionSource SelectSource(IWin32Window owner, Color initialColor)
        {
            DialogResult result = MessageBox.Show(
                owner,
                "Выберите источник цвета:\n\nДа — стандартный диалог\nНет — пипетка\nОтмена — не изменять цвет",
                "Выбор источника цвета",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);

            return result switch
            {
                DialogResult.Yes => ColorSelectionSource.StandardDialog,
                DialogResult.No => ColorSelectionSource.Eyedropper,
                _ => ColorSelectionSource.Cancel
            };
        }
    }

    internal sealed class StandardDialogColorSelectionHandler : IColorSelectionHandler
    {
        public bool TrySelectColor(IWin32Window owner, Color initialColor, out Color selectedColor)
        {
            using ColorDialog colorDialog = new() { Color = initialColor };
            if (colorDialog.ShowDialog(owner) != DialogResult.OK)
            {
                selectedColor = initialColor;
                return false;
            }

            selectedColor = colorDialog.Color;
            return true;
        }
    }

    internal sealed class EyedropperColorSelectionHandler : IColorSelectionHandler
    {
        public bool TrySelectColor(IWin32Window owner, Color initialColor, out Color selectedColor)
        {
            MessageBox.Show(owner, "Режим пипетки пока не реализован.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
            selectedColor = initialColor;
            return false;
        }
    }
}
