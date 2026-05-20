using System.Diagnostics;
using Kurisu.AspNetCore.Abstractions.DistributedLock;
using Kurisu.Extensions.Cache.Providers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kurisu.Test.Cache;

[Trait("feature", "performance")]
public class RedisCacheLockPerformanceTests
{
    [Fact(DisplayName = "高并发独立锁请求应在可接受延迟内完成")]
    public async Task LockAsync_ShouldHandleHighConcurrency_WithAcceptableLatency()
    {
        const int requestCount = 120;
        var elapsed = Stopwatch.StartNew();

        var handlers = await Task.WhenAll(Enumerable.Range(0, requestCount)
            .Select(async i =>
            {
                using var sp = RedisCacheTestSupport.BuildServiceProvider();
                var lockable = sp.GetRequiredService<ILockable>();
                var handler = await lockable.LockAsync($"test:lock:perf:{Guid.NewGuid():N}:{i}", RedisCacheTestSupport.BuildLockOptions(TimeSpan.FromSeconds(3), retryCount: 1, enableRetry: false));
                return handler;
            }));

        elapsed.Stop();

        var acquiredHandlers = handlers.Where(x => x.Acquired).ToArray();
        Assert.Equal(requestCount, acquiredHandlers.Length);
        Assert.True(elapsed.Elapsed < TimeSpan.FromSeconds(15));

        foreach (var handler in acquiredHandlers)
        {
            await handler.DisposeAsync();
        }

        // 抽样校验释放后不残留
        using var sampleSp = RedisCacheTestSupport.BuildServiceProvider();
        var cache = sampleSp.GetRequiredService<RedisCache>();
        var lockable = sampleSp.GetRequiredService<ILockable>();
        var sampleKey = $"test:lock:perf:sample:{Guid.NewGuid():N}";
        await cache.KeyDeleteAsync(sampleKey);
        await using var sampleHandler = await lockable.LockAsync(sampleKey, RedisCacheTestSupport.BuildLockOptions(TimeSpan.FromSeconds(3)));
        Assert.True(sampleHandler.Acquired);
    }
}
