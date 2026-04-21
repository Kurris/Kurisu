namespace Kurisu.Extensions.EventBus.Abstractions;

public interface IEventBusMessageServiceHandler
{
    Task HandlerAsync<TMessage>(TMessage message, Type handlerType, CancellationToken cancellationToken);
}
