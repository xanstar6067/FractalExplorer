using System.Windows.Media;
using FractalExplorer.Engines;
using FractalExplorerWPF.Models;
using Color = System.Windows.Media.Color;

namespace FractalExplorerWPF.Infrastructure;

/// <summary>
/// WPF-представление точек интереса из PresetManager старой версии.
/// Возвращает состояния текущих WPF-моделей, готовые к загрузке и рендеру.
/// </summary>
public static class PresetManager
{
    public static IReadOnlyList<MandelbrotState> GetMandelbrotPresets(MandelbrotVariant variant) => variant switch
    {
        MandelbrotVariant.Mandelbrot =>
        [
            M("Долина Морских Коньков", variant, -0.743643887037151m, 0.13182590420533m, 11500m, 1000, "Лёд"),
            M("Шип Миниброта", variant, -1.74995m, 0m, 4000m, 500, "Огонь"),
            M("Лоза", variant, 0.3855604675494107229386479028m, -0.1050451711526294339131097223m, 150m, 800, "Огонь"),
            M("Спиральная Галактика", variant, -0.16070135m, 1.0375665m, 3000m, 600, "Ультрафиолет")
        ],
        MandelbrotVariant.Julia =>
        [
            M("Классическая Спираль", variant, 0m, 0m, 1m, 500, "Огонь", juliaReal: -0.8m, juliaImaginary: 0.156m),
            M("Дендрит", variant, 0m, 0m, 1m, 300, "Зеленый", juliaReal: 0m, juliaImaginary: 1m),
            M("Снежинка", variant, 0m, 0m, 1m, 400, "Лёд", juliaReal: -0.70176m, juliaImaginary: -0.3842m),
            M("Огненный Вихрь", variant, 0m, 0m, 1m, 350, "Огонь", juliaReal: 0.285m, juliaImaginary: 0.01m)
        ],
        MandelbrotVariant.BurningShip =>
        [
            M("Центральный Корабль", variant, 0m, 0m, 0.8m, 300, "Огонь"),
            M("Глубоководный Корабль", variant, -1.7623214771385076201641266142m, 0.0200163188745603751465416114m, 40m, 700, "Ультрафиолет"),
            M("Призрачные Паруса", variant, -1.7423683296426555512135816837m, 0.0648050817843091259643027922m, 76m, 1000, "Лёд")
        ],
        MandelbrotVariant.Tricorn =>
        [
            M("Тройной Крест", variant, 0m, 0m, 0.85m, 500, "Ультрафиолет"),
            M("Левый Вихрь", variant, -0.295m, 0.018m, 9.5m, 800, "Огонь"),
            M("Северный Лепесток", variant, -0.1096m, 0.8198m, 28m, 1100, "Лёд")
        ],
        MandelbrotVariant.Celtic =>
        [
            M("Кельтский кардиоид", variant, -0.5m, 0m, 1.2m, 700, "Лёд"),
            M("Кельтский спиральный фрагмент", variant, -1.245m, 0.015m, 220m, 900, "Огонь")
        ],
        MandelbrotVariant.JuliaBurningShip =>
        [
            M("Фиолетовый Пламень Жюлиа", variant, 0m, 0m, 1m, 500, "Ультрафиолет", juliaReal: 0.598214268684387m, juliaImaginary: 1.17851734161377m),
            M("Пульсарный Рубин", variant, 0m, 0m, 1m, 350, "Огонь", juliaReal: -0.0517381690442562m, juliaImaginary: -0.267557740211487m),
            M("Психонавт", variant, 0m, 0m, 1m, 500, "Психоделика", juliaReal: 0.736607134342194m, juliaImaginary: 1.09152793884277m)
        ],
        MandelbrotVariant.Generalized =>
        [
            M("Трилистник (p=3.0)", variant, 0m, 0m, 0.8m, 500, "Ультрафиолет", power: 3m),
            M("Астероид (p=4.0)", variant, 0m, 0m, 0.8m, 500, "Огонь", power: 4m)
        ],
        MandelbrotVariant.Buffalo =>
        [
            M("Классический Буффало", variant, 0m, 0m, 0.8m, 500, "Ультрафиолет"),
            M("Глаз Жука", variant, -1.25066m, -0.3837m, 1500m, 800, "Огонь")
        ],
        MandelbrotVariant.Simonobrot =>
        [
            M("Кристальная пещера (p=5)", variant, 0.334m, 0m, 2.7m, 500, "Лёд", power: 5m),
            M("Звезда (p=-2)", variant, -0.43m, 0m, 1.5m, 500, "Ультрафиолет", power: -2m),
            M("Колючка (p=-3, инверсия)", variant, 0.413m, 0m, 23m, 500, "Психоделика", power: -3m, useInversion: true)
        ],
        _ => []
    };

