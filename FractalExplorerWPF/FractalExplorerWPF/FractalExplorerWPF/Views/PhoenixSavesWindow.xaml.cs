using System.Windows;
using FractalExplorerWPF.Infrastructure;
using FractalExplorerWPF.Models;

namespace FractalExplorerWPF.Views;

public partial class PhoenixSavesWindow : Window
{
    private readonly SaveManagerController<PhoenixState> _controller;

    public PhoenixSavesWindow(PhoenixWindow window, PhoenixSaveStore store)
    {
        InitializeComponent();
        _controller = new SaveManagerController<PhoenixState>(this, Manager, new SaveManagerConfiguration<PhoenixState>
        {
            WindowTitle = "Сохранение/Загрузка: Феникс",
            FractalIdentifier = "Phoenix",
            LoadStates = store.Load,
            SaveStates = store.Save,
            CaptureState = window.CaptureState,
            LoadState = window.LoadState,
            RenderPreviewAsync = window.RenderStatePreviewAsync,
            GetName = state => state.SaveName,
            GetTimestamp = state => state.Timestamp,
            GetDetails = state => $"{state.Timestamp:g} · Итерации: {state.Iterations} · Масштаб: {state.Zoom:G8}\n" +
                                        $"C1: {state.C1Real:G8} {(state.C1Imaginary < 0 ? '−' : '+')} {Math.Abs(state.C1Imaginary):G8}i"
        });
        Closed += (_, _) => _controller.Dispose();
    }
}
