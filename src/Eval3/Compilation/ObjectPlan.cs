using System.Reflection;
using Eval3.Compilation;
using SysExpr = System.Linq.Expressions.Expression;

namespace Eval3;

/// <summary>
/// 整树对象编译器（Phase 2）：把整棵表达式树发射成单个 <c>Func&lt;object?&gt;</c>。
/// 内部全部使用强类型局部变量（double / bool / string / DateTime），只在根节点装箱一次，
/// 消除逐节点 <c>object?</c> 往返与虚调用。
/// </summary>
internal static class ObjectPlan
{
    private static readonly MethodInfo AsDoubleMethod =
        typeof(ValueConvert).GetMethod(nameof(ValueConvert.AsDouble))!;

    private static readonly MethodInfo ToTextMethod =
        typeof(ValueConvert).GetMethod(nameof(ValueConvert.ToText))!;

    private static readonly MethodInfo ToBooleanMethod =
        typeof(ValueConvert).GetMethod(nameof(ValueConvert.ToBoolean))!;

    private static readonly MethodInfo ToDateMethod =
        typeof(ValueConvert).GetMethod(nameof(ValueConvert.ToDate))!;

    private static readonly MethodInfo ConvertToStringMethod =
        typeof(ValueConvert).GetMethod(nameof(ValueConvert.ConvertToString))!;

    private static readonly MethodInfo ConvertToMethod =
        typeof(ValueConvert).GetMethod(nameof(ValueConvert.ConvertTo))!;

    private static readonly MethodInfo GetValueIntMethod =
        typeof(Array).GetMethod(nameof(Array.GetValue), [typeof(int)])!;

    private static readonly MethodInfo GetValueIntsMethod =
        typeof(Array).GetMethod(nameof(Array.GetValue), [typeof(int[])])!;

    private static readonly MethodInfo Concat2Method =
        typeof(string).GetMethod(nameof(string.Concat), [typeof(string), typeof(string)])!;

    private static readonly MethodInfo ConcatArrayMethod =
        typeof(string).GetMethod(nameof(string.Concat), [typeof(string[])])!;

    private static readonly MethodInfo Concat3Method =
        typeof(string).GetMethod(nameof(string.Concat), [typeof(string), typeof(string), typeof(string)])!;

    private static readonly MethodInfo DateTimeAddDaysMethod =
        typeof(DateTime).GetMethod(nameof(DateTime.AddDays), [typeof(double)])!;

    private static readonly MethodInfo Concat4Method =
        typeof(string).GetMethod(nameof(string.Concat), [typeof(string), typeof(string), typeof(string), typeof(string)])!;

    /// <summary>尝试把整棵树编译为对象委托（不编译 IL，仅发射表达式树）。</summary>
    /// <summary>把任意子树发射成强类型表达式（供其它计划复用，如数值计划里的日期差）。</summary>
    internal static bool TryEmitValue(Expression node, out SysExpr? value, out Type type) =>
        TryEmit(node, out value, out type);

    public static bool TryBuild(Expression root, out ObjectProgram? program)
    {
        program = null;
        if (!TryEmit(root, out SysExpr? value, out Type type))
        {
            return false;
        }

        program = new ObjectProgram(Box(value!, type));
        return true;
    }

