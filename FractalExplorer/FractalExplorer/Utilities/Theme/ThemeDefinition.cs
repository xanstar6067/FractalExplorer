using System.Drawing;

namespace FractalExplorer.Utilities.Theme
{
    public sealed class ThemeDefinition
    {
        public required string Id { get; init; }
        public required string DisplayName { get; init; }
        public required bool IsBuiltIn { get; init; }
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

        public ThemeDefinition CloneWith(string id, string displayName, bool? isBuiltIn = null)
        {
            return new ThemeDefinition
            {
                Id = id,
                DisplayName = displayName,
                IsBuiltIn = isBuiltIn ?? IsBuiltIn,
                BaseBackground = BaseBackground,
                PanelBackground = PanelBackground,
                ControlBackground = ControlBackground,
                PrimaryText = PrimaryText,
                SecondaryText = SecondaryText,
                AccentPrimary = AccentPrimary,
                AccentSecondary = AccentSecondary,
                HoverBackground = HoverBackground,
                PressedBackground = PressedBackground,
                BorderColor = BorderColor,
                InputBorderColor = InputBorderColor
            };
        }
    }
}
