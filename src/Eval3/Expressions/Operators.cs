namespace Eval3;

/// <summary>二元运算符。</summary>
internal abstract class BinaryOperator
{
    protected BinaryOperator(string symbol, string name, EvalType resultType)
    {
        Symbol = symbol;
        Name = name;
        ResultType = resultType;
    }

    /// <summary>运算符符号。</summary>
    public string Symbol { get; }

    /// <summary>运算符名称。</summary>
    public string Name { get; }

    /// <summary>结果类型。</summary>
    public EvalType ResultType { get; }

    /// <summary>执行运算。</summary>
    public abstract object? Apply(object? left, object? right);

    /// <summary>能否编译为纯数值委托。</summary>
    public virtual bool TryBuildNumeric(out Func<double, double, double>? apply)
    {
        apply = null;
        return false;
    }
}

/// <summary>数值二元运算。</summary>
internal sealed class NumericOperator : BinaryOperator
{
    private readonly Func<double, double, double> _apply;

    public NumericOperator(string symbol, string name, Func<double, double, double> apply)
        : base(symbol, name, EvalType.Number) => _apply = apply;

    public override object? Apply(object? left, object? right) =>
        _apply(ValueConvert.AsDouble(left), ValueConvert.AsDouble(right));

    /// <summary>编译期常量折叠用：直接对两个 double 求值。</summary>
    internal double Apply(double left, double right) => _apply(left, right);

    public override bool TryBuildNumeric(out Func<double, double, double>? apply)
    {
        apply = _apply;
        return true;
    }
}

/// <summary>decimal 二元运算（decimal 模式）。</summary>
internal sealed class DecimalOperator : BinaryOperator
{
    private readonly Func<decimal, decimal, decimal> _apply;

    public DecimalOperator(string symbol, string name, Func<decimal, decimal, decimal> apply)
        : base(symbol, name, EvalType.Number) => _apply = apply;

    public override object? Apply(object? left, object? right) =>
        _apply(ValueConvert.ToDecimal(left), ValueConvert.ToDecimal(right));
}

/// <summary>整数运算种类。</summary>
internal enum LongOperation
{
    Add,
    Subtract,
    Multiply,
    Modulo,
}

/// <summary>整数二元运算（Integer 模式）：checked long，溢出提升为 decimal。</summary>
internal sealed class LongOperator : BinaryOperator
{
    private readonly LongOperation _operation;

    public LongOperator(string symbol, string name, LongOperation operation)
        : base(symbol, name, EvalType.Number) => _operation = operation;

    public override object? Apply(object? left, object? right)
    {
        long a = Convert.ToInt64(left, System.Globalization.CultureInfo.InvariantCulture);
        long b = Convert.ToInt64(right, System.Globalization.CultureInfo.InvariantCulture);

        try
        {
            return _operation switch
            {
                LongOperation.Add => checked(a + b),
                LongOperation.Subtract => checked(a - b),
                LongOperation.Multiply => checked(a * b),
                _ => a % b,
            };
        }
        catch (OverflowException)
        {
            // 溢出时提升为 decimal，保持数值正确；结果类型变为 decimal（与整树编译互斥）。
            return _operation switch
            {
                LongOperation.Add => (decimal)a + b,
                LongOperation.Subtract => (decimal)a - b,
                LongOperation.Multiply => (decimal)a * b,
                _ => (decimal)a % b,
            };
        }
    }
}

/// <summary>时间运算种类。</summary>
internal enum DateOperatorKind
{
    AddDays,
    SubtractDays,
    Difference,
}

/// <summary>时间运算。</summary>
internal sealed class DateOperator : BinaryOperator
{
    private readonly Func<double, double, double>? _numericDays;
    private readonly Func<DateTime, DateTime, double>? _dateDiff;
    private readonly Func<DateTime, double, DateTime>? _dateShift;

    private DateOperator(
        string symbol,
        string name,
        EvalType resultType,
        DateOperatorKind kind,
        Func<DateTime, double, DateTime>? dateShift,
        Func<DateTime, DateTime, double>? dateDiff,
        Func<double, double, double>? numericDays)
        : base(symbol, name, resultType)
    {
        Kind = kind;
        _dateShift = dateShift;
        _dateDiff = dateDiff;
        _numericDays = numericDays;
    }

    /// <summary>运算种类。</summary>
    public DateOperatorKind Kind { get; }

    /// <summary>日期 + 天数。</summary>
    public static DateOperator AddDays { get; } = new(
        "+", "日期加天数", EvalType.Date, DateOperatorKind.AddDays, (date, days) => date.AddDays(days), null, null);

    /// <summary>日期 - 天数。</summary>
    public static DateOperator SubtractDays { get; } = new(
        "-", "日期减天数", EvalType.Date, DateOperatorKind.SubtractDays, (date, days) => date.AddDays(-days), null, null);

    /// <summary>日期 - 日期 = 相差天数。</summary>
    public static DateOperator Difference { get; } = new(
        "-", "日期相差天数", EvalType.Number, DateOperatorKind.Difference, null, (a, b) => (a - b).TotalDays, (a, b) => a - b);

    public override object? Apply(object? left, object? right)
    {
        if (_dateShift is not null)
        {
            return _dateShift(ValueConvert.ToDate(left), ValueConvert.AsDouble(right));
        }

        return _dateDiff!(ValueConvert.ToDate(left), ValueConvert.ToDate(right));
    }

