public static class DecimalMath
{
    // Задаем высокоточные константы
    private static readonly decimal PI = 3.1415926535897932384626433832m;
    private static readonly decimal PI_X_2 = 6.2831853071795864769252867664m;
    private static readonly decimal PI_OVER_2 = 1.5707963267948966192313216916m;

    /// <summary>
    /// Вычисляет косинус для decimal с высокой производительностью и точностью.
    /// </summary>
    public static decimal Cos(decimal x)
    {
        // *** КЛЮЧЕВОЕ ИСПРАВЛЕНИЕ ПРОИЗВОДИТЕЛЬНОСТИ ***
        // Используем оператор % для мгновенного приведения больших чисел.
        // Это заменяет медленный цикл while.
        if (Math.Abs(x) > PI_X_2)
        {
            x %= PI_X_2;
        }

        // *** ОПТИМИЗАЦИЯ СКОРОСТИ СХОДИМОСТИ ***
        // Приводим аргумент к диапазону [-PI/2, PI/2] с помощью тождеств,
        // чтобы ряд Тейлора сходился максимально быстро.
        // cos(x) = cos(-x)
        if (x < 0) x = -x;
        // cos(x) = -cos(x - PI)
        if (x > PI)
        {
            return -Cos(x - PI);
        }
        // cos(x) = sin(PI/2 - x)
        if (x > PI_OVER_2)
        {
            return Sin(PI_OVER_2 - x);
        }

        // Ряд Тейлора для Cos(x): 1 - x^2/2! + x^4/4! - x^6/6! + ...
        // Теперь он работает на малых значениях 'x', что очень быстро.
        decimal result = 1.0m;
        decimal term = 1.0m;
        // Уменьшаем количество итераций, так как сходимость теперь очень быстрая
        for (int i = 1; i <= 30; i += 2)
        {
            term *= -1 * x * x / (i * (i + 1));
            if (term == 0m) break; // Дальнейшие члены будут нулевыми
            result += term;
        }
        return result;
    }

    /// <summary>
    /// Вычисляет синус для decimal с высокой производительностью и точностью.
    /// </summary>
    public static decimal Sin(decimal x)
    {
        // *** КЛЮЧЕВОЕ ИСПРАВЛЕНИЕ ПРОИЗВОДИТЕЛЬНОСТИ ***
        if (Math.Abs(x) > PI_X_2)
        {
            x %= PI_X_2;
        }

        // *** ОПТИМИЗАЦИЯ СКОРОСТИ СХОДИМОСТИ ***
        // Приводим к диапазону [-PI/2, PI/2]
        // sin(x) = -sin(-x)
        if (x < 0) return -Sin(-x);
        // sin(x) = -sin(x - PI)
        if (x > PI)
        {
            return -Sin(x - PI);
        }
        // sin(x) = cos(PI/2 - x)
        if (x > PI_OVER_2)
        {
            return Cos(PI_OVER_2 - x);
        }

        // Ряд Тейлора для Sin(x): x - x^3/3! + x^5/5! - x^7/7! + ...
        decimal result = x;
        decimal term = x;
        for (int i = 2; i <= 30; i += 2)
        {
            term *= -1 * x * x / (i * (i + 1));
            if (term == 0m) break;
            result += term;
        }
        return result;
    }
}