using System.Globalization;
using System.Numerics;
using System.Text;

namespace FractalExplorer.Parsers
{
    #region Tokens

    /// <summary>
    /// Определяет типы лексических токенов, используемых в парсере.
    /// </summary>
    public enum TokenType
    {
        Number,
        Variable,
        Operator,
        LeftParen,  // Открывающая скобка '('
        RightParen  // Закрывающая скобка ')'
    }

    /// <summary>
    /// Представляет лексический токен, состоящий из типа и значения.
    /// </summary>
    /// <param name="Type">Тип токена.</param>
    /// <param name="Value">Строковое значение токена.</param>
    public record Token(TokenType Type, string Value);

    #endregion

    #region Tokenizer

    /// <summary>
    /// Преобразует строковое выражение в список лексических токенов.
    /// Обрабатывает числа, переменные, операторы и скобки, а также вставляет неявное умножение.
    /// </summary>
    public class Tokenizer
    {
        private readonly string _text;
        private int _pos;

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="Tokenizer"/>.
        /// </summary>
        /// <param name="text">Входная строка выражения для токенизации.</param>
        public Tokenizer(string text)
        {
            _text = text.Replace(" ", "").ToLower(); // Удаляем пробелы и приводим к нижнему регистру
            _pos = 0;
        }

        /// <summary>
        /// Выполняет токенизацию входной строки выражения.
        /// </summary>
        /// <returns>Список лексических токенов.</returns>
        /// <exception cref="Exception">Выбрасывается при обнаружении неизвестного символа.</exception>
        public List<Token> Tokenize()
        {
            var tokens = new List<Token>();
            while (_pos < _text.Length)
            {
                char current = _text[_pos];

                if (char.IsDigit(current) || current == '.')
                {
                    var start = _pos;
                    while (_pos < _text.Length && (char.IsDigit(_text[_pos]) || _text[_pos] == '.'))
                    {
                        _pos++;
                    }
                    tokens.Add(new Token(TokenType.Number, _text.Substring(start, _pos - start)));
                    continue;
                }

                if (char.IsLetter(current))
                {
                    var start = _pos;
                    while (_pos < _text.Length && char.IsLetterOrDigit(_text[_pos]))
                    {
                        _pos++;
                    }
                    tokens.Add(new Token(TokenType.Variable, _text.Substring(start, _pos - start)));
                    continue;
                }

                if ("+-*/^".Contains(current))
                {
                    tokens.Add(new Token(TokenType.Operator, current.ToString()));
                    _pos++;
                    continue;
                }

                if (current == '(')
                {
                    tokens.Add(new Token(TokenType.LeftParen, current.ToString()));
                    _pos++;
                    continue;
                }

                if (current == ')')
                {
                    tokens.Add(new Token(TokenType.RightParen, current.ToString()));
                    _pos++;
                    continue;
                }

                throw new Exception($"Неизвестный символ '{current}' на позиции {_pos}");
            }
            return InsertImplicitMultiplication(tokens);
        }

        /// <summary>
        /// Вставляет токены умножения, где подразумевается неявное умножение
        /// (например, "2z" становится "2*z", "(z+1)z" становится "(z+1)*z").
        /// </summary>
        /// <param name="tokens">Исходный список токенов.</param>
        /// <returns>Список токенов с вставленными неявными умножениями.</returns>
        private List<Token> InsertImplicitMultiplication(List<Token> tokens)
        {
            var result = new List<Token>();
            for (int i = 0; i < tokens.Count; i++)
            {
                result.Add(tokens[i]);
                if (i < tokens.Count - 1)
                {
                    var current = tokens[i];
                    var next = tokens[i + 1];

                    // Умножение после числа, переменной или закрывающей скобки перед переменной или открывающей скобкой
                    if ((current.Type == TokenType.Number || current.Type == TokenType.Variable || current.Type == TokenType.RightParen) &&
                        (next.Type == TokenType.Variable || next.Type == TokenType.LeftParen))
                    {
                        result.Add(new Token(TokenType.Operator, "*"));
                    }
                }
            }
            return result;
        }
    }

