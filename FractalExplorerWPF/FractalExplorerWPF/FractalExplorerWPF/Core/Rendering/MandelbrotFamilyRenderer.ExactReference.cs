using System.Globalization;
using FractalExplorerWPF.Core.NewtonMath;
using FractalExplorerWPF.Models;
using Color = System.Windows.Media.Color;

namespace FractalExplorerWPF.Core.Rendering;

/// <summary>
/// Эталонный рендер прямой итерацией в <see cref="BigFloat"/> без пертурбации, BLA и
/// FloatExp — «золотой стандарт» для оценки потерь качества основного глубокого движка.
/// Медленный (BigFloat на каждый пиксель), поэтому только для проверочного проекта.
/// Поддерживает Smooth-окраску вариантов Mandelbrot/Julia, отражённых и Multibrot целой
/// степени — тех же, что и <see cref="ShouldUseDeepZoom"/>.
/// </summary>
public static partial class MandelbrotFamilyRenderer
{
    internal static byte[] RenderExactReferenceForTests(
        MandelbrotState state, int width, int height, CancellationToken token)
    {
        var buffer = new byte[checked(width * height * 4)];

        // Заметно выше рабочей точности движка — чтобы эталон был практически точным.
        int bits = PlanDeepZoom(state).ReferenceBits + 128;

        string centerXRaw = state.CenterXExact is { Length: > 0 } exactX
            ? exactX
            : state.CenterX.ToString(CultureInfo.InvariantCulture);
        string centerYRaw = state.CenterYExact is { Length: > 0 } exactY
            ? exactY
            : state.CenterY.ToString(CultureInfo.InvariantCulture);

        double escapeSquared = (double)(state.Threshold * state.Threshold);
        double viewWidth = 3.0 / state.Zoom;
        double viewHeight = viewWidth * height / width;

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = System.Math.Clamp(
                state.Threads <= 0 ? Environment.ProcessorCount : state.Threads, 1, Environment.ProcessorCount),
        };

        Parallel.For(0, height, options, (y, loopState) =>
        {
            if (token.IsCancellationRequested) { loopState.Stop(); return; }
            using var precision = new BigFloat.PrecisionScope(bits);

            BigFloat centerX = BigFloat.Parse(centerXRaw);
            BigFloat centerY = BigFloat.Parse(centerYRaw);
            BigFloat imaginary = centerY + BigFloat.FromDouble((0.5 - (double)y / height) * viewHeight);
            int row = y * width * 4;

            for (int x = 0; x < width; x++)
            {
                if ((x & 63) == 0 && token.IsCancellationRequested) { loopState.Stop(); return; }
                BigFloat real = centerX + BigFloat.FromDouble(((double)x / width - 0.5) * viewWidth);
                PixelMetrics metrics = ExactIterate(state, real, imaginary, escapeSquared, token);
                Color color = ResolveColor(state, metrics, 0);
                int offset = row + x * 4;
                buffer[offset] = color.B;
                buffer[offset + 1] = color.G;
                buffer[offset + 2] = color.R;
                buffer[offset + 3] = 255;
            }
        });

        return buffer;
    }

    private static PixelMetrics ExactIterate(
        MandelbrotState state, BigFloat startReal, BigFloat startImaginary, double escapeSquared, CancellationToken token)
    {
        bool isJulia = IsJuliaVariant(state.Variant);
        ReflectKind? reflect = ReflectKindOf(state.Variant);
        int multibrotPower = MultibrotPowerOrZero(state);
        int simonobrotPower = SimonobrotPowerOrZero(state);
        bool invertReal = state.Variant == MandelbrotVariant.Simonobrot && state.UseInversion;

        BigFloat constantReal = isJulia
            ? BigFloat.FromDecimal(state.JuliaCReal)
            : invertReal ? -startReal : startReal;
        BigFloat constantImaginary = isJulia ? BigFloat.FromDecimal(state.JuliaCImaginary) : startImaginary;
        BigFloat zReal = isJulia ? startReal : BigFloat.Zero;
        BigFloat zImaginary = isJulia ? startImaginary : BigFloat.Zero;
        BigFloat two = BigFloat.FromInt(2);

        int maxIterations = state.Iterations;
        int iteration = 0;
        double magnitudeSquared = 0;
        bool escaped = false;

        while (iteration < maxIterations)
        {
            if ((iteration & 4095) == 0 && token.IsCancellationRequested) return default;

            if (reflect is { } kind)
            {
                (zReal, zImaginary) = StepReflectedReference(
                    kind, zReal, zImaginary, constantReal, constantImaginary, two);
            }
            else if (simonobrotPower >= 2)
            {
                int halfPower = simonobrotPower / 2;
                BigFloat powerReal = zReal, powerImaginary = zImaginary;
                for (int e = 1; e < simonobrotPower; e++)
                {
                    BigFloat nr = powerReal * zReal - powerImaginary * zImaginary;
                    powerImaginary = powerReal * zImaginary + powerImaginary * zReal;
                    powerReal = nr;
                }
                BigFloat magnitudeSquaredBig = zReal * zReal + zImaginary * zImaginary;
                BigFloat magnitudePower = magnitudeSquaredBig;
                for (int e = 1; e < halfPower; e++) magnitudePower *= magnitudeSquaredBig;
                zReal = powerReal * magnitudePower + constantReal;
                zImaginary = powerImaginary * magnitudePower + constantImaginary;
            }
            else if (multibrotPower >= 3)
            {
                BigFloat powerReal = zReal, powerImaginary = zImaginary;
                for (int e = 1; e < multibrotPower; e++)
                {
                    BigFloat nr = powerReal * zReal - powerImaginary * zImaginary;
                    powerImaginary = powerReal * zImaginary + powerImaginary * zReal;
                    powerReal = nr;
                }
                zReal = powerReal + constantReal;
                zImaginary = powerImaginary + constantImaginary;
            }
            else
            {
                BigFloat nextReal = zReal * zReal - zImaginary * zImaginary + constantReal;
                BigFloat nextImaginary = two * zReal * zImaginary + constantImaginary;
                zReal = nextReal;
                zImaginary = nextImaginary;
            }

            iteration++;

            double realDouble = zReal.ToDouble();
            double imaginaryDouble = zImaginary.ToDouble();
            magnitudeSquared = realDouble * realDouble + imaginaryDouble * imaginaryDouble;
            if (!double.IsFinite(magnitudeSquared) || magnitudeSquared > escapeSquared)
            {
                escaped = true;
                break;
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

        return new PixelMetrics(iteration, smooth, 0, 0);
    }
}
