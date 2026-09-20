using System.ComponentModel;

namespace Eval3.Functions;

/// <summary>财税 / 造价常用计算函数库。</summary>
[Description("财税专业函数库")]
public static class FinanceLibrary
{
    /// <summary>税额。</summary>
    [Description("按税率计算税额：Vat(100, 0.13) = 13")]
    public static double Vat(double amount, double rate = 0.13) => amount * rate;

    /// <summary>价税合计。</summary>
    [Description("不含税价换算价税合计：GrossUp(100, 0.13) = 113")]
    public static double GrossUp(double netAmount, double rate = 0.13) => netAmount * (1d + rate);

    /// <summary>不含税价。</summary>
    [Description("价税合计换算不含税价：NetOfTax(113, 0.13) = 100")]
    public static double NetOfTax(double grossAmount, double rate = 0.13) => grossAmount / (1d + rate);

    /// <summary>净现值。</summary>
    [Description("净现值：Npv(0.05, 100, 200, 300)")]
    public static double Npv(double rate, params double[] cashFlows)
    {
        double total = 0d;
        for (int i = 0; i < cashFlows.Length; i++)
        {
            total += cashFlows[i] / Math.Pow(1d + rate, i + 1);
        }

        return total;
    }

    /// <summary>投资回报率。</summary>
    [Description("投资回报率：(收益 - 成本) / 成本")]
    public static double Roi(double gain, double cost) => cost == 0 ? 0d : (gain - cost) / cost;

    /// <summary>直线法折旧额。</summary>
    [Description("直线法折旧：Depreciation(100000, 5000, 5) = 19000")]
    public static double Depreciation(double cost, double salvage, int life) =>
        life <= 0 ? 0d : (cost - salvage) / life;
}
