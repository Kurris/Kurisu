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
}
