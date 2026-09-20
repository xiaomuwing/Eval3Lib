namespace Eval3;

/// <summary>
/// 表达式节点基类（对应历史版本的 <c>opCode</c>）。
/// 语法分析阶段即完成类型推导与运算方法绑定，求值阶段只做委托调用与装箱，无类型判断开销。
/// </summary>
public abstract class Expression : ITypedValue, IHasDescription
{
    /// <summary>创建节点。</summary>
    /// <param name="source">对应的原始公式片段。</param>
    protected Expression(string source) => Source = source;

    /// <summary>该节点对应的原始公式片段（便于界面展示与定位）。</summary>
    public string Source { get; }

    /// <summary>结果的逻辑类型。</summary>
    public abstract EvalType EvalType { get; }

    /// <summary>结果的 .NET 类型。</summary>
    public virtual Type SystemType => TypeTable.GetSystemType(EvalType);

    /// <summary>求值。</summary>
    public abstract object? Evaluate();

    /// <summary>该节点能否转换到目标逻辑类型（默认允许，由转换节点在运行时完成）。</summary>
    public virtual bool CanReturn(EvalType evalType) => true;

    /// <summary>节点名称。</summary>
    public virtual string Name => GetType().Name;

    /// <summary>节点说明。</summary>
    public virtual string Description => Name;

    /// <summary>子节点（用于语法树展示）。</summary>
    public virtual IReadOnlyList<Expression> Children => [];

    /// <summary>求值并转换为数值。</summary>
    public double EvaluateNumber() => ValueConvert.AsDouble(Evaluate());

    /// <summary>求值并转换为布尔值。</summary>
    public bool EvaluateBoolean() => ValueConvert.ToBoolean(Evaluate());

    /// <summary>求值并按展示规则格式化为字符串。</summary>
    public string EvaluateText() => ValueConvert.ConvertToString(Evaluate());

    object? IValue.Value => Evaluate();

    /// <summary>显示为原始公式片段。</summary>
    public override string ToString() => Source.Length != 0 ? Source : Name;

    /// <summary>
    /// 尝试把整棵子树编译为纯数值委托（跳过装箱 / 拆箱），
    /// 适用于只含数值常量与算术运算的公式。
    /// </summary>
    internal virtual bool TryBuildNumeric(out Func<double> evaluate)
    {
        evaluate = null!;
        return false;
    }
}
