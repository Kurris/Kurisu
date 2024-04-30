using Kurisu.Aspect.DynamicProxy;
using Kurisu.Core.Proxy.Attributes;
using Kurisu.Test.WebApi_A.Interceptors;

namespace Kurisu.Test.WebApi_A.Services;

public interface ITestService : IScopeDependency
{
    [Aop(typeof(TestAop))]
    [ServiceInterceptor(typeof(TestAop1))]
    Task<string> SayAsync();

    Task DoAsync();
}