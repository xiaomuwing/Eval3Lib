using Eval3;
using Eval3.Tests.Support;

namespace Eval3.Tests;

public class LogicalAndComparisonTests
{
    private static readonly Evaluator Engine = new();

    [Theory]
    [InlineData("3 > 2", true)]
    [InlineData("2 >= 2", true)]
    [InlineData("2 <> 2", false)]
    [InlineData("1 <= 1", true)]
    [InlineData("1 < 1", false)]
    [InlineData("3 = 3", true)]
    [InlineData("\"a\" = \"a\"", true)]
    [InlineData("\"A\" < \"a\"", true)]
    [InlineData("true = true", true)]
    [InlineData("false <> true", true)]
    [InlineData("#2024/01/01# < #2024/01/02#", true)]
    [InlineData("1 > 0 and 2 > 1", true)]
    [InlineData("1 > 0 or 2 < 1", true)]
    [InlineData("true and false", false)]
    [InlineData("not (1 > 2)", true)]
    public void EvaluatesComparisons(string formula, bool expected) =>
        Assert.Equal(expected, Engine.Evaluate(formula));

    [Fact]
    public void ComparisonHasLowerPrecedenceThanArithmetic()
    {
        Assert.Equal(true, Engine.Evaluate("1 + 2 * 3 > 6"));
    }

    [Fact]
    public void LogicalOperatorsShortCircuit()
    {
        var engine = new Evaluator();
        engine.AddEnvironmentFunctions(typeof(TestLibrary));

        TestLibrary.Calls = 0;
        Assert.Equal(false, engine.Evaluate("false and not (Counted() > 0)"));
        Assert.Equal(0, TestLibrary.Calls);

        Assert.Equal(true, engine.Evaluate("true or Counted() > 0"));
        Assert.Equal(0, TestLibrary.Calls);
    }

    [Fact]
    public void ConditionalExpressionIsLazy()
    {
        var engine = new Evaluator();
        engine.AddEnvironmentFunctions(typeof(TestLibrary));

        Assert.Equal(5d, engine.EvaluateNumber("if(false, Boom(), 5)"), 10);
        Assert.Equal(10d, engine.EvaluateNumber("if(1 > 0, 10, 20)"), 10);
        Assert.Equal("yes", engine.EvaluateToString("if(2 > 1, \"yes\", \"no\")"));
    }

    [Fact]
    public void ConditionalRequiresThreeArguments()
    {
        ParserException error = Assert.Throws<ParserException>(() => Engine.Parse("if(true, 1)"));
        Assert.Contains("3 个参数", error.Message, StringComparison.Ordinal);
    }
}
