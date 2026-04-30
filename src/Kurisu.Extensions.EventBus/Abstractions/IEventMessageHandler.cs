using Kurisu.AspNetCore.Abstractions.Cache;

namespace Kurisu.Extensions.EventBus.Abstractions;

public abstract class EventMessage : ITryLockKey
{
    public string Code { get; set; }

    public string GetKey()
    {
        return Code;
    }
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