namespace Eval3;

/// <summary>一元负号（取反）节点。</summary>
public sealed class NegateExpression : Expression
{
    private readonly Expression _operand;

    /// <summary>创建节点。</summary>
    public NegateExpression(Expression operand, string source = "")
        : base(source) => _operand = operand;

    /// <summary>操作数。</summary>
    public Expression Operand => _operand;

    /// <inheritdoc />
    public override EvalType EvalType => EvalType.Number;

    /// <inheritdoc />
    public override object? Evaluate() => -_operand.EvaluateNumber();

    /// <inheritdoc />
    public override IReadOnlyList<Expression> Children => [_operand];

    /// <inheritdoc />
    public override string Name => "取负";

    internal override bool TryBuildNumeric(out Func<double> evaluate)
    {
        if (_operand.TryBuildNumeric(out Func<double> inner))
        {
            evaluate = () => -inner();
            return true;
        }

        evaluate = null!;
        return false;
    }

}

/// <summary>逻辑取反节点（<c>not</c>）。</summary>
public sealed class NotExpression : Expression
{
    private readonly Expression _operand;

    /// <summary>创建节点。</summary>
    public NotExpression(Expression operand, string source = "")
        : base(source) => _operand = operand;

    /// <summary>操作数。</summary>
    public Expression Operand => _operand;

    /// <inheritdoc />
    public override EvalType EvalType => EvalType.Boolean;

    /// <inheritdoc />
    public override object? Evaluate() => !_operand.EvaluateBoolean();

    /// <inheritdoc />
    public override IReadOnlyList<Expression> Children => [_operand];

    /// <inheritdoc />
    public override string Name => "逻辑非";
}
