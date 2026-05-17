using Kurisu.AspNetCore.Abstractions.Cache;
using Kurisu.Extensions.Cache;
using Kurisu.Extensions.Cache.Options;
using Kurisu.Extensions.Cache.Providers;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Xunit;

namespace Kurisu.Test.Cache;

[Trait("feature", "lock-regression")]
public class RedisCacheLockRegressionTests
{
    [Fact(DisplayName = "固定过期模式下同异步流重入应通过原子续期延长锁")]
    public async Task LockAsync_ShouldRenewLeaseOnReentry_WhenModeIsFixedExpiry()
    {
        using var serviceProvider = RedisCacheTestSupport.BuildServiceProvider();
        var cache = serviceProvider.GetRequiredService<RedisCache>();
        var lockKey = $"test:lock:fixed-expiry-reentry-renew:{Guid.NewGuid():N}";

        await cache.KeyDeleteAsync(lockKey);

        var options = RedisCacheTestSupport.BuildLockOptions(TimeSpan.FromMilliseconds(500), enableRetry: false);
        var firstHandler = await cache.LockAsync(lockKey, options);
        Assert.True(firstHandler.Acquired);

        await Task.Delay(350);

        var secondHandler = await cache.LockAsync(lockKey, options);
        Assert.True(secondHandler.Acquired);
        Assert.True(await cache.KeyExistsAsync(lockKey));

        await Task.Delay(250);
        Assert.True(await cache.KeyExistsAsync(lockKey));

        await secondHandler.DisposeAsync();
        await firstHandler.DisposeAsync();
        Assert.False(await cache.KeyExistsAsync(lockKey));
    }

    [Fact(DisplayName = "有限续期模式下重入不消耗配额，由后台续期循环独立管理")]
    public async Task LockAsync_ShouldNotConsumeQuotaOnReentry_WhenModeIsLimitedRenewal()
    {
        using var serviceProvider = RedisCacheTestSupport.BuildServiceProvider();
        var cache = serviceProvider.GetRequiredService<RedisCache>();
        var lockKey = $"test:lock:limited-renew-reentry-no-consume:{Guid.NewGuid():N}";

        await cache.KeyDeleteAsync(lockKey);

        var options = new DistributedLockAcquisitionOptions
        {
            TimeModeHandler = LockTimeModeHandler.LimitedRenewal(TimeSpan.FromMilliseconds(800), maxRenewalCount: 1),
            RetryStrategy = new DefaultLockRetryStrategy(0)
        };

        var firstHandler = await cache.LockAsync(lockKey, options);
        Assert.True(firstHandler.Acquired);

        // 重入不消耗配额，走时间戳快速路径直接成功
        var secondHandler = await cache.LockAsync(lockKey, options);
        Assert.True(secondHandler.Acquired);
        Assert.True(await cache.KeyExistsAsync(lockKey));

        // 后台续期循环独立消耗唯一的配额（~267ms 后首次续期，配额耗尽），锁最终自然过期
        await Task.Delay(2500);
        Assert.False(await cache.KeyExistsAsync(lockKey));

        await secondHandler.DisposeAsync();
        await firstHandler.DisposeAsync();
        Assert.False(await cache.KeyExistsAsync(lockKey));
    }

