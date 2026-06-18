using System.Threading.Channels;
using Kurisu.AspNetCore.Abstractions.DataAccess.Core.Context;
using Kurisu.AspNetCore.Abstractions.Startup;
using Kurisu.Extensions.EventBus.Abstractions;
using Kurisu.Extensions.EventBus.Options;
using Kurisu.Extensions.SqlSugar.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kurisu.Extensions.EventBus.Internal;

/// <summary>
/// 本地消息重试后台服务，定期扫描待处理消息（Pending 及租约过期的 Processing），
/// 竞争领取后反序列化并投递到 Channel。
/// </summary>
internal class LocalMessageRetryBackgroundService(
    ILogger<LocalMessageRetryBackgroundService> logger,
    IServiceProvider serviceProvider,
    ChannelWriter<EventMessage> writer,
    LocalMessageDispatchSignal dispatchSignal,
    IOptions<EventBusOptions> options) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await dispatchSignal.WaitAsync(options.Value.ScanInterval, stoppingToken);
                await ScanAndRetryAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "LocalMessageRetry 扫描异常: {error}", ex.Message);
                await Task.Delay(options.Value.ScanInterval, stoppingToken);
            }
        }
    }

    /// <summary>
    /// 扫描待处理消息：Pending 状态、或 Processing 状态但租约已过期的消息。
    /// 每条消息先 TryClaimAsync 竞争领取，领取成功后再反序列化并写入 Channel。
    /// 反序列化失败或投递异常时调用 FailDeliveryAsync 记录失败状态。
    /// </summary>
    private async Task ScanAndRetryAsync(CancellationToken stoppingToken)
    {
        using var scope = serviceProvider.CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var db = scope.ServiceProvider.GetRequiredService<IDbContext>();
            var serializer = scope.ServiceProvider.GetRequiredService<IEventBusSerializer>();
            var localMessageHandler = scope.ServiceProvider.GetRequiredService<IEventBusLocalMessageHandler>();
            using (db.CreateDatasourceScope())
            {
                var now = DateTime.Now;

                var pendingMessages = await db.Queryable<LocalMessage>()
                    .Where(x => (x.Status == LocalMessageStatus.Pending
                                || (x.Status == LocalMessageStatus.Processing && (x.LockedUntil == null || x.LockedUntil <= now)))
                                && (x.NextRetryTime == null || x.NextRetryTime <= now))
                    .Take(options.Value.ScanBatchSize)
                    .ToListAsync(stoppingToken);

                if (pendingMessages.Count == 0) return;

                logger.LogInformation("LocalMessageRetry 发现 {count} 条待重试消息", pendingMessages.Count);

                foreach (var localMessage in pendingMessages)
                {
                    string processingToken = null;
                    try
                    {
                        // 竞争领取，失败说明已被其他实例领取
                        processingToken = await localMessageHandler.TryClaimAsync(localMessage.Code, stoppingToken);
                        if (string.IsNullOrEmpty(processingToken)) continue;

                        var message = serializer.Deserialize<EventMessage>(localMessage.Content);
                        if (message is null)
                        {
                            await localMessageHandler.FailDeliveryAsync(localMessage.Code, processingToken, "反序列化结果为空", stoppingToken);
                            logger.LogWarning("LocalMessageRetry 反序列化结果为空，code={code}", localMessage.Code);
                            continue;
                        }

                        message.Code = localMessage.Code;
                        message.ProcessingToken = processingToken;
                        await writer.WriteAsync(message, stoppingToken);
                        logger.LogInformation(
                            "LocalMessageRetry 重新投递消息 code={code}, attempts={attempts}",
                            localMessage.Code,
                            localMessage.Attempts);
                    }
                    catch (Exception ex)
                    {
                        if (!string.IsNullOrEmpty(processingToken))
                        {
                            await localMessageHandler.FailDeliveryAsync(localMessage.Code, processingToken, ex.Message, stoppingToken);
                        }
                        logger.LogError(ex, "LocalMessageRetry 投递失败 code={code}: {error}", localMessage.Code, ex.Message);
                    }
                }
            }
        }
    }
}
