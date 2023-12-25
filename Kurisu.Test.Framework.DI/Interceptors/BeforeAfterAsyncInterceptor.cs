using System;
using System.Threading.Tasks;
using Kurisu.Core.Proxy;
using Kurisu.Core.Proxy.Abstractions;

namespace Kurisu.Test.Framework.DI.Interceptors;

public class BeforeAfterAsyncInterceptor : Aop
{
    protected override async Task InterceptAsync(IProxyInvocation invocation, Func<IProxyInvocation, Task> proceed)
    {
        Console.WriteLine("before");
        await proceed(invocation).ConfigureAwait(false);
        Console.WriteLine("after");
    }

    protected override async Task<TResult> InterceptAsync<TResult>(IProxyInvocation invocation, Func<IProxyInvocation, Task<TResult>> proceed)
    {
        Console.WriteLine("before");
        var result = await proceed(invocation).ConfigureAwait(false);
        Console.WriteLine("after");
        return result;
    }
}