    public override bool TryBuildNumeric(out Func<double, double, double>? apply)
    {
        apply = _numericDays;
        return _numericDays is not null;
    }
}

/// <summary>字符串连接运算（<c>&amp;</c> 与字符串 <c>+</c>）。</summary>
internal sealed class ConcatOperator : BinaryOperator
{
    private ConcatOperator(string symbol, string name)
        : base(symbol, name, EvalType.String)
    {
    }

    /// <summary><c>&amp;</c> 连接。</summary>
    public static ConcatOperator Ampersand { get; } = new("&", "字符串连接");

    /// <summary>字符串 <c>+</c> 连接。</summary>
    public static ConcatOperator Plus { get; } = new("+", "字符串相加");

    public override object? Apply(object? left, object? right) =>
        string.Concat(ValueConvert.ToText(left), ValueConvert.ToText(right));
}

/// <summary>比较运算的种类。</summary>
internal enum ComparisonKind
{
    Equal,
    NotEqual,
    LessThan,
    LessThanOrEqual,
    GreaterThan,
    GreaterThanOrEqual,
}

/// <summary>比较运算。</summary>
internal sealed class ComparisonOperator : BinaryOperator
{
    private readonly Func<object?, object?, int> _compare;

    public ComparisonOperator(string symbol, ComparisonKind kind, Func<object?, object?, int> compare)
        : base(symbol, "比较", EvalType.Boolean)
    {
        Kind = kind;
        _compare = compare;
    }

    /// <summary>比较种类。</summary>
    public ComparisonKind Kind { get; }

    /// <summary>比较委托（供整树对象路径发射使用）。</summary>
    internal Func<object?, object?, int> Comparer => _compare;

    public override object? Apply(object? left, object? right)
    {
        int result = _compare(left, right);
        return Kind switch
        {
            ComparisonKind.Equal => result == 0,
            ComparisonKind.NotEqual => result != 0,
            ComparisonKind.LessThan => result < 0,
            ComparisonKind.LessThanOrEqual => result <= 0,
            ComparisonKind.GreaterThan => result > 0,
            _ => result >= 0,
        };
    }
}

/// <summary>常量比较器集合。</summary>
internal static class Comparisons
{
    public static readonly Func<object?, object?, int> Numeric =
        static (left, right) => ValueConvert.AsDouble(left).CompareTo(ValueConvert.AsDouble(right));

    public static readonly Func<object?, object?, int> Date =
        static (left, right) => ValueConvert.ToDate(left).CompareTo(ValueConvert.ToDate(right));

    public static readonly Func<object?, object?, int> Boolean =
        static (left, right) => ValueConvert.ToBoolean(left).CompareTo(ValueConvert.ToBoolean(right));

    public static readonly Func<object?, object?, int> Text = static (left, right) =>
        string.CompareOrdinal(ValueConvert.ToText(left), ValueConvert.ToText(right));

    public static readonly Func<object?, object?, int> Decimal =
        static (left, right) => ValueConvert.ToDecimal(left).CompareTo(ValueConvert.ToDecimal(right));

    public static readonly Func<object?, object?, int> Long =
        static (left, right) => Convert.ToInt64(left, System.Globalization.CultureInfo.InvariantCulture)
            .CompareTo(Convert.ToInt64(right, System.Globalization.CultureInfo.InvariantCulture));
}

/// <summary>运算符实例表。</summary>
internal static class Operators
{
    public static NumericOperator Add { get; } = new("+", "加法", static (a, b) => a + b);
    public static NumericOperator Subtract { get; } = new("-", "减法", static (a, b) => a - b);
    public static NumericOperator Multiply { get; } = new("*", "乘法", static (a, b) => a * b);
    public static NumericOperator Divide { get; } = new("/", "除法", static (a, b) => a / b);
    public static NumericOperator Modulo { get; } = new("%", "取模", static (a, b) => a % b);

    /// <summary>百分比：<c>operand % of context</c>；无 context 时退化为 <c>operand / 100</c>。</summary>
    public static NumericOperator PercentOf { get; } =
        new("%", "百分比", static (operand, context) => context * operand / 100d);

    /// <summary>百分比比率：<c>operand / 100</c>。</summary>
    public static LongOperator LongAdd { get; } = new("+", "加法", LongOperation.Add);

    public static LongOperator LongSubtract { get; } = new("-", "减法", LongOperation.Subtract);

    public static LongOperator LongMultiply { get; } = new("*", "乘法", LongOperation.Multiply);

    public static LongOperator LongModulo { get; } = new("%", "取模", LongOperation.Modulo);

    public static DecimalOperator DecimalAdd { get; } = new("+", "加法", static (a, b) => a + b);

    public static DecimalOperator DecimalSubtract { get; } = new("-", "减法", static (a, b) => a - b);

    public static DecimalOperator DecimalMultiply { get; } = new("*", "乘法", static (a, b) => a * b);

    public static DecimalOperator DecimalDivide { get; } = new("/", "除法", static (a, b) => a / b);

    public static DecimalOperator DecimalModulo { get; } = new("%", "取模", static (a, b) => a % b);

    public static NumericOperator PercentRatio { get; } =
        new("%", "百分比", static (operand, _) => operand / 100d);
}
