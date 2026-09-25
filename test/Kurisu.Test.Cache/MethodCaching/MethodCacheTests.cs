using System.Collections.Concurrent;
using AspectCore.DynamicProxy;
using AspectCore.Extensions.DependencyInjection;
using Kurisu.AspNetCore.Abstractions.Cache;
using Kurisu.AspNetCore.Abstractions.Authentication;
using Kurisu.AspNetCore.Abstractions.Cache.Aop;
using Kurisu.AspNetCore.Abstractions.DataAccess.Core;
using Kurisu.AspNetCore.Abstractions.DistributedLock;
using Kurisu.Extensions.Cache;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Moq;
using Xunit;

namespace Kurisu.Test.Cache.MethodCaching;

public class MethodCacheTests
{
    internal static ServiceProvider Build(ICache cache, ILockable locks, QueryWork work,
        Action<MethodCachePolicy>? configure = null, TestTransactions? transactions = null, string tenant = "a", ICurrentUser? user = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(cache);
        services.AddSingleton(locks);
        services.AddSingleton(work);
        if (user != null) services.AddSingleton(user);
        services.AddSingleton<IMethodCacheScopeContributor>(new TenantScope(tenant));
        if (transactions != null) services.AddSingleton<ITransactionCallbackRegistry>(transactions);
        services.AddMethodCaching(o =>
        {
            var policy = o.Policies["Default"];
            policy.LockPollInterval = TimeSpan.FromMilliseconds(5);
            policy.ExpiryJitterRatio = 0;
            configure?.Invoke(policy);
        });
        services.AddTransient<IQueryService, QueryService>();
        return services.BuildDynamicProxyProvider();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public async Task CachesResultIncludingNull_AndRestoresTask(int id)
    {
        var cache = new TestCache();
        var work = new QueryWork();
        using var provider = Build(cache, new TestLocks(), work);
        var service = provider.GetRequiredService<IQueryService>();
        Assert.Equal(await service.GetAsync(id), await service.GetAsync(id));
        Assert.Equal(1, work.Calls);
        Assert.Equal(id == 0 ? TimeSpan.FromSeconds(15) : TimeSpan.FromMinutes(5), cache.LastExpiry);
    }

    [Fact]
    public async Task IndependentContainers_ShareOneDistributedLoad()
    {
        var cache = new TestCache();
        var locks = new TestLocks();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var work = new QueryWork { Handler = async (_, token) => { entered.TrySetResult(); await release.Task.WaitAsync(token); return "loaded"; } };
        using var first = Build(cache, locks, work);
        using var second = Build(cache, locks, work);
        var leader = first.GetRequiredService<IQueryService>().GetAsync(1);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var followers = Enumerable.Range(0, 12).Select(_ => second.GetRequiredService<IQueryService>().GetAsync(1)).ToArray();
        await locks.Contended.Task.WaitAsync(TimeSpan.FromSeconds(5));
        release.SetResult();
        Assert.All(await Task.WhenAll(followers.Append(leader)), value => Assert.Equal("loaded", value));
        Assert.Equal(1, work.Calls);
        Assert.Equal(0, locks.Held);
    }

    [Fact]
    public async Task DifferentKeys_DoNotBlockEachOther()
    {
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var work = new QueryWork { Handler = async (id, token) => { if (id == 1) { entered.SetResult(); await release.Task.WaitAsync(token); } return id.ToString(); } };
        using var provider = Build(new TestCache(), new TestLocks(), work);
        var service = provider.GetRequiredService<IQueryService>();
        var first = service.GetAsync(1);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal("2", await service.GetAsync(2).WaitAsync(TimeSpan.FromSeconds(2)));
        release.SetResult();
        await first;
    }

    [Fact]
    public async Task TenantAndMethodAndCanonicalParameters_ArePartOfKey()
    {
        var cache = new TestCache();
        var locks = new TestLocks();
        var work = new QueryWork();
        using var a = Build(cache, locks, work, tenant: "a");
        using var b = Build(cache, locks, work, tenant: "b");
        await a.GetRequiredService<IQueryService>().GetAsync(1);
        await b.GetRequiredService<IQueryService>().GetAsync(1);
        Assert.Equal(2, work.Calls);
        var service = a.GetRequiredService<IQueryService>();
        await service.SearchAsync(new() { ["x"] = 1, ["y"] = 2 });
        using var cts = new CancellationTokenSource();
        await service.SearchAsync(new() { ["y"] = 2, ["x"] = 1 }, cts.Token);
        Assert.Equal(3, work.Calls);
        await service.OtherSearchAsync(new() { ["x"] = 1, ["y"] = 2 });
        Assert.Equal(4, work.Calls);
    }

    [Fact]
    public async Task Failure_ReleasesLock_AndNeverCachesOrRetriesBusinessCall()
    {
        var work = new QueryWork { Handler = (_, _) => throw new InvalidOperationException("business") };
        var locks = new TestLocks();
        using var provider = Build(new TestCache(), locks, work, p => p.BypassOnCacheFailure = true);
        var service = provider.GetRequiredService<IQueryService>();
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetAsync(1));
        Assert.Equal(1, work.Calls);
        Assert.Equal(0, locks.Held);
        work.Handler = (_, _) => Task.FromResult<string?>("recovered");
        Assert.Equal("recovered", await service.GetAsync(1));
        Assert.Equal(2, work.Calls);
    }

