using System.Linq.Expressions;
using System.Reflection;
using Eval3.Compilation;
using SysExpr = System.Linq.Expressions.Expression;

namespace Eval3;

/// <summary>
/// 整树数值编译器（实验分支）：把整棵数值表达式一次性发射成<b>单个 lambda</b>，
/// 替代旧实现里「每个节点一个 <c>Func&lt;double&gt;</c> 委托 + 闭包」的调用链，
/// 并在编译期做常量折叠。
/// </summary>
/// <remarks>
/// 覆盖范围与旧数值快路径一致（数值常量 / 变量 / 四则 / 取模 / 百分比 / 一元负号 /
/// 数值函数），额外支持 <c>if(...)</c> 的数值分支。任何无法保证语义等价的节点
/// 都返回 <see langword="false"/>，由调用方回退到旧路径。
/// </remarks>
internal static class NumericPlan
{
    private static readonly MethodInfo AsDoubleMethod =
        typeof(ValueConvert).GetMethod(nameof(ValueConvert.AsDouble))!;

    private static readonly MethodInfo ToBooleanMethod =
        typeof(ValueConvert).GetMethod(nameof(ValueConvert.ToBoolean))!;

    private static readonly MethodInfo ToDateMethod =
        typeof(ValueConvert).GetMethod(nameof(ValueConvert.ToDate))!;

    private static readonly MethodInfo CompareMethod =
        typeof(NumericPlan).GetMethod(nameof(Compare), BindingFlags.NonPublic | BindingFlags.Static)!;

    /// <summary>把整棵数值树“发射”成可延迟编译的程序（不生成 IL）。</summary>
    public static bool TryBuild(Expression root, out NumericProgram? program)
    {
        program = null;
        EmitContext context = EmitContext.Create(root);
        if (!TryEmit(root, context, out SysExpr? body, out bool isConstant, out double constantValue) || body is null)
        {
            return false;
        }

        if (!isConstant)
        {
            body = WrapCse(context, body);
        }

        program = isConstant ? new NumericProgram(constantValue) : new NumericProgram(body);
        return true;
    }

    /// <summary>把整棵数值树编译成单个 <c>Func&lt;double&gt;</c>。</summary>
    public static bool TryCompile(Expression root, out Func<double> evaluate)
    {
        evaluate = null!;
        if (!TryBuild(root, out NumericProgram? program))
        {
            return false;
        }

        evaluate = program!.Compile();
        return true;
    }

    /// <summary>
    /// 把整棵数值树编译成列式批量求值委托：
    /// <c>(columns, output, count)</c>，其中 <c>columns[i][n]</c> 是第 i 个变量的第 n 个采样。
    /// </summary>
    public static bool TryCompileBatch(
        Expression root,
        IReadOnlyList<ITypedValue> variables,
        out Action<double[][], double[], int> run)
    {
        run = null!;
        var columns = SysExpr.Parameter(typeof(double[][]), "columns");
        var output = SysExpr.Parameter(typeof(double[]), "output");
        var count = SysExpr.Parameter(typeof(int), "count");
        var index = SysExpr.Variable(typeof(int), "i");

        var slots = new Dictionary<ITypedValue, int>(ReferenceEqualityComparer.Instance);
        for (int i = 0; i < variables.Count; i++)
        {
            slots[variables[i]] = i;
        }

        EmitContext context = EmitContext.Create(root, variable =>
            slots.TryGetValue(variable.Variable, out int column)
                ? SysExpr.ArrayIndex(SysExpr.ArrayIndex(columns, SysExpr.Constant(column)), index)
                : null);

        if (!TryEmit(root, context, out SysExpr? value, out _, out _) || value is null)
        {
            return false;
        }

        value = WrapCse(context, value);

        var breakLabel = SysExpr.Label(typeof(void), "break");
        SysExpr loop = SysExpr.Loop(
            SysExpr.IfThenElse(
                SysExpr.LessThan(index, count),
                SysExpr.Block(
                    SysExpr.Assign(SysExpr.ArrayAccess(output, index), value),
                    SysExpr.PostIncrementAssign(index)),
                SysExpr.Break(breakLabel)),
            breakLabel);

        SysExpr body = SysExpr.Block(
            [index],
            SysExpr.Assign(index, SysExpr.Constant(0)),
            loop);

        try
        {
            run = ExpressionTreeCompiler.Compile(
                SysExpr.Lambda<Action<double[][], double[], int>>(body, columns, output, count));
            return true;
        }
#pragma warning disable CA1031
        catch (Exception)
#pragma warning restore CA1031
        {
            return false;
        }
    }

