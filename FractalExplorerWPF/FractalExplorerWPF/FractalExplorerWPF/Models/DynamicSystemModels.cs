using System.Windows.Media;
using Color = System.Windows.Media.Color;

namespace FractalExplorerWPF.Models;

public enum DynamicSystemKind
{
    Lyapunov,
    Lorenz,
    Rossler,
    LogisticMap,
    Bifurcation,
    Henon,
    Ikeda
}

public sealed class DynamicSystemState
{
    public string SaveName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? PointOfInterestId { get; set; }
    public DynamicSystemKind Kind { get; set; }
    public double CenterX { get; set; }
    public double CenterY { get; set; }
    public double Zoom { get; set; } = 1;
    public int Threads { get; set; } = Environment.ProcessorCount;
    public int SsaaFactor { get; set; } = 1;
    public Color BackgroundColor { get; set; } = Colors.Black;
    public Color FractalColor { get; set; } = Colors.White;
    public string PaletteName { get; set; } = string.Empty;

    public double AMin { get; set; } = 2.5;
    public double AMax { get; set; } = 4;
    public double BMin { get; set; } = 2.5;
    public double BMax { get; set; } = 4;
    public string Pattern { get; set; } = "AB";
    public int Iterations { get; set; } = 300;
    public int TransientIterations { get; set; } = 100;

    public double Sigma { get; set; } = 10;
    public double Rho { get; set; } = 28;
    public double Beta { get; set; } = 2.666666;
    public double Dt { get; set; } = .01;
    public int Steps { get; set; } = 120_000;
    public double StartX { get; set; } = .01;
    public double StartY { get; set; }
    public double StartZ { get; set; }
    public string ProjectionMode { get; set; } = "XY";

    public double A { get; set; } = 1.4;
    public double B { get; set; } = .3;
    public double C { get; set; } = 5.7;
    public double R { get; set; } = 3.8;
    public double U { get; set; } = .918;
    public double X0 { get; set; } = .1;
    public double Y0 { get; set; }
    public int DiscardIterations { get; set; } = 500;
    public string VisualizationMode { get; set; } = "Orbit";
    public double BifurcationRMin { get; set; } = 2.8;
    public double BifurcationRMax { get; set; } = 4;
    public int BifurcationSamples { get; set; } = 1600;
    public int BifurcationTransient { get; set; } = 500;
    public int BifurcationPlottedPoints { get; set; } = 240;
    public int CobwebSteps { get; set; } = 40;
    public double RMin { get; set; } = 2.8;
    public double RMax { get; set; } = 4;
    public double XMin { get; set; }
    public double XMax { get; set; } = 1;
    public int SamplesPerR { get; set; } = 240;
    public double RangeXMin { get; set; } = -2;
    public double RangeXMax { get; set; } = 2;
    public double RangeYMin { get; set; } = -2;
    public double RangeYMax { get; set; } = 2;

    public DynamicSystemState Clone(string? name = null)
    {
        var clone = (DynamicSystemState)MemberwiseClone();
        if (name is not null) clone.SaveName = name;
        return clone;
    }

    public static DynamicSystemState CreateDefault(DynamicSystemKind kind)
    {
        var state = new DynamicSystemState { Kind = kind };
        switch (kind)
        {
            case DynamicSystemKind.Lyapunov:
                state.PaletteName = "Классическая Ляпунова";
                state.CenterX = 3.25; state.CenterY = 3.25;
                break;
            case DynamicSystemKind.Lorenz:
                state.CenterY = 25; state.X0 = .01;
                break;
            case DynamicSystemKind.Rossler:
                state.A = .2; state.B = .2; state.C = 5.7; state.Steps = 150_000; state.StartX = .1;
                break;
            case DynamicSystemKind.LogisticMap:
                state.CenterX = .5; state.CenterY = .5; state.X0 = .2; state.Iterations = 2500;
                state.PaletteName = "Орбиты: периодические полосы";
                break;
            case DynamicSystemKind.Bifurcation:
                state.CenterX = 3.4; state.CenterY = .5; state.Iterations = 1200;
                break;
            case DynamicSystemKind.Henon:
                state.Iterations = 500_000; state.X0 = .1;
                break;
            case DynamicSystemKind.Ikeda:
                state.Iterations = 1_000_000; state.X0 = .1; state.Y0 = .1;
                break;
        }
        return state;
    }
}

public sealed class DynamicPalette
{
    public string Name { get; set; } = "Новая палитра";
    public List<Color> Colors { get; set; } = [];
    public string Mode { get; set; } = "Diverging";
    public double ExponentRange { get; set; } = 2;
    public double ZeroBandWidth { get; set; } = .05;
    public bool IsBuiltIn { get; set; }
    public DynamicPalette Clone(string? name = null) => new() { Name = name ?? Name, Colors = Colors.ToList(), Mode = Mode, ExponentRange = ExponentRange, ZeroBandWidth = ZeroBandWidth };
}
