using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FractalExplorer.Utilities
{
    /// <summary>
    /// Представляет комплексное число, используя тип <see cref="double"/> для высокой производительности.
    /// Эта структура является неизменяемой (immutable).
    /// </summary>
    public readonly struct ComplexDouble
    {
        /// <summary>
        /// Действительная часть комплексного числа.
        /// </summary>
        public readonly double Real;

        /// <summary>
        /// Мнимая часть комплексного числа.
        /// </summary>
        public readonly double Imaginary;

        /// <summary>
        /// Получает аргумент (угол) комплексного числа в радианах.
        /// </summary>
        /// <value>Значение от -π до π.</value>
        public double Argument => Math.Atan2(Imaginary, Real);

        /// <summary>
        /// Получает квадрат модуля (длины) комплексного числа.
        /// Это свойство используется для оптимизации, чтобы избежать вызова <see cref="Math.Sqrt"/>.
        /// </summary>
        public double MagnitudeSquared => Real * Real + Imaginary * Imaginary;

        /// <summary>
        /// Получает модуль (длину) комплексного числа.
        /// </summary>
        public double Magnitude => Math.Sqrt(MagnitudeSquared);

        /// <summary>
        /// Инициализирует новый экземпляр структуры <see cref="ComplexDouble"/> с указанными действительной и мнимой частями.
        /// </summary>
        /// <param name="real">Действительная часть.</param>
        /// <param name="imaginary">Мнимая часть.</param>
        public ComplexDouble(double real, double imaginary)
        {
            Real = real;
            Imaginary = imaginary;
        }

        /// <summary>
        /// Представляет нулевое комплексное число (0, 0).
        /// </summary>
        public static readonly ComplexDouble Zero = new ComplexDouble(0.0, 0.0);

        /// <summary>
        /// Складывает два комплексных числа.
        /// </summary>
        /// <param name="a">Первое комплексное число.</param>
        /// <param name="b">Второе комплексное число.</param>
        /// <returns>Сумма двух комплексных чисел.</returns>
        public static ComplexDouble operator +(ComplexDouble a, ComplexDouble b)
        {
            return new ComplexDouble(a.Real + b.Real, a.Imaginary + b.Imaginary);
        }

        /// <summary>
        /// Умножает два комплексных числа.
        /// </summary>
        /// <param name="a">Первое комплексное число.</param>
        /// <param name="b">Второе комплексное число.</param>
        /// <returns>Произведение двух комплексных чисел.</returns>
        public static ComplexDouble operator *(ComplexDouble a, ComplexDouble b)
        {
            double realPart = a.Real * b.Real - a.Imaginary * b.Imaginary;
            double imaginaryPart = a.Real * b.Imaginary + a.Imaginary * b.Real;
            return new ComplexDouble(realPart, imaginaryPart);
        }

        /// <summary>
        /// Возвращает строковое представление текущего комплексного числа.
        /// </summary>
        /// <returns>Строка в формате "Real + Imaginaryi".</returns>
        public override string ToString()
        {
            return $"{Real} + {Imaginary}i";
        }
    }
}
