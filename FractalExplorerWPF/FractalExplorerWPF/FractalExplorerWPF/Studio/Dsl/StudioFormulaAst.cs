using System.Collections.ObjectModel;

namespace FractalExplorerWPF.Studio.Dsl;

public enum StudioValueKind
{
    Integer,
    Real,
    Complex,
    Boolean
}

public readonly record struct StudioTextSpan(int Line, int Column, int Length)
{
    public override string ToString() => $"{Line}:{Column}";
}

public abstract record StudioExpression(StudioTextSpan Span);

public sealed record StudioNumberExpression(string Text, StudioTextSpan Span) : StudioExpression(Span);

public sealed record StudioIdentifierExpression(string Name, StudioTextSpan Span) : StudioExpression(Span);

public sealed record StudioUnaryExpression(
    StudioTokenKind Operator,
    StudioExpression Operand,
    StudioTextSpan Span) : StudioExpression(Span);

public sealed record StudioBinaryExpression(
    StudioExpression Left,
    StudioTokenKind Operator,
    StudioExpression Right,
    StudioTextSpan Span) : StudioExpression(Span);

public sealed record StudioCallExpression(
    string Name,
    IReadOnlyList<StudioExpression> Arguments,
    StudioTextSpan Span) : StudioExpression(Span);

public abstract record StudioStatement(StudioTextSpan Span);

public sealed record StudioVariableDeclaration(
    StudioValueKind Kind,
    string Name,
    StudioExpression Initializer,
    StudioTextSpan Span) : StudioStatement(Span);

public sealed record StudioAssignment(
    string Name,
    StudioExpression Value,
    StudioTextSpan Span) : StudioStatement(Span);

public sealed record StudioFormulaParameter(
    StudioValueKind Kind,
    string Name,
    StudioExpression DefaultValue,
    IReadOnlyDictionary<string, string> Metadata,
    StudioTextSpan Span)
{
    public string DisplayName =>
        Metadata.TryGetValue("label", out string? label) && !string.IsNullOrWhiteSpace(label)
            ? label
            : Name;
}

public sealed class StudioFormulaDocument
{
    public StudioFormulaDocument(
        string source,
        IReadOnlyList<StudioFormulaParameter> parameters,
        IReadOnlyList<StudioStatement> initialization,
        IReadOnlyList<StudioStatement> iteration,
        StudioExpression escapeCondition)
    {
        Source = source;
        Parameters = new ReadOnlyCollection<StudioFormulaParameter>(parameters.ToList());
        Initialization = new ReadOnlyCollection<StudioStatement>(initialization.ToList());
        Iteration = new ReadOnlyCollection<StudioStatement>(iteration.ToList());
        EscapeCondition = escapeCondition;
    }

    public string Source { get; }
    public IReadOnlyList<StudioFormulaParameter> Parameters { get; }
    public IReadOnlyList<StudioStatement> Initialization { get; }
    public IReadOnlyList<StudioStatement> Iteration { get; }
    public StudioExpression EscapeCondition { get; }
}

public sealed class StudioFormulaException : Exception
{
    public StudioFormulaException(string message, StudioTextSpan span)
        : base($"{message} (строка {span.Line}, столбец {span.Column})")
    {
        Span = span;
    }

    public StudioTextSpan Span { get; }
}
