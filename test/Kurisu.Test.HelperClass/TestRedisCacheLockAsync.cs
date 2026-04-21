using Kurisu.Extensions.Cache;
using Kurisu.Extensions.Cache.Implements;
using Kurisu.Extensions.Cache.Options;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using System.Diagnostics;

namespace Kurisu.Test.HelperClass;

[Trait("customClass", "redisCache")]
public class TestRedisCacheLockAsync
{
    /// <summary>
    /// 同一异步调用链内对同一个 lockKey 重复加锁时，应复用本地可重入锁，避免重复申请 Redis 锁。
    /// </summary>
    [Fact]
    public async Task LockAsync_ShouldReuseLock_WhenSameKeyInSameAsyncFlow()
    {
        using var serviceProvider = BuildServiceProvider();
        if (serviceProvider == null)
        {
            return;
        }

        var cache = serviceProvider.GetRequiredService<RedisCache>();
        var db = serviceProvider.GetRequiredService<IConnectionMultiplexer>().GetDatabase();

        var lockKey = $"test:lock:reuse:{Guid.NewGuid():N}";

        await db.KeyDeleteAsync(lockKey);

        await using var firstHandler = await cache.LockAsync(lockKey, TimeSpan.FromSeconds(10));
        await using var secondHandler = await cache.LockAsync(lockKey, TimeSpan.FromSeconds(10));

        Assert.True(firstHandler.Acquired);
        Assert.True(secondHandler.Acquired);
        Assert.True(await db.KeyExistsAsync(lockKey));

        await secondHandler.DisposeAsync();
        Assert.True(await db.KeyExistsAsync(lockKey));

        await firstHandler.DisposeAsync();
        Assert.False(await db.KeyExistsAsync(lockKey));
    }

    /// <summary>
    /// 同一异步调用链在 await 边界后再次加锁，仍应复用本地可重入锁。
    /// </summary>
    [Fact]
    public async Task LockAsync_ShouldReuseLock_AcrossAwaitBoundaryInSameAsyncFlow()
    {
        using var serviceProvider = BuildServiceProvider();
        if (serviceProvider == null)
        {
            return;
        }

        var cache = serviceProvider.GetRequiredService<RedisCache>();
        var db = serviceProvider.GetRequiredService<IConnectionMultiplexer>().GetDatabase();

        var lockKey = $"test:lock:reuse-await:{Guid.NewGuid():N}";

        await db.KeyDeleteAsync(lockKey);

        await using var firstHandler = await cache.LockAsync(lockKey, TimeSpan.FromSeconds(10));
        await Task.Yield();
        await using var secondHandler = await cache.LockAsync(lockKey, TimeSpan.FromSeconds(10));

        Assert.True(firstHandler.Acquired);
        Assert.True(secondHandler.Acquired);
        Assert.True(await db.KeyExistsAsync(lockKey));

        await secondHandler.DisposeAsync();
        Assert.True(await db.KeyExistsAsync(lockKey));

        await firstHandler.DisposeAsync();
        Assert.False(await db.KeyExistsAsync(lockKey));
    }

    /// <summary>
    /// 释放发生在抑制 ExecutionContext 流动的异步延续中时，也应正确释放底层 Redis 锁。
    /// </summary>
    [Fact]
    public async Task LockAsync_ShouldReleaseLock_WhenDisposeRunsWithoutAsyncLocalContext()
    {
        using var serviceProvider = BuildServiceProvider();
        if (serviceProvider == null)
        {
            return;
        }

        var cache = serviceProvider.GetRequiredService<RedisCache>();
        var db = serviceProvider.GetRequiredService<IConnectionMultiplexer>().GetDatabase();

        var lockKey = $"test:lock:dispose-cross-context:{Guid.NewGuid():N}";

        await db.KeyDeleteAsync(lockKey);

        var firstHandler = await cache.LockAsync(lockKey, TimeSpan.FromSeconds(10));
        var secondHandler = await cache.LockAsync(lockKey, TimeSpan.FromSeconds(10));

        Assert.True(firstHandler.Acquired);
        Assert.True(secondHandler.Acquired);
        Assert.True(await db.KeyExistsAsync(lockKey));

        var asyncFlowControl = ExecutionContext.SuppressFlow();
        var disposeTask = Task.Run(async () =>
        {
            await secondHandler.DisposeAsync();
            await firstHandler.DisposeAsync();
        });
        asyncFlowControl.Undo();
        await disposeTask;

        Assert.False(await db.KeyExistsAsync(lockKey));
    }

