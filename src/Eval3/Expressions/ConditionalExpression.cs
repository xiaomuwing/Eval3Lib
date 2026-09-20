namespace Eval3;

/// <summary>条件表达式节点：<c>if(条件, 真值, 假值)</c>，分支惰性求值。</summary>
public sealed class ConditionalExpression : Expression
{
    private readonly Expression _condition;
    private readonly Expression _whenTrue;
    private readonly Expression _whenFalse;

    internal ConditionalExpression(Expression condition, Expression whenTrue, Expression whenFalse, string source)
        : base(source)
    {
        _condition = condition;
        _whenTrue = whenTrue;
        _whenFalse = whenFalse;
    }

    /// <summary>条件。</summary>
    public Expression Condition => _condition;

    /// <summary>条件为真时的结果。</summary>
    public Expression WhenTrue => _whenTrue;

    /// <summary>条件为假时的结果。</summary>
    public Expression WhenFalse => _whenFalse;

    /// <inheritdoc />
    public override EvalType EvalType =>
        _whenTrue.EvalType == _whenFalse.EvalType ? _whenTrue.EvalType : EvalType.Object;

    /// <inheritdoc />
    public override Type SystemType =>
        _whenTrue.EvalType == _whenFalse.EvalType ? _whenTrue.SystemType : typeof(object);

    /// <inheritdoc />
    public override object? Evaluate() =>
        _condition.EvaluateBoolean() ? _whenTrue.Evaluate() : _whenFalse.Evaluate();

    /// <inheritdoc />
    public override IReadOnlyList<Expression> Children => [_condition, _whenTrue, _whenFalse];

    /// <inheritdoc />
    public override string Name => "条件表达式";
}
