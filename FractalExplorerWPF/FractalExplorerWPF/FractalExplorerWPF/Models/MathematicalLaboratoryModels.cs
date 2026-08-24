using System.Windows.Media;
using Color = System.Windows.Media.Color;

namespace FractalExplorerWPF.Models;

public enum MathematicalLaboratoryKind
{
    ModularArithmetic,
    PascalModulo,
    RationalNumbers,
    PrimeGeometry,
    Phyllotaxis,
    CircleInversion,
    AperiodicTilings,
    HyperbolicGeometry,
    FourierEpicycles
}

public readonly record struct LaboratoryPoint(double X, double Y);

public sealed class MathematicalLaboratoryState
{
    public string SaveName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public MathematicalLaboratoryKind Kind { get; set; }
    public int Mode { get; set; }
    public int PrimaryValue { get; set; }
    public int SecondaryValue { get; set; }
    public int TertiaryValue { get; set; }
    public double Parameter { get; set; }
    public double Phase { get; set; }
    public bool ShowGuides { get; set; } = true;
    public bool Filled { get; set; } = true;
    public bool Animate { get; set; }
    public double ViewCenterX { get; set; }
    public double ViewCenterY { get; set; }
    public double Zoom { get; set; } = 1;
    public double Rotation { get; set; }
    public double AnchorX { get; set; }
    public double AnchorY { get; set; }
    public Color BackgroundColor { get; set; } = Color.FromRgb(7, 13, 27);
    public Color PrimaryColor { get; set; } = Color.FromRgb(34, 211, 238);
    public Color SecondaryColor { get; set; } = Color.FromRgb(244, 63, 94);
    public Color AccentColor { get; set; } = Color.FromRgb(250, 204, 21);
    public List<LaboratoryPoint> InputPoints { get; set; } = [];

    public MathematicalLaboratoryState Clone(string? name = null) => new()
    {
        SaveName = name ?? SaveName,
        Timestamp = Timestamp,
        Kind = Kind,
        Mode = Mode,
        PrimaryValue = PrimaryValue,
        SecondaryValue = SecondaryValue,
        TertiaryValue = TertiaryValue,
        Parameter = Parameter,
        Phase = Phase,
        ShowGuides = ShowGuides,
        Filled = Filled,
        Animate = Animate,
        ViewCenterX = ViewCenterX,
        ViewCenterY = ViewCenterY,
        Zoom = Zoom,
        Rotation = Rotation,
        AnchorX = AnchorX,
        AnchorY = AnchorY,
        BackgroundColor = BackgroundColor,
        PrimaryColor = PrimaryColor,
        SecondaryColor = SecondaryColor,
        AccentColor = AccentColor,
        InputPoints = [.. InputPoints]
    };
}

public sealed record MathematicalLaboratoryDefinition(
    string Title,
    string Description,
    string[] Modes,
    string PrimaryLabel,
    string SecondaryLabel,
    string TertiaryLabel,
    string ParameterLabel,
    string InteractionHint,
    int PrimaryMinimum,
    int PrimaryMaximum,
    int SecondaryMinimum,
    int SecondaryMaximum,
    int TertiaryMinimum,
    int TertiaryMaximum,
    double ParameterMinimum,
    double ParameterMaximum);

public static class MathematicalLaboratoryCatalog
{
    public const string LaunchPrefix = "MathLab:";

    public static string LaunchKey(MathematicalLaboratoryKind kind) => LaunchPrefix + kind;

    public static bool TryParseLaunchKey(string? launchKey, out MathematicalLaboratoryKind kind)
    {
        kind = default;
        return launchKey?.StartsWith(LaunchPrefix, StringComparison.Ordinal) == true &&
               Enum.TryParse(launchKey[LaunchPrefix.Length..], out kind);
    }

