using System.ComponentModel;

namespace Eval3.Functions;

/// <summary>
/// 数学函数库（静态库，通过 <c>evaluator.AddEnvironmentFunctions(typeof(MathLibrary))</c> 注册）。
/// </summary>
[Description("数学函数库")]
[Pure]
public static class MathLibrary
{
    /// <summary>圆周率。</summary>
    [Description("圆周率 π")]
    public static double PI => Math.PI;

    /// <summary>自然常数。</summary>
    [Description("自然常数 e")]
    public static double E => Math.E;

    /// <summary>绝对值。</summary>
    [Description("绝对值：Abs(-3) = 3")]
    public static double Abs(double value) => Math.Abs(value);

    /// <summary>四舍五入。</summary>
    [Description("四舍五入到指定小数位：Round(3.14159, 2) = 3.14")]
    public static double Round(double value, int digits = 0) =>
        Math.Round(value, Math.Clamp(digits, 0, 15), MidpointRounding.AwayFromZero);

    /// <summary>按步长取整（例如按分 / 按元取整）。</summary>
    [Description("按步长四舍五入：RoundTo(12.3456, 0.01) = 12.35")]
    public static double RoundTo(double value, double step)
    {
        if (step == 0)
        {
            return value;
        }

        decimal result = decimal.Round(
            (decimal)value / (decimal)step,
            0,
            MidpointRounding.AwayFromZero);
        return (double)(result * (decimal)step);
    }

    /// <summary>向下取整。</summary>
    [Description("向下取整：Floor(3.9) = 3")]
    public static double Floor(double value) => Math.Floor(value);

    /// <summary>向上取整。</summary>
    [Description("向上取整：Ceiling(3.1) = 4")]
    public static double Ceiling(double value) => Math.Ceiling(value);

    /// <summary>按步长向下取整。</summary>
    [Description("按步长向下取整：FloorTo(12.3456, 0.01) = 12.34")]
    public static double FloorTo(double value, double step) =>
        step == 0 ? value : Math.Floor(Math.Round(value / step, 10)) * step;

    /// <summary>按步长向上取整。</summary>
    [Description("按步长向上取整：CeilingTo(12.3412, 0.01) = 12.35")]
    public static double CeilingTo(double value, double step) =>
        step == 0 ? value : Math.Ceiling(Math.Round(value / step, 10)) * step;

    /// <summary>截断。</summary>
    [Description("截断小数位：Truncate(3.99) = 3")]
    public static double Truncate(double value, int digits = 0) => Math.Round(value, digits, MidpointRounding.ToZero);

    /// <summary>平方根。</summary>
    [Description("平方根：Sqrt(9) = 3")]
    public static double Sqrt(double value) => Math.Sqrt(value);

    /// <summary>幂运算。</summary>
    [Description("幂运算：Pow(2, 10) = 1024")]
    public static double Pow(double value, double power) => Math.Pow(value, power);

    /// <summary>自然对数。</summary>
    [Description("自然对数：Log(e) = 1")]
    public static double Log(double value) => Math.Log(value);

    /// <summary>指定底数的对数。</summary>
    [Description("指定底数对数：Log(8, 2) = 3")]
    public static double Log(double value, double logBase) => Math.Log(value, logBase);

    /// <summary>指数函数。</summary>
    [Description("e 的幂：Exp(1) = 2.718…")]
    public static double Exp(double value) => Math.Exp(value);

    /// <summary>较小值。</summary>
    [Description("取较小值：Min(3, 5) = 3")]
    public static double Min(double left, double right) => Math.Min(left, right);

    /// <summary>较大值。</summary>
    [Description("取较大值：Max(3, 5) = 5")]
    public static double Max(double left, double right) => Math.Max(left, right);

    /// <summary>区间限幅。</summary>
    [Description("限幅：Clamp(12, 0, 10) = 10")]
    public static double Clamp(double value, double min, double max) => Math.Clamp(value, min, max);

    /// <summary>求和。</summary>
    [Description("求和：Sum(1, 2, 3) = 6")]
    public static double Sum(params double[] values) => CompensatedSum(values.AsSpan());

