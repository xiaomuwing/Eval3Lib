using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using Eval3.Compilation;
using FastExpressionCompiler;
using SysExpr = System.Linq.Expressions.Expression;
using SysLambda = System.Linq.Expressions.Expression<System.Func<object?, object?[], object?>>;

namespace Eval3.Benchmarks;

/// <summary>
/// 表达式树编译后端对照：<see cref="ExpressionTreeCompiler"/>（FastExpressionCompiler）
/// 与 <c>Expression.Compile()</c> 的编译耗时、求值耗时对比。
/// <c>dotnet run --project src/Eval3.Benchmarks -c Release -- --compiler</c>
/// </summary>
public static class CompilerBenchmarks
{
    private static long _sink;

    /// <summary>运行编译后端对照基准。</summary>
    public static void Run(int iterations)
    {
        int compileIterations = Math.Clamp(iterations / 200, 200, 5_000);
        int invokeIterations = Math.Clamp(iterations, 50_000, 2_000_000);

        var target = new CompilerTarget();
        object?[] twoArgs = [3d, 4d];
        object?[] oneArg = [3d];

        (string Name, SysLambda Tree, object?[] Args)[] shapes =
        [
            ("静态方法调用（1 参）", BuildInvoker(typeof(CompilerTarget).GetMethod(nameof(CompilerTarget.Scale))!, 1), oneArg),
            ("实例方法调用（2 参）", BuildInvoker(typeof(CompilerTarget).GetMethod(nameof(CompilerTarget.Combine))!, 2), twoArgs),
            ("实例属性读取", BuildInvoker(typeof(CompilerTarget).GetProperty(nameof(CompilerTarget.Factor))!, 0), []),
            ("可选参数默认值（1 参）", BuildInvoker(typeof(CompilerTarget).GetMethod(nameof(CompilerTarget.Describe))!, 1), ["abc"]),
            ("params 数组（3 参）", BuildInvoker(typeof(CompilerTarget).GetMethod(nameof(CompilerTarget.Total))!, 3), [1d, 2d, 3d]),
        ];

        Console.WriteLine($".NET {Environment.Version}，{Environment.ProcessorCount} 逻辑核");
        Console.WriteLine($"编译对照：每项 {compileIterations:N0} 次编译；求值对照：每项 {invokeIterations:N0} 次调用");
        Console.WriteLine();

        Console.WriteLine("| 表达式树（与 MethodInvoker 生成结构一致） | Expression.Compile | FastExpressionCompiler | 编译加速 |");
        Console.WriteLine("| --- | ---: | ---: | ---: |");
        foreach ((string name, SysLambda tree, _) in shapes)
        {
            double system = MeasureCompile(compileIterations, () => tree.Compile());
            double fast = MeasureCompile(compileIterations, () => tree.CompileFast());
            Console.WriteLine(
                $"| {name} | {FormatTime(system)} | {FormatTime(fast)} | {system / fast:F1}× |");
        }

        Console.WriteLine();
        Console.WriteLine("| 已编译委托求值 | Expression.Compile | FastExpressionCompiler | 差异 |");
        Console.WriteLine("| --- | ---: | ---: | ---: |");
        foreach ((string name, SysLambda tree, object?[] args) in shapes)
        {
            Func<object?, object?[], object?> system = tree.Compile();
            Func<object?, object?[], object?> fast = ExpressionTreeCompiler.Compile(tree);
            _ = system(target, args);
            _ = fast(target, args);

            BenchmarkResult systemResult = Benchmark.Measure(
                name, invokeIterations, () => _sink += system(target, args)?.GetHashCode() ?? 0);
            BenchmarkResult fastResult = Benchmark.Measure(
                name, invokeIterations, () => _sink += fast(target, args)?.GetHashCode() ?? 0);

            Console.WriteLine(
                $"| {name} | {FormatTime(systemResult.NanosecondsPerOperation)} | "
                + $"{FormatTime(fastResult.NanosecondsPerOperation)} | "
                + $"{systemResult.NanosecondsPerOperation / fastResult.NanosecondsPerOperation:F2}× |");
        }

        Console.WriteLine();
        Console.WriteLine(
            $"编译统计：FastExpressionCompiler {ExpressionTreeCompiler.FastCompiledCount} 次，"
            + $"回退 {ExpressionTreeCompiler.FallbackCompiledCount} 次（sink={_sink}）");
    }

    private static double MeasureCompile(int iterations, Func<Delegate> compile)
    {
        for (int i = 0; i < 50; i++)
        {
            _sink += compile().GetHashCode();
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();

        var stopwatch = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            _sink += compile().GetHashCode();
        }

        stopwatch.Stop();
        return stopwatch.Elapsed.TotalNanoseconds / iterations;
    }

    /// <summary>按 <c>MethodInvoker.BuildCompiled</c> 的结构生成 <c>(target, args) =&gt; result</c> 表达式树。</summary>
    private static SysLambda BuildInvoker(MemberInfo member, int argumentCount)
    {
        ParameterInfo[] parameters = member switch
        {
            MethodInfo method => method.GetParameters(),
            PropertyInfo property => property.GetIndexParameters(),
            _ => [],
        };

        bool paramsArray = parameters.Length > 0
            && parameters[^1].GetCustomAttribute<ParamArrayAttribute>() is not null;
        var target = SysExpr.Parameter(typeof(object), "target");
        var args = SysExpr.Parameter(typeof(object[]), "args");
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
            _ => throw new NotSupportedException(),
        };

        return SysExpr.Lambda<Func<object?, object?[], object?>>(
            SysExpr.Convert(call, typeof(object)),
            target,
            args);
    }

    private static System.Linq.Expressions.Expression ConvertArgument(
        System.Linq.Expressions.Expression argument,
        Type targetType) =>
        targetType == typeof(object) ? argument : SysExpr.Convert(argument, targetType);

    private static string FormatTime(double nanoseconds) => nanoseconds switch
    {
        < 1_000d => string.Create(CultureInfo.InvariantCulture, $"{nanoseconds:F1} ns"),
        < 1_000_000d => string.Create(CultureInfo.InvariantCulture, $"{nanoseconds / 1_000d:F2} µs"),
        _ => string.Create(CultureInfo.InvariantCulture, $"{nanoseconds / 1_000_000d:F2} ms"),
    };
}

/// <summary>编译后端对照用的被调用方样本。</summary>
public sealed class CompilerTarget
{
    /// <summary>实例属性。</summary>
    public double Factor { get; set; } = 2.5;

    /// <summary>实例方法。</summary>
    public double Combine(double left, double right) => (left * right) + Factor;

    /// <summary>静态方法。</summary>
    public static double Scale(double value) => value * 3d;

    /// <summary>含可选参数的实例方法。</summary>
    public string Describe(string prefix, int count = 2) => $"{prefix}:{count}";

    /// <summary>params 数组方法。</summary>
    public double Total(params double[] values) => values.Sum();
}
