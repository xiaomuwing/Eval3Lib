using System.ComponentModel;
using System.Globalization;

namespace Eval3.Functions;

/// <summary>日期函数库。</summary>
[Description("日期函数库")]
public static class DateLibrary
{
    /// <summary>今天。</summary>
    [Description("今天的日期（不含时间）")]
    public static DateTime Today => DateTime.Today;

    /// <summary>当前时间。</summary>
    [Description("当前日期时间")]
    public static DateTime Now => DateTime.Now;

    /// <summary>年。</summary>
    [Description("年份")]
    public static double Year(DateTime value) => value.Year;

    /// <summary>月。</summary>
    [Description("月份")]
    public static double Month(DateTime value) => value.Month;

    /// <summary>日。</summary>
    [Description("日")]
    public static double Day(DateTime value) => value.Day;

    /// <summary>小时。</summary>
    [Description("小时")]
    public static double Hour(DateTime value) => value.Hour;

    /// <summary>分钟。</summary>
    [Description("分钟")]
    public static double Minute(DateTime value) => value.Minute;

    /// <summary>星期（1 = 周一）。</summary>
    [Description("星期（1 = 周一 … 7 = 周日）")]
    public static double Weekday(DateTime value) => (int)value.DayOfWeek == 0 ? 7 : (int)value.DayOfWeek;

    /// <summary>加天数。</summary>
    [Description("加天数：AddDays(#2024/01/31#, 1) = #2024/02/01#")]
    public static DateTime AddDays(DateTime value, double days) => value.AddDays(days);

    /// <summary>加月数。</summary>
    [Description("加月数")]
    public static DateTime AddMonths(DateTime value, int months) => value.AddMonths(months);

    /// <summary>相差天数。</summary>
    [Description("两个日期相差的天数")]
    public static double DaysBetween(DateTime start, DateTime end) => (end - start).TotalDays;

    /// <summary>本月第一天。</summary>
    [Description("本月第一天")]
    public static DateTime FirstDayOfMonth(DateTime value) => new(value.Year, value.Month, 1);

    /// <summary>本月最后一天。</summary>
    [Description("本月最后一天")]
    public static DateTime LastDayOfMonth(DateTime value) =>
        new DateTime(value.Year, value.Month, DateTime.DaysInMonth(value.Year, value.Month));

    /// <summary>格式化日期。</summary>
    [Description("按格式串格式化日期：Format(#2024/01/31#, \"yyyy-MM-dd\")")]
    public static string Format(DateTime value, string format) => value.ToString(format, CultureInfo.InvariantCulture);
}
