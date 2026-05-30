

namespace Kurisu.Extensions.EventBus.Abstractions;

/// <summary>
/// 事件消息序列化器接口。
/// </summary>
public interface IEventBusSerializer
{
    /// <summary>序列化消息为字符串，用于持久化到本地消息表。</summary>
    public string Serialize<TMessage>(TMessage message);

    /// <summary>反序列化字符串为消息对象。</summary>
    public TMessage Deserialize<TMessage>(string message);
}
