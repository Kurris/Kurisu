using Kurisu.Proxy.Attributes;
using Kurisu.Test.Framework.DI.Dependencies.Abstractions;
using Kurisu.Test.Framework.DI.Interceptors;

namespace Kurisu.Test.Framework.DI.Dependencies;


[Aop(typeof(BeforeAfterInterceptor), typeof(BeforeAfterAsyncInterceptor))]
public class TestScopeWithInterceptor : ITestScopeWithInterceptor
{
    public string Get()
    {
        return "ScopeWithInterceptor";
    }
}