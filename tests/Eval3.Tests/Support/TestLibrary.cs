using System.ComponentModel;

namespace Eval3.Tests.Support;

/// <summary>测试用函数库：用于验证惰性求值、异常传播与实例库调用。</summary>
[Description("测试函数库")]
public sealed class TestLibrary
{
    /// <summary>调用计数。</summary>
    public static int Calls { get; set; }

    /// <summary>总是抛出异常。</summary>
    [Description("总是抛出异常")]
    public static double Boom() => throw new InvalidOperationException("boom");

    /// <summary>记录调用次数。</summary>
    [Description("记录调用次数")]
    public static double Counted()
    {
        Calls++;
        return Calls;
    }

    /// <summary>实例方法。</summary>
    [Description("实例方法：Increment(2) = 3")]
    public double Increment(double value) => value + 1;

    /// <summary>示例字段。</summary>
    [Description("示例字段")]
    public double Factor = 2.5;
}
