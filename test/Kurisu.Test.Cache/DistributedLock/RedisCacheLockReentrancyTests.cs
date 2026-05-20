using Kurisu.Extensions.Cache.Providers;
using Kurisu.AspNetCore.Abstractions.DistributedLock;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kurisu.Test.Cache;

[Trait("feature", "reentrancy")]
public class RedisCacheLockReentrancyTests
{
    public static IEnumerable<object[]> ReentrantModes()
    {
        yield return
        [
            "无限续期",
            RedisCacheTestSupport.BuildLockOptions(),
            true
        ];

        yield return
        [
            "固定过期",
            RedisCacheTestSupport.BuildLockOptions(TimeSpan.FromMilliseconds(600)),
            true
        ];

        yield return
        [
            "有限续期",
            new Kurisu.AspNetCore.Abstractions.DistributedLock.DistributedLockAcquisitionOptions
            {
                TimeModeHandler = Kurisu.AspNetCore.Abstractions.DistributedLock.LockTimeModeHandler.LimitedRenewal(TimeSpan.FromMilliseconds(1200), maxRenewalCount: 1),
                RetryStrategy = new Kurisu.AspNetCore.Abstractions.DistributedLock.DefaultLockRetryStrategy(0)
            },
            true
        ];
    }

    public static IEnumerable<object[]> TripleReentrantModes()
    {
        yield return
        [
            "无限续期",
            RedisCacheTestSupport.BuildLockOptions(),
            true,
            true
        ];

        yield return
        [
            "固定过期",
            RedisCacheTestSupport.BuildLockOptions(TimeSpan.FromMilliseconds(600)),
            true,
            true
        ];

        yield return
        [
            "有限续期",
            new Kurisu.AspNetCore.Abstractions.DistributedLock.DistributedLockAcquisitionOptions
            {
                TimeModeHandler = Kurisu.AspNetCore.Abstractions.DistributedLock.LockTimeModeHandler.LimitedRenewal(TimeSpan.FromMilliseconds(1200), maxRenewalCount: 1),
                RetryStrategy = new Kurisu.AspNetCore.Abstractions.DistributedLock.DefaultLockRetryStrategy(0)
            },
            true,
            true // 重入走时间戳快速路径，不再被配额阻断
        ];
    }

    [Theory(DisplayName = "同异步流内同一key重复加锁在不同模式下都应成功")]
    [MemberData(nameof(ReentrantModes))]
    public async Task LockAsync_ShouldSucceed_WhenSameKeyInSameAsyncFlow_WithDifferentModes(
        string modeName,
        Kurisu.AspNetCore.Abstractions.DistributedLock.DistributedLockAcquisitionOptions options,
        bool expectedAcquired)
    {
        using var serviceProvider = RedisCacheTestSupport.BuildServiceProvider();
        var cache = serviceProvider.GetRequiredService<RedisCache>();
        var lockable = serviceProvider.GetRequiredService<ILockable>();

        var lockKey = $"test:lock:reuse:{modeName}:{Guid.NewGuid():N}";

        await cache.KeyDeleteAsync(lockKey);

        await using var firstHandler = await lockable.LockAsync(lockKey, options);
        await using var secondHandler = await lockable.LockAsync(lockKey, options);

        Assert.True(firstHandler.Acquired);
        Assert.Equal(expectedAcquired, secondHandler.Acquired);
        Assert.True(await cache.KeyExistsAsync(lockKey));

        await secondHandler.DisposeAsync();
        Assert.True(await cache.KeyExistsAsync(lockKey));

        await firstHandler.DisposeAsync();
        Assert.False(await cache.KeyExistsAsync(lockKey));
    }

