

namespace Kurisu.Extensions.EventBus.Abstractions;

public interface IEventBusLocalMessageHandler
{
    Task<string> PersistAsync<TMessage>(TMessage message) where TMessage : EventMessage;

    /// <summary>
    /// 开启本地消息追踪，using 模式自动提交处理状态
    /// </summary>
    Task<ILocalMessageTracker> BeginTrackingAsync(string code, CancellationToken cancellationToken = default);
}
