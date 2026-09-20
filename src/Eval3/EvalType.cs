namespace Eval3;

/// <summary>
/// 表达式求值结果的逻辑类型分类。数值类型（含 decimal/float/整数族）统一归类为 <see cref="Number"/>。
/// </summary>
public enum EvalType
{
    /// <summary>未知类型。</summary>
    Unknown = 0,

    /// <summary>数值（内部统一使用 <see cref="double"/>）。</summary>
    Number,

    /// <summary>布尔值。</summary>
    Boolean,

    /// <summary>字符串。</summary>
    String,

    /// <summary>日期时间。</summary>
    Date,

    /// <summary>任意对象（走反射调用等慢路径）。</summary>
    Object,
}

/// <summary>公式语法风格。</summary>
public enum ParserSyntax
{
    /// <summary>C# 风格：<c>obj.Method(1,2)</c>、<c>array[0]</c>、双引号字符串。</summary>
    CSharp = 0,

    /// <summary>VB 风格：下标与调用都使用圆括号，兼容历史公式。</summary>
    VisualBasic = 1,
}

/// <summary>词法记号类型。</summary>
public enum TokenType
{
    None = 0,
    EndOfFormula,
    OperatorPlus,
    OperatorMinus,
    OperatorMul,
    OperatorDiv,
    OperatorPercent,
    OpenParenthesis,
    CloseParenthesis,
    Comma,
    Dot,
    OperatorNe,
    OperatorGt,
    OperatorGe,
    OperatorEq,
    OperatorLe,
    OperatorLt,
    OperatorAnd,
    OperatorOr,
    OperatorNot,
    OperatorConcat,
    OperatorIf,
    ValueIdentifier,
    ValueTrue,
    ValueFalse,
    ValueNumber,
    ValueString,
    ValueDate,
    OpenBracket,
    CloseBracket,
}

/// <summary>运算符优先级，数值越大优先级越高。</summary>
internal enum Priority
{
    None = 0,
    Or = 1,
    And = 2,
    Not = 3,
    Equality = 4,
    Concat = 5,
    PlusMinus = 6,
    MulDiv = 7,
    Percent = 8,
    UnaryMinus = 9,
}