    /// <summary>Neumaier 补偿求和：大数组 / 量级差异大时显著降低累加误差。</summary>
    internal static double CompensatedSum(ReadOnlySpan<double> values)
    {
        double sum = 0d;
        double compensation = 0d;
        foreach (double value in values)
        {
            double next = sum + value;
            if (Math.Abs(sum) >= Math.Abs(value))
            {
                compensation += (sum - next) + value;
            }
            else
            {
                compensation += (value - next) + sum;
            }

            sum = next;
        }

        return sum + compensation;
    }

    // 参数个数固定的重载：让常见 1~6 参数调用避开 params 数组分配（实验分支）。
    /// <summary>求和（单参数）。</summary>
    [Description("求和（单参数）")]
    public static double Sum(double a) => a;

    /// <summary>求和（2 参数）。</summary>
    [Description("求和（2 参数）")]
    public static double Sum(double a, double b) => a + b;

    /// <summary>求和（3 参数）。</summary>
    [Description("求和（3 参数）")]
    public static double Sum(double a, double b, double c) => a + b + c;

    /// <summary>求和（4 参数）。</summary>
    [Description("求和（4 参数）")]
    public static double Sum(double a, double b, double c, double d) => a + b + c + d;

    /// <summary>求和（5 参数）。</summary>
    [Description("求和（5 参数）")]
    public static double Sum(double a, double b, double c, double d, double e) => a + b + c + d + e;

    /// <summary>求和（6 参数）。</summary>
    [Description("求和（6 参数）")]
    public static double Sum(double a, double b, double c, double d, double e, double f) => a + b + c + d + e + f;

    /// <summary>求平均。</summary>
    [Description("平均值：Average(1, 2, 3) = 2")]
    public static double Average(params double[] values)
    {
        if (values.Length == 0)
        {
            return 0d;
        }

        return CompensatedSum(values.AsSpan()) / values.Length;
    }

    /// <summary>连乘。</summary>
    [Description("连乘：Product(2, 3, 4) = 24")]
    public static double Product(params double[] values)
    {
        double total = 1d;
        for (int i = 0; i < values.Length; i++)
        {
            total *= values[i];
        }

        return total;
    }

    /// <summary>符号。</summary>
    [Description("返回符号：Sign(-2) = -1")]
    public static double Sign(double value) => Math.Sign(value);

    /// <summary>整数部分。</summary>
    [Description("取整数部分：Int(3.9) = 3")]
    public static double Int(double value) => Math.Truncate(value);

    public static double Asin(double v)
    {
        return Math.Asin(v);
    }
    public static double Acos(double v)
    {
        return Math.Acos(v);
    }
    public static double Atan(double v)
    {
        return Math.Atan(v);
    }
    public static double Atan2(double v, double v1)
    {
        return Math.Atan2(v, v1);
    }
    public static double Sin(double v)
    {
        return Math.Sin(v);
    }
    public static double Cos(double v)
    {
        return Math.Cos(v);
    }
    [Impure]
    public static DateTime Now()
    {
        return System.DateTime.Now;
    }
    public static string Trim(string str)
    {
        return str.Trim();
    }
    public static string LeftTrim(string str)
    {
        return str.TrimStart();
    }
    public static string RightTrim(string str)
    {
        return str.TrimEnd();
    }
    public static string PadLeft(string str, int wantedlen, string addedchar)
    {
        while ((str.Length < wantedlen))
        {
            str = (addedchar + str);
            // Warning!!! Optional parameters not supported
        }
        return str;
    }
    public static double Mod(double x, double y)
    {
        return (x % y);
    }
    public static object If(bool cond, object TrueValue, object FalseValue)
    {
        if (cond)
        {
            return TrueValue;
        }
        else
        {
            return FalseValue;
        }
    }
    public static string Lower(string value)
    {
        return value.ToLower();
    }
    public static string Upper(string value)
    {
        return value.ToUpper();
    }
    public static string WCase(string value)
    {
        if ((value.Length == 0))
        {
            return "";
        }
        return (value.Substring(0, 1).ToUpper() + value.Substring(1).ToLower());
    }
    public static DateTime Date(int year, int month, int day)
    {
        return new DateTime(year, month, day);
    }
    public static int Year(DateTime d)
    {
        return d.Year;
    }
    public static int Month(DateTime d)
    {
        return d.Month;
    }
    public static int Day(DateTime d)
    {
        return d.Day;
    }
    public static string Replace(string Base, string search, string repl)
    {
        return Base.Replace(search, repl);
    }
    public static string Substr(string s, int from, int len)
    {
        if ((s == null))
        {
            return string.Empty;
        }
        // Warning!!! Optional parameters not supported
        from--;
        if ((from < 1))
        {
            from = 0;
        }
        if ((from >= s.Length))
        {
            from = s.Length;
        }
        if ((from + len) > s.Length)
        {
            len = (s.Length - from);
        }
        return s.Substring(from, len);
    }
    public static int Len(string str)
    {
        return str.Length;
    }

