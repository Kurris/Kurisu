using Kurisu.AspNetCore.Abstractions.DistributedLock;
using Newtonsoft.Json;

namespace Kurisu.Extensions.EventBus.Abstractions;

/// <summary>
/// 事件消息基类，所有事件消息需继承此类。
/// </summary>
public abstract class EventMessage : ITryLockKey
{
    /// <summary>消息唯一标识</summary>
    public string Code { get; set; }

    /// <summary>本次处理令牌，仅运行时使用，不序列化到内容中</summary>
    [JsonIgnore]
    public string ProcessingToken { get; set; }

    public string GetKey()
    {
        return Code;
    }
}


/// <summary>
/// 事件消息处理器接口，由业务方实现具体消息的处理逻辑。
/// </summary>
public interface IEventMessageHandler<in TMessage> where TMessage : EventMessage
{
    /// <summary>
    /// 处理事件消息。
    /// </summary>
    Task HandleAsync(TMessage message, CancellationToken cancellationToken);
}
