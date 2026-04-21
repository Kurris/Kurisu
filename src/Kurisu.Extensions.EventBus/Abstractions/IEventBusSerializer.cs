

namespace Kurisu.Extensions.EventBus.Abstractions;

public interface IEventBusSerializer
{
    public string Serialize<TMessage>(TMessage message);

    public TMessage Deserialize<TMessage>(string message);
}
