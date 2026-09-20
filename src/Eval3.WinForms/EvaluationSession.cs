using System.Diagnostics;
using Eval3.Functions;
using Eval3.Thermo;

namespace Eval3.WinForms;

/// <summary>已注册的函数库条目。</summary>
public sealed class LibraryEntry
{
    public LibraryEntry(string name, string description, object target, bool enabled = true)
    {
        Name = name;
        Description = description;
        Target = target;
        Enabled = enabled;
    }

    public string Name { get; }

    public string Description { get; }

    public object Target { get; }

    public bool Enabled { get; set; }

    public override string ToString() => $"{Name}（{Description}）";
}

/// <summary>单次验算结果。</summary>
public sealed record EvaluationOutcome(
    string Formula,
    bool Success,
    string Result,
    object? Value,
    EvalType ResultType,
    Type? SystemType,
    EvalException? Error,
    double ParseMilliseconds,
    double EvaluateMilliseconds,
    bool CacheHit,
    bool NumericFastPath,
    string Tree)
{
    public static EvaluationOutcome Failed(string formula, EvalException error) =>
        new(formula, false, string.Empty, null, EvalType.Unknown, null, error, 0d, 0d, false, false, string.Empty);
}

/// <summary>
/// 界面与计算组件之间的会话状态：持有变量表、函数库与 <see cref="Evaluator"/>。
/// 组件重建后（语法 / 选项 / 函数库变化）会触发 <see cref="EngineChanged"/>。
/// </summary>
public sealed class EvaluationSession
{
    private readonly List<LibraryEntry> _libraries = [];
    private Evaluator _evaluator;
    private ParserSyntax _syntax = ParserSyntax.CSharp;
    private PercentMode _percentMode = PercentMode.Modulo;
    private bool _caseSensitive;

    public EvaluationSession()
    {
        Variables = new DictionaryVariableBag();
        _libraries.Add(new LibraryEntry("数学函数库", "Abs / Round / Sum / Sqrt …", typeof(MathLibrary)));
        _libraries.Add(new LibraryEntry("文本函数库", "Upper / Left / Format …", typeof(TextLibrary)));
        _libraries.Add(new LibraryEntry("日期函数库", "Year / AddDays / DaysBetween …", typeof(DateLibrary)));
        _libraries.Add(new LibraryEntry("财税函数库", "Vat / NetOfTax / Npv …", typeof(FinanceLibrary)));
        _libraries.Add(new LibraryEntry("水蒸汽物性库", "P2T / P2HL / PT2H / PH2T …（IAPWS-IF97）", typeof(ThermoLibrary)));
        _evaluator = BuildEvaluator();
        SeedSampleData();
    }

    public DictionaryVariableBag Variables { get; }

    public Evaluator Evaluator => _evaluator;

    public IReadOnlyList<LibraryEntry> Libraries => _libraries;

    /// <summary>示例公式。</summary>
    public string SampleFormula { get; private set; } = "Round(单价 * 数量 * (1 + 税率), 2)";

    public event EventHandler? EngineChanged;

    public ParserSyntax Syntax
    {
        get => _syntax;
        set
        {
            if (_syntax != value)
            {
                _syntax = value;
                Rebuild();
            }
        }
    }

    public PercentMode PercentMode
    {
        get => _percentMode;
        set
        {
            if (_percentMode != value)
            {
                _percentMode = value;
                Rebuild();
            }
        }
    }

    public bool CaseSensitive
    {
        get => _caseSensitive;
        set
        {
            if (_caseSensitive != value)
            {
                _caseSensitive = value;
                Rebuild();
            }
        }
    }

    /// <summary>状态栏摘要。</summary>
    public string Summary =>
        $"语法 {(_syntax == ParserSyntax.CSharp ? "C#" : "VB")} · 变量 {Variables.Count} · 启用函数库 {_libraries.Count(l => l.Enabled)}/{_libraries.Count} · 公式缓存 {_evaluator.CachedFormulaCount}";

    /// <summary>按名称启用 / 停用函数库。</summary>
    public void SetLibraryEnabled(string name, bool enabled)
    {
        foreach (LibraryEntry entry in _libraries)
        {
            if (entry.Name == name && entry.Enabled != enabled)
            {
                entry.Enabled = enabled;
                Rebuild();
                return;
            }
        }
    }

    /// <summary>重建计算组件（保持变量与函数库配置）。</summary>
    public void Rebuild()
    {
        _evaluator = BuildEvaluator();
        EngineChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>变量发生变化后调用：清空公式缓存并通知界面。</summary>
    public void NotifyVariablesChanged()
    {
        _evaluator.ClearCache();
        EngineChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>恢复自带的示例变量。</summary>
    public void ResetSampleVariables()
    {
        Variables.Clear();
        SeedSampleData();
        NotifyVariablesChanged();
    }

    /// <summary>
    /// 预热：先跑几个典型公式，把解析器与调用委托的 JIT 成本挪到启动阶段，
    /// 避免界面首次验算显示虚高的耗时。
    /// </summary>
    public void WarmUp()
    {
        _evaluator.Evaluate("1 + 1");
        _evaluator.Evaluate("Round(1.234, 2)");
        _evaluator.Evaluate(SampleFormula);
        _evaluator.Evaluate("\"合计：%[单价 * 数量]\"");
        _evaluator.ClearCache();
    }

    /// <summary>执行一次验算，返回结果、耗时与表达式树。</summary>
    public EvaluationOutcome Evaluate(string? formula)
    {
        string text = formula ?? string.Empty;
        bool cacheHit = _evaluator.TryGetCachedFormula(text, out _);
        try
        {
            var parseWatch = Stopwatch.StartNew();
            CompiledFormula compiled = _evaluator.Compile(text);
            parseWatch.Stop();

            var evaluateWatch = Stopwatch.StartNew();
            object? value = compiled.Evaluate();
            evaluateWatch.Stop();

            return new EvaluationOutcome(
                text,
                true,
                ValueConvert.ConvertToString(value),
                value,
                compiled.Root.EvalType,
                compiled.Root.SystemType,
                null,
                parseWatch.Elapsed.TotalMilliseconds,
                evaluateWatch.Elapsed.TotalMilliseconds,
                cacheHit,
                compiled.HasNumericFastPath,
                ExpressionTreeFormatter.Dump(compiled.Root));
        }
        catch (EvalException ex)
        {
            return EvaluationOutcome.Failed(text, ex);
        }
#pragma warning disable CA1031
        catch (Exception ex)
#pragma warning restore CA1031
        {
            return EvaluationOutcome.Failed(text, new FormulaRuntimeException(text, ex));
        }
    }

    private Evaluator BuildEvaluator()
    {
        var evaluator = new Evaluator(new EvaluatorOptions
        {
            Syntax = _syntax,
            PercentMode = _percentMode,
            CaseSensitive = _caseSensitive,
        });

        evaluator.AddEnvironmentFunctions(Variables);
        foreach (LibraryEntry entry in _libraries)
        {
            if (entry.Enabled)
            {
                evaluator.AddEnvironmentFunctions(entry.Target);
            }
        }

        return evaluator;
    }

    private void SeedSampleData()
    {
        Variables.Set("单价", 12.5, "示例：单价（元）");
        Variables.Set("数量", 8, "示例：数量");
        Variables.Set("税率", 0.13, "示例：增值税率");
        Variables.Set("项目名称", "示例工程", "示例：文本变量");
        Variables.Set("开工日期", new DateTime(2024, 3, 1), "示例：日期变量");
    }
}
