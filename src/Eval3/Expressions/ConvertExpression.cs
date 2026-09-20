namespace Eval3;

/// <summary>类型转换节点：把子表达式结果转换到目标逻辑类型。</summary>
public sealed class ConvertExpression : Expression
{
    private static readonly Func<object?, object?> ToBoolean = static value => ValueConvert.ToBoolean(value);
    private static readonly Func<object?, object?> ToDate = static value => ValueConvert.ToDate(value);
    private static readonly Func<object?, object?> ToNumber = static value => ValueConvert.ToDouble(value);
    private static readonly Func<object?, object?> ToStringValue = static value => ValueConvert.ToText(value);

    private readonly Expression _source;
    private readonly Func<object?, object?> _converter;
    private readonly EvalType _targetType;

    internal ConvertExpression(Expression source, EvalType targetType, string? sourceText = null)
        : base(sourceText ?? source.Source)
    {
        _source = source;
        _targetType = targetType;
        _converter = targetType switch
        {
            EvalType.Boolean => ToBoolean,
            EvalType.Date => ToDate,
            EvalType.Number => ToNumber,
            EvalType.String => ToStringValue,
            _ => throw new NotSupportedException($"不支持转换到 {targetType}"),
        };
    }

    /// <summary>源表达式。</summary>
    public Expression SourceExpression => _source;

    /// <inheritdoc />
    public override EvalType EvalType => _targetType;

    /// <inheritdoc />
    public override object? Evaluate() => _converter(_source.Evaluate());

    /// <inheritdoc />
    public override IReadOnlyList<Expression> Children => [_source];

    /// <inheritdoc />
    public override string Name => $"转换为 {_targetType}";

    internal override bool TryBuildNumeric(out Func<double> evaluate)
    {
        if (_targetType == EvalType.Number && _source.TryBuildNumeric(out Func<double> inner))
        {
            evaluate = inner;
            return true;
        }

        evaluate = null!;
        return false;
    }

}
