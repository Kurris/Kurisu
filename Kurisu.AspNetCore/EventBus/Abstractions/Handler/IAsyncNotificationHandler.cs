using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.AspNetCore.EventBus.Abstractions.Handler;

/// <summary>
/// 消息分发
/// </summary>
/// <typeparam name="TNotification"></typeparam>
public interface IAsyncNotificationHandler<in TNotification> : IScopeDependency where TNotification : INotifyMessage
{
    /// <summary>
    /// 执行
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    Task InvokeAsync(TNotification message);
}