    public static MathematicalLaboratoryDefinition GetDefinition(MathematicalLaboratoryKind kind) => kind switch
    {
        MathematicalLaboratoryKind.ModularArithmetic => new(
            "Арифметика по модулю",
            "Отображения конечного кольца на окружности: таблицы умножения и функциональные графы превращаются в кардиоиды, нефроиды и плетёные орбиты.",
            ["x → ax mod N", "x → ax+b mod N", "x → x²+c mod N", "x → xᵏ mod N", "Collatz modulo N"],
            "Модуль N", "Коэффициент a", "b / c / степень k", "Толщина линий",
            "Колесо: масштаб. ЛКМ: перемещение. Анимация изменяет коэффициент a.",
            10, 2_000, -10_000, 10_000, -10_000, 10_000, 0.25, 8),
        MathematicalLaboratoryKind.PascalModulo => new(
            "Треугольник Паскаля по модулю N",
            "Остатки биномиальных коэффициентов образуют самоподобные структуры. Для mod 2 непосредственно проявляется треугольник Серпинского.",
            ["Цвет каждого остатка", "Делится / не делится", "Сетка теоремы Люка"],
            "Число строк", "Модуль", "Размер ячейки", "Контраст",
            "Колесо: масштаб. ЛКМ: перемещение. Для крупных треугольников используется растровое уплотнение.",
            8, 2_000, 2, 256, 1, 8, 0.2, 2.5),
        MathematicalLaboratoryKind.RationalNumbers => new(
            "Лаборатория рациональных чисел",
            "Деревья Штерна—Броко и Калкина—Уилфа, последовательности Фарея, окружности Форда и приближения цепными дробями в одном пространстве.",
            ["Дерево Штерна—Броко", "Дерево Калкина—Уилфа", "Последовательность Фарея", "Окружности Форда", "Цепная дробь"],
            "Глубина дерева", "Макс. знаменатель", "Лимит элементов", "Целевое число",
            "Колесо: масштаб. ЛКМ: перемещение. В режиме цепной дроби параметр задаёт приближаемое число.",
            1, 13, 2, 300, 20, 30_000, 0.000001, 1000),
        MathematicalLaboratoryKind.PrimeGeometry => new(
            "Геометрия простых чисел",
            "Простые числа на квадратных, полярных и алгебраических решётках. Цвет показывает класс вычетов, а направляющие помогают увидеть цепочки.",
            ["Спираль Улама", "Спираль Сакса", "Шестиугольная спираль", "Простые Гаусса", "Простые Эйзенштейна"],
            "Радиус / размер", "Модуль цвета", "Константа полинома", "Размер точки",
            "Колесо: масштаб. ЛКМ: перемещение. Жёлтым отмечаются значения n²+n+c.",
            8, 260, 2, 64, -200, 200, 0.5, 8),
        MathematicalLaboratoryKind.Phyllotaxis => new(
            "Филлотаксис и иррациональные вращения",
            "Точки с постоянным угловым шагом показывают парастихии золотого угла и распад структуры около рациональных вращений.",
            ["Золотой угол", "Угол π", "Угол √2", "Рациональное вращение", "Пользовательский угол"],
            "Число точек", "Числитель", "Знаменатель", "Угол, градусы",
            "Колесо: масштаб. ЛКМ: перемещение. Анимация медленно меняет угол вращения.",
            50, 50_000, 1, 10_000, 1, 10_000, -3600, 3600),
        MathematicalLaboratoryKind.CircleInversion => new(
            "Инверсия окружностей и преобразования Мёбиуса",
            "Инверсия переводит прямые и окружности друг в друга; комплексные дробно-линейные отображения продолжают ту же геометрию.",
            ["Инверсия точек", "Инверсия окружностей", "Повторные инверсии", "Преобразование Мёбиуса"],
            "Число объектов", "Число повторов", "Симметрия", "Радиус инверсии",
            "ЛКМ: добавить точку. ПКМ: перенести центр инверсии. Shift+ЛКМ: перемещение. Очистка возвращает исходный узор.",
            3, 2_000, 1, 12, 2, 24, 0.05, 2),
        MathematicalLaboratoryKind.AperiodicTilings => new(
            "Подстановочные и апериодические мозаики",
            "Инфляция и дефляция раскрывают иерархию мозаик Пенроуза, Амманна—Бинкера, Chair, Pinwheel, сфинкса и Фибоначчи.",
            ["Penrose P2/P3", "Ammann—Beenker", "Chair tiling", "Pinwheel tiling", "Мозаика сфинкса", "Треугольники Фибоначчи"],
            "Поколение", "Число секторов", "Вариант правила", "Толщина границ",
            "Колесо: масштаб. ЛКМ: перемещение. Переключатель «Заливка» оставляет только границы плиток.",
            1, 9, 3, 32, 0, 8, 0.25, 6),
        MathematicalLaboratoryKind.HyperbolicGeometry => new(
            "Гиперболическая геометрия",
            "Мозаики {p,q}, геодезические и идеальная граница в модели диска Пуанкаре. Евклидов размер плиток уменьшается к краю диска.",
            ["Мозаика {3,7}", "Мозаика {4,5}", "Мозаика {6,4}", "Пользовательская {p,q}", "Геодезические"],
            "Глубина", "Параметр p", "Параметр q", "Кривизна вида",
            "Колесо: масштаб. ЛКМ: перемещение. Для гиперболической мозаики требуется (p−2)(q−2) > 4.",
            1, 9, 3, 12, 3, 12, 0.15, 1.5),
        MathematicalLaboratoryKind.FourierEpicycles => new(
            "Fourier Epicycles",
            "Замкнутый контур раскладывается в дискретный ряд Фурье и восстанавливается цепочкой вращающихся окружностей.",
            ["Гармоники по частоте", "Гармоники по амплитуде", "Спектр коэффициентов"],
            "Число гармоник", "Число отсчётов", "Точек траектории", "Скорость анимации",
            "Зажмите ЛКМ и нарисуйте замкнутый контур. Shift+ЛКМ перемещает вид. Анимация ведёт эпициклы по траектории.",
            1, 250, 32, 2_048, 50, 4_000, 0.05, 4),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };

