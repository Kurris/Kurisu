using Kurisu.AspNetCore.Abstractions.DataAccess.Aop;
using Kurisu.Extensions.EventBus.Abstractions;

namespace Kurisu.Extensions.EventBus.Defaults;

/// <summary>
/// Channel 消息消费入口，从 Channel 读取消息后调用消息追踪和服务分发。
/// </summary>
public class DefaultEventMessageProcessor(
    ILocalMessageStore localMessageStore,
    IEventMessageDispatcher messageDispatcher)
    : IEventMessageProcessor
{
    [Datasource]
    public async Task ProcessAsync<TMessage>(TMessage message, CancellationToken cancellationToken)
        where TMessage : EventMessage
    {
        await using (var tracker = await localMessageStore.BeginTrackingAsync(message.Code, message.ProcessingToken, cancellationToken))
        {
            if (tracker is null) return;

            try
            {
                await messageDispatcher.DispatchAsync(message, cancellationToken);
                tracker.Complete();
            }
            catch (Exception ex)
            {
                tracker.Fail(ex.Message);
            }
        }
    }
}