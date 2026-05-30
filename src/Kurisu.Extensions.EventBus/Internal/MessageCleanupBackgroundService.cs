using Kurisu.AspNetCore.Abstractions.DataAccess.Core.Context;
using Kurisu.AspNetCore.Abstractions.Startup;
using Kurisu.Extensions.EventBus.Options;
using Kurisu.Extensions.SqlSugar.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kurisu.Extensions.EventBus.Internal;

/// <summary>
/// 消息清理后台服务，定期删除超过保留期的终态消息
/// </summary>
internal class MessageCleanupBackgroundService(
    ILogger<MessageCleanupBackgroundService> logger,
    IServiceProvider serviceProvider,
    IOptions<EventBusOptions> options) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // 首次启动先等待一个清理间隔，避免与其他服务初始化抢资源
        await Task.Delay(options.Value.CleanupInterval, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupAsync(stoppingToken);
                await Task.Delay(options.Value.CleanupInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "EventBus 消息清理异常: {error}", ex.Message);
                await Task.Delay(options.Value.CleanupInterval, stoppingToken);
            }
        }
    }

    /// <summary>
    /// 删除超过保留期的终态消息（Completed、DeadLetter、Ignored）。
    /// 分批删除，防止一次性删除过多行导致锁表。
    /// </summary>
    private async Task CleanupAsync(CancellationToken cancellationToken)
    {
        var retention = options.Value.CompletedMessageRetention;
        if (retention <= TimeSpan.Zero) return;

        using var scope = serviceProvider.CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var db = scope.ServiceProvider.GetRequiredService<IDbContext>();
            using (db.CreateDatasourceScope())
            {
                var cutoff = DateTime.Now.Subtract(retention);

                // 先查出待删除的主键，再做批量删除，避免 SqlSugar Deleteable 不支持 Take 的问题
                var ids = await db.Queryable<LocalMessage>()
                    .Where(x => x.CreateTime <= cutoff
                                && (x.Status == LocalMessageStatus.Completed
                                    || x.Status == LocalMessageStatus.DeadLetter
                                    || x.Status == LocalMessageStatus.Ignored))
                    .Select(x => x.Id)
                    .Take(options.Value.CleanupBatchSize)
                    .ToListAsync(cancellationToken);

                var count = ids.Count > 0
                    ? await db.AsSqlSugarDbContext().Deleteable<LocalMessage>()
                        .Where(x => ids.Contains(x.Id))
                        .ExecuteCommandAsync(cancellationToken)
                    : 0;

                if (count > 0)
                {
                    logger.LogInformation("EventBus 消息清理: 删除 {count} 条在 {cutoff} 之前的终态消息", count, cutoff);
                }
            }
        }
    }
}
