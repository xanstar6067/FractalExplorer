using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace FractalExplorer.Utilities.Theme;

public sealed class WindowsThemeImporter
{
    private const string DwmRegistryPath = @"Software\Microsoft\Windows\DWM";
    private const string PersonalizeRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

    public bool TryBuildThemeFromWindows(out ThemeDefinition theme, out string error)
    {
        ThemeDefinition fallback = ThemeManager.CurrentDefinition;

        bool hasLightThemeMode = TryReadAppsUseLightTheme(out bool useLightTheme, out string modeError);
        bool hasAccentColor = TryReadSystemAccentColor(out var accentColor, out string accentError);

        if (!hasLightThemeMode)
        {
            useLightTheme = IsLightColor(fallback.BaseBackground);
        }

        if (!hasAccentColor)
        {
            accentColor = fallback.AccentPrimary;
        }

        ColorScheme scheme = BuildColorScheme(useLightTheme, accentColor);

        theme = new ThemeDefinition
        {
            Id = "windows-system",
            DisplayName = "Windows (системная)",
            IsBuiltIn = false,
            BaseBackground = scheme.BaseBackground,
            PanelBackground = scheme.PanelBackground,
            ControlBackground = scheme.ControlBackground,
            PrimaryText = scheme.PrimaryText,
            SecondaryText = scheme.SecondaryText,
            AccentPrimary = scheme.AccentPrimary,
            AccentSecondary = scheme.AccentSecondary,
            HoverBackground = scheme.HoverBackground,
            PressedBackground = scheme.PressedBackground,
            BorderColor = scheme.BorderColor,
            InputBorderColor = scheme.InputBorderColor
        };

        List<string> errors = new(2);
        if (!hasLightThemeMode)
        {
            errors.Add(modeError);
        }

        if (!hasAccentColor)
        {
            errors.Add(accentError);
        }

        if (errors.Count == 0)
        {
            error = string.Empty;
            return true;
        }

        error = string.Join(" ", errors);
        return hasLightThemeMode || hasAccentColor;
    }

    private static bool TryReadSystemAccentColor(out Color accentColor, out string error)
    {
        if (TryReadDwmAccentColor(out accentColor))
        {
            error = string.Empty;
            return true;
        }

        if (TryReadRegistryAccentColor(out accentColor))
        {
            error = string.Empty;
            return true;
        }

        accentColor = Color.Empty;
        error = "Не удалось прочитать акцентный цвет Windows из DWM API или реестра.";
        return false;
    }

