namespace Eval3;

/// <summary>
/// 列式批量求值公式：把数值公式编译成
/// <c>(double[][] columns, double[] output, int count)</c>，
/// 一次循环算完 <paramref name="count"/> 个采样，避免逐点委托调用。
/// </summary>
public sealed class BatchFormula
{
    private readonly Action<double[][], double[], int> _run;
    private readonly ITypedValue[] _variables;

    private BatchFormula(string formula, ITypedValue[] variables, Action<double[][], double[], int> run)
    {
        Formula = formula;
        _variables = variables;
        _run = run;
    }

    /// <summary>公式原文。</summary>
    public string Formula { get; }

    /// <summary>参与批量求值的变量，顺序与 <c>columns</c> 一致。</summary>
    public IReadOnlyList<ITypedValue> Variables => _variables;

    /// <summary>变量名（用于日志 / 诊断）。</summary>
    public IEnumerable<string> VariableNames =>
        _variables.Select(static variable => variable is IHasDescription described ? described.Name : "<变量>");

    /// <summary>执行批量求值。</summary>
    /// <param name="columns">按 <see cref="Variables"/> 顺序排列的列。</param>
    /// <param name="destination">结果列，长度不小于 <paramref name="count"/>。</param>
    /// <param name="count">采样数量。</param>
    public void Evaluate(double[][] columns, double[] destination, int count)
    {
        ArgumentNullException.ThrowIfNull(columns);
        ArgumentNullException.ThrowIfNull(destination);
        _run(columns, destination, count);
    }

    /// <summary>单变量便捷重载。</summary>
    public double[] Evaluate(double[] column, int count)
    {
        ArgumentNullException.ThrowIfNull(column);
        var output = new double[count];
        _run([column], output, count);
        return output;
    }

    /// <summary>把公式编译为批量委托；不支持（非纯数值 / 含非数值变量）时返回 <see langword="false"/>。</summary>
    public static bool TryCreate(Evaluator evaluator, string? formula, out BatchFormula? batch)
    {
        ArgumentNullException.ThrowIfNull(evaluator);
        batch = null;

        Expression root;
        try
        {
            root = evaluator.Parse(formula);
        }
        catch (EvalException)
        {
            return false;
        }

        var variables = new List<ITypedValue>();
        if (!CollectVariables(root, variables)
            || !NumericPlan.TryCompileBatch(root, variables, out Action<double[][], double[], int> run))
        {
            return false;
        }

        batch = new BatchFormula(formula ?? string.Empty, variables.ToArray(), run);
        return true;
    }

    private static bool CollectVariables(Expression node, List<ITypedValue> variables)
    {
        if (node is VariableExpression variable)
        {
            if (variable.Variable.EvalType != EvalType.Number)
            {
                return false;
            }

            if (!variables.Contains(variable.Variable, ReferenceEqualityComparer.Instance))
            {
                variables.Add(variable.Variable);
            }

            return true;
        }

        foreach (Expression child in node.Children)
        {
            if (!CollectVariables(child, variables))
            {
                return false;
            }
        }

        return true;
    }
}
