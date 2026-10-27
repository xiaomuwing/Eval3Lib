namespace Eval3.Tests;

/// <summary>Integer 模式：整数字面量与 + - * % 走 long，溢出提升 decimal，除法返回 decimal。</summary>
public class IntegerModeTests
{
    private static Evaluator Create()
    {
        var engine = new Evaluator(new EvaluatorOptions { NumericMode = Eval3.NumericMode.Integer });
        engine.AddEnvironmentFunctions(typeof(Eval3.Functions.MathLibrary));
        return engine;
    }

    [Fact]
    public void IntegerArithmeticStaysLong()
    {
        Evaluator engine = Create();

        object? value = engine.Evaluate("1 + 2 * 3");
        Assert.IsType<long>(value);
        Assert.Equal(7L, (long)value!);
        Assert.Equal("7", engine.EvaluateToString("1 + 2 * 3"));

        Assert.Equal(1L, (long)engine.Evaluate("7 % 3")!);
        Assert.True((bool)engine.Evaluate("1 + 2 * 3 = 7")!);
    }

    [Fact]
    public void IntegerDivisionReturnsDecimal()
    {
        Evaluator engine = Create();

        object? value = engine.Evaluate("10 / 4");
        Assert.IsType<decimal>(value);
        Assert.Equal(2.5m, (decimal)value!);
        Assert.Equal("2.50", engine.EvaluateToString("10 / 4"));
    }

    [Fact]
    public void IntegerOverflowPromotesToDecimal()
    {
        Evaluator engine = Create();

        object? value = engine.Evaluate("9223372036854775807 + 1");
        Assert.IsType<decimal>(value);
        Assert.Equal(9223372036854775808m, (decimal)value!);
    }

    [Fact]
    public void IntegerFormulasDoNotUseDoubleFastPath()
    {
        Evaluator engine = Create();
        CompiledFormula compiled = engine.Compile("1 + 2 * 3");

        Assert.False(compiled.HasNumericFastPath);
        Assert.Equal(7L, (long)compiled.Evaluate()!);
    }
}
