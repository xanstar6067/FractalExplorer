public static class DecimalMath
{
    // Определим константы с высокой точностью
    private static readonly decimal DecimalPI = 3.1415926535897932384626433833m;
    private static readonly decimal DecimalTwoPI = 2 * DecimalPI;

    // Количество итераций для ряда Тейлора. 
    // Для decimal 20-25 итераций обычно дают превосходную точность.
    private const int TaylorSeriesIterations = 28;

    /// <summary>
    /// Вычисляет косинус для числа типа decimal.
    /// </summary>
    public static decimal Cos(decimal angle)
    {
        // Приведение угла к диапазону [-2π, 2π] для быстрой сходимости ряда
        angle %= DecimalTwoPI;

        decimal result = 1.0m;
        decimal term;
        decimal power = angle * angle; // x^2
        decimal factorial = 2.0m;
        int sign = -1;

        for (int i = 1; i <= TaylorSeriesIterations; i++)
        {
            term = power / factorial;
            result += sign * term;

            // Если очередной член ряда стал неотличим от нуля для decimal, можно остановиться.
            if (term == 0m) break;

            sign *= -1;
            power *= angle * angle; // Увеличиваем степень на 2
            factorial *= (2 * i + 1) * (2 * i + 2); // Увеличиваем факториал до (2n+2)!
        }
        return result;
    }

    /// <summary>
    /// Вычисляет синус для числа типа decimal.
    /// </summary>
    public static decimal Sin(decimal angle)
    {
        // Приведение угла к диапазону [-2π, 2π]
        angle %= DecimalTwoPI;

        decimal result = angle;
        decimal term;
        decimal power = angle * angle * angle; // x^3
        decimal factorial = 6.0m; // 3!
        int sign = -1;

        for (int i = 1; i <= TaylorSeriesIterations; i++)
        {
            term = power / factorial;
            result += sign * term;

            if (term == 0m) break;

            sign *= -1;
            power *= angle * angle;
            factorial *= (2 * i + 2) * (2 * i + 3);
        }
        return result;
    }

    /// <summary>
    /// Вычисляет гиперболический косинус для числа типа decimal.
    /// </summary>
    public static decimal Cosh(decimal value)
    {
        // Для гиперболических функций приведение не так критично, но может помочь при очень больших значениях
        decimal result = 1.0m;
        decimal term;
        decimal power = value * value;
        decimal factorial = 2.0m;

        for (int i = 1; i <= TaylorSeriesIterations; i++)
        {
            term = power / factorial;
            result += term;

            if (term == 0m) break;

            power *= value * value;
            factorial *= (2 * i + 1) * (2 * i + 2);
        }
        return result;
    }

    /// <summary>
    /// Вычисляет гиперболический синус для числа типа decimal.
    /// </summary>
    public static decimal Sinh(decimal value)
    {
        decimal result = value;
        decimal term;
        decimal power = value * value * value;
        decimal factorial = 6.0m;

        for (int i = 1; i <= TaylorSeriesIterations; i++)
        {
            term = power / factorial;
            result += term;

            if (term == 0m) break;

            power *= value * value;
            factorial *= (2 * i + 2) * (2 * i + 3);
        }
        return result;
    }
}