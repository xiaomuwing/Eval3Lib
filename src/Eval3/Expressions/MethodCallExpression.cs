using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Eval3.Compilation;
using SysExpr = System.Linq.Expressions.Expression;

namespace Eval3;

/// <summary>
/// 成员调用节点（方法 / 属性 / 索引器 / 字段）。
/// 调用委托与参数类型转换均在解析期编译完成，求值期只做一次数组租借与委托调用。
/// </summary>
public sealed class MethodCallExpression : Expression
{
    private static readonly object?[] NoArguments = [];

    private readonly Expression? _targetExpression;
    private readonly object? _targetObject;
    private readonly Expression[] _arguments;
    private readonly Func<object?, object?[], object?> _invoker;
    private readonly Type _resultType;
    private readonly EvalType _resultEvalType;

    private MethodCallExpression(
        Expression? targetExpression,
        object? targetObject,
        MemberInfo member,
        Expression[] arguments,
        Func<object?, object?[], object?> invoker,
        Type resultType,
        EvalType resultEvalType,
        string source)
        : base(source)
    {
        _targetExpression = targetExpression;
        _targetObject = targetObject;
        Member = member;
        _arguments = arguments;
        _invoker = invoker;
        _resultType = resultType;
        _resultEvalType = resultEvalType;
    }

    /// <summary>被调用的成员。</summary>
    public MemberInfo Member { get; }

    /// <summary>调用目标的表达式（实例成员链式访问时非空）。</summary>
    internal Expression? TargetExpression => _targetExpression;

    /// <summary>调用目标的常量对象（静态库为 <see cref="Type"/>，实例库为实例）。</summary>
    internal object? TargetObject => _targetObject;

    /// <summary>参数表达式。</summary>
    public IReadOnlyList<Expression> Arguments => _arguments;

    /// <inheritdoc />
    public override EvalType EvalType => _resultEvalType;

    /// <inheritdoc />
    public override Type SystemType => _resultType;

    /// <inheritdoc />
    public override object? Evaluate()
    {
        object? target = _targetExpression is null ? _targetObject : Unwrap(_targetExpression.Evaluate());
        if (_arguments.Length == 0)
        {
            return Unwrap(_invoker(target, NoArguments));
        }

        object?[] buffer = ArrayPool<object?>.Shared.Rent(_arguments.Length);
        try
        {
            for (int i = 0; i < _arguments.Length; i++)
            {
                buffer[i] = _arguments[i].Evaluate();
            }

            return Unwrap(_invoker(target, buffer));
        }
        finally
        {
            ArrayPool<object?>.Shared.Return(buffer, clearArray: true);
        }
    }

    /// <inheritdoc />
    public override IReadOnlyList<Expression> Children =>
        _targetExpression is null ? _arguments : [_targetExpression, .. _arguments];

    /// <inheritdoc />
    public override string Name => Member.Name;

    /// <inheritdoc />
    public override string Description => $"{Member.DeclaringType?.Name}.{Member.Name}（{Member.MemberType}）";

    /// <summary>创建调用节点，必要时补一层类型规范化。</summary>
    internal static Expression Create(
        object? target,
        MemberInfo member,
        IReadOnlyList<Expression>? arguments,
        string source)
    {
        Expression[] args = arguments is null ? [] : [.. arguments];
        Type resultType = MemberMetadata.GetResultType(member);
        EvalType resultEvalType = TypeTable.GetEvalType(resultType);
        Expression? targetExpression = target as Expression;
        var node = new MethodCallExpression(
            targetExpression,
            targetExpression is null ? target : null,
            member,
            args,
            MethodInvoker.Get(member, args.Length),
            resultType,
            resultEvalType,
            source);

        if (resultEvalType != EvalType.Object
            && resultEvalType != EvalType.Unknown
            && resultType != typeof(decimal)          // decimal 是 Number 的合法表示，不能规范化成 double
            && resultType != TypeTable.GetSystemType(resultEvalType))
        {
            return new ConvertExpression(node, resultEvalType, source);
        }

        return node;
    }

