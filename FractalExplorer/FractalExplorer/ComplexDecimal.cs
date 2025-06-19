using System;

namespace FractalDraving
{
    /// <summary>
    /// Представляет комплексное число, используя тип decimal для высокой точности.
    /// Эта структура является неизменяемой (immutable).
    /// </summary>
    public readonly struct ComplexDecimal
    {
        /// <summary>
        /// Реальная (действительная) часть комплексного числа.
        /// </summary>
        public readonly decimal Real;

        /// <summary>
        /// Мнимая часть комплексного числа.
        /// </summary>
        public readonly decimal Imaginary;

        /// <summary>
        /// Инициализирует новый экземпляр структуры ComplexDecimal.
        /// </summary>
        /// <param name="real">Реальная часть.</param>
        /// <param name="imaginary">Мнимая часть.</param>
        public ComplexDecimal(decimal real, decimal imaginary)
        {
            Real = real;
            Imaginary = imaginary;
        }

        /// <summary>
        /// Представляет ноль в виде комплексного числа (0, 0i).
        /// </summary>
        public static readonly ComplexDecimal Zero = new ComplexDecimal(0m, 0m);

        /// <summary>
        /// Возвращает квадрат модуля (величины) комплексного числа.
        /// Рассчитывается как Real² + Imaginary².
        /// Использование квадрата модуля позволяет избежать дорогостоящей операции извлечения квадратного корня.
        /// </summary>
        public decimal MagnitudeSquared => Real * Real + Imaginary * Imaginary;

        /// <summary>
        /// Перегрузка оператора сложения для двух комплексных чисел.
        /// </summary>
        public static ComplexDecimal operator +(ComplexDecimal a, ComplexDecimal b)
        {
            return new ComplexDecimal(a.Real + b.Real, a.Imaginary + b.Imaginary);
        }

        /// <summary>
        /// Перегрузка оператора умножения для двух комплексных чисел.
        /// (a + bi) * (c + di) = (ac - bd) + (ad + bc)i
        /// </summary>
        public static ComplexDecimal operator *(ComplexDecimal a, ComplexDecimal b)
        {
            decimal realPart = a.Real * b.Real - a.Imaginary * b.Imaginary;
            decimal imaginaryPart = a.Real * b.Imaginary + a.Imaginary * b.Real;
            return new ComplexDecimal(realPart, imaginaryPart);
        }

        /// <summary>
        /// Возвращает строковое представление комплексного числа.
        /// </summary>
        public override string ToString()
        {
            return $"{Real} + {Imaginary}i";
        }
    }
}