    public static IReadOnlyList<PhoenixState> GetPhoenixPresets() =>
    [
        Phoenix("Классический Феникс", 0.56667m, 0m, -0.5m, 0m, 0m, 0m, 1, 300, "Психоделика"),
        Phoenix("Вихрь комплексной памяти", 0.35m, -0.01m, -0.62m, 0.005m, 0.1m, -0.2m, 1.2, 350, "Психоделика"),
        Phoenix("Хвост павлина", 0.56667m, 0.001m, -0.5m, 0.001m, 0m, 0m, 0.8, 400, "Лёд",
            coloring: PhoenixColoringMode.TriangleInequalityAverage),
        Phoenix("Кубическая корона", 0.42m, 0m, -0.36m, 0m, 0m, 0m, 0.85, 450, "Ультрафиолет",
            primaryPower: 3),
        Phoenix("Параметрическая плоскость C1", 0.56667m, 0m, -0.5m, 0m, -0.25m, 0m, 0.9, 350, "Огонь",
            planeMode: PhoenixPlaneMode.ParameterC1),
        Phoenix("Трикорн Феникса", 0.24m, 0.08m, -0.46m, 0m, 0m, 0m, 0.9, 400, "Ультрафиолет",
            variant: PhoenixVariant.Tricorn, coloring: PhoenixColoringMode.StripeAverage),
        Phoenix("Горящий Феникс", -0.35m, -0.05m, -0.42m, 0.03m, 0m, 0m, 0.8, 400, "Огонь",
            variant: PhoenixVariant.BurningShip, coloring: PhoenixColoringMode.OrbitTrap)
    ];

    public static IReadOnlyList<SerpinskySaveState> GetSerpinskyPresets() =>
    [
        new()
        {
            SaveName = "Классическая Геометрия", Timestamp = DateTime.MinValue,
            RenderMode = SerpinskyRenderMode.Geometric, Iterations = 8, Zoom = 1,
            CenterX = 0, CenterY = 0.1, FractalColor = Colors.Black, BackgroundColor = Colors.White
        },
        new()
        {
            SaveName = "Ночной Хаос", Timestamp = DateTime.MinValue,
            RenderMode = SerpinskyRenderMode.Chaos, Iterations = 100_000, Zoom = 1,
            CenterX = 0, CenterY = 0.1, FractalColor = Colors.OrangeRed,
            BackgroundColor = Color.FromRgb(10, 0, 20)
        }
    ];

