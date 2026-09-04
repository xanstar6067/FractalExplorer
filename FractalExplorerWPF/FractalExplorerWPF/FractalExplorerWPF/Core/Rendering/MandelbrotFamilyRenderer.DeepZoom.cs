using System.Globalization;
using FractalExplorerWPF.Core.NewtonMath;
using FractalExplorerWPF.Models;
using Color = System.Windows.Media.Color;

namespace FractalExplorerWPF.Core.Rendering;

/// <summary>
/// «Второй двигатель» рендера семейства Мандельброта — пертурбационный метод.
///
/// Для Mandelbrot/Julia лестница точности схлопнута до двух ступеней: плоский double
/// (<see cref="Iterate"/>) до <see cref="PerturbationZoomThreshold"/> (~1.5e9), выше —
/// этот движок. Ступень <see cref="decimal"/> (<see cref="IterateDecimal"/>) осталась
/// только для 7 «неглубоких» вариантов и режимов Histogram/DistanceEstimation.
///
/// 1. Один раз на кадр считается опорная орбита <c>Zₙ</c> в центре области — в
///    <see cref="BigFloat"/> с адаптивной точностью (<see cref="PlanDeepZoom"/>),
///    результат кэшируется в double-массивах.
/// 2. Каждый пиксель итерирует лишь отклонение <c>δₙ = zₙ − Zₙ</c>:
///    <c>δₙ₊₁ = 2·Zₙ·δₙ + δₙ² + δc</c> (для Жюлиа слагаемое δc отсутствует, а δc задаёт δ₀).
///    Представление δ — обычный double либо <see cref="FloatExp"/> за ~1e72 (тот же
///    <see cref="PlanDeepZoom"/>).
/// 3. Потеря значимости («глитчи») лечится rebasing по Zhuoran: когда |zₙ| &lt; |δₙ| или
///    опорная орбита закончилась, δ сбрасывается в <c>z − Z₀</c>, а индекс опорной точки — в 0.
///
/// Раскраска, палитры и <see cref="PixelMetrics"/> переиспользуются из основного файла —
/// поэтому класс объявлен <c>partial</c>.
/// </summary>
public static partial class MandelbrotFamilyRenderer
{
    private sealed class ReferenceOrbit
    {
        public required double[] Re;
        public required double[] Im;

        /// <summary>Количество заполненных точек (индексы 0..<see cref="Length"/>-1).</summary>
        public required int Length;

        /// <summary>Опорная орбита вышла за радиус раньше, чем достигла числа итераций.</summary>
        public required bool Escaped;
    }

    // Критерий Pauldelbrot для rebasing: если |z|² падает ниже этой доли от |Zref|²,
    // опорная точка считается ненадёжной. 1e-6 — общепринятое значение.
    private const double GlitchToleranceSquared = 1e-6;

    private static readonly object _orbitLock = new();
    private static string? _orbitKey;
    private static ReferenceOrbit? _orbitCache;

    // Тестовый шов: принудительно включает/выключает пертурбационный движок независимо от
    // зума (в пределах поддерживаемых вариантов и режимов). null — по порогу зума.
    internal static bool? ForceDeepZoomForTests { get; set; }

    private static bool ShouldUseDeepZoom(MandelbrotState state)
    {
        if (state.Variant is not (MandelbrotVariant.Mandelbrot or MandelbrotVariant.Julia))
            return false;
        if (state.ColoringMode is MandelbrotColoringMode.Histogram or MandelbrotColoringMode.DistanceEstimation)
            return false;
        return ForceDeepZoomForTests ?? (state.Zoom > PerturbationZoomThreshold);
    }

    // ------------------------------------------------------------------ precision planner

    // log2(зума), начиная с которого отклонение δ на пиксель уже не помещается надёжно в
    // обычный double (δ² уходит в денормалы и в ноль): 2^239 ≈ 8.8e71. Ниже порога
    // работает проверенный double-δ путь, бит-в-бит совпадающий с прежним движком; выше —
    // δ ведётся в FloatExp. Порог взят с большим запасом ниже реального отказа double
    // (~1e290), чтобы полоса совпадения с прежним поведением была максимально широкой.
    private const double FloatExpDeltaZoomBits = 239;

