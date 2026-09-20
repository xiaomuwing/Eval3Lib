using System.Reflection;

namespace Eval3;

/// <summary>成员种类过滤。</summary>
[Flags]
internal enum CallType
{
    Field = 1,
    Method = 2,
    Property = 4,
    All = Field | Method | Property,
}

/// <summary>
/// 递归下降语法分析器（对应历史版本的 <c>parser</c> 类）。
/// 一次遍历即产出完整的表达式树，并在解析期完成类型推导、重载选择与调用委托编译。
/// </summary>
internal sealed class Parser
{
    private static readonly Expression[] NoArguments = [];

    private readonly Evaluator _evaluator;
    private readonly string _text;
    private readonly string _rootFormula;
    private readonly int _offset;
    private readonly Tokenizer _tokenizer;
    private Token _current;
    private int _lastEnd;

    public Parser(Evaluator evaluator, string text)
        : this(evaluator, text, text, 0)
    {
    }

    private Parser(Evaluator evaluator, string text, string rootFormula, int offset)
    {
        _evaluator = evaluator;
        _text = text;
        _rootFormula = rootFormula;
        _offset = offset;
        _tokenizer = new Tokenizer(this, text);
    }

    /// <summary>解析公式。</summary>
    public Expression Parse()
    {
        NextToken();
        if (_current.Type == TokenType.EndOfFormula)
        {
            return ConstantExpression.EmptyString;
        }

        Expression? result = ParseExpr(null, Priority.None, TokenType.None);
        if (result is null)
        {
            throw RaiseUnexpectedToken("未找到表达式");
        }

        if (_current.Type != TokenType.EndOfFormula)
        {
            throw RaiseUnexpectedToken("公式结尾存在多余内容");
        }

        return result;
    }

    /// <summary>解析插值字符串中的子表达式。</summary>
    internal Expression ParseSubExpression(string text, int offset)
    {
        var parser = new Parser(_evaluator, text, _rootFormula, _offset + offset);
        return parser.Parse();
    }

    /// <summary>构造带全局位置信息的语法异常。</summary>
    internal ParserException Error(string message, int position, int length = 1) =>
        new(message, _rootFormula, position + _offset, length);

    /// <summary>把表达式转换为目标逻辑类型。</summary>
    internal Expression Convert(Expression expression, EvalType targetType, int position)
    {
        if (expression.EvalType == targetType)
        {
            return expression;
        }

        if (expression.CanReturn(targetType))
        {
            return new ConvertExpression(expression, targetType);
        }

        throw Error($"无法把 {expression.Name} 转换为 {targetType}", position);
    }

    private void NextToken()
    {
        _current = _tokenizer.NextToken();
        _lastEnd = _current.End;
    }

    private string SourceOf(Token token) => _text.Substring(token.Start, token.Length);

    private string Source(int start) => _text.Substring(start, Math.Max(0, _lastEnd - start));

    private ParserException RaiseUnexpectedToken(string? message = null, int? position = null)
    {
        int start = position ?? _current.Start;
        string prefix = string.IsNullOrEmpty(message) ? string.Empty : message + "; ";
        string tokenName = _current.Type.ToString().Replace('_', ' ');
        string tokenText = _current.Length > 0 ? SourceOf(_current) : "公式末尾";
        return Error($"{prefix}意外的记号 {tokenName} : {tokenText}", start, Math.Max(1, _current.Length));
    }

