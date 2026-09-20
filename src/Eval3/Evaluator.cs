using System.Collections.Concurrent;

namespace Eval3;

/// <summary>
/// 计算组件入口（对应历史版本的 <c>Evaluator</c> 类）。
/// 解析产物 <see cref="Expression"/> 不持有可变状态，可重复求值与并发求值，适合批量验算场景。
/// </summary>
public sealed class Evaluator
{
    private readonly List<object> _functions = [];
    private readonly ConcurrentDictionary<string, CompiledFormula> _formulaCache = new(StringComparer.Ordinal);
    private readonly MemberResolver _resolver = new();

    /// <summary>使用默认配置创建组件。</summary>
    public Evaluator()
        : this(new EvaluatorOptions())
    {
    }

    /// <summary>按语法风格创建组件。</summary>
    public Evaluator(ParserSyntax syntax, bool caseSensitive = false)
        : this(new EvaluatorOptions { Syntax = syntax, CaseSensitive = caseSensitive })
    {
    }

    /// <summary>按配置创建组件。</summary>
    public Evaluator(EvaluatorOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.MaxCacheSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "MaxCacheSize 必须大于 0");
        }

        Options = options;
    }

    /// <summary>组件配置。</summary>
    public EvaluatorOptions Options { get; }

    /// <summary>公式语法风格。</summary>
    public ParserSyntax Syntax => Options.Syntax;

    /// <summary>是否区分大小写。</summary>
    public bool CaseSensitive => Options.CaseSensitive;

    /// <summary>已注册的函数库（实例，或静态库的 <see cref="Type"/>）。</summary>
    public IReadOnlyList<object> Functions => _functions;

    /// <summary>已缓存的公式数量。</summary>
    public int CachedFormulaCount => _formulaCache.Count;

    internal MemberResolver Resolver => _resolver;

    /// <summary>
    /// 注册函数库 / 变量容器。传入实例时使用实例成员，传入静态类 <see cref="Type"/> 时使用静态成员；
    /// 实现 <see cref="IVariableBag"/> 的对象可提供公式变量。
    /// </summary>
    public void AddEnvironmentFunctions(object? target)
    {
        if (target is null || _functions.Contains(target))
        {
            return;
        }

        _functions.Add(target);
    }

    /// <summary>注册多个函数库 / 变量容器。</summary>
    public void AddEnvironmentFunctions(params object?[] targets)
    {
        ArgumentNullException.ThrowIfNull(targets);
        foreach (object? target in targets)
        {
            AddEnvironmentFunctions(target);
        }
    }

    /// <summary>移除函数库 / 变量容器。</summary>
    public bool RemoveEnvironmentFunctions(object? target) => target is not null && _functions.Remove(target);

    /// <summary>清空已注册的函数库 / 变量容器。</summary>
    public void ClearEnvironmentFunctions() => _functions.Clear();

    /// <summary>解析公式为表达式树（不经过缓存）。</summary>
    public Expression Parse(string? formula)
    {
        var parser = new Parser(this, formula ?? string.Empty);
        return parser.Parse();
    }

    /// <summary>解析并缓存公式，返回可复用的 <see cref="CompiledFormula"/>。</summary>
    public CompiledFormula Compile(string? formula)
    {
        string text = formula ?? string.Empty;
        if (!Options.EnableFormulaCache)
        {
            return new CompiledFormula(text, Parse(text));
        }

        if (_formulaCache.TryGetValue(text, out CompiledFormula? cached))
        {
            return cached;
        }

        var compiled = new CompiledFormula(text, Parse(text));
        if (_formulaCache.Count >= Options.MaxCacheSize)
        {
            _formulaCache.Clear();
        }

        return _formulaCache.GetOrAdd(text, compiled);
    }

    /// <summary>解析并求值。</summary>
    public object? Evaluate(string? formula) => Compile(formula).Evaluate();

    /// <summary>解析并求值，结果按展示规则格式化。</summary>
    public string EvaluateToString(string? formula) => Compile(formula).EvaluateToString();

    /// <summary>按数值求值（纯数值公式走无装箱快路径）。</summary>
    public double EvaluateNumber(string? formula) => Compile(formula).EvaluateNumber();

    /// <summary>尝试解析并求值，失败时返回 <see langword="false"/> 并输出异常。</summary>
    public bool TryEvaluate(string? formula, out object? value, out EvalException? error)
    {
        try
        {
            value = Evaluate(formula);
            error = null;
            return true;
        }
        catch (EvalException ex)
        {
            value = null;
            error = ex;
            return false;
        }
        catch (Exception ex)
        {
            value = null;
            error = new FormulaRuntimeException(formula ?? string.Empty, ex);
            return false;
        }
    }

    /// <summary>尝试解析并求值，返回展示字符串。</summary>
    public bool TryEvaluateToString(string? formula, out string result, out EvalException? error)
    {
        if (TryEvaluate(formula, out object? value, out error))
        {
            result = ValueConvert.ConvertToString(value);
            return true;
        }

        result = string.Empty;
        return false;
    }

    /// <summary>清空公式缓存（变量静态类型变化后建议调用）。</summary>
    public void ClearCache() => _formulaCache.Clear();

    /// <summary>查询公式是否已在缓存中（界面可据此显示“缓存命中”）。</summary>
    public bool TryGetCachedFormula(string? formula, out CompiledFormula? compiled) =>
        _formulaCache.TryGetValue(formula ?? string.Empty, out compiled);

    /// <summary>格式化结果为展示字符串（等价于历史版本的 <c>Evaluator.ConvertToString</c>）。</summary>
    public static string ConvertToString(object? value) => ValueConvert.ConvertToString(value);
}
