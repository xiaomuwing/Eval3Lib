using System.Collections.Concurrent;
using System.Reflection;
using Eval3.Compilation;
using SysExpr = System.Linq.Expressions.Expression;

namespace Eval3;

/// <summary>
/// 成员调用编译器：把反射成员编译为 <c>(target, args) =&gt; result</c> 委托，
/// 免去 <c>MethodInfo.Invoke</c> 的逐参数装箱与反射开销；
/// 参数类型转换在委托内部完成。委托由 <see cref="ExpressionTreeCompiler"/> 编译
/// （优先 FastExpressionCompiler 直接生成 IL），无法编译的签名自动回退到反射调用。
/// </summary>
internal static class MethodInvoker
{
    private static readonly ConcurrentDictionary<(MemberInfo Member, int ArgumentCount), Func<object?, object?[], object?>> Cache = new();

    /// <summary>取得（或编译）调用委托。</summary>
    public static Func<object?, object?[], object?> Get(MemberInfo member, int argumentCount) =>
        Cache.GetOrAdd((member, argumentCount), static key => Build(key.Member, key.ArgumentCount));

    private static Func<object?, object?[], object?> Build(MemberInfo member, int argumentCount)
    {
        try
        {
            return BuildCompiled(member, argumentCount);
        }
#pragma warning disable CA1031
        catch (Exception)
#pragma warning restore CA1031
        {
            return BuildReflective(member, argumentCount);
        }
    }

    private static Func<object?, object?[], object?> BuildCompiled(MemberInfo member, int argumentCount)
    {
        ParameterInfo[] parameters = MemberMetadata.GetParameters(member);
        bool paramsArray = MemberMetadata.HasParamsArray(parameters);
        var target = SysExpr.Parameter(typeof(object), "target");
        var args = SysExpr.Parameter(typeof(object[]), "args");
        var callArguments = new List<SysExpr>(parameters.Length);

        int fixedCount = paramsArray ? parameters.Length - 1 : parameters.Length;
        int provided = Math.Min(argumentCount, fixedCount);
        for (int i = 0; i < provided; i++)
        {
            callArguments.Add(ConvertArgument(
                SysExpr.ArrayIndex(args, SysExpr.Constant(i)),
                parameters[i].ParameterType));
        }

        if (paramsArray)
        {
            Type elementType = parameters[^1].ParameterType.GetElementType() ?? typeof(object);
            var elements = new List<SysExpr>(Math.Max(0, argumentCount - fixedCount));
            for (int i = fixedCount; i < argumentCount; i++)
            {
                elements.Add(ConvertArgument(SysExpr.ArrayIndex(args, SysExpr.Constant(i)), elementType));
            }

            callArguments.Add(SysExpr.NewArrayInit(elementType, elements));
        }
        else
        {
            for (int i = provided; i < parameters.Length; i++)
            {
                ParameterInfo parameter = parameters[i];
                callArguments.Add(MemberMetadata.TryGetDefault(parameter, out object? defaultValue)
                    ? SysExpr.Constant(defaultValue, parameter.ParameterType)
                    : SysExpr.Default(parameter.ParameterType));
            }
        }

        SysExpr call = member switch
        {
            MethodInfo { IsStatic: true } method => SysExpr.Call(method, callArguments),
            MethodInfo method => SysExpr.Call(SysExpr.Convert(target, method.DeclaringType!), method, callArguments),
            PropertyInfo property => BuildProperty(property, target, callArguments),
            FieldInfo { IsStatic: true } field => SysExpr.Field(null, field),
            FieldInfo field => SysExpr.Field(SysExpr.Convert(target, field.DeclaringType!), field),
            _ => throw new NotSupportedException($"{member.MemberType} 成员不受支持"),
        };

        if (member is MethodInfo { ReturnType: var returnType } && returnType == typeof(void))
        {
            call = SysExpr.Block(call, SysExpr.Constant(null, typeof(object)));
        }

        var lambda = SysExpr.Lambda<Func<object?, object?[], object?>>(
            SysExpr.Convert(call, typeof(object)), target, args);
        return ExpressionTreeCompiler.Compile(lambda);
    }

    private static System.Linq.Expressions.IndexExpression BuildProperty(
        PropertyInfo property,
        SysExpr target,
        List<SysExpr> callArguments)
    {
        MethodInfo? getter = MemberMetadata.GetGetter(property)
            ?? throw new NotSupportedException($"属性 {property.Name} 没有 getter");
        if (getter.ReturnType == typeof(void))
        {
            throw new NotSupportedException($"属性 {property.Name} 无法读取");
        }

        return getter.IsStatic
            ? SysExpr.Property(null, property, callArguments)
            : SysExpr.Property(SysExpr.Convert(target, getter.DeclaringType!), property, callArguments);
    }

    private static SysExpr ConvertArgument(SysExpr argument, Type targetType)
    {
        if (targetType == typeof(object))
        {
            return argument;
        }

        SysExpr boxed = SysExpr.Call(
            ConvertToMethod,
            argument,
            SysExpr.Constant(targetType));
        return SysExpr.Convert(boxed, targetType);
    }

    private static readonly MethodInfo ConvertToMethod =
        typeof(ValueConvert).GetMethod(nameof(ValueConvert.ConvertTo))!;

    private static Func<object?, object?[], object?> BuildReflective(MemberInfo member, int argumentCount)
    {
        ParameterInfo[] parameters = MemberMetadata.GetParameters(member);
        return (target, args) =>
        {
            object?[] converted = new object?[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                if (i < argumentCount)
                {
                    converted[i] = ValueConvert.ConvertTo(args[i], parameters[i].ParameterType);
                }
                else if (MemberMetadata.TryGetDefault(parameters[i], out object? defaultValue))
                {
                    converted[i] = defaultValue;
                }
                else
                {
                    converted[i] = parameters[i].ParameterType.IsValueType
                        ? Activator.CreateInstance(parameters[i].ParameterType)
                        : null;
                }
            }

            return member switch
            {
                MethodInfo method => method.Invoke(method.IsStatic ? null : target, converted),
                PropertyInfo property => property.GetValue(target, converted),
                FieldInfo field => field.GetValue(field.IsStatic ? null : target),
                _ => throw new NotSupportedException($"{member.MemberType} 成员不受支持"),
            };
        };
    }
}
