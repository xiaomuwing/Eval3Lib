using System.ComponentModel;
using System.Globalization;

namespace Eval3.Functions;

/// <summary>文本函数库。</summary>
[Description("文本函数库")]
public static class TextLibrary
{
    /// <summary>转大写。</summary>
    [Description("转大写：Upper(\"abc\") = \"ABC\"")]
    public static string Upper(string value) => value.ToUpperInvariant();

    /// <summary>转小写。</summary>
    [Description("转小写：Lower(\"ABC\") = \"abc\"")]
    public static string Lower(string value) => value.ToLowerInvariant();

    /// <summary>去首尾空白。</summary>
    [Description("去掉首尾空白")]
    public static string Trim(string value) => value.Trim();

    /// <summary>字符串长度。</summary>
    [Description("字符串长度：Length(\"abc\") = 3")]
    public static double Length(string value) => value.Length;

    /// <summary>左侧取字符。</summary>
    [Description("左侧取字符：Left(\"abcdef\", 3) = \"abc\"")]
    public static string Left(string value, int count) =>
        count <= 0 ? string.Empty : value.Length <= count ? value : value[..count];

    /// <summary>右侧取字符。</summary>
    [Description("右侧取字符：Right(\"abcdef\", 3) = \"def\"")]
    public static string Right(string value, int count) =>
        count <= 0 ? string.Empty : value.Length <= count ? value : value[^count..];

    /// <summary>中间取字符。</summary>
    [Description("从 start 开始取 count 个字符（start 从 0 开始）")]
    public static string Mid(string value, int start, int count)
    {
        if (start < 0)
        {
            start = 0;
        }

        if (start >= value.Length || count <= 0)
        {
            return string.Empty;
        }

        return value.Substring(start, Math.Min(count, value.Length - start));
    }

    /// <summary>替换。</summary>
    [Description("替换子串")]
    public static string Replace(string value, string oldValue, string newValue) =>
        value.Replace(oldValue, newValue, StringComparison.Ordinal);

    /// <summary>是否包含。</summary>
    [Description("是否包含子串")]
    public static bool Contains(string value, string search) => value.Contains(search, StringComparison.Ordinal);

    /// <summary>是否以某串开头。</summary>
    [Description("是否以指定字符串开头")]
    public static bool StartsWith(string value, string search) => value.StartsWith(search, StringComparison.Ordinal);

    /// <summary>是否以某串结尾。</summary>
    [Description("是否以指定字符串结尾")]
    public static bool EndsWith(string value, string search) => value.EndsWith(search, StringComparison.Ordinal);

    /// <summary>重复拼接。</summary>
    [Description("重复拼接：Repeat(\"ab\", 3) = \"ababab\"")]
    public static string Repeat(string value, int count) => count <= 0 ? string.Empty : string.Concat(Enumerable.Repeat(value, count));

    /// <summary>格式化数值。</summary>
    [Description("按格式串格式化数值：Format(1234.5, \"#,##0.00\")")]
    public static string Format(double value, string format) => value.ToString(format, CultureInfo.InvariantCulture);

    /// <summary>拼接。</summary>
    [Description("拼接多个文本")]
    public static string Concat(params string[] values) => string.Concat(values);
}
