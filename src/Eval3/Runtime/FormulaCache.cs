namespace Eval3;

/// <summary>
/// 公式缓存：按最近最少使用（LRU）淘汰，替代"超过上限整表清空"的抖动策略。
/// 读多写少，内部用一把锁保护；公式编译本身在锁外完成。
/// </summary>
internal sealed class FormulaCache
{
    private readonly int _capacity;
    private readonly Dictionary<string, LinkedListNode<Entry>> _map;
    private readonly LinkedList<Entry> _lru = new();
    private readonly object _gate = new();

    public FormulaCache(int capacity)
    {
        _capacity = Math.Max(1, capacity);
        _map = new Dictionary<string, LinkedListNode<Entry>>(Math.Min(capacity, 256), StringComparer.Ordinal);
    }

    public int Count
    {
        get
        {
            lock (_gate)
            {
                return _map.Count;
            }
        }
    }

    public bool TryGet(string key, out CompiledFormula? value)
    {
        lock (_gate)
        {
            if (_map.TryGetValue(key, out LinkedListNode<Entry>? node))
            {
                _lru.Remove(node);
                _lru.AddFirst(node);
                value = node.Value.Value;
                return true;
            }
        }

        value = null;
        return false;
    }

    public void Set(string key, CompiledFormula formula)
    {
        lock (_gate)
        {
            if (_map.TryGetValue(key, out LinkedListNode<Entry>? existing))
            {
                existing.Value = new Entry(key, formula);
                _lru.Remove(existing);
                _lru.AddFirst(existing);
                return;
            }

            var node = new LinkedListNode<Entry>(new Entry(key, formula));
            _lru.AddFirst(node);
            _map[key] = node;

            while (_map.Count > _capacity && _lru.Last is { } last)
            {
                _lru.RemoveLast();
                _map.Remove(last.Value.Key);
            }
        }
    }

    public void Clear()
    {
        lock (_gate)
        {
            _map.Clear();
            _lru.Clear();
        }
    }

    private readonly record struct Entry(string Key, CompiledFormula Value);
}
