using Kurisu.Extensions.Cache.Providers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kurisu.Test.Cache;

[Trait("feature", "concurrency")]
public class RedisCacheLockConcurrencyTests
{
    [Fact(DisplayName = "并发竞争同一锁时应保持互斥")]
    public async Task LockAsync_ShouldBeMutuallyExclusive_WhenConcurrentCompetition()
    {
        using var serviceProvider = RedisCacheTestSupport.BuildServiceProvider();
        var cache = serviceProvider.GetRequiredService<RedisCache>();
        var lockKey = $"test:lock:mutex:{Guid.NewGuid():N}";
        await cache.KeyDeleteAsync(lockKey);

        const int workers = 20;
        var gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var tasks = Enumerable.Range(0, workers).Select(async _ =>
        {
            await gate.Task;
            var handler = await cache.LockAsync(lockKey, RedisCacheTestSupport.BuildLockOptions(TimeSpan.FromSeconds(3), retryCount: 1, enableRetry: false));
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

        Assert.False(await cache.KeyExistsAsync(lockKey));
    }
}
