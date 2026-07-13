using System.Windows;
using System.Windows.Controls;

namespace FractalExplorerWPF.Controls;

public static class FractalControlPanel
{
    public static void Toggle(
        ref bool isVisible,
        ColumnDefinition column,
        FrameworkElement panel,
        Button button,
        double expandedWidth,
        Action? layoutChanged = null)
    {
        isVisible = !isVisible;
        column.Width = isVisible ? new GridLength(expandedWidth) : new GridLength(0);
        panel.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
        button.Content = isVisible ? "✕" : "☰";
        button.ToolTip = isVisible ? "Скрыть панель параметров" : "Показать панель параметров";
        layoutChanged?.Invoke();
    }
}
