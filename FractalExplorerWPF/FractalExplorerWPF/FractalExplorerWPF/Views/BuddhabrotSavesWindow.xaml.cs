using System.Windows;
using FractalExplorerWPF.Infrastructure;
using FractalExplorerWPF.Models;

namespace FractalExplorerWPF.Views;

public partial class BuddhabrotSavesWindow : Window
{
    private readonly SaveManagerController<BuddhabrotState> _controller;

    public BuddhabrotSavesWindow(BuddhabrotWindow window, BuddhabrotSaveStore store)
    {
        InitializeComponent();
        _controller = new SaveManagerController<BuddhabrotState>(this, Manager, new SaveManagerConfiguration<BuddhabrotState>
        {
            WindowTitle = "Сохранение/Загрузка: Буддаброт",
            FractalIdentifier = "Buddhabrot",
            LoadStates = store.Load,
            SaveStates = store.Save,
            CaptureState = window.CaptureState,
            LoadState = window.LoadState,
            RenderPreviewAsync = window.RenderStatePreviewAsync,
            GetName = state => state.SaveName,
            GetTimestamp = state => state.Timestamp,
            GetDetails = state => $"{state.Timestamp:g} · {state.RenderMode} · {state.SampleCount:N0} сэмплов\n" +
                                        $"Итерации: {state.MaxIterations} · Палитра: {state.Palette.Name}"
        });
        Closed += (_, _) => _controller.Dispose();
    }
}
