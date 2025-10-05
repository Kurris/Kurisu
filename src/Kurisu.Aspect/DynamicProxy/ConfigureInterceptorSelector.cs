using System.Reflection;
using AspectCore.Configuration;

namespace AspectCore.DynamicProxy;

[NonAspect]
public sealed class ConfigureInterceptorSelector : IInterceptorSelector
{
    private readonly AspectConfiguration _aspectConfiguration;
    private readonly IServiceProvider _serviceProvider;

    public ConfigureInterceptorSelector(AspectConfiguration aspectConfiguration, IServiceProvider serviceProvider)
    {
        _aspectConfiguration = aspectConfiguration ?? throw new ArgumentNullException(nameof(aspectConfiguration));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public IEnumerable<IInterceptor> Select(MethodInfo method)
    {
        foreach (var interceptorFactory in _aspectConfiguration.Interceptors)
        {
            yield return interceptorFactory.CreateInstance(_serviceProvider);
        }
    }
}