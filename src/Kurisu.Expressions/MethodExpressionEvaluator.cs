using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace Kurisu.Expressions;

/// <summary>按方法参数名绑定表达式，缓存编译结果及参数数组调用适配器。应复用实例。</summary>
public sealed class MethodExpressionEvaluator(ExpressionCompiler compiler)
{
    private readonly ConcurrentDictionary<(MethodInfo Method, string Expression, Type Result), Lazy<Task<Delegate>>> _compiled = new();

    /// <summary>使用本次方法参数执行表达式，参数名和类型取自方法签名。</summary>
    /// <typeparam name="TResult">表达式结果类型。</typeparam>
    /// <param name="method">提供参数签名的方法。</param>
    /// <param name="expression">C# 表达式。</param>
    /// <param name="arguments">本次调用的参数值。</param>
    /// <param name="cancellationToken">取消本次等待。</param>
    /// <returns>表达式结果。</returns>
    public async Task<TResult> EvaluateAsync<TResult>(MethodInfo method, string expression, object?[] arguments,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var key = (method, expression, typeof(TResult));
        var entry = _compiled.GetOrAdd(key, _ => new Lazy<Task<Delegate>>(() => CompileAsync<TResult>(method, expression)));
        var task = entry.Value;
        Delegate evaluate;
        try { evaluate = await task.WaitAsync(cancellationToken).ConfigureAwait(false); }
        catch when (task.IsFaulted)
        {
            ((ICollection<KeyValuePair<(MethodInfo, string, Type), Lazy<Task<Delegate>>>>)_compiled).Remove(new(key, entry));
            throw;
        }
        return ((Func<object?[], TResult>)evaluate)(arguments);
    }

    private async Task<Delegate> CompileAsync<TResult>(MethodInfo method, string expression)
    {
        var parameters = method.GetParameters();
        var delegateType = Expression.GetDelegateType(parameters.Select(p => p.ParameterType).Append(typeof(TResult)).ToArray());
        return await ((Task<Delegate>)typeof(MethodExpressionEvaluator)
            .GetMethod(nameof(CompileTypedAsync), BindingFlags.NonPublic | BindingFlags.Instance)!
            .MakeGenericMethod(delegateType, typeof(TResult)).Invoke(this, [expression, parameters])!).ConfigureAwait(false);
    }

    private async Task<Delegate> CompileTypedAsync<TDelegate, TResult>(string expression, ParameterInfo[] parameters)
        where TDelegate : Delegate
    {
        // 元数据中的参数名不包含 C# 的 @ 转义前缀。
        var compiled = await compiler.CompileAsync<TDelegate>(expression, parameters.Select(p => "@" + p.Name!).ToArray()).ConfigureAwait(false);
        var arguments = Expression.Parameter(typeof(object[]), "arguments");
        var values = parameters.Select((p, i) => Expression.Convert(Expression.ArrayIndex(arguments, Expression.Constant(i)), p.ParameterType));
        return Expression.Lambda<Func<object?[], TResult>>(Expression.Invoke(Expression.Constant(compiled), values), arguments).Compile();
    }
}
