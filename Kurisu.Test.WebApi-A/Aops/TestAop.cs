using Kurisu.Aspect.DynamicProxy;

namespace Kurisu.Test.WebApi_A.Aops
{
    public class TestAop : IInterceptor
    {
        /// <inheritdoc />
        public int Order { get; set; }

        /// <inheritdoc />
        public async Task Invoke(AspectContext context, AspectDelegate next)
        {
            var logger = context.ServiceProvider.GetService<ILogger<TestAop>>();
            logger.LogInformation("begin");
            await next(context);
            logger.LogInformation("end");
        }
    }
}