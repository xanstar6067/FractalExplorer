using System.Runtime.InteropServices;
using System.Windows.Media;
using Microsoft.Win32;
using Color = System.Windows.Media.Color;

namespace FractalExplorerWPF.Theming;

public sealed class WindowsThemeImporter
{
    private const string DwmRegistryPath = @"Software\Microsoft\Windows\DWM";
    private const string PersonalizeRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

    public bool TryBuildThemeFromWindows(out ThemeDefinition theme, out string error)
    {
        ThemeDefinition fallback = ThemeManager.CurrentDefinition;
        bool hasMode = TryReadAppsUseLightTheme(out bool light, out string modeError);
        bool hasAccent = TryReadSystemAccentColor(out Color accent, out string accentError);

        if (!hasMode) light = IsLight(fallback.BaseBackground);
        if (!hasAccent) accent = fallback.AccentPrimary;

        ColorScheme colors = BuildColorScheme(light, accent);
        theme = new ThemeDefinition
        {
            Id = "windows-system",
            DisplayName = "Windows (системная)",
            IsBuiltIn = false,
            BaseBackground = colors.BaseBackground,
            PanelBackground = colors.PanelBackground,
            ControlBackground = colors.ControlBackground,
            PrimaryText = colors.PrimaryText,
            SecondaryText = colors.SecondaryText,
            AccentPrimary = colors.AccentPrimary,
            AccentSecondary = colors.AccentSecondary,
            HoverBackground = colors.HoverBackground,
            PressedBackground = colors.PressedBackground,
            BorderColor = colors.BorderColor,
            InputBorderColor = colors.InputBorderColor
        };

        error = string.Join(" ", new[] { hasMode ? null : modeError, hasAccent ? null : accentError }
            .Where(value => !string.IsNullOrWhiteSpace(value)));
        return hasMode || hasAccent;
    }

    private static bool TryReadSystemAccentColor(out Color color, out string error)
    {
        if (TryReadDwmAccentColor(out color) || TryReadRegistryAccentColor(out color))
        {
            error = string.Empty;
            return true;
        }

        error = "Не удалось прочитать акцентный цвет Windows из DWM или реестра.";
        return false;
    }

    private static bool TryReadDwmAccentColor(out Color color)
    {
        color = default;
        try
        {
            if (DwmGetColorizationColor(out uint raw, out _) != 0) return false;
            color = FromArgb(raw);
            return true;
        }
        catch { return false; }
    }

    private static bool TryReadRegistryAccentColor(out Color color)
    {
        color = default;
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(DwmRegistryPath, false);
            object? value = key?.GetValue("ColorizationColor");
            if (value is int integer) color = FromArgb(unchecked((uint)integer));
            else if (value is long longInteger) color = FromArgb(unchecked((uint)longInteger));
            else return false;
            return true;
        }
        catch { return false; }
    }

    private static bool TryReadAppsUseLightTheme(out bool useLightTheme, out string error)
    {
        useLightTheme = false;
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(PersonalizeRegistryPath, false);
            object? value = key?.GetValue("AppsUseLightTheme");
            if (value is null) throw new InvalidOperationException();
            useLightTheme = Convert.ToInt32(value) != 0;
            error = string.Empty;
            return true;
        }
        catch
        {
            error = "Не удалось прочитать режим приложений Windows.";
            return false;
        }
    }

    private static ColorScheme BuildColorScheme(bool light, Color accent)
    {
        accent = Color.FromRgb(accent.R, accent.G, accent.B);
        if (light)
        {
            Color background = Color.FromRgb(246, 247, 250);
            return new ColorScheme(
                background, Blend(background, accent, .10), Blend(Colors.White, accent, .05),
                Color.FromRgb(34, 36, 42), Color.FromRgb(86, 93, 108), accent,
                AdjustBrightness(accent, -.16), Blend(Colors.White, accent, .22),
                Blend(Colors.White, accent, .34), Blend(background, accent, .24),
                Blend(background, accent, .35));
        }

        Color dark = Color.FromRgb(20, 22, 26);
        Color control = Blend(dark, accent, .22);
        return new ColorScheme(
            dark, Blend(dark, accent, .13), control, Color.FromRgb(236, 239, 244),
            Color.FromRgb(166, 175, 191), accent, AdjustBrightness(accent, .20),
            Blend(control, accent, .24), Blend(control, accent, .38),
            Blend(dark, accent, .30), Blend(dark, accent, .42));
    }

    private static Color AdjustBrightness(Color color, double amount) =>
        amount >= 0 ? Blend(color, Colors.White, amount) : Blend(color, Colors.Black, -amount);

    private static Color Blend(Color first, Color second, double weight)
    {
        weight = Math.Clamp(weight, 0, 1);
        return Color.FromRgb(
            (byte)Math.Round(first.R + (second.R - first.R) * weight),
            (byte)Math.Round(first.G + (second.G - first.G) * weight),
            (byte)Math.Round(first.B + (second.B - first.B) * weight));
    }

    private static bool IsLight(Color color) => .2126 * color.R + .7152 * color.G + .0722 * color.B >= 140;

    private static Color FromArgb(uint raw)
    {
        byte alpha = (byte)(raw >> 24);
        return Color.FromArgb(alpha == 0 ? (byte)255 : alpha, (byte)(raw >> 16), (byte)(raw >> 8), (byte)raw);
    }

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmGetColorizationColor(out uint colorizationColor, out bool opaqueBlend);

    private sealed record ColorScheme(
        Color BaseBackground, Color PanelBackground, Color ControlBackground,
        Color PrimaryText, Color SecondaryText, Color AccentPrimary, Color AccentSecondary,
        Color HoverBackground, Color PressedBackground, Color BorderColor, Color InputBorderColor);
}
