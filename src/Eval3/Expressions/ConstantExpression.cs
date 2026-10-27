namespace Eval3;

/// <summary>字面量节点。</summary>
public sealed class ConstantExpression : Expression
{
    /// <summary>空字符串常量。</summary>
    public static readonly ConstantExpression EmptyString = new(EvalType.String, string.Empty, string.Empty);

    private readonly object? _value;
    private readonly EvalType _evalType;

    /// <summary>创建字面量节点。</summary>
    public ConstantExpression(EvalType evalType, object? value, string source = "")
        : base(source)
    {
        _value = value;
        _evalType = evalType;
    }

    /// <summary>创建数值常量。</summary>
    public static ConstantExpression Number(double value, string source = "") => new(EvalType.Number, value, source);

    /// <summary>创建整数常量（Integer 模式）。</summary>
    public static ConstantExpression Integer(long value, string source = "") => new(EvalType.Number, value, source);

    /// <summary>创建 decimal 数值常量（decimal 模式）。</summary>
    public static ConstantExpression Decimal(decimal value, string source = "") => new(EvalType.Number, value, source);

    /// <summary>创建布尔常量。</summary>
    public static ConstantExpression Boolean(bool value, string source = "") => new(EvalType.Boolean, value, source);

    /// <summary>创建字符串常量。</summary>
    public static ConstantExpression Text(string value, string source = "") => new(EvalType.String, value, source);

    /// <summary>创建日期常量。</summary>
    public static ConstantExpression Date(DateTime value, string source = "") => new(EvalType.Date, value, source);

    /// <summary>常量值。</summary>
    public object? Value => _value;

    /// <inheritdoc />
    public override EvalType EvalType => _evalType;

    /// <inheritdoc />
    public override Type SystemType => _evalType switch
    {
        EvalType.Object => _value?.GetType() ?? typeof(object),
        EvalType.Number when _value is decimal => typeof(decimal),
        EvalType.Number when _value is long => typeof(long),
        _ => base.SystemType,
    };

    /// <inheritdoc />
    public override object? Evaluate() => _value;

    /// <inheritdoc />
    public override string Name => _evalType switch
    {
        EvalType.Number => "数值常量",
        EvalType.String => "字符串常量",
        EvalType.Boolean => "布尔常量",
        EvalType.Date => "日期常量",
        _ => "常量",
    };

    internal override bool TryBuildNumeric(out Func<double> evaluate)
    {
        if (_evalType == EvalType.Number && _value is double value)
        {
            evaluate = () => value;
            return true;
        }

        evaluate = null!;
        return false;
    }
}
