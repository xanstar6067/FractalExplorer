using System.Globalization;
using FractalExplorerWPF.Core.NewtonMath;
using FractalExplorerWPF.Models;
using Color = System.Windows.Media.Color;

namespace FractalExplorerWPF.Core.Rendering;

/// <summary>
/// Третья ступень точности рендера семейства Мандельброта — «второй двигатель».
///
/// Первые две ступени (double и decimal, см. <see cref="IterateAt"/>) остаются без
/// изменений и обслуживают зум до ~1e25. Начиная с <see cref="PerturbationZoomThreshold"/>
/// плоская арифметика в <see cref="decimal"/> упирается в свои ~28 значащих цифр, поэтому
/// включается пертурбационный метод:
///
/// 1. Один раз на кадр считается опорная орбита <c>Zₙ</c> в центре области — в
///    <see cref="BigFloat"/> (произвольная точность), результат кэшируется в double-массивах.
/// 2. Каждый пиксель итерирует лишь отклонение <c>δₙ = zₙ − Zₙ</c> в обычном double:
///    <c>δₙ₊₁ = 2·Zₙ·δₙ + δₙ² + δc</c> (для Жюлиа слагаемое δc отсутствует, а δc задаёт δ₀).
/// 3. Потеря значимости («глитчи») лечится rebasing по Zhuoran: когда |zₙ| &lt; |δₙ| или
///    опорная орбита закончилась, δ сбрасывается в текущее z, а индекс опорной точки — в 0.
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

    private static bool ShouldUseDeepZoom(MandelbrotState state) =>
        state.Zoom > PerturbationZoomThreshold &&
        state.Variant is MandelbrotVariant.Mandelbrot or MandelbrotVariant.Julia &&
        state.ColoringMode is not (MandelbrotColoringMode.Histogram or MandelbrotColoringMode.DistanceEstimation);

    // ------------------------------------------------------------------ entry points

    private static byte[]? RenderDeepZoomTile(
        MandelbrotState state,
        int canvasWidth,
        int canvasHeight,
        MandelbrotRenderTile tile,
        CancellationToken token)
    {
        ReferenceOrbit orbit = GetReferenceOrbit(state);
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
                PixelMetrics metrics = DeepZoomPixel(
                    state, orbit, isJulia, deltaReal, deltaImaginary, escapeSquared, token);
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
        ReferenceOrbit orbit = GetReferenceOrbit(state);
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
                PixelMetrics metrics = DeepZoomPixel(
                    state, orbit, isJulia, deltaReal, deltaImaginary, escapeSquared, token);
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

    private static ReferenceOrbit GetReferenceOrbit(MandelbrotState state)
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
            state.Threshold.ToString(CultureInfo.InvariantCulture));

        lock (_orbitLock)
        {
            if (_orbitKey == key && _orbitCache is not null) return _orbitCache;

            BigFloat centerX = BigFloat.Parse(centerXRaw);
            BigFloat centerY = BigFloat.Parse(centerYRaw);
            ReferenceOrbit orbit = ComputeReferenceOrbit(state, centerX, centerY);
            _orbitKey = key;
            _orbitCache = orbit;
            return orbit;
        }
    }

    private static ReferenceOrbit ComputeReferenceOrbit(MandelbrotState state, BigFloat centerX, BigFloat centerY)
    {
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
                deltaReal = fullReal;
                deltaImaginary = fullImaginary;
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

    // Костыль на случай вырожденной опорной орбиты (центр вне множества и т.п.): считаем
    // такой тайл проверенной второй ступенью в decimal — точность та же, что была раньше.
    private static byte[]? RenderBruteForceTile(
        MandelbrotState state,
        int canvasWidth,
        int canvasHeight,
        MandelbrotRenderTile tile,
        CancellationToken token)
    {
        int stride = checked(tile.Width * 4);
        var buffer = new byte[checked(stride * tile.Height)];
        decimal viewWidth = DecimalViewWidth(state.Zoom);
        decimal viewHeight = viewWidth * canvasHeight / canvasWidth;

        for (int localY = 0; localY < tile.Height; localY++)
        {
            if (token.IsCancellationRequested) return null;
            int y = tile.Y + localY;
            decimal imaginary = state.CenterY + (0.5m - (decimal)y / canvasHeight) * viewHeight;
            int row = localY * stride;
            for (int localX = 0; localX < tile.Width; localX++)
            {
                int x = tile.X + localX;
                decimal real = state.CenterX + ((decimal)x / canvasWidth - 0.5m) * viewWidth;
                PixelMetrics metrics = IterateAt(state, real, imaginary, token);
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
        decimal viewWidth = DecimalViewWidth(state.Zoom);
        decimal viewHeight = viewWidth * height / width;
        int completedRows = 0;

        Parallel.For(0, height, options, (y, loopState) =>
        {
            if (token.IsCancellationRequested) { loopState.Stop(); return; }
            int row = y * stride;
            decimal imaginary = state.CenterY + (0.5m - (decimal)y / height) * viewHeight;
            for (int x = 0; x < width; x++)
            {
                if ((x & 63) == 0 && token.IsCancellationRequested) { loopState.Stop(); return; }
                decimal real = state.CenterX + ((decimal)x / width - 0.5m) * viewWidth;
                PixelMetrics metrics = IterateAt(state, real, imaginary, token);
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
