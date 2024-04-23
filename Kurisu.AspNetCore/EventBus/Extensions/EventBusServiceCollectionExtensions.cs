using Kurisu.AspNetCore.EventBus.Abstractions;
using Kurisu.AspNetCore.EventBus.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.AspNetCore.EventBus.Extensions;

/// <summary>
/// �������¼�����
/// </summary>
[SkipScan]
public static class EventBusServiceCollectionExtensions
{
    /// <summary>
    /// �������¼�����
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddEventBus(this IServiceCollection services)
    {
        services.AddScoped<IEventBus, InternalEventBus>();
        return services;
    }
}