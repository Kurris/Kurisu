using Kurisu.Aspect.DynamicProxy;
using Kurisu.Core.Proxy;
using Kurisu.Core.Proxy.Abstractions;

namespace Kurisu.Test.WebApi_A.Interceptors
{
    public class TestAop : Aop
    {
        protected override async Task InterceptAsync(IProxyInvocation invocation, Func<IProxyInvocation, Task> proceed)
        {
            await proceed.Invoke(invocation);
        }

        protected override async Task<TResult> InterceptAsync<TResult>(IProxyInvocation invocation, Func<IProxyInvocation, Task<TResult>> proceed)
        {
            return await proceed.Invoke(invocation);
        }
    }


    public class TestAop1 : AbstractInterceptor
    {
        public override async Task Invoke(AspectContext context, AspectDelegate next)
        {
            var logger = context.ServiceProvider.GetService<ILogger<TestAop1>>();
            logger.LogInformation("begin");
            await next.Invoke(context);
            logger.LogInformation("end");
        }
    }
}