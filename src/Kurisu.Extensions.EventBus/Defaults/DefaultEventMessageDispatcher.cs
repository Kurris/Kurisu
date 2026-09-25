using System.Collections.Concurrent;
using System.Reflection;
using Kurisu.AspNetCore.Abstractions.DataAccess.Aop;
using Kurisu.Extensions.EventBus.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Extensions.EventBus.Defaults;

/// <summary>
/// 消息服务分发器，通过反射从 DI 容器获取所有注册的 IEventMessageHandler&lt;T&gt; 实现并逐个调用。
/// 所有处理器在同一个事务边界内依次执行；任一处理器失败会中断分发并触发事务回滚。
/// </summary>
public class DefaultEventMessageDispatcher(IServiceProvider serviceProvider) : IEventMessageDispatcher
{
    private static readonly ConcurrentDictionary<Type, (Type HandlerType, MethodInfo HandleMethod)> HandlerCache = new();

    [Transactional]
    public async Task DispatchAsync<TMessage>(TMessage message, CancellationToken cancellationToken)
        where TMessage : EventMessage
    {
        var (handlerType, handleMethod) = HandlerCache.GetOrAdd(message.GetType(), static messageType =>
        {
            var type = typeof(IEventMessageHandler<>).MakeGenericType(messageType);
            var method = type.GetMethod(nameof(IEventMessageHandler<EventMessage>.HandleAsync));
            return (type, method);
        });

        var handlers = serviceProvider.GetServices(handlerType);
        foreach (var handler in handlers)
        {
            var task = (Task)handleMethod.Invoke(handler, [message, cancellationToken]);
            await task;
        }
    }
}