    [Fact]
    public async Task LockTimeout_DoesNotLoad_AndCanBeExplicitlyDowngraded()
    {
        var locks = new TestLocks { BlockAll = true };
        var work = new QueryWork();
        using var provider = Build(new TestCache(), locks, work, p => p.LockWaitTimeout = TimeSpan.FromMilliseconds(40));
        await Assert.ThrowsAsync<TimeoutException>(() => provider.GetRequiredService<IQueryService>().GetAsync(1));
        Assert.Equal(0, work.Calls);
        using var fallback = Build(new TestCache(), locks, work, p => { p.LockWaitTimeout = TimeSpan.FromMilliseconds(40); p.BypassOnLockTimeout = true; });
        Assert.Equal("1", await fallback.GetRequiredService<IQueryService>().GetAsync(1));
        Assert.Equal(1, work.Calls);
    }

    [Fact]
    public async Task CancellationWhileWaiting_DoesNotLoad()
    {
        var locks = new TestLocks { BlockAll = true };
        var work = new QueryWork();
        using var provider = Build(new TestCache(), locks, work);
        using var cts = new CancellationTokenSource();
        var task = provider.GetRequiredService<IQueryService>().GetAsync(1, cts.Token);
        await locks.Contended.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        Assert.Equal(0, work.Calls);
    }

