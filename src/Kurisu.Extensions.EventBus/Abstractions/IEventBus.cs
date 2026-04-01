using System.Threading.Tasks;

namespace Kurisu.Extensions.EventBus.Abstractions;

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
    Task PublishAsync<TMessage>(TMessage message) where TMessage : EventMessage;
}