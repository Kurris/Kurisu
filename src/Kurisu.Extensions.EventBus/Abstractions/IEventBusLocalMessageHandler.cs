
namespace Kurisu.Extensions.EventBus.Abstractions;

/// <summary>
/// 本地消息处理接口，管理消息的持久化、竞争领取、状态追踪的完整生命周期。
/// </summary>
public interface IEventBusLocalMessageHandler
{
    /// <summary>
    /// 持久化消息到本地消息表，返回生成的唯一 code。
    /// </summary>
    Task<string> PersistAsync<TMessage>(TMessage message) where TMessage : EventMessage;

    /// <summary>
    /// 通过乐观锁竞争领取一条待处理消息。领取成功时返回本次处理令牌，失败返回 null。
    /// </summary>
    Task<string> TryClaimAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// 使用领取令牌开启消息处理追踪，using 释放时根据 Complete/Fail 自动提交最终结果。
    /// 令牌不匹配时返回 null，消费者应跳过处理。
    /// </summary>
    Task<ILocalMessageTracker> BeginTrackingAsync(string code, string processingToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// 记录投递阶段的失败（反序列化失败等），直接更新消息为 Pending 或 DeadLetter。
    /// </summary>
    Task FailDeliveryAsync(string code, string processingToken, string error, CancellationToken cancellationToken = default);
}
