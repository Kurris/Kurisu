namespace Kurisu.Extensions.EventBus.Abstractions;

/// <summary>
/// 消息服务分发接口，负责从 DI 容器解析业务 handler 并逐个调用。
/// </summary>
public interface IEventMessageDispatcher
{
    /// <summary>
    /// 根据消息的运行时类型，从容器获取所有注册的业务处理器并调用 HandleAsync。
    /// </summary>
    Task DispatchAsync<TMessage>(TMessage message, CancellationToken cancellationToken) where TMessage : EventMessage;
}