    public static MathematicalLaboratoryState CreateDefaultState(MathematicalLaboratoryKind kind)
    {
        var state = new MathematicalLaboratoryState { Kind = kind };
        switch (kind)
        {
            case MathematicalLaboratoryKind.ModularArithmetic:
                state.PrimaryValue = 240; state.SecondaryValue = 2; state.TertiaryValue = 0; state.Parameter = 1.1;
                break;
            case MathematicalLaboratoryKind.PascalModulo:
                state.PrimaryValue = 360; state.SecondaryValue = 5; state.TertiaryValue = 1; state.Parameter = 1;
                break;
            case MathematicalLaboratoryKind.RationalNumbers:
                state.PrimaryValue = 8; state.SecondaryValue = 36; state.TertiaryValue = 8_000; state.Parameter = Math.Sqrt(2);
                break;
            case MathematicalLaboratoryKind.PrimeGeometry:
                state.PrimaryValue = 70; state.SecondaryValue = 6; state.TertiaryValue = 41; state.Parameter = 2.2;
                break;
            case MathematicalLaboratoryKind.Phyllotaxis:
                state.PrimaryValue = 5_000; state.SecondaryValue = 1; state.TertiaryValue = 7;
                state.Parameter = 137.50776405003785;
                break;
            case MathematicalLaboratoryKind.CircleInversion:
                state.PrimaryValue = 72; state.SecondaryValue = 5; state.TertiaryValue = 7; state.Parameter = 0.62;
                break;
            case MathematicalLaboratoryKind.AperiodicTilings:
                state.PrimaryValue = 5; state.SecondaryValue = 10; state.TertiaryValue = 0; state.Parameter = 1.1;
                break;
            case MathematicalLaboratoryKind.HyperbolicGeometry:
                state.PrimaryValue = 5; state.SecondaryValue = 3; state.TertiaryValue = 7; state.Parameter = 0.68;
                break;
            case MathematicalLaboratoryKind.FourierEpicycles:
                state.PrimaryValue = 45; state.SecondaryValue = 512; state.TertiaryValue = 900; state.Parameter = 0.7;
                break;
        }
        return state;
    }
}