    private static bool TryEmit(
        Expression node,
        EmitContext context,
        out SysExpr? value,
        out bool isConstant,
        out double constantValue)
    {
        value = null;
        isConstant = false;
        constantValue = 0d;

        switch (node)
        {
            case ConstantExpression { EvalType: EvalType.Number, Value: double number }:
                return EmitConstant(number, out value, out isConstant, out constantValue);

            case VariableExpression { EvalType: EvalType.Number } variable:
                return EmitVariable(variable, context, out value);

            case NegateExpression negate:
                if (!TryEmit(negate.Operand, context, out SysExpr? operand, out bool operandConstant, out double operandValue))
                {
                    return false;
                }

                if (operandConstant)
                {
                    return EmitConstant(-operandValue, out value, out isConstant, out constantValue);
                }

                value = ApplyCse(context, node, SysExpr.Negate(operand!));
                return true;

            case ConvertExpression { EvalType: EvalType.Number } convert
                when convert.SourceExpression.EvalType == EvalType.Number:
                return TryEmit(convert.SourceExpression, context, out value, out isConstant, out constantValue);

            case PercentExpression percent:
                return TryEmitPercent(percent, context, out value, out isConstant, out constantValue);

            case BinaryExpression { Operator: DateOperator { Kind: DateOperatorKind.Difference } } dateDifference:
                return TryEmitDateDifference(dateDifference, context, out value, out isConstant, out constantValue);

            case BinaryExpression binary when binary.Operator is NumericOperator numericOperator:
                return TryEmitBinary(binary, numericOperator, context, out value, out isConstant, out constantValue);

            case MethodCallExpression call:
            {
                Func<Expression, SysExpr?>? emitChild = child => Emit(child, context);
                if (!call.TryEmitNumeric(emitChild, emitVariables: context.VariableEmitter is not null, out SysExpr? body)
                    || body is null)
                {
                    return false;
                }

                value = ApplyCse(context, call, body);
                return true;
            }

            case ConditionalExpression { EvalType: EvalType.Number } conditional:
                return TryEmitConditional(conditional, context, out value, out isConstant, out constantValue);
        }

        return false;
    }

    private static SysExpr? Emit(Expression node, EmitContext context) =>
        TryEmit(node, context, out SysExpr? value, out _, out _) ? value : null;

    private static bool EmitConstant(
        double number,
        out SysExpr? value,
        out bool isConstant,
        out double constantValue)
    {
        value = SysExpr.Constant(number);
        isConstant = true;
        constantValue = number;
        return true;
    }

    private static bool EmitVariable(VariableExpression variable, EmitContext context, out SysExpr? value)
    {
        value = null;
        if (context.VariableEmitter is not null)
        {
            value = context.VariableEmitter(variable);
            return value is not null;
        }

        SysExpr read = variable.Variable is EvalVariable evalVariable
            ? SysExpr.Property(SysExpr.Constant(evalVariable, typeof(EvalVariable)), nameof(EvalVariable.Value))
            : SysExpr.Property(SysExpr.Constant(variable.Variable, typeof(ITypedValue)), nameof(ITypedValue.Value));

        value = SysExpr.Call(AsDoubleMethod, read);
        return true;
    }

    private static bool TryEmitPercent(
        PercentExpression percent,
        EmitContext context,
        out SysExpr? value,
        out bool isConstant,
        out double constantValue)
    {
        value = null;
        isConstant = false;
        constantValue = 0d;

        if (!TryEmit(percent.Operand, context, out SysExpr? operand, out bool operandConstant, out double operandValue))
        {
            return false;
        }

        if (percent.Context is null)
        {
            if (operandConstant)
            {
                return EmitConstant(operandValue / 100d, out value, out isConstant, out constantValue);
            }

            value = ApplyCse(context, percent, SysExpr.Divide(operand!, SysExpr.Constant(100d)));
            return true;
        }

        if (!TryEmit(percent.Context, context, out SysExpr? reference, out bool referenceConstant, out double referenceValue))
        {
            return false;
        }

        if (operandConstant && referenceConstant)
        {
            return EmitConstant(referenceValue * operandValue / 100d, out value, out isConstant, out constantValue);
        }

        value = ApplyCse(context, percent, SysExpr.Divide(SysExpr.Multiply(reference!, operand!), SysExpr.Constant(100d)));
        return true;
    }