    #endregion

    #region Expression Nodes (AST)

    /// <summary>
    /// Абстрактный базовый класс для узлов дерева абстрактного синтаксиса (AST).
    /// </summary>
    public abstract class ExpressionNode
    {
        /// <summary>
        /// Вычисляет значение выражения.
        /// </summary>
        /// <param name="variables">Словарь, содержащий значения переменных.</param>
        /// <returns>Комплексное значение выражения.</returns>
        public abstract Complex Evaluate(Dictionary<string, Complex> variables);

        /// <summary>
        /// Вычисляет производную выражения по заданной переменной.
        /// </summary>
        /// <param name="varName">Имя переменной, по которой производится дифференцирование.</param>
        /// <returns>Узел выражения, представляющий производную.</returns>
        public abstract ExpressionNode Differentiate(string varName);

        /// <summary>
        /// Печатает структурированное представление узла (для отладки).
        /// </summary>
        /// <param name="indent">Строка отступа для форматирования.</param>
        /// <returns>Строковое представление узла.</returns>
        public abstract string Print(string indent = "");

        /// <summary>
        /// Возвращает простое строковое представление узла, используя <see cref="PrintSimple"/>.
        /// </summary>
        /// <returns>Простое строковое представление узла.</returns>
        public override string ToString() => PrintSimple();

        /// <summary>
        /// Печатает простое (линейное) строковое представление узла.
        /// </summary>
        /// <returns>Простое строковое представление узла.</returns>
        public abstract string PrintSimple();
    }

    /// <summary>
    /// Представляет узел числа в дереве выражений.
    /// </summary>
    public class NumberNode : ExpressionNode
    {
        /// <summary>
        /// Получает комплексное значение числа.
        /// </summary>
        public Complex Value { get; }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="NumberNode"/>.
        /// </summary>
        /// <param name="value">Комплексное значение числа.</param>
        public NumberNode(Complex value) => Value = value;

        /// <summary>
        /// Вычисляет значение числа (возвращает само число).
        /// </summary>
        /// <param name="variables">Словарь переменных (не используется для чисел).</param>
        /// <returns>Комплексное значение числа.</returns>
        public override Complex Evaluate(Dictionary<string, Complex> variables) => Value;

        /// <summary>
        /// Вычисляет производную числа (всегда 0).
        /// </summary>
        /// <param name="varName">Имя переменной (не используется).</param>
        /// <returns>Узел числа, представляющий ноль.</returns>
        public override ExpressionNode Differentiate(string varName) => new NumberNode(Complex.Zero);

        /// <summary>
        /// Печатает структурированное представление узла числа.
        /// </summary>
        /// <param name="indent">Строка отступа.</param>
        /// <returns>Строковое представление узла.</returns>
        public override string Print(string indent = "") => $"{indent}Number({Value})";

        /// <summary>
        /// Печатает простое строковое представление числа.
        /// </summary>
        /// <returns>Строковое представление числа.</returns>
        public override string PrintSimple() => Value.ToString();
    }

    /// <summary>
    /// Представляет узел переменной в дереве выражений.
    /// </summary>
    public class VariableNode : ExpressionNode
    {
        /// <summary>
        /// Получает имя переменной.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="VariableNode"/>.
        /// </summary>
        /// <param name="name">Имя переменной.</param>
        public VariableNode(string name) => Name = name;

        /// <summary>
        /// Вычисляет значение переменной.
        /// </summary>
        /// <param name="variables">Словарь, содержащий значения переменных.</param>
        /// <returns>Комплексное значение переменной.</returns>
        /// <exception cref="Exception">Выбрасывается, если значение переменной не предоставлено.</exception>
        public override Complex Evaluate(Dictionary<string, Complex> variables)
        {
            if (Name == "i")
            {
                return Complex.ImaginaryOne;
            }
            if (variables.TryGetValue(Name, out var value))
            {
                return value;
            }
            throw new Exception($"Значение для переменной '{Name}' не предоставлено.(Возможно вы хотели a*b, укажите операцию явно)");
        }

