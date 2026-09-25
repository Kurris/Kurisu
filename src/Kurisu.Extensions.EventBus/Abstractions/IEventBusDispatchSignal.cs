namespace Kurisu.Extensions.EventBus.Abstractions;

/// <summary>
/// EventBus 本地消息投递唤醒信号。
/// </summary>
public interface IEventBusDispatchSignal
{
    /// <summary>
    /// 通知后台服务尽快扫描待投递本地消息。
    /// </summary>
    void Notify();

    /// <summary>
    /// 等待投递通知，收到通知返回 true，超时返回 false；取消时抛出 OperationCanceledException。
    /// </summary>
    Task<bool> WaitAsync(TimeSpan timeout, CancellationToken cancellationToken);
}
