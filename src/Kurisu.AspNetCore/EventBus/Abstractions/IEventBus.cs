using System.Threading.Tasks;

namespace Kurisu.AspNetCore.EventBus.Abstractions;

/// <summary>
/// 事件总线
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// 推送
    /// </summary>
    /// <param name="message"></param>
    /// <typeparam name="TMessage"></typeparam>
    /// <returns></returns>
    Task PublishAsync<TMessage>(TMessage message) where TMessage : IAsyncChannelMessage;

    /// <summary>
    /// 通知
    /// </summary>
    /// <param name="notification"></param>
    /// <typeparam name="TNotification"></typeparam>
    /// <returns></returns>
    Task NotifyAsync<TNotification>(TNotification notification) where TNotification : INotifyMessage;
    
    /// <summary>
    /// 通知
    /// </summary>
    /// <param name="notification"></param>
    /// <typeparam name="TNotification"></typeparam>
    /// <returns></returns>
    Task NotifySequenceAsync<TNotification>(TNotification notification) where TNotification : INotifyMessage;
}