    [Fact]
    public async Task QueryCancellation_AfterWaitDeadline_DoesNotExecuteAgain()
    {
        var cache = new TestCache();
        var locks = new TestLocks();
        var work = new QueryWork { Handler = async (_, token) => { await Task.Delay(Timeout.Infinite, token); return "unused"; } };
        using var provider = Build(cache, locks, work, p =>
        {
            p.LockWaitTimeout = TimeSpan.FromMilliseconds(20);
            p.QueryTimeout = TimeSpan.FromMilliseconds(70);
            p.BypassOnLockTimeout = true;
        });
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => provider.GetRequiredService<IQueryService>().GetAsync(1));
        Assert.Equal(1, work.Calls);
        Assert.Empty(cache.Data);
        Assert.Equal(0, locks.Held);
    }

    [Fact]
    public async Task CacheFailure_DegradesOnlyWhenConfigured_AndWriteFailureDoesNotRepeatQuery()
    {
        var cache = new TestCache { FailRead = true };
        var work = new QueryWork();
        using var strict = Build(cache, new TestLocks(), work);
        await Assert.ThrowsAsync<IOException>(() => strict.GetRequiredService<IQueryService>().GetAsync(1));
        Assert.Equal(0, work.Calls);
        using var fallback = Build(cache, new TestLocks(), work, p => p.BypassOnCacheFailure = true);
        Assert.Equal("1", await fallback.GetRequiredService<IQueryService>().GetAsync(1));
        Assert.Equal(1, work.Calls);
        cache.FailRead = false;
        cache.FailWrite = true;
        Assert.Equal("2", await strict.GetRequiredService<IQueryService>().GetAsync(2));
        Assert.Equal(2, work.Calls);
    }

    [Fact]
    public async Task Eviction_WaitsForCommit_RollbackKeepsCache_AndTransactionsBypassCache()
    {
        var cache = new TestCache();
        var tx = new TestTransactions();
        var work = new QueryWork();
        using var provider = Build(cache, new TestLocks(), work, transactions: tx);
        var service = provider.GetRequiredService<IQueryService>();
        await service.GetAsync(1);
        tx.HasActiveTransaction = true;
        await service.GetAsync(1);
        Assert.Equal(2, work.Calls);
        await service.UpdateAsync(1);
        Assert.Single(cache.Data);
        tx.Rollback();
        await service.GetAsync(1);
        Assert.Equal(2, work.Calls);
        tx.HasActiveTransaction = true;
        await service.UpdateAsync(1);
        Assert.Single(cache.Data);
        await tx.CommitAsync();
        Assert.Empty(cache.Data);
        await service.GetAsync(1);
        Assert.Equal(3, work.Calls);
        await service.UpdateAsync(1);
        Assert.Empty(cache.Data);
    }

    [Fact]
    public async Task AuthorizationRunsOnCacheHit_UnsupportedReturnTypeFails()
    {
        var work = new QueryWork();
        using var provider = Build(new TestCache(), new TestLocks(), work);
        var service = provider.GetRequiredService<IQueryService>();
        await service.GetAsync(1);
        work.Denied = true;
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetAsync(1));
        Assert.Equal(1, work.Calls);
        Assert.Throws<NotSupportedException>(() => service.Invalid());
    }

    [Fact]
    public async Task DistributedLock_RechecksCacheAfterAcquisition()
    {
        var cache = new TestCache();
        var work = new QueryWork();
        var locks = new TestLocks
        {
            OnAcquired = key => cache.Data[key[..^":load-lock".Length]] = "{\"Value\":\"other-instance\"}"
        };
        using var provider = Build(cache, locks, work);
        Assert.Equal("other-instance", await provider.GetRequiredService<IQueryService>().GetAsync(1));
        Assert.Equal(0, work.Calls);
    }

    [Fact]
    public async Task LockProviderFailure_RespectsPolicy()
    {
        var locks = new TestLocks { Fail = true };
        var work = new QueryWork();
        using var strict = Build(new TestCache(), locks, work);
        await Assert.ThrowsAsync<IOException>(() => strict.GetRequiredService<IQueryService>().GetAsync(1));
        Assert.Equal(0, work.Calls);
        using var fallback = Build(new TestCache(), locks, work, p => p.BypassOnCacheFailure = true);
        Assert.Equal("1", await fallback.GetRequiredService<IQueryService>().GetAsync(1));
        Assert.Equal(1, work.Calls);
    }

    [Fact]
    public async Task SameKeyRecursiveQuery_IsRejectedInsteadOfDeadlocking()
    {
        var work = new QueryWork();
        var locks = new TestLocks();
        using var provider = Build(new TestCache(), locks, work);
        var service = provider.GetRequiredService<IQueryService>();
        work.Handler = (id, token) => service.GetAsync(id, token);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetAsync(1));
        Assert.Equal(1, work.Calls);
        Assert.Equal(0, locks.Held);
    }

    [Fact]
    public async Task NullCachingCanBeDisabled_AndZeroIsStillAHit()
    {
        var work = new QueryWork();
        using var provider = Build(new TestCache(), new TestLocks(), work, p => p.CacheNull = false);
        var service = provider.GetRequiredService<IQueryService>();
        await service.GetAsync(0);
        await service.GetAsync(0);
        Assert.Equal(2, work.Calls);
        Assert.Equal(0, await service.ZeroAsync());
        Assert.Equal(0, await service.ZeroAsync());
        Assert.Equal(3, work.Calls);
    }

    [Fact]
    public async Task LostLease_DoesNotPublishResult()
    {
        var locks = new TestLocks();
        var cache = new TestCache();
        var work = new QueryWork { Handler = (_, _) => { locks.LostOwnership = true; return Task.FromResult<string?>("loaded"); } };
        using var provider = Build(cache, locks, work);
        Assert.Equal("loaded", await provider.GetRequiredService<IQueryService>().GetAsync(1));
        Assert.Empty(cache.Data);
        Assert.Equal(0, locks.Held);
    }

    [Fact]
    public async Task UserIdentity_IsIsolatedByDefault()
    {
        var cache = new TestCache();
        var locks = new TestLocks();
        var work = new QueryWork();
        var firstUser = new Mock<ICurrentUser>();
        firstUser.Setup(x => x.GetUserId<string>()).Returns("first");
        var secondUser = new Mock<ICurrentUser>();
        secondUser.Setup(x => x.GetUserId<string>()).Returns("second");
        using var first = Build(cache, locks, work, user: firstUser.Object);
        using var second = Build(cache, locks, work, user: secondUser.Object);
        await first.GetRequiredService<IQueryService>().GetAsync(1);
        await second.GetRequiredService<IQueryService>().GetAsync(1);
        await first.GetRequiredService<IQueryService>().GetAsync(1);
        Assert.Equal(2, work.Calls);
    }

    [Fact]
    public async Task ImplementationAttribute_Works_AndNestedAsyncResultIsRejected()
    {
        var work = new QueryWork();
        using var provider = Build(new TestCache(), new TestLocks(), work);
        var service = provider.GetRequiredService<IQueryService>();
        await service.ImplementationAsync();
        await service.ImplementationAsync();
        Assert.Equal(1, work.Calls);
        await Assert.ThrowsAsync<NotSupportedException>(() => service.NestedAsync());
    }
}

