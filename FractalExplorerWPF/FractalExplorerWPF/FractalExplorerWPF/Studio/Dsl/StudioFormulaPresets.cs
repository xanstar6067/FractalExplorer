namespace FractalExplorerWPF.Studio.Dsl;

public static class StudioFormulaPresets
{
    public const string Mandelbrot = """
        // Fractal Studio DSL — quadratic Mandelbrot
        parameter int maxIterations = 500 [min=10, max=5000, step=10, label="Итерации"];
        parameter real escapeRadius = 4 [min=2, max=100, step=0.5, label="Квадрат радиуса выхода"];

        init {
            complex z = complex(0, 0);
            complex c = pixel;
        }

        iterate {
            z = z * z + c;
        }

        escape {
            norm2(z) > escapeRadius;
        }
        """;
}
