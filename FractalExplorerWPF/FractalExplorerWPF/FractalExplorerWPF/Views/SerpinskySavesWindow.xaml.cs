using System.Windows;
using FractalExplorerWPF.Infrastructure;
using FractalExplorerWPF.Models;

namespace FractalExplorerWPF.Views;

public partial class SerpinskySavesWindow : Window
{
    private readonly SaveManagerController<SerpinskySaveState> _controller;

    public SerpinskySavesWindow(SerpinskyWindow fractalWindow, SerpinskySaveStore store)
    {
        InitializeComponent();
        _controller = new SaveManagerController<SerpinskySaveState>(this, Manager, new SaveManagerConfiguration<SerpinskySaveState>
        {
            WindowTitle = "Сохранение/Загрузка: Серпинский",
            FractalIdentifier = "Serpinsky",
            LoadStates = store.Load,
            SaveStates = store.Save,
            CaptureState = fractalWindow.CaptureState,
            LoadState = fractalWindow.LoadState,
            RenderPreviewAsync = fractalWindow.RenderStatePreviewAsync,
            GetName = state => state.SaveName,
            GetTimestamp = state => state.Timestamp,
            GetDetails = state => $"{state.Timestamp:g} · Режим: {state.RenderMode}\n" +
                                        $"Итерации: {state.Iterations} · Масштаб: {state.Zoom:0.####}"
        });
        Closed += (_, _) => _controller.Dispose();
    }
}
