#Requires -Version 5.1
<#
.SYNOPSIS
    实测遗留版本（VB 版 Eval3.dll）对各类公式的支持情况，用于与新版本做功能对照。

.DESCRIPTION
    只读验证：加载 bin\Debug\Eval3.dll（.NET Framework 4.8 的 VB 编译产物），
    用一段 C# 探针代码（避免 PowerShell 适配器受 VB 元数据大小写重名影响）逐条求值，
    输出 支持 / 报错 的结果表。需要 Windows PowerShell（含 Microsoft.VisualBasic 运行时）。

.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File tools\Test-LegacyVbBehavior.ps1
#>
param(
    [string]$AssemblyPath = (Join-Path $PSScriptRoot '..\bin\Debug\Eval3.dll')
)

$ErrorActionPreference = 'Stop'
$resolved = [System.IO.Path]::GetFullPath($AssemblyPath)
if (-not (Test-Path -LiteralPath $resolved)) {
    throw "找不到遗留程序集：$resolved"
}

Add-Type -Path $resolved
Write-Host "已加载遗留程序集：$resolved" -ForegroundColor Cyan

# 旧版 API 探针（C# 5 语法，兼容 Windows PowerShell 的 CodeDom 编译器）
Add-Type -ReferencedAssemblies $resolved -TypeDefinition @'
using System;
using System.Collections.Generic;

// 旧版变量容器（实现 iVariableBag）
public class LegacyBag : Eval3.iVariableBag
{
    private readonly Dictionary<string, Eval3.iEvalTypedValue> map =
        new Dictionary<string, Eval3.iEvalTypedValue>(StringComparer.OrdinalIgnoreCase);

    public void Set(string name, object value)
    {
        map[name] = new Eval3.EvalVariable(name, value, string.Empty,
            value == null ? typeof(object) : value.GetType());
    }

    public Eval3.iEvalTypedValue GetVariable(string varname)
    {
        Eval3.iEvalTypedValue value;
        return map.TryGetValue(varname, out value) ? value : null;
    }
}

// 旧版函数库（实例方法，模拟业务对象）
public class LegacyFunctions
{
    public double Abs(double value) { return Math.Abs(value); }
    public double Max(double left, double right) { return Math.Max(left, right); }
    public double Round(double value, int digits) { return Math.Round(value, digits); }
    public double Sum(params double[] values)
    {
        double total = 0;
        for (int i = 0; i < values.Length; i++) { total += values[i]; }
        return total;
    }
}

// 含字段成员的业务对象
public class LegacyHolder
{
    public double[] Values = new double[] { 3, 6, 9 };
    public double Amount = 100;
    public double Rate = 0.13;
}

public class LegacyCase
{
    public string Category;
    public string Formula;
    public bool Supported;
    public string Result;
}

public static class LegacyProbe
{
    public static List<LegacyCase> Run()
    {
        LegacyBag bag = new LegacyBag();
        bag.Set("arr", new double[] { 3, 6, 9 });
        bag.Set("price", 100.0);

        // 旧版构造函数只有带可选参数的重载
        Eval3.Evaluator evaluator = new Eval3.Evaluator(Eval3.eParserSyntax.cSharp, false);
        evaluator.AddEnvironmentFunctions(new LegacyFunctions());
        evaluator.AddEnvironmentFunctions(bag);
        evaluator.AddEnvironmentFunctions(new LegacyHolder());

        List<LegacyCase> cases = new List<LegacyCase>();
        Add(cases, evaluator, "算术", "1 + 2 * 3");
        Add(cases, evaluator, "算术", "(1 + 2) * 3");
        Add(cases, evaluator, "算术", "10 / 4");
        Add(cases, evaluator, "算术", "-5 + 3");
        Add(cases, evaluator, "算术", "10 % 3");
        Add(cases, evaluator, "百分比", "100 + 5%");
        Add(cases, evaluator, "百分比", "100 * 5%");
        Add(cases, evaluator, "比较", "1 > 2");
        Add(cases, evaluator, "比较", "1 = 1");
        Add(cases, evaluator, "比较", "2 <= 3");
        Add(cases, evaluator, "连接", "\"a\" + \"b\"");
        Add(cases, evaluator, "连接", "\"a\" & \"b\"");
        Add(cases, evaluator, "连接", "1 + \"a\"");
        Add(cases, evaluator, "逻辑", "true and false");
        Add(cases, evaluator, "逻辑", "not true");
        Add(cases, evaluator, "条件", "if(true, 1, 2)");
        Add(cases, evaluator, "日期", "#2024/01/31#");
        Add(cases, evaluator, "日期", "#2024/01/31# + 1");
        Add(cases, evaluator, "日期", "#2024/02/10# - #2024/02/01#");
        Add(cases, evaluator, "字符串", "\"abc\".Length");
        Add(cases, evaluator, "字符串", "\"合计：%[1 + 1]\"");
        Add(cases, evaluator, "函数", "Abs(-3)");
        Add(cases, evaluator, "函数", "Max(3, 5)");
        Add(cases, evaluator, "函数", "Round(3.14159, 2)");
        Add(cases, evaluator, "函数", "Sum(1, 2, 3)");
        Add(cases, evaluator, "变量", "price * 2");
        Add(cases, evaluator, "变量", "Amount * (1 + Rate)");
        Add(cases, evaluator, "数组", "arr[1]");
        Add(cases, evaluator, "数组", "Values[1]");
        Add(cases, evaluator, "数组", "Values(1)");
        Add(cases, evaluator, "错误", "1 + * 2");
        return cases;
    }

    private static void Add(List<LegacyCase> list, Eval3.Evaluator evaluator, string category, string formula)
    {
        LegacyCase item = new LegacyCase();
        item.Category = category;
        item.Formula = formula;
        try
        {
            object value = evaluator.Parse(formula).value;
            item.Result = value == null ? "<null>" : value.ToString();
            item.Supported = true;
        }
        catch (Exception ex)
        {
            item.Result = ex.GetType().Name + ": " + ex.Message;
            item.Supported = false;
        }

        list.Add(item);
    }
}
'@

$cases = [LegacyProbe]::Run()
$cases | Select-Object Category, Formula, Supported, Result | Format-Table -AutoSize -Wrap

$supported = ($cases | Where-Object { $_.Supported }).Count
Write-Host ("旧版 VB 引擎支持 {0} / {1} 项" -f $supported, $cases.Count) -ForegroundColor Yellow