    public static IReadOnlyList<NewtonState> GetNewtonPresets() =>
    [
        Newton("Ньютон: z^3 - 1 (Классика)", "z^3-1", 0, 0, 1, 100,
            Palette("NewtonPreset1_Classic", [Rgb(255,100,100), Rgb(100,255,100), Rgb(100,100,255)], Rgb(20,0,0), false)),
        Newton("Ньютон: z^4 - 1 (Градиент)", "z^4-1", 0, 0, 1.2, 80,
            Palette("NewtonPreset2_Gradient", [Colors.Cyan, Colors.Magenta, Colors.Yellow, Colors.Lime], Colors.Black, true)),
        Newton("Ньютон: z^5 - z^2 + 1", "z^5 - z^2 + 1", 0, 0, 1.5, 120,
            Palette("NewtonPreset3_Complex", [Colors.Orange, Colors.Purple, Colors.GreenYellow, Colors.SkyBlue, Colors.HotPink], Rgb(10,10,30), false)),
        Newton("Ньютон: z^3-2*z+2 (Сдвиг)", "z^3-2*z+2", 0.5, -0.3, 2, 150,
            Palette("NewtonPreset4_Shifted", [Colors.Teal, Colors.Gold, Colors.Crimson], Rgb(5,5,5), true))
    ];

    public static IReadOnlyList<CollatzState> GetCollatzPresets() =>
    [
        new()
        {
            SaveName = "Стандартный Коллатц", Timestamp = DateTime.MinValue,
            CenterX = 0, CenterY = 0, Zoom = 1, Iterations = 150, Threshold = 100,
            Variation = CollatzVariation.Standard, UseSmoothColoring = false,
            Palette = MandelbrotPalette("Стандартный серый")
        }
    ];

    public static IReadOnlyList<IfsState> GetIfsPresets() => IfsPresets.All.Select(preset => new IfsState
    {
        SaveName = preset.Name, Timestamp = DateTime.MinValue, PointOfInterestId = preset.Id,
        Iterations = preset.Iterations, CenterX = preset.CenterX, CenterY = preset.CenterY, Scale = preset.Scale,
        Transforms = preset.Transforms.Select(transform => transform.Clone()).ToList(),
        FractalColor = Colors.Lime, BackgroundColor = Colors.Black
    }).ToList();

