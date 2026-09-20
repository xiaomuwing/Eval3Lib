using System.ComponentModel;
using System.Reflection;
using System.Text;

namespace Eval3;

/// <summary>函数库成员描述，用于界面展示可用函数清单。</summary>
public sealed record FunctionDescriptor(
    string Name,
    string Signature,
    string Category,
    string Description,
    MemberTypes MemberType,
    bool IsStatic);

/// <summary>反射枚举函数库成员并生成可读清单。</summary>
public static class FunctionCatalog
{
    private const BindingFlags SearchFlags =
        BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy;

    /// <summary>描述函数库（实例或静态类 <see cref="Type"/>）。</summary>
    public static IReadOnlyList<FunctionDescriptor> Describe(object? library)
    {
        if (library is null)
        {
            return [];
        }

        Type type = library as Type ?? library.GetType();
        var descriptors = new List<FunctionDescriptor>(32);

        foreach (MemberInfo member in type.GetMembers(SearchFlags))
        {
            switch (member)
            {
                case MethodInfo { IsSpecialName: false } method when method.DeclaringType != typeof(object):
                    descriptors.Add(new FunctionDescriptor(
                        method.Name,
                        DescribeMethod(method),
                        ResolveCategory(type, method),
                        ResolveDescription(method, method.Name),
                        MemberTypes.Method,
                        method.IsStatic));
                    break;

                case PropertyInfo property when property.GetGetMethod() is { } getter:
                    descriptors.Add(new FunctionDescriptor(
                        property.Name,
                        $"{property.Name} → {Friendly(property.PropertyType)}",
                        ResolveCategory(type, property),
                        ResolveDescription(property, property.Name),
                        MemberTypes.Property,
                        getter.IsStatic));
                    break;

                case FieldInfo field:
                    descriptors.Add(new FunctionDescriptor(
                        field.Name,
                        $"{field.Name} → {Friendly(field.FieldType)}",
                        ResolveCategory(type, field),
                        ResolveDescription(field, field.Name),
                        MemberTypes.Field,
                        field.IsStatic));
                    break;
            }
        }

        descriptors.Sort(static (left, right) => string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase));
        return descriptors;
    }

    /// <summary>取函数库名称。</summary>
    public static string GetLibraryName(object? library)
    {
        if (library is null)
        {
            return string.Empty;
        }

        Type type = library as Type ?? library.GetType();
        return type.GetCustomAttribute<DescriptionAttribute>()?.Description ?? type.Name;
    }

    /// <summary>生成 “名称(参数) → 返回类型” 形式签名。</summary>
    public static string DescribeMethod(MethodInfo method)
    {
        var builder = new StringBuilder(method.Name.Length + 16);
        builder.Append(method.Name).Append('(');
        ParameterInfo[] parameters = method.GetParameters();
        for (int i = 0; i < parameters.Length; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }

            ParameterInfo parameter = parameters[i];
            if (parameter.GetCustomAttribute<ParamArrayAttribute>() is not null)
            {
                builder.Append("params ");
            }

            builder.Append(Friendly(parameter.ParameterType)).Append(' ').Append(parameter.Name);
            if (parameter.IsOptional)
            {
                builder.Append(" = ").Append(parameter.DefaultValue ?? "null");
            }
        }

        builder.Append(')');
        if (method.ReturnType != typeof(void))
        {
            builder.Append(" → ").Append(Friendly(method.ReturnType));
        }

        return builder.ToString();
    }

    private static string Friendly(Type type)
    {
        if (type.IsArray)
        {
            return Friendly(type.GetElementType()!) + "[]";
        }

        Type? nullable = Nullable.GetUnderlyingType(type);
        if (nullable is not null)
        {
            return Friendly(nullable) + "?";
        }

        return type switch
        {
            _ when type == typeof(double) => "double",
            _ when type == typeof(int) => "int",
            _ when type == typeof(bool) => "bool",
            _ when type == typeof(string) => "string",
            _ when type == typeof(DateTime) => "date",
            _ when type == typeof(object) => "object",
            _ => type.Name,
        };
    }

    private static string ResolveCategory(Type libraryType, MemberInfo? member)
    {
        string? category = member?.GetCustomAttribute<CategoryAttribute>()?.Category;
        if (!string.IsNullOrEmpty(category))
        {
            return category;
        }

        return libraryType.GetCustomAttribute<DescriptionAttribute>()?.Description ?? libraryType.Name;
    }

    private static string ResolveDescription(MemberInfo member, string fallback) =>
        member.GetCustomAttribute<DescriptionAttribute>()?.Description ?? fallback;
}
