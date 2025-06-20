using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;
using System;

namespace FractalExplorer.Parsers
{
    public enum TokenType { Number, Variable, Operator, LeftParen, RightParen }
    public record Token(TokenType Type, string Value);

    public class Tokenizer
    {
        private readonly string _text;
        private int _pos;

        public Tokenizer(string text)
        {
            _text = text.Replace(" ", "").ToLower();
            _pos = 0;
        }

        public List<Token> Tokenize()
        {
            var tokens = new List<Token>();
            while (_pos < _text.Length)
            {
                char current = _text[_pos];
                if (char.IsDigit(current) || current == '.')
                {
                    var start = _pos;
                    while (_pos < _text.Length && (char.IsDigit(_text[_pos]) || _text[_pos] == '.')) { _pos++; }
                    tokens.Add(new Token(TokenType.Number, _text.Substring(start, _pos - start)));
                    continue;
                }
                if (char.IsLetter(current))
                {
                    var start = _pos;
                    while (_pos < _text.Length && char.IsLetterOrDigit(_text[_pos])) { _pos++; }
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

    public abstract class ExpressionNode
    {
        public abstract Complex Evaluate(Dictionary<string, Complex> variables);
        public abstract ExpressionNode Differentiate(string varName);
        public abstract string Print(string indent = "");
        public override string ToString() => this.PrintSimple();
        public abstract string PrintSimple();
    }

    public class NumberNode : ExpressionNode
    {
        public Complex Value { get; }
        public NumberNode(Complex value) => Value = value;
        public override Complex Evaluate(Dictionary<string, Complex> variables) => Value;
        public override ExpressionNode Differentiate(string varName) => new NumberNode(Complex.Zero);
        public override string Print(string indent = "") => $"{indent}Number({Value})";
        public override string PrintSimple() => Value.ToString();
    }

    public class VariableNode : ExpressionNode
    {
        public string Name { get; }
        public VariableNode(string name) => Name = name;
        public override Complex Evaluate(Dictionary<string, Complex> variables)
        {
            if (Name == "i") return Complex.ImaginaryOne;
            if (variables.TryGetValue(Name, out var value)) return value;
            throw new Exception($"Значение для переменной '{Name}' не предоставлено.(Возможно вы хотели a*b, укажите операцию явно)");
        }
        public override ExpressionNode Differentiate(string varName)
        {
            if (Name == varName) return new NumberNode(Complex.One);
            if (Name == "i") return new NumberNode(Complex.Zero);
            return new NumberNode(Complex.Zero);
        }
        public override string Print(string indent = "") => $"{indent}Variable({Name})";
        public override string PrintSimple() => Name;
    }

    public class BinaryOpNode : ExpressionNode
    {
        public ExpressionNode Left { get; }
        public string Operator { get; }
        public ExpressionNode Right { get; }
        public BinaryOpNode(ExpressionNode left, string op, ExpressionNode right) { Left = left; Operator = op; Right = right; }
        public override Complex Evaluate(Dictionary<string, Complex> variables)
        {
            var leftVal = Left.Evaluate(variables);
            var rightVal = Right.Evaluate(variables);
            return Operator switch
            {
                "+" => leftVal + rightVal,
                "-" => leftVal - rightVal,
                "*" => leftVal * rightVal,
                "/" => leftVal / rightVal,
                "^" => Complex.Pow(leftVal, rightVal),
                _ => throw new Exception($"Неизвестный бинарный оператор '{Operator}'"),
            };
        }
        public override ExpressionNode Differentiate(string varName)
        {
            var u = Left; var v = Right;
            var du = Left.Differentiate(varName); var dv = Right.Differentiate(varName);
            return Operator switch
            {
                "+" => new BinaryOpNode(du, "+", dv),
                "-" => new BinaryOpNode(du, "-", dv),
                "*" => new BinaryOpNode(new BinaryOpNode(du, "*", v), "+", new BinaryOpNode(u, "*", dv)),
                "/" => new BinaryOpNode(new BinaryOpNode(new BinaryOpNode(du, "*", v), "-", new BinaryOpNode(u, "*", dv)), "/", new BinaryOpNode(v, "^", new NumberNode(new Complex(2, 0)))),
                "^" when v is NumberNode c => new BinaryOpNode(new BinaryOpNode(c, "*", du), "*", new BinaryOpNode(u, "^", new NumberNode(c.Value - 1))),
                _ => throw new Exception($"Дифференцирование для оператора '{Operator}' не поддерживается."),
            };
        }
        public override string Print(string indent = "")
        {
            var sb = new StringBuilder(); sb.AppendLine($"{indent}Op({Operator})");
            sb.AppendLine(Left.Print(indent + "  L:")); sb.Append(Right.Print(indent + "  R:"));
            return sb.ToString();
        }
        public override string PrintSimple() => $"({Left.PrintSimple()} {Operator} {Right.PrintSimple()})";
    }

    public class UnaryOpNode : ExpressionNode
    {
        public string Operator { get; }
        public ExpressionNode Operand { get; }
        public UnaryOpNode(string op, ExpressionNode operand) { Operator = op; Operand = operand; }
        public override Complex Evaluate(Dictionary<string, Complex> variables)
        {
            var operandVal = Operand.Evaluate(variables);
            return Operator switch { "-" => -operandVal, "+" => operandVal, _ => throw new Exception($"Неизвестный унарный оператор '{Operator}'"), };
        }
        public override ExpressionNode Differentiate(string varName) => new UnaryOpNode(Operator, Operand.Differentiate(varName));
        public override string Print(string indent = "")
        {
            var sb = new StringBuilder(); sb.AppendLine($"{indent}UnaryOp({Operator})");
            sb.Append(Operand.Print(indent + "   ")); return sb.ToString();
        }
        public override string PrintSimple() => $"({Operator}{Operand.PrintSimple()})";
    }

    public class Parser
    {
        private readonly List<Token> _tokens;
        private int _pos;
        public Parser(List<Token> tokens) { _tokens = tokens; _pos = 0; }
        private Token Current => _pos < _tokens.Count ? _tokens[_pos] : new Token(TokenType.Operator, "");
        private void Advance() => _pos++;
        public ExpressionNode Parse()
        {
            var result = ParseExpression();
            if (_pos < _tokens.Count) throw new Exception($"Неожиданный токен '{Current.Value}' после завершения выражения.");
            return result;
        }
        private ExpressionNode ParseExpression()
        {
            var node = ParseTerm();
            while (Current.Value == "+" || Current.Value == "-") { var op = Current; Advance(); var right = ParseTerm(); node = new BinaryOpNode(node, op.Value, right); }
            return node;
        }
        private ExpressionNode ParseTerm()
        {
            var node = ParseFactor();
            while (Current.Value == "*" || Current.Value == "/") { var op = Current; Advance(); var right = ParseFactor(); node = new BinaryOpNode(node, op.Value, right); }
            return node;
        }
        private ExpressionNode ParseFactor()
        {
            var node = ParsePrimary();
            if (Current.Value == "^") { var op = Current; Advance(); var right = ParseFactor(); node = new BinaryOpNode(node, op.Value, right); }
            return node;
        }
        private ExpressionNode ParsePrimary()
        {
            var token = Current;
            if (token.Value == "+" || token.Value == "-") { Advance(); return new UnaryOpNode(token.Value, ParsePrimary()); }
            if (token.Type == TokenType.Number)
            {
                Advance();
                if (!double.TryParse(token.Value.Replace('.', ','), out var number) && !double.TryParse(token.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out number))
                { throw new Exception($"Не удалось распознать число '{token.Value}'"); }
                return new NumberNode(new Complex(number, 0));
            }
            if (token.Type == TokenType.Variable) { Advance(); return new VariableNode(token.Value); }
            if (token.Type == TokenType.LeftParen)
            {
                Advance(); var node = ParseExpression();
                if (Current.Type != TokenType.RightParen) throw new Exception("Ожидалась закрывающая скобка ')'");
                Advance(); return node;
            }
            throw new Exception($"Неожиданный токен '{token.Value}'");
        }
    }
}