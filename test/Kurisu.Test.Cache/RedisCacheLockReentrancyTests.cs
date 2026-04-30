using Kurisu.Extensions.Cache.Providers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kurisu.Test.Cache;

[Trait("feature", "reentrancy")]
public class RedisCacheLockReentrancyTests
{
    [Fact(DisplayName = "同异步流内同一key重复加锁应复用本地锁")]
    public async Task LockAsync_ShouldReuseLock_WhenSameKeyInSameAsyncFlow()
    {
        using var serviceProvider = RedisCacheTestSupport.BuildServiceProvider();
        var cache = serviceProvider.GetRequiredService<RedisCache>();

        var lockKey = $"test:lock:reuse:{Guid.NewGuid():N}";

        await cache.KeyDeleteAsync(lockKey);

        await using var firstHandler = await cache.LockAsync(lockKey, RedisCacheTestSupport.BuildLockOptions(TimeSpan.FromSeconds(10)));
        await using var secondHandler = await cache.LockAsync(lockKey, RedisCacheTestSupport.BuildLockOptions(TimeSpan.FromSeconds(10)));

        Assert.True(firstHandler.Acquired);
        Assert.True(secondHandler.Acquired);
        Assert.True(await cache.KeyExistsAsync(lockKey));

        await secondHandler.DisposeAsync();
        Assert.True(await cache.KeyExistsAsync(lockKey));

        await firstHandler.DisposeAsync();
        Assert.False(await cache.KeyExistsAsync(lockKey));
    }

    [Fact(DisplayName = "跨await边界后同异步流仍应复用本地锁")]
    public async Task LockAsync_ShouldReuseLock_AcrossAwaitBoundaryInSameAsyncFlow()
    {
        using var serviceProvider = RedisCacheTestSupport.BuildServiceProvider();
        var cache = serviceProvider.GetRequiredService<RedisCache>();

        var lockKey = $"test:lock:reuse-await:{Guid.NewGuid():N}";

        await cache.KeyDeleteAsync(lockKey);

        await using var firstHandler = await cache.LockAsync(lockKey, RedisCacheTestSupport.BuildLockOptions(TimeSpan.FromSeconds(10)));
        // 人为制造一次异步续体切换；如果仍在同一 async flow，AsyncLocal 上下文应被保留。
        await Task.Yield();
        // 关键点：同一个 lockKey 在同一 async flow 内应命中本地可重入上下文，而不是再次抢 Redis 锁。
        await using var secondHandler = await cache.LockAsync(lockKey, RedisCacheTestSupport.BuildLockOptions(TimeSpan.FromSeconds(10)));

        Assert.True(firstHandler.Acquired);
        Assert.True(secondHandler.Acquired);
        // 若确实复用的是同一底层锁，Redis 中只会有这一个锁键。
        Assert.True(await cache.KeyExistsAsync(lockKey));

        await secondHandler.DisposeAsync();
        Assert.True(await cache.KeyExistsAsync(lockKey));

        await firstHandler.DisposeAsync();
        Assert.False(await cache.KeyExistsAsync(lockKey));
    }

    [Fact(DisplayName = "抑制上下文流后释放也应正确解锁")]
    public async Task LockAsync_ShouldReleaseLock_WhenDisposeRunsWithoutAsyncLocalContext()
    {
        using var serviceProvider = RedisCacheTestSupport.BuildServiceProvider();
        var cache = serviceProvider.GetRequiredService<RedisCache>();

        var lockKey = $"test:lock:dispose-cross-context:{Guid.NewGuid():N}";

        await cache.KeyDeleteAsync(lockKey);

        var firstHandler = await cache.LockAsync(lockKey, RedisCacheTestSupport.BuildLockOptions(TimeSpan.FromSeconds(10)));
        var secondHandler = await cache.LockAsync(lockKey, RedisCacheTestSupport.BuildLockOptions(TimeSpan.FromSeconds(10)));

        Assert.True(firstHandler.Acquired);
        Assert.True(secondHandler.Acquired);
        Assert.True(await cache.KeyExistsAsync(lockKey));

        var asyncFlowControl = ExecutionContext.SuppressFlow();
        var disposeTask = Task.Run(async () =>
        {
            await firstHandler.DisposeAsync();
            await secondHandler.DisposeAsync();
        });
        asyncFlowControl.Undo();
        await disposeTask;

        Assert.False(await cache.KeyExistsAsync(lockKey));
    }