    private static bool TryEmit(Expression node, out SysExpr? value, out Type type)
    {
        value = null;
        type = typeof(object);

        switch (node)
        {
            case ConstantExpression constant:
                type = constant.Value?.GetType() ?? typeof(object);
                value = SysExpr.Constant(constant.Value, type);
                return true;

            case VariableExpression variable:
                // 变量按“原始 object 值”发射：decimal 精度、null 语义、日期 null 等
                // 与树遍历完全一致；数值子式会在使用点转 double（不改变精度语义）。
                value = ReadValue(variable.Variable);
                type = typeof(object);
                return true;

            case NegateExpression negate:
                if (!TryEmit(negate.Operand, out SysExpr? operand, out Type operandType))
                {
                    return false;
                }

                value = SysExpr.Negate(Number(operand!, operandType));
                type = typeof(double);
                return true;

            case NotExpression not:
                if (!TryEmit(not.Operand, out SysExpr? notOperand, out Type notType))
                {
                    return false;
                }

                value = SysExpr.Not(Boolean(notOperand!, notType));
                type = typeof(bool);
                return true;

            case PercentExpression percent:
                return TryEmitPercent(percent, out value, out type);

            case ConvertExpression convert:
            {
                if (!TryEmit(convert.SourceExpression, out SysExpr? inner, out Type innerType))
                {
                    return false;
                }

                switch (convert.EvalType)
                {
                    case EvalType.Number:
                        value = Number(inner!, innerType);
                        type = typeof(double);
                        break;
                    case EvalType.String:
                        value = Text(inner!, innerType);
                        type = typeof(string);
                        break;
                    case EvalType.Boolean:
                        value = Boolean(inner!, innerType);
                        type = typeof(bool);
                        break;
                    case EvalType.Date:
                        value = SysExpr.Call(ToDateMethod, Box(inner!, innerType));
                        type = typeof(DateTime);
                        break;
                    default:
                        value = Box(inner!, innerType);
                        type = typeof(object);
                        break;
                }

                return true;
            }

            case BinaryExpression binary:
                return TryEmitBinary(binary, out value, out type);

            case LogicalExpression logical:
            {
                if (!TryEmit(logical.Left, out SysExpr? left, out Type leftType)
                    || !TryEmit(logical.Right, out SysExpr? right, out Type rightType))
                {
                    return false;
                }

                SysExpr leftBool = Boolean(left!, leftType);
                SysExpr rightBool = Boolean(right!, rightType);
                value = logical.IsAnd
                    ? SysExpr.AndAlso(leftBool, rightBool)
                    : SysExpr.OrElse(leftBool, rightBool);
                type = typeof(bool);
                return true;
            }

            case ConditionalExpression conditional:
                return TryEmitConditional(conditional, out value, out type);

            case MethodCallExpression call:
                return TryEmitCall(call, out value, out type);

            case ArrayIndexExpression index:
                return TryEmitArrayIndex(index, out value, out type);

            case StringTemplateExpression template:
                return TryEmitTemplate(template, out value, out type);
        }

        return false;
    }

    private static bool TryEmitPercent(PercentExpression percent, out SysExpr? value, out Type type)
    {
        value = null;
        type = typeof(double);

        if (!TryEmit(percent.Operand, out SysExpr? operand, out Type operandType))
        {
            return false;
        }

        SysExpr number = Number(operand!, operandType);
        if (percent.Context is null)
        {
            value = SysExpr.Divide(number, SysExpr.Constant(100d));
            return true;
        }

        if (!TryEmit(percent.Context, out SysExpr? reference, out Type referenceType))
        {
            return false;
        }

        value = SysExpr.Divide(
            SysExpr.Multiply(Number(reference!, referenceType), number),
            SysExpr.Constant(100d));
        return true;
    }

