using Kurisu.AspNetCore.Abstractions.DependencyInjection;
using Kurisu.AspNetCore.EventBus.Abstractions;
using Kurisu.AspNetCore.EventBus.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.AspNetCore.EventBus.Extensions;

/// <summary>
/// di
/// </summary>
[SkipScan]
public static class EventBusServiceCollectionExtensions
{
    /// <summary>
    /// 添加事件总线
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddEventBus(this IServiceCollection services)
    {
        services.AddScoped<IEventBus, InternalEventBus>();
        return services;
    }
}