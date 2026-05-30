using Kurisu.AspNetCore.Abstractions.DataAccess.Aop;
using Kurisu.Extensions.EventBus.Abstractions;

namespace Kurisu.Extensions.EventBus.Defaults;

/// <summary>
/// Channel 消息消费入口，从 Channel 读取消息后调用消息追踪和服务分发。
/// </summary>
public class DefaultEventBusMessageHandler(
    IEventBusLocalMessageHandler localMessageHandler,
    IEventBusMessageServiceHandler messageServiceHandler
    )
    : IEventBusMessageHandler
{
    [Datasource]
    public async Task HandleAsync<TMessage>(TMessage message, Type handlerType, CancellationToken cancellationToken)
        where TMessage : EventMessage
    {
        await using (var tracker = await localMessageHandler.BeginTrackingAsync(message.Code, message.ProcessingToken, cancellationToken))
        {
            if (tracker is null) return;

            try
            {
                await messageServiceHandler.HandlerAsync(message, handlerType, cancellationToken);
                tracker.Complete();
            }
            catch (Exception ex)
            {
                tracker.Fail(ex.Message);
            }
        }

    }
}
