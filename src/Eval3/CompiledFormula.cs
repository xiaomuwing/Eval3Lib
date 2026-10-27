using System.Runtime.CompilerServices;

namespace Eval3;

/// <summary>已解析（并可编译为整树委托）的公式，可重复求值。</summary>
public sealed class CompiledFormula
{
    private readonly Func<double>? _legacyNumeric;
    private NumericProgram? _numericProgram;
    private Func<double>? _numericEvaluate;
    private ObjectProgram? _objectProgram;
    private Func<object?>? _objectEvaluate;

    private readonly bool _treeWalkOnly;

    internal CompiledFormula(
        string formula,
        Expression root,
        bool useWholeTreeNumeric = true,
        bool enableJit = true)
    {
        Formula = formula;
        Root = root;
        _treeWalkOnly = !enableJit || ContainsNonDouble(root);

        if (_treeWalkOnly)
        {
            return;
        }

        // 1) 优先整树数值编译（无委托链、常量折叠、数值 if）：只发射，首次求值再生成 IL。
        if (useWholeTreeNumeric && NumericPlan.TryBuild(root, out NumericProgram? numericProgram))
        {
            _numericProgram = numericProgram;
        }
        else if (root.TryBuildNumeric(out Func<double> legacy))
        {
            // 2) 回退旧的逐节点数值委托（日期差等少数场景）。
            _legacyNumeric = legacy;
        }

        // 3) 非数值公式走整树对象编译；数值公式的 Evaluate 复用数值委托（根节点一次装箱）。
        if (!HasNumericFastPath && useWholeTreeNumeric && ObjectPlan.TryBuild(root, out ObjectProgram? objectProgram))
        {
            _objectProgram = objectProgram;
        }
    }

    /// <summary>公式原文。</summary>
    public string Formula { get; }

    /// <summary>表达式树根节点。</summary>
    public Expression Root { get; }

    /// <summary>是否具备纯数值快路径（无装箱 / 拆箱）。</summary>
    public bool HasNumericFastPath => _numericProgram is not null || _legacyNumeric is not null;

    /// <summary>求值。</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public object? Evaluate()
    {
        if (_treeWalkOnly)
        {
            return Root.Evaluate();
        }

        Func<object?>? objectEvaluate = _objectEvaluate;
        if (objectEvaluate is not null)
        {
            return objectEvaluate();
        }

        if (_objectProgram is not null)
        {
            objectEvaluate = _objectProgram.Compile();
            _objectEvaluate = objectEvaluate;
            return objectEvaluate();
        }

        Func<double>? numeric = _numericEvaluate;
        if (numeric is not null)
        {
            return numeric();
        }

        if (_numericProgram is not null)
        {
            numeric = _numericProgram.Compile();
            _numericEvaluate = numeric;
            return numeric();
        }

        return _legacyNumeric is not null ? _legacyNumeric() : Root.Evaluate();
    }

    /// <summary>
    /// 求值；失败时沿表达式树定位到具体节点，并抛出带 <see cref="FormulaRuntimeException.NodeSource"/>
    /// 与近似位置的异常（给编辑器画波浪线用）。成功路径与 <see cref="Evaluate"/> 相同。
    /// </summary>
    public object? EvaluateWithDiagnostics()
    {
        try
        {
            return Evaluate();
        }
        catch (EvalException)
        {
            throw;
        }
#pragma warning disable CA1031
        catch (Exception exception)
#pragma warning restore CA1031
        {
            Expression? node = ExpressionDiagnostics.FindFaultingNode(Root);
            throw new FormulaRuntimeException(Formula, exception, node);
        }
    }

    /// <summary>求值并格式化为展示字符串。</summary>
    public string EvaluateToString() => ValueConvert.ConvertToString(Evaluate());

    /// <summary>按数值求值（有快路径时跳过装箱）。</summary>
    public double EvaluateNumber()
    {
        if (_treeWalkOnly)
        {
            return ValueConvert.AsDouble(Root.Evaluate());
        }

        Func<double>? numeric = _numericEvaluate;
        if (numeric is not null)
        {
            return numeric();
        }

        if (_numericProgram is not null)
        {
            numeric = _numericProgram.Compile();
            _numericEvaluate = numeric;
            return numeric();
        }

        return _legacyNumeric is not null ? _legacyNumeric() : ValueConvert.AsDouble(Evaluate());
    }

    /// <summary>尝试取纯数值委托（会触发首次编译）。</summary>
    public bool TryGetNumericDelegate(out Func<double>? evaluate)
    {
        if (_numericProgram is not null)
        {
            evaluate = _numericEvaluate ??= _numericProgram.Compile();
            return true;
        }

        evaluate = _legacyNumeric;
        return evaluate is not null;
    }

    /// <summary>decimal / long 公式不进入整树编译：整树路径按 double 发射会丢精度或溢出。</summary>
    private static bool ContainsNonDouble(Expression node)
    {
        if (node is ConstantExpression { Value: decimal or long })
        {
            return true;
        }

        if (node is VariableExpression variable
            && (variable.SystemType == typeof(decimal) || variable.SystemType == typeof(long)))
        {
            return true;
        }

        if (node is BinaryExpression { Operator: DecimalOperator or LongOperator })
        {
            return true;
        }

        foreach (Expression child in node.Children)
        {
            if (ContainsNonDouble(child))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>返回公式原文。</summary>
    public override string ToString() => Formula;
}
