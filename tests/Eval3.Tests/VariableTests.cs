using Eval3;

namespace Eval3.Tests;

public class VariableTests
{
    private static Evaluator CreateEngine(out DictionaryVariableBag bag)
    {
        bag = new DictionaryVariableBag();
        var engine = new Evaluator();
        engine.AddEnvironmentFunctions(bag);
        return engine;
    }

    [Fact]
    public void ResolvesChineseVariableNames()
    {
        Evaluator engine = CreateEngine(out DictionaryVariableBag bag);
        bag.Set("单价", 12.5);
        bag.Set("数量", 4);

        Assert.Equal(50d, engine.EvaluateNumber("单价 * 数量"), 10);
    }

    [Fact]
    public void IsCaseInsensitiveByDefault()
    {
        Evaluator engine = CreateEngine(out DictionaryVariableBag bag);
        bag.Set("Price", 10);

        Assert.Equal(20d, engine.EvaluateNumber("price * 2"), 10);
    }

    [Fact]
    public void CaseSensitiveEngineRejectsWrongCasing()
    {
        var bag = new DictionaryVariableBag(caseSensitive: true);
        var engine = new Evaluator(new EvaluatorOptions { CaseSensitive = true });
        engine.AddEnvironmentFunctions(bag);
        bag.Set("Price", 10);

        Assert.Equal(10d, engine.EvaluateNumber("Price"), 10);
        Assert.Throws<ParserException>(() => engine.Parse("price"));
    }

    [Fact]
    public void VariableValuesAreReadAtEvaluationTime()
    {
        Evaluator engine = CreateEngine(out DictionaryVariableBag bag);
        EvalVariable price = bag.Set("price", 10);

        Assert.Equal(10d, engine.EvaluateNumber("price"), 10);
        price.Value = 25d;
        Assert.Equal(25d, engine.EvaluateNumber("price"), 10);
    }

    [Fact]
    public void UnknownVariableReportsName()
    {
        Evaluator engine = CreateEngine(out _);
        ParserException error = Assert.Throws<ParserException>(() => engine.Parse("1 + 未知变量"));
        Assert.Contains("未知变量", error.Message, StringComparison.Ordinal);
        Assert.Equal(4, error.Position);
    }

    [Fact]
    public void SupportsArrayIndexingWithCSharpSyntax()
    {
        Evaluator engine = CreateEngine(out DictionaryVariableBag bag);
        bag.Set("values", new[] { 3d, 6d, 9d });

        Assert.Equal(6d, engine.EvaluateNumber("values[1]"), 10);
        Assert.Equal(12d, engine.EvaluateNumber("values[1] * 2"), 10);
    }

    [Fact]
    public void SupportsArrayIndexingWithVbSyntax()
    {
        var bag = new DictionaryVariableBag();
        var engine = new Evaluator(ParserSyntax.VisualBasic);
        engine.AddEnvironmentFunctions(bag);
        bag.Set("values", new[] { 3d, 6d, 9d });

        Assert.Equal(9d, engine.EvaluateNumber("values(2)"), 10);
    }

    [Fact]
    public void SupportsMemberAccessOnVariableValues()
    {
        Evaluator engine = CreateEngine(out DictionaryVariableBag bag);
        bag.Set("name", "eval3");

        Assert.Equal(5d, engine.EvaluateNumber("name.Length"), 10);
        Assert.Equal("EVAL3", engine.EvaluateToString("name.ToUpper()"));
        Assert.Equal("va", engine.EvaluateToString("name.Substring(1, 2)"));
    }

    [Fact]
    public void SupportsNestedVariableBags()
    {
        var inner = new DictionaryVariableBag();
        inner.Set("material", 120d);
        var outer = new ChildBag(inner);

        var engine = new Evaluator();
        engine.AddEnvironmentFunctions(outer);

        Assert.Equal(240d, engine.EvaluateNumber("material * 2"), 10);
    }

    [Fact]
    public void VariablesWinOverContainerMembers()
    {
        Evaluator engine = CreateEngine(out DictionaryVariableBag bag);
        engine.AddEnvironmentFunctions(typeof(Functions.MathLibrary));
        bag.Set("count", 8);
        bag.Set("price", 12.5);

        // 变量名 count 不应被 DictionaryVariableBag.Count 属性遮蔽
        Assert.Equal(100d, engine.EvaluateNumber("price * count"), 10);
        Assert.Equal(113d, engine.EvaluateNumber("round(price * count * 1.13)"), 10);
    }

    [Fact]
    public void SupportsPlainBusinessObjectsAsVariableSources()
    {
        var context = new OrderContext { Amount = 1000d };
        var engine = new Evaluator();
        engine.AddEnvironmentFunctions(context);

        Assert.Equal(1130d, engine.EvaluateNumber("Amount * (1 + Rate)"), 10);
        Assert.Equal(130d, engine.EvaluateNumber("Amount * Rate"), 10);
    }

    private sealed class OrderContext
    {
        public double Amount { get; set; }

        public double Rate = 0.13;
    }

    private sealed class ChildBag : IVariableBag, IInheritedFunctions
    {
        private readonly IVariableBag _parent;

        public ChildBag(IVariableBag parent) => _parent = parent;

        public object? InheritedFunctions => _parent;

        public ITypedValue? GetVariable(string name) => null;
    }
}
