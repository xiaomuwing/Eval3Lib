using System.Globalization;

namespace Eval3;

/// <summary>
/// 值转换工具。所有转换均使用不变文化（InvariantCulture），保证公式在不同区域设置下结果一致。
/// </summary>
/// <remarks>
/// 与 VB 版本的差异：历史版本使用 <c>CDbl</c>/<c>CStr</c> 等依赖当前区域设置的关键字，
/// 在中文 / 英文系统下 <c>"1,5"</c> 的解析结果会不同；此处统一为不变文化。
/// </remarks>
public static class ValueConvert
{
    /// <summary>转换为布尔值。</summary>
    public static bool ToBoolean(object? value)
    {
        if (value is null)
        {
            return false;
        }

        if (value is bool b)
        {
            return b;
        }

        if (value is string s)
        {
            if (bool.TryParse(s, out bool parsed))
            {
                return parsed;
            }

            throw new InvalidCastException($"无法将字符串 \"{s}\" 转换为布尔值");
        }

        return Convert.ToBoolean(value, CultureInfo.InvariantCulture);
    }

    /// <summary>转换为日期时间。</summary>
    public static DateTime ToDate(object? value)
    {
        if (value is null)
        {
            return default;
        }

        if (value is DateTime dt)
        {
            return dt;
        }

        if (value is DateTimeOffset dto)
        {
            return dto.DateTime;
        }

        return Convert.ToDateTime(value, CultureInfo.InvariantCulture);
    }

    /// <summary>转换为双精度数值（组件内部统一使用的数值表示）。</summary>
    public static double ToDouble(object? value)
    {
        if (value is null)
        {
            return 0d;
        }

        if (value is double d)
        {
            return d;
        }

        return value switch
        {
            int i => i,
            long l => l,
            decimal m => (double)m,
            float f => f,
            short sh => sh,
            byte by => by,
            uint ui => ui,
            ulong ul => ul,
            sbyte sb => sb,
            ushort us => us,
            bool b => b ? 1d : 0d,
            string s => ParseDouble(s),
            IConvertible c => c.ToDouble(CultureInfo.InvariantCulture),
            _ => throw new InvalidCastException($"无法将 {value.GetType().Name} 转换为数值"),
        };
    }

    /// <summary>热路径专用：值已是 <see cref="double"/> 时零分支返回。</summary>
    public static double AsDouble(object? value) => value is double d ? d : ToDouble(value);

    /// <summary>转换为字符串（原始 ToString，用于 <c>&amp;</c> 连接运算）。</summary>
    public static string ToText(object? value) =>
        value is null ? string.Empty : value as string ?? Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;

    private static double ParseDouble(string text) =>
        double.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double result)
            ? result
            : throw new InvalidCastException($"无法将字符串 \"{text}\" 转换为数值");

    /// <summary>
    /// 展示用格式化（等价于 VB 版本的 <c>Evaluator.ConvertToString</c>）：
    /// 日期带时间则输出完整时间，否则只输出日期；数值按含千分位的格式输出。
    /// </summary>
    public static string ConvertToString(object? value)
    {
        switch (value)
        {
            case null:
                return string.Empty;
            case string s:
                return s;
            case bool b:
                return b ? "True" : "False";
            case DateTime date:
                return date.TimeOfDay.TotalMilliseconds > 0
                    ? date.ToString(CultureInfo.InvariantCulture)
                    : date.ToShortDateString();
            case decimal dec:
                return FormatNumber((double)dec);
            case double dbl:
                return FormatNumber(dbl);
            case float flo:
                return FormatNumber(flo);
            case IConvertible convertible when value is not bool:
                return FormatNumber(convertible.ToDouble(CultureInfo.InvariantCulture));
            default:
                return value.ToString() ?? string.Empty;
        }
    }

    private static string FormatNumber(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        return value % 1 != 0
            ? value.ToString("#,##0.00", CultureInfo.InvariantCulture)
            : value.ToString("#,##0", CultureInfo.InvariantCulture);
    }

    /// <summary>按目标参数类型转换（供反射调用使用），类型已匹配时零开销返回。</summary>
    public static object? ConvertTo(object? value, Type targetType)
    {
        if (targetType == typeof(object))
        {
            return value;
        }

        if (value is null)
        {
            if (!targetType.IsValueType || Nullable.GetUnderlyingType(targetType) is not null)
            {
                return null;
            }

            throw new InvalidCastException($"无法将 null 转换为 {targetType.Name}");
        }

        if (targetType.IsInstanceOfType(value))
        {
            return value;
        }

        Type? nullable = Nullable.GetUnderlyingType(targetType);
        if (nullable is not null)
        {
            return Convert.ChangeType(value, nullable, CultureInfo.InvariantCulture);
        }

        if (targetType.IsEnum)
        {
            return value is string enumText
                ? Enum.Parse(targetType, enumText, ignoreCase: true)
                : Enum.ToObject(targetType, Convert.ChangeType(value, Enum.GetUnderlyingType(targetType), CultureInfo.InvariantCulture));
        }

        if (value is string text && targetType == typeof(DateTime))
        {
            return DateTime.Parse(text, CultureInfo.InvariantCulture);
        }

        return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
    }
}
