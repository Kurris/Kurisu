using AspectCore.Extensions.DependencyInjection;
using Kurisu.AspNetCore.Abstractions.Cache;
using Kurisu.AspNetCore.Abstractions.Cache.Aop;
using Kurisu.AspNetCore.Abstractions.DistributedLock;
using Kurisu.Extensions.Cache;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kurisu.Test.Cache.MethodCaching;

public class EvictionLockTests
{
    private static ServiceProvider Build(TestCache cache, TestLocks locks, EvictionWork work)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ICache>(cache);
        services.AddSingleton<ILockable>(locks);
        services.AddSingleton(work);
        services.AddMethodCaching(o =>
        {
            o.Default.LockWaitTimeout = TimeSpan.FromMilliseconds(300);
            o.Default.LockPollInterval = TimeSpan.FromMilliseconds(5);
        });
        services.AddTransient<IEvictionService, EvictionService>();
        return services.BuildDynamicProxyProvider();
    }

    [Fact]
    public async Task Lock_IsHeldDuringBusinessAndBatchDeletion_AndReleasedAfterwards()
    {
        var cache = new TestCache();
        var locks = new TestLocks();
        var work = new EvictionWork();
        await using var provider = Build(cache, locks, work);
        var service = provider.GetRequiredService<IEvictionService>();
        await service.GetAsync(1);
        var cacheKey = Assert.Single(cache.Data).Key;
        var events = new List<string>();
        locks.OnAcquired = key =>
        {
            Assert.Equal(cacheKey + ":load-lock", key);
            events.Add("lock");
        };
        work.Update = (_, _) =>
        {
            Assert.Equal(1, locks.Held);
            events.Add("business");
            return Task.CompletedTask;
        };
        cache.OnBatchRemove = () =>
        {
            Assert.Equal(1, locks.Held);
            events.Add("delete");
        };
        await service.UpdateAsync(1);
        Assert.Equal(new[] { "lock", "business", "delete" }, events);
        Assert.Empty(cache.Data);
        Assert.Equal(0, locks.Held);
    }

    [Fact]
    public async Task OldLoader_CompletesBeforeUpdate_AndItsResultIsDeleted()
    {
        var cache = new TestCache();
        var locks = new TestLocks();
        var work = new EvictionWork();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var value = "old";
        var updated = false;
        work.Query = async (_, token) =>
        {
            var snapshot = value;
            started.TrySetResult();
            await release.Task.WaitAsync(token);
            return snapshot;
        };
        work.Update = (_, _) =>
        {
            value = "new";
            updated = true;
            return Task.CompletedTask;
        };
        await using var provider = Build(cache, locks, work);
        var service = provider.GetRequiredService<IEvictionService>();
        var query = service.GetAsync(1);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(3));
        var update = service.UpdateAsync(1);
        try
        {
            await locks.Contended.Task.WaitAsync(TimeSpan.FromSeconds(3));
            Assert.False(updated);
        }
        finally
        {
            release.TrySetResult();
        }

        Assert.Equal("old", await query);
        await update;
        Assert.Empty(cache.Data);
        Assert.Equal("new", await service.GetAsync(1));
        Assert.Equal(0, locks.Held);
    }

    [Fact]
    public async Task Batch_LocksDistinctKeysInOrder_BeforeAnyBusinessOrDeletion()
    {
        var cache = new TestCache();
        var locks = new TestLocks();
        var work = new EvictionWork();
        await using var provider = Build(cache, locks, work);
        var service = provider.GetRequiredService<IEvictionService>();
        await service.GetAsync(1);
        await service.GetAsync(2);
        var expected = cache.Data.Keys.OrderBy(k => k, StringComparer.Ordinal).Select(k => k + ":load-lock").ToArray();
        var acquired = new List<string>();
        locks.OnAcquired = acquired.Add;
        work.Update = (_, _) =>
        {
            Assert.Equal(2, locks.Held);
            return Task.CompletedTask;
        };
        cache.OnBatchRemove = () => Assert.Equal(2, locks.Held);
        await service.BatchAsync([2, 1, 2]);
        Assert.Equal(expected, acquired);
        Assert.Equal(1, cache.BatchRemoves);
        Assert.Empty(cache.Data);
        Assert.Equal(0, locks.Held);
    }

    [Fact]
    public async Task PartialAcquisitionTimeout_ReleasesEarlierLocks_AndDoesNotRunBusiness()
    {
        var cache = new TestCache();
        var locks = new TestLocks();
        var work = new EvictionWork();
        await using var provider = Build(cache, locks, work);
        var service = provider.GetRequiredService<IEvictionService>();
        await service.GetAsync(1);
        await service.GetAsync(2);
        var lastKey = cache.Data.Keys.OrderBy(k => k, StringComparer.Ordinal).Last();
        await using var occupied = await locks.LockAsync(lastKey + ":load-lock", new DistributedLockAcquisitionOptions());
        await Assert.ThrowsAsync<TimeoutException>(() => service.BatchAsync([2, 1]));
        Assert.Equal(0, work.Updates);
        Assert.Equal(0, cache.BatchRemoves);
        Assert.Equal(1, locks.Held);
    }

    [Fact]
    public async Task CallerCancellationWhileWaiting_DoesNotRunBusiness()
    {
        var cache = new TestCache();
        var locks = new TestLocks { BlockAll = true };
        var work = new EvictionWork();
        await using var provider = Build(cache, locks, work);
        using var cancellation = new CancellationTokenSource();
        var update = provider.GetRequiredService<IEvictionService>().UpdateAsync(1, cancellation.Token);
        await locks.Contended.Task.WaitAsync(TimeSpan.FromSeconds(3));
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => update);
        Assert.Equal(0, work.Updates);
        Assert.Equal(0, cache.BatchRemoves);
        Assert.Equal(0, locks.Held);
    }

    [Fact]
    public async Task LockProviderFailure_DoesNotRunBusinessOrDeleteCache()
    {
        var cache = new TestCache();
        var locks = new TestLocks { Fail = true };
        var work = new EvictionWork();
        await using var provider = Build(cache, locks, work);
        await Assert.ThrowsAsync<IOException>(() => provider.GetRequiredService<IEvictionService>().UpdateAsync(1));
        Assert.Equal(0, work.Updates);
        Assert.Equal(0, cache.BatchRemoves);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BusinessOrDeletionFailure_ReleasesLock(bool deletionFails)
    {
        var cache = new TestCache();
        var locks = new TestLocks();
        var work = new EvictionWork();
        await using var provider = Build(cache, locks, work);
        var service = provider.GetRequiredService<IEvictionService>();
        await service.GetAsync(1);
        if (deletionFails) cache.OnBatchRemove = () => throw new IOException("delete failed");
        else work.Update = (_, _) => throw new IOException("business failed");
        await Assert.ThrowsAsync<IOException>(() => service.UpdateAsync(1));
        Assert.Equal(deletionFails ? 1 : 0, cache.BatchRemoves);
        Assert.Single(cache.Data);
        Assert.Equal(0, locks.Held);
    }
}

public class EvictionWork
{
    public int Updates;
    public Func<int, CancellationToken, Task<string>> Query = (_, _) => Task.FromResult("value");
    public Func<int, CancellationToken, Task> Update = (_, _) => Task.CompletedTask;
}

public interface IEvictionService
{
    [Cacheable("eviction")]
    Task<string> GetAsync(int id, CancellationToken token = default);

    [CacheEvict("eviction")]
    Task UpdateAsync(int id, CancellationToken token = default);

    [CacheEvict("eviction", Keys = "ids")]
    Task BatchAsync(int[] ids, CancellationToken token = default);
}

public class EvictionService(EvictionWork work) : IEvictionService
{
    public Task<string> GetAsync(int id, CancellationToken token = default) => work.Query(id, token);

    public Task UpdateAsync(int id, CancellationToken token = default)
    {
        work.Updates++;
        return work.Update(id, token);
    }

    public async Task BatchAsync(int[] ids, CancellationToken token = default)
    {
        foreach (var id in ids) await UpdateAsync(id, token);
    }
}