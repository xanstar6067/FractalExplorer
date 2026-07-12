using System.Windows;
using FractalExplorerWPF.Infrastructure;
using FractalExplorerWPF.Models;

namespace FractalExplorerWPF.Views;

public partial class MandelbrotSavesWindow : Window
{
    private readonly SaveManagerController<MandelbrotState> _controller;

    public MandelbrotSavesWindow(MandelbrotWindow fractalWindow, MandelbrotSaveStore store)
    {
        InitializeComponent();
        _controller = new SaveManagerController<MandelbrotState>(this, Manager, new SaveManagerConfiguration<MandelbrotState>
        {
            WindowTitle = $"Сохранение/Загрузка: {fractalWindow.SaveManagerDisplayName}",
            FractalIdentifier = fractalWindow.SaveManagerIdentifier,
            LoadStates = store.Load,
            SaveStates = store.Save,
            CaptureState = fractalWindow.CaptureState,
            LoadState = fractalWindow.LoadState,
            RenderPreviewAsync = fractalWindow.RenderStatePreviewAsync,
            GetName = state => state.SaveName,
            GetTimestamp = state => state.Timestamp,
            GetDetails = Describe
        });
        Closed += (_, _) => _controller.Dispose();
    }

    private static string Describe(MandelbrotState state)
    {
        string details = $"{state.Timestamp:g} · Итерации: {state.Iterations} · Масштаб: {state.Zoom:G6}\n" +
                         $"Центр: {state.CenterX:G8}; {state.CenterY:G8} · Палитра: {state.PaletteName} · {state.ColoringMode}";
        if (state.Variant is MandelbrotVariant.Julia or MandelbrotVariant.JuliaBurningShip)
            details += $"\nКонстанта C: {state.JuliaCReal:G10}; {state.JuliaCImaginary:G10}i";
        return details;
    }
}
