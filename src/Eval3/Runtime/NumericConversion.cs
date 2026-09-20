using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using SysExpr = System.Linq.Expressions.Expression;

namespace Eval3;

/// <summary>
/// 数值快路径的转换规则：把「已经算成 <see cref="double"/> 的值」还原成成员签名要求的类型，
/// 结果与 <see cref="ValueConvert.ConvertTo"/> 完全一致，但全程不装箱。
/// </summary>
/// <remarks>
/// 对数值目标类型而言，<c>Convert.ChangeType(double, T, InvariantCulture)</c> 等价于
/// <c>Convert.ToXxx(double)</c>（含银行家舍入与溢出行为），因此编译期可以直接发射后者，
/// 而不必绕道 <see cref="object"/>。日期 / 文本 / 枚举 / 可空类型一律不参与快路径。
/// </remarks>
internal static class NumericConversion
{
    private static readonly FrozenDictionary<Type, MethodInfo> DoubleToType = BuildDoubleToType();

    /// <summary>该类型能否在数值快路径中免装箱地承载或产出 <see cref="double"/>。</summary>
    public static bool IsSupported(Type type) => DoubleToType.ContainsKey(type);

    /// <summary>取「成员返回值 → <see cref="double"/>」的表达式包装。</summary>
    public static bool TryGetResultConverter(Type type, [NotNullWhen(true)] out Func<SysExpr, SysExpr>? convert)
    {
        if (!DoubleToType.ContainsKey(type))
        {
            convert = null;
            return false;
        }

        convert = type == typeof(double)
            ? static value => value
            : static value => SysExpr.Convert(value, typeof(double));
        return true;
    }

    /// <summary>取「<see cref="double"/> 实参 → 形参类型」的表达式包装。</summary>
    public static bool TryGetParameterConverter(Type type, [NotNullWhen(true)] out Func<SysExpr, SysExpr>? convert)
    {
        if (!DoubleToType.TryGetValue(type, out MethodInfo? method))
        {
            convert = null;
            return false;
        }

        convert = type == typeof(double)
            ? static value => value
            : value => SysExpr.Call(method, value);
        return true;
    }

    private static FrozenDictionary<Type, MethodInfo> BuildDoubleToType()
    {
        (Type Type, string Name)[] entries =
        [
            (typeof(double), nameof(Convert.ToDouble)),
            (typeof(float), nameof(Convert.ToSingle)),
            (typeof(decimal), nameof(Convert.ToDecimal)),
            (typeof(int), nameof(Convert.ToInt32)),
            (typeof(uint), nameof(Convert.ToUInt32)),
            (typeof(long), nameof(Convert.ToInt64)),
            (typeof(ulong), nameof(Convert.ToUInt64)),
            (typeof(short), nameof(Convert.ToInt16)),
            (typeof(ushort), nameof(Convert.ToUInt16)),
            (typeof(byte), nameof(Convert.ToByte)),
            (typeof(sbyte), nameof(Convert.ToSByte)),
        ];

        var table = new Dictionary<Type, MethodInfo>(entries.Length);
        foreach ((Type type, string name) in entries)
        {
            MethodInfo? method = typeof(Convert).GetMethod(name, [typeof(double)]);
            if (method is not null)
            {
                table[type] = method;
            }
        }

        return table.ToFrozenDictionary();
    }
}
