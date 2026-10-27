namespace Eval3;

/// <summary>二元运算节点（<c>+ - * / % &amp; 比较</c>）。</summary>
public sealed class BinaryExpression : Expression
{
    private readonly BinaryOperator _operator;

    internal BinaryExpression(Expression left, BinaryOperator op, Expression right, string source)
        : base(source)
    {
        Left = left;
        _operator = op;
        Right = right;
    }

    /// <summary>左操作数。</summary>
    public Expression Left { get; }

    /// <summary>右操作数。</summary>
    public Expression Right { get; }

    /// <summary>运算符符号。</summary>
    public string Symbol => _operator.Symbol;

    /// <summary>运算符实例（供整树数值编译使用）。</summary>
    internal BinaryOperator Operator => _operator;

    /// <inheritdoc />
    public override EvalType EvalType => _operator.ResultType;

    /// <inheritdoc />
    public override Type SystemType => _operator switch
    {
        LongOperator => typeof(long),
        DecimalOperator => typeof(decimal),
        _ => base.SystemType,
    };

    /// <inheritdoc />
    public override object? Evaluate() => _operator.Apply(Left.Evaluate(), Right.Evaluate());

    /// <inheritdoc />
    public override IReadOnlyList<Expression> Children => [Left, Right];

    /// <inheritdoc />
    public override string Name => _operator.Name;

    internal override bool TryBuildNumeric(out Func<double> evaluate)
    {
        if (_operator.TryBuildNumeric(out Func<double, double, double>? apply)
            && Left.TryBuildNumeric(out Func<double> left)
            && Right.TryBuildNumeric(out Func<double> right))
        {
            evaluate = () => apply!(left(), right());
            return true;
        }

        evaluate = null!;
        return false;
    }

}

/// <summary>百分比节点：<c>5%</c>、<c>100 + 5%</c>。</summary>
public sealed class PercentExpression : Expression
{
    private readonly Expression _operand;
    private readonly Expression? _context;

    internal PercentExpression(Expression operand, Expression? context, string source)
        : base(source)
    {
        _operand = operand;
        _context = context;
    }

    /// <summary>百分数操作数。</summary>
    public Expression Operand => _operand;

    /// <summary>百分比参照的左侧操作数（可为空）。</summary>
    public Expression? Context => _context;

    /// <inheritdoc />
    public override EvalType EvalType => EvalType.Number;

    /// <inheritdoc />
    public override object? Evaluate()
    {
        double operand = _operand.EvaluateNumber();
        return _context is null ? operand / 100d : _context.EvaluateNumber() * operand / 100d;
    }

    /// <inheritdoc />
    public override IReadOnlyList<Expression> Children => _context is null ? [_operand] : [_operand, _context];

    /// <inheritdoc />
    public override string Name => _context is null ? "百分比" : "百分比（相对左值）";

    internal override bool TryBuildNumeric(out Func<double> evaluate)
    {
        if (_operand.TryBuildNumeric(out Func<double> operand))
        {
            if (_context is null)
            {
                evaluate = () => operand() / 100d;
                return true;
            }

            if (_context.TryBuildNumeric(out Func<double> context))
            {
                evaluate = () => context() * operand() / 100d;
                return true;
            }
        }

        evaluate = null!;
        return false;
    }
}
