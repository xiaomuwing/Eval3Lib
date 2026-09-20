using Eval3.Thermo;
using XSteamNET;

namespace Eval3.Benchmarks;

/// <summary>
/// 水蒸汽物性库性能对照（纯托管 IAPWS-IF97 实现）。
/// <c>dotnet run --project src/Eval3.Benchmarks -c Release -- --thermo</c>
/// </summary>
/// <remarks>
/// 参照值来自历史 WASPCN 库（413 KB 版原生 DLL）在同一台机器 32 位进程中的实测：
/// P2T(1)=303 ns、P2HG(1)=537 ns、PT2H(1,300)=271 ns、PT2U(1,300)=584 ns、
/// T2P(300)=39 ns、PH2T(1,3051.2)=2715 ns。
/// </remarks>
public static class ThermoBenchmarks
{
    private static readonly XSteam Steam = new();

    /// <summary>运行对照基准。</summary>
    public static void Run(int iterations)
    {
        Console.WriteLine($".NET {Environment.Version}，{Environment.ProcessorCount} 逻辑核");
        Console.WriteLine($"每组迭代 {iterations:N0} 次（取 3 轮最快值）；WASPCN 为同机 32 位原生库实测");
        Console.WriteLine();

        (string Name, double WaspcnNs, Action Call)[] cases =
        [
            ("P2T(1) 饱和温度", 303.3, () => _ = ThermoLibrary.P2T(1d)),
            ("P2HG(1) 饱和汽比焓", 537.4, () => _ = ThermoLibrary.P2HG(1d)),
            ("PT2H(1,300) 过热汽比焓", 271.3, () => _ = ThermoLibrary.PT2H(1d, 300d)),
            ("PT2U(1,300) 运动粘度", 584.2, () => _ = ThermoLibrary.PT2U(1d, 300d)),
            ("T2P(300) 饱和压力", 38.6, () => _ = ThermoLibrary.T2P(300d)),
            ("PH2T(1,3051.2) 反算温度", 2715.1, () => _ = ThermoLibrary.PH2T(1d, 3051.2d)),
            ("PS2T(1,7.1229) 反算温度", 2715.1, () => _ = ThermoLibrary.PS2T(1d, 7.1229d)),
        ];

        Console.WriteLine("| 函数 | XSteamNET（纯托管） | 每次分配 | WASPCN（原生） | 相对 |");
        Console.WriteLine("| --- | ---: | ---: | ---: | ---: |");
        foreach ((string name, double waspcn, Action call) in cases)
        {
            BenchmarkResult result = Benchmark.Measure(name, iterations, call);
            Console.WriteLine(
                $"| {name} | {result.NanosecondsPerOperation:F0} ns | {result.BytesPerOperation:F0} B | "
                + $"{waspcn:F0} ns | {waspcn / result.NanosecondsPerOperation:F2}× |");
        }

        Console.WriteLine();
        Console.WriteLine("分层：底层原生调用 vs ThermoLibrary 封装（含单位换算与范围校验）");
        Console.WriteLine("| 环节 | 耗时 | 分配 |");
        Console.WriteLine("| --- | ---: | ---: |");
        Print("XSteam.h_pt(1, 573.15)", iterations, () => _ = Steam.h_pt(1d, 573.15d));
        Print("ThermoLibrary.PT2H(1, 300)", iterations, () => _ = ThermoLibrary.PT2H(1d, 300d));
        Print("XSteam.tsat_p(1)", iterations, () => _ = Steam.tsat_p(1d));
        Print("ThermoLibrary.P2T(1)", iterations, () => _ = ThermoLibrary.P2T(1d));

        Console.WriteLine();
        var engine = new Evaluator();
        engine.AddEnvironmentFunctions(typeof(ThermoLibrary));
        CompiledFormula formula = engine.Compile("PT2H(1, 300) - P2HG(0.1)");
        BenchmarkResult formulaResult = Benchmark.Measure(
            "formula", iterations, () => _ = formula.EvaluateNumber());
        Console.WriteLine(
            $"Eval3 公式 `PT2H(1, 300) - P2HG(0.1)`：{formulaResult.NanosecondsPerOperation:F0} ns/次，"
            + $"{formulaResult.BytesPerOperation:F0} B/次，结果 {formula.EvaluateNumber():F4}");

        int total = Math.Min(iterations * 4, 200_000);
        double parallelThroughput = MeasureParallel(total, () => _ = formula.EvaluateNumber());
        double serialThroughput = 1e9 / formulaResult.NanosecondsPerOperation;
        Console.WriteLine(
            $"并行吞吐（{Environment.ProcessorCount} 核）：{parallelThroughput:N0} 次/秒，"
            + $"单线程 {serialThroughput:N0} 次/秒，加速比 {parallelThroughput / serialThroughput:F2}×");
    }

    private static void Print(string name, int iterations, Action action)
    {
        BenchmarkResult result = Benchmark.Measure(name, iterations, action);
        Console.WriteLine($"| {name} | {result.NanosecondsPerOperation:F0} ns | {result.BytesPerOperation:F0} B |");
    }

    private static double MeasureParallel(int total, Action action)
    {
        for (int i = 0; i < 2_000; i++)
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
}