    /// <summary>
    /// 同一异步调用链内进行三层及以上重入时，引用计数应正确累加与回收。
    /// </summary>
    [Fact]
    public async Task LockAsync_ShouldSupportTripleReentrancy_AndReleaseOnFinalDispose()
    {
        using var serviceProvider = BuildServiceProvider();
        if (serviceProvider == null)
        {
            return;
        }

        var cache = serviceProvider.GetRequiredService<RedisCache>();
        var db = serviceProvider.GetRequiredService<IConnectionMultiplexer>().GetDatabase();
        var lockKey = $"test:lock:triple-reentry:{Guid.NewGuid():N}";

        await db.KeyDeleteAsync(lockKey);

        await using var firstHandler = await cache.LockAsync(lockKey, TimeSpan.FromSeconds(10));
        await using var secondHandler = await cache.LockAsync(lockKey, TimeSpan.FromSeconds(10));
        await using var thirdHandler = await cache.LockAsync(lockKey, TimeSpan.FromSeconds(10));

        Assert.True(firstHandler.Acquired);
        Assert.True(secondHandler.Acquired);
        Assert.True(thirdHandler.Acquired);
        Assert.True(await db.KeyExistsAsync(lockKey));

        await thirdHandler.DisposeAsync();
        Assert.True(await db.KeyExistsAsync(lockKey));

        await secondHandler.DisposeAsync();
        Assert.True(await db.KeyExistsAsync(lockKey));

        await firstHandler.DisposeAsync();
        Assert.False(await db.KeyExistsAsync(lockKey));
    }

    /// <summary>
    /// 同一释放句柄重复释放时，应保持幂等，不抛异常且不误删锁状态。
    /// </summary>
    [Fact]
    public async Task LockAsync_ShouldBeIdempotent_WhenDisposeCalledMultipleTimes()
    {
        using var serviceProvider = BuildServiceProvider();
        if (serviceProvider == null)
        {
            return;
        }

        var cache = serviceProvider.GetRequiredService<RedisCache>();
        var db = serviceProvider.GetRequiredService<IConnectionMultiplexer>().GetDatabase();
        var lockKey = $"test:lock:idempotent-dispose:{Guid.NewGuid():N}";

        await db.KeyDeleteAsync(lockKey);

        var handler = await cache.LockAsync(lockKey, TimeSpan.FromSeconds(3));
        Assert.True(handler.Acquired);

        await handler.DisposeAsync();
        await handler.DisposeAsync();

        Assert.False(await db.KeyExistsAsync(lockKey));
    }

    /// <summary>
    /// 当锁被其他持有者占用且未配置重试间隔时，应直接返回未获取成功的句柄。
    /// </summary>
    [Fact]
    public async Task LockAsync_ShouldReturnFailedHandler_WhenAcquireFailedAndNoRetryInterval()
    {
        using var serviceProvider = BuildServiceProvider();
        if (serviceProvider == null)
        {
            return;
        }

        var cache = serviceProvider.GetRequiredService<RedisCache>();
        var db = serviceProvider.GetRequiredService<IConnectionMultiplexer>().GetDatabase();

        var lockKey = $"test:lock:fail:{Guid.NewGuid():N}";

        await db.KeyDeleteAsync(lockKey);
        var occupied = await db.StringSetAsync(lockKey, "external-holder", TimeSpan.FromSeconds(10), when: When.NotExists);
        Assert.True(occupied);

        var handler = await cache.LockAsync(lockKey, TimeSpan.FromSeconds(10), retryInterval: null, retryCount: 3);

        Assert.False(handler.Acquired);

        await db.KeyDeleteAsync(lockKey);
    }

    /// <summary>
    /// 短暂竞争解除后（模拟抖动恢复），带重试的加锁应最终成功。
    /// </summary>
    [Fact]
    public async Task LockAsync_ShouldSucceedAfterTransientContention_WhenRetryEnabled()
    {
        using var serviceProvider = BuildServiceProvider();
        if (serviceProvider == null)
        {
            return;
        }

        var cache = serviceProvider.GetRequiredService<RedisCache>();
        var db = serviceProvider.GetRequiredService<IConnectionMultiplexer>().GetDatabase();
        var lockKey = $"test:lock:transient:{Guid.NewGuid():N}";

        await db.KeyDeleteAsync(lockKey);
        var occupied = await db.StringSetAsync(lockKey, "temporary-holder", TimeSpan.FromSeconds(2), when: When.NotExists);
        Assert.True(occupied);

        _ = Task.Run(async () =>
        {
            await Task.Delay(250);
            await db.KeyDeleteAsync(lockKey);
        });

        var handler = await cache.LockAsync(lockKey, TimeSpan.FromSeconds(3), retryInterval: TimeSpan.FromMilliseconds(100), retryCount: 10);

        Assert.True(handler.Acquired);
        await handler.DisposeAsync();
        Assert.False(await db.KeyExistsAsync(lockKey));
    }

