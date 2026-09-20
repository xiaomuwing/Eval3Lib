namespace Eval3;

/// <summary>逻辑与 / 或节点，采用短路求值。</summary>
public sealed class LogicalExpression : Expression
{
    private readonly Expression _left;
    private readonly Expression _right;
    private readonly bool _isAnd;

    internal LogicalExpression(Expression left, Expression right, bool isAnd, string source)
        : base(source)
    {
        _left = left;
        _right = right;
        _isAnd = isAnd;
    }

    /// <summary>左操作数。</summary>
    public Expression Left => _left;

    /// <summary>右操作数。</summary>
    public Expression Right => _right;

    /// <summary>是否为逻辑与。</summary>
    public bool IsAnd => _isAnd;

    /// <inheritdoc />
    public override EvalType EvalType => EvalType.Boolean;

    /// <inheritdoc />
    public override object? Evaluate()
    {
        bool left = _left.EvaluateBoolean();
        return _isAnd ? left && _right.EvaluateBoolean() : left || _right.EvaluateBoolean();
    }

    /// <inheritdoc />
    public override IReadOnlyList<Expression> Children => [_left, _right];

    /// <inheritdoc />
    public override string Name => _isAnd ? "逻辑与" : "逻辑或";
}
