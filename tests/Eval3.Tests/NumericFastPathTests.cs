using System.Globalization;
using Eval3.Functions;

namespace Eval3.Tests;

/// <summary>
/// 数值快路径（免装箱）的覆盖范围与一致性：
/// 变量与数值函数调用都应进入快路径，且结果必须与逐节点求值逐位相同。
/// </summary>
public class NumericFastPathTests
{
    private static Evaluator CreateEngine()
    {
        var bag = new DictionaryVariableBag();
        bag.Set("price", 12.5);
        bag.Set("count", 8d);
        bag.Set("rate", 0.13);

        var engine = new Evaluator();
        engine.AddEnvironmentFunctions(bag, typeof(MathLibrary), typeof(TextLibrary), typeof(DateLibrary));
        return engine;
    }

    [Theory]
    [InlineData("price * count * (1 + rate)")]
    [InlineData("price * count * (1 + rate) / 3")]
    [InlineData("Round(price * count * (1 + rate) / 3, 2)")]
    [InlineData("Round(price, 2)")]
    [InlineData("Round(Round(price * count, 2) * 1.13, 2)")]
    [InlineData("RoundTo(price * count, 0.01)")]
    [InlineData("Abs(-price)")]
    [InlineData("Max(price, count)")]
    [InlineData("Min(price, count) * 2")]
    [InlineData("Sqrt(price * price + count * count)")]
    [InlineData("Pow(price, 2) + 1")]
    [InlineData("Sum(1, 2, 3, 4, 5)")]
    [InlineData("Sum()")]
    [InlineData("PI * price")]
    public void FastPathMatchesTreeEvaluation(string formula)
    {
        CompiledFormula compiled = CreateEngine().Compile(formula);

        Assert.True(compiled.HasNumericFastPath, $"{formula} 应走数值快路径");

        // EvaluateNumber 走编译后的委托，Evaluate 始终逐节点求值，两者必须一致。
        double throughFastPath = compiled.EvaluateNumber();
        double throughTree = ValueConvert.AsDouble(compiled.Evaluate());

        Assert.Equal(throughTree, throughFastPath);
    }

    [Theory]
    [InlineData("\"a\" & \"b\"")]
    [InlineData("1 < 2")]
    [InlineData("Upper(\"abc\")")]
    [InlineData("Format(1234.5, \"#,##0.00\")")]
    [InlineData("if(1 < 2, 1, 2)")]
    [InlineData("\"合计：%[1 + 1]\"")]
    [InlineData("#2024-01-02# - #2024-01-01#")]
    public void NonNumericFormulasStayOnTreePath(string formula)
    {
        CompiledFormula compiled = CreateEngine().Compile(formula);

        Assert.False(compiled.HasNumericFastPath, $"{formula} 不应走数值快路径");
    }

    /// <summary>日期变量参与运算时语义与数值不同（相差天数），必须留在逐节点求值路径上。</summary>
    [Fact]
    public void DateVariablesStayOnTreePath()
    {
        var bag = new DictionaryVariableBag();
        bag.Set("from", new DateTime(2024, 1, 1));
        bag.Set("to", new DateTime(2024, 1, 11));

        var engine = new Evaluator();
        engine.AddEnvironmentFunctions(bag);
        CompiledFormula compiled = engine.Compile("to - from");

        Assert.False(compiled.HasNumericFastPath);
        Assert.Equal(10d, compiled.EvaluateNumber(), 10);
    }

    /// <summary>
    /// 整型形参的转换必须与 <c>ValueConvert.ConvertTo</c> 一致（银行家舍入，而不是截断）。
    /// </summary>
    [Theory]
    [InlineData(2.5)]
    [InlineData(2.6)]
    [InlineData(-2.6)]
    [InlineData(0.4)]
    [InlineData(3.5)]
    public void IntegerParameterConversionMatchesTree(double digits)
    {
        string literal = digits.ToString(CultureInfo.InvariantCulture);
        CompiledFormula compiled = CreateEngine().Compile($"Round(1.23456789, {literal})");

        Assert.True(compiled.HasNumericFastPath);
        Assert.Equal(ValueConvert.AsDouble(compiled.Evaluate()), compiled.EvaluateNumber());
    }

    /// <summary>变量为 null 时，实参转换仍应抛出与反射调用一致的异常，而不是静默当成 0。</summary>
    [Fact]
    public void NullVariableArgumentKeepsConversionError()
    {
        var bag = new DictionaryVariableBag();
        bag.Set("price", 12.5);

        var engine = new Evaluator();
        engine.AddEnvironmentFunctions(bag, typeof(MathLibrary));
        CompiledFormula compiled = engine.Compile("Round(price, 2)");

        Assert.True(compiled.HasNumericFastPath);

        bag.Find("price")!.Value = null;

        Assert.Throws<InvalidCastException>(() => compiled.EvaluateNumber());
        Assert.Throws<InvalidCastException>(() => compiled.Evaluate());
    }

    /// <summary>变量改值后，编译好的数值委托必须读到最新值。</summary>
    [Fact]
    public void FastPathReadsUpdatedVariableValue()
    {
        var bag = new DictionaryVariableBag();
        bag.Set("price", 10d);
        bag.Set("count", 2d);

        var engine = new Evaluator();
        engine.AddEnvironmentFunctions(bag);
        CompiledFormula compiled = engine.Compile("price * count");

        Assert.Equal(20d, compiled.EvaluateNumber(), 10);

        bag.Find("count")!.Value = 5d;

        Assert.Equal(50d, compiled.EvaluateNumber(), 10);
        Assert.Equal(ValueConvert.AsDouble(compiled.Evaluate()), compiled.EvaluateNumber());
    }

    /// <summary>对象成员（属性 / 字段 / 实例方法）参与数值运算时同样走快路径。</summary>
    [Fact]
    public void ObjectMemberAccessUsesFastPath()
    {
        var engine = new Evaluator();
        engine.AddEnvironmentFunctions(new Support.TestLibrary());

        CompiledFormula property = engine.Compile("Factor * 2");
        Assert.True(property.HasNumericFastPath);
        Assert.Equal(5d, property.EvaluateNumber(), 10);

        CompiledFormula method = engine.Compile("Increment(2) * 10");
        Assert.True(method.HasNumericFastPath);
        Assert.Equal(30d, method.EvaluateNumber(), 10);
    }

    /// <summary>
    /// 实参与形参同为非 double 数值类型（如 decimal → decimal）时不能走快路径：
    /// 经过一次 double 往返会丢精度，必须留在逐节点求值路径上。
    /// </summary>
    [Fact]
    public void DecimalArgumentsStayOnTreePathToKeepPrecision()
    {
        const decimal amount = 0.1234567890123456789012345678m;
        var bag = new DictionaryVariableBag();
        bag.Set("amount", amount);

        var engine = new Evaluator();
        engine.AddEnvironmentFunctions(bag, typeof(DecimalProbe));
        CompiledFormula compiled = engine.Compile("AsNumber(amount)");

        Assert.False(compiled.HasNumericFastPath);
        Assert.Equal((double)amount, compiled.EvaluateNumber());
        Assert.Equal(ValueConvert.AsDouble(compiled.Evaluate()), compiled.EvaluateNumber());
    }
}

/// <summary>decimal 精度回归用的函数库。</summary>
public static class DecimalProbe
{
    /// <summary>把 decimal 原样换算为 double（取值只取决于入参精度）。</summary>
    public static double AsNumber(decimal value) => (double)value;
}
