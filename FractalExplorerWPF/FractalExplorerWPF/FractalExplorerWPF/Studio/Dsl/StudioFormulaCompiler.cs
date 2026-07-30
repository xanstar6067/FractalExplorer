using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using FractalExplorer.Utilities;

namespace FractalExplorerWPF.Studio.Dsl;

public sealed class StudioCompiledFormula
{
    private readonly IReadOnlyDictionary<string, ParameterBinding> _bindings;

    internal StudioCompiledFormula(
        StudioFormulaDocument document,
        IReadOnlyDictionary<string, ParameterBinding> bindings,
        StudioDoubleKernel doubleKernel,
        StudioDecimalKernel decimalKernel)
    {
        Document = document;
        _bindings = bindings;
        DoubleKernel = doubleKernel;
        DecimalKernel = decimalKernel;
    }

    public StudioFormulaDocument Document { get; }
    public StudioDoubleKernel DoubleKernel { get; }
    public StudioDecimalKernel DecimalKernel { get; }

    public IReadOnlyDictionary<string, string> CreateDefaultParameterValues()
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (StudioFormulaParameter parameter in Document.Parameters)
        {
            values[parameter.Name] = parameter.Kind == StudioValueKind.Complex
                ? ComplexDefaultText(parameter)
                : DefaultText(parameter);
        }
        return values;
    }

    public int GetIntegerParameter(StudioDoubleParameterSet values, string name)
    {
        if (!_bindings.TryGetValue(name, out ParameterBinding binding) ||
            binding.Kind != StudioValueKind.Integer)
            throw new KeyNotFoundException($"Целочисленный параметр «{name}» не найден.");
        return values.Integers[binding.Index];
    }

    public int GetIntegerParameter(StudioDecimalParameterSet values, string name)
    {
        if (!_bindings.TryGetValue(name, out ParameterBinding binding) ||
            binding.Kind != StudioValueKind.Integer)
            throw new KeyNotFoundException($"Целочисленный параметр «{name}» не найден.");
        return values.Integers[binding.Index];
    }

    public StudioDoubleParameterSet CreateDoubleParameters(IReadOnlyDictionary<string, string> overrides)
    {
        int realCount = _bindings.Values.Count(value => value.Kind == StudioValueKind.Real);
        int integerCount = _bindings.Values.Count(value => value.Kind == StudioValueKind.Integer);
        int complexCount = _bindings.Values.Count(value => value.Kind == StudioValueKind.Complex);
        int booleanCount = _bindings.Values.Count(value => value.Kind == StudioValueKind.Boolean);
        var result = new StudioDoubleParameterSet
        {
            Reals = new double[realCount],
            Integers = new int[integerCount],
            Complexes = new StudioComplexDouble[complexCount],
            Booleans = new bool[booleanCount]
        };
        FillDoubleParameters(result, overrides);
        return result;
    }

    public StudioDecimalParameterSet CreateDecimalParameters(IReadOnlyDictionary<string, string> overrides)
    {
        int realCount = _bindings.Values.Count(value => value.Kind == StudioValueKind.Real);
        int integerCount = _bindings.Values.Count(value => value.Kind == StudioValueKind.Integer);
        int complexCount = _bindings.Values.Count(value => value.Kind == StudioValueKind.Complex);
        int booleanCount = _bindings.Values.Count(value => value.Kind == StudioValueKind.Boolean);
        var result = new StudioDecimalParameterSet
        {
            Reals = new decimal[realCount],
            Integers = new int[integerCount],
            Complexes = new StudioComplexDecimal[complexCount],
            Booleans = new bool[booleanCount]
        };
        FillDecimalParameters(result, overrides);
        return result;
    }

    private void FillDoubleParameters(
        StudioDoubleParameterSet target,
        IReadOnlyDictionary<string, string> overrides)
    {
        foreach (StudioFormulaParameter parameter in Document.Parameters)
        {
            ParameterBinding binding = _bindings[parameter.Name];
            string text = overrides.TryGetValue(parameter.Name, out string? value)
                ? value
                : DefaultText(parameter);
            switch (binding.Kind)
            {
                case StudioValueKind.Integer:
                    target.Integers[binding.Index] = StudioFormulaValueParser.Integer(text, parameter.DisplayName);
                    break;
                case StudioValueKind.Real:
                    target.Reals[binding.Index] = StudioFormulaValueParser.Double(text, parameter.DisplayName);
                    break;
                case StudioValueKind.Boolean:
                    target.Booleans[binding.Index] = StudioFormulaValueParser.Boolean(text, parameter.DisplayName);
                    break;
                case StudioValueKind.Complex:
                    target.Complexes[binding.Index] = ParseDoubleComplex(parameter, overrides);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    private void FillDecimalParameters(
        StudioDecimalParameterSet target,
        IReadOnlyDictionary<string, string> overrides)
    {
        foreach (StudioFormulaParameter parameter in Document.Parameters)
        {
            ParameterBinding binding = _bindings[parameter.Name];
            string text = overrides.TryGetValue(parameter.Name, out string? value)
                ? value
                : DefaultText(parameter);
            switch (binding.Kind)
            {
                case StudioValueKind.Integer:
                    target.Integers[binding.Index] = StudioFormulaValueParser.Integer(text, parameter.DisplayName);
                    break;
                case StudioValueKind.Real:
                    target.Reals[binding.Index] = StudioFormulaValueParser.Decimal(text, parameter.DisplayName);
                    break;
                case StudioValueKind.Boolean:
                    target.Booleans[binding.Index] = StudioFormulaValueParser.Boolean(text, parameter.DisplayName);
                    break;
                case StudioValueKind.Complex:
                    target.Complexes[binding.Index] = ParseDecimalComplex(parameter, overrides);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    private static string DefaultText(StudioFormulaParameter parameter) => parameter.DefaultValue switch
    {
        StudioNumberExpression number => number.Text,
        StudioUnaryExpression { Operator: StudioTokenKind.Minus, Operand: StudioNumberExpression number } =>
            "-" + number.Text,
        StudioIdentifierExpression identifier when parameter.Kind == StudioValueKind.Boolean => identifier.Name,
        _ when parameter.Kind == StudioValueKind.Complex => "0,0",
        _ => throw new StudioFormulaException(
            $"Значение по умолчанию параметра «{parameter.Name}» должно быть литералом",
            parameter.Span)
    };

    private static StudioComplexDouble ParseDoubleComplex(
        StudioFormulaParameter parameter,
        IReadOnlyDictionary<string, string> overrides)
    {
        if (overrides.TryGetValue(parameter.Name, out string? text))
        {
            string[] parts = text.Split(',', StringSplitOptions.TrimEntries);
            if (parts.Length != 2)
                throw new FormatException($"Параметр «{parameter.DisplayName}» должен иметь формат Re, Im.");
            return new StudioComplexDouble(
                StudioFormulaValueParser.Double(parts[0], parameter.DisplayName),
                StudioFormulaValueParser.Double(parts[1], parameter.DisplayName));
        }

        (string real, string imaginary) = ComplexDefault(parameter);
        return new StudioComplexDouble(
            StudioFormulaValueParser.Double(real, parameter.DisplayName),
            StudioFormulaValueParser.Double(imaginary, parameter.DisplayName));
    }

    private static StudioComplexDecimal ParseDecimalComplex(
        StudioFormulaParameter parameter,
        IReadOnlyDictionary<string, string> overrides)
    {
        if (overrides.TryGetValue(parameter.Name, out string? text))
        {
            string[] parts = text.Split(',', StringSplitOptions.TrimEntries);
            if (parts.Length != 2)
                throw new FormatException($"Параметр «{parameter.DisplayName}» должен иметь формат Re, Im.");
            return new StudioComplexDecimal(
                StudioFormulaValueParser.Decimal(parts[0], parameter.DisplayName),
                StudioFormulaValueParser.Decimal(parts[1], parameter.DisplayName));
        }

        (string real, string imaginary) = ComplexDefault(parameter);
        return new StudioComplexDecimal(
            StudioFormulaValueParser.Decimal(real, parameter.DisplayName),
            StudioFormulaValueParser.Decimal(imaginary, parameter.DisplayName));
    }

    private static (string Real, string Imaginary) ComplexDefault(StudioFormulaParameter parameter)
    {
        if (parameter.DefaultValue is not StudioCallExpression
            {
                Name: var name,
                Arguments.Count: 2
            } call ||
            !string.Equals(name, "complex", StringComparison.OrdinalIgnoreCase))
        {
            throw new StudioFormulaException(
                $"Комплексный параметр «{parameter.Name}» должен иметь значение complex(Re, Im)",
                parameter.Span);
        }

        return (LiteralText(call.Arguments[0]), LiteralText(call.Arguments[1]));
    }

    private static string ComplexDefaultText(StudioFormulaParameter parameter)
    {
        (string real, string imaginary) = ComplexDefault(parameter);
        return $"{real}, {imaginary}";
    }

    private static string LiteralText(StudioExpression expression) => expression switch
    {
        StudioNumberExpression number => number.Text,
        StudioUnaryExpression { Operator: StudioTokenKind.Minus, Operand: StudioNumberExpression number } =>
            "-" + number.Text,
        _ => throw new StudioFormulaException("Ожидался числовой литерал", expression.Span)
    };
}

internal readonly record struct ParameterBinding(StudioValueKind Kind, int Index);

public static class StudioFormulaCompiler
{
    public static StudioCompiledFormula Compile(string source)
    {
        StudioFormulaDocument document = StudioFormulaParser.Parse(source);
        IReadOnlyDictionary<string, ParameterBinding> bindings = CreateBindings(document);
        var doubleBuilder = new KernelBuilder(document, bindings, decimalMode: false);
        var decimalBuilder = new KernelBuilder(document, bindings, decimalMode: true);
        return new StudioCompiledFormula(
            document,
            bindings,
            (StudioDoubleKernel)doubleBuilder.Build(),
            (StudioDecimalKernel)decimalBuilder.Build());
    }

    private static IReadOnlyDictionary<string, ParameterBinding> CreateBindings(StudioFormulaDocument document)
    {
        var bindings = new Dictionary<string, ParameterBinding>(StringComparer.OrdinalIgnoreCase);
        var indices = new Dictionary<StudioValueKind, int>();
        foreach (StudioFormulaParameter parameter in document.Parameters)
        {
            if (bindings.ContainsKey(parameter.Name))
                throw new StudioFormulaException(
                    $"Параметр «{parameter.Name}» объявлен повторно",
                    parameter.Span);
            int index = indices.GetValueOrDefault(parameter.Kind);
            bindings[parameter.Name] = new ParameterBinding(parameter.Kind, index);
            indices[parameter.Kind] = index + 1;
        }

        if (!bindings.TryGetValue("maxIterations", out ParameterBinding maxIterations) ||
            maxIterations.Kind != StudioValueKind.Integer)
        {
            throw new StudioFormulaException(
                "Формула должна объявлять целочисленный параметр maxIterations",
                document.Parameters.FirstOrDefault()?.Span ?? new StudioTextSpan(1, 1, 1));
        }

        return bindings;
    }

    private sealed class KernelBuilder
    {
        private readonly StudioFormulaDocument _document;
        private readonly IReadOnlyDictionary<string, ParameterBinding> _parameters;
        private readonly bool _decimalMode;
        private readonly Type _realType;
        private readonly Type _complexType;
        private readonly ParameterExpression _pixelReal;
        private readonly ParameterExpression _pixelImaginary;
        private readonly ParameterExpression _realParameters;
        private readonly ParameterExpression _integerParameters;
        private readonly ParameterExpression _complexParameters;
        private readonly ParameterExpression _booleanParameters;
        private readonly ParameterExpression _iteration = Expression.Variable(typeof(int), "iteration");
        private readonly ParameterExpression _escaped = Expression.Variable(typeof(bool), "escaped");
        private readonly Dictionary<string, VariableBinding> _variables =
            new(StringComparer.OrdinalIgnoreCase);

        public KernelBuilder(
            StudioFormulaDocument document,
            IReadOnlyDictionary<string, ParameterBinding> parameters,
            bool decimalMode)
        {
            _document = document;
            _parameters = parameters;
            _decimalMode = decimalMode;
            _realType = decimalMode ? typeof(decimal) : typeof(double);
            _complexType = decimalMode ? typeof(StudioComplexDecimal) : typeof(StudioComplexDouble);
            _pixelReal = Expression.Parameter(_realType, "pixelReal");
            _pixelImaginary = Expression.Parameter(_realType, "pixelImaginary");
            _realParameters = Expression.Parameter(_realType.MakeArrayType(), "realParameters");
            _integerParameters = Expression.Parameter(typeof(int[]), "integerParameters");
            _complexParameters = Expression.Parameter(_complexType.MakeArrayType(), "complexParameters");
            _booleanParameters = Expression.Parameter(typeof(bool[]), "booleanParameters");
        }

        public Delegate Build()
        {
            var initialization = new List<Expression>();
            foreach (StudioStatement statement in _document.Initialization)
                initialization.Add(BindInitialization(statement));

            if (!_variables.TryGetValue("z", out VariableBinding z) || z.Kind != StudioValueKind.Complex)
            {
                throw new StudioFormulaException(
                    "Секция init должна объявлять комплексную переменную z",
                    _document.Initialization.FirstOrDefault()?.Span ?? new StudioTextSpan(1, 1, 1));
            }

            var iterationBody = new List<Expression>();
            foreach (StudioStatement statement in _document.Iteration)
                iterationBody.Add(BindAssignment(statement));

            BoundExpression escape = BindExpression(_document.EscapeCondition);
            Require(escape, StudioValueKind.Boolean, "Условие escape должно возвращать bool");

            ParameterBinding maxBinding = _parameters["maxIterations"];
            Expression maxIterations = Expression.ArrayIndex(
                _integerParameters,
                Expression.Constant(maxBinding.Index));
            LabelTarget breakLabel = Expression.Label("endOrbit");
            Expression loop = Expression.Loop(
                Expression.Block(
                    Expression.IfThen(
                        Expression.GreaterThanOrEqual(_iteration, maxIterations),
                        Expression.Break(breakLabel)),
                    Expression.Block(iterationBody),
                    Expression.PostIncrementAssign(_iteration),
                    Expression.IfThen(
                        escape.Expression,
                        Expression.Block(
                            Expression.Assign(_escaped, Expression.Constant(true)),
                            Expression.Break(breakLabel)))),
                breakLabel);

            MethodInfo resultFactory = typeof(StudioOrbitSample).GetMethod(
                _decimalMode ? nameof(StudioOrbitSample.FromDecimal) : nameof(StudioOrbitSample.FromDouble),
                BindingFlags.Public | BindingFlags.Static)!;
            Expression result = Expression.Call(resultFactory, _iteration, _escaped, z.Variable);
            var body = Expression.Block(
                _variables.Values.Select(value => value.Variable).Append(_iteration).Append(_escaped),
                Expression.Assign(_iteration, Expression.Constant(0)),
                Expression.Assign(_escaped, Expression.Constant(false)),
                Expression.Block(initialization),
                loop,
                result);

            Type delegateType = _decimalMode ? typeof(StudioDecimalKernel) : typeof(StudioDoubleKernel);
            return Expression.Lambda(
                    delegateType,
                    body,
                    _pixelReal,
                    _pixelImaginary,
                    _realParameters,
                    _integerParameters,
                    _complexParameters,
                    _booleanParameters)
                .Compile();
        }

        private Expression BindInitialization(StudioStatement statement)
        {
            if (statement is not StudioVariableDeclaration declaration)
                throw new StudioFormulaException(
                    "Секция init должна содержать только объявления переменных",
                    statement.Span);
            if (_variables.ContainsKey(declaration.Name) ||
                _parameters.ContainsKey(declaration.Name) ||
                string.Equals(declaration.Name, "pixel", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(declaration.Name, "iteration", StringComparison.OrdinalIgnoreCase))
            {
                throw new StudioFormulaException(
                    $"Имя «{declaration.Name}» уже используется",
                    declaration.Span);
            }

            Type type = RuntimeType(declaration.Kind);
            var variable = Expression.Variable(type, declaration.Name);
            var binding = new VariableBinding(declaration.Kind, variable);
            _variables[declaration.Name] = binding;
            BoundExpression initializer = Convert(BindExpression(declaration.Initializer), declaration.Kind);
            return Expression.Assign(variable, initializer.Expression);
        }

        private Expression BindAssignment(StudioStatement statement)
        {
            if (statement is StudioVariableDeclaration)
                throw new StudioFormulaException(
                    "В первой версии локальные переменные объявляются в init",
                    statement.Span);
            if (statement is not StudioAssignment assignment)
                throw new StudioFormulaException("Ожидалось присваивание", statement.Span);
            if (!_variables.TryGetValue(assignment.Name, out VariableBinding variable))
                throw new StudioFormulaException(
                    $"Переменная «{assignment.Name}» не объявлена в init",
                    assignment.Span);
            BoundExpression value = Convert(BindExpression(assignment.Value), variable.Kind);
            return Expression.Assign(variable.Variable, value.Expression);
        }

        private BoundExpression BindExpression(StudioExpression expression) => expression switch
        {
            StudioNumberExpression number => new(
                _decimalMode
                    ? Expression.Constant(
                        decimal.Parse(number.Text, NumberStyles.Float, CultureInfo.InvariantCulture))
                    : Expression.Constant(
                        double.Parse(number.Text, NumberStyles.Float, CultureInfo.InvariantCulture)),
                StudioValueKind.Real),
            StudioIdentifierExpression identifier => BindIdentifier(identifier),
            StudioUnaryExpression unary => BindUnary(unary),
            StudioBinaryExpression binary => BindBinary(binary),
            StudioCallExpression call => BindCall(call),
            _ => throw new StudioFormulaException("Неизвестный вид выражения", expression.Span)
        };

        private BoundExpression BindIdentifier(StudioIdentifierExpression identifier)
        {
            if (string.Equals(identifier.Name, "pixel", StringComparison.OrdinalIgnoreCase))
            {
                ConstructorInfo constructor = _complexType.GetConstructor([_realType, _realType])!;
                return new BoundExpression(
                    Expression.New(constructor, _pixelReal, _pixelImaginary),
                    StudioValueKind.Complex);
            }

            if (string.Equals(identifier.Name, "iteration", StringComparison.OrdinalIgnoreCase))
                return new BoundExpression(_iteration, StudioValueKind.Integer);
            if (string.Equals(identifier.Name, "true", StringComparison.OrdinalIgnoreCase))
                return new BoundExpression(Expression.Constant(true), StudioValueKind.Boolean);
            if (string.Equals(identifier.Name, "false", StringComparison.OrdinalIgnoreCase))
                return new BoundExpression(Expression.Constant(false), StudioValueKind.Boolean);
            if (_variables.TryGetValue(identifier.Name, out VariableBinding variable))
                return new BoundExpression(variable.Variable, variable.Kind);
            if (_parameters.TryGetValue(identifier.Name, out ParameterBinding parameter))
            {
                ParameterExpression array = parameter.Kind switch
                {
                    StudioValueKind.Integer => _integerParameters,
                    StudioValueKind.Real => _realParameters,
                    StudioValueKind.Complex => _complexParameters,
                    StudioValueKind.Boolean => _booleanParameters,
                    _ => throw new ArgumentOutOfRangeException()
                };
                return new BoundExpression(
                    Expression.ArrayIndex(array, Expression.Constant(parameter.Index)),
                    parameter.Kind);
            }

            throw new StudioFormulaException(
                $"Неизвестное имя «{identifier.Name}»",
                identifier.Span);
        }

        private BoundExpression BindUnary(StudioUnaryExpression unary)
        {
            BoundExpression operand = BindExpression(unary.Operand);
            return unary.Operator switch
            {
                StudioTokenKind.Plus when IsNumeric(operand.Kind) => operand,
                StudioTokenKind.Minus when IsNumeric(operand.Kind) =>
                    new BoundExpression(Expression.Negate(operand.Expression), operand.Kind),
                StudioTokenKind.Bang when operand.Kind == StudioValueKind.Boolean =>
                    new BoundExpression(Expression.Not(operand.Expression), StudioValueKind.Boolean),
                _ => throw new StudioFormulaException(
                    "Оператор неприменим к этому типу",
                    unary.Span)
            };
        }

        private BoundExpression BindBinary(StudioBinaryExpression binary)
        {
            BoundExpression left = BindExpression(binary.Left);
            BoundExpression right = BindExpression(binary.Right);
            if (binary.Operator is StudioTokenKind.AmpersandAmpersand or StudioTokenKind.PipePipe)
            {
                Require(left, StudioValueKind.Boolean, "Логический оператор требует bool");
                Require(right, StudioValueKind.Boolean, "Логический оператор требует bool");
                return new BoundExpression(
                    binary.Operator == StudioTokenKind.AmpersandAmpersand
                        ? Expression.AndAlso(left.Expression, right.Expression)
                        : Expression.OrElse(left.Expression, right.Expression),
                    StudioValueKind.Boolean);
            }

            if (binary.Operator is StudioTokenKind.EqualsEquals or StudioTokenKind.BangEquals)
            {
                StudioValueKind common = CommonKind(left.Kind, right.Kind, binary.Span);
                left = Convert(left, common);
                right = Convert(right, common);
                Expression equality = Expression.Equal(left.Expression, right.Expression);
                return new BoundExpression(
                    binary.Operator == StudioTokenKind.EqualsEquals
                        ? equality
                        : Expression.Not(equality),
                    StudioValueKind.Boolean);
            }

            if (binary.Operator is StudioTokenKind.Greater or StudioTokenKind.GreaterOrEquals or
                StudioTokenKind.Less or StudioTokenKind.LessOrEquals)
            {
                if (!IsScalar(left.Kind) || !IsScalar(right.Kind))
                    throw new StudioFormulaException(
                        "Сравнивать можно только скалярные значения",
                        binary.Span);
                left = Convert(left, StudioValueKind.Real);
                right = Convert(right, StudioValueKind.Real);
                Expression comparison = binary.Operator switch
                {
                    StudioTokenKind.Greater => Expression.GreaterThan(left.Expression, right.Expression),
                    StudioTokenKind.GreaterOrEquals =>
                        Expression.GreaterThanOrEqual(left.Expression, right.Expression),
                    StudioTokenKind.Less => Expression.LessThan(left.Expression, right.Expression),
                    StudioTokenKind.LessOrEquals =>
                        Expression.LessThanOrEqual(left.Expression, right.Expression),
                    _ => throw new ArgumentOutOfRangeException()
                };
                return new BoundExpression(comparison, StudioValueKind.Boolean);
            }

            if (!IsNumeric(left.Kind) || !IsNumeric(right.Kind))
                throw new StudioFormulaException("Арифметический оператор требует числа", binary.Span);

            StudioValueKind resultKind = left.Kind == StudioValueKind.Complex ||
                                         right.Kind == StudioValueKind.Complex
                ? StudioValueKind.Complex
                : StudioValueKind.Real;
            left = Convert(left, resultKind);
            right = Convert(right, resultKind);
            if (binary.Operator == StudioTokenKind.Caret)
            {
                if (resultKind == StudioValueKind.Complex)
                {
                    BoundExpression baseValue = Convert(left, StudioValueKind.Complex);
                    BoundExpression exponent = Convert(BindExpression(binary.Right), StudioValueKind.Real);
                    MethodInfo pow = _complexType.GetMethod(
                        nameof(StudioComplexDouble.Pow),
                        BindingFlags.Public | BindingFlags.Static)!;
                    return new BoundExpression(
                        Expression.Call(pow, baseValue.Expression, exponent.Expression),
                        StudioValueKind.Complex);
                }

                MethodInfo realPow = _decimalMode
                    ? typeof(DecimalMath).GetMethod(nameof(DecimalMath.Pow), [typeof(decimal), typeof(decimal)])!
                    : typeof(Math).GetMethod(nameof(Math.Pow), [typeof(double), typeof(double)])!;
                return new BoundExpression(
                    Expression.Call(realPow, left.Expression, right.Expression),
                    StudioValueKind.Real);
            }

            Expression operation = binary.Operator switch
            {
                StudioTokenKind.Plus => Expression.Add(left.Expression, right.Expression),
                StudioTokenKind.Minus => Expression.Subtract(left.Expression, right.Expression),
                StudioTokenKind.Star => Expression.Multiply(left.Expression, right.Expression),
                StudioTokenKind.Slash => Expression.Divide(left.Expression, right.Expression),
                _ => throw new StudioFormulaException("Неизвестный оператор", binary.Span)
            };
            return new BoundExpression(operation, resultKind);
        }

        private BoundExpression BindCall(StudioCallExpression call)
        {
            string name = call.Name.ToLowerInvariant();
            if (name == "complex")
            {
                RequireArgumentCount(call, 2);
                BoundExpression real = Convert(BindExpression(call.Arguments[0]), StudioValueKind.Real);
                BoundExpression imaginary = Convert(BindExpression(call.Arguments[1]), StudioValueKind.Real);
                return new BoundExpression(
                    Expression.New(_complexType.GetConstructor([_realType, _realType])!,
                        real.Expression,
                        imaginary.Expression),
                    StudioValueKind.Complex);
            }

            if (name is "norm2" or "real" or "imag" or "conj")
            {
                RequireArgumentCount(call, 1);
                BoundExpression value = Convert(BindExpression(call.Arguments[0]), StudioValueKind.Complex);
                string member = name switch
                {
                    "norm2" => nameof(StudioComplexDouble.NormSquared),
                    "real" => nameof(StudioComplexDouble.Real),
                    "imag" => nameof(StudioComplexDouble.Imaginary),
                    "conj" => string.Empty,
                    _ => throw new ArgumentOutOfRangeException()
                };
                if (name == "conj")
                {
                    Expression real = Expression.Property(value.Expression, nameof(StudioComplexDouble.Real));
                    Expression imaginary = Expression.Negate(
                        Expression.Property(value.Expression, nameof(StudioComplexDouble.Imaginary)));
                    return new BoundExpression(
                        Expression.New(_complexType.GetConstructor([_realType, _realType])!, real, imaginary),
                        StudioValueKind.Complex);
                }
                return new BoundExpression(
                    Expression.Property(value.Expression, member),
                    StudioValueKind.Real);
            }

            if (name == "pow")
            {
                RequireArgumentCount(call, 2);
                BoundExpression value = BindExpression(call.Arguments[0]);
                BoundExpression exponent = Convert(BindExpression(call.Arguments[1]), StudioValueKind.Real);
                if (value.Kind == StudioValueKind.Complex)
                {
                    MethodInfo complexPow = _complexType.GetMethod(
                        nameof(StudioComplexDouble.Pow),
                        BindingFlags.Public | BindingFlags.Static)!;
                    return new BoundExpression(
                        Expression.Call(complexPow, value.Expression, exponent.Expression),
                        StudioValueKind.Complex);
                }

                value = Convert(value, StudioValueKind.Real);
                MethodInfo realPow = _decimalMode
                    ? typeof(DecimalMath).GetMethod(nameof(DecimalMath.Pow), [typeof(decimal), typeof(decimal)])!
                    : typeof(Math).GetMethod(nameof(Math.Pow), [typeof(double), typeof(double)])!;
                return new BoundExpression(
                    Expression.Call(realPow, value.Expression, exponent.Expression),
                    StudioValueKind.Real);
            }

            if (name is "abs" or "sin" or "cos" or "sqrt" or "log" or "exp")
            {
                RequireArgumentCount(call, 1);
                BoundExpression value = Convert(BindExpression(call.Arguments[0]), StudioValueKind.Real);
                Type mathType = name == "abs"
                    ? typeof(Math)
                    : _decimalMode
                        ? typeof(DecimalMath)
                        : typeof(Math);
                string methodName = name switch
                {
                    "abs" => nameof(Math.Abs),
                    "sin" => nameof(Math.Sin),
                    "cos" => nameof(Math.Cos),
                    "sqrt" => nameof(Math.Sqrt),
                    "log" => nameof(Math.Log),
                    "exp" => nameof(Math.Exp),
                    _ => throw new ArgumentOutOfRangeException()
                };
                MethodInfo method = mathType.GetMethod(methodName, [_realType])!;
                return new BoundExpression(
                    Expression.Call(method, value.Expression),
                    StudioValueKind.Real);
            }

            throw new StudioFormulaException($"Неизвестная функция «{call.Name}»", call.Span);
        }

        private BoundExpression Convert(BoundExpression value, StudioValueKind target)
        {
            if (value.Kind == target)
                return value;
            if (value.Kind == StudioValueKind.Integer && target == StudioValueKind.Real)
                return new BoundExpression(Expression.Convert(value.Expression, _realType), StudioValueKind.Real);
            if ((value.Kind == StudioValueKind.Integer || value.Kind == StudioValueKind.Real) &&
                target == StudioValueKind.Complex)
            {
                BoundExpression real = Convert(value, StudioValueKind.Real);
                return new BoundExpression(
                    Expression.New(
                        _complexType.GetConstructor([_realType, _realType])!,
                        real.Expression,
                        Expression.Constant(_decimalMode ? 0m : 0d, _realType)),
                    StudioValueKind.Complex);
            }
            throw new StudioFormulaException($"Нельзя преобразовать {value.Kind} в {target}", new(1, 1, 1));
        }

        private Type RuntimeType(StudioValueKind kind) => kind switch
        {
            StudioValueKind.Integer => typeof(int),
            StudioValueKind.Real => _realType,
            StudioValueKind.Complex => _complexType,
            StudioValueKind.Boolean => typeof(bool),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };

        private static StudioValueKind CommonKind(
            StudioValueKind left,
            StudioValueKind right,
            StudioTextSpan span)
        {
            if (left == right)
                return left;
            if (IsNumeric(left) && IsNumeric(right))
                return left == StudioValueKind.Complex || right == StudioValueKind.Complex
                    ? StudioValueKind.Complex
                    : StudioValueKind.Real;
            throw new StudioFormulaException("Операнды имеют несовместимые типы", span);
        }

        private static bool IsNumeric(StudioValueKind kind) =>
            kind is StudioValueKind.Integer or StudioValueKind.Real or StudioValueKind.Complex;

        private static bool IsScalar(StudioValueKind kind) =>
            kind is StudioValueKind.Integer or StudioValueKind.Real;

        private static void Require(BoundExpression value, StudioValueKind kind, string message)
        {
            if (value.Kind != kind)
                throw new StudioFormulaException(message, new(1, 1, 1));
        }

        private static void RequireArgumentCount(StudioCallExpression call, int expected)
        {
            if (call.Arguments.Count != expected)
                throw new StudioFormulaException(
                    $"Функция {call.Name} ожидает аргументов: {expected}",
                    call.Span);
        }
    }

    private readonly record struct VariableBinding(StudioValueKind Kind, ParameterExpression Variable);
    private readonly record struct BoundExpression(Expression Expression, StudioValueKind Kind);
}
