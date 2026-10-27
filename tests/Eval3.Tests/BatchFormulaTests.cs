namespace Eval3.Tests;

/// <summary>列式批量求值：结果必须与逐点标量求值一致，且支持数值 <c>if</c> 与数值函数。</summary>
public class BatchFormulaTests
{
    private static (Evaluator Engine, DictionaryVariableBag Bag) CreateEngine()
    {
        var bag = new DictionaryVariableBag();
        bag.Set("price", 12.5);
        bag.Set("count", 8d);

        var engine = new Evaluator();
        engine.AddEnvironmentFunctions(bag, typeof(Eval3.Functions.MathLibrary));
        return (engine, bag);
    }

    [Theory]
    [InlineData("price * 1.13")]
    [InlineData("Round(price * 1.13, 2)")]
    [InlineData("if(price > 10, price * 2, price + 1)")]
    [InlineData("price / 3 + count")]
    public void BatchMatchesScalar(string formula)
    {
        (Evaluator engine, DictionaryVariableBag bag) = CreateEngine();
        Assert.True(BatchFormula.TryCreate(engine, formula, out BatchFormula? batch));
        Assert.NotNull(batch);

        var price = bag.Find("price")!;
        var count = bag.Find("count")!;
        const int n = 1000;
        var priceColumn = new double[n];
        var countColumn = new double[n];
        for (int i = 0; i < n; i++)
        {
            priceColumn[i] = 5 + i * 0.01;
            countColumn[i] = i % 7;
        }

        var expected = new double[n];
        CompiledFormula compiled = engine.Compile(formula);
        for (int i = 0; i < n; i++)
        {
            price.Value = priceColumn[i];
            count.Value = countColumn[i];
            expected[i] = compiled.EvaluateNumber();
        }

        var columns = batch!.Variables
            .Select(variable => variable is IHasDescription described && described.Name == "count" ? countColumn : priceColumn)
            .ToArray();

        var actual = new double[n];
        batch.Evaluate(columns, actual, n);

        for (int i = 0; i < n; i++)
        {
            Assert.Equal(expected[i], actual[i]);
        }
    }

    [Fact]
    public void NonNumericFormulaIsRejected()
    {
        (Evaluator engine, _) = CreateEngine();
        Assert.False(BatchFormula.TryCreate(engine, "Upper(\"abc\")", out _));
        Assert.False(BatchFormula.TryCreate(engine, "\"a\" & price", out _));
    }
}