    [Fact(DisplayName = "有限续期锁自然过期后再次加锁应重新访问Redis而不是复用旧scope")]
    public async Task LockAsync_ShouldNotReuseStaleScope_AfterLimitedRenewalLockExpires()
    {
        using var serviceProvider = RedisCacheTestSupport.BuildServiceProvider();
        using var contenderServiceProvider = RedisCacheTestSupport.BuildServiceProvider();
        var cache = serviceProvider.GetRequiredService<RedisCache>();
        var contenderCache = contenderServiceProvider.GetRequiredService<RedisCache>();
        var lockKey = $"test:lock:limited-renew-stale-scope:{Guid.NewGuid():N}";

        await cache.KeyDeleteAsync(lockKey);

        var options = new DistributedLockAcquisitionOptions
        {
            TimeModeHandler = LockTimeModeHandler.LimitedRenewal(TimeSpan.FromMilliseconds(300), maxRenewalCount: 1),
            RetryStrategy = new DefaultLockRetryStrategy(0)
        };

        var firstHandler = await cache.LockAsync(lockKey, options);
        Assert.True(firstHandler.Acquired);

        await Task.Delay(1000);
        Assert.False(await cache.KeyExistsAsync(lockKey));

        var contenderHandler = await contenderCache.LockAsync(lockKey, RedisCacheTestSupport.BuildLockOptions(TimeSpan.FromSeconds(3)));
        Assert.True(contenderHandler.Acquired);
        Assert.True(await cache.KeyExistsAsync(lockKey));

        var secondHandler = await cache.LockAsync(lockKey, options);
        Assert.False(secondHandler.Acquired);

        await secondHandler.DisposeAsync();
        await contenderHandler.DisposeAsync();
        await firstHandler.DisposeAsync();
    }

    [Fact(DisplayName = "缺少TimeModeHandler时应抛出参数异常而不是空引用异常")]
    public async Task LockAsync_ShouldThrowArgumentNullException_WhenTimeModeHandlerMissing()
    {
        using var serviceProvider = RedisCacheTestSupport.BuildServiceProvider();
        var cache = serviceProvider.GetRequiredService<RedisCache>();

        var options = new DistributedLockAcquisitionOptions
        {
            TimeModeHandler = null!,
            RetryStrategy = new DefaultLockRetryStrategy(0)
        };

        await Assert.ThrowsAsync<ArgumentNullException>(() => cache.LockAsync($"test:lock:missing-time-handler:{Guid.NewGuid():N}", options));
    }