    public static IReadOnlyList<FlameState> GetFlamePresets() =>
    [
        F("Огненный лист",0,.1,4.2,1_500_000,22,24,1.42,2.15,
            T(1,.53,.03,-.34,-.02,.55,-.03,FlameVariation.Linear,Colors.OrangeRed),
            T(.94,.51,-.03,.33,.02,.50,0,FlameVariation.Sinusoidal,Colors.Gold),
            T(.66,.47,0,.01,0,.44,.39,FlameVariation.Spherical,Colors.DeepSkyBlue)),
        F("Ледяная бабочка",-.05,0,3.7,1_800_000,24,26,1.35,2.30,
            T(1,.62,-.08,-.36,.08,.62,-.03,FlameVariation.Sinusoidal,Colors.Cyan),
            T(1,.62,.08,.36,-.08,.62,-.03,FlameVariation.Sinusoidal,Colors.MediumPurple),
            T(.55,.45,0,0,0,.45,.45,FlameVariation.Spherical,Colors.WhiteSmoke)),
        F("Галактический вихрь",0,-.05,5,2_200_000,26,28,1.28,2.25,
            T(.92,.78,-.18,-.05,.18,.78,.04,FlameVariation.Linear,Colors.MediumOrchid),
            T(.92,.78,.18,.05,-.18,.78,.04,FlameVariation.Sinusoidal,Colors.DeepPink),
            T(.40,.31,0,0,0,.31,-.62,FlameVariation.Spherical,Colors.LightSkyBlue)),
        F("Световые лепестки",.02,.08,3.6,1_700_000,23,24,1.45,2.10,
            T(.95,.58,-.22,-.21,.22,.58,-.02,FlameVariation.Linear,Colors.HotPink),
            T(.95,.58,.22,.21,-.22,.58,-.02,FlameVariation.Sinusoidal,Colors.Orange),
            T(.58,.39,0,0,0,.39,.51,FlameVariation.Spherical,Colors.Aqua)),
        F("Симметричный кристалл",0,0,4.6,2_200_000,24,26,1.34,2.18,
            T(1,.64,-.14,-.32,.14,.64,0,FlameVariation.Linear,Colors.AliceBlue),
            T(1,.64,.14,.32,-.14,.64,0,FlameVariation.Linear,Colors.SkyBlue),
            T(.62,.44,0,0,0,.44,.50,FlameVariation.Sinusoidal,Colors.Plum)),
        F("Туманность Андромеды",.03,-.08,5.3,2_800_000,27,30,1.18,2.34,
            T(.86,.81,-.24,-.07,.24,.81,.03,FlameVariation.Sinusoidal,Colors.MediumPurple),
            T(.86,.81,.24,.07,-.24,.81,.03,FlameVariation.Sinusoidal,Colors.DeepPink),
            T(.40,.28,0,.02,0,.28,-.66,FlameVariation.Spherical,Colors.LightCyan)),
        F("Папоротник рассвета",-.01,-.22,3.4,2_100_000,23,26,1.50,2.08,
            T(1.12,.83,.04,0,-.04,.86,.18,FlameVariation.Linear,Colors.ForestGreen),
            T(.74,.32,-.30,-.21,.26,.30,.24,FlameVariation.Sinusoidal,Colors.LawnGreen),
            T(.66,.32,.30,.21,-.26,.30,.24,FlameVariation.Sinusoidal,Colors.Gold),
            T(.24,.14,0,0,0,.16,-.56,FlameVariation.Spherical,Colors.LightSkyBlue)),
        F("Контрастный неон",0,0,4,2_500_000,25,28,1.65,1.95,
            T(1,.60,-.26,-.28,.26,.60,-.01,FlameVariation.Linear,Colors.Fuchsia),
            T(1,.60,.26,.28,-.26,.60,-.01,FlameVariation.Sinusoidal,Colors.Cyan),
            T(.54,.40,0,0,0,.40,.56,FlameVariation.Spherical,Colors.Yellow)),
        F("Храмовая мандала",0,.03,4.8,2_400_000,25,27,1.30,2.20,
            T(.96,.70,-.12,-.24,.12,.70,0,FlameVariation.Linear,Colors.Goldenrod),
            T(.96,.70,.12,.24,-.12,.70,0,FlameVariation.Linear,Colors.OrangeRed),
            T(.52,.36,0,0,0,.36,.52,FlameVariation.Sinusoidal,Colors.LightGoldenrodYellow),
            T(.30,.28,0,0,0,.28,-.64,FlameVariation.Spherical,Colors.MediumPurple)),
        F("Полярное сияние",-.02,-.04,4.4,2_300_000,24,27,1.38,2.16,
            T(.92,.66,-.21,-.22,.18,.69,-.04,FlameVariation.Sinusoidal,Colors.Aquamarine),
            T(.92,.66,.21,.22,-.18,.69,-.04,FlameVariation.Sinusoidal,Colors.SpringGreen),
            T(.44,.34,0,0,0,.34,.62,FlameVariation.Spherical,Colors.DeepSkyBlue)),
        F("Ртутный вихрь",.01,-.01,5.1,2_700_000,27,30,1.22,2.28,
            T(.90,.79,-.19,-.04,.19,.79,.05,FlameVariation.Linear,Colors.Silver),
            T(.90,.79,.19,.04,-.19,.79,.05,FlameVariation.Sinusoidal,Colors.LightSteelBlue),
            T(.38,.30,0,0,0,.30,-.68,FlameVariation.Spherical,Colors.WhiteSmoke)),
        F("Пламя",0,-.16,3.25,2_900_000,27,31,1.74,1.92,
            T(1.26,.84,0,0,0,.46,.34,FlameVariation.Linear,Colors.OrangeRed),
            T(.95,.57,-.27,-.20,.23,.56,.07,FlameVariation.Sinusoidal,Colors.Gold),
            T(.95,.57,.27,.20,-.23,.56,.07,FlameVariation.Sinusoidal,Colors.Orange),
            T(.33,.28,0,0,0,.30,-.73,FlameVariation.Spherical,Colors.DodgerBlue))
    ];

