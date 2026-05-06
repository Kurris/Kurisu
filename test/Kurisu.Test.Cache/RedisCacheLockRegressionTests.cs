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

    [Fact(DisplayName = "有限续期模式下同异步流重入应续期并消耗续期次数")]
    public async Task LockAsync_ShouldRenewLeaseAndConsumeQuotaOnReentry_WhenModeIsLimitedRenewal()
    {
        using var serviceProvider = RedisCacheTestSupport.BuildServiceProvider();
        var cache = serviceProvider.GetRequiredService<RedisCache>();
        var lockKey = $"test:lock:limited-renew-reentry-renew:{Guid.NewGuid():N}";

        await cache.KeyDeleteAsync(lockKey);

        var options = new DistributedLockAcquisitionOptions
        {
            TimeModeHandler = LockTimeModeHandler.LimitedRenewal(TimeSpan.FromMilliseconds(2500), maxRenewalCount: 1),
            RetryStrategy = new DefaultLockRetryStrategy(0)
        };

        var firstHandler = await cache.LockAsync(lockKey, options);
        Assert.True(firstHandler.Acquired);

        await Task.Delay(200);

        var secondHandler = await cache.LockAsync(lockKey, options);
        Assert.True(secondHandler.Acquired);
        Assert.True(await cache.KeyExistsAsync(lockKey));

        await Task.Delay(1800);
        Assert.True(await cache.KeyExistsAsync(lockKey));

        await Task.Delay(900);
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
}
