using System.Linq.Expressions;
using System.Reflection;
using Eval3.Compilation;
using SysExpr = System.Linq.Expressions.Expression;

namespace Eval3.Tests;

/// <summary>编译后端测试会修改进程级开关，禁止与其它测试并行。</summary>
[CollectionDefinition("编译器后端", DisableParallelization = true)]
public sealed class CompilerBackendCollection
{
}

/// <summary>
/// 表达式树编译后端测试：验证默认走 FastExpressionCompiler、可回退到 Expression.Compile()，
/// 且两条后端对 <c>MethodInvoker</c> 会生成的各类成员调用结构给出完全一致的结果。
/// </summary>
[Collection("编译器后端")]
public class CompilerTests
{
    private static Expression<Func<double, double, double>> AddTree() => (left, right) => left + right;

    [Fact]
    public void CompilePrefersFastCompilerByDefault()
    {
        RunWithBackend(fast: true, () =>
        {
            ExpressionTreeCompiler.ResetStatistics();

            Func<double, double, double> compiled = ExpressionTreeCompiler.Compile(AddTree());

            Assert.Equal(3d, compiled(1, 2), 10);
            Assert.Equal(1, ExpressionTreeCompiler.FastCompiledCount);
            Assert.Equal(0, ExpressionTreeCompiler.FallbackCompiledCount);
        });
    }

    [Fact]
    public void CompileFallsBackToSystemCompilerWhenDisabled()
    {
        RunWithBackend(fast: false, () =>
        {
            ExpressionTreeCompiler.ResetStatistics();

            Func<double, double, double> compiled = ExpressionTreeCompiler.Compile(AddTree());

            Assert.Equal(3d, compiled(1, 2), 10);
            Assert.Equal(0, ExpressionTreeCompiler.FastCompiledCount);
            Assert.Equal(1, ExpressionTreeCompiler.FallbackCompiledCount);
        });
    }

    [Fact]
    public void CompileRejectsNullTree() =>
        Assert.Throws<ArgumentNullException>(
            () => ExpressionTreeCompiler.Compile((Expression<Func<double, double, double>>)null!));

    [Theory]
    [InlineData(nameof(CompilerProbe.Add), 2)]
    [InlineData(nameof(CompilerProbe.Scale), 1)]
    [InlineData(nameof(CompilerProbe.Scale), 2)]
    [InlineData(nameof(CompilerProbe.Total), 0)]
    [InlineData(nameof(CompilerProbe.Total), 3)]
    [InlineData(nameof(CompilerProbe.Describe), 1)]
    [InlineData(nameof(CompilerProbe.Describe), 2)]
    [InlineData(nameof(CompilerProbe.Touch), 0)]
    [InlineData(nameof(CompilerProbe.Offset), 0)]
    [InlineData(nameof(CompilerProbe.Factor), 0)]
    [InlineData(nameof(CompilerProbe.Constant), 0)]
    public void BothBackendsProduceIdenticalResults(string memberName, int argumentCount)
    {
        var probe = new CompilerProbe();
        MemberInfo member = FindMember(memberName, argumentCount);
        object?[] arguments = CreateArguments(member, argumentCount);

        Func<object?, object?[], object?> fast = CompileInvoker(member, argumentCount, preferFast: true);
        Func<object?, object?[], object?> system = CompileInvoker(member, argumentCount, preferFast: false);

        object? expected = system(probe, arguments);
        object? actual = fast(probe, arguments);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("Add(2, 3)", 5d)]
    [InlineData("Scale(4)", 12d)]
    [InlineData("Scale(4, 5)", 20d)]
    [InlineData("Total()", 0d)]
    [InlineData("Total(1, 2, 3)", 6d)]
    [InlineData("Describe(\"a\")", "a:2")]
    [InlineData("Describe(\"a\", 5)", "a:5")]
    [InlineData("Offset", 1.5d)]
    [InlineData("Factor", 2.5d)]
    [InlineData("Constant", 7d)]
    public void MemberShapesEvaluateThroughEngine(string formula, object expected)
    {
        AuthorizeFastCompiler(() =>
        {
            var engine = new Evaluator();
            engine.AddEnvironmentFunctions(new CompilerProbe());

            object? actual = engine.Evaluate(formula);

            if (expected is double number)
            {
                Assert.Equal(number, Convert.ToDouble(actual), 10);
            }
            else
            {
                Assert.Equal(expected, actual);
            }
        });
    }

