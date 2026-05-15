using Kurisu.Extensions.Cache.Locking;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace Kurisu.Test.Cache;

[Trait("feature", "lock-mock")]
public class RedisCacheLockMockTests
{
    private static RedisLock CreateLock(
        Mock<IDatabase> mockDb,
        TimeSpan? expiry = null,
        bool enableAutoRenew = false,
        int? maxRenewalCount = null)
    {
        var logger = new Mock<ILogger>().Object;
        return new RedisLock(logger, mockDb.Object, $"test:mock:{Guid.NewGuid():N}",
            expiry ?? TimeSpan.FromSeconds(5), enableAutoRenew, maxRenewalCount);
    }

    private static void SetupStringSetSuccess(Mock<IDatabase> mockDb)
    {
        mockDb.As<IDatabaseAsync>().Setup(db => db.StringSetAsync(
                It.IsAny<RedisKey>(), It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(), It.IsAny<When>()))
            .ReturnsAsync(true);
    }

    private static void SetupScriptEvaluateThrowsOnRenew(Mock<IDatabase> mockDb)
    {
        mockDb.As<IDatabaseAsync>().Setup(db => db.ScriptEvaluateAsync(
                It.IsAny<string>(), It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(), It.IsAny<CommandFlags>()))
            .Returns((string script, RedisKey[] _, RedisValue[] _, CommandFlags _) =>
            {
                if (script.Contains("pexpire"))
                    return Task.FromException<RedisResult>(
                        new RedisConnectionException(ConnectionFailureType.UnableToConnect,
                            "Simulated failure"));
                return Task.FromResult(RedisResult.Create(0L));
            });
    }

    private static void SetupScriptEvaluateSuccess(Mock<IDatabase> mockDb)
    {
        mockDb.As<IDatabaseAsync>().Setup(db => db.ScriptEvaluateAsync(
                It.IsAny<string>(), It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisResult.Create(1L));
    }

    [Fact(DisplayName = "TryRenewAsync中Redis异常时应在catch块回滚配额后重新抛出")]
    public async Task TryRenewAsync_ShouldRollbackQuotaAndRethrow_WhenRedisThrows()
    {
        var mockDb = new Mock<IDatabase>();
        SetupStringSetSuccess(mockDb);
        SetupScriptEvaluateThrowsOnRenew(mockDb);

        var lockObj = CreateLock(mockDb, TimeSpan.FromSeconds(3), enableAutoRenew: false);
        var handler = await lockObj.LockAsync();
        Assert.True(handler.Acquired);

        // TryReenterAsync → TryRenewAsync → ScriptEvaluateAsync 抛异常 → catch+didsReserve=false → rethrow
        await Assert.ThrowsAsync<RedisConnectionException>(
            async () => await lockObj.TryReenterAsync());

        Assert.True(handler.Acquired);
        await lockObj.DisposeAsync();
    }

    [Fact(DisplayName = "有限续期模式异常时应覆盖didReserve=true的配额回滚路径")]
    public async Task TryRenewAsync_ShouldRollbackDidReserveQuota_WhenLimitedRenewalAndRedisThrows()
    {
        var mockDb = new Mock<IDatabase>();
        SetupStringSetSuccess(mockDb);
        SetupScriptEvaluateThrowsOnRenew(mockDb);

        // maxRenewalCount=3 → TryReserveRenewalQuota 真正预留配额（didReserve=true）
        var lockObj = CreateLock(mockDb, TimeSpan.FromSeconds(3),
            enableAutoRenew: false, maxRenewalCount: 3);

        var handler = await lockObj.LockAsync();
        Assert.True(handler.Acquired);

        await Assert.ThrowsAsync<RedisConnectionException>(
            async () => await lockObj.TryReenterAsync());

        Assert.True(handler.Acquired);
        await lockObj.DisposeAsync();
    }

    [Fact(DisplayName = "后台续期循环应捕获Redis异常并应用退避后继续")]
    public async Task StartRenewalAsync_ShouldCatchExceptionAndContinue_WhenRedisFails()
    {
        var mockDb = new Mock<IDatabase>();
        SetupStringSetSuccess(mockDb);
        SetupScriptEvaluateThrowsOnRenew(mockDb);

        // 短过期→interval=50ms floor，加速。无限续期避免配额耗尽干扰
        var lockObj = CreateLock(mockDb, TimeSpan.FromMilliseconds(60),
            enableAutoRenew: true, maxRenewalCount: null);

        var handler = await lockObj.LockAsync();
        Assert.True(handler.Acquired);

        // interval(50ms) + 异常 + 退避(100ms) → 循环至少完成一次
        await Task.Delay(500);
        Assert.True(handler.Acquired);

        await lockObj.DisposeAsync();
    }

    [Fact(DisplayName = "DisposeAsync取消CTS后续期循环应通过TaskCanceledException退出")]
    public async Task StartRenewalAsync_ShouldExit_WhenCtsCancelledByDispose()
    {
        var mockDb = new Mock<IDatabase>();
        SetupStringSetSuccess(mockDb);
        SetupScriptEvaluateSuccess(mockDb);

        var lockObj = CreateLock(mockDb, TimeSpan.FromMilliseconds(60),
            enableAutoRenew: true, maxRenewalCount: null);

        var handler = await lockObj.LockAsync();
        Assert.True(handler.Acquired);

        // 让续期循环进入 Task.Delay
        await Task.Delay(100);
        await lockObj.DisposeAsync();
        Assert.False(handler.Acquired);
    }

    [Fact(DisplayName = "TryReenterAsync在Acquired为false时应直接返回false")]
    public async Task TryReenterAsync_ShouldReturnFalse_WhenNotAcquired()
    {
        var mockDb = new Mock<IDatabase>();
        SetupStringSetSuccess(mockDb);
        SetupScriptEvaluateSuccess(mockDb);

        var lockObj = CreateLock(mockDb, TimeSpan.FromSeconds(3), enableAutoRenew: false);
        var handler = await lockObj.LockAsync();
        Assert.True(handler.Acquired);

        await lockObj.DisposeAsync();
        Assert.False(handler.Acquired);

        // TryReenterAsync → !Acquired 守卫 → 返回 false
        var canReenter = await lockObj.TryReenterAsync();
        Assert.False(canReenter);
    }

    [Fact(DisplayName = "DisposeAsync二次释放应走else分支清理残留CTS")]
    public async Task DisposeAsync_ShouldCleanupLeftoverCts_WhenCalledSecondTime()
    {
        var mockDb = new Mock<IDatabase>();
        SetupStringSetSuccess(mockDb);
        SetupScriptEvaluateSuccess(mockDb);

        var lockObj = CreateLock(mockDb, TimeSpan.FromMilliseconds(60),
            enableAutoRenew: true, maxRenewalCount: null);

        var handler = await lockObj.LockAsync();
        Assert.True(handler.Acquired);

        await lockObj.DisposeAsync();
        Assert.False(handler.Acquired);

        // 二次释放走 else 分支
        await lockObj.DisposeAsync();
        Assert.False(handler.Acquired);
    }
}
