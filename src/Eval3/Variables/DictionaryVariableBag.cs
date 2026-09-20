using System.Collections;
using System.Collections.Concurrent;

namespace Eval3;

/// <summary>
/// 基于字典的变量容器。读操作无锁、可安全并发，适合批量并行验算；
/// 变量集合在公式解析期被读取，修改变量请避免与解析并发进行。
/// </summary>
public sealed class DictionaryVariableBag : IVariableBag, IReadOnlyCollection<EvalVariable>
{
    private readonly ConcurrentDictionary<string, EvalVariable> _variables;

    /// <summary>创建变量容器。</summary>
    /// <param name="caseSensitive">变量名是否区分大小写。</param>
    public DictionaryVariableBag(bool caseSensitive = false)
    {
        CaseSensitive = caseSensitive;
        _variables = new ConcurrentDictionary<string, EvalVariable>(
            caseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>变量名是否区分大小写。</summary>
    public bool CaseSensitive { get; }

    /// <summary>变量数量。</summary>
    public int Count => _variables.Count;

    /// <summary>设置（或覆盖）变量，类型按值推断。</summary>
    public EvalVariable Set(string name, object? value, string description = "")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var variable = new EvalVariable(name, value, description);
        _variables[name] = variable;
        return variable;
    }

    /// <summary>设置（或覆盖）变量，并显式指定静态类型。</summary>
    public EvalVariable Set(string name, object? value, Type systemType, string description = "")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var variable = new EvalVariable(name, value, description, systemType);
        _variables[name] = variable;
        return variable;
    }

    /// <summary>添加变量实例（若同名已存在则覆盖）。</summary>
    public void Add(EvalVariable variable)
    {
        ArgumentNullException.ThrowIfNull(variable);
        _variables[variable.Name] = variable;
    }

    /// <summary>按名称取变量实例。</summary>
    public EvalVariable? Find(string name) =>
        _variables.TryGetValue(name, out EvalVariable? variable) ? variable : null;

    /// <summary>删除变量。</summary>
    public bool Remove(string name) => _variables.TryRemove(name, out _);

    /// <summary>清空变量。</summary>
    public void Clear() => _variables.Clear();

    /// <summary>批量设置变量。</summary>
    public void SetRange(IEnumerable<(string Name, object? Value, string Description)> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        foreach ((string name, object? value, string description) in items)
        {
            Set(name, value, description);
        }
    }

    /// <inheritdoc />
    public ITypedValue? GetVariable(string name) => Find(name);

    /// <inheritdoc />
    public IEnumerator<EvalVariable> GetEnumerator() => _variables.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
