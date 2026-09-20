using System.Diagnostics;

namespace Eval3.WinForms;

/// <summary>一次性能测量结果。</summary>
public sealed record BenchmarkMeasurement(
    string Name,
    int Iterations,
    double NanosecondsPerOperation,
    double OperationsPerSecond,
    double BytesPerOperation,
    string Note);

/// <summary>极简基准测试运行器（与命令行工具使用同一套测量方式）。</summary>
internal static class BenchmarkRunner
{
    public static BenchmarkMeasurement Measure(string name, int iterations, Action action, string note = "")
    {
        int warmup = Math.Clamp(iterations / 10, 500, 50_000);
        for (int i = 0; i < warmup; i++)
        {
            action();
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();

        long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var stopwatch = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            action();
        }

        stopwatch.Stop();
        long allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        double nanoseconds = stopwatch.Elapsed.TotalNanoseconds / Math.Max(1, iterations);
        return new BenchmarkMeasurement(
            name,
            iterations,
            nanoseconds,
            1_000_000_000d / Math.Max(nanoseconds, double.Epsilon),
            (double)allocated / Math.Max(1, iterations),
            note);
    }

    /// <summary>测量并行吞吐。</summary>
    public static BenchmarkMeasurement MeasureParallel(string name, int totalOperations, Action action)
    {
        for (int i = 0; i < Math.Min(5_000, totalOperations / 10); i++)
        {
            action();
        }

        var stopwatch = Stopwatch.StartNew();
        Parallel.For(0, totalOperations, _ => action());
        stopwatch.Stop();

        double nanoseconds = stopwatch.Elapsed.TotalNanoseconds / Math.Max(1, totalOperations);
        return new BenchmarkMeasurement(
            name,
            totalOperations,
            nanoseconds,
            1_000_000_000d / Math.Max(nanoseconds, double.Epsilon),
            0d,
            $"并行度 {Environment.ProcessorCount}");
    }
}