    public static int Trunc(double value, int prec)
    {
        value = (value - (0.5 / Math.Pow(10, prec)));
        // Warning!!! Optional parameters not supported
        return (int)(Math.Round(value, prec));
    }
    public static double Dec(object value)
    {
        return (double)(value);
    }
    public static double Round(object value)
    {
        return Math.Round((double)(value));
    }
    [Impure]
    public static double RandomX(object valueMin, object valueMax)
    {
        Random r = new Random(Guid.NewGuid().GetHashCode());
        int min, max;
        _ = double.TryParse(valueMin.ToString(), out double minD);
        _ = double.TryParse(valueMax.ToString(), out double maxD);
        min = (int)(minD * 1000000d);
        max = (int)(maxD * 1000000d);
        double result = (r.Next(min, max)) / 1000000d;
        return result;
    }
    public static string Chr(int c)
    {
        return "" + (char)(c);
    }
    public static string ChCR()
    {
        return "\r";
    }
    public static string ChLF()
    {
        return "\n";
    }
    public static string ChCRLF()
    {
        return "\r\n";
    }
    public static double Exp(double Base, double pexp)
    {

        return Math.Pow(Base, pexp);
    }
    public static string[] Split(string s, string delimiter)
    {
        return s.Split(delimiter[0]);
        // Warning!!! Optional parameters not supported
    }
    public static DBNull DbNull()
    {
        return DBNull.Value;
    }
    public static double Power(double v, double e)
    {
        return Math.Pow(v, e);
    }
    public static double Ln(double v)
    {
        return Math.Log(v);
    }
    static readonly int[] pt100_table = new int[861] { 1852,1895,1938,1982,2025,2068,2111,2154,2197,2240,2283,2325,2368,2411,2454,2497,2539,2582,2624,2667,2710,
                                                2752,2795,2837,2880,2922,2964,3007,3049,3091,3134,3176,3218,3260,3302,3344,3386,3428,3470,3512,3554,
                                                3596,3638,3680,3722,3764,3805,3847,3889,3931,3972,4014,4056,4097,4139,4180,4222,4263,4305,4346,4388,
                                                4429,4470,4512,4553,4594,4636,4677,4718,4759,4800,4842,4883,4924,4965,5006,5047,5088,5129,5170,5211,
                                                5252,5293,5334,5375,5415,5456,5497,5538,5579,5619,5660,5701,5741,5782,5823,5863,5904,5944,5985,6026,
                                                6066,6107,6147,6188,6228,6268,6309,6349,6390,6430,6470,6511,6551,6591,6631,6672,6712,6752,6792,6833,
                                                6873,6913,6953,6993,7033,7073,7113,7153,7193,7233,7273,7313,7353,7393,7433,7473,7513,7553,7593,7633,
                                                7673,7712,7752,7792,7832,7872,7911,7951,7991,8031,8070,8110,8150,8189,8229,8269,8308,8348,8387,8427,
                                                8467,8506,8546,8585,8625,8664,8704,8743,8783,8822,8862,8901,8940,8980,9019,9059,9098,9137,9177,9216,
                                                9255,9295,9334,9373,9412,9452,9491,9530,9569,9609,9648,9687,9726,9765,9804,9844,9883,9922,9961,
                                                10000,10039,10078,10117,10156,10195,10234,10273,10312,10351,10390,10429,10468,10507,10546,10585,10624,10663,10702,10740,
                                                10779,10818,10857,10896,10935,10973,11012,11051,11090,11129,11167,11206,11245,11283,11322,11361,11400,11438,11477,11515,
                                                11554,11593,11631,11670,11708,11747,11786,11824,11863,11901,11940,11978,12017,12055,12094,12132,12171,12209,12247,12286,
                                                12324,12363,12401,12439,12478,12516,12554,12593,12631,12669,12708,12746,12784,12822,12861,12899,12937,12975,13013,13052,
                                                13090,13128,13166,13204,13242,13280,13318,13357,13395,13433,13471,13509,13547,13585,13623,13661,13699,13737,13775,13813,
                                                13851,13888,13926,13964,14002,14040,14078,14116,14154,14191,14229,14267,14305,14343,14380,14418,14456,14494,14531,14569,
                                                14607,14644,14682,14720,14757,14795,14833,14870,14908,14946,14983,15021,15058,15096,15133,15171,15208,15246,15283,15321,
                                                15358,15396,15433,15471,15508,15546,15583,15620,15658,15695,15733,15770,15807,15845,15882,15919,15956,15994,16031,16068,
                                                16105,16143,16180,16217,16254,16291,16329,16366,16403,16440,16477,16514,16551,16589,16626,16663,16700,16737,16774,16811,
                                                16848,16885,16922,16959,16996,17033,17070,17107,17143,17180,17217,17254,17291,17328,17365,17402,17438,17475,17512,17549,
                                                17586,17622,17659,17696,17733,17769,17806,17843,17879,17916,17953,17989,18026,18063,18099,18136,18172,18209,18246,18282,
                                                18319,18355,18392,18428,18465,18501,18538,18574,18611,18647,18684,18720,18756,18793,18829,18866,18902,18938,18975,19011,
                                                19047,19084,19120,19156,19192,19229,19265,19301,19337,19374,19410,19446,19482,19518,19555,19591,19627,19663,19699,19735,
                                                19771,19807,19843,19879,19915,19951,19987,20023,20059,20095,20131,20167,20203,20239,20275,20311,20347,20383,20419,20455,
                                                20490,20526,20562,20598,20634,20670,20705,20741,20777,20813,20848,20884,20920,20956,20991,21027,21063,21098,21134,21170,
                                                21205,21241,21276,21312,21348,21383,21419,21454,21490,21525,21561,21596,21632,21667,21703,21738,21774,21809,21844,21880,
                                                21915,21951,21986,22021,22057,22092,22127,22163,22198,22233,22268,22304,22339,22374,22409,22445,22480,22515,22550,22585,
                                                22621,22656,22691,22726,22761,22796,22831,22866,22902,22937,22972,23007,23042,23077,23112,23147,23182,23217,23252,23287,
                                                23321,23356,23391,23426,23461,23496,23531,23566,23600,23635,23670,23705,23740,23774,23809,23844,23879,23913,23948,23983,
                                                24018,24052,24087,24122,24156,24191,24226,24260,24295,24329,24364,24399,24433,24468,24502,24537,24571,24606,24640,24675,
                                                24709,24744,24778,24813,24847,24881,24916,24950,24585,25019,25053,25088,25122,25156,25191,25225,25259,25293,25328,25362,
                                                25396,25430,25465,25499,25533,25567,25601,25635,25670,25704,25738,25772,25806,25840,25874,25908,25942,25976,26010,26044,
                                                26078,26112,26146,26180,26214,26248,26282,26316,26350,26384,26418,26452,26486,26520,26553,26587,26621,26655,26689,26722,
                                                26756,26790,26824,26857,26891,26925,26959,26992,27026,27060,27093,27127,27161,27194,27228,27261,27295,27329,27362,27396,
                                                27429,27463,27496,27530,27563,27597,27630,27664,27697,27731,27764,27798,27831,27864,27898,27931,27964,27998,28031,28064,
                                                28098,28131,28164,28198,28231,28264,28297,28331,28364,28397,28430,28463,28497,28530,28563,28596,28629,28662,28685,28729,
                                                28762,28795,28828,28861,28894,28927,28960,28993,29026,29059,29092,29125,29158,29191,29224,29256,29289,29322,29355,29388,
                                                29421,29454,29486,29519,29552,29585,29618,29650,29683,29716,29749,29781,29814,29847,29880,29912,29945,29978,30010,30043,
                                                30075,30108,30141,30173,30206,30238,30271,30303,30336,30369,30401,30434,30466,30498,30531,30563,30596,30628,30661,30693,
                                                30725,30758,30790,30823,30855,30887,30920,30952,30984,31016,31049,31081,31113,31145,31178,31210,31242,31274,31306,31339,
                                                31371,31403,31435,31467,31499,31531,31564,31596,31628,31660,31692,31724,31756,31788,31820,31852,31884,31916,31948,31980,
                                                32012,32043,32075,32107,32139,32171,32203,32235,32267,32298,32330,32362,32394,32426,32457,32489,32521,32553,32584,32616,
                                                32648,32679,32711,32743,32774,32806,32838,32869,32901,32932,32964,32996,33027,33059,33090,33122,33153,33185,33216,33248,33279};
    public static double ResistanceToTemp(double v)
    {
        v *= 100;

        int low = 0;
        double a;
        int mid;
        int high = 860;

        if ((v < pt100_table[0]) || (v > pt100_table[high]))
        {
            return -1000;
        }
        try
        {
            while (high >= low)
            {
                mid = (high + low) / 2;

                if (v >= pt100_table[mid])
                {
                    if (v < pt100_table[mid + 1])
                    {
                        a = v - pt100_table[mid];
                        a /= (pt100_table[mid + 1] - pt100_table[mid]);
                        a = a + mid - 200;
                        return a;
                    }
                    else
                    {
                        low = mid + 1;
                    }
                }
                else if (v < pt100_table[mid])
                {
                    high = mid - 1;
                }
            }
        }
        catch
        {
            return -1000;
        }
        return -1000;
    }

