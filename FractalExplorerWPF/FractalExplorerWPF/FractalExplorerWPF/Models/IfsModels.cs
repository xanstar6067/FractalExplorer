using System.Windows.Media;
using Color = System.Windows.Media.Color;

namespace FractalExplorerWPF.Models;

public sealed class IfsAffineTransform
{
    public double A { get; set; }
    public double B { get; set; }
    public double C { get; set; }
    public double D { get; set; }
    public double E { get; set; }
    public double F { get; set; }
    public double Probability { get; set; }
    public IfsAffineTransform Clone() => (IfsAffineTransform)MemberwiseClone();
}

public sealed class IfsPreset
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public int Iterations { get; init; }
    public double CenterX { get; init; }
    public double CenterY { get; init; }
    public double Scale { get; init; }
    public required List<IfsAffineTransform> Transforms { get; init; }
    public override string ToString() => Name;
}

public sealed class IfsState
{
    public string SaveName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? PointOfInterestId { get; set; }
    public int Iterations { get; set; } = 220_000;
    public double CenterX { get; set; }
    public double CenterY { get; set; }
    public double Scale { get; set; } = 5;
    public List<IfsAffineTransform> Transforms { get; set; } = [];
    public Color FractalColor { get; set; } = Colors.Lime;
    public Color BackgroundColor { get; set; } = Colors.Black;
    public IfsState Clone(string? name=null)=>new(){SaveName=name??SaveName,Timestamp=Timestamp,PointOfInterestId=PointOfInterestId,Iterations=Iterations,CenterX=CenterX,CenterY=CenterY,Scale=Scale,Transforms=Transforms.Select(t=>t.Clone()).ToList(),FractalColor=FractalColor,BackgroundColor=BackgroundColor};
}

public static class IfsPresets
{
    public static IReadOnlyList<IfsPreset> All { get; } =
    [
        new(){Id="barnsley_overview",Name="Barnsley Fern — общий вид",Iterations=220_000,Scale=5,Transforms=
        [
            new(){A=0,B=0,C=0,D=.16,E=0,F=0,Probability=.01},
            new(){A=.85,B=.04,C=-.04,D=.85,E=0,F=1.6,Probability=.85},
            new(){A=.2,B=-.26,C=.23,D=.22,E=0,F=1.6,Probability=.07},
            new(){A=-.15,B=.28,C=.26,D=.24,E=0,F=.44,Probability=.07}
        ]},
        new(){Id="dragon_overview",Name="Heighway Dragon — общий вид",Iterations=200_000,Scale=5,Transforms=
        [
            new(){A=.824074,B=.281428,C=-.212346,D=.864198,E=-1.882290,F=-.110607,Probability=.5},
            new(){A=.088272,B=.520988,C=-.463889,D=-.377778,E=.785360,F=8.095795,Probability=.5}
        ]}
    ];
}
