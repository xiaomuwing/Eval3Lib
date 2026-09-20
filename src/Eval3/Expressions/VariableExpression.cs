namespace Eval3;

/// <summary>变量引用节点。</summary>
public sealed class VariableExpression : Expression
{
    private readonly ITypedValue _variable;

    internal VariableExpression(ITypedValue variable, string source)
        : base(source) => _variable = variable;

    /// <summary>被引用的变量。</summary>
    public ITypedValue Variable => _variable;

    /// <inheritdoc />
    public override EvalType EvalType => _variable.EvalType;

    /// <inheritdoc />
    public override Type SystemType => _variable.SystemType;

    /// <inheritdoc />
    public override object? Evaluate() => _variable.Value;

    /// <inheritdoc />
    public override string Name =>
        _variable is IHasDescription described ? $"变量 {described.Name}" : "变量";

    internal override bool TryBuildNumeric(out Func<double> evaluate)
    {
        // 只有数值型变量能进入快路径：日期 / 文本 / 对象参与运算时语义不同
        // （例如「日期 - 日期」取相差天数），必须走逐节点求值。
        if (_variable.EvalType != EvalType.Number)
        {
            evaluate = null!;
            return false;
        }

        ITypedValue variable = _variable;
        evaluate = () => ValueConvert.AsDouble(variable.Value);
        return true;
    }
}
