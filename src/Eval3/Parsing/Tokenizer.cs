using System.Globalization;
using System.Text;

namespace Eval3;

/// <summary>
/// 词法分析器：单遍扫描公式文本，直接把数值 / 日期字面量解析为强类型值，
/// 不再像 VB 版本那样逐字符拼装 <see cref="StringBuilder"/> 后再统一转换。
/// </summary>
internal sealed class Tokenizer
{
    private readonly Parser _parser;
    private readonly string _text;
    private readonly int _length;
    private int _pos;

    public Tokenizer(Parser parser, string text)
    {
        _parser = parser;
        _text = text;
        _length = text.Length;
    }

    /// <summary>公式原文。</summary>
    public string Text => _text;

    /// <summary>当前扫描位置。</summary>
    public int Position => _pos;

    /// <summary>读取下一个记号。</summary>
    public Token NextToken()
    {
        while (true)
        {
            int start = _pos;
            if (start >= _length)
            {
                return new Token(TokenType.EndOfFormula, start, 0);
            }

            char c = _text[start];
            if (c <= ' ')
            {
                _pos = start + 1;
                continue;
            }

            switch (c)
            {
                case >= '0' and <= '9':
                    return ReadNumber(start);
                case '-':
                case '?':
                    _pos = start + 1;
                    return new Token(TokenType.OperatorMinus, start, 1);
                case '+':
                    _pos = start + 1;
                    return new Token(TokenType.OperatorPlus, start, 1);
                case '*':
                    _pos = start + 1;
                    return new Token(TokenType.OperatorMul, start, 1);
                case '/':
                    _pos = start + 1;
                    return new Token(TokenType.OperatorDiv, start, 1);
                case '%':
                    _pos = start + 1;
                    return new Token(TokenType.OperatorPercent, start, 1);
                case '(':
                    _pos = start + 1;
                    return new Token(TokenType.OpenParenthesis, start, 1);
                case ')':
                    _pos = start + 1;
                    return new Token(TokenType.CloseParenthesis, start, 1);
                case '<':
                    return ReadLessThan(start);
                case '>':
                    return ReadGreaterThan(start);
                case ',':
                    _pos = start + 1;
                    return new Token(TokenType.Comma, start, 1);
                case '=':
                    _pos = start + 1;
                    return new Token(TokenType.OperatorEq, start, 1);
                case '.':
                    if (start + 1 < _length && IsDigit(_text[start + 1]))
                    {
                        return ReadNumber(start);
                    }

                    _pos = start + 1;
                    return new Token(TokenType.Dot, start, 1);
                case '\'':
                case '"':
                case '\u2018':
                case '\u2019':
                case '\u201C':
                case '\u201D':
                    return ReadString(start, NormalizeQuote(c));
                case '#':
                    return ReadDate(start);
                case '&':
                    _pos = start + 1;
                    return new Token(TokenType.OperatorConcat, start, 1);
                case '[':
                    _pos = start + 1;
                    return new Token(TokenType.OpenBracket, start, 1);
                case ']':
                    _pos = start + 1;
                    return new Token(TokenType.CloseBracket, start, 1);
                default:
                    if (IsIdentifierStart(c))
                    {
                        return ReadIdentifier(start);
                    }

                    throw _parser.Error($"无法识别的字符 '{c}'（U+{(int)c:X4}）", start);
            }
        }
    }

    private static bool IsDigit(char c) => c is >= '0' and <= '9';

    private static bool IsIdentifierStart(char c) => char.IsLetter(c) || c == '_' || c > 127;

    private static bool IsIdentifierPart(char c) =>
        char.IsLetterOrDigit(c) || c == '_' || c > 127;

    private static char NormalizeQuote(char c) => c switch
    {
        '\u2018' or '\u2019' => '\'',
        '\u201C' or '\u201D' => '"',
        _ => c,
    };

    private static bool IsSameQuote(char c, char quote) => c == quote || NormalizeQuote(c) == quote;