    private static bool TryEmitBinary(BinaryExpression binary, out SysExpr? value, out Type type)
    {
        value = null;
        type = typeof(object);

        switch (binary.Operator)
        {
            case NumericOperator numericOperator:
            {
                if (!TryEmit(binary.Left, out SysExpr? left, out Type leftType)
                    || !TryEmit(binary.Right, out SysExpr? right, out Type rightType))
                {
                    return false;
                }

                SysExpr l = Number(left!, leftType);
                SysExpr r = Number(right!, rightType);
                SysExpr? node =
                    numericOperator == Operators.Add ? SysExpr.Add(l, r) :
                    numericOperator == Operators.Subtract ? SysExpr.Subtract(l, r) :
                    numericOperator == Operators.Multiply ? SysExpr.Multiply(l, r) :
                    numericOperator == Operators.Divide ? SysExpr.Divide(l, r) :
                    numericOperator == Operators.Modulo ? SysExpr.Modulo(l, r) :
                    null;

                if (node is null)
                {
                    return false;
                }

                value = node;
                type = typeof(double);
                return true;
            }

            case ConcatOperator:
            {
                if (!TryEmit(binary.Left, out SysExpr? left, out Type leftType)
                    || !TryEmit(binary.Right, out SysExpr? right, out Type rightType))
                {
                    return false;
                }

                value = SysExpr.Call(Concat2Method, Text(left!, leftType), Text(right!, rightType));
                type = typeof(string);
                return true;
            }

            case DateOperator dateOperator:
                return TryEmitDate(binary, dateOperator, out value, out type);

            case ComparisonOperator comparison:
            {
                if (!TryEmit(binary.Left, out SysExpr? left, out Type leftType)
                    || !TryEmit(binary.Right, out SysExpr? right, out Type rightType))
                {
                    return false;
                }

                SysExpr compared = SysExpr.Invoke(
                    SysExpr.Constant(comparison.Comparer, typeof(Func<object?, object?, int>)),
                    Box(left!, leftType),
                    Box(right!, rightType));

                SysExpr zero = SysExpr.Constant(0);
                value = comparison.Kind switch
                {
                    ComparisonKind.Equal => SysExpr.Equal(compared, zero),
                    ComparisonKind.NotEqual => SysExpr.NotEqual(compared, zero),
                    ComparisonKind.LessThan => SysExpr.LessThan(compared, zero),
                    ComparisonKind.LessThanOrEqual => SysExpr.LessThanOrEqual(compared, zero),
                    ComparisonKind.GreaterThan => SysExpr.GreaterThan(compared, zero),
                    _ => SysExpr.GreaterThanOrEqual(compared, zero),
                };
                type = typeof(bool);
                return true;
            }
        }

        // 日期运算（日期 ± 天数 / 日期差）暂不进入整树路径，交由树遍历。
        return false;
    }

    private static bool TryEmitDate(
        BinaryExpression binary,
        DateOperator dateOperator,
        out SysExpr? value,
        out Type type)
    {
        value = null;
        type = typeof(object);

        if (!TryEmit(binary.Left, out SysExpr? left, out Type leftType)
            || !TryEmit(binary.Right, out SysExpr? right, out Type rightType))
        {
            return false;
        }

        SysExpr leftDate = DateValue(left!, leftType);
        SysExpr rightNumber = Number(right!, rightType);

        switch (dateOperator.Kind)
        {
            case DateOperatorKind.AddDays:
                value = SysExpr.Call(leftDate, DateTimeAddDaysMethod, rightNumber);
                type = typeof(DateTime);
                return true;

            case DateOperatorKind.SubtractDays:
                value = SysExpr.Call(leftDate, DateTimeAddDaysMethod, SysExpr.Negate(rightNumber));
                type = typeof(DateTime);
                return true;

            default:
                value = SysExpr.Property(
                    SysExpr.Subtract(leftDate, DateValue(right!, rightType)),
                    nameof(TimeSpan.TotalDays));
                type = typeof(double);
                return true;
        }
    }

    private static SysExpr DateValue(SysExpr value, Type type) =>
        type == typeof(DateTime) ? value : SysExpr.Call(ToDateMethod, Box(value, type));

    private static bool TryEmitConditional(ConditionalExpression conditional, out SysExpr? value, out Type type)
    {
        value = null;
        type = typeof(object);

        if (!TryEmit(conditional.Condition, out SysExpr? condition, out Type conditionType)
            || !TryEmit(conditional.WhenTrue, out SysExpr? whenTrue, out Type trueType)
            || !TryEmit(conditional.WhenFalse, out SysExpr? whenFalse, out Type falseType))
        {
            return false;
        }

        SysExpr test = Boolean(condition!, conditionType);
        if (trueType == falseType)
        {
            value = SysExpr.Condition(test, whenTrue!, whenFalse!);
            type = trueType;
            return true;
        }

        value = SysExpr.Condition(test, Box(whenTrue!, trueType), Box(whenFalse!, falseType));
        type = typeof(object);
        return true;
    }

