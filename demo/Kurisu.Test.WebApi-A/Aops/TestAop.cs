using AspectCore.DynamicProxy;

namespace Kurisu.Test.WebApi_A.Aops;

public class TestAop : AbstractInterceptorAttribute
{
    public override async Task Invoke(AspectContext context, AspectDelegate next)
    {
        var logger = context.ServiceProvider.GetService<ILogger<TestAop>>();
        logger.LogInformation("begin");

        await next(context);

        logger.LogInformation("end");
    }
}

public class TestAop2 : AbstractInterceptorAttribute
{
    public override async Task Invoke(AspectContext context, AspectDelegate next)
    {
        var logger = context.ServiceProvider.GetService<ILogger<TestAop2>>();
        logger.LogInformation("begin");

        await next(context);

        logger.LogInformation("end");
    }
}