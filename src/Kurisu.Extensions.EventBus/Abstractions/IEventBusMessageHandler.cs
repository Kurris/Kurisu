

namespace Kurisu.Extensions.EventBus.Abstractions;

public interface IEventBusMessageHandler
{
    /// <summary>
    /// 处理消息
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public Task HandleAsync<TMessage>(TMessage message, Type handlerType, CancellationToken cancellationToken) where TMessage : EventMessage;
}
