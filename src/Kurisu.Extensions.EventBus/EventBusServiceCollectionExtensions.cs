using System.Threading.Channels;
using Kurisu.Extensions.EventBus.Abstractions;
using Kurisu.Extensions.EventBus.Defaults;
using Kurisu.Extensions.EventBus.Internal;
using Kurisu.Extensions.EventBus.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kurisu.Extensions.EventBus;

/// <summary>
/// EventBus 服务注册扩展
/// </summary>
public static class EventBusServiceCollectionExtensions
{
    /// <summary>
    /// 注册 EventBus 所有服务：Channel、消费后台服务、重试扫描服务、清理服务、序列化器、handler 等。
    /// </summary>
    public static IServiceCollection AddEventBus(this IServiceCollection services, Action<EventBusOptions> configure = null)
    {
        var options = new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false, // 允许多个管道读写，提高管道吞吐量（无序操作）
            SingleWriter = false
        };

        // Channel 作为单例，后台服务通过 Reader/Writer 解耦
        services.TryAddSingleton(Channel.CreateBounded<EventMessage>(options));
        services.AddSingleton(sp => sp.GetRequiredService<Channel<EventMessage>>().Writer);
        services.AddSingleton(sp => sp.GetRequiredService<Channel<EventMessage>>().Reader);

        // 后台服务
        services.AddHostedService<MessageConsumerBackgroundService>();
        services.AddHostedService<LocalMessageRetryBackgroundService>();
        services.AddHostedService<MessageCleanupBackgroundService>();

        services.AddOptions<EventBusOptions>();
        if (configure is not null) services.Configure(configure);

        // 默认实现
        services.TryAddSingleton<IEventBusSerializer, DefaultEventBusSerializer>();
        services.TryAddScoped<IEventBusLocalMessageHandler, DefaultEventBusLocalMessageHandler>();
        services.TryAddScoped<IEventBusMessageHandler, DefaultEventBusMessageHandler>();
        services.TryAddScoped<IEventBusMessageServiceHandler, DefaultEventBusMessageServiceHandler>();
        services.TryAddScoped<IEventBusDeadLetterService, DefaultEventBusDeadLetterService>();
        services.TryAddScoped<IEventBusUniqueCodeGenerator, DefaultEventBusUniqueCodeGenerator>();
        services.TryAddScoped<IEventBus, DefaultEventBus>();

        return services;
    }
}
