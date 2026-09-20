using Eval3;
using Eval3.Functions;

namespace Eval3.Tests;

public class FunctionTests
{
    private static Evaluator CreateEngine()
    {
        var engine = new Evaluator();
        engine.AddEnvironmentFunctions(
            typeof(MathLibrary),
            typeof(TextLibrary),
            typeof(DateLibrary),
            typeof(FinanceLibrary));
        return engine;
    }

    [Theory]
    [InlineData("Abs(-3)", 3d)]
    [InlineData("Round(3.14159, 2)", 3.14d)]
    [InlineData("Round(3.6)", 4d)]
    [InlineData("RoundTo(12.3456, 0.01)", 12.35d)]
    [InlineData("FloorTo(12.3456, 0.01)", 12.34d)]
    [InlineData("Sum(1, 2, 3)", 6d)]
    [InlineData("Sum()", 0d)]
    [InlineData("Average(1, 2, 3, 4)", 2.5d)]
    [InlineData("Pow(2, 10)", 1024d)]
    [InlineData("Round(Sqrt(16), 1)", 4d)]
    [InlineData("Max(Min(10, 3), 2)", 3d)]
    [InlineData("Vat(100, 0.13)", 13d)]
    [InlineData("NetOfTax(113, 0.13)", 100d)]
    [InlineData("Npv(0, 100, 200)", 300d)]
    public void EvaluatesLibraryFunctions(string formula, double expected) =>
        Assert.Equal(expected, CreateEngine().EvaluateNumber(formula), 8);

    [Theory]
    [InlineData("PI", Math.PI)]
    [InlineData("E", Math.E)]
    public void ReadsStaticProperties(string formula, double expected) =>
        Assert.Equal(expected, CreateEngine().EvaluateNumber(formula), 10);

    [Theory]
    [InlineData("Upper(\"abc\")", "ABC")]
    [InlineData("Left(\"abcdef\", 3)", "abc")]
    [InlineData("Mid(\"abcdef\", 2, 3)", "cde")]
    [InlineData("Repeat(\"ab\", 2)", "abab")]
    [InlineData("Format(1234.5, \"#,##0.00\")", "1,234.50")]
    public void EvaluatesTextFunctions(string formula, string expected) =>
        Assert.Equal(expected, CreateEngine().EvaluateToString(formula));

    [Fact]
    public void SupportsInstanceLibraries()
    {
        var engine = new Evaluator();
        engine.AddEnvironmentFunctions(new Support.TestLibrary());

        Assert.Equal(3d, engine.EvaluateNumber("Increment(2)"), 10);
        Assert.Equal(2.5d, engine.EvaluateNumber("Factor"), 10);
    }

    [Fact]
    public void CombinesVariablesAndFunctions()
    {
        var bag = new DictionaryVariableBag();
        bag.Set("area", 128.456);
        var engine = CreateEngine();
        engine.AddEnvironmentFunctions(bag);

        Assert.Equal(128.46d, engine.EvaluateNumber("Round(area, 2)"), 10);
        Assert.Equal(19.27d, engine.EvaluateNumber("RoundTo(area * 0.15, 0.01)"), 10);
    }

    [Fact]
    public void NestedLibraryLookupFollowsInheritanceChain()
    {
        var engine = new Evaluator();
        engine.AddEnvironmentFunctions(new ChildLibrary());

        Assert.Equal(4d, engine.EvaluateNumber("Parent(2)"), 10);
        Assert.Equal(5d, engine.EvaluateNumber("Child(2)"), 10);
    }

    private sealed class ParentLibrary
    {
        public double Parent(double value) => value * 2;
    }

    private sealed class ChildLibrary : IInheritedFunctions
    {
        private readonly ParentLibrary _parent = new();

        public object? InheritedFunctions => _parent;

        public double Child(double value) => value + 3;
    }
}
