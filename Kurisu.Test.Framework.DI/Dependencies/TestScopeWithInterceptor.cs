using Kurisu.Test.Framework.DI.Dependencies.Abstractions;
using Kurisu.Test.Framework.DI.Interceptors;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Test.Framework.DI.Dependencies
{
    [Register(typeof(BeforeAfterInterceptor), typeof(BeforeAfterAsyncInterceptor))]
    public class TestScopeWithInterceptor : ITestScopeWithInterceptor, IScopeDependency
    {
        public string Get()
        {
            return "ScopeWithInterceptor";
        }
    }
}