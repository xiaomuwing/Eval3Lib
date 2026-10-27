namespace Eval3;

/// <summary>
/// 求值期错误定位：先按正常路径求值；失败后沿表达式树逐层复算，
/// 找到"自己失败、子节点都成功"的最深节点，供编辑器高亮与提示。
/// 只在异常路径使用，不影响热路径性能。
/// </summary>
public static class ExpressionDiagnostics
{
    /// <summary>返回第一个失败节点；公式本身成功时返回 <see langword="null"/>。</summary>
    public static Expression? FindFaultingNode(Expression root)
    {
        ArgumentNullException.ThrowIfNull(root);

        try
        {
            root.Evaluate();
            return null;
        }
#pragma warning disable CA1031
        catch (Exception)
#pragma warning restore CA1031
        {
            return Descend(root);
        }
    }

    private static Expression Descend(Expression node)
    {
        foreach (Expression child in node.Children)
        {
            try
            {
                child.Evaluate();
            }
#pragma warning disable CA1031
            catch (Exception)
#pragma warning restore CA1031
            {
                return Descend(child);
            }
        }

        return node;
    }
}
