using System.Threading.Tasks;

namespace Kurisu.AspNetCore.EventBus.Abstractions;

/// <summary>
/// 事件总线
/// </summary>
public interface IEventBus
{
    Task PublishAsync<TMessage>(TMessage message) where TMessage : IAsyncChannelMessage;

    Task NotificationAsync<TNotification>(TNotification notification) where TNotification : INotificationMessage;
    
    Task NotificationSequenceAsync<TNotification>(TNotification notification) where TNotification : INotificationMessage;
}