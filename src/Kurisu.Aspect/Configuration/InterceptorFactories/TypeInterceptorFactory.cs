using System.Reflection;
using AspectCore.DynamicProxy;

namespace AspectCore.Configuration;

/// <summary>
/// 创建类型拦截器
/// </summary>
public sealed class TypeInterceptorFactory : InterceptorFactory
{
    private static readonly object[] emptyArgs = Array.Empty<object>();
    private readonly object[] _args;
    private readonly Type _interceptorType;

    public TypeInterceptorFactory(Type interceptorType, object[] args)
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
        _args = args ?? emptyArgs;
    }

    public override IInterceptor CreateInstance(IServiceProvider serviceProvider)
    {
        return (IInterceptor)Activator.CreateInstance(_interceptorType, _args);
    }
}