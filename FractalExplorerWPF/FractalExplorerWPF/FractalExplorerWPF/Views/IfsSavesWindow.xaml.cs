using System.Windows;
using FractalExplorerWPF.Infrastructure;
using FractalExplorerWPF.Models;

namespace FractalExplorerWPF.Views;

public partial class IfsSavesWindow : Window
{
    private readonly SaveManagerController<IfsState> _controller;

    public IfsSavesWindow(IfsWindow window, IfsSaveStore store)
    {
        InitializeComponent();
        _controller = new SaveManagerController<IfsState>(this, Manager, new SaveManagerConfiguration<IfsState>
        {
            WindowTitle = "Сохранение/Загрузка: IFS",
            FractalIdentifier = "IFS",
            LoadStates = store.Load,
            SaveStates = store.Save,
            CaptureState = window.CaptureState,
            LoadState = state => window.LoadState(state.Clone()),
            RenderPreviewAsync = (state, width, height, token) => window.RenderStatePreviewAsync(state.Clone(), width, height, token),
            GetName = state => state.SaveName,
            GetTimestamp = state => state.Timestamp,
            GetDetails = Describe,
            PointsOfInterest = CreatePointsOfInterest()
        });
        Closed += (_, _) => _controller.Dispose();
    }

    private static IReadOnlyList<IfsState> CreatePointsOfInterest() => IfsPresets.All.Select(preset => new IfsState
    {
        SaveName = preset.Name,
        PointOfInterestId = preset.Id,
        Iterations = preset.Iterations,
        CenterX = preset.CenterX,
        CenterY = preset.CenterY,
        Scale = preset.Scale,
        Transforms = preset.Transforms.Select(transform => transform.Clone()).ToList()
    }).ToList();

    private static string Describe(IfsState state)
    {
        string prefix = state.Timestamp == default ? "Точка интереса" : state.Timestamp.ToString("g");
        return $"{prefix} · {state.Iterations:N0} итераций · Масштаб: {state.Scale:F4}\n" +
               $"Преобразований: {state.Transforms.Count}";
    }
}
