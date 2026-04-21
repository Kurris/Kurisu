using System.Threading.Channels;
using Kurisu.AspNetCore.Abstractions.DataAccess.Core.Context;
using Kurisu.AspNetCore.Abstractions.Startup;
using Kurisu.Extensions.EventBus.Abstractions;
using Kurisu.Extensions.SqlSugar.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Kurisu.Extensions.EventBus.Internal;

/// <summary>
/// 本地消息重试后台服务，定期扫描未处理消息并按指数退避策略重新投递到 Channel
/// </summary>
internal class LocalMessageRetryBackgroundService(
    ILogger<LocalMessageRetryBackgroundService> logger,
    IServiceProvider serviceProvider,
    ChannelWriter<EventMessage> writer) : BackgroundService
{
    /// <summary>
    /// 扫描间隔（60 秒）
    /// </summary>
    private static readonly TimeSpan ScanInterval = TimeSpan.FromSeconds(60);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(ScanInterval, stoppingToken);

            try
            {
                await ScanAndRetryAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "LocalMessageRetry 扫描异常: {error}", ex.Message);
            }
        }
    }

    private async Task ScanAndRetryAsync(CancellationToken stoppingToken)
    {
        using var scope = serviceProvider.CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var db = scope.ServiceProvider.GetRequiredService<IDbContext>();
            var serializer = scope.ServiceProvider.GetRequiredService<IEventBusSerializer>();
            using (db.CreateDatasourceScope())
            {
                var now = DateTime.Now;

                // 查询未处理且重试时间已到的消息（NextRetryTime 为 null 表示初次尚未尝试过的消息）
                var pendingMessages = await db.Queryable<LocalMessage>()
                    .Where(x => !x.Processed && (x.NextRetryTime == null || x.NextRetryTime <= now))
                    .ToListAsync(stoppingToken);

                if (pendingMessages.Count == 0) return;

                logger.LogInformation("LocalMessageRetry 发现 {count} 条待重试消息", pendingMessages.Count);

                foreach (var localMessage in pendingMessages)
                {
                    try
                    {
                        // 反序列化消息类型名称，Content 为 JSON，需解析 $type 或约定类型
                        var message = serializer.Deserialize<EventMessage>(localMessage.Content);
                        if (message is null)
                        {
                            logger.LogWarning("LocalMessageRetry 反序列化失败，code={code}", localMessage.Code);
                            continue;
                        }

                        message.Code = localMessage.Code;
                        await writer.WriteAsync(message, stoppingToken);
                        logger.LogInformation("LocalMessageRetry 重新投递消息 code={code}, retry={retry}", localMessage.Code, localMessage.Retry);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "LocalMessageRetry 投递失败 code={code}: {error}", localMessage.Code, ex.Message);
                    }
                }
            }
        }
    }
}
