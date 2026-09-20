namespace Eval3;

/// <summary>带类型、名称与说明的变量，可被公式引用。</summary>
public sealed class EvalVariable : ITypedValue, IHasDescription
{
    private volatile object? _value;
    private Type _systemType;
    private EvalType _evalType;

    /// <summary>创建变量。</summary>
    /// <param name="name">变量名。</param>
    /// <param name="value">初始值。</param>
    /// <param name="description">说明文本。</param>
    /// <param name="systemType">显式指定的 .NET 类型；为空时按值推断。</param>
    public EvalVariable(string name, object? value, string description = "", Type? systemType = null)
    {
        Name = name;
        Description = description;
        _value = value;
        _systemType = systemType ?? value?.GetType() ?? typeof(object);
        _evalType = TypeTable.GetEvalType(_systemType);
    }

    /// <summary>变量名。</summary>
    public string Name { get; }

    /// <summary>说明文本。</summary>
    public string Description { get; set; }

    /// <summary>变量当前值。写入时不改变静态类型，静态类型变更需重新解析公式。</summary>
    public object? Value
    {
        get => _value;
        set => _value = value;
    }

    /// <summary>变量的 .NET 类型。</summary>
    public Type SystemType => _systemType;

    /// <summary>变量的逻辑类型。</summary>
    public EvalType EvalType => _evalType;

    /// <summary>显式设置静态类型（会影响已解析公式的类型推导，请谨慎使用）。</summary>
    public void SetSystemType(Type systemType)
    {
        ArgumentNullException.ThrowIfNull(systemType);
        _systemType = systemType;
        _evalType = TypeTable.GetEvalType(systemType);
    }

    /// <summary>返回 <c>名称 = 值</c> 形式的文本。</summary>
    public override string ToString() => $"{Name} = {ValueConvert.ConvertToString(_value)}";
}
