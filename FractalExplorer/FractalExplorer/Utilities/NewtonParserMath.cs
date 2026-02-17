using AngouriMath;
using AngouriMath.Extensions;
using System.Globalization;
using System.Numerics;

namespace FractalExplorer.Parsers
{
    public enum TokenType
    {
        Expression
    }

    public record Token(TokenType Type, string Value);

    public class Tokenizer
    {
        private readonly string _expression;

        public Tokenizer(string text)
        {
            _expression = text?.Trim() ?? string.Empty;
        }

        public List<Token> Tokenize()
        {
            if (string.IsNullOrWhiteSpace(_expression))
            {
                throw new Exception("Формула не может быть пустой.");
            }

            return new List<Token> { new(TokenType.Expression, _expression) };
        }
    }

    public abstract class ExpressionNode
    {
        public abstract Complex Evaluate(Dictionary<string, Complex> variables);
        public abstract ExpressionNode Differentiate(string varName);
        public abstract string Print(string indent = "");
        public abstract string PrintSimple();
        public override string ToString() => PrintSimple();
    }

    public class AngouriExpressionNode : ExpressionNode
    {
        private readonly Entity _entity;

        public AngouriExpressionNode(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
            {
                throw new Exception("Формула не может быть пустой.");
            }

            _entity = MathS.FromString(expression).Simplify();
        }

        private AngouriExpressionNode(Entity entity)
        {
            _entity = entity.Simplify();
        }

        public override Complex Evaluate(Dictionary<string, Complex> variables)
        {
            Entity substituted = _entity;

            foreach (var variable in variables)
            {
                substituted = substituted.Substitute(variable.Key, ToEntity(variable.Value));
            }

            substituted = substituted.Substitute("i", MathS.i);

            if (!substituted.EvaluableNumerical)
            {
                return new Complex(double.NaN, double.NaN);
            }

            Entity numericResult = substituted.EvalNumerical();
            return ToComplex(numericResult);
        }

        public override ExpressionNode Differentiate(string varName)
            => new AngouriExpressionNode(_entity.Differentiate(varName));

        public override string Print(string indent = "") => indent + _entity.Stringize();

        public override string PrintSimple() => _entity.Stringize();

        private static Entity ToEntity(Complex complex)
        {
            string real = complex.Real.ToString("R", CultureInfo.InvariantCulture);
            string imaginary = complex.Imaginary.ToString("R", CultureInfo.InvariantCulture);
            return MathS.FromString($"({real}+({imaginary})i)");
        }

        private static Complex ToComplex(Entity entity)
        {
            string expression = entity.Stringize();
            expression = expression.Replace(" ", string.Empty).Replace("I", "i");

            if (!expression.Contains('i'))
            {
                return new Complex(ParseDouble(expression), 0);
            }

            expression = expression.Replace("*", string.Empty);

            if (expression == "i")
            {
                return new Complex(0, 1);
            }

            if (expression == "-i")
            {
                return new Complex(0, -1);
            }

            int iIndex = expression.IndexOf('i');
            string beforeI = expression[..iIndex];

            int splitIndex = -1;
            for (int index = 1; index < beforeI.Length; index++)
            {
                if (beforeI[index] == '+' || beforeI[index] == '-')
                {
                    splitIndex = index;
                }
            }

            if (splitIndex == -1)
            {
                return new Complex(0, ParseImaginary(beforeI));
            }

            string realPart = beforeI[..splitIndex];
            string imaginaryPart = beforeI[splitIndex..];
            return new Complex(ParseDouble(realPart), ParseImaginary(imaginaryPart));
        }

        private static double ParseImaginary(string value)
        {
            if (value == "+" || string.IsNullOrEmpty(value)) return 1;
            if (value == "-") return -1;
            return ParseDouble(value);
        }

        private static double ParseDouble(string value)
        {
            if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
            {
                return result;
            }

            throw new Exception($"Не удалось преобразовать числовое значение '{value}' в комплексное число.");
        }
    }

    public class Parser
    {
        private readonly List<Token> _tokens;

        public Parser(List<Token> tokens)
        {
            _tokens = tokens;
        }

        public ExpressionNode Parse()
        {
            if (_tokens.Count == 0)
            {
                throw new Exception("Не удалось разобрать формулу: нет токенов.");
            }

            string expression = string.Join("", _tokens.Select(token => token.Value));
            return new AngouriExpressionNode(expression);
        }
    }
}