    [Fact(DisplayName = "三层重入应在最终释放后正确解锁")]
    public async Task LockAsync_ShouldSupportTripleReentrancy_AndReleaseOnFinalDispose()
    {
        using var serviceProvider = RedisCacheTestSupport.BuildServiceProvider();
        var cache = serviceProvider.GetRequiredService<RedisCache>();
        var lockKey = $"test:lock:triple-reentry:{Guid.NewGuid():N}";

        await cache.KeyDeleteAsync(lockKey);

        await using var firstHandler = await cache.LockAsync(lockKey, RedisCacheTestSupport.BuildLockOptions(TimeSpan.FromSeconds(10)));
        await using var secondHandler = await cache.LockAsync(lockKey, RedisCacheTestSupport.BuildLockOptions(TimeSpan.FromSeconds(10)));
        await using var thirdHandler = await cache.LockAsync(lockKey, RedisCacheTestSupport.BuildLockOptions(TimeSpan.FromSeconds(10)));

        Assert.True(firstHandler.Acquired);
        Assert.True(secondHandler.Acquired);
        Assert.True(thirdHandler.Acquired);
        Assert.True(await cache.KeyExistsAsync(lockKey));

        await thirdHandler.DisposeAsync();
        Assert.True(await cache.KeyExistsAsync(lockKey));

        await secondHandler.DisposeAsync();
        Assert.True(await cache.KeyExistsAsync(lockKey));

        await firstHandler.DisposeAsync();
        Assert.False(await cache.KeyExistsAsync(lockKey));
    }

    [Fact(DisplayName = "中间层释放后仍应继续重入复用")]
    public async Task LockAsync_ShouldContinueReentrantReuse_AfterIntermediateDispose()
    {
        using var serviceProvider = RedisCacheTestSupport.BuildServiceProvider();
        var cache = serviceProvider.GetRequiredService<RedisCache>();

        var lockKey = $"test:lock:partial-release:{Guid.NewGuid():N}";

        await cache.KeyDeleteAsync(lockKey);

        await using var firstHandler = await cache.LockAsync(lockKey, RedisCacheTestSupport.BuildLockOptions(TimeSpan.FromSeconds(10)));
        var secondHandler = await cache.LockAsync(lockKey, RedisCacheTestSupport.BuildLockOptions(TimeSpan.FromSeconds(10)));
        await using var thirdHandler = await cache.LockAsync(lockKey, RedisCacheTestSupport.BuildLockOptions(TimeSpan.FromSeconds(10)));

        Assert.True(firstHandler.Acquired);
        Assert.True(secondHandler.Acquired);
        Assert.True(thirdHandler.Acquired);
        Assert.True(await cache.KeyExistsAsync(lockKey));

        await secondHandler.DisposeAsync();
        Assert.True(await cache.KeyExistsAsync(lockKey));

        var fourthHandler = await cache.LockAsync(lockKey, RedisCacheTestSupport.BuildLockOptions(TimeSpan.FromSeconds(10)));
        Assert.True(fourthHandler.Acquired);
        Assert.True(await cache.KeyExistsAsync(lockKey));

        await fourthHandler.DisposeAsync();
        await thirdHandler.DisposeAsync();
        await firstHandler.DisposeAsync();

        Assert.False(await cache.KeyExistsAsync(lockKey));
    }
}
