using System.Data;
using System.Globalization;
using System.Text;
using Eval3;
using Eval3.Compilation;
using Eval3.Functions;

namespace Eval3.Benchmarks;

/// <summary>
/// 性能对比工具：
/// <c>dotnet run --project src/Eval3.Benchmarks -c Release -- 500000</c>
/// </summary>
internal static class Program
{
    private static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        // --no-fast-compiler：强制表达式树编译回退到 Expression.Compile()，便于 A/B 对照
        if (args.Any(static a => a.StartsWith("--no-fast-compiler", StringComparison.OrdinalIgnoreCase)))
        {
            ExpressionTreeCompiler.PreferFastCompiler = false;
            Console.WriteLine("注意：已禁用 FastExpressionCompiler，表达式树回退到 Expression.Compile()。");
            Console.WriteLine();
        }

        // 与遗留 VB 版引擎的对照基准（CSV 输出，供 tools\Compare-Performance.ps1 汇总）
        if (args.Any(static a => a.StartsWith("--parity", StringComparison.OrdinalIgnoreCase)))
        {
            ParityBenchmarks.Run(
                ReadInt(args, "--parse-iterations", 20_000),
                ReadInt(args, "--eval-iterations", 200_000),
                ReadText(args, "--out") ?? Path.Combine("artifacts", "eval3-net10.csv"));
            return 0;
        }

        // 表达式树编译后端对照（FastExpressionCompiler vs Expression.Compile）
        if (args.Any(static a => a.StartsWith("--compiler", StringComparison.OrdinalIgnoreCase)))
        {
            CompilerBenchmarks.Run(ReadInt(args, "--iterations", 200_000));
            return 0;
        }

        // 水蒸汽物性库性能对照
        if (args.Any(static a => a.StartsWith("--thermo", StringComparison.OrdinalIgnoreCase)))
        {
            ThermoBenchmarks.Run(ReadInt(args, "--iterations", 100_000));
            return 0;
        }

