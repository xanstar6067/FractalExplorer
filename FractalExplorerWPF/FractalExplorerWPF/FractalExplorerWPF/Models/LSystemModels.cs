using System.Windows.Media;
using Color = System.Windows.Media.Color;

namespace FractalExplorerWPF.Models;

public enum LSystemStyleMode
{
    Generation,
    BranchDepth,
    DrawingOrder,
    Uniform
}

public sealed class LSystemDefinition
{
    public string Axiom { get; set; } = "F";
    public string RulesText { get; set; } = "F → F+F--F+F";
    public string DrawSymbols { get; set; } = "F";
    public int Depth { get; set; } = 4;
    public double AngleDegrees { get; set; } = 60;
    public double InitialAngleDegrees { get; set; }
    public Color StartColor { get; set; } = Color.FromRgb(40, 120, 255);
    public Color EndColor { get; set; } = Color.FromRgb(130, 235, 255);
    public Color BackgroundColor { get; set; } = Color.FromRgb(7, 10, 18);
    public double StartThickness { get; set; } = 2.4;
    public double EndThickness { get; set; } = 0.8;
    public LSystemStyleMode StyleMode { get; set; } = LSystemStyleMode.DrawingOrder;

    public LSystemDefinition Clone() => (LSystemDefinition)MemberwiseClone();
}

public sealed class LSystemPreset
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required LSystemDefinition Definition { get; init; }

    public override string ToString() => Name;
}

public static class LSystemPresets
{
    private static readonly Color Blue = Color.FromRgb(54, 132, 255);
    private static readonly Color Cyan = Color.FromRgb(104, 232, 255);
    private static readonly Color Amber = Color.FromRgb(255, 170, 48);
    private static readonly Color Rose = Color.FromRgb(255, 91, 133);
    private static readonly Color Violet = Color.FromRgb(157, 108, 255);
    private static readonly Color Bark = Color.FromRgb(116, 70, 42);
    private static readonly Color Leaf = Color.FromRgb(74, 220, 104);
    private static readonly Color DarkBackground = Color.FromRgb(7, 10, 18);

    public static IReadOnlyList<LSystemPreset> All { get; } =
    [
        Preset(
            "koch_curve",
            "Кривая Коха",
            "Классическая кривая Коха: каждая линия заменяется четырьмя отрезками.",
            "F", "F → F+F--F+F", "F", 5, 60, 0, Blue, Cyan, 2.2, 0.8),
        Preset(
            "koch_snowflake",
            "Снежинка Коха",
            "Три кривые Коха, замкнутые в снежинку.",
            "F--F--F", "F → F+F--F+F", "F", 5, 60, 0,
            Color.FromRgb(155, 214, 255), Color.FromRgb(245, 252, 255), 2.0, 0.75),
        Preset(
            "pythagoras_tree",
            "Дерево Пифагора",
            "Ветвящаяся двоичная L‑система: 0 и 1 рисуют отрезки, скобки сохраняют состояние черепахи.",
            "0", "1 → 11\n0 → 1[+0]-0", "01", 10, 45, -90, Bark, Leaf, 5.2, 0.7,
            LSystemStyleMode.BranchDepth),
        Preset(
            "fractal_plant",
            "Фрактальное растение",
            "Классическая модель растения с вложенными ветвями.",
            "X", "X → F+[[X]-X]-F[-FX]+X\nF → FF", "F", 6, 25, -90,
            Bark, Leaf, 5.5, 0.65, LSystemStyleMode.BranchDepth),
        Preset(
            "hilbert_curve",
            "Кривая Гильберта",
            "Заполняющая пространство ортогональная кривая. A и B управляют развёрткой, F рисует.",
            "A", "A → +BF-AFA-FB+\nB → -AF+BFB+FA-", "F", 6, 90, 0,
            Violet, Cyan, 2.0, 0.7),
        Preset(
            "levy_c",
            "Кривая Леви C",
            "Самоподобная кривая из двух повёрнутых копий.",
            "F", "F → +F--F+", "F", 14, 45, 0, Amber, Rose, 2.0, 0.65),
        Preset(
            "dragon_curve",
            "Dragon Curve",
            "Дракон Хейуэя. X и Y задают повороты, F оставляет след.",
            "FX", "X → X+YF+\nY → -FX-Y", "F", 14, 90, 0,
            Rose, Violet, 2.1, 0.65, LSystemStyleMode.Generation),
        Preset(
            "sierpinski_triangle",
            "Треугольник Серпинского",
            "L‑системная версия Серпинского заменяет прежний отдельный геометрический режим.",
            "F-G-G", "F → F-G+F+G-F\nG → GG", "FG", 7, 120, 0,
            Amber, Rose, 2.0, 0.65)
    ];

    private static LSystemPreset Preset(
        string id,
        string name,
        string description,
        string axiom,
        string rules,
        string drawSymbols,
        int depth,
        double angle,
        double initialAngle,
        Color start,
        Color end,
        double startThickness,
        double endThickness,
        LSystemStyleMode styleMode = LSystemStyleMode.DrawingOrder) =>
        new()
        {
            Id = id,
            Name = name,
            Description = description,
            Definition = new LSystemDefinition
            {
                Axiom = axiom,
                RulesText = rules,
                DrawSymbols = drawSymbols,
                Depth = depth,
                AngleDegrees = angle,
                InitialAngleDegrees = initialAngle,
                StartColor = start,
                EndColor = end,
                BackgroundColor = DarkBackground,
                StartThickness = startThickness,
                EndThickness = endThickness,
                StyleMode = styleMode
            }
        };
}