    private static bool TryReadDwmAccentColor(out Color accentColor)
    {
        accentColor = Color.Empty;

        try
        {
            int result = DwmGetColorizationColor(out uint rawColor, out _);
            if (result != 0)
            {
                return false;
            }

            accentColor = FromArgb(rawColor);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryReadRegistryAccentColor(out Color accentColor)
    {
        accentColor = Color.Empty;

        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(DwmRegistryPath, false);
            if (key?.GetValue("ColorizationColor") is int value)
            {
                accentColor = FromArgb(unchecked((uint)value));
                return true;
            }

            if (key?.GetValue("ColorizationColor") is long longValue)
            {
                accentColor = FromArgb(unchecked((uint)longValue));
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryReadAppsUseLightTheme(out bool useLightTheme, out string error)
    {
        useLightTheme = false;

        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(PersonalizeRegistryPath, false);
            object? value = key?.GetValue("AppsUseLightTheme");
            if (value is null)
            {
                error = "Не удалось прочитать AppsUseLightTheme из реестра Windows.";
                return false;
            }

            int intValue = Convert.ToInt32(value);
            useLightTheme = intValue != 0;
            error = string.Empty;
            return true;
        }
        catch
        {
            error = "Не удалось прочитать AppsUseLightTheme из реестра Windows.";
            return false;
        }
    }

    private static ColorScheme BuildColorScheme(bool isLightMode, Color accent)
    {
        if (isLightMode)
        {
            Color baseBackground = Color.FromArgb(246, 247, 250);
            Color panelBackground = Blend(baseBackground, accent, 0.10);
            Color controlBackground = Blend(Color.White, accent, 0.05);
            Color primaryText = Color.FromArgb(34, 36, 42);
            Color secondaryText = Color.FromArgb(86, 93, 108);
            Color accentSecondary = AdjustBrightness(accent, -0.16);
            Color hoverBackground = Blend(controlBackground, accent, 0.22);
            Color pressedBackground = Blend(controlBackground, accent, 0.34);
            Color borderColor = Blend(baseBackground, accent, 0.24);
            Color inputBorderColor = Blend(baseBackground, accent, 0.35);

            return new ColorScheme(
                baseBackground,
                panelBackground,
                controlBackground,
                primaryText,
                secondaryText,
                NormalizeAccent(accent),
                accentSecondary,
                hoverBackground,
                pressedBackground,
                borderColor,
                inputBorderColor);
        }

        Color darkBaseBackground = Color.FromArgb(20, 22, 26);
        Color darkPanelBackground = Blend(darkBaseBackground, accent, 0.13);
        Color darkControlBackground = Blend(darkBaseBackground, accent, 0.22);
        Color darkPrimaryText = Color.FromArgb(236, 239, 244);
        Color darkSecondaryText = Color.FromArgb(166, 175, 191);
        Color darkAccentSecondary = AdjustBrightness(accent, 0.20);
        Color darkHoverBackground = Blend(darkControlBackground, accent, 0.24);
        Color darkPressedBackground = Blend(darkControlBackground, accent, 0.38);
        Color darkBorderColor = Blend(darkBaseBackground, accent, 0.30);
        Color darkInputBorderColor = Blend(darkBaseBackground, accent, 0.42);

        return new ColorScheme(
            darkBaseBackground,
            darkPanelBackground,
            darkControlBackground,
            darkPrimaryText,
            darkSecondaryText,
            NormalizeAccent(accent),
            darkAccentSecondary,
            darkHoverBackground,
            darkPressedBackground,
            darkBorderColor,
            darkInputBorderColor);
    }

    private static Color NormalizeAccent(Color accent)
    {
        if (accent.A == byte.MaxValue)
        {
            return accent;
        }

        return Color.FromArgb(byte.MaxValue, accent.R, accent.G, accent.B);
    }

    private static Color AdjustBrightness(Color color, double amount)
    {
        amount = Math.Clamp(amount, -1d, 1d);

        return amount >= 0d
            ? Blend(color, Color.White, amount)
            : Blend(color, Color.Black, -amount);
    }

    private static Color Blend(Color baseColor, Color mixColor, double mixAmount)
    {
        double amount = Math.Clamp(mixAmount, 0d, 1d);
        int r = (int)Math.Round(baseColor.R + ((mixColor.R - baseColor.R) * amount));
        int g = (int)Math.Round(baseColor.G + ((mixColor.G - baseColor.G) * amount));
        int b = (int)Math.Round(baseColor.B + ((mixColor.B - baseColor.B) * amount));
        return Color.FromArgb(255, Math.Clamp(r, 0, 255), Math.Clamp(g, 0, 255), Math.Clamp(b, 0, 255));
    }

    private static bool IsLightColor(Color color)
    {
        double luminance = (0.2126 * color.R) + (0.7152 * color.G) + (0.0722 * color.B);
        return luminance >= 140d;
    }

    private static Color FromArgb(uint raw)
    {
        int a = (int)((raw >> 24) & 0xFF);
        int r = (int)((raw >> 16) & 0xFF);
        int g = (int)((raw >> 8) & 0xFF);
        int b = (int)(raw & 0xFF);

        if (a == 0)
        {
            a = 255;
        }

        return Color.FromArgb(a, r, g, b);
    }

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmGetColorizationColor(out uint pcrColorization, out bool pfOpaqueBlend);

    private sealed record ColorScheme(
        Color BaseBackground,
        Color PanelBackground,
        Color ControlBackground,
        Color PrimaryText,
        Color SecondaryText,
        Color AccentPrimary,
        Color AccentSecondary,
        Color HoverBackground,
        Color PressedBackground,
        Color BorderColor,
        Color InputBorderColor);
}
