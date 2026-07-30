using System.Globalization;

namespace FractalExplorerWPF.Studio.Dsl;

public enum StudioTokenKind
{
    End,
    Identifier,
    Number,
    String,
    Plus,
    Minus,
    Star,
    Slash,
    Caret,
    Equals,
    EqualsEquals,
    Bang,
    BangEquals,
    Greater,
    GreaterOrEquals,
    Less,
    LessOrEquals,
    AmpersandAmpersand,
    PipePipe,
    LeftParenthesis,
    RightParenthesis,
    LeftBrace,
    RightBrace,
    LeftBracket,
    RightBracket,
    Comma,
    Semicolon
}

public readonly record struct StudioToken(
    StudioTokenKind Kind,
    string Text,
    StudioTextSpan Span);

public static class StudioFormulaLexer
{
    public static IReadOnlyList<StudioToken> Tokenize(string source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var tokens = new List<StudioToken>();
        int offset = 0;
        int line = 1;
        int column = 1;

        while (offset < source.Length)
        {
            char current = source[offset];
            if (char.IsWhiteSpace(current))
            {
                Advance(current, ref offset, ref line, ref column);
                continue;
            }

            if (current == '#' || current == '/' && Peek(source, offset + 1) == '/')
            {
                while (offset < source.Length && source[offset] is not '\r' and not '\n')
                    Advance(source[offset], ref offset, ref line, ref column);
                continue;
            }

            int tokenLine = line;
            int tokenColumn = column;
            int tokenOffset = offset;

            if (char.IsLetter(current) || current == '_')
            {
                do
                {
                    Advance(source[offset], ref offset, ref line, ref column);
                } while (offset < source.Length &&
                         (char.IsLetterOrDigit(source[offset]) || source[offset] == '_'));

                tokens.Add(Create(
                    StudioTokenKind.Identifier,
                    source[tokenOffset..offset],
                    tokenLine,
                    tokenColumn));
                continue;
            }

            if (char.IsDigit(current) || current == '.' && char.IsDigit(Peek(source, offset + 1)))
            {
                bool seenDecimalPoint = false;
                bool seenExponent = false;
                while (offset < source.Length)
                {
                    char value = source[offset];
                    if (char.IsDigit(value))
                    {
                        Advance(value, ref offset, ref line, ref column);
                        continue;
                    }

                    if (value == '.' && !seenDecimalPoint && !seenExponent)
                    {
                        seenDecimalPoint = true;
                        Advance(value, ref offset, ref line, ref column);
                        continue;
                    }

                    if ((value == 'e' || value == 'E') && !seenExponent)
                    {
                        seenExponent = true;
                        Advance(value, ref offset, ref line, ref column);
                        if (offset < source.Length && source[offset] is '+' or '-')
                            Advance(source[offset], ref offset, ref line, ref column);
                        continue;
                    }

                    break;
                }

                string number = source[tokenOffset..offset];
                if (!double.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                    throw new StudioFormulaException("Некорректное число", new(tokenLine, tokenColumn, number.Length));
                tokens.Add(Create(StudioTokenKind.Number, number, tokenLine, tokenColumn));
                continue;
            }

            if (current == '"')
            {
                Advance(current, ref offset, ref line, ref column);
                var value = new System.Text.StringBuilder();
                while (offset < source.Length && source[offset] != '"')
                {
                    char item = source[offset];
                    if (item == '\\' && offset + 1 < source.Length)
                    {
                        char escaped = source[offset + 1];
                        value.Append(escaped switch
                        {
                            'n' => '\n',
                            'r' => '\r',
                            't' => '\t',
                            '"' => '"',
                            '\\' => '\\',
                            _ => escaped
                        });
                        Advance(item, ref offset, ref line, ref column);
                        Advance(source[offset], ref offset, ref line, ref column);
                        continue;
                    }

                    value.Append(item);
                    Advance(item, ref offset, ref line, ref column);
                }

                if (offset >= source.Length)
                    throw new StudioFormulaException(
                        "Незавершённая строка",
                        new(tokenLine, tokenColumn, Math.Max(1, offset - tokenOffset)));
                Advance(source[offset], ref offset, ref line, ref column);
                tokens.Add(Create(StudioTokenKind.String, value.ToString(), tokenLine, tokenColumn));
                continue;
            }

            StudioTokenKind kind = current switch
            {
                '+' => StudioTokenKind.Plus,
                '-' => StudioTokenKind.Minus,
                '*' => StudioTokenKind.Star,
                '/' => StudioTokenKind.Slash,
                '^' => StudioTokenKind.Caret,
                '(' => StudioTokenKind.LeftParenthesis,
                ')' => StudioTokenKind.RightParenthesis,
                '{' => StudioTokenKind.LeftBrace,
                '}' => StudioTokenKind.RightBrace,
                '[' => StudioTokenKind.LeftBracket,
                ']' => StudioTokenKind.RightBracket,
                ',' => StudioTokenKind.Comma,
                ';' => StudioTokenKind.Semicolon,
                '=' when Peek(source, offset + 1) == '=' => StudioTokenKind.EqualsEquals,
                '=' => StudioTokenKind.Equals,
                '!' when Peek(source, offset + 1) == '=' => StudioTokenKind.BangEquals,
                '!' => StudioTokenKind.Bang,
                '>' when Peek(source, offset + 1) == '=' => StudioTokenKind.GreaterOrEquals,
                '>' => StudioTokenKind.Greater,
                '<' when Peek(source, offset + 1) == '=' => StudioTokenKind.LessOrEquals,
                '<' => StudioTokenKind.Less,
                '&' when Peek(source, offset + 1) == '&' => StudioTokenKind.AmpersandAmpersand,
                '|' when Peek(source, offset + 1) == '|' => StudioTokenKind.PipePipe,
                _ => throw new StudioFormulaException(
                    $"Недопустимый символ «{current}»",
                    new(tokenLine, tokenColumn, 1))
            };

            int length = kind is StudioTokenKind.EqualsEquals or StudioTokenKind.BangEquals or
                StudioTokenKind.GreaterOrEquals or StudioTokenKind.LessOrEquals or
                StudioTokenKind.AmpersandAmpersand or StudioTokenKind.PipePipe
                ? 2
                : 1;
            for (int i = 0; i < length; i++)
                Advance(source[offset], ref offset, ref line, ref column);
            tokens.Add(new StudioToken(kind, source[tokenOffset..offset], new(tokenLine, tokenColumn, length)));
        }

        tokens.Add(new StudioToken(StudioTokenKind.End, string.Empty, new(line, column, 0)));
        return tokens;
    }

    private static StudioToken Create(StudioTokenKind kind, string text, int line, int column) =>
        new(kind, text, new(line, column, text.Length));

    private static char Peek(string source, int offset) =>
        offset >= 0 && offset < source.Length ? source[offset] : '\0';

    private static void Advance(char value, ref int offset, ref int line, ref int column)
    {
        offset++;
        if (value == '\n')
        {
            line++;
            column = 1;
        }
        else
        {
            column++;
        }
    }
}
