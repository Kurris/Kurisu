using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading.Tasks;
using Kurisu.Proxy.Abstractions;

namespace Kurisu.Proxy;

/// <summary>
/// copy from castle.core.async
/// </summary>
public abstract class Aop : IAsyncInterceptor
{
    private static readonly MethodInfo InterceptSynchronousMethodInfo =
       typeof(Aop).GetMethod(
           nameof(InterceptSynchronousResult), BindingFlags.Static | BindingFlags.NonPublic)!;

    private static readonly ConcurrentDictionary<Type, GenericSynchronousHandler> GenericSynchronousHandlers = new()
    {
        [typeof(void)] = InterceptSynchronousVoid,
    };

    private delegate void GenericSynchronousHandler(Aop me, IProxyInvocation invocation);

    public void InterceptSynchronous(IProxyInvocation invocation)
    {
        Type returnType = invocation.Method.ReturnType;
        GenericSynchronousHandler handler = GenericSynchronousHandlers.GetOrAdd(returnType, CreateHandler);
        handler(this, invocation);
    }

    public void InterceptAsynchronous(IProxyInvocation invocation)
    {
        invocation.ReturnValue = InterceptAsync(invocation, ProceedAsynchronous);
    }

    public void InterceptAsynchronous<TResult>(IProxyInvocation invocation)
    {
        invocation.ReturnValue = InterceptAsync(invocation, ProceedAsynchronous<TResult>);
    }

    protected abstract Task InterceptAsync(IProxyInvocation invocation, Func<IProxyInvocation, Task> proceed);

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
            Task.Run(() => task).GetAwaiter().GetResult();
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
        MethodInfo method = InterceptSynchronousMethodInfo.MakeGenericMethod(returnType);
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