    private static bool TryEmitCall(MethodCallExpression call, out SysExpr? value, out Type type)
    {
        value = null;
        type = typeof(object);

        MemberInfo member = call.Member;
        Type? declaring = member.DeclaringType;
        SysExpr? target = null;

        if (call.TargetExpression is not null)
        {
            if (!TryEmit(call.TargetExpression, out SysExpr? targetValue, out Type targetType) || declaring is null)
            {
                return false;
            }

            target = targetType == declaring
                ? targetValue
                : SysExpr.Convert(Box(targetValue!, targetType), declaring);
        }
        else if (!IsStatic(member) && call.TargetObject is not null && declaring is not null)
        {
            target = SysExpr.Constant(call.TargetObject, declaring);
        }

        ParameterInfo[] parameters = MemberMetadata.GetParameters(member);
        bool paramsArray = MemberMetadata.HasParamsArray(parameters);
        int fixedCount = paramsArray ? parameters.Length - 1 : parameters.Length;
        int provided = Math.Min(call.Arguments.Count, fixedCount);
        var arguments = new List<SysExpr>(parameters.Length);

        for (int i = 0; i < provided; i++)
        {
            if (!TryEmit(call.Arguments[i], out SysExpr? argument, out Type argumentType))
            {
                return false;
            }

            arguments.Add(ConvertArgument(argument!, argumentType, parameters[i].ParameterType));
        }

        if (paramsArray)
        {
            Type elementType = parameters[^1].ParameterType.GetElementType() ?? typeof(object);
            var elements = new List<SysExpr>(Math.Max(0, call.Arguments.Count - fixedCount));
            for (int i = fixedCount; i < call.Arguments.Count; i++)
            {
                if (!TryEmit(call.Arguments[i], out SysExpr? element, out Type elementTypeValue))
                {
                    return false;
                }

                elements.Add(ConvertArgument(element!, elementTypeValue, elementType));
            }

            arguments.Add(SysExpr.NewArrayInit(elementType, elements));
        }
        else
        {
            for (int i = provided; i < parameters.Length; i++)
            {
                arguments.Add(MemberMetadata.TryGetDefault(parameters[i], out object? defaultValue)
                    ? SysExpr.Constant(defaultValue, parameters[i].ParameterType)
                    : SysExpr.Default(parameters[i].ParameterType));
            }
        }

        SysExpr? callExpression = member switch
        {
            MethodInfo { IsStatic: true } method => SysExpr.Call(method, arguments),
            MethodInfo method => SysExpr.Call(target!, method, arguments),
            PropertyInfo property => BuildProperty(property, target, arguments),
            FieldInfo { IsStatic: true } field => SysExpr.Field(null, field),
            FieldInfo field => SysExpr.Field(target!, field),
            _ => null,
        };

        if (callExpression is null)
        {
            return false;
        }

        value = callExpression;
        type = MemberMetadata.GetResultType(member);
        return true;
    }

    private static SysExpr BuildProperty(PropertyInfo property, SysExpr? target, List<SysExpr> arguments)
    {
        MethodInfo getter = MemberMetadata.GetGetter(property)
            ?? throw new NotSupportedException($"属性 {property.Name} 没有 getter");
        if (getter.IsStatic)
        {
            return SysExpr.Property(null, property, arguments);
        }

        return SysExpr.Property(target!, property, arguments);
    }

    private static bool TryEmitArrayIndex(ArrayIndexExpression index, out SysExpr? value, out Type type)
    {
        value = null;
        type = typeof(object);

        if (!TryEmit(index.Array, out SysExpr? array, out Type arrayType))
        {
            return false;
        }

        SysExpr arrayExpression = arrayType == typeof(Array)
            ? array!
            : SysExpr.Convert(array!, typeof(Array));

        Type elementType = index.SystemType;
        SysExpr getValue;
        if (index.Indices.Count == 1)
        {
            if (!TryEmit(index.Indices[0], out SysExpr? indexValue, out Type indexType))
            {
                return false;
            }

            getValue = SysExpr.Call(
                arrayExpression,
                GetValueIntMethod,
                SysExpr.Convert(Number(indexValue!, indexType), typeof(int)));
        }
        else
        {
            var indexes = new List<SysExpr>(index.Indices.Count);
            foreach (Expression indexNode in index.Indices)
            {
                if (!TryEmit(indexNode, out SysExpr? indexValue, out Type indexType))
                {
                    return false;
                }

                indexes.Add(SysExpr.Convert(Number(indexValue!, indexType), typeof(int)));
            }

            getValue = SysExpr.Call(arrayExpression, GetValueIntsMethod, SysExpr.NewArrayInit(typeof(int), indexes));
        }

        value = elementType == typeof(object) ? getValue : SysExpr.Convert(getValue, elementType);
        type = elementType;
        return true;
    }

