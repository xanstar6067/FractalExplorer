using System.Windows;
using FractalExplorerWPF.Infrastructure;
using FractalExplorerWPF.Models;

namespace FractalExplorerWPF.Views;

public partial class NewtonSavesWindow : Window
{
    private readonly SaveManagerController<NewtonState> _controller;

    public NewtonSavesWindow(NewtonPoolsWindow fractalWindow, NewtonSaveStore store)
    {
        InitializeComponent();
        _controller = new SaveManagerController<NewtonState>(this, Manager, new SaveManagerConfiguration<NewtonState>
        {
            WindowTitle = "Сохранение/Загрузка: Бассейны Ньютона",
            FractalIdentifier = "NewtonPools",
            LoadStates = store.Load,
            SaveStates = store.Save,
            CaptureState = fractalWindow.CaptureState,
            LoadState = fractalWindow.LoadState,
            RenderPreviewAsync = fractalWindow.RenderStatePreviewAsync,
            GetName = state => state.SaveName,
            GetTimestamp = state => state.Timestamp,
            GetDetails = state => $"{state.Timestamp:g} · Метод: {state.IterationMethod} · Итерации: {state.MaxIterations}\n" +
                                        $"Формула: {state.Formula} · Масштаб: {state.Zoom:0.####}"
        });
        Closed += (_, _) => _controller.Dispose();
    }
}
