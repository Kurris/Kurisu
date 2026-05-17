using Kurisu.Extensions.Cache.Providers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kurisu.Test.Cache;

[Trait("feature", "lifecycle")]
public class RedisCacheLockLifecycleTests
{
    [Fact(DisplayName = "显式过期且不释放句柄时应在TTL后自动释放")]
    public async Task LockAsync_ShouldAutoReleaseAfterExpiry_WhenNotDisposed()
    {
        using var serviceProvider = RedisCacheTestSupport.BuildServiceProvider();
        using var contenderServiceProvider = RedisCacheTestSupport.BuildServiceProvider();
        var cache = serviceProvider.GetRequiredService<RedisCache>();
        var contenderCache = contenderServiceProvider.GetRequiredService<RedisCache>();
        var lockKey = $"test:lock:auto-expire:{Guid.NewGuid():N}";
        await cache.KeyDeleteAsync(lockKey);

        var firstHandler = await cache.LockAsync(lockKey, RedisCacheTestSupport.BuildLockOptions(TimeSpan.FromMilliseconds(800)));
        Assert.True(firstHandler.Acquired);
        Assert.True(await cache.KeyExistsAsync(lockKey));

        await Task.Delay(1400);

        var secondHandler = await contenderCache.LockAsync(lockKey, RedisCacheTestSupport.BuildLockOptions(TimeSpan.FromSeconds(3), retryCount: 1, enableRetry: false));
        Assert.True(secondHandler.Acquired);

        await firstHandler.DisposeAsync();
        Assert.True(await cache.KeyExistsAsync(lockKey));

        await secondHandler.DisposeAsync();
        Assert.False(await cache.KeyExistsAsync(lockKey));
    }

    [Fact(DisplayName = "旧持有者过期后释放不应删除新持有者锁")]
    public async Task LockAsync_ShouldNotDeleteNewOwnerLock_WhenOldOwnerDisposesAfterExpiry()
    {
        using var serviceProvider = RedisCacheTestSupport.BuildServiceProvider();
        using var anotherServiceProvider = RedisCacheTestSupport.BuildServiceProvider();
        var cache = serviceProvider.GetRequiredService<RedisCache>();
        var anotherCache = anotherServiceProvider.GetRequiredService<RedisCache>();
        var lockKey = $"test:lock:no-wrong-delete:{Guid.NewGuid():N}";
        await cache.KeyDeleteAsync(lockKey);

        var firstHandler = await cache.LockAsync(lockKey, RedisCacheTestSupport.BuildLockOptions(TimeSpan.FromMilliseconds(700)));
        Assert.True(firstHandler.Acquired);

        await Task.Delay(1300);

        var secondHandler = await anotherCache.LockAsync(lockKey, RedisCacheTestSupport.BuildLockOptions(TimeSpan.FromSeconds(3), retryCount: 1, enableRetry: false));
        Assert.True(secondHandler.Acquired);
        Assert.True(await cache.KeyExistsAsync(lockKey));

        await firstHandler.DisposeAsync();
        Assert.True(await cache.KeyExistsAsync(lockKey));

        await secondHandler.DisposeAsync();
        Assert.False(await cache.KeyExistsAsync(lockKey));
    }

    [Fact(DisplayName = "未显式过期时应自动续期并在长任务中保持锁")]
    public async Task LockAsync_ShouldKeepAlive_WhenNoExplicitExpiryAndTaskRunsLongerThanDefaultTtl()
    {
        using var serviceProvider = RedisCacheTestSupport.BuildServiceProvider();
        using var contenderServiceProvider = RedisCacheTestSupport.BuildServiceProvider();
        var cache = serviceProvider.GetRequiredService<RedisCache>();
        var contenderCache = contenderServiceProvider.GetRequiredService<RedisCache>();
        var lockKey = $"test:lock:watchdog:{Guid.NewGuid():N}";
        await cache.KeyDeleteAsync(lockKey);

        var holder = await cache.LockAsync(lockKey, RedisCacheTestSupport.BuildLockOptions());
        Assert.True(holder.Acquired);

        await Task.Delay(7500);
        Assert.True(await cache.KeyExistsAsync(lockKey));

        var contender = await contenderCache.LockAsync(lockKey, RedisCacheTestSupport.BuildLockOptions(TimeSpan.FromSeconds(2), retryCount: 1, enableRetry: false));
        Assert.False(contender.Acquired);

        await contender.DisposeAsync();
        await holder.DisposeAsync();
        Assert.False(await cache.KeyExistsAsync(lockKey));
    }
}
