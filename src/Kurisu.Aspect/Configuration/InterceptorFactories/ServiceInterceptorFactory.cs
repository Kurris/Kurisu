using System.Reflection;
using AspectCore.DynamicProxy;

namespace AspectCore.Configuration;

/// <summary>
/// 创建服务拦截器
/// </summary>
public sealed class ServiceInterceptorFactory : InterceptorFactory
{
    private readonly Type _interceptorType;

    public ServiceInterceptorFactory(Type interceptorType)
    {
        if (interceptorType == null)
        {
            throw new ArgumentNullException(nameof(interceptorType));
        }

        if (!typeof(IInterceptor).GetTypeInfo().IsAssignableFrom(interceptorType.GetTypeInfo()))
        {
            throw new ArgumentException($"{interceptorType} is not an interceptor type.", nameof(interceptorType));
        }

        _interceptorType = interceptorType;
    }

    public override IInterceptor CreateInstance(IServiceProvider serviceProvider)
    {
        return new ServiceInterceptorAttribute(_interceptorType);
    }
}