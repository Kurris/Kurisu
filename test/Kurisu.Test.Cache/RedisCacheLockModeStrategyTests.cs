using Kurisu.AspNetCore.Abstractions.Cache;
using Kurisu.Extensions.Cache;
using Kurisu.Extensions.Cache.Locking;
using Kurisu.Extensions.Cache.Options;
using Kurisu.Extensions.Cache.Providers;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Xunit;

namespace Kurisu.Test.Cache;

[Trait("feature", "lock-mode-strategy")]
public class RedisCacheLockModeStrategyTests
{
    [Fact(DisplayName = "有限续期模式到达上限后应停止续期")]
    public async Task LockAsync_ShouldStopRenewing_WhenLimitedRenewalReached()
    {
        using var serviceProvider = RedisCacheTestSupport.BuildServiceProvider();
        var cache = serviceProvider.GetRequiredService<RedisCache>();
        var lockKey = $"test:lock:limited-renew:{Guid.NewGuid():N}";

        await cache.KeyDeleteAsync(lockKey);

        var options = new DistributedLockAcquisitionOptions
        {
            TimeModeHandler = LockTimeModeHandler.LimitedRenewal(TimeSpan.FromMilliseconds(500), maxRenewalCount: 1),
            RetryStrategy = new NoRetryDistributedLockRetryStrategy()
        };

        var handler = await cache.LockAsync(lockKey, options);
        Assert.True(handler.Acquired);

        await Task.Delay(1500);

        Assert.False(await cache.KeyExistsAsync(lockKey));
        await handler.DisposeAsync();
    }

    [Fact(DisplayName = "指定自定义锁时间处理器时应按处理器行为执行")]
    public async Task LockAsync_ShouldUseCustomTimeModeHandler_WhenHandlerTypeProvided()
    {
        using var serviceProvider = RedisCacheTestSupport.BuildServiceProvider();
        var cache = serviceProvider.GetRequiredService<RedisCache>();
        var lockKey = $"test:lock:custom-time-handler:{Guid.NewGuid():N}";

        await cache.KeyDeleteAsync(lockKey);

        var options = new DistributedLockAcquisitionOptions
        {
            TimeModeHandler = new CustomNoRenewalTimeModeHandler(),
            RetryStrategy = new NoRetryDistributedLockRetryStrategy()
        };

        var handler = await cache.LockAsync(lockKey, options);
        Assert.True(handler.Acquired);

        await Task.Delay(500);

        Assert.False(await cache.KeyExistsAsync(lockKey));
        await handler.DisposeAsync();
    }

    [Fact(DisplayName = "指定自定义重试策略时应覆盖默认重试处理")]
    public async Task LockAsync_ShouldUseCustomRetryStrategy_WhenStrategyTypeProvided()
    {
        using var serviceProvider = RedisCacheTestSupport.BuildServiceProvider();
        var cache = serviceProvider.GetRequiredService<RedisCache>();
        var lockKey = $"test:lock:custom-retry:{Guid.NewGuid():N}";

        await cache.KeyDeleteAsync(lockKey);

        var occupied = await cache.StringSetAsync(lockKey, "temporary-holder", TimeSpan.FromMilliseconds(300), When.NotExists);
        Assert.True(occupied);

        var options = new DistributedLockAcquisitionOptions
        {
            TimeModeHandler = LockTimeModeHandler.FixedExpiry(TimeSpan.FromSeconds(2)),
            RetryStrategy = new CustomAggressiveRetryStrategy()
        };

        var handler = await cache.LockAsync(lockKey, options);
        Assert.True(handler.Acquired);

        await handler.DisposeAsync();
        Assert.False(await cache.KeyExistsAsync(lockKey));
    }

    [Fact(DisplayName = "重试模式为NoRetry时不应发生重试")]
    public async Task LockAsync_ShouldNotRetry_WhenRetryModeIsNoRetry()
    {
        using var serviceProvider = RedisCacheTestSupport.BuildServiceProvider();
        var cache = serviceProvider.GetRequiredService<RedisCache>();
        var lockKey = $"test:lock:no-retry-mode:{Guid.NewGuid():N}";

        await cache.KeyDeleteAsync(lockKey);
        var occupied = await cache.StringSetAsync(lockKey, "external-holder", TimeSpan.FromSeconds(2), When.NotExists);
        Assert.True(occupied);

        var options = new DistributedLockAcquisitionOptions
        {
            TimeModeHandler = LockTimeModeHandler.FixedExpiry(TimeSpan.FromSeconds(1)),
            RetryStrategy = new NoRetryDistributedLockRetryStrategy()
        };

        var handler = await cache.LockAsync(lockKey, options);
        Assert.False(handler.Acquired);

        await handler.DisposeAsync();
        await cache.KeyDeleteAsync(lockKey);
    }

    private sealed class CustomNoRenewalTimeModeHandler : IDistributedLockTimeModeHandler
    {
        public DistributedLockTimeSettings Resolve()
        {
            return new DistributedLockTimeSettings
            {
                Expiry = TimeSpan.FromMilliseconds(200),
                EnableAutoRenewal = false,
                MaxRenewalCount = null
            };
        }
    }

    private sealed class CustomAggressiveRetryStrategy : IDistributedLockRetryStrategy
    {
        public Task<bool> ShouldRetryAsync(int attempt, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(attempt < 20);
        }

        public Task DelayBeforeRetryAsync(int attempt, CancellationToken cancellationToken = default)
        {
            return Task.Delay(TimeSpan.FromMilliseconds(50), cancellationToken);
        }
    }
}
