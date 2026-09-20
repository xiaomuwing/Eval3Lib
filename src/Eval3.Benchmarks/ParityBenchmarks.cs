using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace Eval3.Benchmarks;

/// <summary>与遗留 VB 版引擎对照用的函数库（实例方法，两边定义一致）。</summary>
public sealed class ParityFunctions
{
    public double Abs(double value) => Math.Abs(value);

    public double Max(double left, double right) => Math.Max(left, right);

    public double Min(double left, double right) => Math.Min(left, right);

    public double Round(double value, int digits) => Math.Round(value, digits);

    public double Sqrt(double value) => Math.Sqrt(value);

    public double Pow(double value, double power) => Math.Pow(value, power);
}

/// <summary>
/// 新旧引擎对照基准：变量、函数库、公式集合与 <c>tools\Bench-LegacyVb.ps1</c> 完全一致，
/// 输出同样格式的 CSV，便于逐条对比。
/// </summary>
public static class ParityBenchmarks
{
    /// <summary>公式编号。</summary>
    public static readonly string[] Ids =
    [
        "F1-长算术表达式",
        "F2-变量+函数",
        "F3-复杂业务公式",
        "F4-函数密集嵌套",
    ];

    /// <summary>对照公式集合（只使用旧版同样支持的特性）。</summary>
    public static readonly string[] Formulas =
    [
        "(1.5 + 2.25) * (3.75 - 4.125) / 2.5 + (6.25 * 1.75 - 8.5 / 2) * (9.75 + 3.5 - 1.25)"
            + " - ((2.5 + 4.75) * 3.25 - (7.5 / 2.5 + 6.75 * 1.5)) * (4.5 - 2.25)"
            + " + 12.5 * (3.25 + 7.75) / (1.5 + 0.5)",
        "Round(Max(price * count, cost) * (1 + rate) - Min(price, cost) / count, 2)",
        "Round(Sqrt(Abs(price * price - cost * cost) + Max(weight, 1) * 2.5) * (1 + rate)"
            + " - Min(price, cost) * count / (1 + weight), 2)",
        "Round(Round(Round(price * count * (1 + rate), 2) + Round(cost / count, 2), 2) * (1 + rate), 2)",
    ];

    /// <summary>运行对照基准并写出 CSV。</summary>
    public static void Run(int parseIterations, int evalIterations, string outFile)
    {
        var variables = new DictionaryVariableBag();
        variables.Set("price", 100d);
        variables.Set("count", 8d);
        variables.Set("cost", 2540.75);
        variables.Set("rate", 0.13);
        variables.Set("weight", 12.5);

        var evaluator = new Evaluator();
        evaluator.AddEnvironmentFunctions(new ParityFunctions(), variables);

        var csv = new StringBuilder();
        csv.AppendLine("engine,formula,mode,iterations,ns_per_op,ops_per_sec,bytes_per_op,value");

        for (int i = 0; i < Formulas.Length; i++)
        {
            string formula = Formulas[i];
            string id = Ids[i];
            object? value = evaluator.Parse(formula).Evaluate();
            Expression root = evaluator.Parse(formula);
            Console.WriteLine(
                $"{id} = {Convert.ToString(value, CultureInfo.InvariantCulture)}"
                + $"（表达式树节点 {ExpressionTreeFormatter.Flatten(root).Count} 个，公式 {formula.Length} 字符）");

            csv.AppendLine(Measure(id, "parse+eval", parseIterations, () => evaluator.Parse(formula).Evaluate(), value));

            CompiledFormula compiled = evaluator.Compile(formula);
            csv.AppendLine(Measure(id, "eval-only", evalIterations, () =>
            {
                object? result = compiled.Evaluate();
                return result;
            }, value));

            csv.AppendLine(Measure(id, "cached-eval", evalIterations, () => evaluator.Evaluate(formula), value));

            if (compiled.HasNumericFastPath)
            {
                csv.AppendLine(MeasureNumber(id, "eval-fast", evalIterations, compiled.EvaluateNumber, value));
            }
        }

        string folder = Path.GetDirectoryName(Path.GetFullPath(outFile)) ?? ".";
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.GetFullPath(outFile), csv.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        Console.WriteLine("已写出：" + Path.GetFullPath(outFile));
    }

    private static string Measure(string id, string mode, int iterations, Func<object?> action, object? expected)
    {
        int warmup = Math.Min(iterations / 10 + 200, 20_000);
        for (int i = 0; i < warmup; i++)
        {
            _ = action();
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        long before = GC.GetAllocatedBytesForCurrentThread();
        double best = double.MaxValue;
        long bytes = 0;
        for (int pass = 0; pass < 3; pass++)
        {
            long passBefore = GC.GetAllocatedBytesForCurrentThread();
            var watch = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                if (action() is null)
                {
                    throw new InvalidOperationException();
                }
            }

            watch.Stop();
            best = Math.Min(best, watch.Elapsed.TotalMilliseconds * 1_000_000d / iterations);
            bytes = GC.GetAllocatedBytesForCurrentThread() - passBefore;
        }

        _ = before;
        return Row("eval3-net10", id, mode, iterations, best, bytes, expected);
    }

    private static string MeasureNumber(string id, string mode, int iterations, Func<double> action, object? expected)
    {
        int warmup = Math.Min(iterations / 10 + 200, 20_000);
        for (int i = 0; i < warmup; i++)
        {
            _ = action();
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        double best = double.MaxValue;
        long bytes = 0;
        for (int pass = 0; pass < 3; pass++)
        {
            long passBefore = GC.GetAllocatedBytesForCurrentThread();
            var watch = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                if (double.IsNaN(action()))
                {
                    throw new InvalidOperationException();
                }
            }

            watch.Stop();
            best = Math.Min(best, watch.Elapsed.TotalMilliseconds * 1_000_000d / iterations);
            bytes = GC.GetAllocatedBytesForCurrentThread() - passBefore;
        }

        return Row("eval3-net10", id, mode, iterations, best, bytes, expected);
    }

    private static string Row(string engine, string id, string mode, int iterations, double nanoseconds, long bytes, object? value)
    {
        return string.Join(
            ',',
            engine,
            id,
            mode,
            iterations.ToString(CultureInfo.InvariantCulture),
            nanoseconds.ToString("F1", CultureInfo.InvariantCulture),
            (1_000_000_000d / nanoseconds).ToString("F0", CultureInfo.InvariantCulture),
            ((double)bytes / iterations).ToString("F1", CultureInfo.InvariantCulture),
            Convert.ToString(value, CultureInfo.InvariantCulture));
    }
}
