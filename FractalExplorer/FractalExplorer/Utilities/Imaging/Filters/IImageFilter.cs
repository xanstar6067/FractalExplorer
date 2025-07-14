using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FractalExplorer.Utilities.Imaging.Filters
{
    public interface IImageFilter
    {
        /// <summary>
        /// Применяет фильтр к исходному изображению.
        /// </summary>
        /// <param name="sourceImage">Исходное изображение.</param>
        /// <returns>Новое изображение с примененным фильтром.</returns>
        Bitmap Apply(Bitmap sourceImage);
    }
}
