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

        // 解析基准专用：关闭 AST 缓存，测“真正的首次解析”分配。
        var parseEngine = new Evaluator(new EvaluatorOptions { EnableParseCache = false });
        parseEngine.AddEnvironmentFunctions(
            variables,
            typeof(MathLibrary),
            typeof(TextLibrary),
            typeof(DateLibrary),
            typeof(FinanceLibrary));

        // 旧路径：关闭整树编译，保留改造前的“逐节点委托 / 树遍历”行为，用于 A/B 对照。
        var legacyEngine = new Evaluator(new EvaluatorOptions { UseWholeTreeNumericCompiler = false });
        legacyEngine.AddEnvironmentFunctions(
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

        CompiledFormula legacyNumeric = legacyEngine.Compile(numericFormula);
        CompiledFormula legacyVariable = legacyEngine.Compile(variableFormula);
        CompiledFormula legacyFunction = legacyEngine.Compile(functionFormula);
        CompiledFormula legacyParamsFunction = legacyEngine.Compile(paramsFunctionFormula);
        CompiledFormula legacyText = legacyEngine.Compile(textFormula);

        var dataTable = new DataTable();
        double expected = engine.EvaluateNumber(dataTableFormula);
        double actual = Convert.ToDouble(dataTable.Compute(dataTableFormula, string.Empty), CultureInfo.InvariantCulture);
        Console.WriteLine($"DataTable 基准自检：{dataTableFormula} = {actual}（Eval3 = {expected}）");
        Console.WriteLine();

        var results = new List<BenchmarkResult>
        {
            Benchmark.Measure("解析 + 求值（新路径，未缓存）", iterations / 4, () => parseEngine.Parse(numericFormula).Evaluate()),
            Benchmark.Measure("缓存公式 Evaluate（新路径）", iterations, () => numeric.Evaluate()),
            Benchmark.Measure("缓存公式 EvaluateNumber（新路径）", iterations, () => numeric.EvaluateNumber()),
            Benchmark.Measure("缓存公式 Evaluate（旧路径）", iterations, () => legacyNumeric.Evaluate()),
            Benchmark.Measure("缓存公式 EvaluateNumber（旧路径）", iterations, () => legacyNumeric.EvaluateNumber()),
            Benchmark.Measure("DataTable.Compute（旧 C# 方案）", iterations / 4, () => dataTable.Compute(dataTableFormula, string.Empty)),

            Benchmark.Measure("变量公式：旧路径 Evaluate", iterations, () => legacyVariable.Evaluate()),
            Benchmark.Measure("变量公式：新路径 Evaluate", iterations, () => variable.Evaluate()),
            Benchmark.Measure("变量公式：旧路径 EvaluateNumber", iterations, () => legacyVariable.EvaluateNumber()),
            Benchmark.Measure("变量公式：新路径 EvaluateNumber", iterations, () => variable.EvaluateNumber()),

            Benchmark.Measure("函数调用 Round：旧路径 Evaluate", iterations, () => legacyFunction.Evaluate()),
            Benchmark.Measure("函数调用 Round：新路径 Evaluate", iterations, () => function.Evaluate()),
            Benchmark.Measure("函数调用 Round：旧路径 EvaluateNumber", iterations, () => legacyFunction.EvaluateNumber()),
            Benchmark.Measure("函数调用 Round：新路径 EvaluateNumber", iterations, () => function.EvaluateNumber()),

            Benchmark.Measure("函数调用 Sum：旧路径 Evaluate", iterations, () => legacyParamsFunction.Evaluate()),
            Benchmark.Measure("函数调用 Sum：新路径 Evaluate", iterations, () => paramsFunction.Evaluate()),
            Benchmark.Measure("函数调用 Sum：旧路径 EvaluateNumber", iterations, () => legacyParamsFunction.EvaluateNumber()),
            Benchmark.Measure("函数调用 Sum：新路径 EvaluateNumber", iterations, () => paramsFunction.EvaluateNumber()),

            Benchmark.Measure("字符串插值模板：旧路径", iterations / 2, () => legacyText.Evaluate()),
            Benchmark.Measure("字符串插值模板：新路径", iterations / 2, () => text.Evaluate()),
        };

        Print(results);

        ParallelThroughput(engine, legacyEngine, functionFormula, iterations);
        return 0;
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

    private static void ParallelThroughput(Evaluator engine, Evaluator legacyEngine, string formula, int iterations)
    {
        CompiledFormula compiled = engine.Compile(formula);
        CompiledFormula legacy = legacyEngine.Compile(formula);
        int total = iterations * 4;

        Console.WriteLine();
        Console.WriteLine($"并行吞吐（{Environment.ProcessorCount} 核，公式：{formula}）");
        Console.WriteLine("| 求值方式 | 迭代 | 每秒次数 | 相对旧路径 |");
        Console.WriteLine("| --- | ---: | ---: | ---: |");

        double oldOps = MeasureParallel(total, () => legacy.EvaluateNumber());
        double newOps = MeasureParallel(total, () => compiled.EvaluateNumber());

        Console.WriteLine($"| 旧路径（逐节点委托） | {total:N0} | {oldOps:N0} | 1.00× |");
        Console.WriteLine($"| 新路径（整树单 lambda） | {total:N0} | {newOps:N0} | {newOps / oldOps:F2}× |");
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
