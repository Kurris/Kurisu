using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.EFSharding.Core.DependencyInjection;

internal sealed class ShardingProvider : IShardingProvider
{
    private readonly IServiceProvider _internalServiceProvider;

    public ShardingProvider(IServiceProvider internalServiceProvider, IServiceProvider applicationServiceProvider)
    {
        _internalServiceProvider = internalServiceProvider;
        ApplicationServiceProvider = applicationServiceProvider;
    }

    public object GetService(Type serviceType)
    {
        return (_internalServiceProvider.GetService(serviceType)
                ?? ApplicationServiceProvider.GetService(serviceType))!;
    }

    public TService GetService<TService>(bool tryApplicationServiceProvider = true)
    {
        return (TService) GetService(typeof(TService));
    }

    public object GetRequiredService(Type serviceType, bool tryApplicationServiceProvider = true)
    {
        var service = GetService(serviceType);
        if (service == null)
        {
            throw new ArgumentNullException($"cant unable resolve service:[{serviceType}]");
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

    public object CreateInstance(Type serviceType)
    {
        var constructors = serviceType.GetConstructors()
            .Where(c => !c.IsStatic && c.IsPublic)
            .ToArray();

        //依赖注入只允许一个构造函数
        if (constructors.Length != 1)
        {
            throw new ArgumentException($"type :[{serviceType}] found more than one  declared constructor ");
        }

        var @params = constructors[0].GetParameters().Select(x => GetService(x.ParameterType))
            .ToArray();

        return Activator.CreateInstance(serviceType, @params);
    }

    public TService CreateInstance<TService>() where TService : class, new()
    {
        return (TService) CreateInstance(typeof(TService));
    }
}