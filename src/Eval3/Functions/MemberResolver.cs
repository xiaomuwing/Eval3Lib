using System.Collections.Concurrent;
using System.Reflection;

namespace Eval3;

/// <summary>
/// 反射成员解析器：带缓存地把 “类型 + 成员名” 解析为候选成员列表，
/// 并按参数静态类型完成重载打分（历史版本每次解析都重新反射并按运行时节点类型打分）。
/// </summary>
internal sealed class MemberResolver
{
    private const BindingFlags SearchFlags =
        BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
        BindingFlags.GetProperty | BindingFlags.GetField | BindingFlags.InvokeMethod |
        BindingFlags.FlattenHierarchy;

    private readonly ConcurrentDictionary<MemberCacheKey, MemberInfo[]> _cache = new();

    /// <summary>取候选成员。</summary>
    public MemberInfo[] GetMembers(Type type, string? name, bool ignoreCase)
    {
        var key = new MemberCacheKey(type, name, ignoreCase);
        return _cache.GetOrAdd(key, static k => Resolve(k));
    }

    /// <summary>按参数静态类型为候选成员打分，挑选最匹配的一个。</summary>
    public static MemberInfo? SelectBest(MemberInfo[] candidates, IReadOnlyList<Expression>? arguments)
    {
        if (candidates.Length == 0)
        {
            return null;
        }

        MemberInfo? best = null;
        int bestScore = 0;
        for (int i = 0; i < candidates.Length; i++)
        {
            int score = Score(candidates[i], arguments);
            if (score > bestScore)
            {
                bestScore = score;
                best = candidates[i];
            }
        }

        return best;
    }

    private static MemberInfo[] Resolve(MemberCacheKey key)
    {
        BindingFlags flags = key.IgnoreCase ? SearchFlags | BindingFlags.IgnoreCase : SearchFlags;
        try
        {
            return key.Name is null ? key.Type.GetDefaultMembers() : key.Type.GetMember(key.Name, flags);
        }
#pragma warning disable CA1031
        catch (Exception)
#pragma warning restore CA1031
        {
            return [];
        }
    }

    private static int Score(MemberInfo member, IReadOnlyList<Expression>? arguments)
    {
        ParameterInfo[] parameters = MemberMetadata.GetParameters(member);
        bool paramsArray = MemberMetadata.HasParamsArray(parameters);
        int argumentCount = arguments?.Count ?? 0;
        int fixedCount = paramsArray ? parameters.Length - 1 : parameters.Length;

        if (paramsArray ? argumentCount < fixedCount : argumentCount > parameters.Length)
        {
            return 0;
        }

        int score = 10;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (paramsArray && i == parameters.Length - 1)
            {
                Type elementType = parameters[i].ParameterType.GetElementType() ?? typeof(object);
                for (int j = i; j < argumentCount; j++)
                {
                    int element = Compatibility(arguments![j], elementType);
                    if (element <= 0)
                    {
                        return 0;
                    }

                    score += element;
                }

                continue;
            }

            if (i < argumentCount)
            {
                int compatibility = Compatibility(arguments![i], parameters[i].ParameterType);
                if (compatibility <= 0)
                {
                    return 0;
                }

                score += compatibility;
            }
            else
            {
                if (!parameters[i].IsOptional)
                {
                    return 0;
                }

                score += 10;
            }
        }

        // 参数个数完全匹配的定长重载优先于 params 重载（C# 重载决议的同款规则），
        // 否则 Sum(1,2,3) 会命中 params 版本并产生一次数组分配。
        if (!paramsArray && argumentCount == parameters.Length)
        {
            score++;
        }

        return score;
    }

    private static int Compatibility(Expression argument, Type parameterType)
    {
        Type argumentType = argument.SystemType;
        if (parameterType == argumentType)
        {
            return 12;
        }

        if (parameterType == typeof(object))
        {
            return 3;
        }

        if (parameterType.IsAssignableFrom(argumentType))
        {
            return 10;
        }

        EvalType parameterEvalType = TypeTable.GetEvalType(parameterType);
        if (parameterEvalType == EvalType.Number && argument.EvalType == EvalType.Number)
        {
            return 8;
        }

        if (parameterType == typeof(string) && argument.CanReturn(EvalType.String))
        {
            return 4;
        }

        return argument.EvalType == EvalType.Object ? 1 : 0;
    }

    private readonly record struct MemberCacheKey(Type Type, string? Name, bool IgnoreCase);
}