    // Тестовый шов: принудительно задаёт представление δ (true — FloatExp, false — double).
    // null — выбор по <see cref="PlanDeepZoom"/>. Используется только из проверочного проекта.
    internal static bool? ForceFloatExpDeltaForTests { get; set; }

    /// <summary>
    /// План точности на кадр: разрядность мантиссы опорной орбиты (адаптивно от глубины
    /// зума и числа итераций, но не ниже <see cref="BigFloat.MinimumPrecisionBits"/>) и
    /// выбор представления δ (double либо FloatExp).
    /// </summary>
    private readonly record struct DeepZoomPlan(int ReferenceBits, bool UseFloatExpDelta);

    private static DeepZoomPlan PlanDeepZoom(MandelbrotState state)
    {
        double zoomBits = state.Zoom > 0 && double.IsFinite(state.Zoom)
            ? System.Math.Log2(state.Zoom)
            : 0;

        // Бит на разрешение соседних пикселей ≈ log2(zoom); удвоенный запас по числу
        // итераций поглощает накопление ошибки округления вдоль опорной орбиты; +48 —
        // суб-пиксельная точность и общий люфт. Ниже ~1e93 формула даёт < 384 и рабочая
        // точность остаётся ровно 384 — прежнее поведение сохраняется бит-в-бит.
        int iterationBits = BitLength(System.Math.Max(state.Iterations, 2));
        int needed = (int)System.Math.Ceiling(zoomBits) + 2 * iterationBits + 48;
        int referenceBits = System.Math.Max(BigFloat.MinimumPrecisionBits, RoundUpToMultiple(needed, 64));

        bool floatExpDelta = ForceFloatExpDeltaForTests ?? (zoomBits >= FloatExpDeltaZoomBits);
        return new DeepZoomPlan(referenceBits, floatExpDelta);
    }

    private static int BitLength(int value) =>
        32 - System.Numerics.BitOperations.LeadingZeroCount((uint)value);

    private static int RoundUpToMultiple(int value, int multiple) =>
        (value + multiple - 1) / multiple * multiple;

    private static PixelMetrics DeepZoomPixelDispatch(
        DeepZoomPlan plan,
        MandelbrotState state,
        ReferenceOrbit orbit,
        bool isJulia,
        double deltaReal,
        double deltaImaginary,
        double escapeSquared,
        CancellationToken token) =>
        plan.UseFloatExpDelta
            ? DeepZoomPixelFloatExp(state, orbit, isJulia, deltaReal, deltaImaginary, escapeSquared, token)
            : DeepZoomPixel(state, orbit, isJulia, deltaReal, deltaImaginary, escapeSquared, token);

    // ------------------------------------------------------------------ entry points

    private static byte[]? RenderDeepZoomTile(
        MandelbrotState state,
        int canvasWidth,
        int canvasHeight,
        MandelbrotRenderTile tile,
        CancellationToken token)
    {
        DeepZoomPlan plan = PlanDeepZoom(state);
        ReferenceOrbit orbit = GetReferenceOrbit(state, plan.ReferenceBits);
        if (IsDegenerateOrbit(orbit, state.Iterations))
            return RenderBruteForceTile(state, canvasWidth, canvasHeight, tile, token);

        bool isJulia = state.Variant == MandelbrotVariant.Julia;
        double escapeSquared = (double)(state.Threshold * state.Threshold);
        double viewWidth = 3.0 / state.Zoom;
        double viewHeight = viewWidth * canvasHeight / canvasWidth;

        int stride = checked(tile.Width * 4);
        var buffer = new byte[checked(stride * tile.Height)];

        for (int localY = 0; localY < tile.Height; localY++)
        {
            if (token.IsCancellationRequested) return null;
            int y = tile.Y + localY;
            double deltaImaginary = (0.5 - (double)y / canvasHeight) * viewHeight;
            int row = localY * stride;
            for (int localX = 0; localX < tile.Width; localX++)
            {
                int x = tile.X + localX;
                double deltaReal = ((double)x / canvasWidth - 0.5) * viewWidth;
                PixelMetrics metrics = DeepZoomPixelDispatch(
                    plan, state, orbit, isJulia, deltaReal, deltaImaginary, escapeSquared, token);
                Color color = ResolveColor(state, metrics, 0);
                int offset = row + localX * 4;
                buffer[offset] = color.B;
                buffer[offset + 1] = color.G;
                buffer[offset + 2] = color.R;
                buffer[offset + 3] = 255;
            }
        }

        return token.IsCancellationRequested ? null : buffer;
    }

