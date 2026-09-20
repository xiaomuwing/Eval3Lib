using System.Reflection;

namespace Eval3;

/// <summary>成员元数据读取辅助。</summary>
internal static class MemberMetadata
{
    /// <summary>取成员的参数列表（方法参数 / 索引器参数）。</summary>
    public static ParameterInfo[] GetParameters(MemberInfo member) => member switch
    {
        MethodInfo method => method.GetParameters(),
        PropertyInfo property => property.GetIndexParameters(),
        _ => [],
    };

    /// <summary>最后一个参数是否为 <c>params</c> 数组。</summary>
    public static bool HasParamsArray(ParameterInfo[] parameters) =>
        parameters.Length > 0 && parameters[^1].GetCustomAttribute<ParamArrayAttribute>() is not null;

    /// <summary>取成员的返回 / 字段类型。</summary>
    public static Type GetResultType(MemberInfo member) => member switch
    {
        MethodInfo method => method.ReturnType,
        PropertyInfo property => property.PropertyType,
        FieldInfo field => field.FieldType,
        _ => typeof(object),
    };

    /// <summary>取可选参数默认值。</summary>
    public static bool TryGetDefault(ParameterInfo parameter, out object? defaultValue)
    {
        defaultValue = null;
        if (!parameter.IsOptional)
        {
            return false;
        }

        object? value = parameter.DefaultValue;
        if (value is null || value is DBNull || value is Missing)
        {
            return !parameter.ParameterType.IsValueType
                || Nullable.GetUnderlyingType(parameter.ParameterType) is not null;
        }

        Type type = parameter.ParameterType;
        if (type.IsEnum)
        {
            try
            {
                defaultValue = Enum.ToObject(type, value);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        if (!type.IsInstanceOfType(value))
        {
            return false;
        }

        defaultValue = value;
        return true;
    }

    /// <summary>取属性 getter（含非公开）。</summary>
    public static MethodInfo? GetGetter(PropertyInfo property) => property.GetGetMethod(nonPublic: true);
}
