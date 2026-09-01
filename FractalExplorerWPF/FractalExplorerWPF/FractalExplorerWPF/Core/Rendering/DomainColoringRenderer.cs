using System.Numerics;
using System.Windows.Media;
using FractalExplorerWPF.Core.NewtonMath;
using FractalExplorerWPF.Models;
using Color = System.Windows.Media.Color;

namespace FractalExplorerWPF.Core.Rendering;

public sealed class DomainColoringRenderer
{
    public const double BaseScale = 4;
    private const double TwoPi = Math.PI * 2;
    private const double InverseLogTwo = 1.4426950408889634;
    private readonly DomainColoringState _state;
    private readonly CompiledComplexExpression _formula;

    public DomainColoringRenderer(DomainColoringState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (string.IsNullOrWhiteSpace(state.Formula))
            throw new InvalidOperationException("Введите комплексную функцию от z.");

        ExpressionNode expression = new Parser(new Tokenizer(state.Formula.Trim()).Tokenize()).Parse();
        _formula = CompiledComplexExpression.Compile(expression);
        _state = state;
        ParsedFormula = expression.PrintSimple();
    }

    public string ParsedFormula { get; }

    public byte[]? RenderTile(
        MandelbrotRenderTile tile,
        int canvasWidth,
        int canvasHeight,
        CancellationToken token)
    {
        if (tile.Width <= 0 || tile.Height <= 0 || canvasWidth <= 0 || canvasHeight <= 0)
            return null;

        byte[] pixels = new byte[checked(tile.Width * tile.Height * 4)];
        PixelTransform transform = CreateTransform(canvasWidth, canvasHeight);
        int offset = 0;
        for (int localY = 0; localY < tile.Height; localY++)
        {
            if ((localY & 7) == 0) token.ThrowIfCancellationRequested();
            int y = tile.Y + localY;
            for (int localX = 0; localX < tile.Width; localX++)
            {
                int x = tile.X + localX;
                WritePixel(pixels, offset, x, y, transform);
                offset += 4;
            }
        }
        return pixels;
    }

    public void Render(
        byte[] buffer,
        int width,
        int height,
        int stride,
        int threadCount,
        CancellationToken token,
        Action<int>? reportProgress = null)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        if (width <= 0 || height <= 0 || stride < width * 4 || buffer.Length < stride * height)
            throw new ArgumentOutOfRangeException(nameof(width));

