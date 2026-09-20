namespace Eval3;

/// <summary>任何可提供值的对象（表达式节点、变量等）。</summary>
public interface IValue
{
    /// <summary>获取当前值。</summary>
    object? Value { get; }
}

/// <summary>带类型信息的值。</summary>
public interface ITypedValue : IValue
{
    /// <summary>对应的 .NET 类型。</summary>
    Type SystemType { get; }

    /// <summary>对应的逻辑类型。</summary>
    EvalType EvalType { get; }
}

/// <summary>带名称与说明的成员（用于函数库 / 变量列表展示）。</summary>
public interface IHasDescription
{
    /// <summary>名称。</summary>
    string Name { get; }

    /// <summary>说明文本。</summary>
    string Description { get; }
}

/// <summary>变量容器：公式中的裸标识符会优先按函数 / 字段解析，其次再按变量容器解析。</summary>
public interface IVariableBag
{
    /// <summary>按名称查找变量，未找到返回 <see langword="null"/>。</summary>
    ITypedValue? GetVariable(string name);
}

/// <summary>
/// 函数库链：实现该接口的对象可以通过 <see cref="InheritedFunctions"/> 提供“父级”函数库，
/// 形成链式（继承式）查找，类似 VB 版本的 <c>iEvalFunctions</c>。
/// </summary>
public interface IInheritedFunctions
{
    /// <summary>父级函数库对象，<see langword="null"/> 表示链结束。</summary>
    object? InheritedFunctions { get; }
}
