using Kurisu.AspNetCore.EventBus.Abstractions;
using Kurisu.AspNetCore.EventBus.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.AspNetCore.EventBus.Extensions;

[SkipScan]
public static class EventBusServiceCollectionExtensions
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddEventBus(this IServiceCollection services)
    {
        services.AddScoped<IEventBus, InternalEventBus>();
        return services;
    }
}