        int iterations = args.Length > 0 && int.TryParse(args[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
            ? parsed
            : 200_000;

        Console.WriteLine($".NET {Environment.Version}，{Environment.ProcessorCount} 逻辑核，{System.Runtime.InteropServices.RuntimeInformation.OSDescription}");
        Console.WriteLine($"每组迭代 {iterations:N0} 次（含预热）");
        Console.WriteLine();

        var variables = new DictionaryVariableBag();
        variables.Set("price", 12.5);
        variables.Set("count", 8);
        variables.Set("rate", 0.13);
        variables.Set("name", "Eval3");

        var engine = new Evaluator();
        engine.AddEnvironmentFunctions(
            variables,
            typeof(MathLibrary),
            typeof(TextLibrary),
            typeof(DateLibrary),
            typeof(FinanceLibrary));

        const string numericFormula = "(1 + 2 * 3 - 4 / 8) * 6 % 7 + 12.5";
        // DataTable.Compute 不支持 double 与 int 混合取模，旧方案只能用无 % 的等价公式
        const string dataTableFormula = "(1 + 2 * 3 - 4 / 8) * 6 + 12.5";
        const string variableFormula = "price * count * (1 + rate)";
        const string functionFormula = "Round(price * count * (1 + rate) / 3, 2)";
        const string paramsFunctionFormula = "Sum(1, 2, 3, 4, 5)";
        const string textFormula = "\"合计：%[price * count] 元，含税 %[Round(price * count * 1.13, 2)]\"";

        CompiledFormula numeric = engine.Compile(numericFormula);
        CompiledFormula variable = engine.Compile(variableFormula);
        CompiledFormula function = engine.Compile(functionFormula);
        CompiledFormula paramsFunction = engine.Compile(paramsFunctionFormula);
        CompiledFormula text = engine.Compile(textFormula);

        var dataTable = new DataTable();
        double expected = engine.EvaluateNumber(dataTableFormula);
        double actual = Convert.ToDouble(dataTable.Compute(dataTableFormula, string.Empty), CultureInfo.InvariantCulture);
        Console.WriteLine($"DataTable 基准自检：{dataTableFormula} = {actual}（Eval3 = {expected}）");
        Console.WriteLine();

        var results = new List<BenchmarkResult>
        {
            Benchmark.Measure("Eval3 解析 + 求值（未缓存）", iterations / 4, () => engine.Parse(numericFormula).Evaluate()),
            Benchmark.Measure("Eval3 缓存公式求值（装箱）", iterations, () => numeric.Evaluate()),
            Benchmark.Measure("Eval3 数值快路径（免装箱）", iterations, () => numeric.EvaluateNumber()),
            Benchmark.Measure("DataTable.Compute（旧 C# 方案）", iterations / 4, () => dataTable.Compute(dataTableFormula, string.Empty)),

            // 同一公式的两种求值方式对照：Evaluate 始终逐节点求值（改动前的行为），
            // EvaluateNumber 在有快路径时走编译后的免装箱委托。
            Benchmark.Measure("变量公式：逐节点求值", iterations, () => variable.Evaluate()),
            Benchmark.Measure("变量公式：数值快路径", iterations, () => variable.EvaluateNumber()),
            Benchmark.Measure("函数调用 Round：逐节点求值", iterations, () => function.Evaluate()),
            Benchmark.Measure("函数调用 Round：数值快路径", iterations, () => function.EvaluateNumber()),
            Benchmark.Measure("函数调用 Sum：逐节点求值", iterations, () => paramsFunction.Evaluate()),
            Benchmark.Measure("函数调用 Sum：数值快路径", iterations, () => paramsFunction.EvaluateNumber()),

            Benchmark.Measure("字符串插值模板", iterations / 2, () => text.Evaluate()),
        };

        Print(results);

        ParallelThroughput(engine, functionFormula, iterations);
        return 0;
    }

    private static void Print(List<BenchmarkResult> results)
    {
        BenchmarkResult baseline = results[2];
        Console.WriteLine("| 测试项 | 迭代 | 每次耗时 | 每秒次数 | 每次分配 | 相对数值快路径 |");
        Console.WriteLine("| --- | ---: | ---: | ---: | ---: | ---: |");
        foreach (BenchmarkResult result in results)
        {
            double relative = result.NanosecondsPerOperation / baseline.NanosecondsPerOperation;
            Console.WriteLine(
                $"| {result.Name} | {result.Iterations:N0} | {FormatTime(result.NanosecondsPerOperation)} | {result.OperationsPerSecond:N0} | {result.BytesPerOperation:N1} B | {relative:F1}× |");
        }
    }

    private static void ParallelThroughput(Evaluator engine, string formula, int iterations)
    {
        CompiledFormula compiled = engine.Compile(formula);
        int total = iterations * 4;

        Console.WriteLine();
        Console.WriteLine($"并行吞吐（{Environment.ProcessorCount} 核，公式：{formula}）");
        Console.WriteLine("| 求值方式 | 迭代 | 每秒次数 | 相对数值快路径 |");
        Console.WriteLine("| --- | ---: | ---: | ---: |");

        double tree = MeasureParallel(total, () => compiled.Evaluate());
        double fast = MeasureParallel(total, () => compiled.EvaluateNumber());

        Console.WriteLine($"| 逐节点求值 | {total:N0} | {tree:N0} | {fast / tree:F2}× |");
        Console.WriteLine($"| 数值快路径 | {total:N0} | {fast:N0} | 1.00× |");
        _ = engine;
    }

    private static double MeasureParallel(int total, Action action)
    {
        for (int i = 0; i < 10_000; i++)
        {
            action();
        }

        double best = 0d;
        for (int round = 0; round < 3; round++)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            Parallel.For(0, total, _ => action());
            stopwatch.Stop();
            best = Math.Max(best, total / stopwatch.Elapsed.TotalSeconds);
        }

        return best;
    }

    private static string FormatTime(double nanoseconds) => nanoseconds switch
    {
        < 1_000d => $"{nanoseconds:F1} ns",
        < 1_000_000d => $"{nanoseconds / 1_000d:F2} µs",
        _ => $"{nanoseconds / 1_000_000d:F2} ms",
    };

    private static int ReadInt(string[] args, string name, int fallback)
    {
        string? text = ReadText(args, name);
        return text is not null && int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
            ? value
            : fallback;
    }

    private static string? ReadText(string[] args, string name)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].StartsWith(name, StringComparison.OrdinalIgnoreCase))
            {
                int separator = args[i].IndexOf('=');
                if (separator > 0)
                {
                    return args[i][(separator + 1)..];
                }

                return i + 1 < args.Length ? args[i + 1] : null;
            }
        }

        return null;
    }
}
