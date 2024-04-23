using Kurisu.AspNetCore.EventBus.Abstractions;
using Kurisu.AspNetCore.EventBus.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.AspNetCore.EventBus.Extensions;

/// <summary>
/// 进程内事件总线
/// </summary>
[SkipScan]
public static class EventBusServiceCollectionExtensions
{
    /// <summary>
    /// 进程内事件总线
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddEventBus(this IServiceCollection services)
    {
        services.AddScoped<IEventBus, InternalEventBus>();
        return services;
    }
}