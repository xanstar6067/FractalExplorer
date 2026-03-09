using System.Drawing;

namespace FractalExplorer.Utilities.Theme
{
    public static class BuiltInThemeProvider
    {
        public const string DefaultThemeId = "dark-modern-lab-green";

        public static IReadOnlyList<ThemeDefinition> GetThemes()
        {
            return new List<ThemeDefinition>
            {
                new ThemeDefinition
                {
                    Id = DefaultThemeId,
                    DisplayName = "Тёмная (зелёная)",
                    IsBuiltIn = true,
                    BaseBackground = Color.FromArgb(18, 18, 18),
                    PanelBackground = Color.FromArgb(24, 34, 28),
                    ControlBackground = Color.FromArgb(34, 48, 39),
                    PrimaryText = Color.FromArgb(236, 242, 236),
                    SecondaryText = Color.FromArgb(170, 188, 172),
                    AccentPrimary = Color.FromArgb(76, 175, 80),
                    AccentSecondary = Color.FromArgb(139, 195, 74),
                    HoverBackground = Color.FromArgb(48, 66, 54),
                    PressedBackground = Color.FromArgb(61, 83, 68),
                    BorderColor = Color.FromArgb(72, 98, 79),
                    InputBorderColor = Color.FromArgb(90, 118, 98)
                },
                new ThemeDefinition
                {
                    Id = "dark-modern-lab-blue",
                    DisplayName = "Тёмная (синяя)",
                    IsBuiltIn = true,
                    BaseBackground = Color.FromArgb(18, 18, 18),
                    PanelBackground = Color.FromArgb(28, 30, 36),
                    ControlBackground = Color.FromArgb(36, 40, 48),
                    PrimaryText = Color.FromArgb(236, 240, 245),
                    SecondaryText = Color.FromArgb(168, 176, 188),
                    AccentPrimary = Color.FromArgb(38, 132, 255),
                    AccentSecondary = Color.FromArgb(111, 86, 205),
                    HoverBackground = Color.FromArgb(54, 61, 74),
                    PressedBackground = Color.FromArgb(72, 80, 96),
                    BorderColor = Color.FromArgb(64, 74, 92),
                    InputBorderColor = Color.FromArgb(82, 95, 118)
                },
                new ThemeDefinition
                {
                    Id = "dark-modern-lab-violet",
                    DisplayName = "Тёмная (фиолетовая)",
                    IsBuiltIn = true,
                    BaseBackground = Color.FromArgb(18, 18, 18),
                    PanelBackground = Color.FromArgb(30, 26, 38),
                    ControlBackground = Color.FromArgb(42, 36, 52),
                    PrimaryText = Color.FromArgb(236, 240, 245),
                    SecondaryText = Color.FromArgb(176, 168, 188),
                    AccentPrimary = Color.FromArgb(111, 86, 205),
                    AccentSecondary = Color.FromArgb(38, 132, 255),
                    HoverBackground = Color.FromArgb(60, 50, 76),
                    PressedBackground = Color.FromArgb(78, 65, 95),
                    BorderColor = Color.FromArgb(88, 74, 110),
                    InputBorderColor = Color.FromArgb(105, 90, 132)
                },
                new ThemeDefinition
                {
                    Id = "light-warm",
                    DisplayName = "Тёплая",
                    IsBuiltIn = true,
                    BaseBackground = Color.FromArgb(255, 249, 238),
                    PanelBackground = Color.FromArgb(255, 242, 214),
                    ControlBackground = Color.FromArgb(255, 252, 244),
                    PrimaryText = Color.FromArgb(79, 57, 24),
                    SecondaryText = Color.FromArgb(130, 99, 57),
                    AccentPrimary = Color.FromArgb(236, 214, 174),
                    AccentSecondary = Color.FromArgb(220, 183, 122),
                    HoverBackground = Color.FromArgb(255, 234, 194),
                    PressedBackground = Color.FromArgb(247, 214, 162),
                    BorderColor = Color.FromArgb(220, 183, 122),
                    InputBorderColor = Color.FromArgb(207, 165, 102)
                },
                new ThemeDefinition
                {
                    Id = "light-fire",
                    DisplayName = "Огненная",
                    IsBuiltIn = true,
                    BaseBackground = Color.FromArgb(255, 246, 238),
                    PanelBackground = Color.FromArgb(255, 227, 204),
                    ControlBackground = Color.FromArgb(255, 250, 243),
                    PrimaryText = Color.FromArgb(88, 45, 21),
                    SecondaryText = Color.FromArgb(140, 85, 56),
                    AccentPrimary = Color.FromArgb(235, 194, 165),
                    AccentSecondary = Color.FromArgb(224, 157, 119),
                    HoverBackground = Color.FromArgb(255, 216, 184),
                    PressedBackground = Color.FromArgb(247, 197, 163),
                    BorderColor = Color.FromArgb(224, 157, 119),
                    InputBorderColor = Color.FromArgb(209, 134, 92)
                },
                new ThemeDefinition
                {
                    Id = "light",
                    DisplayName = "Светлая",
                    IsBuiltIn = true,
                    BaseBackground = Color.FromArgb(245, 246, 248),
                    PanelBackground = Color.FromArgb(233, 235, 240),
                    ControlBackground = Color.FromArgb(255, 255, 255),
                    PrimaryText = Color.FromArgb(34, 36, 42),
                    SecondaryText = Color.FromArgb(84, 92, 106),
                    AccentPrimary = Color.FromArgb(196, 203, 216),
                    AccentSecondary = Color.FromArgb(176, 185, 201),
                    HoverBackground = Color.FromArgb(222, 226, 234),
                    PressedBackground = Color.FromArgb(205, 211, 223),
                    BorderColor = Color.FromArgb(176, 185, 201),
                    InputBorderColor = Color.FromArgb(158, 168, 188)
                }
            };
        }
    }
}
