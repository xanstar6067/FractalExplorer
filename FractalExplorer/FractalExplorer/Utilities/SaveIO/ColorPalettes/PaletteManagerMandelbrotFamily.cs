using System.Text.Json.Serialization;

namespace FractalExplorer.Utilities.SaveIO.ColorPalettes
{
    /// <summary>
    /// Базовый класс для определения цветовой палитры.
    /// Содержит имя палитры, список цветов, флаг градиента и признак, является ли палитра встроенной.
    /// </summary>
    public class PaletteManagerMandelbrotFamily
    {
        #region Properties

        /// <summary>
        /// Получает или устанавливает имя палитры.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Получает или устанавливает список цветов, составляющих палитру.
        /// </summary>
        public List<Color> Colors { get; set; } = new List<Color>();

        /// <summary>
        /// Получает или устанавливает значение, указывающее, следует ли использовать цвета как градиент (True)
        /// или как дискретные (циклические) цвета (False).
        /// </summary>
        public bool IsGradient { get; set; } = true;

        /// <summary>
        /// Получает или устанавливает значение гамма-коррекции для палитры.
        /// Значение 1.0 означает отсутствие коррекции.
        /// </summary>
        public double Gamma { get; set; } = 1.0;

        /// <summary>
        /// Получает или устанавливает максимальное количество итераций для одного полного цикла палитры.
        /// Это позволяет отвязать раскраску от общего числа итераций рендеринга.
        /// </summary>
        public int MaxColorIterations { get; set; } = 500;

        // НОВОЕ: Флаг для синхронизации длины палитры с итерациями рендера.
        /// <summary>
        /// Получает или устанавливает значение, указывающее, должна ли длина цветового цикла
        /// быть равна количеству итераций рендера (старое поведение).
        /// </summary>
        public bool AlignWithRenderIterations { get; set; } = false;

        /// <summary>
        /// Получает или устанавливает значение, указывающее, является ли палитра встроенной (предопределенной).
        /// Встроенные палитры не могут быть удалены или изменены пользователем через UI.
        /// Игнорируется при сериализации в JSON.
        /// </summary>
        [JsonIgnore]
        public bool IsBuiltIn { get; set; } = false;

        #endregion

        #region Constructors

        /// <summary>
        /// Инициализирует новый пустой экземпляр класса <see cref="PaletteManagerMandelbrotFamily"/>.
        /// Используется для десериализации.
        /// </summary>
        public PaletteManagerMandelbrotFamily() { }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="PaletteManagerMandelbrotFamily"/> с заданными параметрами.
        /// </summary>
        /// <param name="name">Имя палитры.</param>
        /// <param name="colors">Список цветов палитры.</param>
        /// <param name="isGradient">Флаг, указывающий, является ли палитра градиентом.</param>
        /// <param name="isBuiltIn">Флаг, указывающий, является ли палитра встроенной.</param>
        /// <param name="maxColorIterations">Длина цветового цикла.</param>
        /// <param name="gamma">Значение гамма-коррекции.</param>
        /// <param name="alignWithRenderIterations">Флаг для синхронизации с итерациями рендера.</param> // НОВОЕ
        public PaletteManagerMandelbrotFamily(string name, List<Color> colors, bool isGradient, bool isBuiltIn = false, int maxColorIterations = 500, double gamma = 1.0, bool alignWithRenderIterations = false)
        {
            Name = name;
            Colors = colors;
            IsGradient = isGradient;
            IsBuiltIn = isBuiltIn;
            MaxColorIterations = maxColorIterations;
            Gamma = gamma;
            AlignWithRenderIterations = alignWithRenderIterations;
        }

        #endregion
    }
}