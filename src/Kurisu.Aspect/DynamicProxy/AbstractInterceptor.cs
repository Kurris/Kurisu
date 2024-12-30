namespace Kurisu.Aspect.DynamicProxy;

[NonAspect]
public abstract class AbstractInterceptor : IInterceptor
{
    public virtual int Order { get; set; } = 0;

    public abstract Task Invoke(AspectContext context, AspectDelegate next);
}