    /// <summary>
    /// 表达式解析主体。
    /// <paramref name="leftContext"/> 为所在二元运算的左操作数（供 <c>%</c> 使用），
    /// <paramref name="enclosingOperator"/> 为触发本次递归的运算符。
    /// </summary>
    private Expression? ParseExpr(Expression? leftContext, Priority priority, TokenType enclosingOperator)
    {
        Expression? valueLeft = null;

        while (valueLeft is null)
        {
            int start = _current.Start;
            switch (_current.Type)
            {
                case TokenType.OperatorMinus:
                {
                    NextToken();
                    Expression operand = ParseExpr(null, Priority.UnaryMinus, TokenType.OperatorMinus)
                        ?? throw RaiseUnexpectedToken("一元负号缺少操作数", start);
                    valueLeft = new NegateExpression(Convert(operand, EvalType.Number, start), Source(start));
                    break;
                }

                case TokenType.OperatorPlus:
                    // 一元正号：忽略后继续解析基本表达式
                    NextToken();
                    continue;

                case TokenType.OperatorNot:
                {
                    NextToken();
                    Expression operand = ParseExpr(null, Priority.Not, TokenType.OperatorNot)
                        ?? throw RaiseUnexpectedToken("not 缺少操作数", start);
                    valueLeft = new NotExpression(Convert(operand, EvalType.Boolean, start), Source(start));
                    break;
                }

                case TokenType.ValueIdentifier:
                {
                    string identifier = _current.Text ?? string.Empty;
                    ParseIdentifier(ref valueLeft, identifier, start);
                    if (valueLeft is null)
                    {
                        throw RaiseUnexpectedToken($"无法解析标识符 {identifier}", start);
                    }

                    break;
                }

                case TokenType.ValueTrue:
                    valueLeft = new ConstantExpression(EvalType.Boolean, true, SourceOf(_current));
                    NextToken();
                    break;

                case TokenType.ValueFalse:
                    valueLeft = new ConstantExpression(EvalType.Boolean, false, SourceOf(_current));
                    NextToken();
                    break;

                case TokenType.ValueNumber:
                    valueLeft = ConstantExpression.Number(_current.Number, SourceOf(_current));
                    NextToken();
                    break;

                case TokenType.ValueDate:
                    valueLeft = ConstantExpression.Date(_current.Date, SourceOf(_current));
                    NextToken();
                    break;

                case TokenType.ValueString:
                    valueLeft = _current.Parts is { Count: > 0 } parts
                        ? new StringTemplateExpression(parts, SourceOf(_current))
                        : ConstantExpression.Text(_current.Text ?? string.Empty, SourceOf(_current));
                    NextToken();
                    break;

                case TokenType.OpenParenthesis:
                {
                    NextToken();
                    valueLeft = ParseExpr(null, Priority.None, TokenType.None)
                        ?? throw RaiseUnexpectedToken("括号内缺少表达式", start);
                    if (_current.Type == TokenType.CloseParenthesis)
                    {
                        NextToken();
                    }
                    else
                    {
                        throw RaiseUnexpectedToken("缺少右括号 ')'");
                    }

                    break;
                }

                case TokenType.OperatorIf:
                    valueLeft = ParseConditional();
                    break;

                case TokenType.EndOfFormula:
                case TokenType.CloseParenthesis:
                case TokenType.CloseBracket:
                case TokenType.Comma:
                    return null;

                default:
                    throw RaiseUnexpectedToken();
            }
        }

        ParseDot(ref valueLeft);

        while (true)
        {
            TokenType tokenType = _current.Type;
            switch (tokenType)
            {
                case TokenType.EndOfFormula:
                    return valueLeft;

                case TokenType.OperatorPlus:
                case TokenType.OperatorMinus:
                case TokenType.OperatorConcat:
                case TokenType.OperatorMul:
                case TokenType.OperatorDiv:
                case TokenType.OperatorOr:
                case TokenType.OperatorAnd:
                case TokenType.OperatorNe:
                case TokenType.OperatorGt:
                case TokenType.OperatorGe:
                case TokenType.OperatorEq:
                case TokenType.OperatorLe:
                case TokenType.OperatorLt:
                {
                    Priority required = ExpressionFactory.PriorityOf(tokenType);
                    if (priority >= required)
                    {
                        return valueLeft;
                    }

                    int operatorPosition = _current.Start;
                    NextToken();
                    Expression right = ParseExpr(valueLeft, required, tokenType)
                        ?? throw RaiseUnexpectedToken($"运算符 {ExpressionFactory.SymbolOf(tokenType)} 缺少右操作数", operatorPosition);
                    valueLeft = CreateBinary(tokenType, valueLeft!, right, operatorPosition);
                    break;
                }

                case TokenType.OperatorPercent:
                {
                    bool modulo = _evaluator.Options.PercentMode == PercentMode.Modulo;
                    Priority required = modulo ? Priority.MulDiv : Priority.Percent;
                    if (priority >= required)
                    {
                        return valueLeft;
                    }

                    int operatorPosition = _current.Start;
                    NextToken();
                    valueLeft = modulo
                        ? CreateModulo(valueLeft!, operatorPosition)
                        : CreatePercent(
                            valueLeft!,
                            enclosingOperator is TokenType.OperatorMul or TokenType.OperatorDiv ? null : leftContext,
                            operatorPosition);
                    break;
                }

                default:
                    return valueLeft;
            }
        }
    }

