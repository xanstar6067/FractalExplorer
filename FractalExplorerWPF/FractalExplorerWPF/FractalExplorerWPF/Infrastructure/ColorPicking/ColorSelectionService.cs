using System.Windows;
using System.Windows.Media;
using FractalExplorerWPF.Views;
using Color = System.Windows.Media.Color;

namespace FractalExplorerWPF.Infrastructure.ColorPicking;

/// <summary>Common entry point for color selection throughout the WPF application.</summary>
public sealed class ColorSelectionService
{
    public static ColorSelectionService Default { get; } = new();

    public bool TrySelectColor(Window owner, Color initial, out Color selected)
    {
        var dialog = new ColorPickerWindow(initial) { Owner = owner };
        if (dialog.ShowDialog() != true)
        {
            selected = initial;
            return false;
        }

        selected = dialog.SelectedColor;
        return true;
    }
}
