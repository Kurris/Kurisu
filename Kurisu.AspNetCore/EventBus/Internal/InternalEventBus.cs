using System.Linq;
using System.Threading.Tasks;
using Kurisu.AspNetCore.EventBus.Abstractions;
using Kurisu.AspNetCore.EventBus.Abstractions.Handler;
using Kurisu.AspNetCore.Scope;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.AspNetCore.EventBus.Internal;

/// <summary>
/// 事件总线
/// </summary>
[SkipScan]
internal class InternalEventBus : IEventBus
{
    public Task PublishAsync<TMessage>(TMessage message) where TMessage : IAsyncChannelMessage
    {
        return AsyncChannelContext<TMessage>.PublishAsync(message);
    }

    public Task NotificationAsync<TNotification>(TNotification notification) where TNotification : INotificationMessage
    {
        //todo pipeline
        return Scoped.Request.Value.CreateAsync(sp =>
        {
            var notifications = sp.GetServices<IAsyncNotificationHandler<TNotification>>();
            var tasks = notifications.Select(x => x.InvokeAsync(notification)).ToArray();

            return Task.WhenAll(tasks);
        });
    }


    public Task NotificationSequenceAsync<TNotification>(TNotification notification) where TNotification : INotificationMessage
    {
        //todo pipeline
        return Scoped.Request.Value.CreateAsync(async sp =>
        {
            var notifications = sp.GetServices<IAsyncNotificationHandler<TNotification>>();
            foreach (IAsyncNotificationHandler<TNotification> asyncNotificationHandler in notifications)
            {
                await asyncNotificationHandler.InvokeAsync(notification);
            }
        });
    }
}