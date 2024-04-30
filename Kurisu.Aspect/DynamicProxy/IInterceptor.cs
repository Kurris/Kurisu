namespace Kurisu.Aspect.DynamicProxy;

public interface IInterceptor
{
    int Order { get; set; }

    Task Invoke(AspectContext context, AspectDelegate next);
}