    [Fact]
    public void MemberInvokersUseFastCompiler()
    {
        AuthorizeFastCompiler(() =>
        {
            ExpressionTreeCompiler.ResetStatistics();

            var engine = new Evaluator();
            engine.AddEnvironmentFunctions(new FreshCompilerProbe());

            Assert.Equal(5d, Convert.ToDouble(engine.Evaluate("Combine(2, 3)")), 10);
            Assert.Equal(6d, Convert.ToDouble(engine.Evaluate("Collect(1, 2, 3)")), 10);

            Assert.True(
                ExpressionTreeCompiler.FastCompiledCount > 0,
                "成员调用委托应当由 FastExpressionCompiler 编译。");
            Assert.Equal(0, ExpressionTreeCompiler.FallbackCompiledCount);
        });
    }

    [Fact]
    public void NumericFastPathCallUsesFastCompiler()
    {
        AuthorizeFastCompiler(() =>
        {
            var bag = new DictionaryVariableBag();
            bag.Set("price", 12.5);

            var engine = new Evaluator();
            engine.AddEnvironmentFunctions(bag, typeof(Eval3.Functions.MathLibrary));

            ExpressionTreeCompiler.ResetStatistics();
            CompiledFormula compiled = engine.Compile("Round(price * 2, 2)");
            double value = compiled.EvaluateNumber();

            Assert.True(compiled.HasNumericFastPath);
            Assert.Equal(25d, value, 10);
            Assert.True(
                ExpressionTreeCompiler.FastCompiledCount > 0,
                "数值快路径的调用委托应当由 FastExpressionCompiler 编译。");
            Assert.Equal(0, ExpressionTreeCompiler.FallbackCompiledCount);
        });
    }

    private static Func<object?, object?[], object?> CompileInvoker(
        MemberInfo member,
        int argumentCount,
        bool preferFast) =>
        RunWithBackend(preferFast, () => ExpressionTreeCompiler.Compile(BuildInvoker(member, argumentCount)));

