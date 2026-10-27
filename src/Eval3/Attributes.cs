namespace Eval3;

/// <summary>
/// 标记“纯函数 / 纯函数库”：相同实参总是返回相同结果，且没有可观察副作用。
/// 数值计划据此启用公共子表达式消除（CSE）；未标记的成员一律按有副作用处理。
/// </summary>
[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field,
    AllowMultiple = false,
    Inherited = true)]
public sealed class PureAttribute : Attribute
{
}

/// <summary>在 <see cref="PureAttribute"/> 标记的库中显式排除某个成员（如 <c>Now()</c>）。</summary>
[AttributeUsage(
    AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field,
    AllowMultiple = false,
    Inherited = true)]
public sealed class ImpureAttribute : Attribute
{
}
