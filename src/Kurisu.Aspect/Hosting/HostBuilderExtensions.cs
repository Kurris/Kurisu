using AspectCore.Configuration;
using AspectCore.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AspectCore.Extensions.Hosting;

public static class HostBuilderExtensions
{
    public static IHostBuilder UseDynamicProxy(this IHostBuilder hostBuilder, Action<IServiceCollection, AspectConfiguration> configureDelegate = null)
    {
        hostBuilder.UseServiceProviderFactory(new DynamicProxyServiceProviderFactory());

        if (configureDelegate != null)
        {
            hostBuilder.ConfigureServices(services => { services.ConfigureDynamicProxy(config => { configureDelegate(services, config); }); });
        }

        return hostBuilder;
    }
}