namespace Eval3;

/// <summary><c>%</c> 运算符的语义。</summary>
public enum PercentMode
{
    /// <summary>取模（C# 风格 / 历史 Eval3Lib2 行为，默认值）：<c>10 % 3 = 1</c>。</summary>
    Modulo = 0,

    /// <summary>百分比（历史 VB 版行为）：<c>5% = 0.05</c>、<c>100 + 5% = 105</c>。</summary>
    PercentOf = 1,
}

/// <summary>数值模式。</summary>
public enum NumericMode
{
    /// <summary>double（默认，性能优先）。</summary>
    Double = 0,

    /// <summary>decimal（金额/税额等需要十进制定点的场景；不参与整树编译，走树遍历）。</summary>
    Decimal = 1,

    /// <summary>整数（整数字面量与 + - * % 走 long；溢出提升为 decimal；/ 返回 decimal）。</summary>
    Integer = 2,
}

/// <summary>计算组件配置。</summary>
public sealed class EvaluatorOptions
{
    /// <summary>公式语法风格，默认 C# 风格。</summary>
    public ParserSyntax Syntax { get; init; } = ParserSyntax.CSharp;

    /// <summary>变量 / 成员名是否区分大小写，默认不区分。</summary>
    public bool CaseSensitive { get; init; }

    /// <summary><c>%</c> 运算符语义。</summary>
    public PercentMode PercentMode { get; init; } = PercentMode.Modulo;

    /// <summary>是否缓存已解析公式，重复公式可跳过词法与语法分析。</summary>
    public bool EnableFormulaCache { get; init; } = true;

    /// <summary>公式缓存上限（LRU 容量）。</summary>
    public int MaxCacheSize { get; init; } = 2048;

    /// <summary>是否缓存已解析 AST（编辑器反复校验同一公式时避免重复解析分配），默认开启。</summary>
    public bool EnableParseCache { get; init; } = true;

    /// <summary>
    /// 是否使用「整树单 lambda」数值编译器（默认开启）。
    /// 置为 <see langword="false"/> 可回退到旧的逐节点 <c>Func&lt;double&gt;</c> 组合路径，便于 A/B 对照。
    /// </summary>
    public bool UseWholeTreeNumericCompiler { get; init; } = true;

    /// <summary>数值模式：double（默认）或 decimal。</summary>
    public NumericMode NumericMode { get; init; } = Eval3.NumericMode.Double;

    /// <summary>
    /// 是否允许生成表达式树 / IL（默认开启）。
    /// 置为 <see langword="false"/> 时所有公式走树遍历，适用于 NativeAOT、iOS 等禁止动态代码的环境。
    /// </summary>
    public bool EnableJit { get; init; } = true;
}