    /// <summary>
    /// 按 <c>MethodInvoker.BuildCompiled</c> 的结构生成 <c>(target, args) =&gt; result</c> 表达式树，
    /// 覆盖拆箱转换、可选参数默认值、params 数组、属性 / 字段读取与 void 方法等形态。
    /// </summary>
    private static Expression<Func<object?, object?[], object?>> BuildInvoker(MemberInfo member, int argumentCount)
    {
        ParameterInfo[] parameters = GetParameters(member);
        bool paramsArray = parameters.Length > 0
            && parameters[^1].GetCustomAttribute<ParamArrayAttribute>() is not null;
        ParameterExpression target = SysExpr.Parameter(typeof(object), "target");
        ParameterExpression args = SysExpr.Parameter(typeof(object[]), "args");
        var callArguments = new List<System.Linq.Expressions.Expression>(parameters.Length);

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
            Type elementType = parameters[^1].ParameterType.GetElementType()!;
            var elements = new List<System.Linq.Expressions.Expression>();
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
                callArguments.Add(SysExpr.Constant(parameters[i].DefaultValue, parameters[i].ParameterType));
            }
        }

        System.Linq.Expressions.Expression call = member switch
        {
            MethodInfo { IsStatic: true } method => SysExpr.Call(method, callArguments),
            MethodInfo method => SysExpr.Call(SysExpr.Convert(target, method.DeclaringType!), method, callArguments),
            PropertyInfo property => SysExpr.Property(SysExpr.Convert(target, property.DeclaringType!), property),
            FieldInfo { IsStatic: true } field => SysExpr.Field(null, field),
            FieldInfo field => SysExpr.Field(SysExpr.Convert(target, field.DeclaringType!), field),
            _ => throw new NotSupportedException(),
        };

        if (member is MethodInfo { ReturnType: var returnType } && returnType == typeof(void))
        {
            call = SysExpr.Block(call, SysExpr.Constant(null, typeof(object)));
        }

        return SysExpr.Lambda<Func<object?, object?[], object?>>(
            SysExpr.Convert(call, typeof(object)),
            target,
            args);
    }

    private static System.Linq.Expressions.Expression ConvertArgument(
        System.Linq.Expressions.Expression argument,
        Type targetType) =>
        targetType == typeof(object) ? argument : SysExpr.Convert(argument, targetType);

    private static ParameterInfo[] GetParameters(MemberInfo member) => member switch
    {
        MethodInfo method => method.GetParameters(),
        PropertyInfo property => property.GetIndexParameters(),
        _ => [],
    };

    private static MemberInfo FindMember(string name, int argumentCount) =>
        typeof(CompilerProbe)
            .GetMember(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .First(member => SupportsArgumentCount(member, argumentCount));

    private static bool SupportsArgumentCount(MemberInfo member, int argumentCount)
    {
        if (member is not MethodInfo method)
        {
            return argumentCount == 0;
        }

        ParameterInfo[] parameters = method.GetParameters();
        bool paramsArray = parameters.Length > 0
            && parameters[^1].GetCustomAttribute<ParamArrayAttribute>() is not null;
        int fixedCount = paramsArray ? parameters.Length - 1 : parameters.Length;
        int required = parameters.Count(parameter => !parameter.IsOptional) - (paramsArray ? 1 : 0);

        return argumentCount >= required && (paramsArray || argumentCount <= fixedCount);
    }

    /// <summary>按成员签名构造实际参数，避免装箱类型与形参不匹配。</summary>
    private static object?[] CreateArguments(MemberInfo member, int argumentCount)
    {
        ParameterInfo[] parameters = GetParameters(member);
        bool paramsArray = parameters.Length > 0
            && parameters[^1].GetCustomAttribute<ParamArrayAttribute>() is not null;
        int fixedCount = paramsArray ? parameters.Length - 1 : parameters.Length;

        object?[] arguments = new object?[argumentCount];
        for (int i = 0; i < argumentCount; i++)
        {
            Type type = i < fixedCount
                ? parameters[i].ParameterType
                : paramsArray
                    ? parameters[^1].ParameterType.GetElementType()!
                    : typeof(double);
            arguments[i] = CreateValue(type, i);
        }

        return arguments;
    }

    private static object CreateValue(Type type, int index) => type switch
    {
        _ when type == typeof(string) => $"a{index}",
        _ when type == typeof(int) => index + 1,
        _ when type == typeof(double) => index + 1d,
        _ => throw new NotSupportedException($"测试样本未覆盖形参类型 {type.Name}"),
    };

    private static void AuthorizeFastCompiler(Action action) => RunWithBackend(fast: true, action);

    private static void RunWithBackend(bool fast, Action action)
    {
        _ = RunWithBackend(fast, () =>
        {
            action();
            return true;
        });
    }

    private static T RunWithBackend<T>(bool fast, Func<T> action)
    {
        bool original = ExpressionTreeCompiler.PreferFastCompiler;
        try
        {
            ExpressionTreeCompiler.PreferFastCompiler = fast;
            return action();
        }
        finally
        {
            ExpressionTreeCompiler.PreferFastCompiler = original;
        }
    }
}

/// <summary>编译后端测试用的成员样本。</summary>
public sealed class CompilerProbe
{
    /// <summary>实例字段。</summary>
    public double Factor = 2.5;

    /// <summary>实例属性。</summary>
    public double Offset { get; set; } = 1.5;

    /// <summary>静态字段。</summary>
    public static readonly double Constant = 7d;

    /// <summary>实例方法，两个必填参数。</summary>
    public double Add(double left, double right) => left + right;

    /// <summary>静态方法，含可选参数。</summary>
    public static double Scale(double value, double factor = 3d) => value * factor;

    /// <summary>params 数组方法。</summary>
    public double Total(params double[] values) => values.Sum();

    /// <summary>字符串 + 可选参数方法。</summary>
    public string Describe(string prefix, int count = 2) => $"{prefix}:{count}";

    /// <summary>无返回值方法。</summary>
    public void Touch()
    {
        Factor = 2.5;
    }
}

/// <summary>
/// 仅供"成员调用委托确实由 FastExpressionCompiler 编译"用例使用的样本：
/// 成员编译委托在进程内缓存，必须用其它用例不会触碰的成员才能观察到首次编译行为。
/// </summary>
public sealed class FreshCompilerProbe
{
    /// <summary>两个必填参数的实例方法。</summary>
    public double Combine(double left, double right) => left + right;

    /// <summary>params 数组方法。</summary>
    public double Collect(params double[] values) => values.Sum();
}
