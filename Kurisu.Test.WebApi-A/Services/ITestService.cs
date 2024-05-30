using Kurisu.Aspect.DynamicProxy;
using Kurisu.Test.WebApi_A.Aops;

namespace Kurisu.Test.WebApi_A.Services;


public interface ITestService 
{

    [ServiceInterceptor(typeof(TestAop))]
    Task<string> SayAsync();

    Task DoAsync();
}