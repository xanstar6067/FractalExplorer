using FractalExplorer.Resources;

namespace FractalExplorer.Utilities.SaveIO.SaveStateImplementations
{
    /// <summary>
    /// Представляет состояние сохранения для фракталов Нова (разновидность Мандельброта).
    /// Включает общие параметры, такие как центр, масштаб, итерации, пороговое значение и имя палитры,
    /// а также специфичные для фрактала Нова комплексные параметры P, Z0 и параметр релаксации M.
    /// </summary>
    public class NovaMandelbrotSaveState : FractalSaveStateBase
    {
        /// <summary>
        /// Получает или задает X-координату центра фрактала в комплексной плоскости.
        /// </summary>
        public decimal CenterX { get; set; }

        /// <summary>
        /// Получает или задает Y-координату центра фрактала в комплексной плоскости.
        /// </summary>
        public decimal CenterY { get; set; }

        /// <summary>
        /// Получает или задает коэффициент масштабирования (зум) фрактала.
        /// </summary>
        public decimal Zoom { get; set; }

        /// <summary>
        /// Получает или задает пороговое значение, используемое для определения того,
        /// находится ли точка внутри множества.
        /// </summary>
        public decimal Threshold { get; set; }

        /// <summary>
        /// Получает или задает максимальное количество итераций,
        /// используемых при вычислении цвета каждой точки фрактала.
        /// </summary>
        public int Iterations { get; set; }

        /// <summary>
        /// Получает или задает имя цветовой палитры, примененной к фракталу.
        /// </summary>
        public string PaletteName { get; set; }

        /// <summary>
        /// Получает или задает действительную часть комплексной степени 'p',
        /// специфичной для итерационной формулы фрактала Нова.
        /// </summary>
        public decimal P_Re { get; set; }

        /// <summary>
        /// Получает или задает мнимую часть комплексной степени 'p',
        /// специфичной для итерационной формулы фрактала Нова.
        /// </summary>
        public decimal P_Im { get; set; }

        /// <summary>
        /// Получает или задает действительную часть начального значения 'z0',
        /// специфичного для итерационной формулы фрактала Нова.
        /// </summary>
        public decimal Z0_Re { get; set; }

        /// <summary>
        /// Получает или задает мнимую часть начального значения 'z0',
        /// специфичной для итерационной формулы фрактала Нова.
        /// </summary>
        public decimal Z0_Im { get; set; }

        /// <summary>
        /// Получает или задает параметр релаксации 'm',
        /// специфичный для итерационной формулы фрактала Нова.
        /// </summary>
        public decimal M { get; set; }


        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="NovaMandelbrotSaveState"/>.
        /// Этот конструктор необходим для корректной десериализации JSON.
        /// </summary>
        public NovaMandelbrotSaveState()
        {
        }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="NovaMandelbrotSaveState"/>
        /// с указанным идентификатором типа фрактала.
        /// </summary>
        /// <param name="fractalType">Строковый идентификатор типа фрактала (например, "NovaMandelbrot").</param>
        public NovaMandelbrotSaveState(string fractalType)
        {
            FractalType = fractalType;
        }
    }
}