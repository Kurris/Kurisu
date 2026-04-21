using System.Threading.Channels;
using Kurisu.AspNetCore.Abstractions.DataAccess;
using Kurisu.AspNetCore.Abstractions.DataAccess.Aop;
using Kurisu.Extensions.EventBus.Abstractions;

namespace Kurisu.Extensions.EventBus.Defaults;

/// <summary>
/// channel事件总线
/// </summary>
public class DefaultEventBus(
    ChannelWriter<EventMessage> writer,
    IEventBusLocalMessageHandler localMessageHandler
   )
    : IEventBus
{
    [Transactional(Propagation = Propagation.Mandatory)]
    public async Task PublishAsync<TMessage>(TMessage message) where TMessage : EventMessage
    {
        var code = await localMessageHandler.PersistAsync(message);
        if (string.IsNullOrEmpty(message.Code)) message.Code = code;
        await writer.WriteAsync(message);
    }
}