    [Fact(DisplayName = "缺少RetryStrategy时应抛出参数异常而不是空引用异常")]
    public async Task LockAsync_ShouldThrowArgumentNullException_WhenRetryStrategyMissing()
    {
        using var serviceProvider = RedisCacheTestSupport.BuildServiceProvider();
        var cache = serviceProvider.GetRequiredService<RedisCache>();
        var lockKey = $"test:lock:missing-retry-strategy:{Guid.NewGuid():N}";

        await cache.KeyDeleteAsync(lockKey);
        var occupied = await cache.StringSetAsync(lockKey, "external-holder", TimeSpan.FromSeconds(5), When.NotExists);
        Assert.True(occupied);

        var options = new DistributedLockAcquisitionOptions
        {
            TimeModeHandler = LockTimeModeHandler.FixedExpiry(TimeSpan.FromSeconds(1)),
            RetryStrategy = null!
        };

        try
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => cache.LockAsync(lockKey, options));
        }
        finally
        {
            await cache.KeyDeleteAsync(lockKey);
        }
    }

    [Fact(DisplayName = "未配置RedisOptions时应抛出明确的配置异常")]
    public void AddRedis_ShouldThrowInvalidOperationException_WhenRedisOptionsMissing()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRedis();

        using var serviceProvider = services.BuildServiceProvider();

        Assert.Throws<InvalidOperationException>(() => serviceProvider.GetRequiredService<IConnectionMultiplexer>());
    }

    [Fact(DisplayName = "锁被外部抢占时重入应通过TryRenewAsync的result==0路径检测到并标记失主")]
    public async Task LockAsync_ShouldMarkLostOwnership_WhenReentryDetectsStolenLock()
    {
        using var serviceProvider = RedisCacheTestSupport.BuildServiceProvider();
        var cache = serviceProvider.GetRequiredService<RedisCache>();
        var lockKey = $"test:lock:reentry-stolen:{Guid.NewGuid():N}";
        await cache.KeyDeleteAsync(lockKey);

        // FixedExpiry 确保重入走 TryRenewAsync → Redis PEXPIRE 路径（不走时间戳快速路径）
        var options = new DistributedLockAcquisitionOptions
        {
            TimeModeHandler = LockTimeModeHandler.FixedExpiry(TimeSpan.FromSeconds(5)),
            RetryStrategy = new DefaultLockRetryStrategy(0)
        };

        var handler = await cache.LockAsync(lockKey, options);
        Assert.True(handler.Acquired);

        // 模拟锁被外部抢占：直接覆盖 key 为新值
        await cache.StringSetAsync(lockKey, "stolen-value");

        // 重入应触发 TryRenewAsync 的 result==0 分支 → MarkLostOwnership
        var reentry = await cache.LockAsync(lockKey, options);
        Assert.False(reentry.Acquired);
        Assert.False(handler.Acquired);

        await reentry.DisposeAsync();
        await handler.DisposeAsync();
        await cache.KeyDeleteAsync(lockKey);
    }

    [Fact(DisplayName = "后台续期循环检测到锁被抢占时应停止续期并标记失主")]
    public async Task LockAsync_ShouldStopRenewalLoop_WhenLockStolenDetected()
    {
        using var serviceProvider = RedisCacheTestSupport.BuildServiceProvider();
        var cache = serviceProvider.GetRequiredService<RedisCache>();
        var lockKey = $"test:lock:loop-stolen:{Guid.NewGuid():N}";
        await cache.KeyDeleteAsync(lockKey);

        // 自动续期 + 短过期，让续期循环快速运行并检测到抢占
        var options = new DistributedLockAcquisitionOptions
        {
            TimeModeHandler = LockTimeModeHandler.InfiniteRenewal(TimeSpan.FromMilliseconds(500)),
            RetryStrategy = new DefaultLockRetryStrategy(0)
        };

        var handler = await cache.LockAsync(lockKey, options);
        Assert.True(handler.Acquired);

        // 等待续期循环首次成功续期
        await Task.Delay(200);

        // 模拟锁被外部抢占
        await cache.StringSetAsync(lockKey, "stolen-value");

        // 等待续期循环下一次检测到值不匹配（续期间隔 ≈ 167ms，给足够时间）
        await Task.Delay(500);
        Assert.False(handler.Acquired);

        await handler.DisposeAsync();
        await cache.KeyDeleteAsync(lockKey);
    }

    [Fact(DisplayName = "有限续期配额耗尽后锁应自然过期，可被其他实例获取且原句柄释放安全")]
    public async Task LockAsync_ShouldExpireGracefully_WhenRenewalQuotaExhausted()
    {
        using var serviceProvider = RedisCacheTestSupport.BuildServiceProvider();
        using var otherServiceProvider = RedisCacheTestSupport.BuildServiceProvider();
        var cache = serviceProvider.GetRequiredService<RedisCache>();
        var otherCache = otherServiceProvider.GetRequiredService<RedisCache>();
        var lockKey = $"test:lock:quota-exhausted:{Guid.NewGuid():N}";
        await cache.KeyDeleteAsync(lockKey);

        // maxRenewalCount=1：续期循环消耗一次配额后停止
        var options = new DistributedLockAcquisitionOptions
        {
            TimeModeHandler = LockTimeModeHandler.LimitedRenewal(TimeSpan.FromMilliseconds(300), maxRenewalCount: 1),
            RetryStrategy = new DefaultLockRetryStrategy(0)
        };

        var handler = await cache.LockAsync(lockKey, options);
        Assert.True(handler.Acquired);

        // 续期循环在 ~100ms 消耗配额后停止，锁在 ~400ms 自然过期。等待足够时间
        await Task.Delay(1000);

        // 其他实例可获取同一把锁，证明原锁已过期
        var otherHandler = await otherCache.LockAsync(lockKey, RedisCacheTestSupport.BuildLockOptions(TimeSpan.FromSeconds(2)));
        Assert.True(otherHandler.Acquired);

        // 原句柄释放不应影响新实例持有的锁
        await handler.DisposeAsync();
        Assert.True(await cache.KeyExistsAsync(lockKey));

        await otherHandler.DisposeAsync();
        Assert.False(await cache.KeyExistsAsync(lockKey));
    }

    [Fact(DisplayName = "构造时传无效过期时间应抛出参数异常")]
    public void RedisLock_ShouldThrow_WhenExpiryNotPositive()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            using var serviceProvider = RedisCacheTestSupport.BuildServiceProvider();
            var cache = serviceProvider.GetRequiredService<RedisCache>();
            var options = new DistributedLockAcquisitionOptions
            {
                TimeModeHandler = new LockTimeModeHandler(TimeSpan.Zero),
                RetryStrategy = new DefaultLockRetryStrategy(0)
            };
            _ = cache.LockAsync($"test:lock:invalid-expiry:{Guid.NewGuid():N}", options).GetAwaiter().GetResult();
        });
    }

    [Fact(DisplayName = "有限续期模式锁被外部抢占时，续期循环应回滚预留配额并标记失主")]
    public async Task LockAsync_ShouldRollbackQuotaAndMarkLostOwnership_WhenLimitedRenewalLockStolen()
    {
        using var serviceProvider = RedisCacheTestSupport.BuildServiceProvider();
        var cache = serviceProvider.GetRequiredService<RedisCache>();
        var lockKey = $"test:lock:limited-stolen-rollback:{Guid.NewGuid():N}";
        await cache.KeyDeleteAsync(lockKey);

        // maxRenewalCount=3：让续期循环预留配额（didReserve=true），再用外部抢占触发 result==0 回滚
        var options = new DistributedLockAcquisitionOptions
        {
            TimeModeHandler = LockTimeModeHandler.LimitedRenewal(TimeSpan.FromMilliseconds(500), maxRenewalCount: 3),
            RetryStrategy = new DefaultLockRetryStrategy(0)
        };

        var handler = await cache.LockAsync(lockKey, options);
        Assert.True(handler.Acquired);

        // 等待续期循环首次续期成功（~167ms），此时 didReserve=true 且配额已预留
        await Task.Delay(300);

        // 模拟锁被外部抢占：覆盖 key 为新值
        await cache.StringSetAsync(lockKey, "stolen-value");

        // 等待续期循环下一次检测（~167ms 间隔）：result==0 + didReserve → Interlocked.Decrement + MarkLostOwnership
        await Task.Delay(500);
        Assert.False(handler.Acquired);

        await handler.DisposeAsync();
        await cache.KeyDeleteAsync(lockKey);
    }

    [Fact(DisplayName = "锁被续期循环标记失主后，同异步流重入应命中 !Acquired 守卫并回退到新锁获取")]
    public async Task LockAsync_ShouldHitNotAcquiredGuard_WhenReenteringAfterLoopDetectsStolenLock()
    {
        using var serviceProvider = RedisCacheTestSupport.BuildServiceProvider();
        var cache = serviceProvider.GetRequiredService<RedisCache>();
        var lockKey = $"test:lock:reenter-after-stolen:{Guid.NewGuid():N}";
        await cache.KeyDeleteAsync(lockKey);

        // 自动续期 + 短过期，让续期循环快速检测抢占
        var options = new DistributedLockAcquisitionOptions
        {
            TimeModeHandler = LockTimeModeHandler.InfiniteRenewal(TimeSpan.FromMilliseconds(400)),
            RetryStrategy = new DefaultLockRetryStrategy(0)
        };

        var handler = await cache.LockAsync(lockKey, options);
        Assert.True(handler.Acquired);

        // 模拟锁被外部抢占
        await cache.StringSetAsync(lockKey, "stolen-value");

        // 等待续期循环检测到值不匹配并调用 MarkLostOwnership
        await Task.Delay(600);
        Assert.False(handler.Acquired);

        // 同异步流再次 LockAsync：TryEnterAsync → TryReenterAsync → !Acquired 守卫 → 回退到 LockCoreAsync
        // 由于被抢占的 key 仍在 Redis，新锁无法获取
        var reentry = await cache.LockAsync(lockKey, new DistributedLockAcquisitionOptions
        {
            TimeModeHandler = LockTimeModeHandler.FixedExpiry(TimeSpan.FromSeconds(1)),
            RetryStrategy = new DefaultLockRetryStrategy(0)
        });
        Assert.False(reentry.Acquired);

        await reentry.DisposeAsync();
        await handler.DisposeAsync();
        await cache.KeyDeleteAsync(lockKey);
    }
}
