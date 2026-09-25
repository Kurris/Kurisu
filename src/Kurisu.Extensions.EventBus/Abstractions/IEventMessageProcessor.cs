

namespace Kurisu.Extensions.EventBus.Abstractions;

/// <summary>
/// Channel 消息消费入口，负责消息追踪（BeginTracking）和服务分发。
/// </summary>
public interface IEventMessageProcessor
{
    /// <summary>
    /// 处理从 Channel 消费的消息。通过 Tracking 模式管理状态，成功/失败自动写回数据库。
    /// </summary>
    public Task ProcessAsync<TMessage>(TMessage message, CancellationToken cancellationToken) where TMessage : EventMessage;
}
