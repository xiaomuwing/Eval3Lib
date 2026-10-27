namespace Eval3.Tests;

/// <summary>Phase 3/4：LRU 缓存、无 JIT 回退、运行时错误定位、补偿求和。</summary>
public class Phase34Tests
{
    [Fact]
    public void LruCacheEvictsLeastRecentlyUsed()
    {
        var engine = new Evaluator(new EvaluatorOptions { MaxCacheSize = 2 });
        engine.Compile("1 + 1");
        engine.Compile("2 + 2");

        // 访问 1+1，使其成为最近使用；再插入 3+3 应淘汰 2+2。
        Assert.True(engine.TryGetCachedFormula("1 + 1", out _));
        engine.Compile("3 + 3");

        Assert.True(engine.TryGetCachedFormula("1 + 1", out _));
        Assert.False(engine.TryGetCachedFormula("2 + 2", out _));
        Assert.Equal(2, engine.CachedFormulaCount);
    }

    [Fact]
    public void CacheCountersTrackHitsAndMisses()
    {
        var engine = new Evaluator();
        engine.Evaluate("1 + 2");
        engine.Evaluate("1 + 2");
        engine.Evaluate("1 + 2");

        Assert.Equal(2, engine.CacheHits);
        Assert.Equal(1, engine.CacheMisses);
    }

    [Fact]
    public void DisabledJitFallsBackToTreeWalk()
    {
        var engine = new Evaluator(new EvaluatorOptions { EnableJit = false });
        engine.AddEnvironmentFunctions(typeof(Eval3.Functions.MathLibrary));

        CompiledFormula compiled = engine.Compile("Round(1.234, 2) * 3");
        Assert.False(compiled.HasNumericFastPath);
        Assert.Equal(3.69, compiled.EvaluateNumber(), 6);
        Assert.Equal("3.69", compiled.EvaluateToString());
    }

    [Fact]
    public void RuntimeErrorReportsFaultingNode()
    {
        var bag = new DictionaryVariableBag();
        bag.Set("arr", new[] { 1d, 2d });

        var engine = new Evaluator();
        engine.AddEnvironmentFunctions(bag);

        FormulaRuntimeException exception = Assert.Throws<FormulaRuntimeException>(
            () => engine.EvaluateWithDiagnostics("arr[5]"));
        Assert.Equal("arr[5]", exception.NodeSource);
        Assert.True(exception.Position >= 0);
    }

    [Fact]
    public void SumUsesCompensatedAccumulation()
    {
        const int n = 1_000_000;
        var values = new double[n];
        Array.Fill(values, 0.1);

        double sum = Eval3.Functions.MathLibrary.Sum(values);
        Assert.Equal(100_000d, sum, 6);
    }
}
