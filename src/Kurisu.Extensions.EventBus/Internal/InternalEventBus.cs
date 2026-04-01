using System.Threading.Channels;
using System.Threading.Tasks;
using Kurisu.Extensions.EventBus.Abstractions;

namespace Kurisu.Extensions.EventBus.Internal;

/// <summary>
/// 内部channel事件总线
/// </summary>
internal class InternalEventBus : IEventBus
{
    private readonly ChannelWriter<EventMessage> _writer;

    public InternalEventBus(ChannelWriter<EventMessage> writer)
    {
        _writer = writer;
    }

    public async Task PublishAsync<TMessage>(TMessage message) where TMessage : EventMessage
    {
        await _writer.WriteAsync(message);
    }
}
