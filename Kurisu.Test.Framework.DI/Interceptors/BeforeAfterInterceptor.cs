using System;
using Kurisu.Proxy.Abstractions;

namespace Kurisu.Test.Framework.DI.Interceptors;

public class BeforeAfterInterceptor : IInterceptor
{
    public void Intercept(IProxyInvocation invocation)
    {
        Console.WriteLine("before");
        invocation.Proceed();
        Console.WriteLine("after");
    }
}