    private static bool TryEmitTemplate(StringTemplateExpression template, out SysExpr? value, out Type type)
    {
        value = null;
        type = typeof(string);

        var parts = new List<SysExpr>(template.Parts.Count);
        foreach (StringTemplatePart part in template.Parts)
        {
            if (part.Literal is { } literal)
            {
                parts.Add(SysExpr.Constant(literal));
                continue;
            }

            if (!TryEmit(part.Expression!, out SysExpr? partValue, out Type partType))
            {
                return false;
            }

            parts.Add(SysExpr.Call(ConvertToStringMethod, Box(partValue!, partType)));
        }

        value = parts.Count switch
        {
            0 => SysExpr.Constant(string.Empty),
            1 => parts[0],
            2 => SysExpr.Call(Concat2Method, parts[0], parts[1]),
            3 => SysExpr.Call(Concat3Method, parts[0], parts[1], parts[2]),
            4 => SysExpr.Call(Concat4Method, parts[0], parts[1], parts[2], parts[3]),
            _ => SysExpr.Call(ConcatArrayMethod, SysExpr.NewArrayInit(typeof(string), parts)),
        };
        return true;
    }

    private static SysExpr ConvertArgument(SysExpr value, Type valueType, Type parameterType)
    {
        if (parameterType == valueType)
        {
            return value;
        }

        if (parameterType == typeof(object))
        {
            return Box(value, valueType);
        }

        if (parameterType.IsAssignableFrom(valueType))
        {
            return SysExpr.Convert(value, parameterType);
        }

        if (NumericConversion.IsSupported(parameterType)
            && TypeTable.GetEvalType(valueType) == EvalType.Number
            && NumericConversion.TryGetParameterConverter(parameterType, out Func<SysExpr, SysExpr>? converter))
        {
            SysExpr numeric = valueType == typeof(double) ? value : SysExpr.Convert(value, typeof(double));
            return converter(numeric);
        }

        SysExpr converted = SysExpr.Call(ConvertToMethod, Box(value, valueType), SysExpr.Constant(parameterType));
        return SysExpr.Convert(converted, parameterType);
    }

    private static bool IsStatic(MemberInfo member) => member switch
    {
        MethodInfo method => method.IsStatic,
        PropertyInfo property => MemberMetadata.GetGetter(property)?.IsStatic ?? false,
        FieldInfo field => field.IsStatic,
        _ => false,
    };

    private static SysExpr ReadValue(ITypedValue variable) =>
        variable is EvalVariable evalVariable
            ? SysExpr.Property(SysExpr.Constant(evalVariable, typeof(EvalVariable)), nameof(EvalVariable.Value))
            : SysExpr.Property(SysExpr.Constant(variable, typeof(ITypedValue)), nameof(ITypedValue.Value));

    private static SysExpr Box(SysExpr value, Type type) =>
        type == typeof(object) ? value : SysExpr.Convert(value, typeof(object));

    private static SysExpr Number(SysExpr value, Type type) =>
        type == typeof(double) ? value : SysExpr.Call(AsDoubleMethod, Box(value, type));

    private static SysExpr Text(SysExpr value, Type type) =>
        type == typeof(string) ? value : SysExpr.Call(ToTextMethod, Box(value, type));

    private static SysExpr Boolean(SysExpr value, Type type) =>
        type == typeof(bool) ? value : SysExpr.Call(ToBooleanMethod, Box(value, type));
}

/// <summary>整树对象编译产物（延迟到首次求值才生成 IL）。</summary>
internal sealed class ObjectProgram
{
    private readonly SysExpr _body;
    private Func<object?>? _compiled;

    public ObjectProgram(SysExpr body) => _body = body;

    public Func<object?> Compile() =>
        _compiled ??= ExpressionTreeCompiler.Compile(SysExpr.Lambda<Func<object?>>(_body));
}
