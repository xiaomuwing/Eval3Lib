using System.Text;

namespace Eval3;

/// <summary>表达式树文本化，用于界面展示解析结果。</summary>
public static class ExpressionTreeFormatter
{
    /// <summary>输出缩进树。</summary>
    public static string Dump(Expression expression)
    {
        ArgumentNullException.ThrowIfNull(expression);
        var builder = new StringBuilder(256);
        Append(builder, expression, string.Empty, isLast: true, isRoot: true);
        return builder.ToString();
    }

    /// <summary>收集树中所有节点（前序遍历）。</summary>
    public static IReadOnlyList<Expression> Flatten(Expression expression)
    {
        ArgumentNullException.ThrowIfNull(expression);
        var nodes = new List<Expression>(16);
        var stack = new Stack<Expression>();
        stack.Push(expression);
        while (stack.Count > 0)
        {
            Expression node = stack.Pop();
            nodes.Add(node);
            IReadOnlyList<Expression> children = node.Children;
            for (int i = children.Count - 1; i >= 0; i--)
            {
                stack.Push(children[i]);
            }
        }

        return nodes;
    }

    private static void Append(StringBuilder builder, Expression node, string indent, bool isLast, bool isRoot)
    {
        if (!isRoot)
        {
            builder.Append(indent).Append(isLast ? "└─ " : "├─ ");
        }

        builder.Append(node.Name);
        builder.Append("  [").Append(node.EvalType).Append(']');
        if (node.Source.Length > 0 && !node.Source.Equals(node.Name, StringComparison.Ordinal))
        {
            builder.Append("  ").Append(node.Source);
        }

        builder.AppendLine();

        IReadOnlyList<Expression> children = node.Children;
        if (children.Count == 0)
        {
            return;
        }

        string childIndent = isRoot ? string.Empty : indent + (isLast ? "   " : "│  ");
        for (int i = 0; i < children.Count; i++)
        {
            Append(builder, children[i], childIndent, i == children.Count - 1, isRoot: false);
        }
    }
}