    private static bool TryEmitBinary(
        BinaryExpression binary,
        NumericOperator numericOperator,
        EmitContext context,
        out SysExpr? value,
        out bool isConstant,
        out double constantValue)
    {
        value = null;
        isConstant = false;
        constantValue = 0d;

        if (!TryEmit(binary.Left, context, out SysExpr? left, out bool leftConstant, out double leftValue)
            || !TryEmit(binary.Right, context, out SysExpr? right, out bool rightConstant, out double rightValue))
        {
            return false;
        }

        if (leftConstant && rightConstant)
        {
            return EmitConstant(
                numericOperator.Apply(leftValue, rightValue),
                out value,
                out isConstant,
                out constantValue);
        }

        SysExpr? node =
            numericOperator == Operators.Add ? SysExpr.Add(left!, right!) :
            numericOperator == Operators.Subtract ? SysExpr.Subtract(left!, right!) :
            numericOperator == Operators.Multiply ? SysExpr.Multiply(left!, right!) :
            numericOperator == Operators.Divide ? SysExpr.Divide(left!, right!) :
            numericOperator == Operators.Modulo ? SysExpr.Modulo(left!, right!) :
            null;

        if (node is null)
        {
            return false;
        }

        value = ApplyCse(context, binary, node);
        return true;
    }

    private static bool TryEmitDateDifference(
        BinaryExpression difference,
        EmitContext context,
        out SysExpr? value,
        out bool isConstant,
        out double constantValue)
    {
        value = null;
        isConstant = false;
        constantValue = 0d;

        // 批量模式读取的是列，日期不在列式快路径范围内。
        if (context.VariableEmitter is not null)
        {
            return false;
        }

        if (difference.Left is ConstantExpression { Value: DateTime leftConstant }
            && difference.Right is ConstantExpression { Value: DateTime rightConstant })
        {
            return EmitConstant((leftConstant - rightConstant).TotalDays, out value, out isConstant, out constantValue);
        }

        if (!ObjectPlan.TryEmitValue(difference.Left, out SysExpr? left, out Type leftType)
            || !ObjectPlan.TryEmitValue(difference.Right, out SysExpr? right, out Type rightType))
        {
            return false;
        }

        SysExpr leftDate = leftType == typeof(DateTime) ? left! : SysExpr.Call(ToDateMethod, left!);
        SysExpr rightDate = rightType == typeof(DateTime) ? right! : SysExpr.Call(ToDateMethod, right!);
        value = SysExpr.Property(SysExpr.Subtract(leftDate, rightDate), nameof(TimeSpan.TotalDays));
        return true;
    }

    private static bool TryEmitConditional(
        ConditionalExpression conditional,
        EmitContext context,
        out SysExpr? value,
        out bool isConstant,
        out double constantValue)
    {
        value = null;
        isConstant = false;
        constantValue = 0d;

        if (!TryEmitBoolean(conditional.Condition, context, out SysExpr? condition) || condition is null)
        {
            return false;
        }

        if (!TryEmit(conditional.WhenTrue, context, out SysExpr? whenTrue, out bool trueConstant, out double trueValue)
            || !TryEmit(conditional.WhenFalse, context, out SysExpr? whenFalse, out bool falseConstant, out double falseValue))
        {
            return false;
        }

        // 条件本身是常量时直接折叠分支（与惰性求值一致：只保留命中的分支）。
        if (condition is System.Linq.Expressions.ConstantExpression { Value: bool flag })
        {
            if (flag)
            {
                value = whenTrue;
                isConstant = trueConstant;
                constantValue = trueValue;
            }
            else
            {
                value = whenFalse;
                isConstant = falseConstant;
                constantValue = falseValue;
            }

            return value is not null;
        }

        value = SysExpr.Condition(condition, whenTrue!, whenFalse!);
        return true;
    }

