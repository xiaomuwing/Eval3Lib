using System.Diagnostics;
using Eval3;
using Eval3.Functions;

namespace Eval3.Tests;

public class EngineTests
{
    [Fact]
    public void NumericFastPathSkipsBoxing()
    {
        var engine = new Evaluator();
        engine.AddEnvironmentFunctions(typeof(MathLibrary));

        Assert.True(engine.Compile("1 + 2 * 3").HasNumericFastPath);
        Assert.True(engine.Compile("(1 + 2) * 3 - 4 / 2").HasNumericFastPath);
        Assert.True(engine.Compile("Round(3.5)").HasNumericFastPath);
        Assert.True(engine.Compile("Sum(1, 2, 3)").HasNumericFastPath);
        Assert.True(engine.Compile("PI * 2").HasNumericFastPath);

        var bag = new DictionaryVariableBag();
        bag.Set("price", 10d);
        engine.AddEnvironmentFunctions(bag);
        Assert.True(engine.Compile("price * 2").HasNumericFastPath);
        Assert.True(engine.Compile("Round(price * 2, 2)").HasNumericFastPath);

        var percent = new Evaluator(new EvaluatorOptions { PercentMode = PercentMode.PercentOf });
        Assert.True(percent.Compile("100 + 5%").HasNumericFastPath);
    }

    [Fact]
    public void FormulaCacheReusesParsedTrees()
    {
        var engine = new Evaluator();
        CompiledFormula first = engine.Compile("1+1");
        CompiledFormula second = engine.Compile("1+1");

        Assert.Same(first, second);
        Assert.Equal(1, engine.CachedFormulaCount);

        engine.ClearCache();
        Assert.Equal(0, engine.CachedFormulaCount);
    }

    [Fact]
    public void PercentModeSupportsLegacyCalculatorSemantics()
    {
        var engine = new Evaluator(new EvaluatorOptions { PercentMode = PercentMode.PercentOf });

        Assert.Equal(105d, engine.EvaluateNumber("100 + 5%"), 10);
        Assert.Equal(5d, engine.EvaluateNumber("100 * 5%"), 10);
        Assert.Equal(180d, engine.EvaluateNumber("200 - 10%"), 10);
        Assert.Equal(0.05d, engine.EvaluateNumber("5%"), 10);
    }

    [Fact]
    public void ParsesLargeFormulas()
    {
        var engine = new Evaluator();
        string formula = string.Join("+", Enumerable.Range(1, 500));

        Stopwatch stopwatch = Stopwatch.StartNew();
        double value = engine.EvaluateNumber(formula);
        stopwatch.Stop();

        Assert.Equal(125250d, value, 6);
        Assert.True(stopwatch.ElapsedMilliseconds < 500, $"解析 500 项公式耗时 {stopwatch.ElapsedMilliseconds} ms");
    }

    [Fact]
    public void SupportsParallelEvaluation()
    {
        var bag = new DictionaryVariableBag();
        bag.Set("price", 12.5);
        bag.Set("count", 8);
        var engine = new Evaluator();
        engine.AddEnvironmentFunctions(bag, typeof(MathLibrary));

        CompiledFormula formula = engine.Compile("Round(price * count * 1.13, 2)");
        double[] results = new double[2048];
        Parallel.For(0, results.Length, i => results[i] = formula.EvaluateNumber());

        Assert.All(results, value => Assert.Equal(113d, value, 10));
    }

    [Fact]
    public void ExpressionTreeFormatterDumpsNodes()
    {
        var engine = new Evaluator();
        Expression root = engine.Parse("1 + 2 * 3");

        string dump = ExpressionTreeFormatter.Dump(root);
        Assert.Contains("加法", dump, StringComparison.Ordinal);
        Assert.Contains("乘法", dump, StringComparison.Ordinal);
        Assert.Equal(5, ExpressionTreeFormatter.Flatten(root).Count);
    }

    [Fact]
    public void FunctionCatalogDescribesLibraries()
    {
        IReadOnlyList<FunctionDescriptor> functions = FunctionCatalog.Describe(typeof(MathLibrary));

        Assert.Contains(functions, f => f.Name == "Round");
        FunctionDescriptor round = functions.First(f => f.Name == "Round");
        Assert.Contains("digits", round.Signature, StringComparison.Ordinal);
        Assert.True(round.IsStatic);
        Assert.Equal("数学函数库", FunctionCatalog.GetLibraryName(typeof(MathLibrary)));
    }

    [Fact]
    public void EvaluatorUsesConfiguredSyntax()
    {
        var bag = new DictionaryVariableBag();
        bag.Set("values", new[] { 1d, 2d, 3d });

        var csharp = new Evaluator(ParserSyntax.CSharp);
        csharp.AddEnvironmentFunctions(bag);
        Assert.Equal(2d, csharp.EvaluateNumber("values[1]"), 10);
        // C# 语法下也兼容历史 VB 公式中的括号下标写法
        Assert.Equal(2d, csharp.EvaluateNumber("values(1)"), 10);

        var vbStyle = new Evaluator(ParserSyntax.VisualBasic);
        vbStyle.AddEnvironmentFunctions(bag);
        Assert.Equal(2d, vbStyle.EvaluateNumber("values(1)"), 10);
        Assert.Throws<ParserException>(() => vbStyle.Parse("values[1]"));
    }
}
