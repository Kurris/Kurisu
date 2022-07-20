using Microsoft.Extensions.DependencyInjection;
using WebApiDemo.Interceptors;

namespace WebApiDemo.Services.Aop
{
    [Register(typeof(BeforeAfterInterceptor))]
    public class AopService : IAopService
    {
        public virtual string Hello()
        {
            return "Hello";
        }
    }
}