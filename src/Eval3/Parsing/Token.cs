namespace Eval3;

/// <summary>插值字符串（<c>"合计：%[金额 * 1.06]"</c>）中的片段。</summary>
public sealed class StringTemplatePart
{
    private StringTemplatePart(string? literal, Expression? expression)
    {
        Literal = literal;
        Expression = expression;
    }

    /// <summary>字面量片段（与 <see cref="Expression"/> 互斥）。</summary>
    public string? Literal { get; }

    /// <summary>子表达式的语法树（与 <see cref="Literal"/> 互斥）。</summary>
    public Expression? Expression { get; }

    /// <summary>创建字面量片段。</summary>
    public static StringTemplatePart FromLiteral(string literal) => new(literal, null);

    /// <summary>创建子表达式片段。</summary>
    public static StringTemplatePart FromExpression(Expression expression) => new(null, expression);

    /// <summary>返回片段文本。</summary>
    public override string ToString() => Literal ?? Expression?.Source ?? Expression?.Name ?? string.Empty;
}

/// <summary>词法记号。</summary>
public readonly struct Token
{
    /// <summary>创建记号。</summary>
    public Token(
        TokenType type,
        int start,
        int length,
        string? text = null,
        double number = 0d,
        DateTime date = default,
        IReadOnlyList<StringTemplatePart>? parts = null)
    {
        Type = type;
        Start = start;
        Length = length;
        Text = text;
        Number = number;
        Date = date;
        Parts = parts;
    }

    /// <summary>记号类型。</summary>
    public TokenType Type { get; }

    /// <summary>在公式文本中的起始下标。</summary>
    public int Start { get; }

    /// <summary>在公式文本中的长度。</summary>
    public int Length { get; }

    /// <summary>标识符 / 字符串字面量文本。</summary>
    public string? Text { get; }

    /// <summary>数值字面量的值。</summary>
    public double Number { get; }

    /// <summary>日期字面量的值。</summary>
    public DateTime Date { get; }

    /// <summary>插值字符串片段（仅字符串字面量可能非空）。</summary>
    public IReadOnlyList<StringTemplatePart>? Parts { get; }

    /// <summary>结束下标（不含）。</summary>
    public int End => Start + Length;
}
