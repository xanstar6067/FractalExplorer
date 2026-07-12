using System.Windows;
using FractalExplorerWPF.Infrastructure;
using FractalExplorerWPF.Models;

namespace FractalExplorerWPF.Views;

public partial class NovaSavesWindow : Window
{
    private readonly SaveManagerController<NovaState> _controller;

    public NovaSavesWindow(NovaWindow window, NovaSaveStore store, NovaVariant variant)
    {
        InitializeComponent();
        string variantName = variant == NovaVariant.Julia ? "Nova Julia" : "Nova Mandelbrot";
        _controller = new SaveManagerController<NovaState>(this, Manager, new SaveManagerConfiguration<NovaState>
        {
            WindowTitle = $"Сохранение/Загрузка: {variantName}",
            FractalIdentifier = variant == NovaVariant.Julia ? "NovaJulia" : "NovaMandelbrot",
            LoadStates = store.Load,
            SaveStates = store.Save,
            CaptureState = window.CaptureState,
            LoadState = window.LoadState,
            RenderPreviewAsync = window.RenderStatePreviewAsync,
            GetName = state => state.SaveName,
            GetTimestamp = state => state.Timestamp,
            GetDetails = Describe
        });
        Closed += (_, _) => _controller.Dispose();
    }

    private static string Describe(NovaState state)
    {
        string details = $"{state.Timestamp:g} · Итерации: {state.Iterations} · Масштаб: {state.Zoom:G8}\n" +
                         $"P: {Complex(state.PReal, state.PImaginary)} · Z₀: {Complex(state.Z0Real, state.Z0Imaginary)} · m: {state.M:G8}";
        return state.Variant == NovaVariant.Julia
            ? details + $"\nC: {Complex(state.CReal, state.CImaginary)}"
            : details;
    }

    private static string Complex(decimal real, decimal imaginary) =>
        $"{real:G8} {(imaginary < 0 ? '−' : '+')} {Math.Abs(imaginary):G8}i";
}
