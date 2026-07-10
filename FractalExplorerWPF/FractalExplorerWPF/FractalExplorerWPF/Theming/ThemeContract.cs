using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace FractalExplorerWPF.Theming;

/// <summary>
/// Safety net for migrated and newly added UI. Windows are themed automatically; intentional
/// render/color surfaces can opt out of the audit with IgnoreAudit="True".
/// </summary>
public static class ThemeContract
{
    public static readonly DependencyProperty ExcludeProperty = DependencyProperty.RegisterAttached(
        "Exclude", typeof(bool), typeof(ThemeContract), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));

    public static readonly DependencyProperty IgnoreAuditProperty = DependencyProperty.RegisterAttached(
        "IgnoreAudit", typeof(bool), typeof(ThemeContract), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));

    public static bool GetExclude(DependencyObject target) => (bool)target.GetValue(ExcludeProperty);
    public static void SetExclude(DependencyObject target, bool value) => target.SetValue(ExcludeProperty, value);
    public static bool GetIgnoreAudit(DependencyObject target) => (bool)target.GetValue(IgnoreAuditProperty);
    public static void SetIgnoreAudit(DependencyObject target, bool value) => target.SetValue(IgnoreAuditProperty, value);

    internal static void ScheduleAudit(Window window)
    {
#if DEBUG
        window.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, () => Audit(window));
#endif
    }

    [Conditional("DEBUG")]
    private static void Audit(Window window)
    {
        var violations = new List<string>();
        Visit(window, violations);
        if (violations.Count == 0) return;

        Debug.WriteLine($"[Theme audit] {window.GetType().Name}: найдены локальные цвета, которые не меняются вместе с темой:");
        foreach (string violation in violations.Distinct()) Debug.WriteLine($"  - {violation}");
        Debug.WriteLine("Используйте {DynamicResource Theme.*Brush} либо ThemeContract.IgnoreAudit=True для намеренной цветовой поверхности.");
    }

    private static void Visit(DependencyObject element, List<string> violations)
    {
        if (GetIgnoreAudit(element)) return;

        switch (element)
        {
            case Control control:
                Check(control, Control.BackgroundProperty, "Background", violations);
                Check(control, Control.ForegroundProperty, "Foreground", violations);
                Check(control, Control.BorderBrushProperty, "BorderBrush", violations);
                break;
            case Border border:
                Check(border, Border.BackgroundProperty, "Background", violations);
                Check(border, Border.BorderBrushProperty, "BorderBrush", violations);
                break;
            case Panel panel:
                Check(panel, Panel.BackgroundProperty, "Background", violations);
                break;
            case TextBlock text:
                Check(text, TextBlock.ForegroundProperty, "Foreground", violations);
                break;
        }

        int children = VisualTreeHelper.GetChildrenCount(element);
        for (int index = 0; index < children; index++) Visit(VisualTreeHelper.GetChild(element, index), violations);
    }

    private static void Check(DependencyObject element, DependencyProperty property, string propertyName, List<string> violations)
    {
        ValueSource source = DependencyPropertyHelper.GetValueSource(element, property);
        if (source.BaseValueSource != BaseValueSource.Local || source.IsExpression) return;
        if (element.GetValue(property) is not SolidColorBrush) return;

        string name = element is FrameworkElement { Name.Length: > 0 } frameworkElement
            ? frameworkElement.Name
            : element.GetType().Name;
        violations.Add($"{name}.{propertyName}");
    }
}
