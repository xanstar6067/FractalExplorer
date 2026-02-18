using System;
using System.Linq;
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
            var entityAssembly = entityType.Assembly;
            var variableEntity = BuildVariableEntity(variable, entityAssembly);

            // 1) Пробуем инстанс-методы разных версий API.
            foreach (var method in entityType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                     .Where(m => string.Equals(m.Name, "Differentiate", StringComparison.Ordinal) ||
                                 string.Equals(m.Name, "Derivative", StringComparison.Ordinal)))
            {
                if (TryInvokeMethod(method, entity, variable, variableEntity, out var derivative))
                {
                    return TrySimplify(derivative);
                }
            }

            // 2) Пробуем extension/static-методы из всей сборки AngouriMath.
            foreach (var method in entityAssembly
                     .GetTypes()
                     .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static))
                     .Where(m => string.Equals(m.Name, "Differentiate", StringComparison.Ordinal) ||
                                 string.Equals(m.Name, "Derivative", StringComparison.Ordinal)))
            {
                if (TryInvokeExtensionMethod(method, entity, variable, variableEntity, out var derivative))
                {
                    return TrySimplify(derivative);
                }
            }

            var availableSignatures = string.Join(", ",
                entityType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Where(m => string.Equals(m.Name, "Differentiate", StringComparison.Ordinal) ||
                                string.Equals(m.Name, "Derivative", StringComparison.Ordinal))
                    .Select(m => $"{m.Name}({string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name))})"));

            throw new Exception(
                $"Не найден совместимый метод дифференцирования в AngouriMath для текущей версии API. " +
                $"Найденные сигнатуры: {(string.IsNullOrWhiteSpace(availableSignatures) ? "<нет>" : availableSignatures)}");
        }

        private static object? BuildVariableEntity(string variable, Assembly entityAssembly)
        {
            var mathSType = entityAssembly.GetType("AngouriMath.MathS");
            var fromString = mathSType?.GetMethod("FromString", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null);
            if (fromString != null)
            {
                return fromString.Invoke(null, new object[] { variable });
            }

            var entityType = entityAssembly.GetType("AngouriMath.Entity");
            var parseMethod = entityType?.GetMethod("Parse", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null);
            return parseMethod?.Invoke(null, new object[] { variable });
        }

        private static bool TryInvokeMethod(MethodInfo method, object target, string variable, object? variableEntity, out object derivative)
        {
            derivative = null!;
            var parameters = method.GetParameters();
            if (parameters.Length != 1)
            {
                return false;
            }

            if (TryBuildArgument(parameters[0].ParameterType, variable, variableEntity, out var argument))
            {
                var result = method.Invoke(target, new[] { argument });
                if (result != null)
                {
                    derivative = result;
                    return true;
                }
            }

            return false;
        }

        private static bool TryInvokeExtensionMethod(MethodInfo method, object entity, string variable, object? variableEntity, out object derivative)
        {
            derivative = null!;
            var parameters = method.GetParameters();
            if (parameters.Length != 2)
            {
                return false;
            }

            if (!parameters[0].ParameterType.IsInstanceOfType(entity))
            {
                return false;
            }

            if (!TryBuildArgument(parameters[1].ParameterType, variable, variableEntity, out var argument))
            {
                return false;
            }

            var result = method.Invoke(null, new[] { entity, argument });
            if (result != null)
            {
                derivative = result;
                return true;
            }

            return false;
        }

        private static bool TryBuildArgument(Type parameterType, string variable, object? variableEntity, out object argument)
        {
            if (parameterType == typeof(string))
            {
                argument = variable;
                return true;
            }

            if (variableEntity != null && parameterType.IsInstanceOfType(variableEntity))
            {
                argument = variableEntity;
                return true;
            }

            argument = null!;
            return false;
        }

        private static object TrySimplify(object expression)
        {
            var simplifyMethod = expression.GetType().GetMethod("Simplify", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
            if (simplifyMethod == null)
            {
                return expression;
            }

            return simplifyMethod.Invoke(expression, null) ?? expression;
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
