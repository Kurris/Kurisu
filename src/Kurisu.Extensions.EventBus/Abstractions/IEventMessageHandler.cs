using System.Threading;
using System.Threading.Tasks;


namespace Kurisu.Extensions.EventBus.Abstractions;

public abstract class EventMessage
{
}

/// <summary>
/// channel 消息处理
/// </summary>
/// <typeparam name="TMessage"></typeparam>
public interface IEventMessageHandler<in TMessage> where TMessage : EventMessage
{
    /// <summary>
    /// 执行
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    Task HandleAsync(TMessage message, CancellationToken cancellationToken);
}