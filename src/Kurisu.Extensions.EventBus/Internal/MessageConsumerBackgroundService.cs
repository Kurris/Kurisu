using System.Threading.Channels;
using Kurisu.AspNetCore.Abstractions.Startup;
using Kurisu.Extensions.EventBus.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Kurisu.Extensions.EventBus.Internal;

/// <summary>
/// 消息消费者
/// </summary>
internal class MessageConsumerBackgroundService(
    ILogger<MessageConsumerBackgroundService> logger,
    IServiceProvider serviceProvider,
    ChannelReader<EventMessage> reader)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        //等待获取消息
        await foreach (var message in reader.ReadAllAsync(stoppingToken))
        {
            var messageType = message.GetType();
            var handlerType = typeof(IEventMessageHandler<>).MakeGenericType(messageType);

            try
            {
                using var scope = serviceProvider.CreateScope();
                using (scope.ServiceProvider.InitLifecycle())
                {
                    var handler = scope.ServiceProvider.GetService<IEventBusMessageHandler>();
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
