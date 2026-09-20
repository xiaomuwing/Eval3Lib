namespace Eval3;

/// <summary><c>%</c> 运算符的语义。</summary>
public enum PercentMode
{
    /// <summary>取模（C# 风格 / 历史 Eval3Lib2 行为，默认值）：<c>10 % 3 = 1</c>。</summary>
    Modulo = 0,

    /// <summary>百分比（历史 VB 版行为）：<c>5% = 0.05</c>、<c>100 + 5% = 105</c>。</summary>
    PercentOf = 1,
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

    /// <summary>公式缓存上限，超出后整体清空，避免无限增长。</summary>
    public int MaxCacheSize { get; init; } = 2048;
}