    private static bool TryEmitBoolean(Expression node, EmitContext context, out SysExpr? value)
    {
        value = null;
        switch (node)
        {
            case ConstantExpression { EvalType: EvalType.Boolean, Value: bool constant }:
                value = SysExpr.Constant(constant);
                return true;

            case VariableExpression { EvalType: EvalType.Boolean } variable:
                value = SysExpr.Call(ToBooleanMethod, ReadValue(variable.Variable));
                return true;

            case NotExpression negation when TryEmitBoolean(negation.Operand, context, out SysExpr? inner):
                value = SysExpr.Not(inner!);
                return true;

            case LogicalExpression logical:
            {
                if (!TryEmitBoolean(logical.Left, context, out SysExpr? left)
                    || !TryEmitBoolean(logical.Right, context, out SysExpr? right))
                {
                    return false;
                }

                value = logical.IsAnd ? SysExpr.AndAlso(left!, right!) : SysExpr.OrElse(left!, right!);
                return true;
            }

            case BinaryExpression { Operator: ComparisonOperator comparisonOperator } comparison
                when comparison.Left.EvalType == EvalType.Number && comparison.Right.EvalType == EvalType.Number:
            {
                if (!TryEmit(comparison.Left, context, out SysExpr? left, out _, out _)
                    || !TryEmit(comparison.Right, context, out SysExpr? right, out _, out _))
                {
                    return false;
                }

                // 用 double.CompareTo 而不是 < / >：NaN 的排序语义不同
                // （CompareTo 认为 NaN 小于一切，而 C# 的 < 对 NaN 恒为 false）。
                SysExpr compared = SysExpr.Call(CompareMethod, left!, right!);
                SysExpr zero = SysExpr.Constant(0);
                value = comparisonOperator.Kind switch
                {
                    ComparisonKind.Equal => SysExpr.Equal(compared, zero),
                    ComparisonKind.NotEqual => SysExpr.NotEqual(compared, zero),
                    ComparisonKind.LessThan => SysExpr.LessThan(compared, zero),
                    ComparisonKind.LessThanOrEqual => SysExpr.LessThanOrEqual(compared, zero),
                    ComparisonKind.GreaterThan => SysExpr.GreaterThan(compared, zero),
                    _ => SysExpr.GreaterThanOrEqual(compared, zero),
                };
                return true;
            }

            case ConvertExpression { EvalType: EvalType.Boolean } convert
                when convert.SourceExpression.EvalType == EvalType.Number:
            {
                if (!TryEmit(convert.SourceExpression, context, out SysExpr? number, out _, out _))
                {
                    return false;
                }

                value = SysExpr.NotEqual(number!, SysExpr.Constant(0d));
                return true;
            }
        }

        return false;
    }

    private static SysExpr ReadValue(ITypedValue variable) =>
        variable is EvalVariable evalVariable
            ? SysExpr.Property(SysExpr.Constant(evalVariable, typeof(EvalVariable)), nameof(EvalVariable.Value))
            : SysExpr.Property(SysExpr.Constant(variable, typeof(ITypedValue)), nameof(ITypedValue.Value));

    private static int Compare(double left, double right) => left.CompareTo(right);

    /// <summary>发射上下文：变量绑定策略 + 公共子表达式表（仅无方法调用时启用）。</summary>
    private sealed class EmitContext
    {
        public Func<VariableExpression, SysExpr?>? VariableEmitter { get; init; }

        public Dictionary<string, ParameterExpression>? Cse { get; init; }

        public List<(ParameterExpression Variable, SysExpr Value)>? Assignments { get; init; }

        private readonly Dictionary<ITypedValue, int> _variableIds = new(ReferenceEqualityComparer.Instance);

        public int VariableId(ITypedValue variable)
        {
            if (!_variableIds.TryGetValue(variable, out int id))
            {
                id = _variableIds.Count;
                _variableIds[variable] = id;
            }

            return id;
        }

        public static EmitContext Create(
            Expression root,
            Func<VariableExpression, SysExpr?>? variableEmitter = null)
        {
            // 只有整条公式没有方法调用时才做 CSE：此时求值无副作用，
            // 重复子式合并不会改变可观测行为（变量在求值期间不会被改写）。
            bool pure = !ContainsImpureCall(root);
            return new EmitContext
            {
                VariableEmitter = variableEmitter,
                Cse = pure ? new Dictionary<string, ParameterExpression>(StringComparer.Ordinal) : null,
                Assignments = pure ? [] : null,
            };
        }
    }

