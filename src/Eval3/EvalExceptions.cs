namespace Eval3;

/// <summary>Eval3 组件中所有可预期异常的基类。</summary>
public abstract class EvalException : Exception
{
    /// <summary>创建异常。</summary>
    protected EvalException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// 公式的语法 / 语义错误。携带出错公式与出错位置，便于界面高亮定位。
/// </summary>
public sealed class ParserException : EvalException
{
    /// <summary>创建语法错误异常。</summary>
    public ParserException(string message, string formula, int position, int length = 1)
        : base(message)
    {
        RawMessage = message;
        Formula = formula;
        Position = position;
        Length = length;
    }

    /// <summary>不含位置后缀的原始错误信息。</summary>
    public string RawMessage { get; }

    /// <summary>出错时正在解析的完整公式文本。</summary>
    public string Formula { get; }

    /// <summary>出错位置（从 0 开始的字符下标）。</summary>
    public int Position { get; }

    /// <summary>出错记号长度（用于界面高亮，最小为 1）。</summary>
    public int Length { get; }

    /// <summary>返回形如 <c>... 位置 5</c> 的简短描述。</summary>
    public override string Message => $"{base.Message}（位置 {Position}）";
}

/// <summary>公式中引用了不存在的变量或函数。</summary>
public sealed class VariableNotFoundException : EvalException
{
    /// <summary>创建异常。</summary>
    public VariableNotFoundException(string variableName, Exception? innerException = null)
        : base($"{variableName} was not found", innerException)
    {
        VariableName = variableName;
    }

    /// <summary>变量名。</summary>
    public string VariableName { get; }
}

/// <summary>求值期错误（除零、类型不匹配、成员调用异常等）。</summary>
public sealed class FormulaRuntimeException : EvalException
{
    /// <summary>创建异常。</summary>
    public FormulaRuntimeException(string formula, Exception innerException, Expression? node = null)
        : base(BuildMessage(formula, node, innerException), innerException)
    {
        Formula = formula;
        NodeSource = node?.Source;
        NodeName = node?.Name;
        if (!string.IsNullOrEmpty(NodeSource))
        {
            int position = formula.IndexOf(NodeSource, StringComparison.Ordinal);
            if (position >= 0)
            {
                Position = position;
                Length = NodeSource!.Length;
            }
        }
    }

    /// <summary>出错的公式。</summary>
    public string Formula { get; }

    /// <summary>出错节点的原文片段（可能为空）。</summary>
    public string? NodeSource { get; }

    /// <summary>出错节点名称（可能为空）。</summary>
    public string? NodeName { get; }

    /// <summary>出错节点在公式中的近似位置（-1 表示无法定位）。</summary>
    public int Position { get; } = -1;

    /// <summary>出错节点的近似长度。</summary>
    public int Length { get; }

    private static string BuildMessage(string formula, Expression? node, Exception innerException)
    {
        string where = node is null || node.Source.Length == 0 ? string.Empty : $"（在 `{node.Source}` 处）";
        return $"计算 \"{formula}\" 时出错{where}：{innerException.Message}";
    }
}