public interface IQueryService
{
    [CheckAccess]
    [Cacheable("detail", KeyParameterIndex = 0)]
    Task<string?> GetAsync(int id, CancellationToken cancellationToken = default);
    [CacheEvict("detail", KeyParameterIndex = 0)]
    Task UpdateAsync(int id);
    [Cacheable("search")]
    Task<string?> SearchAsync(Dictionary<string, int> input, CancellationToken cancellationToken = default);
    [Cacheable("search")]
    Task<string?> OtherSearchAsync(Dictionary<string, int> input);
    [Cacheable("invalid")]
    string Invalid();
    [Cacheable("zero")]
    Task<int> ZeroAsync();
    Task<string?> ImplementationAsync();
    [Cacheable("nested")]
    Task<ValueTask<int>> NestedAsync();
}

public class QueryWork
{
    public int Calls;
    public bool Denied;
    public Func<int, CancellationToken, Task<string?>> Handler = (id, _) => Task.FromResult(id == 0 ? null : id.ToString());
    public Task<string?> Run(int id, CancellationToken token)
    {
        Interlocked.Increment(ref Calls);
        return Handler(id, token);
    }
}

public class QueryService(QueryWork work) : IQueryService
{
    public Task<string?> GetAsync(int id, CancellationToken cancellationToken = default) => work.Run(id, cancellationToken);
    public Task UpdateAsync(int id) => Task.CompletedTask;
    public Task<string?> SearchAsync(Dictionary<string, int> input, CancellationToken cancellationToken = default) => work.Run(1, cancellationToken);
    public Task<string?> OtherSearchAsync(Dictionary<string, int> input) => work.Run(1, default);
    public string Invalid() => "invalid";
    public Task<int> ZeroAsync() { Interlocked.Increment(ref work.Calls); return Task.FromResult(0); }
    [Cacheable("implementation")]
    public Task<string?> ImplementationAsync() => work.Run(1, default);
    public Task<ValueTask<int>> NestedAsync() => Task.FromResult(ValueTask.FromResult(0));
}