    /// <summary>
    /// Redis 连接不可用时应抛出异常，调用方可感知故障并处理。
    /// </summary>
    [Fact]
    public async Task LockAsync_ShouldThrow_WhenRedisConnectionIsUnavailable()
    {
        using var serviceProvider = BuildServiceProvider();
        if (serviceProvider == null)
        {
            return;
        }

        var cache = serviceProvider.GetRequiredService<RedisCache>();
        var multiplexer = serviceProvider.GetRequiredService<IConnectionMultiplexer>();
        multiplexer.Dispose();

        await Assert.ThrowsAnyAsync<Exception>(async () =>
        {
            _ = await cache.LockAsync($"test:lock:redis-down:{Guid.NewGuid():N}", TimeSpan.FromSeconds(3), retryInterval: null, retryCount: 1);
        });
    }

    /// <summary>
    /// 多并发竞争同一把锁时，应保证互斥：同一轮无重试竞争仅一个成功持有者。
    /// </summary>
    [Fact]
    public async Task LockAsync_ShouldBeMutuallyExclusive_WhenConcurrentCompetition()
    {
        using var serviceProvider = BuildServiceProvider();
        if (serviceProvider == null)
        {
            return;
        }

        var cache = serviceProvider.GetRequiredService<RedisCache>();
        var db = serviceProvider.GetRequiredService<IConnectionMultiplexer>().GetDatabase();
        var lockKey = $"test:lock:mutex:{Guid.NewGuid():N}";
        await db.KeyDeleteAsync(lockKey);

        const int workers = 20;
        var gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var tasks = Enumerable.Range(0, workers).Select(async _ =>
        {
            await gate.Task;
            var handler = await cache.LockAsync(lockKey, TimeSpan.FromSeconds(3), retryInterval: null, retryCount: 1);
            return handler;
        }).ToArray();

        gate.SetResult(true);
        var handlers = await Task.WhenAll(tasks);

        var acquiredHandlers = handlers.Where(x => x.Acquired).ToArray();
        Assert.Single(acquiredHandlers);

        foreach (var handler in handlers)
        {
            await handler.DisposeAsync();
        }

        Assert.False(await db.KeyExistsAsync(lockKey));
    }

    /// <summary>
    /// 显式过期时间下不释放句柄时，锁应在 TTL 到期后自动释放，后续请求可重新获取。
    /// </summary>
    [Fact]
    public async Task LockAsync_ShouldAutoReleaseAfterExpiry_WhenNotDisposed()
    {
        using var serviceProvider = BuildServiceProvider();
        using var contenderServiceProvider = BuildServiceProvider();
        if (serviceProvider == null)
        {
            return;
        }

        if (contenderServiceProvider == null)
        {
            return;
        }

        var cache = serviceProvider.GetRequiredService<RedisCache>();
        var contenderCache = contenderServiceProvider.GetRequiredService<RedisCache>();
        var db = serviceProvider.GetRequiredService<IConnectionMultiplexer>().GetDatabase();
        var lockKey = $"test:lock:auto-expire:{Guid.NewGuid():N}";
        await db.KeyDeleteAsync(lockKey);

        var firstHandler = await cache.LockAsync(lockKey, TimeSpan.FromMilliseconds(800));
        Assert.True(firstHandler.Acquired);
        Assert.True(await db.KeyExistsAsync(lockKey));

        await Task.Delay(1400);

        var secondHandler = await contenderCache.LockAsync(lockKey, TimeSpan.FromSeconds(3), retryInterval: null, retryCount: 1);
        Assert.True(secondHandler.Acquired);

        await firstHandler.DisposeAsync();
        Assert.True(await db.KeyExistsAsync(lockKey));

        await secondHandler.DisposeAsync();
        Assert.False(await db.KeyExistsAsync(lockKey));
    }

