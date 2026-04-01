using System.Threading.Channels;
using Kurisu.Extensions.EventBus.Abstractions;
using Kurisu.Extensions.EventBus.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kurisu.Extensions.EventBus;

/// <summary>
/// event bus 服务扩展
/// </summary>
public static class EventBusServiceCollectionExtensions
{
    /// <summary>
    /// 添加事件总线
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddEventBus(this IServiceCollection services)
    {
        var options = new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false, // 允许多个管道读写，提供管道吞吐量（无序操作）
            SingleWriter = false
        };

        services.TryAddSingleton(Channel.CreateBounded<EventMessage>(options));
        services.AddSingleton(sp => sp.GetRequiredService<Channel<EventMessage>>().Writer);
        services.AddSingleton(sp => sp.GetRequiredService<Channel<EventMessage>>().Reader);

        services.AddHostedService<MessageConsumer>();
        services.TryAddSingleton<IEventBus, InternalEventBus>();

        return services;
    }
}