public class CheckAccessAttribute : AopAttribute
{
    public override Task Invoke(AspectContext context, AspectDelegate next)
    {
        if (context.ServiceProvider.GetRequiredService<QueryWork>().Denied) throw new UnauthorizedAccessException();
        return next(context);
    }
}

internal sealed class TenantScope(string tenant) : IMethodCacheScopeContributor
{
    public void Contribute(MethodCacheScope scope) => scope.Dimensions["tenant"] = tenant;
}

internal sealed class TestTransactions : ITransactionCallbackRegistry
{
    public bool HasActiveTransaction { get; set; }
    private readonly List<Func<Task>> _callbacks = [];
    public Task RegisterAfterCommitAsync(Func<Task> callback)
    {
        if (!HasActiveTransaction) return callback();
        _callbacks.Add(callback);
        return Task.CompletedTask;
    }
    public async Task CommitAsync()
    {
        HasActiveTransaction = false;
        foreach (var callback in _callbacks) await callback();
        _callbacks.Clear();
    }
    public void Rollback() { HasActiveTransaction = false; _callbacks.Clear(); }
}

internal sealed class TestCache : ICache
{
    public readonly ConcurrentDictionary<string, string> Data = new();
    public bool FailRead;
    public bool FailWrite;
    public TimeSpan? LastExpiry;
    public Task<T> GetAsync<T>(string key)
    {
        if (FailRead) throw new IOException("cache unavailable");
        return Task.FromResult(Data.TryGetValue(key, out var json) ? JsonConvert.DeserializeObject<T>(json)! : default!);
    }
    public Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        if (FailWrite) throw new IOException("cache unavailable");
        LastExpiry = expiry;
        Data[key] = JsonConvert.SerializeObject(value);
        return Task.FromResult(true);
    }
    public Task<bool> RemoveAsync(string key) => Task.FromResult(Data.TryRemove(key, out _));
    public Task<bool> ExistsAsync(string key) => Task.FromResult(Data.ContainsKey(key));
    public Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null) => throw new NotSupportedException();
}

internal sealed class TestLocks : ILockable
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _gates = new();
    public readonly TaskCompletionSource Contended = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public bool BlockAll;
    public bool Fail;
    public bool LostOwnership;
    public Action<string>? OnAcquired;
    public int Held;
    public async Task<ILockHandler> LockAsync(string lockKey, DistributedLockAcquisitionOptions options, CancellationToken cancellationToken = default)
    {
        if (Fail) throw new IOException("lock unavailable");
        var gate = _gates.GetOrAdd(lockKey, _ => new SemaphoreSlim(1));
        var acquired = !BlockAll && await gate.WaitAsync(0, cancellationToken);
        if (acquired) { Interlocked.Increment(ref Held); OnAcquired?.Invoke(lockKey); }
        else Contended.TrySetResult();
        return new Handle(this, gate, acquired);
    }
    private sealed class Handle(TestLocks owner, SemaphoreSlim gate, bool acquired) : ILockHandler
    {
        public bool Acquired => acquired && !owner.LostOwnership;
        public ValueTask DisposeAsync()
        {
            if (acquired) { Interlocked.Decrement(ref owner.Held); gate.Release(); }
            return ValueTask.CompletedTask;
        }
    }
}
