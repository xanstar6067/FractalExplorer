namespace FractalExplorerWPF.Studio.Dsl;

public sealed class StudioFormulaParser
{
    private readonly string _source;
    private readonly IReadOnlyList<StudioToken> _tokens;
    private int _position;

    private StudioFormulaParser(string source)
    {
        _source = source;
        _tokens = StudioFormulaLexer.Tokenize(source);
    }

    public static StudioFormulaDocument Parse(string source) => new StudioFormulaParser(source).ParseDocument();

    private StudioFormulaDocument ParseDocument()
    {
        var parameters = new List<StudioFormulaParameter>();
        IReadOnlyList<StudioStatement>? initialization = null;
        IReadOnlyList<StudioStatement>? iteration = null;
        StudioExpression? escape = null;

        while (Current.Kind != StudioTokenKind.End)
        {
            if (IsIdentifier("parameter"))
            {
                parameters.Add(ParseParameter());
                continue;
            }

            StudioToken section = Expect(StudioTokenKind.Identifier, "Ожидалось объявление параметра или секция");
            switch (section.Text.ToLowerInvariant())
            {
                case "init":
                    if (initialization is not null)
                        Error("Секция init объявлена повторно", section.Span);
                    initialization = ParseStatementBlock();
                    break;
                case "iterate":
                    if (iteration is not null)
                        Error("Секция iterate объявлена повторно", section.Span);
                    iteration = ParseStatementBlock();
                    break;
                case "escape":
                    if (escape is not null)
                        Error("Секция escape объявлена повторно", section.Span);
                    Expect(StudioTokenKind.LeftBrace, "После escape ожидалась «{»");
                    escape = ParseExpression();
                    Expect(StudioTokenKind.Semicolon, "После условия escape ожидалась «;»");
                    Expect(StudioTokenKind.RightBrace, "Ожидалась закрывающая «}» секции escape");
                    break;
                default:
                    Error($"Неизвестная секция «{section.Text}»", section.Span);
                    break;
            }
        }

        if (initialization is null)
            Error("Отсутствует обязательная секция init", Current.Span);
        if (iteration is null)
            Error("Отсутствует обязательная секция iterate", Current.Span);
        if (escape is null)
            Error("Отсутствует обязательная секция escape", Current.Span);

        return new StudioFormulaDocument(_source, parameters, initialization!, iteration!, escape!);
    }

    private StudioFormulaParameter ParseParameter()
    {
        StudioToken keyword = Advance();
        StudioToken type = Expect(StudioTokenKind.Identifier, "Ожидался тип параметра");
        StudioValueKind kind = ParseType(type);
        StudioToken name = Expect(StudioTokenKind.Identifier, "Ожидалось имя параметра");
        Expect(StudioTokenKind.Equals, "После имени параметра ожидался знак «=»");
        StudioExpression defaultValue = ParseExpression();
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (Match(StudioTokenKind.LeftBracket))
        {
            while (Current.Kind is not StudioTokenKind.RightBracket and not StudioTokenKind.End)
            {
                StudioToken key = Expect(StudioTokenKind.Identifier, "Ожидалось имя метаданных");
                Expect(StudioTokenKind.Equals, "В метаданных ожидался знак «=»");
                StudioToken value = Current.Kind switch
                {
                    StudioTokenKind.Number or StudioTokenKind.String or StudioTokenKind.Identifier => Advance(),
                    _ => throw CreateError("Ожидалось значение метаданных", Current.Span)
                };
                metadata[key.Text] = value.Text;
                if (!Match(StudioTokenKind.Comma))
                    break;
            }
            Expect(StudioTokenKind.RightBracket, "Ожидалась закрывающая «]»");
        }

        Expect(StudioTokenKind.Semicolon, "После параметра ожидалась «;»");
        return new StudioFormulaParameter(kind, name.Text, defaultValue, metadata, keyword.Span);
    }

    private IReadOnlyList<StudioStatement> ParseStatementBlock()
    {
        Expect(StudioTokenKind.LeftBrace, "После имени секции ожидалась «{»");
        var statements = new List<StudioStatement>();
        while (Current.Kind is not StudioTokenKind.RightBrace and not StudioTokenKind.End)
            statements.Add(ParseStatement());
        Expect(StudioTokenKind.RightBrace, "Ожидалась закрывающая «}»");
        return statements;
    }

    private StudioStatement ParseStatement()
    {
        StudioToken first = Expect(StudioTokenKind.Identifier, "Ожидалось объявление или присваивание");
        if (TryParseType(first, out StudioValueKind kind))
        {
            StudioToken name = Expect(StudioTokenKind.Identifier, "Ожидалось имя переменной");
            Expect(StudioTokenKind.Equals, "После имени переменной ожидался знак «=»");
            StudioExpression initializer = ParseExpression();
            Expect(StudioTokenKind.Semicolon, "После объявления ожидалась «;»");
            return new StudioVariableDeclaration(kind, name.Text, initializer, first.Span);
        }

        Expect(StudioTokenKind.Equals, "После имени переменной ожидался знак «=»");
        StudioExpression value = ParseExpression();
        Expect(StudioTokenKind.Semicolon, "После присваивания ожидалась «;»");
        return new StudioAssignment(first.Text, value, first.Span);
    }

    private StudioExpression ParseExpression() => ParseLogicalOr();