    private Token ReadLessThan(int start)
    {
        _pos = start + 1;
        if (_pos < _length)
        {
            if (_text[_pos] == '=')
            {
                _pos++;
                return new Token(TokenType.OperatorLe, start, 2);
            }

            if (_text[_pos] == '>')
            {
                _pos++;
                return new Token(TokenType.OperatorNe, start, 2);
            }
        }

        return new Token(TokenType.OperatorLt, start, 1);
    }

    private Token ReadGreaterThan(int start)
    {
        _pos = start + 1;
        if (_pos < _length && _text[_pos] == '=')
        {
            _pos++;
            return new Token(TokenType.OperatorGe, start, 2);
        }

        return new Token(TokenType.OperatorGt, start, 1);
    }

    /// <summary>数值字面量：<c>12</c>、<c>12.5</c>、<c>.5</c>、<c>1.2e-3</c>。</summary>
    private Token ReadNumber(int start)
    {
        int pos = start;
        while (pos < _length && IsDigit(_text[pos]))
        {
            pos++;
        }

        if (pos < _length && _text[pos] == '.')
        {
            pos++;
            while (pos < _length && IsDigit(_text[pos]))
            {
                pos++;
            }
        }

        if (pos < _length && (_text[pos] == 'e' || _text[pos] == 'E'))
        {
            int exponent = pos + 1;
            if (exponent < _length && (_text[exponent] == '+' || _text[exponent] == '-'))
            {
                exponent++;
            }

            if (exponent < _length && IsDigit(_text[exponent]))
            {
                pos = exponent;
                while (pos < _length && IsDigit(_text[pos]))
                {
                    pos++;
                }
            }
        }

        ReadOnlySpan<char> span = _text.AsSpan(start, pos - start);
        if (!double.TryParse(span, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
        {
            throw _parser.Error($"无效的数字 {new string(span)}", start, pos - start);
        }

        _pos = pos;
        return new Token(TokenType.ValueNumber, start, pos - start, number: value);
    }

    /// <summary>标识符与关键字（关键字不区分大小写）。</summary>
    private Token ReadIdentifier(int start)
    {
        int pos = start + 1;
        while (pos < _length && IsIdentifierPart(_text[pos]))
        {
            pos++;
        }

        ReadOnlySpan<char> span = _text.AsSpan(start, pos - start);
        TokenType type = span switch
        {
            _ when span.Equals("and", StringComparison.OrdinalIgnoreCase) => TokenType.OperatorAnd,
            _ when span.Equals("or", StringComparison.OrdinalIgnoreCase) => TokenType.OperatorOr,
            _ when span.Equals("not", StringComparison.OrdinalIgnoreCase) => TokenType.OperatorNot,
            _ when span.Equals("true", StringComparison.OrdinalIgnoreCase) => TokenType.ValueTrue,
            _ when span.Equals("yes", StringComparison.OrdinalIgnoreCase) => TokenType.ValueTrue,
            _ when span.Equals("false", StringComparison.OrdinalIgnoreCase) => TokenType.ValueFalse,
            _ when span.Equals("no", StringComparison.OrdinalIgnoreCase) => TokenType.ValueFalse,
            _ when span.Equals("if", StringComparison.OrdinalIgnoreCase) => TokenType.OperatorIf,
            _ => TokenType.ValueIdentifier,
        };

        _pos = pos;
        string text = _text.Substring(start, pos - start);
        return new Token(type, start, pos - start, text);
    }

    /// <summary>字符串字面量，支持 <c>""</c> 转义与 <c>%[子表达式]</c> 插值。</summary>
    private Token ReadString(int start, char quote)
    {
        int pos = start + 1;
        int segmentStart = pos;
        StringBuilder? builder = null;
        List<StringTemplatePart>? parts = null;

        while (pos < _length)
        {
            char c = _text[pos];
            if (IsSameQuote(c, quote))
            {
                char next = pos + 1 < _length ? _text[pos + 1] : '\0';
                if (IsSameQuote(next, quote))
                {
                    builder ??= new StringBuilder();
                    builder.Append(_text, segmentStart, pos - segmentStart).Append(quote);
                    pos += 2;
                    segmentStart = pos;
                    continue;
                }

                string literal = builder is null
                    ? _text.Substring(segmentStart, pos - segmentStart)
                    : builder.Append(_text, segmentStart, pos - segmentStart).ToString();
                pos++;
                _pos = pos;

                if (parts is null)
                {
                    return new Token(TokenType.ValueString, start, pos - start, text: literal);
                }

                if (literal.Length > 0)
                {
                    parts.Add(StringTemplatePart.FromLiteral(literal));
                }

                return new Token(TokenType.ValueString, start, pos - start, parts: parts);
            }

            if (c == '%' && pos + 1 < _length && _text[pos + 1] == '[')
            {
                string literal = builder is null
                    ? _text.Substring(segmentStart, pos - segmentStart)
                    : builder.Append(_text, segmentStart, pos - segmentStart).ToString();
                builder = null;
                parts ??= [];
                if (literal.Length > 0)
                {
                    parts.Add(StringTemplatePart.FromLiteral(literal));
                }

                int close = FindClosingBracket(pos + 1);
                if (close < 0)
                {
                    throw _parser.Error("字符串中的 %[ 缺少匹配的 ]", pos);
                }

                int innerStart = pos + 2;
                string sub = _text.Substring(innerStart, close - innerStart);
                parts.Add(StringTemplatePart.FromExpression(_parser.ParseSubExpression(sub, innerStart)));
                pos = close + 1;
                segmentStart = pos;
                continue;
            }

            pos++;
        }

        throw _parser.Error($"字符串缺少结尾的引号 {quote}", start, Math.Max(1, _length - start));
    }

    /// <summary>查找与 <paramref name="openIndex"/> 处 <c>[</c> 匹配的 <c>]</c>，跳过字符串字面量。</summary>
    private int FindClosingBracket(int openIndex)
    {
        int depth = 0;
        for (int i = openIndex; i < _length; i++)
        {
            char c = _text[i];
            switch (c)
            {
                case '[':
                    depth++;
                    break;
                case ']':
                    depth--;
                    if (depth == 0)
                    {
                        return i;
                    }

                    break;
                case '\'':
                case '"':
                    int quoteEnd = i + 1;
                    while (quoteEnd < _length && _text[quoteEnd] != c)
                    {
                        quoteEnd++;
                    }

                    i = quoteEnd;
                    break;
            }
        }

        return -1;
    }

    private static readonly string[] DateFormats =
    [
        "yyyy/M/d H:m:s", "yyyy/M/d H:m", "yyyy/M/d",
        "yyyy-M-d H:m:s", "yyyy-M-d", "yyyy.M.d",
        "yyyyMMdd", "d/M/yyyy", "M/d/yyyy",
    ];

    /// <summary>日期字面量：<c>#2024/01/31#</c>。</summary>
    private Token ReadDate(int start)
    {
        int close = _text.IndexOf('#', start + 1);
        if (close < 0)
        {
            throw _parser.Error("日期常量缺少结尾的 #（应形如 #2024/01/31#）", start);
        }

        ReadOnlySpan<char> text = _text.AsSpan(start + 1, close - start - 1).Trim();
        if (!TryParseDate(text, out DateTime date))
        {
            throw _parser.Error($"无效的日期 {new string(text)}，应形如 #2024/01/31#", start, close - start + 1);
        }

        _pos = close + 1;
        return new Token(TokenType.ValueDate, start, close - start + 1, date: date);
    }

    private static bool TryParseDate(ReadOnlySpan<char> text, out DateTime date)
    {
        if (DateTime.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.None, out date))
        {
            return true;
        }

        if (DateTime.TryParseExact(text, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
        {
            return true;
        }

        return DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }
}
