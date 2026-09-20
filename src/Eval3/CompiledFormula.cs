using System.Runtime.CompilerServices;

namespace Eval3;

/// <summary>已解析（并可选编译为纯数值委托）的公式，可重复求值。</summary>
public sealed class CompiledFormula
{
    private readonly Func<double>? _numericEvaluate;

    internal CompiledFormula(string formula, Expression root)
    {
        Formula = formula;
        Root = root;
        _numericEvaluate = root.TryBuildNumeric(out Func<double> numeric) ? numeric : null;
    }

    /// <summary>公式原文。</summary>
    public string Formula { get; }

    /// <summary>表达式树根节点。</summary>
    public Expression Root { get; }

    /// <summary>是否具备纯数值快路径（无装箱 / 拆箱）。</summary>
    public bool HasNumericFastPath => _numericEvaluate is not null;

    /// <summary>求值。</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public object? Evaluate() => Root.Evaluate();

    /// <summary>求值并格式化为展示字符串。</summary>
    public string EvaluateToString() => ValueConvert.ConvertToString(Root.Evaluate());

    /// <summary>按数值求值（有快路径时跳过装箱）。</summary>
    public double EvaluateNumber() =>
        _numericEvaluate is null ? ValueConvert.AsDouble(Root.Evaluate()) : _numericEvaluate();

    /// <summary>尝试取纯数值委托。</summary>
    public bool TryGetNumericDelegate(out Func<double>? evaluate)
    {
        evaluate = _numericEvaluate;
        return _numericEvaluate is not null;
    }

    /// <summary>返回公式原文。</summary>
    public override string ToString() => Formula;
}
