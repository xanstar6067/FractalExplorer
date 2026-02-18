using System;
using System.Reflection;
using System.Text.RegularExpressions;

namespace FractalExplorer.Parsers
{
    /// <summary>
    /// Мост между AngouriMath и внутренним AST парсера Newton.
    /// Использует AngouriMath только на этапе подготовки формулы,
    /// а затем переводит выражение обратно в локальный <see cref="ExpressionNode"/>.
    /// </summary>
    public static class AngouriExpressionBridge
    {
        /// <summary>
        /// Строит локальный AST для исходной формулы и её производной через AngouriMath.
        /// </summary>
        public static (ExpressionNode FormulaAst, ExpressionNode DerivativeAst, string FormulaText, string DerivativeText) Build(string expression)
        {
            var parsedEntity = ParseWithAngouri(expression);
            var derivativeEntity = DifferentiateWithAngouri(parsedEntity, "z");

            var formulaText = NormalizeForLegacyParser(parsedEntity.ToString() ?? string.Empty);
            var derivativeText = NormalizeForLegacyParser(derivativeEntity.ToString() ?? string.Empty);

            EnsureLegacyCompatible(formulaText);
            EnsureLegacyCompatible(derivativeText);

            var formulaAst = BuildLegacyAst(formulaText);
            var derivativeAst = BuildLegacyAst(derivativeText);

            return (formulaAst, derivativeAst, formulaText, derivativeText);
        }

        private static ExpressionNode BuildLegacyAst(string expression)
        {
            var tokenizer = new Tokenizer(expression);
            var tokens = tokenizer.Tokenize();
            var parser = new Parser(tokens);
            return parser.Parse();
        }

        private static object ParseWithAngouri(string expression)
        {
            var mathSType = Type.GetType("AngouriMath.MathS, AngouriMath");
            if (mathSType != null)
            {
                var fromString = mathSType.GetMethod("FromString", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null);
                if (fromString != null)
                {
                    return fromString.Invoke(null, new object[] { expression })
                        ?? throw new Exception("AngouriMath вернул null при парсинге выражения.");
                }
            }

            var entityType = Type.GetType("AngouriMath.Entity, AngouriMath")
                ?? throw new Exception("Не найден тип AngouriMath.Entity. Проверьте, что пакет AngouriMath доступен проекту.");

            var parseMethod = entityType.GetMethod("Parse", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null)
                ?? throw new Exception("Не найден метод парсинга в AngouriMath (ожидался MathS.FromString или Entity.Parse).");

            return parseMethod.Invoke(null, new object[] { expression })
                ?? throw new Exception("AngouriMath вернул null при парсинге выражения.");
        }

        private static object DifferentiateWithAngouri(object entity, string variable)
        {
            var entityType = entity.GetType();
            var differentiateMethod = entityType.GetMethod("Differentiate", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(string) }, null)
                ?? entityType.GetMethod("Derivative", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(string) }, null)
                ?? throw new Exception("Не найден метод дифференцирования в AngouriMath (ожидался Differentiate или Derivative).");

            var derivative = differentiateMethod.Invoke(entity, new object[] { variable })
                ?? throw new Exception("AngouriMath вернул null при вычислении производной.");

            var simplifyMethod = derivative.GetType().GetMethod("Simplify", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
            if (simplifyMethod != null)
            {
                derivative = simplifyMethod.Invoke(derivative, null) ?? derivative;
            }

            return derivative;
        }

        private static string NormalizeForLegacyParser(string expression)
        {
            return expression
                .Replace("**", "^")
                .Replace("−", "-")
                .Trim();
        }

        private static void EnsureLegacyCompatible(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
            {
                throw new Exception("AngouriMath вернул пустое выражение.");
            }

            var functionPattern = new Regex(@"[a-zA-Z]+\s*\(", RegexOptions.Compiled);
            var match = functionPattern.Match(expression);
            if (match.Success)
            {
                var functionName = match.Value.Replace("(", string.Empty).Trim();
                if (!string.Equals(functionName, "z", StringComparison.OrdinalIgnoreCase))
                {
                    throw new Exception($"Выражение '{expression}' содержит функцию '{functionName}(...)', которая пока не поддерживается внутренним AST.");
                }
            }
        }
    }
}
