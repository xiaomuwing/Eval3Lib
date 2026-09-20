using Eval3;
using Eval3.Functions;

namespace Eval3.Tests;

public class StringAndDateTests
{
    private static readonly Evaluator Engine = CreateEngine();

    private static Evaluator CreateEngine()
    {
        var engine = new Evaluator();
        engine.AddEnvironmentFunctions(typeof(MathLibrary));
        return engine;
    }

    [Theory]
    [InlineData("\"a\" + \"b\"", "ab")]
    [InlineData("\"a\" & 1", "a1")]
    [InlineData("1 + \"a\"", "1a")]
    [InlineData("'a' & 'b'", "ab")]
    [InlineData("\"a\"\"b\"", "a\"b")]
    [InlineData("\"合计：%[100 * 1.06]\"", "合计：106")]
    [InlineData("\"金额：%[Round(1000 / 3, 2)]\"", "金额：333.33")]
    public void EvaluatesStrings(string formula, string expected) =>
        Assert.Equal(expected, Engine.EvaluateToString(formula));

    [Fact]
    public void EvaluatesDateArithmetic()
    {
        Assert.Equal(
            new DateTime(2024, 2, 1),
            Engine.Evaluate("#2024/01/31# + 1"));

        Assert.Equal(9d, Engine.EvaluateNumber("#2024/02/10# - #2024/02/01#"), 10);
        Assert.Equal(new DateTime(2024, 1, 30), Engine.Evaluate("#2024/01/31# - 1"));
    }

    [Fact]
    public void DateFunctionsWork()
    {
        var engine = new Evaluator();
        engine.AddEnvironmentFunctions(typeof(DateLibrary));

        Assert.Equal(2024d, engine.EvaluateNumber("Year(#2024/05/06#)"), 10);
        Assert.Equal(5d, engine.EvaluateNumber("Month(#2024/05/06#)"), 10);
        Assert.Equal(29d, engine.EvaluateNumber("DaysBetween(#2024/02/01#, #2024/03/01#)"), 10);
        Assert.Equal(new DateTime(2024, 1, 31), engine.Evaluate("LastDayOfMonth(#2024/01/15#)"));
    }

    [Fact]
    public void InvalidDateReportsPosition()
    {
        ParserException error = Assert.Throws<ParserException>(() => Engine.Parse("1 + #abc#"));
        Assert.Equal("1 + #abc#", error.Formula);
        Assert.Equal(4, error.Position);
    }

    [Fact]
    public void MissingClosingQuoteReportsPosition()
    {
        ParserException error = Assert.Throws<ParserException>(() => Engine.Parse("\"abc"));
        Assert.Equal(0, error.Position);
    }
}
