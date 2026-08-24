namespace FractalExplorerWPF.Models;

public static class FractalCatalog
{
    public static IReadOnlyList<FractalCatalogItem> Create() =>
    [
        Item("Множество Мандельброта", "Классический Мандельброт", "Классическое множество Z = Z² + C с глубоким масштабированием и настраиваемым окрашиванием.", "mandelbrot_preview_sq512.png"),
        Item("Множество Мандельброта", "Горящий Корабль", "Вариация Мандельброта с модулями компонент комплексного числа перед итерацией.", "burningship_preview_sq512.png"),
        Item("Множество Мандельброта", "Трикорн (Mandelbar)", "Антиголоморфная вариация с комплексным сопряжением.", "tricorn__preview_sq512.png"),
        Item("Множество Мандельброта", "Буффало", "Симметричная модификация семейства Мандельброта с модулем компонент.", "buffalo_preview_sq512.png"),
        Item("Множество Мандельброта", "Кельтский Мандельброт", "Вариант с модулем действительной части результата Z².", "celtic_mandelbrot_preview_sq512.png"),
        Item("Множество Мандельброта", "Симоноброт", "Фрактал с произвольной степенью и опциональной инверсией.", "simonobrot_preview_sq512.png"),
        Item("Множество Мандельброта", "Обобщенный Мандельброт", "Мультиброт Z = Zᵖ + C с настраиваемой степенью.", "general_mandelbrot_preview_sq512.png"),
        Item("Множество Жюлиа", "Классическое Жюлиа", "Семейство Жюлиа с фиксированной комплексной константой C.", "julia_preview_sq512.png"),
        Item("Множество Жюлиа", "Горящий Корабль (Жюлиа)", "Julia-вариант алгоритма «Горящий корабль».", "julia_burningship_preview_sq512.png"),
        Item("Множество Жюлиа", "Галерея констант C (Жюлиа)", "Пакетный просмотр множества Жюлиа для сетки значений C.", "julia_preview_sq512.png", "JuliaGallery"),
        Item("Множество Жюлиа", "Галерея констант C (Жюлиа горящий корабль)", "Сетка вариантов Julia Burning Ship.", "julia_burningship_preview_sq512.png", "JuliaBurningShipGallery"),
        Item("Итерируемые функции", "Бассейны Ньютона+", "Бассейны притяжения корней для методов Newton, Halley и Householder.", "newton_preview_sq512.png"),
        Item("Итерируемые функции", "Фрактал Феникс", "Итерационная система, учитывающая текущее и предыдущее значения Z.", "phoenix_preview_sq512.png"),
        Item("Итерируемые функции", "Фрактал Коллатца", "Комплексное обобщение отображения Коллатца.", "collatz_preview_sq512.png"),
        Item("Математические лаборатории", "Арифметика по модулю", "Отображения x → ax+b mod N и функциональные графы на окружности: кардиоиды, нефроиды и плетёные конечные орбиты.", "inverse_collatz_tree_preview_sq512.png", MathematicalLaboratoryCatalog.LaunchKey(MathematicalLaboratoryKind.ModularArithmetic)),
        Item("Математические лаборатории", "Треугольник Паскаля по модулю N", "Цветовая карта C(n,k) mod m с режимом делимости и направляющими теоремы Люка.", "inverse_collatz_tree_preview_sq512.png", MathematicalLaboratoryCatalog.LaunchKey(MathematicalLaboratoryKind.PascalModulo)),
        Item("Математические лаборатории", "Лаборатория рациональных чисел", "Деревья Штерна—Броко и Калкина—Уилфа, последовательности Фарея, окружности Форда и цепные дроби.", "inverse_collatz_tree_preview_sq512.png", MathematicalLaboratoryCatalog.LaunchKey(MathematicalLaboratoryKind.RationalNumbers)),
        Item("Математические лаборатории", "Геометрия простых чисел", "Спирали Улама и Сакса, шестиугольная решётка, простые Гаусса и Эйзенштейна с раскраской по остаткам.", "inverse_collatz_tree_preview_sq512.png", MathematicalLaboratoryCatalog.LaunchKey(MathematicalLaboratoryKind.PrimeGeometry)),
        Item("Математические лаборатории", "Филлотаксис и иррациональные вращения", "Золотой угол, рациональные приближения и настраиваемые вращения образуют парастихии и лучевые резонансы.", "inverse_collatz_tree_preview_sq512.png", MathematicalLaboratoryCatalog.LaunchKey(MathematicalLaboratoryKind.Phyllotaxis)),
        Item("Математические лаборатории", "Инверсия окружностей и преобразования Мёбиуса", "Интерактивная инверсия точек и окружностей, повторные преобразования и комплексные дробно-линейные отображения.", "inverse_collatz_tree_preview_sq512.png", MathematicalLaboratoryCatalog.LaunchKey(MathematicalLaboratoryKind.CircleInversion)),
        Item("Математические лаборатории", "Обратное дерево Коллатца", "Точное обратное дерево целочисленного отображения Коллатца с радиальной и древовидной раскладкой, фильтрами по остаткам и анимацией роста.", "inverse_collatz_tree_preview_sq512.png", "InverseCollatzTree"),
        Item("Математические лаборатории", "Аполлонова прокладка", "Рекурсивная упаковка взаимно касающихся окружностей с раскраской по глубине, кривизне или родительской ветви.", "apollonian_preview_sq512.png", "ApollonianGasket"),
        Item("Математические лаборатории", "Подстановочные и апериодические мозаики", "Инфляция мозаик Penrose, Ammann—Beenker, Chair, Pinwheel, сфинкса и треугольников Фибоначчи.", "inverse_collatz_tree_preview_sq512.png", MathematicalLaboratoryCatalog.LaunchKey(MathematicalLaboratoryKind.AperiodicTilings)),
        Item("Математические лаборатории", "Domain Coloring", "Раскраска комплексных функций: аргумент f(z) задаёт оттенок, а модуль — яркость и контурные линии.", "domain_coloring_preview_sq512.png", "DomainColoring"),
        Item("Математические лаборатории", "Гиперболическая геометрия", "Мозаики {p,q}, геодезические и идеальная граница в модели диска Пуанкаре.", "inverse_collatz_tree_preview_sq512.png", MathematicalLaboratoryCatalog.LaunchKey(MathematicalLaboratoryKind.HyperbolicGeometry)),
        Item("Математические лаборатории", "Fourier Epicycles", "Рисование замкнутого контура, дискретный ряд Фурье, спектр коэффициентов и анимированная цепочка эпициклов.", "inverse_collatz_tree_preview_sq512.png", MathematicalLaboratoryCatalog.LaunchKey(MathematicalLaboratoryKind.FourierEpicycles)),
        Item("Математические лаборатории", "Фигуры Хладни и интерференция волн", "Стоячие волны квадратных пластин и круглых мембран, узловые линии и интерференционные поля когерентных источников.", "inverse_collatz_tree_preview_sq512.png", MathematicalLaboratoryCatalog.LaunchKey(MathematicalLaboratoryKind.ChladniWaveInterference)),
        Item("Итерируемые функции", "Фрактал Нова (Мандельброт)", "Модификация метода Ньютона с добавлением константы C.", "NovaMandelbrot_preview_sq512.png"),
        Item("Итерируемые функции", "Фрактал Нова (Жюлиа)", "Julia-вариант семейства Nova.", "NovaJulia_preview_sq512.png"),
        Item("Итерируемые функции", "Буддаброт / Анти-Буддаброт", "Накопительный рендер плотности посещения орбит.", "buddhabrot_f_preview_sq512.png"),
        Item("Итерируемые функции", "Фрактальное пламя (стохастическое)", "Стохастический рендер с накоплением HDR-гистограммы.", "flame_fractal_preview_sq512.png"),
        Item("Итерируемые функции", "DLA — диффузионно-ограниченная агрегация", "Случайно блуждающие частицы прилипают к растущему кластеру, образуя молнии, кораллы и морозные узоры. Процесс отображается в реальном времени.", "dla_preview_sq512.png", "DLA"),
        Item("Геометрические", "L‑системы и черепашья графика", "Редактор аксиом и правил с анимацией: Кох, Гильберт, Леви, Dragon Curve, деревья, растения и Серпинский.", "lsystem_preview_sq512.png", "LSystem"),
        Item("Геометрические", "Серпинский — игра хаоса", "Стохастическое построение треугольника Серпинского случайными переходами к вершинам.", "serpinski_preview_sq512.png", "SerpinskyChaos"),
        Item("Геометрические", "IFS Барнсли / Хейуэя", "Стохастическая система аффинных преобразований.", "ifs_fractal_preview_sq512.png"),
        Item("Динамические системы", "Экспонента Ляпунова", "Карта экспонент Ляпунова логистического отображения.", "lyapunov_preview_sq512.png", "Lyapunov"),
        Item("Аттракторы", "Аттрактор Лоренца", "Визуализация классической хаотической системы Лоренца.", "sig_preview_sq512.png", "Lorenz"),
        Item("Аттракторы", "Аттрактор Рёсслера", "Визуализация хаотической системы Рёсслера.", "rossler_preview_sq512.png", "Rossler"),
        Item("Динамические системы", "Логистическое отображение (орбиты)", "Орбитальный график логистического отображения.", "logistic_map_preview_sq512.png", "LogisticMap"),
        Item("Динамические системы", "Диаграмма бифуркации", "Диаграмма бифуркаций логистического отображения.", "bifurcation_preview_sq512.png", "Bifurcation"),
        Item("Аттракторы", "Карта Хенона", "Классическое двумерное отображение Хенона.", "henon_preview_sq512.png", "Henon"),
        Item("Аттракторы", "Отображение Икэды", "Двумерное нелинейное отображение Икэды.", "ikeda_preview_sq512.png", "Ikeda"),
        Item("Аттракторы", "Странные аттракторы", "Облака плотности для аттракторов Клиффорда, Питера де Йонга, Tinkerbell и Gumowski–Mira.", "strange_attractors_preview_sq512.png", "Attractors2D")
    ];

