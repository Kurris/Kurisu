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

    public Task NotifyAsync<TNotification>(TNotification notification) where TNotification : INotifyMessage
    {
        //todo pipeline
        return Scoped.Request.Value.InvokeAsync(sp =>
        {
            var notifications = sp.GetServices<IAsyncNotificationHandler<TNotification>>();
            var tasks = notifications.Select(x => x.InvokeAsync(notification)).ToArray();

            return Task.WhenAll(tasks);
        });
    }


    public Task NotifySequenceAsync<TNotification>(TNotification notification) where TNotification : INotifyMessage
    {
        //todo pipeline
        return Scoped.Request.Value.InvokeAsync(async sp =>
        {
            var notifications = sp.GetServices<IAsyncNotificationHandler<TNotification>>();
            foreach (IAsyncNotificationHandler<TNotification> asyncNotificationHandler in notifications)
            {
                await asyncNotificationHandler.InvokeAsync(notification);
            }
        });
    }
}