using System.Collections.Concurrent;
using System.Reflection;
using Kurisu.RemoteCall.Proxy.Abstractions;

namespace Kurisu.RemoteCall.Proxy;

/// <summary>
/// Aop代理 -- castle.core.async
/// </summary>
internal abstract class Aop : IAsyncInterceptor
{
    private delegate void GenericSynchronousHandler(Aop me, IProxyInvocation invocation);

    private static readonly MethodInfo InterceptSynchronousResultGenericMethodInfo = typeof(Aop).GetMethod(nameof(InterceptSynchronousResult), BindingFlags.NonPublic | BindingFlags.Static);

    private static readonly ConcurrentDictionary<Type, GenericSynchronousHandler> GenericSynchronousHandlers = new()
    {
        [typeof(void)] = InterceptSynchronousVoid
    };

    /// <summary>
    /// 拦截同步
    /// </summary>
    /// <param name="invocation"></param>
    public void Intercept(IProxyInvocation invocation)
    {
        var returnType = invocation.Method.ReturnType;
        var handler = GenericSynchronousHandlers.GetOrAdd(returnType, static t =>
        {
            var m = InterceptSynchronousResultGenericMethodInfo.MakeGenericMethod(t);
            return m.CreateDelegate<GenericSynchronousHandler>();
        });

        handler.Invoke(this, invocation);
    }

    /// <summary>
    /// 拦截异步
    /// </summary>
    /// <param name="invocation"></param>
    public void InterceptAsync(IProxyInvocation invocation)
    {
        invocation.ReturnValue = HandleAsync(invocation);
    }

    /// <summary>
    /// 拦截异步
    /// </summary>
    /// <param name="invocation"></param>
    /// <typeparam name="TResult"></typeparam>
    public void InterceptAsync<TResult>(IProxyInvocation invocation)
    {
        invocation.ReturnValue = HandleAsync<TResult>(invocation);
    }

    /// <summary>
    /// 拦截异步
    /// </summary>
    /// <param name="invocation"></param>
    /// <returns></returns>
    protected abstract Task HandleAsync(IProxyInvocation invocation);

    /// <summary>
    /// 拦截异步
    /// </summary>
    /// <param name="invocation"></param>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    protected abstract Task<TResult> HandleAsync<TResult>(IProxyInvocation invocation);

    private static void InterceptSynchronousVoid(Aop me, IProxyInvocation invocation)
    {
        var task = me.HandleAsync(invocation);

        // If the intercept task has yet to complete, wait for it.
        if (!task.IsCompleted)
        {
            // Need to use Task.Run() to prevent deadlock in .NET Framework ASP.NET requests.
            // GetAwaiter().GetResult() prevents a thrown exception being wrapped in a AggregateException.
            // See https://stackoverflow.com/a/17284612
            Task.Run(() => task).GetAwaiter().GetResult();
        }

        task.RethrowIfFaulted();
    }

    private static void InterceptSynchronousResult<TResult>(Aop me, IProxyInvocation invocation)
    {
        var task = me.HandleAsync<TResult>(invocation);

        // If the intercept task has yet to complete, wait for it.
        // Need to use Task.Run() to prevent deadlock in .NET Framework ASP.NET requests.
        // GetAwaiter().GetResult() prevents a thrown exception being wrapped in a AggregateException.
        // See https://stackoverflow.com/a/17284612
        invocation.ReturnValue = task.IsCompleted
            ? task.Result
            : Task.Run(() => task).GetAwaiter().GetResult();

        task.RethrowIfFaulted();
    }
}