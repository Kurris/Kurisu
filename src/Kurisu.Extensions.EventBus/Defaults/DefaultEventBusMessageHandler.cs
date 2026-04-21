using Kurisu.AspNetCore.Abstractions.Cache.Aop;
using Kurisu.AspNetCore.Abstractions.DataAccess.Aop;
using Kurisu.Extensions.EventBus.Abstractions;

namespace Kurisu.Extensions.EventBus.Defaults;

public class DefaultEventBusMessageHandler(
    IEventBusLocalMessageHandler localMessageHandler,
    IEventBusMessageServiceHandler messageServiceHandler
    )
    : IEventBusMessageHandler
{
    [TryLock("EventBus", "正在处理中")]
    [Datasource]
    public async Task HandleAsync<TMessage>(TMessage message, Type handlerType, CancellationToken cancellationToken)
        where TMessage : EventMessage
    {
        await using (var tracker = await localMessageHandler.BeginTrackingAsync(message.Code, cancellationToken))
        {
            try
            {
                await messageServiceHandler.HandlerAsync(message, handlerType, cancellationToken);
            }
            catch (Exception ex)
            {
                //不能throw
                tracker.Fail(ex.Message);
            }

            tracker.Complete();
        }

    }
}
