using System.Collections.Frozen;

namespace Eval3;

/// <summary>
/// <see cref="Type"/> 与 <see cref="EvalType"/> 之间的映射表。
/// 热路径使用 <see cref="Type.GetTypeCode(Type)"/> 分支（JIT 会编译为跳转表），冷路径使用冻结字典。
/// </summary>
internal static class TypeTable
{
    private static readonly FrozenDictionary<Type, EvalType> KnownTypes = new Dictionary<Type, EvalType>
    {
        [typeof(double)] = EvalType.Number,
        [typeof(float)] = EvalType.Number,
        [typeof(decimal)] = EvalType.Number,
        [typeof(byte)] = EvalType.Number,
        [typeof(sbyte)] = EvalType.Number,
        [typeof(short)] = EvalType.Number,
        [typeof(ushort)] = EvalType.Number,
        [typeof(int)] = EvalType.Number,
        [typeof(uint)] = EvalType.Number,
        [typeof(long)] = EvalType.Number,
        [typeof(ulong)] = EvalType.Number,
        [typeof(IntPtr)] = EvalType.Number,
        [typeof(string)] = EvalType.String,
        [typeof(char)] = EvalType.String,
        [typeof(bool)] = EvalType.Boolean,
        [typeof(DateTime)] = EvalType.Date,
        [typeof(DateTimeOffset)] = EvalType.Date,
        [typeof(TimeSpan)] = EvalType.Number,
        [typeof(object)] = EvalType.Object,
    }.ToFrozenDictionary();

    private static readonly Type[] SystemTypeOfEvalType =
    [
        typeof(object),   // Unknown
        typeof(double),   // Number
        typeof(bool),     // Boolean
        typeof(string),   // String
        typeof(DateTime), // Date
        typeof(object),   // Object
    ];

    /// <summary>.NET 类型 → 逻辑类型。未知类型一律按 <see cref="EvalType.Object"/> 处理，与历史版本保持一致。</summary>
    public static EvalType GetEvalType(Type? type)
    {
        if (type is null)
        {
            return EvalType.Unknown;
        }

        if (KnownTypes.TryGetValue(type, out EvalType known))
        {
            return known;
        }

        Type? underlying = Nullable.GetUnderlyingType(type);
        if (underlying is not null)
        {
            EvalType inner = GetEvalType(underlying);
            return inner == EvalType.Number ? EvalType.Number : EvalType.Object;
        }

        switch (Type.GetTypeCode(type))
        {
            case TypeCode.Double:
            case TypeCode.Single:
            case TypeCode.Decimal:
            case TypeCode.Byte:
            case TypeCode.SByte:
            case TypeCode.Int16:
            case TypeCode.UInt16:
            case TypeCode.Int32:
            case TypeCode.UInt32:
            case TypeCode.Int64:
            case TypeCode.UInt64:
                return EvalType.Number;
            case TypeCode.String:
            case TypeCode.Char:
                return EvalType.String;
            case TypeCode.Boolean:
                return EvalType.Boolean;
            case TypeCode.DateTime:
                return EvalType.Date;
            case TypeCode.Object:
                if (type.IsEnum)
                {
                    return EvalType.Number;
                }

                return EvalType.Object;
            default:
                return EvalType.Object;
        }
    }

    /// <summary>逻辑类型 → 规范 .NET 类型。</summary>
    public static Type GetSystemType(EvalType evalType)
    {
        int index = (int)evalType;
        return (uint)index < (uint)SystemTypeOfEvalType.Length ? SystemTypeOfEvalType[index] : typeof(object);
    }

    /// <summary>取对象的逻辑类型。</summary>
    public static EvalType GetEvalType(object? value) => value is null ? EvalType.Unknown : GetEvalType(value.GetType());
}
