using FractalExplorer.Engines; // <-- Добавьте эту директиву using для доступа к CollatzVariation
using FractalExplorer.Resources;
using System;
using System.Collections.Generic;
using System.Drawing; // <-- Добавьте эту директиву using для доступа к Bitmap
using System.Threading;
using System.Threading.Tasks;

namespace FractalExplorer.Utilities.RenderUtilities
{
    /// <summary>
    /// Представляет состояние фрактала для рендеринга в высоком разрешении.
    /// </summary>
    public class HighResRenderState
    {
        public string EngineType { get; set; }
        public decimal CenterX { get; set; }
        public decimal CenterY { get; set; }
        public decimal Zoom { get; set; }
        public decimal BaseScale { get; set; }
        public int Iterations { get; set; }
        public decimal Threshold { get; set; }
        public string ActivePaletteName { get; set; }
        public ComplexDecimal? JuliaC { get; set; }
        public string FileNameDetails { get; set; }
        public bool UseSmoothColoring { get; set; }
        public decimal? Power { get; set; } // Степень 'p' для Обобщенного Мандельброта
        public decimal Scale { get; set; }
        public bool UseInversion { get; set; }

        #region Collatz Specific Parameters
        /// <summary>
        /// Получает или задает вариацию формулы для фрактала Коллатца.
        /// Используется только движком Коллатца.
        /// </summary>
        public CollatzVariation? Variation { get; set; }

        /// <summary>
        /// Получает или задает параметр 'P' для обобщенной вариации Коллатца.
        /// Используется только движком Коллатца.
        /// </summary>
        public decimal? P_Parameter { get; set; }
        #endregion

        public HighResRenderState Clone()
        {
            // MemberwiseClone корректно скопирует все свойства, включая новые.
            return (HighResRenderState)this.MemberwiseClone();
        }
    }

    /// <summary>
    /// Описывает прогресс выполнения операции рендеринга.
    /// </summary>
    public class RenderProgress
    {
        public int Percentage { get; set; }
        public string Status { get; set; }
    }

    /// <summary>
    /// Интерфейс для объектов, которые могут быть отрендерены в высоком разрешении.
    /// </summary>
    public interface IHighResRenderable
    {

        /// <summary>
        /// Получает текущее состояние фрактала для рендеринга.
        /// </summary>
        HighResRenderState GetRenderState();

        /// <summary>
        /// Асинхронно рендерит фрактал в высоком разрешении.
        /// </summary>
        Task<Bitmap> RenderHighResolutionAsync(HighResRenderState state, int width, int height, int ssaaFactor, IProgress<RenderProgress> progress, CancellationToken cancellationToken);

        /// <summary>
        /// Рендерит небольшое превью фрактала.
        /// </summary>
        Bitmap RenderPreview(HighResRenderState state, int previewWidth, int previewHeight);
    }
}