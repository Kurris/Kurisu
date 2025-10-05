using AspectCore.Configuration;
using AspectCore.DynamicProxy;
using AspectCore.DynamicProxy.Parameters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AspectCore.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection ConfigureDynamicProxy(this IServiceCollection services, Action<AspectConfiguration> configure = null)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        var configurationService = services.LastOrDefault(x => x.ServiceType == typeof(AspectConfiguration) && x.ImplementationInstance != null);
        var configuration = (AspectConfiguration)configurationService?.ImplementationInstance ?? new AspectConfiguration();
        configure?.Invoke(configuration);

        if (configurationService == null)
        {
            services.AddSingleton(configuration);
        }

        return services;
    }

    internal static IServiceCollection TryAddDynamicProxyServices(this IServiceCollection services)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        services.ConfigureDynamicProxy();

        services.TryAddScoped<IAspectContextFactory, AspectContextFactory>();
        services.TryAddScoped<IAspectActivatorFactory, AspectActivatorFactory>();
        services.TryAddScoped<IProxyGenerator, ProxyGenerator>();
        services.TryAddScoped<IParameterInterceptorSelector, ParameterInterceptorSelector>();

        services.TryAddSingleton<IInterceptorCollector, InterceptorCollector>();
        services.TryAddSingleton<IAspectValidatorBuilder, AspectValidatorBuilder>();
        services.TryAddSingleton<IAspectBuilderFactory, AspectBuilderFactory>();
        services.TryAddSingleton<IProxyTypeGenerator, ProxyTypeGenerator>();
        services.TryAddSingleton(typeof(AspectCaching<,>));

        services.AddSingleton<IInterceptorSelector, ConfigureInterceptorSelector>();
        services.AddSingleton<IInterceptorSelector, AttributeInterceptorSelector>();
        services.AddSingleton<IAdditionalInterceptorSelector, AttributeAdditionalInterceptorSelector>();

        return services;
    }
}