    private Expression CreateBinary(TokenType tokenType, Expression left, Expression right, int operatorPosition)
    {
        string source = _text.Substring(operatorPosition, Math.Max(0, _lastEnd - operatorPosition));
        if (tokenType is TokenType.OperatorAnd or TokenType.OperatorOr)
        {
            return new LogicalExpression(
                Convert(left, EvalType.Boolean, operatorPosition),
                Convert(right, EvalType.Boolean, operatorPosition),
                tokenType == TokenType.OperatorAnd,
                source);
        }

        return ExpressionFactory.CreateBinary(this, tokenType, left, right, source, operatorPosition);
    }

    private Expression CreateModulo(Expression left, int operatorPosition)
    {
        Expression right = ParseExpr(left, Priority.MulDiv, TokenType.OperatorPercent)
            ?? throw RaiseUnexpectedToken("运算符 % 缺少右操作数", operatorPosition);
        string source = _text.Substring(operatorPosition, Math.Max(0, _lastEnd - operatorPosition));
        return ExpressionFactory.CreateBinary(this, TokenType.OperatorPercent, left, right, source, operatorPosition);
    }

    private PercentExpression CreatePercent(Expression operand, Expression? context, int operatorPosition)
    {
        string source = _text.Substring(operatorPosition, Math.Max(0, _lastEnd - operatorPosition));
        return new PercentExpression(Convert(operand, EvalType.Number, operatorPosition), context, source);
    }

    /// <summary>链式成员访问：<c>a.b.c</c>、<c>a(1)</c>。</summary>
    private void ParseDot(ref Expression? valueLeft)
    {
        while (true)
        {
            switch (_current.Type)
            {
                case TokenType.Dot:
                    NextToken();
                    break;
                case TokenType.OpenParenthesis:
                    break;
                default:
                    return;
            }

            if (_current.Type != TokenType.ValueIdentifier)
            {
                throw RaiseUnexpectedToken("缺少成员名");
            }

            string identifier = _current.Text ?? string.Empty;
            ParseIdentifier(ref valueLeft, identifier, _current.Start);
            if (valueLeft is null)
            {
                throw RaiseUnexpectedToken($"无法解析成员 {identifier}");
            }
        }
    }

    private void ParseIdentifier(ref Expression? valueLeft, string identifier, int start)
    {
        NextToken();
        List<Expression>? parameters = ParseParameters(out bool isBrackets);
        string source = Source(start);

        if (parameters is not null)
        {
            bool parametersNotUsed = false;
            if (_evaluator.Syntax == ParserSyntax.VisualBasic)
            {
                if (!EmitCallFunction(ref valueLeft, identifier, parameters, CallType.All, errorIfNotFound: false, start))
                {
                    EmitCallFunction(ref valueLeft, identifier, NoArguments, CallType.All, errorIfNotFound: true, start);
                    parametersNotUsed = true;
                }
            }
            else if (isBrackets)
            {
                if (!EmitCallFunction(ref valueLeft, identifier, parameters, CallType.Property, errorIfNotFound: false, start))
                {
                    EmitCallFunction(ref valueLeft, identifier, NoArguments, CallType.All, errorIfNotFound: true, start);
                    parametersNotUsed = true;
                }
            }
            else if (!EmitCallFunction(
                ref valueLeft,
                identifier,
                parameters,
                CallType.Field | CallType.Method,
                errorIfNotFound: false,
                start))
            {
                EmitCallFunction(ref valueLeft, identifier, NoArguments, CallType.All, errorIfNotFound: true, start);
                parametersNotUsed = true;
            }

            if (parametersNotUsed)
            {
                Type baseType = valueLeft!.SystemType;
                if (baseType.IsArray)
                {
                    if (parameters.Count == baseType.GetArrayRank())
                    {
                        valueLeft = new ArrayIndexExpression(valueLeft, parameters, source, baseType.GetElementType());
                    }
                    else
                    {
                        throw Error($"该数组是 {baseType.GetArrayRank()} 维，参数个数不匹配", start);
                    }
                }
                else
                {
                    MemberInfo? member = MemberResolver.SelectBest(
                        _evaluator.Resolver.GetMembers(baseType, null, !_evaluator.CaseSensitive),
                        parameters);
                    if (member is null)
                    {
                        throw Error("此处不支持参数：既不是数组，也没有匹配的索引器 / 默认属性", start);
                    }

                    valueLeft = MethodCallExpression.Create(valueLeft, member, parameters, source);
                }
            }

            return;
        }

        EmitCallFunction(ref valueLeft, identifier, null, CallType.All, errorIfNotFound: true, start);
    }

