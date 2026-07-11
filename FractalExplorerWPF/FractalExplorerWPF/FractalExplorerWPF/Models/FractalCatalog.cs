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
        Item("Итерируемые функции", "Фрактал Нова (Мандельброт)", "Модификация метода Ньютона с добавлением константы C.", "NovaMandelbrot_preview_sq512.png"),
        Item("Итерируемые функции", "Фрактал Нова (Жюлиа)", "Julia-вариант семейства Nova.", "NovaJulia_preview_sq512.png"),
        Item("Итерируемые функции", "Буддаброт / Анти-Буддаброт", "Накопительный рендер плотности посещения орбит.", "buddhabrot_f_preview_sq512.png"),
        Item("Итерируемые функции", "Фрактальное пламя (стохастическое)", "Стохастический рендер с накоплением HDR-гистограммы.", "flame_fractal_preview_sq512.png"),
        Item("Геометрические", "Треугольник Серпинского", "Классический самоподобный геометрический фрактал.", "serpinski_preview_sq512.png", "Serpinsky"),
        Item("Геометрические", "IFS Барнсли / Хейуэя", "Стохастическая система аффинных преобразований.", "ifs_fractal_preview_sq512.png"),
        Item("Динамические системы", "Экспонента Ляпунова", "Карта экспонент Ляпунова логистического отображения.", "lyapunov_preview_sq512.png"),
        Item("Динамические системы", "Аттрактор Лоренца", "Визуализация классической хаотической системы Лоренца.", "sig_preview_sq512.png"),
        Item("Динамические системы", "Аттрактор Рёсслера", "Визуализация хаотической системы Рёсслера.", "rossler_preview_sq512.png"),
        Item("Динамические системы", "Логистическое отображение (орбиты)", "Орбитальный график логистического отображения.", "temporary_preview_sq512.png"),
        Item("Динамические системы", "Диаграмма бифуркации", "Диаграмма бифуркаций логистического отображения.", "bifurcation_preview_sq512.png"),
        Item("Динамические системы", "Карта Хенона", "Классическое двумерное отображение Хенона.", "temporary_preview_sq512.png"),
        Item("Динамические системы", "Отображение Икэды", "Двумерное нелинейное отображение Икэды.", "temporary_preview_sq512.png")
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
        _ => null
    };
}
