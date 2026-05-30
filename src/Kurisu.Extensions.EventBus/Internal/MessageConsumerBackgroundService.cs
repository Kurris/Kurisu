using System.Threading.Channels;
using Kurisu.AspNetCore.Abstractions.Startup;
using Kurisu.Extensions.EventBus.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Kurisu.Extensions.EventBus.Internal;

/// <summary>
/// 消息消费后台服务，从 Channel 中持续读取消息并分发到对应的处理器。
/// 每条消息在独立 Scope 中消费，异常隔离不影响其他消息。
/// </summary>
internal class MessageConsumerBackgroundService(
    ILogger<MessageConsumerBackgroundService> logger,
    IServiceProvider serviceProvider,
    ChannelReader<EventMessage> reader)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var message in reader.ReadAllAsync(stoppingToken))
        {
            var messageType = message.GetType();
            var handlerType = typeof(IEventMessageHandler<>).MakeGenericType(messageType);

            try
            {
                using var scope = serviceProvider.CreateScope();
                using (scope.ServiceProvider.InitLifecycle())
                {
                    var handler = scope.ServiceProvider.GetRequiredService<IEventBusMessageHandler>();
                    await handler.HandleAsync(message, handlerType, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "EventBus处理器处理异常:{messageType},{error}", messageType.Name, ex.Message);
            }
        }
    }
}