    private static FractalCatalogItem Item(
        string family,
        string name,
        string description,
        string previewFile,
        string? launchKey = null) =>
        new(family, name, description, $"Assets/Previews/{previewFile}", launchKey ?? GetLaunchKey(previewFile));

    private static string? GetLaunchKey(string previewFile) => previewFile switch
    {
        "mandelbrot_preview_sq512.png" => "Mandelbrot",
        "burningship_preview_sq512.png" => "BurningShip",
        "tricorn__preview_sq512.png" => "Tricorn",
        "buffalo_preview_sq512.png" => "Buffalo",
        "celtic_mandelbrot_preview_sq512.png" => "Celtic",
        "simonobrot_preview_sq512.png" => "Simonobrot",
        "general_mandelbrot_preview_sq512.png" => "Generalized",
        "julia_preview_sq512.png" => "Julia",
        "julia_burningship_preview_sq512.png" => "JuliaBurningShip",
        "newton_preview_sq512.png" => "NewtonPools",
        "phoenix_preview_sq512.png" => "Phoenix",
        "collatz_preview_sq512.png" => "Collatz",
        "NovaMandelbrot_preview_sq512.png" => "NovaMandelbrot",
        "NovaJulia_preview_sq512.png" => "NovaJulia",
        "buddhabrot_f_preview_sq512.png" => "Buddhabrot",
        "flame_fractal_preview_sq512.png" => "Flame",
        "ifs_fractal_preview_sq512.png" => "IFS",
        _ => null
    };
}
