using System;
using Castle.DynamicProxy;

namespace Kurisu.Test.Framework.DI.Interceptors;

public class BeforeAfterInterceptor : IInterceptor
{
    public void Intercept(IInvocation invocation)
    {
        Console.WriteLine("before");
        invocation.Proceed();
        Console.WriteLine("after");
        //invocation.ReturnValue
    }
}