    private static void RenderDeepZoom(
        MandelbrotState state,
        byte[] buffer,
        int width,
        int height,
        int stride,
        CancellationToken token,
        Action<int>? reportProgress)
    {
        DeepZoomPlan plan = PlanDeepZoom(state);
        ReferenceOrbit orbit = GetReferenceOrbit(state, plan.ReferenceBits);
        if (IsDegenerateOrbit(orbit, state.Iterations))
        {
            RenderBruteForceFull(state, buffer, width, height, stride, token, reportProgress);
            return;
        }

        bool isJulia = state.Variant == MandelbrotVariant.Julia;
        double escapeSquared = (double)(state.Threshold * state.Threshold);
        double viewWidth = 3.0 / state.Zoom;
        double viewHeight = viewWidth * height / width;

        int threads = state.Threads <= 0 ? Environment.ProcessorCount : state.Threads;
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = System.Math.Clamp(threads, 1, Environment.ProcessorCount)
        };
        int completedRows = 0;

        Parallel.For(0, height, options, (y, loopState) =>
        {
            if (token.IsCancellationRequested) { loopState.Stop(); return; }
            int row = y * stride;
            double deltaImaginary = (0.5 - (double)y / height) * viewHeight;
            for (int x = 0; x < width; x++)
            {
                if ((x & 63) == 0 && token.IsCancellationRequested) { loopState.Stop(); return; }
                double deltaReal = ((double)x / width - 0.5) * viewWidth;
                PixelMetrics metrics = DeepZoomPixelDispatch(
                    plan, state, orbit, isJulia, deltaReal, deltaImaginary, escapeSquared, token);
                Color color = ResolveColor(state, metrics, 0);
                int offset = row + x * 4;
                buffer[offset] = color.B;
                buffer[offset + 1] = color.G;
                buffer[offset + 2] = color.R;
                buffer[offset + 3] = 255;
            }

            int done = Interlocked.Increment(ref completedRows);
            if (done == height || done % System.Math.Max(1, height / 100) == 0)
                reportProgress?.Invoke(done * 100 / height);
        });
    }

    // ------------------------------------------------------------------ reference orbit

    // Короткая опорная орбита (центр ушёл за радиус почти сразу) — единственный случай,
    // когда пертурбации нечем оперировать. Рано вышедшая, но не мгновенно, орбита
    // отлично обслуживается rebasing'ом — это обычный случай для глубокого «внешнего» вида.
    private static bool IsDegenerateOrbit(ReferenceOrbit orbit, int iterations) => orbit.Length < 4;

    private static ReferenceOrbit GetReferenceOrbit(MandelbrotState state, int referenceBits)
    {
        string centerXRaw = state.CenterXExact is { Length: > 0 } exactX
            ? exactX
            : state.CenterX.ToString(CultureInfo.InvariantCulture);
        string centerYRaw = state.CenterYExact is { Length: > 0 } exactY
            ? exactY
            : state.CenterY.ToString(CultureInfo.InvariantCulture);

        string key = string.Join('|',
            centerXRaw,
            centerYRaw,
            state.Zoom.ToString(CultureInfo.InvariantCulture),
            state.Iterations.ToString(CultureInfo.InvariantCulture),
            ((int)state.Variant).ToString(CultureInfo.InvariantCulture),
            state.JuliaCReal.ToString(CultureInfo.InvariantCulture),
            state.JuliaCImaginary.ToString(CultureInfo.InvariantCulture),
            state.Threshold.ToString(CultureInfo.InvariantCulture),
            referenceBits.ToString(CultureInfo.InvariantCulture));

        lock (_orbitLock)
        {
            if (_orbitKey == key && _orbitCache is not null) return _orbitCache;

            ReferenceOrbit orbit = ComputeReferenceOrbit(state, centerXRaw, centerYRaw, referenceBits);
            _orbitKey = key;
            _orbitCache = orbit;
            return orbit;
        }
    }

    private static ReferenceOrbit ComputeReferenceOrbit(
        MandelbrotState state, string centerXRaw, string centerYRaw, int referenceBits)
    {
        // Вся арифметика центра и опорной орбиты — с адаптивной точностью; ниже ~1e93
        // referenceBits == MinimumPrecisionBits, поэтому парсинг и итерация идут ровно
        // так же, как раньше. Парсим внутри области: Parse тоже округляет до рабочей точности.
        using var precision = new BigFloat.PrecisionScope(referenceBits);

        BigFloat centerX = BigFloat.Parse(centerXRaw);
        BigFloat centerY = BigFloat.Parse(centerYRaw);

        int capacity = state.Iterations + 1;
        var re = new double[capacity];
        var im = new double[capacity];

        bool isJulia = state.Variant == MandelbrotVariant.Julia;
        BigFloat constantReal = isJulia ? BigFloat.FromDecimal(state.JuliaCReal) : centerX;
        BigFloat constantImaginary = isJulia ? BigFloat.FromDecimal(state.JuliaCImaginary) : centerY;
        BigFloat zReal = isJulia ? centerX : BigFloat.Zero;
        BigFloat zImaginary = isJulia ? centerY : BigFloat.Zero;
        BigFloat two = BigFloat.FromInt(2);

        // Опорную орбиту гоняем до большого радиуса, чтобы она давала полезные точки как
        // можно дольше; настоящий bailout пикселя гораздо меньше, а хвост закрывает rebasing.
        const double referenceEscapeSquared = 1e18;
        int length = 0;
        bool escaped = false;

        for (int index = 0; index < capacity; index++)
        {
            double realDouble = zReal.ToDouble();
            double imaginaryDouble = zImaginary.ToDouble();
            re[index] = realDouble;
            im[index] = imaginaryDouble;
            length = index + 1;

            double magnitudeSquared = realDouble * realDouble + imaginaryDouble * imaginaryDouble;
            if (!double.IsFinite(magnitudeSquared) || magnitudeSquared > referenceEscapeSquared)
            {
                escaped = true;
                break;
            }

            BigFloat nextReal = zReal * zReal - zImaginary * zImaginary + constantReal;
            BigFloat nextImaginary = two * zReal * zImaginary + constantImaginary;
            zReal = nextReal;
            zImaginary = nextImaginary;
        }

        return new ReferenceOrbit { Re = re, Im = im, Length = length, Escaped = escaped };
    }

    // ------------------------------------------------------------------ per-pixel perturbation

    private static PixelMetrics DeepZoomPixel(
        MandelbrotState state,
        ReferenceOrbit orbit,
        bool isJulia,
        double deltaConstantReal,
        double deltaConstantImaginary,
        double escapeSquared,
        CancellationToken token)
    {
        int maxIterations = state.Iterations;
        bool trackTrap = state.ColoringMode == MandelbrotColoringMode.OrbitTrap;
        bool trackStripe = state.ColoringMode == MandelbrotColoringMode.StripeAverage;

        // Мандельброт: δ₀ = 0, а δc добавляется каждый шаг. Жюлиа: c постоянна, поэтому
        // δc не добавляется, но задаёт начальное возмущение δ₀.
        double deltaReal = isJulia ? deltaConstantReal : 0.0;
        double deltaImaginary = isJulia ? deltaConstantImaginary : 0.0;
        double addReal = isJulia ? 0.0 : deltaConstantReal;
        double addImaginary = isJulia ? 0.0 : deltaConstantImaginary;

        int referenceIndex = 0;
        int iteration = 0;
        double magnitudeSquared = 0;
        double minTrap = double.MaxValue;
        double stripe = 0;
        bool escaped = false;

        while (iteration < maxIterations)
        {
            if ((iteration & 8191) == 0 && token.IsCancellationRequested) return default;

            double referenceReal = orbit.Re[referenceIndex];
            double referenceImaginary = orbit.Im[referenceIndex];

            double currentReal = referenceReal + deltaReal;
            double currentImaginary = referenceImaginary + deltaImaginary;
            if (trackTrap)
                minTrap = System.Math.Min(minTrap,
                    System.Math.Min(System.Math.Abs(currentReal), System.Math.Abs(currentImaginary)));
            if (trackStripe)
                stripe += 0.5 + 0.5 * System.Math.Sin(
                    state.StripeFrequency * System.Math.Atan2(currentImaginary, currentReal));

            // δ ← 2·Z·δ + δ² + δc
            double twoZDeltaReal = 2 * (referenceReal * deltaReal - referenceImaginary * deltaImaginary);
            double twoZDeltaImaginary = 2 * (referenceReal * deltaImaginary + referenceImaginary * deltaReal);
            double deltaSquaredReal = deltaReal * deltaReal - deltaImaginary * deltaImaginary;
            double deltaSquaredImaginary = 2 * deltaReal * deltaImaginary;
            deltaReal = twoZDeltaReal + deltaSquaredReal + addReal;
            deltaImaginary = twoZDeltaImaginary + deltaSquaredImaginary + addImaginary;

            referenceIndex++;
            iteration++;

            double nextReferenceReal = referenceIndex < orbit.Length ? orbit.Re[referenceIndex] : 0.0;
            double nextReferenceImaginary = referenceIndex < orbit.Length ? orbit.Im[referenceIndex] : 0.0;
            double fullReal = nextReferenceReal + deltaReal;
            double fullImaginary = nextReferenceImaginary + deltaImaginary;
            magnitudeSquared = fullReal * fullReal + fullImaginary * fullImaginary;

            if (magnitudeSquared > escapeSquared)
            {
                escaped = true;
                break;
            }

            double deltaMagnitudeSquared = deltaReal * deltaReal + deltaImaginary * deltaImaginary;
            double referenceMagnitudeSquared =
                nextReferenceReal * nextReferenceReal + nextReferenceImaginary * nextReferenceImaginary;
            // Rebasing по Zhuoran (|z| < |δ|) плюс критерий Pauldelbrot (|z|² ≪ |Zref|²):
            // и то и другое означает, что опорная точка перестала быть хорошим приближением.
            if (referenceIndex >= orbit.Length - 1 ||
                magnitudeSquared < deltaMagnitudeSquared ||
                magnitudeSquared < GlitchToleranceSquared * referenceMagnitudeSquared)
            {
                // δ отсчитывается от опорной точки, поэтому при сбросе индекса в 0 нужно
                // δ = z − Z₀. Для Мандельброта Z₀ = 0 (вычитание — no-op, бит-в-бит как
                // раньше); для Жюлиа Z₀ = центр, и без вычитания rebasing давал глитчи.
                deltaReal = fullReal - orbit.Re[0];
                deltaImaginary = fullImaginary - orbit.Im[0];
                referenceIndex = 0;
            }
        }

        if (!escaped)
            return new PixelMetrics(maxIterations, maxIterations, 0, 0);

        double smooth = iteration;
        if (magnitudeSquared > 1)
        {
            double logZn = System.Math.Log(magnitudeSquared) / 2;
            const double smoothingPower = 2;
            double nu = System.Math.Log(System.Math.Max(logZn, 1e-300) / System.Math.Log(smoothingPower)) /
                        System.Math.Log(smoothingPower);
            if (double.IsFinite(nu)) smooth = iteration + 1 - nu;
        }

        return new PixelMetrics(
            iteration,
            smooth,
            minTrap == double.MaxValue ? 0 : minTrap,
            iteration == 0 ? 0 : stripe / iteration);
    }

    // Тот же алгоритм, что <see cref="DeepZoomPixel"/>, но отклонение δ ведётся в
    // <see cref="FloatExp"/>: на зуме за ~1e72 δ и особенно δ² перестают помещаться в
    // обычный double. Опорная орбита и все проверки (escape, rebasing) остаются в double —
    // там значения ограничены и расширенный диапазон не нужен. Ниже порога FloatExp этот
    // путь не вызывается, поэтому расхождение округления с double-путём картинку не задевает.
    private static PixelMetrics DeepZoomPixelFloatExp(
        MandelbrotState state,
        ReferenceOrbit orbit,
        bool isJulia,
        double deltaConstantReal,
        double deltaConstantImaginary,
        double escapeSquared,
        CancellationToken token)
    {
        int maxIterations = state.Iterations;
        bool trackTrap = state.ColoringMode == MandelbrotColoringMode.OrbitTrap;
        bool trackStripe = state.ColoringMode == MandelbrotColoringMode.StripeAverage;

        FloatExp deltaReal = FloatExp.FromDouble(isJulia ? deltaConstantReal : 0.0);
        FloatExp deltaImaginary = FloatExp.FromDouble(isJulia ? deltaConstantImaginary : 0.0);
        FloatExp addReal = FloatExp.FromDouble(isJulia ? 0.0 : deltaConstantReal);
        FloatExp addImaginary = FloatExp.FromDouble(isJulia ? 0.0 : deltaConstantImaginary);

        int referenceIndex = 0;
        int iteration = 0;
        double magnitudeSquared = 0;
        double minTrap = double.MaxValue;
        double stripe = 0;
        bool escaped = false;

        while (iteration < maxIterations)
        {
            if ((iteration & 8191) == 0 && token.IsCancellationRequested) return default;

            double referenceReal = orbit.Re[referenceIndex];
            double referenceImaginary = orbit.Im[referenceIndex];

            double currentReal = referenceReal + deltaReal.ToDouble();
            double currentImaginary = referenceImaginary + deltaImaginary.ToDouble();
            if (trackTrap)
                minTrap = System.Math.Min(minTrap,
                    System.Math.Min(System.Math.Abs(currentReal), System.Math.Abs(currentImaginary)));
            if (trackStripe)
                stripe += 0.5 + 0.5 * System.Math.Sin(
                    state.StripeFrequency * System.Math.Atan2(currentImaginary, currentReal));

            // δ ← 2·Z·δ + δ² + δc
            FloatExp twoZDeltaReal = (deltaReal * referenceReal - deltaImaginary * referenceImaginary) * 2.0;
            FloatExp twoZDeltaImaginary = (deltaReal * referenceImaginary + deltaImaginary * referenceReal) * 2.0;
            FloatExp deltaSquaredReal = deltaReal * deltaReal - deltaImaginary * deltaImaginary;
            FloatExp deltaSquaredImaginary = deltaReal * deltaImaginary * 2.0;
            deltaReal = twoZDeltaReal + deltaSquaredReal + addReal;
            deltaImaginary = twoZDeltaImaginary + deltaSquaredImaginary + addImaginary;

            referenceIndex++;
            iteration++;

            double nextReferenceReal = referenceIndex < orbit.Length ? orbit.Re[referenceIndex] : 0.0;
            double nextReferenceImaginary = referenceIndex < orbit.Length ? orbit.Im[referenceIndex] : 0.0;
            double fullReal = nextReferenceReal + deltaReal.ToDouble();
            double fullImaginary = nextReferenceImaginary + deltaImaginary.ToDouble();
            magnitudeSquared = fullReal * fullReal + fullImaginary * fullImaginary;

            if (magnitudeSquared > escapeSquared)
            {
                escaped = true;
                break;
            }

            // |δ|²: при действительно малом δ обращается в 0 и не даёт ложного rebasing;
            // ближе к |δ| ~ |z| снова становится числом в диапазоне double — как и нужно.
            double deltaMagnitudeSquared = FloatExp.MagnitudeSquared(deltaReal, deltaImaginary).ToDouble();
            double referenceMagnitudeSquared =
                nextReferenceReal * nextReferenceReal + nextReferenceImaginary * nextReferenceImaginary;
            if (referenceIndex >= orbit.Length - 1 ||
                magnitudeSquared < deltaMagnitudeSquared ||
                magnitudeSquared < GlitchToleranceSquared * referenceMagnitudeSquared)
            {
                // δ = z − Z₀ (для Мандельброта Z₀ = 0; для Жюлиа — центр). См. double-ядро.
                deltaReal = FloatExp.FromDouble(fullReal - orbit.Re[0]);
                deltaImaginary = FloatExp.FromDouble(fullImaginary - orbit.Im[0]);
                referenceIndex = 0;
            }
        }

        if (!escaped)
            return new PixelMetrics(maxIterations, maxIterations, 0, 0);

        double smooth = iteration;
        if (magnitudeSquared > 1)
        {
            double logZn = System.Math.Log(magnitudeSquared) / 2;
            const double smoothingPower = 2;
            double nu = System.Math.Log(System.Math.Max(logZn, 1e-300) / System.Math.Log(smoothingPower)) /
                        System.Math.Log(smoothingPower);
            if (double.IsFinite(nu)) smooth = iteration + 1 - nu;
        }

        return new PixelMetrics(
            iteration,
            smooth,
            minTrap == double.MaxValue ? 0 : minTrap,
            iteration == 0 ? 0 : stripe / iteration);
    }

    // ------------------------------------------------------------------ brute-force safety net

    private static (double X, double Y) ProjectCenterToDouble(MandelbrotState state)
    {
        double x = state.CenterXExact is { Length: > 0 } exactX
            ? BigFloat.Parse(exactX).ToDouble()
            : (double)state.CenterX;
        double y = state.CenterYExact is { Length: > 0 } exactY
            ? BigFloat.Parse(exactY).ToDouble()
            : (double)state.CenterY;
        return (x, y);
    }

    // Запасной путь на случай вырожденной опорной орбиты (центр в глубоком внешнем регионе,
    // орбита выходит за радиус почти сразу). Такой вид почти однороден: всё убегает за
    // считаные итерации, накапливать ошибку нечему, и обычного double достаточно. Ступень
    // decimal здесь больше не используется.
    private static byte[]? RenderBruteForceTile(
        MandelbrotState state,
        int canvasWidth,
        int canvasHeight,
        MandelbrotRenderTile tile,
        CancellationToken token)
    {
        int stride = checked(tile.Width * 4);
        var buffer = new byte[checked(stride * tile.Height)];
        var (centerX, centerY) = ProjectCenterToDouble(state);
        double viewWidth = 3.0 / state.Zoom;
        double viewHeight = viewWidth * canvasHeight / canvasWidth;

        for (int localY = 0; localY < tile.Height; localY++)
        {
            if (token.IsCancellationRequested) return null;
            int y = tile.Y + localY;
            double imaginary = centerY + (0.5 - (double)y / canvasHeight) * viewHeight;
            int row = localY * stride;
            for (int localX = 0; localX < tile.Width; localX++)
            {
                int x = tile.X + localX;
                double real = centerX + ((double)x / canvasWidth - 0.5) * viewWidth;
                PixelMetrics metrics = Iterate(state, real, imaginary, token);
                Color color = ResolveColor(state, metrics, 0);
                int offset = row + localX * 4;
                buffer[offset] = color.B;
                buffer[offset + 1] = color.G;
                buffer[offset + 2] = color.R;
                buffer[offset + 3] = 255;
            }
        }

        return token.IsCancellationRequested ? null : buffer;
    }

    private static void RenderBruteForceFull(
        MandelbrotState state,
        byte[] buffer,
        int width,
        int height,
        int stride,
        CancellationToken token,
        Action<int>? reportProgress)
    {
        int threads = state.Threads <= 0 ? Environment.ProcessorCount : state.Threads;
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = System.Math.Clamp(threads, 1, Environment.ProcessorCount)
        };
        var (centerX, centerY) = ProjectCenterToDouble(state);
        double viewWidth = 3.0 / state.Zoom;
        double viewHeight = viewWidth * height / width;
        int completedRows = 0;

        Parallel.For(0, height, options, (y, loopState) =>
        {
            if (token.IsCancellationRequested) { loopState.Stop(); return; }
            int row = y * stride;
            double imaginary = centerY + (0.5 - (double)y / height) * viewHeight;
            for (int x = 0; x < width; x++)
            {
                if ((x & 63) == 0 && token.IsCancellationRequested) { loopState.Stop(); return; }
                double real = centerX + ((double)x / width - 0.5) * viewWidth;
                PixelMetrics metrics = Iterate(state, real, imaginary, token);
                Color color = ResolveColor(state, metrics, 0);
                int offset = row + x * 4;
                buffer[offset] = color.B;
                buffer[offset + 1] = color.G;
                buffer[offset + 2] = color.R;
                buffer[offset + 3] = 255;
            }

            int done = Interlocked.Increment(ref completedRows);
            if (done == height || done % System.Math.Max(1, height / 100) == 0)
                reportProgress?.Invoke(done * 100 / height);
        });
    }
}