    private static object? Unwrap(object? value) => value is IValue wrapped ? wrapped.Value : value;

    private static readonly MethodInfo RequireNumericMethod = typeof(MethodCallExpression)
        .GetMethod(nameof(RequireNumeric), BindingFlags.NonPublic | BindingFlags.Static)!;

    /// <summary>读取变量实参并转成数值；为 <see langword="null"/> 时保持原有的转换异常行为。</summary>
    private static double RequireNumeric(ITypedValue variable) =>
        (double)ValueConvert.ConvertTo(variable.Value, typeof(double))!;

    /// <summary>
    /// 把「成员调用」整棵子树编译为单个 <c>Func&lt;double&gt;</c>：
    /// 实参走各自的数值委托，形参与返回值按 <see cref="NumericConversion"/> 的规则转换，
    /// 全程不经过 <see cref="object"/> 数组与装箱。无法保证语义一致的签名一律返回 <see langword="false"/>。
    /// </summary>
    internal override bool TryBuildNumeric(out Func<double> evaluate)
    {
        evaluate = null!;
        try
        {
            if (!TryEmitNumericCore(emitChild: null, emitVariables: false, out SysExpr? body))
            {
                return false;
            }

            evaluate = ExpressionTreeCompiler.Compile(SysExpr.Lambda<Func<double>>(body!));
            return true;
        }
#pragma warning disable CA1031
        catch (Exception)
#pragma warning restore CA1031
        {
            evaluate = null!;
            return false;
        }
    }

    /// <summary>
    /// 把成员调用发射成 <see cref="SysExpr"/>（供整树数值编译内联使用）。
    /// <paramref name="emitChild"/> 非空时，实参直接内联进调用表达式；
    /// 为空时保持旧行为（变量走 <c>RequireNumeric</c>，其余走已编译的数值委托）。
    /// </summary>
    internal bool TryEmitNumeric(Func<Eval3.Expression, SysExpr?>? emitChild, bool emitVariables, out SysExpr? body)
    {
        body = null;
        try
        {
            return TryEmitNumericCore(emitChild, emitVariables, out body);
        }
#pragma warning disable CA1031
        catch (Exception)
#pragma warning restore CA1031
        {
            body = null;
            return false;
        }
    }

