using System.Text;

namespace Eval3;

/// <summary>
/// 插值字符串节点：<c>"合计：%[金额 * 1.06]"</c>。
/// 子表达式在解析期编译、求值期计算，替代历史版本中“解析期立刻求值”的不可用实现。
/// </summary>
public sealed class StringTemplateExpression : Expression
{
    private readonly StringTemplatePart[] _parts;
    private readonly int _capacity;

    internal StringTemplateExpression(IReadOnlyList<StringTemplatePart> parts, string source)
        : base(source)
    {
        _parts = [.. parts];
        int capacity = 0;
        foreach (StringTemplatePart part in _parts)
        {
            capacity += part.Literal?.Length ?? 16;
        }

        _capacity = Math.Clamp(capacity, 16, 256);
    }

    /// <summary>片段列表。</summary>
    public IReadOnlyList<StringTemplatePart> Parts => _parts;

    /// <inheritdoc />
    public override EvalType EvalType => EvalType.String;

    /// <inheritdoc />
    public override object? Evaluate()
    {
        if (_parts.Length == 1)
        {
            StringTemplatePart only = _parts[0];
            return only.Literal ?? ValueConvert.ConvertToString(only.Expression!.Evaluate());
        }

        var builder = new StringBuilder(_capacity);
        foreach (StringTemplatePart part in _parts)
        {
            if (part.Literal is { } literal)
            {
                builder.Append(literal);
            }
            else
            {
                builder.Append(ValueConvert.ConvertToString(part.Expression!.Evaluate()));
            }
        }

        return builder.ToString();
    }

    /// <inheritdoc />
    public override IReadOnlyList<Expression> Children =>
        _parts.Where(static p => p.Expression is not null).Select(static p => p.Expression!).ToArray();

    /// <inheritdoc />
    public override string Name => "插值字符串";
}
