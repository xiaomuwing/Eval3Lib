namespace Eval3.Tests;

/// <summary>decimal 模式：金额/税额走十进制定点，不进入 double 整树编译。</summary>
public class DecimalModeTests
{
    private static Evaluator Create()
    {
        var engine = new Evaluator(new EvaluatorOptions { NumericMode = Eval3.NumericMode.Decimal });
        engine.AddEnvironmentFunctions(
            typeof(Eval3.Functions.MathLibrary),
            typeof(Eval3.Functions.TextLibrary),
            typeof(Eval3.Functions.FinanceLibrary));
        return engine;
    }

    [Fact]
    public void DecimalArithmeticKeepsExactScale()
    {
        Evaluator engine = Create();

        object? value = engine.Evaluate("0.1 + 0.2");
        Assert.IsType<decimal>(value);
        Assert.Equal(0.3m, (decimal)value!);
        Assert.Equal("0.30", engine.EvaluateToString("0.1 + 0.2"));
        Assert.Equal(true, engine.Evaluate("0.1 + 0.2 = 0.3"));
    }

    [Fact]
    public void FinanceFunctionsUseDecimalOverloads()
    {
        Evaluator engine = Create();

        Assert.Equal(13.00m, (decimal)engine.Evaluate("Vat(100.00, 0.13)")!);
        Assert.Equal(113.00m, (decimal)engine.Evaluate("GrossUp(100.00, 0.13)")!);
        Assert.Equal(100m / 1.13m, (decimal)engine.Evaluate("NetOfTax(100.00, 0.13)")!);
    }

    [Fact]
    public void DecimalVariablesStayDecimal()
    {
        var bag = new DictionaryVariableBag();
        bag.Set("amount", 100.00m);
        bag.Set("rate", 0.13m);

        var engine = new Evaluator(new EvaluatorOptions { NumericMode = Eval3.NumericMode.Decimal });
        engine.AddEnvironmentFunctions(bag, typeof(Eval3.Functions.FinanceLibrary));

        object? value = engine.Evaluate("amount * rate");
        Assert.IsType<decimal>(value);
        Assert.Equal(13.0000m, (decimal)value!);
    }

    [Fact]
    public void DecimalFormulasDoNotUseDoubleFastPath()
    {
        Evaluator engine = Create();
        CompiledFormula compiled = engine.Compile("0.1 + 0.2");

        Assert.False(compiled.HasNumericFastPath);
        Assert.Equal(0.3m, (decimal)compiled.Evaluate()!);
    }

    [Fact]
    public void DecimalMathOverloadsWork()
    {
        Evaluator engine = Create();

        Assert.Equal(2.35m, (decimal)engine.Evaluate("Round(2.345, 2)")!);
        Assert.Equal(3m, (decimal)engine.Evaluate("Max(2, 3)")!);
        Assert.Equal(6.6m, (decimal)engine.Evaluate("Sum(1.1, 2.2, 3.3)")!);
    }
}
