using AspectCore.DynamicProxy;

namespace AspectCore.Configuration;

public abstract class InterceptorFactory
{
    public abstract IInterceptor CreateInstance(IServiceProvider serviceProvider);
}