namespace Eval3;

/// <summary>二元运算节点工厂：按操作数的静态类型选择运算符实现（历史版本在 <c>opCodeBinary</c> 构造函数中完成）。</summary>
internal static class ExpressionFactory
{
    public static Expression CreateBinary(
        Parser parser,
        TokenType tokenType,
        Expression left,
        Expression right,
        string source,
        int position)
    {
        switch (tokenType)
        {
            case TokenType.OperatorPlus:
                if (left.EvalType == EvalType.Number && right.EvalType == EvalType.Number)
                {
                    return new BinaryExpression(left, Operators.Add, right, source);
                }

                if (left.EvalType == EvalType.Number && right.EvalType == EvalType.Date)
                {
                    return new BinaryExpression(right, DateOperator.AddDays, left, source);
                }

                if (left.EvalType == EvalType.Date && right.EvalType == EvalType.Number)
                {
                    return new BinaryExpression(left, DateOperator.AddDays, right, source);
                }

                if (left.CanReturn(EvalType.String) && right.CanReturn(EvalType.String))
                {
                    return new BinaryExpression(
                        parser.Convert(left, EvalType.String, position),
                        ConcatOperator.Plus,
                        parser.Convert(right, EvalType.String, position),
                        source);
                }

                break;

            case TokenType.OperatorMinus:
                if (left.EvalType == EvalType.Number && right.EvalType == EvalType.Number)
                {
                    return new BinaryExpression(left, Operators.Subtract, right, source);
                }

                if (left.EvalType == EvalType.Date && right.EvalType == EvalType.Number)
                {
                    return new BinaryExpression(left, DateOperator.SubtractDays, right, source);
                }

                if (left.EvalType == EvalType.Date && right.EvalType == EvalType.Date)
                {
                    return new BinaryExpression(left, DateOperator.Difference, right, source);
                }

                break;

            case TokenType.OperatorMul:
                if (left.EvalType == EvalType.Number && right.EvalType == EvalType.Number)
                {
                    return new BinaryExpression(left, Operators.Multiply, right, source);
                }

                break;

            case TokenType.OperatorDiv:
                if (left.EvalType == EvalType.Number && right.EvalType == EvalType.Number)
                {
                    return new BinaryExpression(left, Operators.Divide, right, source);
                }

                break;

            case TokenType.OperatorPercent:
                if (left.EvalType == EvalType.Number && right.EvalType == EvalType.Number)
                {
                    return new BinaryExpression(left, Operators.Modulo, right, source);
                }

                break;

            case TokenType.OperatorConcat:
                if (left.CanReturn(EvalType.String) && right.CanReturn(EvalType.String))
                {
                    return new BinaryExpression(
                        parser.Convert(left, EvalType.String, position),
                        ConcatOperator.Ampersand,
                        parser.Convert(right, EvalType.String, position),
                        source);
                }

                break;

            case TokenType.OperatorEq:
            case TokenType.OperatorNe:
            case TokenType.OperatorLt:
            case TokenType.OperatorLe:
            case TokenType.OperatorGt:
            case TokenType.OperatorGe:
                return new BinaryExpression(
                    left,
                    new ComparisonOperator(SymbolOf(tokenType), KindOf(tokenType), SelectComparer(left, right)),
                    right,
                    source);
        }

        throw parser.Error(
            $"运算符 {SymbolOf(tokenType)} 不能应用于 {left.EvalType} 和 {right.EvalType}",
            position);
    }

    public static Priority PriorityOf(TokenType tokenType) => tokenType switch
    {
        TokenType.OperatorPlus or TokenType.OperatorMinus => Priority.PlusMinus,
        TokenType.OperatorConcat => Priority.Concat,
        TokenType.OperatorMul or TokenType.OperatorDiv => Priority.MulDiv,
        TokenType.OperatorPercent => Priority.Percent,
        TokenType.OperatorAnd => Priority.And,
        TokenType.OperatorOr => Priority.Or,
        _ => Priority.Equality,
    };

    public static string SymbolOf(TokenType tokenType) => tokenType switch
    {
        TokenType.OperatorPlus => "+",
        TokenType.OperatorMinus => "-",
        TokenType.OperatorMul => "*",
        TokenType.OperatorDiv => "/",
        TokenType.OperatorPercent => "%",
        TokenType.OperatorConcat => "&",
        TokenType.OperatorEq => "=",
        TokenType.OperatorNe => "<>",
        TokenType.OperatorLt => "<",
        TokenType.OperatorLe => "<=",
        TokenType.OperatorGt => ">",
        TokenType.OperatorGe => ">=",
        TokenType.OperatorAnd => "and",
        TokenType.OperatorOr => "or",
        TokenType.OperatorNot => "not",
        TokenType.CloseParenthesis => ")",
        TokenType.CloseBracket => "]",
        TokenType.OpenParenthesis => "(",
        TokenType.OpenBracket => "[",
        TokenType.Comma => ",",
        _ => tokenType.ToString(),
    };

    private static ComparisonKind KindOf(TokenType tokenType) => tokenType switch
    {
        TokenType.OperatorEq => ComparisonKind.Equal,
        TokenType.OperatorNe => ComparisonKind.NotEqual,
        TokenType.OperatorLt => ComparisonKind.LessThan,
        TokenType.OperatorLe => ComparisonKind.LessThanOrEqual,
        TokenType.OperatorGt => ComparisonKind.GreaterThan,
        _ => ComparisonKind.GreaterThanOrEqual,
    };

    private static Func<object?, object?, int> SelectComparer(Expression left, Expression right)
    {
        if (left.EvalType == EvalType.Number && right.EvalType == EvalType.Number)
        {
            return Comparisons.Numeric;
        }

        if (left.EvalType == EvalType.Date && right.EvalType == EvalType.Date)
        {
            return Comparisons.Date;
        }

        if (left.EvalType == EvalType.Boolean && right.EvalType == EvalType.Boolean)
        {
            return Comparisons.Boolean;
        }

        return Comparisons.Text;
    }
}
