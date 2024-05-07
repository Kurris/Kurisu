using System.Collections.Concurrent;
using System.Reflection;
using Kurisu.RemoteCall.Proxy.Abstractions;
using Kurisu.RemoteCall.Proxy.Helpers;

namespace Kurisu.RemoteCall.Proxy;

/// <summary>
/// Aop代理 -- castle.core.async
/// </summary>
internal abstract class Aop : IAsyncInterceptor
{
    private static readonly MethodInfo _interceptSynchronousMethodInfo =
       typeof(Aop).GetMethod(
           nameof(InterceptSynchronousResult), BindingFlags.Static | BindingFlags.NonPublic)!;

    private static readonly ConcurrentDictionary<Type, GenericSynchronousHandler> _genericSynchronousHandlers = new()
    {
        [typeof(void)] = InterceptSynchronousVoid,
    };

    private delegate void GenericSynchronousHandler(Aop me, IProxyInvocation invocation);

    /// <summary>
    /// 拦截同步
    /// </summary>
    /// <param name="invocation"></param>
    public void InterceptSynchronous(IProxyInvocation invocation)
    {
        Type returnType = invocation.Method.ReturnType;
        GenericSynchronousHandler handler = _genericSynchronousHandlers.GetOrAdd(returnType, CreateHandler);
        handler(this, invocation);
    }

    /// <summary>
    /// 拦截异步
    /// </summary>
    /// <param name="invocation"></param>
    public void InterceptAsynchronous(IProxyInvocation invocation)
    {
        invocation.ReturnValue = InterceptAsync(invocation, ProceedAsynchronous);
    }

    /// <summary>
    /// 拦截异步
    /// </summary>
    /// <param name="invocation"></param>
    /// <typeparam name="TResult"></typeparam>
    public void InterceptAsynchronous<TResult>(IProxyInvocation invocation)
    {
        invocation.ReturnValue = InterceptAsync(invocation, ProceedAsynchronous<TResult>);
    }

    /// <summary>
    /// 拦截异步
    /// </summary>
    /// <param name="invocation"></param>
    /// <param name="proceed"></param>
    /// <returns></returns>
    protected abstract Task InterceptAsync(IProxyInvocation invocation, Func<IProxyInvocation, Task> proceed);

    /// <summary>
    /// 拦截异步
    /// </summary>
    /// <param name="invocation"></param>
    /// <param name="proceed"></param>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    protected abstract Task<TResult> InterceptAsync<TResult>(IProxyInvocation invocation, Func<IProxyInvocation, Task<TResult>> proceed);


    private static void InterceptSynchronousResult<TResult>(Aop me, IProxyInvocation invocation)
    {
        Task<TResult> task = me.InterceptAsync(invocation, ProceedSynchronous<TResult>);

        // If the intercept task has yet to complete, wait for it.
        if (!task.IsCompleted)
        {
            // Need to use Task.Run() to prevent deadlock in .NET Framework ASP.NET requests.
            // GetAwaiter().GetResult() prevents a thrown exception being wrapped in a AggregateException.
            // See https://stackoverflow.com/a/17284612
            invocation.ReturnValue = Task.Run(() => task).GetAwaiter().GetResult();
        }

        task.RethrowIfFaulted();

    }

    private static void InterceptSynchronousVoid(Aop me, IProxyInvocation invocation)
    {
        Task task = me.InterceptAsync(invocation, ProceedSynchronous);

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

    private static GenericSynchronousHandler CreateHandler(Type returnType)
    {
        MethodInfo method = _interceptSynchronousMethodInfo.MakeGenericMethod(returnType);
        return (GenericSynchronousHandler)method.CreateDelegate(typeof(GenericSynchronousHandler));
    }

    private static Task ProceedSynchronous(IProxyInvocation invocation)
    {
        try
        {
            invocation.Proceed();
            return Task.CompletedTask;
        }
        catch (Exception e)
        {
            return Task.FromException(e);
        }
    }


    private static Task<TResult> ProceedSynchronous<TResult>(IProxyInvocation invocation)
    {
        try
        {
            invocation.Proceed();
            return Task.FromResult((TResult)invocation.ReturnValue);
        }
        catch (Exception e)
        {
            return Task.FromException<TResult>(e);
        }
    }



    private static async Task ProceedAsynchronous(IProxyInvocation invocation)
    {
        invocation.Proceed();

        // Get the task to await.
        var originalReturnValue = (Task)invocation.ReturnValue;

        await originalReturnValue.ConfigureAwait(false);
    }

    private static async Task<TResult> ProceedAsynchronous<TResult>(IProxyInvocation invocation)
    {
        invocation.Proceed();

        // Get the task to await.
        var originalReturnValue = (Task<TResult>)invocation.ReturnValue;

        TResult result = await originalReturnValue.ConfigureAwait(false);
        return result;
    }
}
