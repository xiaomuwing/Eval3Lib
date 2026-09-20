namespace Eval3;

/// <summary>
/// 数组取值节点（C# 语法 <c>arr[0]</c> 或 VB 语法 <c>arr(0)</c>）。
/// 元素类型在解析期推导，避免求值期的反射与类型判断。
/// </summary>
public sealed class ArrayIndexExpression : Expression
{
    private readonly Expression _array;
    private readonly Expression[] _indices;
    private readonly Type _elementType;
    private readonly EvalType _elementEvalType;

    internal ArrayIndexExpression(Expression array, IReadOnlyList<Expression> indices, string source, Type? elementType)
        : base(source)
    {
        _array = array;
        _indices = [.. indices];
        _elementType = elementType ?? typeof(object);
        _elementEvalType = TypeTable.GetEvalType(_elementType);
    }

    /// <summary>数组表达式。</summary>
    public Expression Array => _array;

    /// <summary>下标表达式。</summary>
    public IReadOnlyList<Expression> Indices => _indices;

    /// <inheritdoc />
    public override EvalType EvalType => _elementEvalType;

    /// <inheritdoc />
    public override Type SystemType => _elementType;

    /// <inheritdoc />
    public override object? Evaluate()
    {
        if (_array.Evaluate() is not Array array)
        {
            throw new InvalidOperationException($"{_array.Name} 不是数组");
        }

        if (_indices.Length == 1)
        {
            return array.GetValue((int)_indices[0].EvaluateNumber());
        }

        int[] indexes = new int[_indices.Length];
        for (int i = 0; i < indexes.Length; i++)
        {
            indexes[i] = (int)_indices[i].EvaluateNumber();
        }

        return array.GetValue(indexes);
    }

    /// <inheritdoc />
    public override IReadOnlyList<Expression> Children => [_array, .. _indices];

    /// <inheritdoc />
    public override string Name => "数组取值";
}
