// 遗留 VB 版引擎的探针（由 tools\Bench-LegacyVb.ps1 通过 Add-Type 编译，C# 5 语法）。
// 变量、函数库、公式集合与 src\Eval3.Benchmarks 的 --parity 模式保持完全一致。
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text;

public class LegacyBag : Eval3.iVariableBag
{
    private readonly Dictionary<string, Eval3.iEvalTypedValue> map =
        new Dictionary<string, Eval3.iEvalTypedValue>(StringComparer.OrdinalIgnoreCase);

    public void Set(string name, object value)
    {
        map[name] = new Eval3.EvalVariable(name, value, string.Empty,
            value == null ? typeof(object) : value.GetType());
    }

    public Eval3.iEvalTypedValue GetVariable(string varname)
    {
        Eval3.iEvalTypedValue value;
        return map.TryGetValue(varname, out value) ? value : null;
    }
}

public class ParityFunctions
{
    public double Abs(double value) { return Math.Abs(value); }
    public double Max(double left, double right) { return Math.Max(left, right); }
    public double Min(double left, double right) { return Math.Min(left, right); }
    public double Round(double value, int digits) { return Math.Round(value, digits); }
    public double Sqrt(double value) { return Math.Sqrt(value); }
    public double Pow(double value, double power) { return Math.Pow(value, power); }
}

public static class LegacyBench
{
    private static readonly MethodInfo AllocMethod =
        typeof(GC).GetMethod("GetAllocatedBytesForCurrentThread", Type.EmptyTypes);

    public static readonly string[] Ids =
    {
        "F1-长算术表达式",
        "F2-变量+函数",
        "F3-复杂业务公式",
        "F4-函数密集嵌套",
    };

    public static readonly string[] Formulas =
    {
        "(1.5 + 2.25) * (3.75 - 4.125) / 2.5 + (6.25 * 1.75 - 8.5 / 2) * (9.75 + 3.5 - 1.25)" +
            " - ((2.5 + 4.75) * 3.25 - (7.5 / 2.5 + 6.75 * 1.5)) * (4.5 - 2.25)" +
            " + 12.5 * (3.25 + 7.75) / (1.5 + 0.5)",
        "Round(Max(price * count, cost) * (1 + rate) - Min(price, cost) / count, 2)",
        "Round(Sqrt(Abs(price * price - cost * cost) + Max(weight, 1) * 2.5) * (1 + rate)" +
            " - Min(price, cost) * count / (1 + weight), 2)",
        "Round(Round(Round(price * count * (1 + rate), 2) + Round(cost / count, 2), 2) * (1 + rate), 2)",
    };

    public static void Run(int parseIterations, int evalIterations, string outFile)
    {
        LegacyBag bag = new LegacyBag();
        bag.Set("price", 100.0);
        bag.Set("count", 8.0);
        bag.Set("cost", 2540.75);
        bag.Set("rate", 0.13);
        bag.Set("weight", 12.5);

        Eval3.Evaluator evaluator = new Eval3.Evaluator(Eval3.eParserSyntax.cSharp, false);
        evaluator.AddEnvironmentFunctions(new ParityFunctions());
        evaluator.AddEnvironmentFunctions(bag);

        StringBuilder csv = new StringBuilder();
        csv.AppendLine("engine,formula,mode,iterations,ns_per_op,ops_per_sec,bytes_per_op,value");

        for (int i = 0; i < Formulas.Length; i++)
        {
            object value = evaluator.Parse(Formulas[i]).value;
            Console.WriteLine(Ids[i] + " = " + Convert.ToString(value, CultureInfo.InvariantCulture));

            csv.AppendLine(MeasureParseAndEval(evaluator, Ids[i], Formulas[i], parseIterations, value));
            Eval3.opCode compiled = evaluator.Parse(Formulas[i]);
            csv.AppendLine(MeasureEvalOnly(compiled, Ids[i], evalIterations, value));
        }

        string full = System.IO.Path.GetFullPath(outFile);
        string folder = System.IO.Path.GetDirectoryName(full);
        if (!string.IsNullOrEmpty(folder))
        {
            System.IO.Directory.CreateDirectory(folder);
        }

        System.IO.File.WriteAllText(full, csv.ToString(), new UTF8Encoding(true));
        Console.WriteLine("已写出：" + full);
    }

    private static string MeasureParseAndEval(
        Eval3.Evaluator evaluator, string id, string formula, int iterations, object value)
    {
        int warmup = Math.Min(iterations / 10 + 200, 5000);
        for (int i = 0; i < warmup; i++)
        {
            object warm = evaluator.Parse(formula).value;
            if (warm == null) { throw new InvalidOperationException(); }
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        double best = double.MaxValue;
        long bytes = 0;
        for (int pass = 0; pass < 3; pass++)
        {
            long passBefore = Allocated();
            Stopwatch watch = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                object result = evaluator.Parse(formula).value;
                if (result == null) { throw new InvalidOperationException(); }
            }

            watch.Stop();
            best = Math.Min(best, watch.Elapsed.TotalMilliseconds * 1000000.0 / iterations);
            bytes = Allocated() - passBefore;
        }

        return Row("legacy-vb", id, "parse+eval", iterations, best, bytes, value);
    }

    private static string MeasureEvalOnly(Eval3.opCode compiled, string id, int iterations, object value)
    {
        int warmup = Math.Min(iterations / 10 + 200, 20000);
        for (int i = 0; i < warmup; i++)
        {
            object warm = compiled.value;
            if (warm == null) { throw new InvalidOperationException(); }
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        double best = double.MaxValue;
        long bytes = 0;
        for (int pass = 0; pass < 3; pass++)
        {
            long passBefore = Allocated();
            Stopwatch watch = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                object result = compiled.value;
                if (result == null) { throw new InvalidOperationException(); }
            }

            watch.Stop();
            best = Math.Min(best, watch.Elapsed.TotalMilliseconds * 1000000.0 / iterations);
            bytes = Allocated() - passBefore;
        }

        return Row("legacy-vb", id, "eval-only", iterations, best, bytes, value);
    }

    private static string Row(string engine, string id, string mode, int iterations, double nanoseconds, long bytes, object value)
    {
        double ns = nanoseconds;
        return string.Join(",", new string[]
        {
            engine,
            id,
            mode,
            iterations.ToString(CultureInfo.InvariantCulture),
            ns.ToString("F1", CultureInfo.InvariantCulture),
            (1000000000.0 / ns).ToString("F0", CultureInfo.InvariantCulture),
            ((double)bytes / iterations).ToString("F1", CultureInfo.InvariantCulture),
            Convert.ToString(value, CultureInfo.InvariantCulture),
        });
    }

    private static long Allocated()
    {
        if (AllocMethod == null) { return 0; }
        return (long)AllocMethod.Invoke(null, null);
    }
}