    /// <summary>绝对值（decimal）。</summary>
    [Description("绝对值（decimal）：Abs(-3.5m) = 3.5")]
    public static decimal Abs(decimal value) => decimal.Abs(value);

    /// <summary>四舍五入（decimal）。</summary>
    [Description("四舍五入到指定小数位（decimal）")]
    public static decimal Round(decimal value, int digits = 0) =>
        decimal.Round(value, Math.Clamp(digits, 0, 28), MidpointRounding.AwayFromZero);

    /// <summary>按步长四舍五入（decimal）。</summary>
    [Description("按步长四舍五入（decimal）")]
    public static decimal RoundTo(decimal value, decimal step) =>
        step == 0m ? value : decimal.Round(value / step, 0, MidpointRounding.AwayFromZero) * step;

    /// <summary>向下取整（decimal）。</summary>
    [Description("向下取整（decimal）")]
    public static decimal Floor(decimal value) => decimal.Floor(value);

    /// <summary>向上取整（decimal）。</summary>
    [Description("向上取整（decimal）")]
    public static decimal Ceiling(decimal value) => decimal.Ceiling(value);

    /// <summary>截断小数位（decimal）。</summary>
    [Description("截断小数位（decimal）")]
    public static decimal Truncate(decimal value, int digits = 0)
    {
        decimal factor = 1m;
        for (int i = 0; i < Math.Clamp(digits, 0, 28); i++) factor *= 10m;
        return decimal.Truncate(value * factor) / factor;
    }

    /// <summary>取较小值（decimal）。</summary>
    [Description("取较小值（decimal）")]
    public static decimal Min(decimal left, decimal right) => decimal.Min(left, right);

    /// <summary>取较大值（decimal）。</summary>
    [Description("取较大值（decimal）")]
    public static decimal Max(decimal left, decimal right) => decimal.Max(left, right);

    /// <summary>限幅（decimal）。</summary>
    [Description("限幅（decimal）")]
    public static decimal Clamp(decimal value, decimal min, decimal max) => decimal.Clamp(value, min, max);

    /// <summary>符号（decimal）。</summary>
    [Description("返回符号（decimal）")]
    public static decimal Sign(decimal value) => decimal.Sign(value);

    /// <summary>求和（decimal）。</summary>
    [Description("求和（decimal）")]
    public static decimal Sum(params decimal[] values)
    {
        decimal total = 0m;
        foreach (decimal value in values) total += value;
        return total;
    }

    /// <summary>平均值（decimal）。</summary>
    [Description("平均值（decimal）")]
    public static decimal Average(params decimal[] values)
    {
        if (values.Length == 0) return 0m;
        decimal total = 0m;
        foreach (decimal value in values) total += value;
        return total / values.Length;
    }
}
