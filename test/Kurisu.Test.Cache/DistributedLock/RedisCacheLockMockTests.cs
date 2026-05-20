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

    [Fact(DisplayName = "构造时传无效maxRenewalCount应抛出参数异常")]
    public void RedisLock_ShouldThrow_WhenMaxRenewalCountNotPositive()
    {
        var mockDb = new Mock<IDatabase>();
        var logger = new Mock<ILogger>().Object;

        Assert.Throws<ArgumentException>(() =>
            new RedisLock(logger, mockDb.Object, "test", TimeSpan.FromSeconds(3), false, 0));

        Assert.Throws<ArgumentException>(() =>
            new RedisLock(logger, mockDb.Object, "test", TimeSpan.FromSeconds(3), false, -1));
    }

    [Fact(DisplayName = "Redis连接不可用时应抛出RedisConnectionException")]
    public async Task LockAsync_ShouldThrow_WhenRedisUnavailable()
    {
        var mockDb = new Mock<IDatabase>();
        mockDb.As<IDatabaseAsync>().Setup(db => db.StringSetAsync(
                It.IsAny<RedisKey>(), It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(), It.IsAny<When>()))
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect,
                "无法连接"));

        var lockObj = CreateLock(mockDb, TimeSpan.FromSeconds(3), enableAutoRenew: false);

        await Assert.ThrowsAsync<RedisConnectionException>(
            async () => await lockObj.LockAsync());
    }

    [Fact(DisplayName = "续期配额耗尽时TryRenewAsync应通过TryReserveRenewalQuota返回false")]
    public async Task TryRenewAsync_ShouldReturnFalse_WhenQuotaExhausted()
    {
        var mockDb = new Mock<IDatabase>();
        SetupStringSetSuccess(mockDb);
        SetupScriptEvaluateSuccess(mockDb);

        // maxRenewalCount=1: 首次续期消耗配额，第二次 TryReserveRenewalQuota 返回 false
        var lockObj = CreateLock(mockDb, TimeSpan.FromMilliseconds(50),
            enableAutoRenew: false, maxRenewalCount: 1);

        var handler = await lockObj.LockAsync();
        Assert.True(handler.Acquired);

        // 第一次续期成功，配额从1减为0
        var result1 = await lockObj.TryReenterAsync();
        Assert.True(result1);

        // 第二次续期：TryReserveRenewalQuota 发现 current(1) >= max(1) → 返回 false
        var result2 = await lockObj.TryReenterAsync();
        Assert.False(result2);
        Assert.True(handler.Acquired);

        await lockObj.DisposeAsync();
    }

    [Fact(DisplayName = "DisposeAsync在ReleaseAsync返回false时应走日志警告路径")]
    public async Task DisposeAsync_ShouldLogWarning_WhenReleaseReturnsFalse()
    {
        var mockDb = new Mock<IDatabase>();
        SetupStringSetSuccess(mockDb);
        // ReleaseScript 返回 0：key 值不匹配（锁已被外部删除或过期）
        mockDb.As<IDatabaseAsync>().Setup(db => db.ScriptEvaluateAsync(
                It.IsAny<string>(), It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisResult.Create(0L));

        var lockObj = CreateLock(mockDb, TimeSpan.FromSeconds(3));
        var handler = await lockObj.LockAsync();
        Assert.True(handler.Acquired);

        await lockObj.DisposeAsync();
        Assert.False(handler.Acquired);
    }

    [Fact(DisplayName = "TryRenewAsync在Acquired为false时应走守卫快速返回")]
    public async Task TryRenewAsync_ShouldReturnFalse_WhenNotAcquiredGuard()
    {
        var mockDb = new Mock<IDatabase>();
        SetupStringSetSuccess(mockDb);
        SetupScriptEvaluateSuccess(mockDb);

        // 自动续期启用，确保 TryReenterAsync 走 TryRenewAsync 路径而非时间戳快速路径
        var lockObj = CreateLock(mockDb, TimeSpan.FromMilliseconds(600),
            enableAutoRenew: true, maxRenewalCount: null);

        var handler = await lockObj.LockAsync();
        Assert.True(handler.Acquired);

        // 释放后 Acquired 为 false
        await lockObj.DisposeAsync();
        Assert.False(handler.Acquired);

        // TryReenterAsync → TryRenewAsync → !Acquired 守卫 → 返回 false
        // （启用自动续期但时间戳为0，不会走时间戳快速路径）
        var canReenter = await lockObj.TryReenterAsync();
        Assert.False(canReenter);
    }

    [Fact(DisplayName = "后台续期循环连续失败达上限后应调用MarkLostOwnership放弃锁")]
    public async Task StartRenewalAsync_ShouldCallMarkLostOwnership_WhenConsecutiveFailuresExceedMax()
    {
        var mockDb = new Mock<IDatabase>();
        SetupStringSetSuccess(mockDb);
        // 所有脚本调用（包括 RenewScript）都抛异常
        mockDb.As<IDatabaseAsync>().Setup(db => db.ScriptEvaluateAsync(
                It.IsAny<string>(), It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(), It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisException("Simulated persistent failure"));

        // 短过期 → interval=50ms，加速；MaxConsecutiveFailureCount=10
        var lockObj = CreateLock(mockDb, TimeSpan.FromMilliseconds(60),
            enableAutoRenew: true, maxRenewalCount: null);

        var handler = await lockObj.LockAsync();
        Assert.True(handler.Acquired);

        // 续期循环每次失败，退避 100ms * 2^n（上限为 interval=50ms → backoff≈50ms），
        // 10次连续失败后调用 MarkLostOwnership
        await Task.Delay(2000);
        Assert.False(handler.Acquired);

        await lockObj.DisposeAsync();
    }

    [Fact(DisplayName = "加锁前已取消应直接抛OperationCanceledException")]
    public async Task LockAsync_ShouldThrow_WhenCancelledBeforeAcquire()
    {
        using var cts = new CancellationTokenSource();
        var mockDb = new Mock<IDatabase>();
        SetupStringSetSuccess(mockDb);

        var lockObj = CreateLock(mockDb, TimeSpan.FromSeconds(3), enableAutoRenew: false);
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await lockObj.LockAsync(cancellationToken: cts.Token));
    }

    [Fact(DisplayName = "加锁成功后若中途CancellationToken被取消应回滚锁并抛出")]
    public async Task LockAsync_ShouldRollbackAndThrow_WhenCancelledAfterAcquire()
    {
        using var cts = new CancellationTokenSource();
        var mockDb = new Mock<IDatabase>();
        // StringSetAsync 返回 true 的同时取消令牌，模拟获取成功后取消
        mockDb.As<IDatabaseAsync>().Setup(db => db.StringSetAsync(
                It.IsAny<RedisKey>(), It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(), It.IsAny<When>()))
            .Callback(() => cts.Cancel())
            .ReturnsAsync(true);
        // ReleaseScript 返回 1 确保回滚成功
        mockDb.As<IDatabaseAsync>().Setup(db => db.ScriptEvaluateAsync(
                It.Is<string>(s => s.Contains("del")),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisResult.Create(1L));

        var lockObj = CreateLock(mockDb, TimeSpan.FromSeconds(3), enableAutoRenew: false);

        // LockAsync: ThrowIfCancellationRequested开头通过 → StringSetAsync返回true且Callback取消令牌
        // → cancellationToken.IsCancellationRequested=true → ReleaseAsync回滚 → 重新throw
        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await lockObj.LockAsync(cancellationToken: cts.Token));
        Assert.False(lockObj.Acquired);
    }

    [Fact(DisplayName = "构造时传null数据库应抛出ArgumentNullException")]
    public void RedisLock_ShouldThrow_WhenDbIsNull()
    {
        var logger = new Mock<ILogger>().Object;

        Assert.Throws<ArgumentNullException>(() =>
            new RedisLock(logger, null!, "test", TimeSpan.FromSeconds(3), false, null));
    }

    [Fact(DisplayName = "构造时传null锁键应抛出ArgumentNullException")]
    public void RedisLock_ShouldThrow_WhenLockKeyIsNull()
    {
        var mockDb = new Mock<IDatabase>();
        var logger = new Mock<ILogger>().Object;

        Assert.Throws<ArgumentNullException>(() =>
            new RedisLock(logger, mockDb.Object, null!, TimeSpan.FromSeconds(3), false, null));
    }

    [Fact(DisplayName = "DisposeAsync中ReleaseAsync抛异常时应走catch日志错误路径")]
    public async Task DisposeAsync_ShouldLogError_WhenReleaseAsyncThrows()
    {
        var mockDb = new Mock<IDatabase>();
        SetupStringSetSuccess(mockDb);
        // ScriptEvaluateAsync 在 release 脚本时抛异常
        mockDb.As<IDatabaseAsync>().Setup(db => db.ScriptEvaluateAsync(
                It.IsAny<string>(), It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(), It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisException("Simulated release failure"));

        var lockObj = CreateLock(mockDb, TimeSpan.FromSeconds(3), enableAutoRenew: false);
        var handler = await lockObj.LockAsync();
        Assert.True(handler.Acquired);

        // DisposeAsync → ReleaseAsync 抛异常 → catch (Exception ex) → LogError
        await lockObj.DisposeAsync();
        Assert.False(handler.Acquired);
    }

    [Fact(DisplayName = "构造时极短过期应触发ticks<=0分支设ticks为1")]
    public void RedisLock_ShouldSetTicksToOne_WhenExpiryTooShortForIntegerDivision()
    {
        var mockDb = new Mock<IDatabase>();
        var logger = new Mock<ILogger>().Object;

        // TimeSpan.FromTicks(2): Ticks=2, ticks = 2/3 = 0 → ticks<=0 分支 → ticks=1
        // 然后 ticks=1 < minInterval(500000) → ticks=500000
        var lockObj = new RedisLock(logger, mockDb.Object, "test-micro-ticks",
            TimeSpan.FromTicks(2), false, null);

        Assert.False(lockObj.Acquired);
    }

    [Fact(DisplayName = "TryReenterAsync在自动续期启用但时间戳过期时应回退到TryRenewAsync")]
    public async Task TryReenterAsync_ShouldFallbackToTryRenew_WhenAutoRenewTimestampStale()
    {
        var mockDb = new Mock<IDatabase>();
        SetupStringSetSuccess(mockDb);
        SetupScriptEvaluateSuccess(mockDb);

        // 启用自动续期+有限配额：续期循环耗尽配额后停止，时间戳变陈旧
        var lockObj = CreateLock(mockDb, TimeSpan.FromMilliseconds(100),
            enableAutoRenew: true, maxRenewalCount: 1);

        var handler = await lockObj.LockAsync();
        Assert.True(handler.Acquired);

        // 等待续期循环消耗唯一次配额并退出，时间戳超过100ms后失效
        await Task.Delay(400);

        // TryReenterAsync: _enableAutoRenew=true, lastMs>0 但 timestamp 过期
        // → 回退到 TryRenewAsync → quota 耗尽 → 返回 false
        var canReenter = await lockObj.TryReenterAsync();
        Assert.False(canReenter);
        Assert.True(handler.Acquired);

        await lockObj.DisposeAsync();
    }
}
