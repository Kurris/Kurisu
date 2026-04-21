using Kurisu.AspNetCore.Abstractions.DataAccess.Aop;
using Kurisu.Extensions.EventBus.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Extensions.EventBus.Defaults;

public class DefaultEventBusMessageServiceHandler(IServiceProvider serviceProvider) : IEventBusMessageServiceHandler
{
    [Transactional]
    public async Task HandlerAsync<TMessage>(TMessage message, Type handlerType, CancellationToken cancellationToken)
    {
        var handleMethod = handlerType.GetMethod(nameof(IEventMessageHandler<EventMessage>.HandleAsync));
        var handlers = serviceProvider.GetServices(handlerType);
        foreach (var handler in handlers)
        {
            var task = (Task)handleMethod.Invoke(handler, [message, cancellationToken]);
            await task;
        }
    }
}
