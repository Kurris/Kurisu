using System.Linq;
using Kurisu.Proxy;
using Kurisu.Proxy.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.RemoteCall.Extensions;

public static class EnableRemoteClientExtensions
{
    public static IServiceCollection AddKurisuRemoteCall(this IServiceCollection services)
    {
        services.AddSingleton<EnableHttpClient>();
        services.AddHttpClient("kurisu.remote.httpClient");

        //
        var interfaceTypes = App.ActiveTypes.Where(x => x.IsInterface && x.IsDefined(typeof(EnableRemoteClientAttribute), false));
        foreach (var interfaceType in interfaceTypes)
        {
            services.Add(ServiceDescriptor.Describe(interfaceType, sp =>
            {
                var interceptor = sp.GetService<EnableHttpClient>().ToInterceptor();
                return ProxyGenerator.Create(null, interfaceType, interceptor);

            }, ServiceLifetime.Singleton));
        }

        return services;
    }
}
