using System.Windows;
using FractalExplorerWPF.Infrastructure;
using FractalExplorerWPF.Models;

namespace FractalExplorerWPF.Views;

public partial class FlameSavesWindow : Window
{
    private readonly SaveManagerController<FlameState> _controller;

    public FlameSavesWindow(FlameWindow window, FlameSaveStore store)
    {
        InitializeComponent();
        _controller = new SaveManagerController<FlameState>(this, Manager, new SaveManagerConfiguration<FlameState>
        {
            WindowTitle = "Сохранение/Загрузка: Flame",
            FractalIdentifier = "Flame",
            LoadStates = store.Load,
            SaveStates = store.Save,
            CaptureState = window.CaptureState,
            LoadState = state => window.LoadState(state.Clone()),
            RenderPreviewAsync = (state, width, height, token) => window.RenderStatePreviewAsync(state.Clone(), width, height, token),
            GetName = state => state.SaveName,
            GetTimestamp = state => state.Timestamp,
            GetDetails = state => $"{state.Timestamp:g} · {state.Samples:N0} сэмплов · {state.IterationsPerSample} итераций\n" +
                                        $"Трансформаций: {state.Transforms.Count} · Экспозиция: {state.Exposure:F2} · Гамма: {state.Gamma:F2}"
        });
        Closed += (_, _) => _controller.Dispose();
    }
}
