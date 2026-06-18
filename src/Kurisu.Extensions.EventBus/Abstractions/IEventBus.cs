using Kurisu.AspNetCore.Abstractions.DataAccess;
using Kurisu.AspNetCore.Abstractions.DataAccess.Aop;

namespace Kurisu.Extensions.EventBus.Abstractions;

/// <summary>
/// 事件总线
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// 发布事件
    /// </summary>
    /// <param name="message"></param>
    /// <typeparam name="TMessage"></typeparam>
    /// <returns></returns>
    [Transactional(Propagation = Propagation.Mandatory)]
    Task PublishAsync<TMessage>(TMessage message) where TMessage : EventMessage;
}