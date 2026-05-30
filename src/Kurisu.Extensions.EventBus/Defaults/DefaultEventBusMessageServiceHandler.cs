using Kurisu.AspNetCore.Abstractions.DataAccess.Aop;
using Kurisu.Extensions.EventBus.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Extensions.EventBus.Defaults;

/// <summary>
/// 消息服务分发器，通过反射从 DI 容器获取所有注册的 IEventMessageHandler&lt;T&gt; 实现并逐个调用。
/// 每次处理包裹在独立事务中，单个 handler 失败不影响其他 handler。
/// </summary>
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