    private StudioExpression ParseLogicalOr()
    {
        StudioExpression left = ParseLogicalAnd();
        while (Current.Kind == StudioTokenKind.PipePipe)
        {
            StudioToken operation = Advance();
            left = new StudioBinaryExpression(left, operation.Kind, ParseLogicalAnd(), operation.Span);
        }
        return left;
    }

    private StudioExpression ParseLogicalAnd()
    {
        StudioExpression left = ParseEquality();
        while (Current.Kind == StudioTokenKind.AmpersandAmpersand)
        {
            StudioToken operation = Advance();
            left = new StudioBinaryExpression(left, operation.Kind, ParseEquality(), operation.Span);
        }
        return left;
    }

    private StudioExpression ParseEquality()
    {
        StudioExpression left = ParseComparison();
        while (Current.Kind is StudioTokenKind.EqualsEquals or StudioTokenKind.BangEquals)
        {
            StudioToken operation = Advance();
            left = new StudioBinaryExpression(left, operation.Kind, ParseComparison(), operation.Span);
        }
        return left;
    }

    private StudioExpression ParseComparison()
    {
        StudioExpression left = ParseTerm();
        while (Current.Kind is StudioTokenKind.Greater or StudioTokenKind.GreaterOrEquals or
               StudioTokenKind.Less or StudioTokenKind.LessOrEquals)
        {
            StudioToken operation = Advance();
            left = new StudioBinaryExpression(left, operation.Kind, ParseTerm(), operation.Span);
        }
        return left;
    }

    private StudioExpression ParseTerm()
    {
        StudioExpression left = ParseFactor();
        while (Current.Kind is StudioTokenKind.Plus or StudioTokenKind.Minus)
        {
            StudioToken operation = Advance();
            left = new StudioBinaryExpression(left, operation.Kind, ParseFactor(), operation.Span);
        }
        return left;
    }

    private StudioExpression ParseFactor()
    {
        StudioExpression left = ParsePower();
        while (Current.Kind is StudioTokenKind.Star or StudioTokenKind.Slash)
        {
            StudioToken operation = Advance();
            left = new StudioBinaryExpression(left, operation.Kind, ParsePower(), operation.Span);
        }
        return left;
    }

    private StudioExpression ParsePower()
    {
        StudioExpression left = ParseUnary();
        if (Current.Kind == StudioTokenKind.Caret)
        {
            StudioToken operation = Advance();
            return new StudioBinaryExpression(left, operation.Kind, ParsePower(), operation.Span);
        }
        return left;
    }

    private StudioExpression ParseUnary()
    {
        if (Current.Kind is StudioTokenKind.Plus or StudioTokenKind.Minus or StudioTokenKind.Bang)
        {
            StudioToken operation = Advance();
            return new StudioUnaryExpression(operation.Kind, ParseUnary(), operation.Span);
        }
        return ParsePrimary();
    }

    private StudioExpression ParsePrimary()
    {
        if (Current.Kind == StudioTokenKind.Number)
        {
            StudioToken number = Advance();
            return new StudioNumberExpression(number.Text, number.Span);
        }

        if (Current.Kind == StudioTokenKind.Identifier)
        {
            StudioToken identifier = Advance();
            if (!Match(StudioTokenKind.LeftParenthesis))
                return new StudioIdentifierExpression(identifier.Text, identifier.Span);

            var arguments = new List<StudioExpression>();
            if (Current.Kind != StudioTokenKind.RightParenthesis)
            {
                do
                {
                    arguments.Add(ParseExpression());
                } while (Match(StudioTokenKind.Comma));
            }
            Expect(StudioTokenKind.RightParenthesis, "Ожидалась закрывающая «)»");
            return new StudioCallExpression(identifier.Text, arguments, identifier.Span);
        }

        if (Match(StudioTokenKind.LeftParenthesis))
        {
            StudioExpression expression = ParseExpression();
            Expect(StudioTokenKind.RightParenthesis, "Ожидалась закрывающая «)»");
            return expression;
        }

        throw CreateError("Ожидалось выражение", Current.Span);
    }

    private static StudioValueKind ParseType(StudioToken token) =>
        TryParseType(token, out StudioValueKind kind)
            ? kind
            : throw CreateError($"Неизвестный тип «{token.Text}»", token.Span);

    private static bool TryParseType(StudioToken token, out StudioValueKind kind)
    {
        kind = token.Text.ToLowerInvariant() switch
        {
            "int" => StudioValueKind.Integer,
            "real" => StudioValueKind.Real,
            "complex" => StudioValueKind.Complex,
            "bool" => StudioValueKind.Boolean,
            _ => (StudioValueKind)(-1)
        };
        return (int)kind >= 0;
    }

    private bool IsIdentifier(string value) =>
        Current.Kind == StudioTokenKind.Identifier &&
        string.Equals(Current.Text, value, StringComparison.OrdinalIgnoreCase);

    private bool Match(StudioTokenKind kind)
    {
        if (Current.Kind != kind)
            return false;
        _position++;
        return true;
    }

    private StudioToken Expect(StudioTokenKind kind, string message)
    {
        if (Current.Kind != kind)
            throw CreateError(message, Current.Span);
        return Advance();
    }

    private StudioToken Advance()
    {
        StudioToken current = Current;
        if (_position < _tokens.Count - 1)
            _position++;
        return current;
    }

    private StudioToken Current => _tokens[Math.Min(_position, _tokens.Count - 1)];

    private static void Error(string message, StudioTextSpan span) => throw CreateError(message, span);
    private static StudioFormulaException CreateError(string message, StudioTextSpan span) => new(message, span);
}