    [Theory(DisplayName = "跨await边界后同异步流在不同模式下都应成功")]
    [MemberData(nameof(ReentrantModes))]
    public async Task LockAsync_ShouldSucceed_AcrossAwaitBoundaryInSameAsyncFlow_WithDifferentModes(
        string modeName,
        Kurisu.AspNetCore.Abstractions.DistributedLock.DistributedLockAcquisitionOptions options,
        bool expectedAcquired)
    {
        using var serviceProvider = RedisCacheTestSupport.BuildServiceProvider();
        var cache = serviceProvider.GetRequiredService<RedisCache>();
        var lockable = serviceProvider.GetRequiredService<ILockable>();

        var lockKey = $"test:lock:reuse-await:{modeName}:{Guid.NewGuid():N}";

        await cache.KeyDeleteAsync(lockKey);

        await using var firstHandler = await lockable.LockAsync(lockKey, options);
        // 人为制造一次异步续体切换；如果仍在同一 async flow，AsyncLocal 上下文应被保留。
        await Task.Yield();
        // 关键点：同一个 lockKey 在同一 async flow 内应命中本地可重入上下文，而不是再次抢 Redis 锁。
        await using var secondHandler = await lockable.LockAsync(lockKey, options);

        Assert.True(firstHandler.Acquired);
        Assert.Equal(expectedAcquired, secondHandler.Acquired);
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
        var lockable = serviceProvider.GetRequiredService<ILockable>();

        var lockKey = $"test:lock:dispose-cross-context:{Guid.NewGuid():N}";

        await cache.KeyDeleteAsync(lockKey);

        var firstHandler = await lockable.LockAsync(lockKey, RedisCacheTestSupport.BuildLockOptions());
        var secondHandler = await lockable.LockAsync(lockKey, RedisCacheTestSupport.BuildLockOptions());

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

    [Theory(DisplayName = "三层重入在不同模式下应符合续期配额预期并最终正确解锁")]
    [MemberData(nameof(TripleReentrantModes))]
    public async Task LockAsync_ShouldSupportTripleReentrancy_AndReleaseOnFinalDispose_WithDifferentModes(
        string modeName,
        Kurisu.AspNetCore.Abstractions.DistributedLock.DistributedLockAcquisitionOptions options,
        bool secondExpectedAcquired,
        bool thirdExpectedAcquired)
    {
        using var serviceProvider = RedisCacheTestSupport.BuildServiceProvider();
        var cache = serviceProvider.GetRequiredService<RedisCache>();
        var lockable = serviceProvider.GetRequiredService<ILockable>();
        var lockKey = $"test:lock:triple-reentry:{modeName}:{Guid.NewGuid():N}";

        await cache.KeyDeleteAsync(lockKey);

        await using var firstHandler = await lockable.LockAsync(lockKey, options);
        await using var secondHandler = await lockable.LockAsync(lockKey, options);
        await using var thirdHandler = await lockable.LockAsync(lockKey, options);

        Assert.True(firstHandler.Acquired);
        Assert.Equal(secondExpectedAcquired, secondHandler.Acquired);
        Assert.Equal(thirdExpectedAcquired, thirdHandler.Acquired);
        Assert.True(await cache.KeyExistsAsync(lockKey));

        await thirdHandler.DisposeAsync();
        Assert.True(await cache.KeyExistsAsync(lockKey));

        await secondHandler.DisposeAsync();
        Assert.True(await cache.KeyExistsAsync(lockKey));

        await firstHandler.DisposeAsync();
        Assert.False(await cache.KeyExistsAsync(lockKey));
    }

    [Theory(DisplayName = "中间层释放后在不同模式下应符合续期配额预期")]
    [MemberData(nameof(TripleReentrantModes))]
    public async Task LockAsync_ShouldContinueReentrantReuse_AfterIntermediateDispose_WithDifferentModes(
        string modeName,
        Kurisu.AspNetCore.Abstractions.DistributedLock.DistributedLockAcquisitionOptions options,
        bool secondExpectedAcquired,
        bool fourthExpectedAcquired)
    {
        using var serviceProvider = RedisCacheTestSupport.BuildServiceProvider();
        var cache = serviceProvider.GetRequiredService<RedisCache>();
        var lockable = serviceProvider.GetRequiredService<ILockable>();

        var lockKey = $"test:lock:partial-release:{modeName}:{Guid.NewGuid():N}";

        await cache.KeyDeleteAsync(lockKey);

        await using var firstHandler = await lockable.LockAsync(lockKey, options);
        var secondHandler = await lockable.LockAsync(lockKey, options);
        await using var thirdHandler = await lockable.LockAsync(lockKey, options);

        Assert.True(firstHandler.Acquired);
        Assert.Equal(secondExpectedAcquired, secondHandler.Acquired);
        Assert.Equal(fourthExpectedAcquired, thirdHandler.Acquired);
        Assert.True(await cache.KeyExistsAsync(lockKey));

        await secondHandler.DisposeAsync();
        Assert.True(await cache.KeyExistsAsync(lockKey));

        var fourthHandler = await lockable.LockAsync(lockKey, options);
        Assert.Equal(fourthExpectedAcquired, fourthHandler.Acquired);
        Assert.True(await cache.KeyExistsAsync(lockKey));

        await fourthHandler.DisposeAsync();
        await thirdHandler.DisposeAsync();
        await firstHandler.DisposeAsync();

        Assert.False(await cache.KeyExistsAsync(lockKey));
    }

    [Fact(DisplayName = "重入时应沿用首次加锁模式，不因后续不同参数实例而改变行为")]
    public async Task LockAsync_ShouldReuseFirstLockSemantics_WhenReentrantHandlersUseDifferentOptionInstances()
    {
        using var serviceProvider = RedisCacheTestSupport.BuildServiceProvider();
        var cache = serviceProvider.GetRequiredService<RedisCache>();
        var lockable = serviceProvider.GetRequiredService<ILockable>();
        var lockKey = $"test:lock:mixed-options:{Guid.NewGuid():N}";

        await cache.KeyDeleteAsync(lockKey);

        var firstOptions = RedisCacheTestSupport.BuildLockOptions();
        var secondOptions = RedisCacheTestSupport.BuildLockOptions();
        var thirdOptions = RedisCacheTestSupport.BuildLockOptions();

        await using var firstHandler = await lockable.LockAsync(lockKey, firstOptions);
        var secondHandler = await lockable.LockAsync(lockKey, secondOptions);
        var thirdHandler = await lockable.LockAsync(lockKey, thirdOptions);

        // 二次、三次重入只是复用第一次成功获取的真实锁，不会用新的 options 改写锁模式。
        Assert.True(firstHandler.Acquired);
        Assert.True(secondHandler.Acquired);
        Assert.True(thirdHandler.Acquired);

        await thirdHandler.DisposeAsync();
        Assert.True(await cache.KeyExistsAsync(lockKey));

        await secondHandler.DisposeAsync();
        Assert.True(await cache.KeyExistsAsync(lockKey));

        await firstHandler.DisposeAsync();
        Assert.False(await cache.KeyExistsAsync(lockKey));
    }
}
