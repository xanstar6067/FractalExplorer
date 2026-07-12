using System.Windows;
using FractalExplorerWPF.Infrastructure;
using FractalExplorerWPF.Models;

namespace FractalExplorerWPF.Views;

public partial class CollatzSavesWindow : Window
{
    private readonly SaveManagerController<CollatzState> _controller;

    public CollatzSavesWindow(CollatzWindow window, CollatzSaveStore store)
    {
        InitializeComponent();
        _controller = new SaveManagerController<CollatzState>(this, Manager, new SaveManagerConfiguration<CollatzState>
        {
            WindowTitle = "Сохранение/Загрузка: Коллатц",
            FractalIdentifier = "Collatz",
            LoadStates = store.Load,
            SaveStates = store.Save,
            CaptureState = window.CaptureState,
            LoadState = window.LoadState,
            RenderPreviewAsync = window.RenderStatePreviewAsync,
            GetName = state => state.SaveName,
            GetTimestamp = state => state.Timestamp,
            GetDetails = state => $"{state.Timestamp:g} · Режим: {VariationName(state.Variation)} · Итерации: {state.Iterations}\n" +
                                        $"Порог: {state.Threshold:G8} · Масштаб: {state.Zoom:G8}"
        });
        Closed += (_, _) => _controller.Dispose();
    }

    private static string VariationName(CollatzVariation variation) => variation switch
    {
        CollatzVariation.SineVariation => "Синусная",
        CollatzVariation.GeneralizedP => "Обобщённая p·x+1",
        _ => "Стандартная"
    };
}
