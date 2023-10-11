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

    private delegate void GenericSynchronousHandler(Aop me, IProxyInfo invocation);

    public void InterceptSynchronous(IProxyInfo invocation)
    {
        Type returnType = invocation.Method.ReturnType;
        GenericSynchronousHandler handler = GenericSynchronousHandlers.GetOrAdd(returnType, CreateHandler);
        handler(this, invocation);
    }

    public void InterceptAsynchronous(IProxyInfo invocation)
    {
        invocation.ReturnValue = InterceptAsync(invocation, ProceedAsynchronous);
    }

    public void InterceptAsynchronous<TResult>(IProxyInfo invocation)
    {
        invocation.ReturnValue = InterceptAsync(invocation, ProceedAsynchronous<TResult>);
    }

    protected abstract Task InterceptAsync(IProxyInfo invocation, Func<IProxyInfo, Task> proceed);

    protected abstract Task<TResult> InterceptAsync<TResult>(IProxyInfo invocation, Func<IProxyInfo, Task<TResult>> proceed);


    private static void InterceptSynchronousResult<TResult>(Aop me, IProxyInfo invocation)
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

    private static void InterceptSynchronousVoid(Aop me, IProxyInfo invocation)
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

    private static Task ProceedSynchronous(IProxyInfo invocation)
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


    private static Task<TResult> ProceedSynchronous<TResult>(IProxyInfo invocation)
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



    private static async Task ProceedAsynchronous(IProxyInfo invocation)
    {
        invocation.Proceed();

        // Get the task to await.
        var originalReturnValue = (Task)invocation.ReturnValue;

        await originalReturnValue.ConfigureAwait(false);
    }

    private static async Task<TResult> ProceedAsynchronous<TResult>(IProxyInfo invocation)
    {
        invocation.Proceed();

        // Get the task to await.
        var originalReturnValue = (Task<TResult>)invocation.ReturnValue;

        TResult result = await originalReturnValue.ConfigureAwait(false);
        return result;
    }
}
