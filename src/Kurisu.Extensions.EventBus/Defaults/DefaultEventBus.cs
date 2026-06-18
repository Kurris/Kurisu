using Kurisu.AspNetCore.Abstractions.DataAccess;
using Kurisu.AspNetCore.Abstractions.DataAccess.Aop;
using Kurisu.AspNetCore.Abstractions.DataAccess.Core;
using Kurisu.Extensions.EventBus.Abstractions;

namespace Kurisu.Extensions.EventBus.Defaults;

/// <summary>
/// 默认事件总线实现，将消息持久化到本地消息表后由后台服务异步扫描投递。
/// 必须在事务内调用，消息的 INSERT 与业务数据在同一事务中提交，保证一致性。
/// </summary>
public class DefaultEventBus(
    IEventBusLocalMessageHandler localMessageHandler,
    IEventBusDispatchSignal dispatchSignal,
    ITransactionCallbackRegistry transactionCallbackRegistry
)
    : IEventBus
{
    /// <summary>
    /// 发布事件消息。消息首先持久化到 LocalMessage 表（状态 Pending），
    /// 由后台扫描服务投递到 Channel 后异步消费。
    /// 要求调用方必须处于已开启的事务中（Propagation.Mandatory）。
    /// </summary>
    public async Task PublishAsync<TMessage>(TMessage message) where TMessage : EventMessage
    {
        var code = await localMessageHandler.PersistAsync(message);
        if (string.IsNullOrEmpty(message.Code)) message.Code = code;
        await transactionCallbackRegistry.RegisterAfterCommitAsync(() =>
        {
            dispatchSignal.Notify();
            return Task.CompletedTask;
        });
    }
}