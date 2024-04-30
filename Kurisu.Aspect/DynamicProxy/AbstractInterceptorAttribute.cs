namespace Kurisu.Aspect.DynamicProxy;

[NonAspect]
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Interface, Inherited = false)]
public abstract class AbstractInterceptorAttribute : Attribute, IInterceptor
{
    public virtual int Order { get; set; } = 0;

    public abstract Task Invoke(AspectContext context, AspectDelegate next);
}