    private static MandelbrotState M(string name, MandelbrotVariant variant, decimal centerX, decimal centerY,
        decimal zoom, int iterations, string paletteName, decimal power = 2m, bool useInversion = false,
        decimal juliaReal = 0m, decimal juliaImaginary = 0m)
    {
        MandelbrotPalette palette = MandelbrotPalette(paletteName);
        return new MandelbrotState
        {
            SaveName = name, Timestamp = DateTime.MinValue, Variant = variant, CenterX = centerX, CenterY = centerY,
            Zoom = (double)zoom, Iterations = iterations, Threshold = 2m, ColoringMode = MandelbrotColoringMode.Smooth,
            PaletteName = paletteName, Palette = palette, Power = power, UseInversion = useInversion,
            JuliaCReal = juliaReal, JuliaCImaginary = juliaImaginary, InteriorColor = palette.InteriorColor
        };
    }

    private static PhoenixState Phoenix(string name, decimal c1Real, decimal c1Imaginary, decimal c2Real,
        decimal c2Imaginary, decimal centerX, decimal centerY, double zoom, int iterations, string paletteName,
        PhoenixPlaneMode planeMode = PhoenixPlaneMode.Julia, PhoenixVariant variant = PhoenixVariant.Classic,
        int primaryPower = 2, int secondaryPower = 0,
        PhoenixColoringMode coloring = PhoenixColoringMode.Smooth) => new()
    {
        SaveName = name, Timestamp = DateTime.MinValue, C1Real = c1Real, C1Imaginary = c1Imaginary,
        C2Real = c2Real, C2Imaginary = c2Imaginary, CenterX = centerX, CenterY = centerY,
        Zoom = zoom, Iterations = iterations, Threshold = 4m, PlaneMode = planeMode, Variant = variant,
        PrimaryPower = primaryPower, SecondaryPower = secondaryPower, ColoringMode = coloring,
        Palette = MandelbrotPalette(paletteName)
    };

    private static NewtonState Newton(string name, string formula, double centerX, double centerY,
        double zoom, int iterations, NewtonColorPalette palette) => new()
    {
        SaveName = name, Timestamp = DateTime.MinValue, Formula = formula, CenterX = centerX, CenterY = centerY,
        Zoom = zoom, MaxIterations = iterations, IterationMethod = NewtonIterationMethod.Newton,
        HouseholderOrder = 3, Palette = palette
    };

    private static NewtonColorPalette Palette(string name, List<Color> colors, Color background, bool gradient) => new()
    {
        Name = name, RootColors = colors, BackgroundColor = background, IsGradient = gradient
    };

    private static FlameState F(string name, double centerX, double centerY, double scale, int samples,
        int iterations, int warmup, double exposure, double gamma, params FlameTransform[] transforms) => new()
    {
        SaveName = name, Timestamp = DateTime.MinValue, CenterX = centerX, CenterY = centerY, Scale = scale,
        Samples = samples, IterationsPerSample = iterations, WarmupIterations = warmup,
        Exposure = exposure, Gamma = gamma, Transforms = transforms.ToList()
    };

    private static FlameTransform T(double weight, double a, double b, double c, double d, double e,
        double f, FlameVariation variation, Color color) => new()
    {
        Weight = weight, A = a, B = b, C = c, D = d, E = e, F = f, Variation = variation, Color = color
    };

    private static MandelbrotPalette MandelbrotPalette(string name)
    {
        var manager = new MandelbrotPaletteManager();
        MandelbrotPalette template = manager.Palettes.FirstOrDefault(palette =>
            palette.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ?? manager.Palettes[0];
        return template.Clone(template.Name);
    }

    private static Color Rgb(byte red, byte green, byte blue) => Color.FromRgb(red, green, blue);
}
