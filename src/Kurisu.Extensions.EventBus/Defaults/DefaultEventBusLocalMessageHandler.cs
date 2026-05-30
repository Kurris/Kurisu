using Kurisu.AspNetCore.Abstractions.DataAccess.Core.Context;
using Kurisu.Extensions.EventBus.Abstractions;
using Kurisu.Extensions.EventBus.Options;
using Kurisu.Extensions.SqlSugar.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kurisu.Extensions.EventBus.Defaults;

/// <summary>
/// 本地消息处理器，管理消息的持久化、竞争领取、状态追踪的完整生命周期。
/// </summary>
public class DefaultEventBusLocalMessageHandler(
    IDbContext db,
    IEventBusSerializer serializer,
    IEventBusUniqueCodeGenerator codeGenerator,
    IOptions<EventBusOptions> options,
    ILogger<DefaultEventBusLocalMessageHandler> logger)
    : IEventBusLocalMessageHandler
{
    /// <summary>
    /// 将消息持久化到本地消息表，状态为 Pending，等待后台服务扫描投递。
    /// </summary>
    public async Task<string> PersistAsync<TMessage>(TMessage message) where TMessage : EventMessage
    {
        var code = codeGenerator.GenerateUniqueCode();
        message.Code = code;

        var localMessage = new LocalMessage
        {
            Code = code,
            Content = serializer.Serialize(message),
            NextRetryTime = null
        };

        await db.InsertAsync(localMessage);
        return code;
    }

    /// <summary>
    /// 通过乐观锁竞争领取一条待处理消息。领取成功后设置 ProcessingToken 和租约过期时间。
    /// 返回 null 表示已被其他实例领取或不满足处理条件。
    /// </summary>
    public async Task<string> TryClaimAsync(string code, CancellationToken cancellationToken = default)
    {
        var now = DateTime.Now;
        var processingToken = Guid.NewGuid().ToString();
        var effect = await db.AsSqlSugarDbContext().Updateable<LocalMessage>()
            .SetColumns(x => new LocalMessage
            {
                Status = LocalMessageStatus.Processing,
                LockedUntil = now.Add(options.Value.ProcessingLease),
                ProcessingToken = processingToken,
                Retry = x.Retry + 1
            })
            .Where(x => x.Code == code
                        && (x.Status == LocalMessageStatus.Pending
                            || (x.Status == LocalMessageStatus.Processing && (x.LockedUntil == null || x.LockedUntil <= now)))
                        && (x.NextRetryTime == null || x.NextRetryTime <= now))
            .ExecuteCommandAsync(cancellationToken);

        return effect == 1 ? processingToken : null;
    }

    /// <summary>
    /// 使用领取令牌开启消息处理追踪。using 释放时根据 Complete/Fail 状态自动提交最终结果。
    /// 令牌不匹配时返回 null，消费者应跳过该消息。
    /// </summary>
    public async Task<ILocalMessageTracker> BeginTrackingAsync(
        string code,
        string processingToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(processingToken)) return null;

        var localMessage = await db.Queryable<LocalMessage>()
            .SingleAsync(x => x.Code == code
                              && x.ProcessingToken == processingToken
                              && x.Status == LocalMessageStatus.Processing);

        return localMessage is null
            ? null
            : new LocalMessageTracker(this, localMessage, cancellationToken);
    }

    /// <summary>
    /// 记录投递阶段的失败（反序列化失败等），直接转为 Pending 或 DeadLetter。
    /// </summary>
    public async Task FailDeliveryAsync(
        string code,
        string processingToken,
        string error,
        CancellationToken cancellationToken = default)
    {
        var localMessage = await db.Queryable<LocalMessage>()
            .SingleAsync(x => x.Code == code
                              && x.ProcessingToken == processingToken
                              && x.Status == LocalMessageStatus.Processing);
        if (localMessage is null) return;

        await CompleteFailureAsync(localMessage, error, cancellationToken);
    }

    /// <summary>
    /// 标记消息处理成功，状态转为 Completed，清除租约和令牌。
    /// </summary>
    private async Task CompleteSuccessAsync(LocalMessage localMessage, CancellationToken cancellationToken)
    {
        await db.AsSqlSugarDbContext().Updateable<LocalMessage>()
            .SetColumns(x => new LocalMessage
            {
                Status = LocalMessageStatus.Completed,
                NextRetryTime = null,
                LockedUntil = null,
                ProcessingToken = null,
                Result = null
            })
            .Where(x => x.Code == localMessage.Code
                        && x.ProcessingToken == localMessage.ProcessingToken
                        && x.Status == LocalMessageStatus.Processing)
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 标记消息处理失败。若重试次数已达上限则转入 DeadLetter，否则退回 Pending 等待下次重试。
    /// 重试延迟采用指数退避策略：2^Retry 分钟，上限由 MaxRetryDelay 控制。
    /// </summary>
    private async Task CompleteFailureAsync(LocalMessage localMessage, string error, CancellationToken cancellationToken)
    {
        var now = DateTime.Now;
        var deadLetter = localMessage.Retry >= options.Value.MaxRetryCount;
        var status = deadLetter ? LocalMessageStatus.DeadLetter : LocalMessageStatus.Pending;
        DateTime? nextRetryTime = deadLetter ? null : now.Add(GetRetryDelay(localMessage.Retry));

        var effect = await db.AsSqlSugarDbContext().Updateable<LocalMessage>()
            .SetColumns(x => new LocalMessage
            {
                Status = status,
                NextRetryTime = nextRetryTime,
                LockedUntil = null,
                ProcessingToken = null,
                Result = error,
                DeadLetterTime = deadLetter ? now : null
            })
            .Where(x => x.Code == localMessage.Code
                        && x.ProcessingToken == localMessage.ProcessingToken
                        && x.Status == LocalMessageStatus.Processing)
            .ExecuteCommandAsync(cancellationToken);

        if (effect == 1 && deadLetter)
        {
            logger.LogError(
                "EventBus 消息进入死信状态: code={code}, retry={retry}, error={error}",
                localMessage.Code,
                localMessage.Retry,
                error);
        }
    }

    /// <summary>
    /// 指数退避重试延迟：2^Retry 分钟，重试次数超过 10 时按 2^10 计算，且不超过 MaxRetryDelay。
    /// </summary>
    private TimeSpan GetRetryDelay(int retry)
    {
        var delay = TimeSpan.FromMinutes(Math.Pow(2, Math.Min(retry, 10)));
        return delay <= options.Value.MaxRetryDelay ? delay : options.Value.MaxRetryDelay;
    }

    /// <summary>
    /// 消息处理追踪器，通过 using 模式约定：调 Complete() 标记成功，调 Fail() 标记失败。
    /// Dispose 时自动将状态写回数据库。
    /// </summary>
    private sealed class LocalMessageTracker(
        DefaultEventBusLocalMessageHandler handler,
        LocalMessage localMessage,
        CancellationToken cancellationToken)
        : ILocalMessageTracker
    {
        private string _error;
        private bool _completed;

        public void Complete() => _completed = true;

        public void Fail(string error) => _error = error;

        public async ValueTask DisposeAsync()
        {
            if (!string.IsNullOrEmpty(_error))
            {
                await handler.CompleteFailureAsync(localMessage, _error, cancellationToken);
            }
            else if (_completed)
            {
                await handler.CompleteSuccessAsync(localMessage, cancellationToken);
            }
        }
    }
}
