using System.Diagnostics;

namespace Eval3.Benchmarks;

/// <summary>单次测量结果。</summary>
public sealed record BenchmarkResult(
    string Name,
    int Iterations,
    double NanosecondsPerOperation,
    double OperationsPerSecond,
    double BytesPerOperation);

/// <summary>极简基准测试运行器（不依赖第三方库，离线可用）。</summary>
public static class Benchmark
{
    /// <summary>预热时长：分层编译 / PGO 需要足够时间才会稳定，按调用次数预热对纳秒级操作远远不够。</summary>
    private const int WarmupMilliseconds = 300;

    /// <summary>测量轮数，取最快一轮（与 <c>tools\Compare-Performance.ps1</c> 口径一致）。</summary>
    private const int Rounds = 3;

    /// <summary>测量 <paramref name="action"/> 的单次耗时与分配量，取多轮最优值。</summary>
    public static BenchmarkResult Measure(string name, int iterations, Action action)
    {
        var warmup = Stopwatch.StartNew();
        do
        {
            for (int i = 0; i < 512; i++)
            {
                action();
            }
        }
        while (warmup.ElapsedMilliseconds < WarmupMilliseconds);

        BenchmarkResult? best = null;
        for (int round = 0; round < Rounds; round++)
        {
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

            double nanoseconds = stopwatch.Elapsed.TotalNanoseconds / iterations;
            if (best is null || nanoseconds < best.NanosecondsPerOperation)
            {
                best = new BenchmarkResult(
                    name,
                    iterations,
                    nanoseconds,
                    1_000_000_000d / nanoseconds,
                    (double)allocated / iterations);
            }
        }

        return best!;
    }
}
