using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using FastExpressionCompiler;

namespace Eval3.Compilation;

/// <summary>
/// 表达式树编译后端：优先使用 <b>FastExpressionCompiler</b> 直接生成 IL，
/// 无法快速编译时自动回退到 <see cref="System.Linq.Expressions.Expression{TDelegate}.Compile()"/>。
/// </summary>
/// <remarks>
/// <para>
/// FastExpressionCompiler 与 <c>Expression.Compile()</c> 生成的是同一份 IL，求值性能相当，
/// 但编译耗时只有后者的 1/20 ~ 1/200（实测，见基准工具的 <c>--compiler</c>），
/// 因此特别适合"运行时按需编译、编译结果被缓存复用"的场景。
/// 本组件用它编译反射成员的调用委托（见 <c>MethodInvoker</c>）。
/// </para>
/// <para>
/// <see cref="PreferFastCompiler"/> 与统计计数是<b>进程级</b>状态，建议只在启动阶段修改。
/// 两条路径的求值语义完全一致，回退不会影响计算结果。
/// </para>
/// </remarks>
public static class ExpressionTreeCompiler
{
    private static long _fastCompiled;
    private static long _fallbackCompiled;
    private static volatile bool _preferFastCompiler = true;

    /// <summary>
    /// 是否优先使用 FastExpressionCompiler，默认 <see langword="true"/>。
    /// 置为 <see langword="false"/> 可强制走 <c>Expression.Compile()</c>（便于对照排查）。
    /// </summary>
    public static bool PreferFastCompiler
    {
        get => _preferFastCompiler;
        set => _preferFastCompiler = value;
    }

    /// <summary>由 FastExpressionCompiler 编译成功的表达式树数量。</summary>
    public static long FastCompiledCount => Interlocked.Read(ref _fastCompiled);

    /// <summary>回退到 <c>Expression.Compile()</c> 的表达式树数量。</summary>
    public static long FallbackCompiledCount => Interlocked.Read(ref _fallbackCompiled);

    /// <summary>清空编译统计计数。</summary>
    public static void ResetStatistics()
    {
        Interlocked.Exchange(ref _fastCompiled, 0);
        Interlocked.Exchange(ref _fallbackCompiled, 0);
    }

    /// <summary>编译强类型 lambda；FastExpressionCompiler 不可用时回退到 <c>Expression.Compile()</c>。</summary>
    /// <typeparam name="TDelegate">委托类型。</typeparam>
    /// <param name="lambda">待编译的表达式树。</param>
    /// <returns>已编译的委托。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="lambda"/> 为 <see langword="null"/>。</exception>
    public static TDelegate Compile<TDelegate>(Expression<TDelegate> lambda)
        where TDelegate : Delegate
    {
        ArgumentNullException.ThrowIfNull(lambda);

        if (_preferFastCompiler && TryCompileFast(lambda, out TDelegate? fast))
        {
            Interlocked.Increment(ref _fastCompiled);
            return fast;
        }

        Interlocked.Increment(ref _fallbackCompiled);
        return lambda.Compile();
    }

    private static bool TryCompileFast<TDelegate>(
        Expression<TDelegate> lambda,
        [NotNullWhen(true)] out TDelegate? compiled)
        where TDelegate : Delegate
    {
        try
        {
            // ifFastFailedReturnNull: 遇到不支持的表达式节点时返回 null，交由调用方回退。
            compiled = lambda.CompileFast(ifFastFailedReturnNull: true);
            return compiled is not null;
        }
        catch (Exception exception) when (IsFastCompilerFailure(exception))
        {
            compiled = null;
            return false;
        }
    }

    /// <summary>FastExpressionCompiler 已知的"放弃快速编译"异常类型，均可安全回退。</summary>
    private static bool IsFastCompilerFailure(Exception exception) => exception is
        NotSupportedExpressionException
        or NotSupportedException
        or NotImplementedException
        or InvalidOperationException
        or ArgumentException
        or PlatformNotSupportedException
        or TypeLoadException;
}
