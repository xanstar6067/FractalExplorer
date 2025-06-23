using System.Collections.Generic;
using System.Drawing;
using System.Text.Json.Serialization;

namespace FractalExplorer.Core
{
    /// <summary>
    /// Базовый класс для определения цветовой палитры.
    /// Содержит имя палитры, список цветов, флаг градиента и признак, является ли палитра встроенной.
    /// </summary>
    public class ColorPaletteBase
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
        /// Получает или устанавливает значение, указывающее, является ли палитра встроенной (предопределенной).
        /// Встроенные палитры не могут быть удалены или изменены пользователем через UI.
        /// Игнорируется при сериализации в JSON.
        /// </summary>
        [JsonIgnore]
        public bool IsBuiltIn { get; set; } = false;

        #endregion

        #region Constructors

        /// <summary>
        /// Инициализирует новый пустой экземпляр класса <see cref="ColorPaletteBase"/>.
        /// Используется для десериализации.
        /// </summary>
        public ColorPaletteBase() { }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="ColorPaletteBase"/> с заданными параметрами.
        /// </summary>
        /// <param name="name">Имя палитры.</param>
        /// <param name="colors">Список цветов палитры.</param>
        /// <param name="isGradient">Флаг, указывающий, является ли палитра градиентом.</param>
        /// <param name="isBuiltIn">Флаг, указывающий, является ли палитра встроенной.</param>
        public ColorPaletteBase(string name, List<Color> colors, bool isGradient, bool isBuiltIn = false)
        {
            Name = name;
            Colors = colors;
            IsGradient = isGradient;
            IsBuiltIn = isBuiltIn;
        }

        #endregion
    }
}