    private List<Expression>? ParseParameters(out bool brackets)
    {
        brackets = false;
        bool isParenthesis = _current.Type == TokenType.OpenParenthesis;
        bool isBracket = _current.Type == TokenType.OpenBracket && _evaluator.Syntax == ParserSyntax.CSharp;
        if (!isParenthesis && !isBracket)
        {
            return null;
        }

        brackets = isBracket;
        TokenType closing = isBracket ? TokenType.CloseBracket : TokenType.CloseParenthesis;
        var parameters = new List<Expression>(4);
        NextToken();

        while (true)
        {
            if (_current.Type == closing)
            {
                NextToken();
                return parameters;
            }

            Expression argument = ParseExpr(null, Priority.None, TokenType.None)
                ?? throw RaiseUnexpectedToken("参数缺少表达式");
            parameters.Add(argument);

            if (_current.Type == closing)
            {
                NextToken();
                return parameters;
            }

            if (_current.Type == TokenType.Comma)
            {
                NextToken();
                continue;
            }

            throw RaiseUnexpectedToken($"缺少 {ExpressionFactory.SymbolOf(closing)}");
        }
    }

    private ConditionalExpression ParseConditional()
    {
        int start = _current.Start;
        NextToken();
        List<Expression>? parameters = ParseParameters(out _);
        if (parameters is null)
        {
            throw Error("if 需要参数列表，例如 if(条件, 真值, 假值)", start);
        }

        if (parameters.Count != 3)
        {
            throw Error($"if 需要 3 个参数，实际 {parameters.Count} 个", start);
        }

        return new ConditionalExpression(
            Convert(parameters[0], EvalType.Boolean, start),
            parameters[1],
            parameters[2],
            Source(start));
    }

    private bool EmitCallFunction(
        ref Expression? valueLeft,
        string funcName,
        IReadOnlyList<Expression>? parameters,
        CallType callType,
        bool errorIfNotFound,
        int position)
    {
        Expression? result;
        string source = Source(position);

        if (valueLeft is null)
        {
            result = null;
            foreach (object functions in _evaluator.Functions)
            {
                object? current = functions;
                while (current is not null)
                {
                    Type libraryType = current as Type ?? current.GetType();
                    object? libraryInstance = current as Type is null ? current : null;
                    result = TryGetLocalFunction(libraryInstance, libraryType, funcName, parameters, callType, source, position);
                    if (result is not null)
                    {
                        break;
                    }

                    current = current is IInheritedFunctions inherited ? inherited.InheritedFunctions : null;
                }

                if (result is not null)
                {
                    break;
                }
            }
        }
        else
        {
            result = TryGetLocalFunction(valueLeft, valueLeft.SystemType, funcName, parameters, callType, source, position);
        }

        if (result is null)
        {
            if (errorIfNotFound)
            {
                throw Error($"找不到变量或方法 {funcName}", position, Math.Max(1, funcName.Length));
            }

            return false;
        }

        valueLeft = result;
        return true;
    }

    private Expression? TryGetLocalFunction(
        object? baseObject,
        Type baseType,
        string funcName,
        IReadOnlyList<Expression>? parameters,
        CallType callType,
        string source,
        int position)
    {
        // 变量容器优先：容器内的变量名不受容器自身公共成员（如 Count）干扰。
        if (baseObject is IVariableBag bag && (parameters is null || parameters.Count == 0))
        {
            ITypedValue? variable = bag.GetVariable(funcName);
            if (variable is not null)
            {
                return new VariableExpression(variable, source);
            }
        }

        MemberInfo? member = MemberResolver.SelectBest(
            _evaluator.Resolver.GetMembers(baseType, funcName, !_evaluator.CaseSensitive),
            parameters);

        if (member is not null)
        {
            switch (member.MemberType)
            {
                case MemberTypes.Field when (callType & CallType.Field) == 0:
                    throw Error($"此处不支持字段访问 {funcName}", position);
                case MemberTypes.Method when (callType & CallType.Method) == 0:
                    throw Error($"此处不支持方法调用 {funcName}", position);
                case MemberTypes.Property when (callType & CallType.Property) == 0:
                    throw Error($"此处不支持属性访问 {funcName}", position);
                case MemberTypes.Field:
                case MemberTypes.Method:
                case MemberTypes.Property:
                    return MethodCallExpression.Create(baseObject, member, parameters, source);
                default:
                    throw Error($"{member.MemberType} 成员不受支持", position);
            }
        }

        return null;
    }
}
