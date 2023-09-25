using System;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.DataAccess.Sharding.DependencyInjection;

public class ShardingProvider : IShardingProvider
{
    /// <summary>
    /// EfCore内部服务提供器
    /// </summary>
    private readonly IServiceProvider _internalServiceProvider;

    public ShardingProvider(IServiceProvider internalServiceProvider, IServiceProvider applicationServiceProvider)
    {
        _internalServiceProvider = internalServiceProvider;
        ApplicationServiceProvider = applicationServiceProvider;
    }


    public object GetService(Type serviceType, bool tryApplicationServiceProvider = true)
    {
        var service = _internalServiceProvider?.GetService(serviceType);
        if (tryApplicationServiceProvider && service == null)
        {
            service = ApplicationServiceProvider?.GetService(serviceType);
        }

        return service;
    }

    public TService GetService<TService>(bool tryApplicationServiceProvider = true)
    {
        return (TService) GetService(typeof(TService), tryApplicationServiceProvider);
    }

    public object GetRequiredService(Type serviceType, bool tryApplicationServiceProvider = true)
    {
        var service = GetService(serviceType, tryApplicationServiceProvider);
        if (service == null)
        {
            ArgumentNullException.ThrowIfNull(nameof(service));
        }

        return service;
    }

    public TService GetRequiredService<TService>(bool tryApplicationServiceProvider = true)
    {
        return (TService) GetRequiredService(typeof(TService), tryApplicationServiceProvider);
    }

    public IServiceProvider ApplicationServiceProvider { get; }


    public IShardingScope CreateScope()
    {
        return new ShardingScope(_internalServiceProvider.CreateScope(), ApplicationServiceProvider.CreateScope());
    }

    public AsyncShardingScope CreateAsyncScope()
    {
        return new AsyncShardingScope(_internalServiceProvider.CreateAsyncScope(), ApplicationServiceProvider.CreateAsyncScope());
    }
}