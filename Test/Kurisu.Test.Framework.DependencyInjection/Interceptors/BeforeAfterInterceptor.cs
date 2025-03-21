// using System;
// using Kurisu.Core.Proxy.Abstractions;
//
// namespace Kurisu.Test.Framework.DependencyInjection.Interceptors;
//
// public class BeforeAfterInterceptor : IInterceptor
// {
//     public void Intercept(IProxyInvocation invocation)
//     {
//         Console.WriteLine("before");
//         invocation.Proceed();
//         Console.WriteLine("after");
//     }
// }