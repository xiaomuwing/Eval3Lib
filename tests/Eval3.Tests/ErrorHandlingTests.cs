using Eval3;

namespace Eval3.Tests;

public class ErrorHandlingTests
{
    private static readonly Evaluator Engine = new();

    [Theory]
    [InlineData("1 + * 2", 4)]
    [InlineData("(1+2", 4)]
    [InlineData("1 + 2)", 5)]
    [InlineData("1 2", 2)]
    [InlineData("@", 0)]
    public void ReportsErrorPosition(string formula, int expectedPosition)
    {
        ParserException error = Assert.Throws<ParserException>(() => Engine.Parse(formula));
        Assert.Equal(formula, error.Formula);
        Assert.Equal(expectedPosition, error.Position);
    }

    [Fact]
    public void ReportsUnexpectedTrailingContent()
    {
        ParserException error = Assert.Throws<ParserException>(() => Engine.Parse("1 + 2 +"));
        Assert.Contains("意外的记号", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TryEvaluateCapturesErrors()
    {
        Assert.False(Engine.TryEvaluate("1 + 未知", out object? value, out EvalException? error));
        Assert.Null(value);
        Assert.IsType<ParserException>(error);

        Assert.True(Engine.TryEvaluate("1 + 1", out object? ok, out EvalException? none));
        Assert.Equal(2d, ok);
        Assert.Null(none);
    }

    [Fact]
    public void RuntimeErrorsAreWrapped()
    {
        var bag = new DictionaryVariableBag();
        bag.Set("values", new[] { 1d, 2d });
        var engine = new Evaluator();
        engine.AddEnvironmentFunctions(bag);

        Assert.False(engine.TryEvaluate("values[9]", out _, out EvalException? error));
        Assert.IsType<FormulaRuntimeException>(error);
    }

    [Fact]
    public void DivisionByZeroYieldsInfinityLikeDoubleSemantics()
    {
        Assert.True(double.IsPositiveInfinity(Engine.EvaluateNumber("1 / 0")));
    }
}