        /// <summary>
        /// Вычисляет производную переменной.
        /// </summary>
        /// <param name="varName">Имя переменной, по которой производится дифференцирование.</param>
        /// <returns>Узел числа, представляющий 1, если имя переменной совпадает, иначе 0.</returns>
        public override ExpressionNode Differentiate(string varName)
        {
            if (Name == varName)
            {
                return new NumberNode(Complex.One);
            }
            if (Name == "i") // Производная от константы 'i' равна 0
            {
                return new NumberNode(Complex.Zero);
            }
            return new NumberNode(Complex.Zero);
        }

        /// <summary>
        /// Печатает структурированное представление узла переменной.
        /// </summary>
        /// <param name="indent">Строка отступа.</param>
        /// <returns>Строковое представление узла.</returns>
        public override string Print(string indent = "") => $"{indent}Variable({Name})";

        /// <summary>
        /// Печатает простое строковое представление переменной.
        /// </summary>
        /// <returns>Строковое представление переменной.</returns>
        public override string PrintSimple() => Name;
    }

    /// <summary>
    /// Представляет узел бинарной операции в дереве выражений.
    /// </summary>
    public class BinaryOpNode : ExpressionNode
    {
        /// <summary>
        /// Получает левый операнд.
        /// </summary>
        public ExpressionNode Left { get; }

        /// <summary>
        /// Получает оператор.
        /// </summary>
        public string Operator { get; }

        /// <summary>
        /// Получает правый операнд.
        /// </summary>
        public ExpressionNode Right { get; }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="BinaryOpNode"/>.
        /// </summary>
        /// <param name="left">Левый операнд.</param>
        /// <param name="op">Оператор.</param>
        /// <param name="right">Правый операнд.</param>
        public BinaryOpNode(ExpressionNode left, string op, ExpressionNode right)
        {
            Left = left;
            Operator = op;
            Right = right;
        }

        /// <summary>
        /// Вычисляет значение бинарной операции.
        /// </summary>
        /// <param name="variables">Словарь переменных.</param>
        /// <returns>Комплексное значение результата операции.</returns>
        /// <exception cref="Exception">Выбрасывается при неизвестном бинарном операторе.</exception>
        public override Complex Evaluate(Dictionary<string, Complex> variables)
        {
            var leftValue = Left.Evaluate(variables);
            var rightValue = Right.Evaluate(variables);

            return Operator switch
            {
                "+" => leftValue + rightValue,
                "-" => leftValue - rightValue,
                "*" => leftValue * rightValue,
                "/" => leftValue / rightValue,
                "^" => Complex.Pow(leftValue, rightValue),
                _ => throw new Exception($"Неизвестный бинарный оператор '{Operator}'"),
            };
        }

        /// <summary>
        /// Вычисляет производную бинарной операции по заданной переменной.
        /// Поддерживает правила дифференцирования для сложения, вычитания, умножения, деления и возведения в степень (только для числового показателя).
        /// </summary>
        /// <param name="varName">Имя переменной, по которой производится дифференцирование.</param>
        /// <returns>Узел выражения, представляющий производную.</returns>
        /// <exception cref="Exception">Выбрасывается, если дифференцирование для данного оператора не поддерживается.</exception>
        public override ExpressionNode Differentiate(string varName)
        {
            var u = Left; // Обозначение для левого операнда
            var v = Right; // Обозначение для правого операнда
            var du = Left.Differentiate(varName); // Производная левого операнда
            var dv = Right.Differentiate(varName); // Производная правого операнда

            return Operator switch
            {
                "+" => new BinaryOpNode(du, "+", dv),
                "-" => new BinaryOpNode(du, "-", dv),
                "*" => new BinaryOpNode(new BinaryOpNode(du, "*", v), "+", new BinaryOpNode(u, "*", dv)), // Правило произведения: (uv)' = u'v + uv'
                "/" => new BinaryOpNode(new BinaryOpNode(new BinaryOpNode(du, "*", v), "-", new BinaryOpNode(u, "*", dv)), "/", new BinaryOpNode(v, "^", new NumberNode(new Complex(2, 0)))), // Правило частного: (u/v)' = (u'v - uv') / v^2
                "^" when v is NumberNode c => new BinaryOpNode(new BinaryOpNode(c, "*", du), "*", new BinaryOpNode(u, "^", new NumberNode(c.Value - 1))), // Правило степени: (u^c)' = c * u^(c-1) * u'
                _ => throw new Exception($"Дифференцирование для оператора '{Operator}' не поддерживается."),
            };
        }

