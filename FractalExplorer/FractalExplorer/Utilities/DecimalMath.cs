using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FractalExplorer.Utilities
{
    public static class DecimalMath
    {
        // Константы с максимальной точностью
        private static readonly decimal PI = 3.1415926535897932384626433833m;
        private static readonly decimal PI_OVER_2 = PI / 2m;
        private static readonly decimal TWO_PI = 2m * PI;

        // Точность для остановки вычислений
        private const decimal Epsilon = 1e-28m;

        /// <summary>
        /// Вычисляет e^x для decimal.
        /// </summary>
        public static decimal Exp(decimal d)
        {
            if (d == 0m) return 1m;

            decimal sum = 1m;
            decimal term = 1m;
            int n = 1;

            while (Math.Abs(term) > Epsilon)
            {
                term *= d / n;
                if (term == 0m) break; // Достигнут предел точности
                sum += term;
                n++;
                // Защита от бесконечного цикла
                if (n > 1000) break;
            }
            return sum;
        }

        /// <summary>
        /// Приводит угол в радианах к диапазону [-PI, PI].
        /// </summary>
        private static decimal ReduceAngle(decimal angle)
        {
            angle %= TWO_PI;
            if (angle > PI)
                angle -= TWO_PI;
            else if (angle < -PI)
                angle += TWO_PI;
            return angle;
        }

        /// <summary>
        /// Вычисляет sin(x) для decimal.
        /// </summary>
        public static decimal Sin(decimal d)
        {
            d = ReduceAngle(d);

            decimal sum = d;
            decimal term = d;
            int n = 1;

            while (Math.Abs(term) > Epsilon)
            {
                term *= -1m * d * d / ((2 * n + 1) * (2 * n));
                if (term == 0m) break;
                sum += term;
                n++;
                if (n > 500) break;
            }
            return sum;
        }

        /// <summary>
        /// Вычисляет cos(x) для decimal.
        /// </summary>
        public static decimal Cos(decimal d)
        {
            d = ReduceAngle(d);

            decimal sum = 1m;
            decimal term = 1m;
            int n = 1;

            while (Math.Abs(term) > Epsilon)
            {
                term *= -1m * d * d / ((2 * n - 1) * (2 * n));
                if (term == 0m) break;
                sum += term;
                n++;
                if (n > 500) break;
            }
            return sum;
        }

        /// <summary>
        /// Вычисляет натуральный логарифм ln(d) для decimal.
        /// Использует более быстро сходящийся ряд для atanh.
        /// </summary>
        public static decimal Log(decimal d)
        {
            if (d <= 0) throw new ArgumentOutOfRangeException(nameof(d), "Аргумент логарифма должен быть положительным.");
            if (d == 1m) return 0m;

            // Используем формулу log(x) = 2 * atanh((x-1)/(x+1))
            decimal x = (d - 1m) / (d + 1m);
            decimal sum = x;
            decimal term = x;
            int n = 1;

            while (Math.Abs(term) > Epsilon)
            {
                term *= x * x * (2 * n - 1) / (2 * n + 1);
                if (term == 0m) break;
                sum += term;
                n++;
                if (n > 1000) break;
            }
            return 2m * sum;
        }

        /// <summary>
        /// Вычисляет арктангенс atan(d) для decimal.
        /// </summary>
        public static decimal Atan(decimal d)
        {
            if (Math.Abs(d) > 1m)
            {
                return (d > 0 ? PI_OVER_2 : -PI_OVER_2) - Atan(1m / d);
            }

            decimal sum = d;
            decimal term = d;
            int n = 1;

            while (Math.Abs(term) > Epsilon)
            {
                term *= -1m * d * d * (2 * n - 1) / (2 * n + 1);
                if (term == 0m) break;
                sum += term;
                n++;
                if (n > 1000) break;
            }
            return sum;
        }

        /// <summary>
        /// Вычисляет арктангенс atan2(y, x) для decimal.
        /// </summary>
        public static decimal Atan2(decimal y, decimal x)
        {
            if (x > 0m) return Atan(y / x);
            if (x < 0m)
            {
                if (y >= 0m) return Atan(y / x) + PI;
                return Atan(y / x) - PI;
            }
            // x == 0
            if (y > 0m) return PI_OVER_2;
            if (y < 0m) return -PI_OVER_2;
            return 0m; // y == 0, x == 0
        }
    }
}
