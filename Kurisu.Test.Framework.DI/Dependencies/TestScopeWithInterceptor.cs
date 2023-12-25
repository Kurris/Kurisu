using Kurisu.Core.Proxy.Attributes;
using Kurisu.Test.Framework.DI.Dependencies.Abstractions;
using Kurisu.Test.Framework.DI.Interceptors;

namespace Kurisu.Test.Framework.DI.Dependencies;


[Aop(Interceptors = new[] { typeof(BeforeAfterInterceptor), typeof(BeforeAfterAsyncInterceptor) })]
public class TestScopeWithInterceptor : ITestScopeWithInterceptor
{
    public string Get()
    {
        return "ScopeWithInterceptor";
    }
}