        int completedRows = 0;
        var options = new ParallelOptions
        {
            CancellationToken = token,
            MaxDegreeOfParallelism = Math.Clamp(threadCount, 1, Environment.ProcessorCount)
        };
        PixelTransform transform = CreateTransform(width, height);
        Parallel.For(0, height, options, y =>
        {
            int offset = y * stride;
            for (int x = 0; x < width; x++)
            {
                WritePixel(buffer, offset, x, y, transform);
                offset += 4;
            }
            int done = Interlocked.Increment(ref completedRows);
            if (done == height || done % Math.Max(1, height / 100) == 0)
                reportProgress?.Invoke(done * 100 / height);
        });
    }

    // Масштаб и половины размеров холста не зависят от пикселя. Выносим их из
    // внутреннего цикла; сами пиксельные выражения ниже сохранены дословно
    // (тот же порядок операций), поэтому результат бит-в-бит прежний.
    private readonly record struct PixelTransform(
        double Scale, double HalfWidth, double HalfHeight, double UnitsPerPixel, int Width);

    private PixelTransform CreateTransform(int width, int height)
    {
        double zoom = Math.Clamp(_state.Zoom, 1e-12, 1e12);
        double scale = BaseScale / zoom;
        return new PixelTransform(scale, width / 2.0, height / 2.0, scale / width, width);
    }

    private void WritePixel(byte[] pixels, int offset, int x, int y, in PixelTransform transform)
    {
        double scale = transform.Scale;
        int width = transform.Width;
        double worldX = _state.CenterX + (x + 0.5 - transform.HalfWidth) * scale / width;
        double worldY = _state.CenterY + (transform.HalfHeight - y - 0.5) * scale / width;
        double unitsPerPixel = transform.UnitsPerPixel;

        Complex value = _formula.Evaluate(new Complex(worldX, worldY));
        Color color = Colorize(value);
        if (_state.ShowAxes &&
            (Math.Abs(worldX) <= unitsPerPixel * 0.65 || Math.Abs(worldY) <= unitsPerPixel * 0.65))
        {
            color = Blend(color, Colors.White, 0.55);
        }

        pixels[offset] = color.B;
        pixels[offset + 1] = color.G;
        pixels[offset + 2] = color.R;
        pixels[offset + 3] = byte.MaxValue;
    }

    private Color Colorize(Complex value)
    {
        if (!double.IsFinite(value.Real) || !double.IsFinite(value.Imaginary))
            return _state.InvalidColor;

        double magnitude = value.Magnitude;
        if (!double.IsFinite(magnitude)) return _state.InvalidColor;
        if (magnitude <= 1e-300) return Colors.Black;

        double normalizedArgument = Wrap01((Math.Atan2(value.Imaginary, value.Real) + Math.PI) / TwoPi);
        double hue = Wrap01(normalizedArgument * _state.HueCycles);
        double logarithmicMagnitude = Math.Log(magnitude) * InverseLogTwo;
        double valueLevel = 1;

        switch (_state.ColoringMode)
        {
            case DomainColoringMode.SmoothMagnitude:
                double response = 0.5 + 0.5 * Math.Tanh(logarithmicMagnitude * _state.MagnitudeExposure * 0.55);
                valueLevel = 0.22 + 0.78 * response;
                break;

            case DomainColoringMode.LogarithmicRings:
                valueLevel = RingBrightness(logarithmicMagnitude);
                break;

            case DomainColoringMode.PhaseContours:
                valueLevel = PhaseBrightness(normalizedArgument);
                break;

            case DomainColoringMode.PolarGrid:
                valueLevel = Math.Min(RingBrightness(logarithmicMagnitude),
                    PhaseBrightness(normalizedArgument));
                break;

            case DomainColoringMode.ArgumentOnly:
                valueLevel = 1;
                break;
        }

        return HsvToColor(hue, _state.Saturation, Math.Clamp(valueLevel, 0, 1));
    }

    private double RingBrightness(double logarithmicMagnitude)
    {
        double wave = 0.5 + 0.5 * Math.Cos(TwoPi * logarithmicMagnitude * _state.RingDensity);
        double contour = Math.Pow(wave, 7);
        return 1 - _state.ContourStrength * contour;
    }

    private double PhaseBrightness(double normalizedArgument)
    {
        double distance = Math.Abs(Math.Sin(Math.PI * normalizedArgument * _state.PhaseSectors));
        double contour = Math.Exp(-distance * distance * 80);
        return 1 - _state.ContourStrength * contour;
    }

    private static double Wrap01(double value)
    {
        value -= Math.Floor(value);
        return value < 0 ? value + 1 : value;
    }

    private static Color HsvToColor(double hue, double saturation, double value)
    {
        double scaledHue = Wrap01(hue) * 6;
        int sector = (int)Math.Floor(scaledHue);
        double fraction = scaledHue - sector;
        double p = value * (1 - saturation);
        double q = value * (1 - saturation * fraction);
        double t = value * (1 - saturation * (1 - fraction));
        (double r, double g, double b) = sector switch
        {
            0 => (value, t, p),
            1 => (q, value, p),
            2 => (p, value, t),
            3 => (p, q, value),
            4 => (t, p, value),
            _ => (value, p, q)
        };
        return Color.FromRgb(ToByte(r), ToByte(g), ToByte(b));
    }

    private static Color Blend(Color first, Color second, double amount)
    {
        amount = Math.Clamp(amount, 0, 1);
        return Color.FromRgb(
            ToByte((first.R * (1 - amount) + second.R * amount) / 255),
            ToByte((first.G * (1 - amount) + second.G * amount) / 255),
            ToByte((first.B * (1 - amount) + second.B * amount) / 255));
    }

    private static byte ToByte(double value) =>
        (byte)Math.Clamp((int)Math.Round(value * 255), 0, 255);
}
