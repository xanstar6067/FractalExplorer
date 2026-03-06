using System.Drawing;

namespace FractalExplorer.Utilities.Theme
{
    public enum AppTheme
    {
        DarkModernLabBlue,
        DarkModernLabViolet,
        Light
    }

    public sealed class ThemeDefinition
    {
        public required Color BaseBackground { get; init; }
        public required Color PanelBackground { get; init; }
        public required Color ControlBackground { get; init; }
        public required Color PrimaryText { get; init; }
        public required Color SecondaryText { get; init; }
        public required Color AccentPrimary { get; init; }
        public required Color AccentSecondary { get; init; }
        public required Color HoverBackground { get; init; }
        public required Color PressedBackground { get; init; }
        public required Color BorderColor { get; init; }
        public required Color InputBorderColor { get; init; }
    }
}
