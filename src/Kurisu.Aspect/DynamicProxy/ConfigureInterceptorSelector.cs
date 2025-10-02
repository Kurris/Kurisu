using System.Reflection;
using AspectCore.Configuration;

namespace AspectCore.DynamicProxy
{
    [NonAspect]
    public sealed class ConfigureInterceptorSelector : IInterceptorSelector
    {
        private readonly IAspectConfiguration _aspectConfiguration;
        private readonly IServiceProvider _serviceProvider;

        public ConfigureInterceptorSelector(IAspectConfiguration aspectConfiguration, IServiceProvider serviceProvider)
        {
            _aspectConfiguration = aspectConfiguration ?? throw new ArgumentNullException(nameof(aspectConfiguration));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public IEnumerable<IInterceptor> Select(MethodInfo method)
        {
            //todo fix nonaspect
            foreach (var interceptorFactory in _aspectConfiguration.Interceptors)
            {
                if (!_aspectConfiguration.NonAspectPredicates.Any(x => x(method)))
                {
                    if (interceptorFactory.CanCreated(method))
                    {
                        yield return interceptorFactory.CreateInstance(_serviceProvider);
                    }
                }
            }
        }
    }
}