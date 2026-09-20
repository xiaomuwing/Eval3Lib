using Eval3;
using Eval3.Functions;

namespace Eval3.Cli;

/// <summary>命令行验算工具：<c>eval3 "1+2*3" "Round(3.14159, 2)"</c>，无参数时进入交互模式。</summary>
internal static class Program
{
    private static readonly double[] SampleArray = [3d, 6d, 9d];

    private static int Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        bool percentMode = args.Any(static a => a.Equals("--percent", StringComparison.OrdinalIgnoreCase));
        bool vbSyntax = args.Any(static a => a.Equals("--vb", StringComparison.OrdinalIgnoreCase));
        args = [.. args.Where(static a => !a.StartsWith("--", StringComparison.Ordinal))];

        var evaluator = new Evaluator(new EvaluatorOptions
        {
            Syntax = vbSyntax ? ParserSyntax.VisualBasic : ParserSyntax.CSharp,
            PercentMode = percentMode ? PercentMode.PercentOf : PercentMode.Modulo,
        });
        var variables = new DictionaryVariableBag();
        evaluator.AddEnvironmentFunctions(
            variables,
            typeof(MathLibrary),
            typeof(TextLibrary),
            typeof(DateLibrary),
            typeof(FinanceLibrary));

        variables.Set("单价", 12.5, "示例变量");
        variables.Set("数量", 8, "示例变量");
        variables.Set("税率", 0.13, "示例变量");
        variables.Set("price", 100d, "示例变量（与旧版对照脚本同名）");
        variables.Set("arr", SampleArray, "示例变量（数组）");

        // 业务对象：公共属性 / 字段可直接作为公式变量（与旧版对照脚本一致）
        evaluator.AddEnvironmentFunctions(new SampleProject());

        if (args.Length > 0)
        {
            return Run(evaluator, args);
        }

        Console.WriteLine("Eval3 命令行验算工具，输入公式回车计算，输入 :quit 退出。");
        Console.WriteLine("示例变量：单价 = 12.5，数量 = 8，税率 = 0.13");
        while (true)
        {
            Console.Write("eval> ");
            string? line = Console.ReadLine();
            if (line is null || line.Trim() is ":quit" or ":q" or "exit")
            {
                return 0;
            }

            Run(evaluator, [line]);
        }
    }

    private static int Run(Evaluator evaluator, string[] formulas)
    {
        int failures = 0;
        foreach (string formula in formulas)
        {
            if (evaluator.TryEvaluateToString(formula, out string result, out EvalException? error))
            {
                Console.WriteLine($"{formula} = {result}");
            }
            else
            {
                failures++;
                Console.WriteLine($"{formula} ! {error!.Message}");
                if (error is ParserException parserException)
                {
                    Console.WriteLine("  " + parserException.Formula);
                    Console.WriteLine("  " + new string(' ', Math.Max(0, parserException.Position)) + "^");
                }
            }
        }

        return failures == 0 ? 0 : 1;
    }

    /// <summary>示例业务对象：字段 / 属性直接暴露为公式变量。</summary>
    private sealed class SampleProject
    {
        public double Amount { get; } = 100d;

        public double Rate = 0.13;

        public double[] Values = [3d, 6d, 9d];
    }
}