    private static bool ContainsImpureCall(Expression node)
    {
        if (node is MethodCallExpression call && !IsPure(call.Member))
        {
            return true;
        }

        foreach (Expression child in node.Children)
        {
            if (ContainsImpureCall(child))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>成员是否声明为纯函数：库/成员带 <see cref="PureAttribute"/>，且未被 <see cref="ImpureAttribute"/> 排除。</summary>
    private static bool IsPure(System.Reflection.MemberInfo member)
    {
        if (member.GetCustomAttribute<ImpureAttribute>() is not null)
        {
            return false;
        }

        return member.GetCustomAttribute<PureAttribute>() is not null
            || member.DeclaringType?.GetCustomAttribute<PureAttribute>() is not null;
    }

    private static SysExpr ApplyCse(EmitContext context, Expression node, SysExpr value)
    {
        if (context.Cse is null)
        {
            return value;
        }

        string? key = CseKey(node, context);
        if (key is null)
        {
            return value;
        }

        if (context.Cse.TryGetValue(key, out ParameterExpression? existing))
        {
            return existing;
        }

        var variable = SysExpr.Variable(typeof(double), "cse" + context.Assignments!.Count);
        context.Assignments.Add((variable, value));
        context.Cse[key] = variable;
        return variable;
    }

    /// <summary>结构键：只覆盖纯数值节点（无方法调用、无副作用），用于安全的公共子表达式消除。</summary>
    private static string? CseKey(Expression node, EmitContext context) => node switch
    {
        ConstantExpression { EvalType: EvalType.Number, Value: double number } =>
            "c" + BitConverter.DoubleToInt64Bits(number).ToString("X16"),
        VariableExpression variable => "v" + context.VariableId(variable.Variable),
        NegateExpression negate => WrapKey("n", CseKey(negate.Operand, context)),
        ConvertExpression { EvalType: EvalType.Number } convert =>
            WrapKey("x", CseKey(convert.SourceExpression, context)),
        PercentExpression percent when percent.Context is null =>
            WrapKey("p", CseKey(percent.Operand, context)),
        PercentExpression percent =>
            WrapKey2("P", CseKey(percent.Operand, context), CseKey(percent.Context!, context)),
        BinaryExpression { Operator: NumericOperator numericOperator } binary =>
            WrapKey2("b" + numericOperator.Symbol, CseKey(binary.Left, context), CseKey(binary.Right, context)),
        MethodCallExpression call when call.TargetExpression is null && IsPure(call.Member) =>
            MethodCallKey(call, context),
        _ => null,
    };

    private static string? MethodCallKey(MethodCallExpression call, EmitContext context)
    {
        var parts = new List<string>(call.Arguments.Count);
        foreach (Expression argument in call.Arguments)
        {
            string? key = CseKey(argument, context);
            if (key is null)
            {
                return null;
            }

            parts.Add(key);
        }

        return "m" + call.Member.DeclaringType?.FullName + "::" + call.Member.Name
            + "#" + call.Member.MetadataToken + "(" + string.Join(",", parts) + ")";
    }

    private static string? WrapKey(string prefix, string? child) =>
        child is null ? null : prefix + "(" + child + ")";

    private static string? WrapKey2(string prefix, string? left, string? right) =>
        left is null || right is null ? null : prefix + "(" + left + "," + right + ")";

    private static SysExpr WrapCse(EmitContext context, SysExpr body)
    {
        if (context.Assignments is not { Count: > 0 } assignments)
        {
            return body;
        }

        var variables = new List<ParameterExpression>(assignments.Count);
        var expressions = new List<SysExpr>(assignments.Count + 1);
        foreach ((ParameterExpression variable, SysExpr value) in assignments)
        {
            variables.Add(variable);
            expressions.Add(SysExpr.Assign(variable, value));
        }

        expressions.Add(body);
        return SysExpr.Block(variables, expressions);
    }
}

/// <summary>整树数值编译产物：先发射表达式树，首次求值时才生成 IL。</summary>
internal sealed class NumericProgram
{
    private readonly SysExpr? _body;
    private readonly bool _constant;
    private readonly double _value;
    private Func<double>? _compiled;

    public NumericProgram(SysExpr body) => _body = body;

    public NumericProgram(double value)
    {
        _constant = true;
        _value = value;
    }

    public Func<double> Compile()
    {
        if (_compiled is not null)
        {
            return _compiled;
        }

        _compiled = _constant
            ? () => _value
            : ExpressionTreeCompiler.Compile(SysExpr.Lambda<Func<double>>(_body!));
        return _compiled;
    }
}
