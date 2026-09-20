using Eval3;

namespace Eval3.Tests;

public class ArithmeticTests
{
    private static readonly Evaluator Engine = new();

    [Theory]
    [InlineData("1+2*3", 7d)]
    [InlineData("(1+2)*3", 9d)]
    [InlineData("10/4", 2.5d)]
    [InlineData("10 % 3", 1d)]
    [InlineData("-5 + 3", -2d)]
    [InlineData("3--2", 5d)]
    [InlineData("+7", 7d)]
    [InlineData("2.5 * .4", 1d)]
    [InlineData("1.5e2 + 0.5", 150.5d)]
    [InlineData("  12  ", 12d)]
    [InlineData("-(2+3)*4", -20d)]
    [InlineData("1 - - - 1", 0d)]
    [InlineData("100 / 5 / 2", 10d)]
    [InlineData("7 % 4 % 2", 1d)]
    public void EvaluatesArithmetic(string formula, double expected) =>
        Assert.Equal(expected, Engine.EvaluateNumber(formula), 10);

    [Fact]
    public void EvaluatesNestedParentheses()
    {
        Assert.Equal(33d, Engine.EvaluateNumber("((1+2)*(3+4))+12"), 10);
    }

    [Fact]
    public void EmptyFormulaYieldsEmptyString()
    {
        Assert.Equal(string.Empty, Engine.EvaluateToString(string.Empty));
        Assert.Equal(string.Empty, Engine.EvaluateToString("   "));
    }

    [Fact]
    public void ResultFormattingKeepsLegacyBehavior()
    {
        Assert.Equal("1,234.50", Engine.EvaluateToString("1234.5"));
        Assert.Equal("1,235", Engine.EvaluateToString("1234.5 + 0.5"));
    }
}
