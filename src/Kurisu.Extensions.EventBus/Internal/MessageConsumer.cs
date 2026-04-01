using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Kurisu.AspNetCore.Abstractions.Startup;
using Kurisu.Extensions.EventBus.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Kurisu.Extensions.EventBus.Internal;

/// <summary>
/// 消息消费者
/// </summary>
internal class MessageConsumer : BackgroundService
{
    private readonly ILogger<MessageConsumer> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly ChannelReader<EventMessage> _reader;

    public MessageConsumer(ILogger<MessageConsumer> logger, IServiceProvider serviceProvider, ChannelReader<EventMessage> reader)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _reader = reader;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var message in _reader.ReadAllAsync(stoppingToken))
        {
            var messageType = message.GetType();
            var handlerType = typeof(IEventMessageHandler<>).MakeGenericType(messageType);

            try
            {
                using var scope = _serviceProvider.CreateScope();
                using (scope.ServiceProvider.InitLifecycle())
                {
                    // 从 DI 容器解析处理器
                    var handler = scope.ServiceProvider.GetService(handlerType);
                    if (handler is null)
                    {
                        _logger.LogWarning("找不到匹配的处理器: {messageType}", messageType.Name);
                        continue;
                    }

                    // 调用 HandleAsync 方法
                    var handleMethod = handlerType.GetMethod(nameof(IEventMessageHandler<EventMessage>.HandleAsync));
                    if (handleMethod != null)
                    {
                        var task = (Task)handleMethod.Invoke(handler, new object[] { message, stoppingToken })!;
                        await task;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理器处理异常:{messageType},{error}", messageType.Name, ex.Message);
            }
        }
    }
}