    private bool TryEmitNumericCore(Func<Eval3.Expression, SysExpr?>? emitChild, bool emitVariables, out SysExpr? body)
    {
        body = null;

        // 调用目标本身是表达式（如 obj.Sub.Value）时不在本节点编译，退回逐节点求值。
        if (_targetExpression is not null
            || !NumericConversion.TryGetResultConverter(_resultType, out Func<SysExpr, SysExpr>? convertResult))
        {
            return false;
        }

        ParameterInfo[] parameters = MemberMetadata.GetParameters(Member);
        bool paramsArray = MemberMetadata.HasParamsArray(parameters);
        Type? paramsElementType = paramsArray
            ? parameters[^1].ParameterType.GetElementType() ?? typeof(object)
            : null;

        // 形参类型必须全部支持免装箱转换，否则签名语义无法保证一致。
        int fixedCount = paramsArray ? parameters.Length - 1 : parameters.Length;
        for (int i = 0; i < fixedCount; i++)
        {
            if (!NumericConversion.IsSupported(parameters[i].ParameterType))
            {
                return false;
            }
        }

        if (paramsArray && !NumericConversion.IsSupported(paramsElementType!))
        {
            return false;
        }

        int provided = Math.Min(_arguments.Length, fixedCount);
        var callArguments = new List<SysExpr>(parameters.Length);
        for (int i = 0; i < provided; i++)
        {
            if (!TryBuildArgument(_arguments[i], parameters[i].ParameterType, emitChild, emitVariables, out SysExpr? argument))
            {
                return false;
            }

            callArguments.Add(argument);
        }

        if (paramsArray)
        {
            var elements = new List<SysExpr>(Math.Max(0, _arguments.Length - fixedCount));
            for (int i = fixedCount; i < _arguments.Length; i++)
            {
                if (!TryBuildArgument(_arguments[i], paramsElementType!, emitChild, emitVariables, out SysExpr? element))
                {
                    return false;
                }

                elements.Add(element);
            }

            callArguments.Add(SysExpr.NewArrayInit(paramsElementType!, elements));
        }
        else
        {
            // 与反映射调用保持一致：缺省实参用默认值补齐。
            for (int i = provided; i < parameters.Length; i++)
            {
                ParameterInfo parameter = parameters[i];
                callArguments.Add(MemberMetadata.TryGetDefault(parameter, out object? defaultValue)
                    ? SysExpr.Constant(defaultValue, parameter.ParameterType)
                    : SysExpr.Default(parameter.ParameterType));
            }
        }

        SysExpr? call = Member switch
        {
            MethodInfo { IsStatic: true } method => SysExpr.Call(method, callArguments),
            MethodInfo method => SysExpr.Call(Target(method.DeclaringType!), method, callArguments),
            PropertyInfo property => BuildPropertyCall(property, callArguments),
            FieldInfo { IsStatic: true } field => SysExpr.Field(null, field),
            FieldInfo field => SysExpr.Field(Target(field.DeclaringType!), field),
            _ => null,
        };

        if (call is null)
        {
            return false;
        }

        body = convertResult(call);
        return true;
    }

    /// <summary>把单个实参编译为「形参类型」的表达式；失败表示该签名不参与快路径。</summary>
    private static bool TryBuildArgument(
        Eval3.Expression argument,
        Type parameterType,
        Func<Eval3.Expression, SysExpr?>? emitChild,
        bool emitVariables,
        [NotNullWhen(true)] out SysExpr? value)
    {
        value = null;

        // 「先转成 double 再换算」只有在下列情形下才与 ConvertTo(实参, 形参类型) 完全等价：
        // 实参本身已是 double，或形参就是 double。其余组合（如 decimal 实参 → decimal 形参）
        // 会先经过一次 double 往返而丢精度，必须留在逐节点求值路径上。
        if ((argument.SystemType != typeof(double) && parameterType != typeof(double))
            || !NumericConversion.TryGetParameterConverter(parameterType, out Func<SysExpr, SysExpr>? convert))
        {
            return false;
        }

        SysExpr? raw;
        if (emitChild is not null && (emitVariables || argument is not VariableExpression))
        {
            raw = emitChild(argument);
            if (raw is null)
            {
                return false;
            }
        }
        else if (argument is VariableExpression variable)
        {
            // 变量可能为 null：用一次性类型检查保持与反射调用一致的异常行为（不产生装箱，值本身已装箱）。
            raw = SysExpr.Call(RequireNumericMethod, SysExpr.Constant(variable.Variable, typeof(ITypedValue)));
        }
        else
        {
            if (!argument.TryBuildNumeric(out Func<double> compiled))
            {
                return false;
            }

            raw = SysExpr.Invoke(SysExpr.Constant(compiled, typeof(Func<double>)));
        }

        value = convert(raw);
        return true;
    }

    private System.Linq.Expressions.ConstantExpression Target(Type declaringType) =>
        SysExpr.Constant(_targetObject, declaringType);

    private System.Linq.Expressions.IndexExpression BuildPropertyCall(
        PropertyInfo property,
        List<SysExpr> callArguments)
    {
        MethodInfo getter = MemberMetadata.GetGetter(property)
            ?? throw new NotSupportedException($"属性 {property.Name} 没有 getter");
        return getter.IsStatic
            ? SysExpr.Property(null, property, callArguments)
            : SysExpr.Property(Target(getter.DeclaringType!), property, callArguments);
    }
}