        /// <summary>
        /// Печатает структурированное представление узла бинарной операции.
        /// </summary>
        /// <param name="indent">Строка отступа.</param>
        /// <returns>Строковое представление узла.</returns>
        public override string Print(string indent = "")
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{indent}Op({Operator})");
            sb.AppendLine(Left.Print(indent + "  L:")); // Левый операнд
            sb.Append(Right.Print(indent + "  R:")); // Правый операнд
            return sb.ToString();
        }

        /// <summary>
        /// Печатает простое строковое представление бинарной операции.
        /// </summary>
        /// <returns>Строковое представление операции.</returns>
        public override string PrintSimple() => $"({Left.PrintSimple()} {Operator} {Right.PrintSimple()})";
    }

    /// <summary>
    /// Представляет узел унарной операции в дереве выражений.
    /// </summary>
    public class UnaryOpNode : ExpressionNode
    {
        /// <summary>
        /// Получает оператор.
        /// </summary>
        public string Operator { get; }

        /// <summary>
        /// Получает операнд.
        /// </summary>
        public ExpressionNode Operand { get; }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="UnaryOpNode"/>.
        /// </summary>
        /// <param name="op">Оператор.</param>
        /// <param name="operand">Операнд.</param>
        public UnaryOpNode(string op, ExpressionNode operand)
        {
            Operator = op;
            Operand = operand;
        }

        /// <summary>
        /// Вычисляет значение унарной операции.
        /// </summary>
        /// <param name="variables">Словарь переменных.</param>
        /// <returns>Комплексное значение результата операции.</returns>
        /// <exception cref="Exception">Выбрасывается при неизвестном унарном операторе.</exception>
        public override Complex Evaluate(Dictionary<string, Complex> variables)
        {
            var operandValue = Operand.Evaluate(variables);
            return Operator switch
            {
                "-" => -operandValue,
                "+" => operandValue,
                _ => throw new Exception($"Неизвестный унарный оператор '{Operator}'"),
            };
        }

        /// <summary>
        /// Вычисляет производную унарной операции.
        /// </summary>
        /// <param name="varName">Имя переменной, по которой производится дифференцирование.</param>
        /// <returns>Узел выражения, представляющий производную.</returns>
        public override ExpressionNode Differentiate(string varName) => new UnaryOpNode(Operator, Operand.Differentiate(varName));

        /// <summary>
        /// Печатает структурированное представление узла унарной операции.
        /// </summary>
        /// <param name="indent">Строка отступа.</param>
        /// <returns>Строковое представление узла.</returns>
        public override string Print(string indent = "")
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{indent}UnaryOp({Operator})");
            sb.Append(Operand.Print(indent + "   "));
            return sb.ToString();
        }

        /// <summary>
        /// Печатает простое строковое представление унарной операции.
        /// </summary>
        /// <returns>Строковое представление операции.</returns>
        public override string PrintSimple() => $"({Operator}{Operand.PrintSimple()})";
    }

    #endregion

    #region Parser

    /// <summary>
    /// Парсер выражений, который строит дерево абстрактного синтаксиса (AST)
    /// из списка токенов. Использует рекурсивный спуск для обработки выражений.
    /// </summary>
    public class Parser
    {
        private readonly List<Token> _tokens;
        private int _pos;

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="Parser"/>.
        /// </summary>
        /// <param name="tokens">Список токенов, полученный от токенизатора.</param>
        public Parser(List<Token> tokens)
        {
            _tokens = tokens;
            _pos = 0;
        }

        /// <summary>
        /// Получает текущий токен без перемещения указателя.
        /// Возвращает пустой оператор, если достигнут конец списка токенов.
        /// </summary>
        private Token Current => _pos < _tokens.Count ? _tokens[_pos] : new Token(TokenType.Operator, "");

        /// <summary>
        /// Перемещает указатель к следующему токену.
        /// </summary>
        private void Advance() => _pos++;

        /// <summary>
        /// Начинает процесс парсинга выражения и возвращает корневой узел AST.
        /// </summary>
        /// <returns>Корневой узел <see cref="ExpressionNode"/>.</returns>
        /// <exception cref="Exception">Выбрасывается при обнаружении неожиданных токенов после завершения выражения.</exception>
        public ExpressionNode Parse()
        {
            var result = ParseExpression();
            if (_pos < _tokens.Count)
            {
                throw new Exception($"Неожиданный токен '{Current.Value}' после завершения выражения.");
            }
            return result;
        }

        /// <summary>
        /// Парсит выражение, обрабатывая операции сложения и вычитания.
        /// </summary>
        /// <returns>Узел выражения.</returns>
        private ExpressionNode ParseExpression()
        {
            var node = ParseTerm();
            while (Current.Value == "+" || Current.Value == "-")
            {
                var op = Current;
                Advance();
                var right = ParseTerm();
                node = new BinaryOpNode(node, op.Value, right);
            }
            return node;
        }

        /// <summary>
        /// Парсит член выражения, обрабатывая операции умножения и деления.
        /// </summary>
        /// <returns>Узел выражения.</returns>
        private ExpressionNode ParseTerm()
        {
            var node = ParseFactor();
            while (Current.Value == "*" || Current.Value == "/")
            {
                var op = Current;
                Advance();
                var right = ParseFactor();
                node = new BinaryOpNode(node, op.Value, right);
            }
            return node;
        }

        /// <summary>
        /// Парсит фактор выражения, обрабатывая операции возведения в степень.
        /// </summary>
        /// <returns>Узел выражения.</returns>
        private ExpressionNode ParseFactor()
        {
            var node = ParsePrimary();
            if (Current.Value == "^")
            {
                var op = Current;
                Advance();
                var right = ParseFactor(); // Правая ассоциативность для степени
                node = new BinaryOpNode(node, op.Value, right);
            }
            return node;
        }

        /// <summary>
        /// Парсит первичные элементы выражения: числа, переменные, унарные операции, выражения в скобках.
        /// </summary>
        /// <returns>Узел выражения.</returns>
        /// <exception cref="Exception">Выбрасывается при ошибках парсинга чисел или отсутствии закрывающей скобки.</exception>
        private ExpressionNode ParsePrimary()
        {
            var token = Current;
            if (token.Value == "+" || token.Value == "-")
            {
                Advance();
                return new UnaryOpNode(token.Value, ParsePrimary());
            }

            if (token.Type == TokenType.Number)
            {
                Advance();
                // Пробуем парсить число с учетом различных десятичных разделителей
                if (!double.TryParse(token.Value.Replace('.', ','), out double number) &&
                    !double.TryParse(token.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out number))
                {
                    throw new Exception($"Не удалось распознать число '{token.Value}'");
                }
                return new NumberNode(new Complex(number, 0));
            }

            if (token.Type == TokenType.Variable)
            {
                Advance();
                return new VariableNode(token.Value);
            }

            if (token.Type == TokenType.LeftParen)
            {
                Advance();
                var node = ParseExpression();
                if (Current.Type != TokenType.RightParen)
                {
                    throw new Exception("Ожидалась закрывающая скобка ')'");
                }
                Advance();
                return node;
            }

            throw new Exception($"Неожиданный токен '{token.Value}'");
        }
    }

    #endregion
}