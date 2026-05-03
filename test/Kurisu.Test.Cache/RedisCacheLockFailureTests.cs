using Kurisu.Extensions.Cache.Providers;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Xunit;

namespace Kurisu.Test.Cache;

[Trait("feature", "failure-retry")]
public class RedisCacheLockFailureTests
{
    [Fact(DisplayName = "重复释放句柄应幂等且不抛异常")]
    public async Task LockAsync_ShouldBeIdempotent_WhenDisposeCalledMultipleTimes()
    {
        using var serviceProvider = RedisCacheTestSupport.BuildServiceProvider();
        var cache = serviceProvider.GetRequiredService<RedisCache>();
        var lockKey = $"test:lock:idempotent-dispose:{Guid.NewGuid():N}";

        await cache.KeyDeleteAsync(lockKey);

        var handler = await cache.LockAsync(lockKey, RedisCacheTestSupport.BuildLockOptions(TimeSpan.FromSeconds(3)));
        Assert.True(handler.Acquired);

        await handler.DisposeAsync();
        await handler.DisposeAsync();

        Assert.False(await cache.KeyExistsAsync(lockKey));
    }

    [Fact(DisplayName = "禁用重试且锁被占用时应返回失败句柄")]
    public async Task LockAsync_ShouldReturnFailedHandler_WhenRetryIsDisabled()
    {
        using var serviceProvider = RedisCacheTestSupport.BuildServiceProvider();
        var cache = serviceProvider.GetRequiredService<RedisCache>();

        var lockKey = $"test:lock:fail:{Guid.NewGuid():N}";

        await cache.KeyDeleteAsync(lockKey);
        var occupied = await cache.StringSetAsync(lockKey, "external-holder", TimeSpan.FromSeconds(10), When.NotExists);
        Assert.True(occupied);

        var handler = await cache.LockAsync(lockKey, RedisCacheTestSupport.BuildLockOptions(TimeSpan.FromSeconds(10), enableRetry: false));
        Assert.False(handler.Acquired);

        await cache.KeyDeleteAsync(lockKey);
    }

    [Fact(DisplayName = "retryCount为1时应至少重试一次并成功获取")]
    public async Task LockAsync_ShouldRetryOnce_WhenRetryCountIsOne()
    {
        using var serviceProvider = RedisCacheTestSupport.BuildServiceProvider();
        var cache = serviceProvider.GetRequiredService<RedisCache>();

        var lockKey = $"test:lock:retry-once:{Guid.NewGuid():N}";

        await cache.KeyDeleteAsync(lockKey);
        var occupied = await cache.StringSetAsync(lockKey, "temporary-holder", TimeSpan.FromSeconds(2), When.NotExists);
        Assert.True(occupied);

        _ = Task.Run(async () =>
        {
            await Task.Delay(1200);
            await cache.KeyDeleteAsync(lockKey);
        });

        var handler = await cache.LockAsync(lockKey, RedisCacheTestSupport.BuildLockOptions(TimeSpan.FromSeconds(3), retryCount: 15));

        Assert.True(handler.Acquired);
        await handler.DisposeAsync();
        Assert.False(await cache.KeyExistsAsync(lockKey));
    }

    [Fact(DisplayName = "失败句柄释放不应删除外部已持有锁")]
    public async Task LockAsync_ShouldNotDeleteExternalLock_WhenFailedHandlerIsDisposed()
    {
        using var serviceProvider = RedisCacheTestSupport.BuildServiceProvider();
        var cache = serviceProvider.GetRequiredService<RedisCache>();

        var lockKey = $"test:lock:failed-handler-dispose:{Guid.NewGuid():N}";

        await cache.KeyDeleteAsync(lockKey);
        var occupied = await cache.StringSetAsync(lockKey, "external-holder", TimeSpan.FromSeconds(10), When.NotExists);
        Assert.True(occupied);

        var handler = await cache.LockAsync(lockKey, RedisCacheTestSupport.BuildLockOptions(TimeSpan.FromSeconds(10), retryCount: 1, enableRetry: false));

        Assert.False(handler.Acquired);

        await handler.DisposeAsync();
        Assert.True(await cache.KeyExistsAsync(lockKey));

        await cache.KeyDeleteAsync(lockKey);
    }

    [Fact(DisplayName = "短暂竞争解除后启用重试应最终成功")]
    public async Task LockAsync_ShouldSucceedAfterTransientContention_WhenRetryEnabled()
    {
        using var serviceProvider = RedisCacheTestSupport.BuildServiceProvider();
        var cache = serviceProvider.GetRequiredService<RedisCache>();
        var lockKey = $"test:lock:transient:{Guid.NewGuid():N}";

        await cache.KeyDeleteAsync(lockKey);
        var occupied = await cache.StringSetAsync(lockKey, "temporary-holder", TimeSpan.FromSeconds(2), When.NotExists);
        Assert.True(occupied);

        _ = Task.Run(async () =>
        {
            await Task.Delay(250);
            await cache.KeyDeleteAsync(lockKey);
        });

        var handler = await cache.LockAsync(lockKey, RedisCacheTestSupport.BuildLockOptions(TimeSpan.FromSeconds(3), retryCount: 10));

        Assert.True(handler.Acquired);
        await handler.DisposeAsync();
        Assert.False(await cache.KeyExistsAsync(lockKey));
    }

    [Fact(DisplayName = "Redis连接不可用时应抛出异常")]
    public async Task LockAsync_ShouldThrow_WhenRedisConnectionIsUnavailable()
    {
        await Assert.ThrowsAnyAsync<Exception>(async () =>
        {
            using var serviceProvider = RedisCacheTestSupport.BuildServiceProvider("127.0.0.1:0,abortConnect=false,connectTimeout=100,syncTimeout=100");
            var cache = serviceProvider.GetRequiredService<RedisCache>();
            _ = await cache.LockAsync($"test:lock:redis-down:{Guid.NewGuid():N}", RedisCacheTestSupport.BuildLockOptions(TimeSpan.FromSeconds(3), retryCount: 1, enableRetry: false));
        });
    }
}
