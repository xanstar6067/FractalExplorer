using FractalExplorerWPF.Models;
using FractalExplorerWPF.Views;

namespace FractalExplorerWPF.Infrastructure;

public static class SaveManagerConfigurations
{
    public static SaveManagerConfiguration<MandelbrotState> ForMandelbrot(
        MandelbrotWindow window, MandelbrotSaveStore store) => new()
    {
        WindowTitle = $"Сохранение/Загрузка: {window.SaveManagerDisplayName}",
        FractalIdentifier = window.SaveManagerIdentifier,
        LoadStates = store.Load,
        SaveStates = store.Save,
        CaptureState = window.CaptureState,
        LoadState = window.LoadState,
        RenderPreviewAsync = window.RenderStatePreviewAsync,
        GetName = state => state.SaveName,
        GetTimestamp = state => state.Timestamp,
        GetDetails = DescribeMandelbrot,
        PointsOfInterest = PresetManager.GetMandelbrotPresets(window.SaveManagerVariant)
    };

    public static SaveManagerConfiguration<SerpinskySaveState> ForSerpinsky(
        SerpinskyWindow window, SerpinskySaveStore store) => new()
    {
        WindowTitle = "Сохранение/Загрузка: Серпинский",
        FractalIdentifier = "Serpinsky",
        LoadStates = store.Load,
        SaveStates = store.Save,
        CaptureState = window.CaptureState,
        LoadState = window.LoadState,
        RenderPreviewAsync = window.RenderStatePreviewAsync,
        GetName = state => state.SaveName,
        GetTimestamp = state => state.Timestamp,
        GetDetails = state => $"{Prefix(state.Timestamp)} · Режим: {state.RenderMode}\n" +
                                    $"Итерации: {state.Iterations} · Масштаб: {state.Zoom:0.####}",
        PointsOfInterest = PresetManager.GetSerpinskyPresets()
    };

    public static SaveManagerConfiguration<NewtonState> ForNewton(
        NewtonPoolsWindow window, NewtonSaveStore store) => new()
    {
        WindowTitle = "Сохранение/Загрузка: Бассейны Ньютона",
        FractalIdentifier = "NewtonPools",
        LoadStates = store.Load,
        SaveStates = store.Save,
        CaptureState = window.CaptureState,
        LoadState = window.LoadState,
        RenderPreviewAsync = window.RenderStatePreviewAsync,
        GetName = state => state.SaveName,
        GetTimestamp = state => state.Timestamp,
        GetDetails = state => $"{Prefix(state.Timestamp)} · Метод: {state.IterationMethod} · Итерации: {state.MaxIterations}\n" +
                                    $"Формула: {state.Formula} · Масштаб: {state.Zoom:0.####}\n" +
                                    $"Корней: {state.Roots.Count} · Точность: {state.RootTolerance:G3} · Поиск: {state.RootSearchMode}",
        PointsOfInterest = PresetManager.GetNewtonPresets()
    };

    public static SaveManagerConfiguration<NovaState> ForNova(
        NovaWindow window, NovaSaveStore store, NovaVariant variant) => new()
    {
        WindowTitle = $"Сохранение/Загрузка: {(variant == NovaVariant.Julia ? "Nova Julia" : "Nova Mandelbrot")}",
        FractalIdentifier = variant == NovaVariant.Julia ? "NovaJulia" : "NovaMandelbrot",
        LoadStates = store.Load,
        SaveStates = store.Save,
        CaptureState = window.CaptureState,
        LoadState = window.LoadState,
        RenderPreviewAsync = window.RenderStatePreviewAsync,
        GetName = state => state.SaveName,
        GetTimestamp = state => state.Timestamp,
        GetDetails = DescribeNova
    };

    public static SaveManagerConfiguration<PhoenixState> ForPhoenix(
        PhoenixWindow window, PhoenixSaveStore store) => new()
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
        GetDetails = state => $"{Prefix(state.Timestamp)} · Итерации: {state.Iterations} · Масштаб: {state.Zoom:G8}\n" +
                                    $"C1: {Complex(state.C1Real, state.C1Imaginary)}",
        PointsOfInterest = PresetManager.GetPhoenixPresets()
    };

    public static SaveManagerConfiguration<CollatzState> ForCollatz(
        CollatzWindow window, CollatzSaveStore store) => new()
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
        GetDetails = state => $"{Prefix(state.Timestamp)} · Режим: {VariationName(state.Variation)} · Итерации: {state.Iterations}\n" +
                                    $"Порог: {state.Threshold:G8} · Масштаб: {state.Zoom:G8}",
        PointsOfInterest = PresetManager.GetCollatzPresets()
    };

    public static SaveManagerConfiguration<BuddhabrotState> ForBuddhabrot(
        BuddhabrotWindow window, BuddhabrotSaveStore store) => new()
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
        GetDetails = state => $"{Prefix(state.Timestamp)} · {state.RenderMode} · {state.SampleCount:N0} сэмплов\n" +
                                    $"Итерации: {state.MaxIterations} · Палитра: {state.Palette.Name}"
    };

    public static SaveManagerConfiguration<FlameState> ForFlame(
        FlameWindow window, FlameSaveStore store) => new()
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
        GetDetails = state => $"{Prefix(state.Timestamp)} · {state.Samples:N0} сэмплов · {state.IterationsPerSample} итераций\n" +
                                    $"Трансформаций: {state.Transforms.Count} · Экспозиция: {state.Exposure:F2} · Гамма: {state.Gamma:F2}",
        PointsOfInterest = PresetManager.GetFlamePresets()
    };

    public static SaveManagerConfiguration<IfsState> ForIfs(
        IfsWindow window, IfsSaveStore store) => new()
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
        GetDetails = state => $"{Prefix(state.Timestamp)} · {state.Iterations:N0} итераций · Масштаб: {state.Scale:F4}\n" +
                                    $"Преобразований: {state.Transforms.Count}",
        PointsOfInterest = PresetManager.GetIfsPresets()
    };

    private static string DescribeMandelbrot(MandelbrotState state)
    {
        string details = $"{Prefix(state.Timestamp)} · Итерации: {state.Iterations} · Масштаб: {state.Zoom:G6}\n" +
                         $"Центр: {state.CenterX:G8}; {state.CenterY:G8} · Палитра: {state.PaletteName} · {state.ColoringMode}";
        if (state.Variant is MandelbrotVariant.Julia or MandelbrotVariant.JuliaBurningShip)
            details += $"\nКонстанта C: {Complex(state.JuliaCReal, state.JuliaCImaginary)}";
        return details;
    }

    private static string DescribeNova(NovaState state)
    {
        string details = $"{Prefix(state.Timestamp)} · Итерации: {state.Iterations} · Масштаб: {state.Zoom:G8}\n" +
                         $"P: {Complex(state.PReal, state.PImaginary)} · Z₀: {Complex(state.Z0Real, state.Z0Imaginary)} · m: {state.M:G8}";
        return state.Variant == NovaVariant.Julia ? details + $"\nC: {Complex(state.CReal, state.CImaginary)}" : details;
    }

    private static string Prefix(DateTime timestamp) => timestamp == DateTime.MinValue ? "Точка интереса" : timestamp.ToString("g");

    private static string Complex(decimal real, decimal imaginary) =>
        $"{real:G8} {(imaginary < 0 ? '−' : '+')} {Math.Abs(imaginary):G8}i";

    private static string VariationName(CollatzVariation variation) => variation switch
    {
        CollatzVariation.SineVariation => "Синусная",
        CollatzVariation.GeneralizedP => "Обобщённая p·x+1",
        _ => "Стандартная"
    };
}
