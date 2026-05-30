namespace Kurisu.Extensions.EventBus.Abstractions;

/// <summary>
/// 消息服务分发接口，负责从 DI 容器解析业务 handler 并逐个调用。
/// </summary>
public interface IEventBusMessageServiceHandler
{
    /// <summary>
    /// 根据 handlerType 从容器获取所有注册的 handler 并调用 HandleAsync。
    /// </summary>
    Task HandlerAsync<TMessage>(TMessage message, Type handlerType, CancellationToken cancellationToken);
}
