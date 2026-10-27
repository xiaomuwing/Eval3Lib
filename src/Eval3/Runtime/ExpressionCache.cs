namespace Eval3;

/// <summary>
/// 已解析表达式缓存（LRU）：编辑器反复校验 / 格式化同一条公式时，直接复用不可变 AST，
/// 避免重复的词法 / 语法分析分配。容量与公式缓存一致。
/// </summary>
internal sealed class ExpressionCache
{
    private readonly int _capacity;
    private readonly Dictionary<string, LinkedListNode<Entry>> _map;
    private readonly LinkedList<Entry> _lru = new();
    private readonly object _gate = new();

    public ExpressionCache(int capacity)
    {
        _capacity = Math.Max(1, capacity);
        _map = new Dictionary<string, LinkedListNode<Entry>>(Math.Min(capacity, 256), StringComparer.Ordinal);
    }

    public bool TryGet(string key, out Expression? value)
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

    public void Set(string key, Expression value)
    {
        lock (_gate)
        {
            if (_map.TryGetValue(key, out LinkedListNode<Entry>? existing))
            {
                existing.Value = new Entry(key, value);
                _lru.Remove(existing);
                _lru.AddFirst(existing);
                return;
            }

            var node = new LinkedListNode<Entry>(new Entry(key, value));
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

    private readonly record struct Entry(string Key, Expression Value);
}