    /// <summary>
    /// 旧持有者过期后再释放时，不应误删新持有者的锁（Value 唯一性校验）。
    /// </summary>
    [Fact]
    public async Task LockAsync_ShouldNotDeleteNewOwnerLock_WhenOldOwnerDisposesAfterExpiry()
    {
        using var serviceProvider = BuildServiceProvider();
        using var anotherServiceProvider = BuildServiceProvider();
        if (serviceProvider == null)
        {
            return;
        }

        if (anotherServiceProvider == null)
        {
            return;
        }

        var cache = serviceProvider.GetRequiredService<RedisCache>();
        var anotherCache = anotherServiceProvider.GetRequiredService<RedisCache>();
        var db = serviceProvider.GetRequiredService<IConnectionMultiplexer>().GetDatabase();
        var lockKey = $"test:lock:no-wrong-delete:{Guid.NewGuid():N}";
        await db.KeyDeleteAsync(lockKey);

        var firstHandler = await cache.LockAsync(lockKey, TimeSpan.FromMilliseconds(700));
        Assert.True(firstHandler.Acquired);

        await Task.Delay(1300);

        var secondHandler = await anotherCache.LockAsync(lockKey, TimeSpan.FromSeconds(3), retryInterval: null, retryCount: 1);
        Assert.True(secondHandler.Acquired);
        Assert.True(await db.KeyExistsAsync(lockKey));

        await firstHandler.DisposeAsync();
        Assert.True(await db.KeyExistsAsync(lockKey));

        await secondHandler.DisposeAsync();
        Assert.False(await db.KeyExistsAsync(lockKey));
    }

    /// <summary>
    /// 默认模式（未显式传过期时间）应启用自动续期，长任务期间锁不应提前过期。
    /// </summary>
    [Fact]
    public async Task LockAsync_ShouldKeepAlive_WhenNoExplicitExpiryAndTaskRunsLongerThanDefaultTtl()
    {
        using var serviceProvider = BuildServiceProvider();
        using var contenderServiceProvider = BuildServiceProvider();
        if (serviceProvider == null)
        {
            return;
        }

        if (contenderServiceProvider == null)
        {
            return;
        }

        var cache = serviceProvider.GetRequiredService<RedisCache>();
        var contenderCache = contenderServiceProvider.GetRequiredService<RedisCache>();
        var db = serviceProvider.GetRequiredService<IConnectionMultiplexer>().GetDatabase();
        var lockKey = $"test:lock:watchdog:{Guid.NewGuid():N}";
        await db.KeyDeleteAsync(lockKey);

        var holder = await cache.LockAsync(lockKey);
        Assert.True(holder.Acquired);

        await Task.Delay(7500);
        Assert.True(await db.KeyExistsAsync(lockKey));

        var contender = await contenderCache.LockAsync(lockKey, TimeSpan.FromSeconds(2), retryInterval: null, retryCount: 1);
        Assert.False(contender.Acquired);

        await contender.DisposeAsync();
        await holder.DisposeAsync();
        Assert.False(await db.KeyExistsAsync(lockKey));
    }

    /// <summary>
    /// 高并发下大量独立锁请求应快速完成，且最终全部可释放。
    /// </summary>
    [Fact]
    public async Task LockAsync_ShouldHandleHighConcurrency_WithAcceptableLatency()
    {
        using var serviceProvider = BuildServiceProvider();
        if (serviceProvider == null)
        {
            return;
        }

        var cache = serviceProvider.GetRequiredService<RedisCache>();
        var db = serviceProvider.GetRequiredService<IConnectionMultiplexer>().GetDatabase();

        const int requestCount = 120;
        var elapsed = Stopwatch.StartNew();

        var handlers = await Task.WhenAll(Enumerable.Range(0, requestCount)
            .Select(i => cache.LockAsync($"test:lock:perf:{Guid.NewGuid():N}:{i}", TimeSpan.FromSeconds(3), retryInterval: null, retryCount: 1)));

        elapsed.Stop();

        Assert.Equal(requestCount, handlers.Count(x => x.Acquired));
        Assert.True(elapsed.Elapsed < TimeSpan.FromSeconds(15));

        foreach (var handler in handlers)
        {
            await handler.DisposeAsync();
        }

        // 抽样校验释放后不残留
        var sampleKey = $"test:lock:perf:sample:{Guid.NewGuid():N}";
        await db.KeyDeleteAsync(sampleKey);
        await using var sampleHandler = await cache.LockAsync(sampleKey, TimeSpan.FromSeconds(3));
        Assert.True(sampleHandler.Acquired);
    }

    /// <summary>
    /// 通过依赖注入构建 RedisCache，连接字符串来自环境变量 KURISU_TEST_REDIS。
    /// </summary>
    private static ServiceProvider? BuildServiceProvider()
    {
        const string fallbackConnectionString = "192.168.199.124:6379,defaultDatabase=1,password=dlhis123";
        var connectionString = Environment.GetEnvironmentVariable("KURISU_TEST_REDIS");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = fallbackConnectionString;
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return null;
        }

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions<RedisOptions>().Configure(options => options.ConnectionString = connectionString);
        services.AddRedis();
        return services.BuildServiceProvider();
    }
}
