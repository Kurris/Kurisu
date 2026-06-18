using Kurisu.AspNetCore.Abstractions.DataAccess.Core.Context;
using Kurisu.Extensions.EventBus.Abstractions;
using Kurisu.Extensions.SqlSugar.Utils;

namespace Kurisu.Extensions.EventBus.Defaults;

/// <summary>
/// 死信处理服务，用于查询、人工忽略死信消息。
/// </summary>
public class DefaultEventBusDeadLetterService(IDbContext db) : IEventBusDeadLetterService
{
    /// <summary>
    /// 根据 code 查询一条死信消息。
    /// </summary>
    public async Task<LocalMessage> GetAsync(string code, CancellationToken cancellationToken = default)
    {
        return await db.Queryable<LocalMessage>().SingleAsync(x => x.Code == code);
    }

    /// <summary>
    /// 将死信消息标记为 Ignored 状态，必须填写原因。
    /// </summary>
    public async Task IgnoreAsync(string code, string reason, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("必须填写忽略原因。", nameof(reason));

        var effect = await db.AsSqlSugarDbContext().Updateable<LocalMessage>()
            .SetColumns(x => new LocalMessage
            {
                Status = LocalMessageStatus.Ignored,
                LockedUntil = null,
                ProcessingToken = null,
                DispositionReason = reason
            })
            .Where(x => x.Code == code && x.Status == LocalMessageStatus.DeadLetter)
            .ExecuteCommandAsync(cancellationToken);

        if (effect != 1)
        {
            throw new InvalidOperationException($"死信消息不存在或状态已变更: {code}");
        }
    }
}
