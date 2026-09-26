using Kurisu.AspNetCore.Abstractions.DataAccess.Core.Context;
using System.Collections.Concurrent;
using System.Globalization;
using AspectCore.DynamicProxy;
using AspectCore.Extensions.DependencyInjection;
using Kurisu.AspNetCore.Abstractions.Cache;
using Kurisu.AspNetCore.Abstractions.Authentication;
using Kurisu.AspNetCore.Abstractions.Cache.Aop;
using Kurisu.AspNetCore.Abstractions.DataAccess.Contract.Page;
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
        services.AddSingleton<IDbTenantAccessor>(new TestTenantAccessor(tenant));
        if (transactions != null) services.AddSingleton<ITransactionCallbackRegistry>(transactions);
        services.AddMethodCaching(o =>
        {
            var policy = o.Policies["Default"];
            policy.LockPollInterval = TimeSpan.FromMilliseconds(5);
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
        var expiry = id == 0 ? TimeSpan.FromSeconds(30) : TimeSpan.FromMinutes(30);
        Assert.NotNull(cache.LastExpiry);
        Assert.InRange(cache.LastExpiry.Value, TimeSpan.FromTicks(expiry.Ticks * 9 / 10), expiry);
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
        using var provider = Build(new TestCache(), locks, work);
        var service = provider.GetRequiredService<IQueryService>();
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetAsync(1));
        Assert.Equal(1, work.Calls);
        Assert.Equal(0, locks.Held);
        work.Handler = (_, _) => Task.FromResult<string?>("recovered");
        Assert.Equal("recovered", await service.GetAsync(1));
        Assert.Equal(2, work.Calls);
    }

    [Fact]
    public async Task LockTimeout_DoesNotLoad_AndReleasesResources()
    {
        var cache = new TestCache();
        var locks = new TestLocks { BlockAll = true };
        var work = new QueryWork();
        using var provider = Build(cache, locks, work, p =>
        {
            p.LockWaitTimeout = TimeSpan.FromMilliseconds(40);
        });
        var service = provider.GetRequiredService<IQueryService>();
        await Assert.ThrowsAsync<TimeoutException>(() => service.GetAsync(1));
        Assert.Equal(0, work.Calls);
        Assert.Empty(cache.Data);
        Assert.Equal(0, locks.Held);
        locks.BlockAll = false;
        Assert.Equal("1", await service.GetAsync(1));
        Assert.Equal(1, work.Calls);
        Assert.Equal(0, locks.Held);
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
        });
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => provider.GetRequiredService<IQueryService>().GetAsync(1));
        Assert.Equal(1, work.Calls);
        Assert.Empty(cache.Data);
        Assert.Equal(0, locks.Held);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task CacheReadFailure_AtAnyRead_DoesNotLoad_AndReleasesResources(int failingRead)
    {
        var cache = new TestCache { FailOnRead = failingRead };
        var locks = new TestLocks();
        var work = new QueryWork();
        using var provider = Build(cache, locks, work);
        var service = provider.GetRequiredService<IQueryService>();
        await Assert.ThrowsAsync<IOException>(() => service.GetAsync(1));
        Assert.Equal(0, work.Calls);
        Assert.Empty(cache.Data);
        Assert.Equal(0, locks.Held);
        cache.FailOnRead = 0;
        Assert.Equal("1", await service.GetAsync(1));
        Assert.Equal(1, work.Calls);
        Assert.Equal(0, locks.Held);
    }

    [Fact]
    public async Task CacheWriteFailure_ReturnsResult_WithoutRepeatingQuery()
    {
        var cache = new TestCache { FailWrite = true };
        var locks = new TestLocks();
        var work = new QueryWork();
        using var provider = Build(cache, locks, work);
        Assert.Equal("2", await provider.GetRequiredService<IQueryService>().GetAsync(2));
        Assert.Equal(1, work.Calls);
        Assert.Empty(cache.Data);
        Assert.Equal(0, locks.Held);
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
        Assert.Equal(0, locks.Held);
    }

    [Fact]
    public async Task LockProviderFailure_DoesNotLoad_AndReleasesResources()
    {
        var cache = new TestCache();
        var locks = new TestLocks { Fail = true };
        var work = new QueryWork();
        using var provider = Build(cache, locks, work);
        var service = provider.GetRequiredService<IQueryService>();
        await Assert.ThrowsAsync<IOException>(() => service.GetAsync(1));
        Assert.Equal(0, work.Calls);
        Assert.Empty(cache.Data);
        Assert.Equal(0, locks.Held);
        locks.Fail = false;
        Assert.Equal("1", await service.GetAsync(1));
        Assert.Equal(1, work.Calls);
        Assert.Equal(0, locks.Held);
    }

    [Fact]
    public async Task SameKeyRecursiveQuery_TimesOut_AndReleasesLocks()
    {
        var work = new QueryWork();
        var locks = new TestLocks();
        using var provider = Build(new TestCache(), locks, work, p => p.LockWaitTimeout = TimeSpan.FromMilliseconds(40));
        var service = provider.GetRequiredService<IQueryService>();
        work.Handler = (id, token) => service.GetAsync(id, token);
        var error = await Assert.ThrowsAsync<TimeoutException>(() => service.GetAsync(1).WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Equal("等待进程内缓存加载锁超时。", error.Message);
        Assert.Equal(1, work.Calls);
        Assert.Equal(0, locks.Held);
        work.Handler = (_, _) => Task.FromResult<string?>("recovered");
        Assert.Equal("recovered", await service.GetAsync(1));
        Assert.Equal(2, work.Calls);
        Assert.Equal(0, locks.Held);
    }

    [Fact]
    public async Task NullUsesFixedExpiry_AndZeroIsStillAHit()
    {
        var cache = new TestCache();
        var work = new QueryWork();
        using var provider = Build(cache, new TestLocks(), work, p => p.Expiry = TimeSpan.FromHours(1));
        var service = provider.GetRequiredService<IQueryService>();
        Assert.Null(await service.GetAsync(0));
        Assert.Null(await service.GetAsync(0));
        Assert.Equal(1, work.Calls);
        Assert.NotNull(cache.LastExpiry);
        Assert.InRange(cache.LastExpiry.Value, TimeSpan.FromSeconds(27), TimeSpan.FromSeconds(30));
        Assert.Equal(0, await service.ZeroAsync());
        Assert.Equal(0, await service.ZeroAsync());
        Assert.Equal(2, work.Calls);
        Assert.InRange(cache.LastExpiry.Value, TimeSpan.FromMinutes(54), TimeSpan.FromHours(1));
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
    public async Task DifferentUsersAndRoles_ShareCachedResult_AndEvictTheSameKey()
    {
        var cache = new TestCache();
        var locks = new TestLocks();
        var work = new QueryWork();
        var firstUser = new Mock<ICurrentUser>();
        firstUser.Setup(x => x.GetUserId<string>()).Returns("first");
        firstUser.Setup(x => x.GetRoles()).Returns(["admin"]);
        var secondUser = new Mock<ICurrentUser>();
        secondUser.Setup(x => x.GetUserId<string>()).Returns("second");
        secondUser.Setup(x => x.GetRoles()).Returns(["reader"]);
        using var first = Build(cache, locks, work, user: firstUser.Object);
        using var second = Build(cache, locks, work, user: secondUser.Object);
        await first.GetRequiredService<IQueryService>().GetAsync(1);
        await second.GetRequiredService<IQueryService>().GetAsync(1);
        await first.GetRequiredService<IQueryService>().GetAsync(1);
        Assert.Equal(1, work.Calls);
        await second.GetRequiredService<IQueryService>().UpdateAsync(1);
        await first.GetRequiredService<IQueryService>().GetAsync(1);
        Assert.Equal(2, work.Calls);
    }

    [Theory]
    [InlineData("database-tenant")]
    [InlineData(null)]
    public async Task DatabaseTenant_IsAuthoritativeEvenWhenNull(string? databaseTenant)
    {
        var cache = new TestCache();
        var locks = new TestLocks();
        var work = new QueryWork();
        var firstUser = new Mock<ICurrentUser>();
        firstUser.Setup(user => user.GetTenantId()).Returns("identity-a");
        var secondUser = new Mock<ICurrentUser>();
        secondUser.Setup(user => user.GetTenantId()).Returns("identity-b");
        using var first = Build(cache, locks, work, tenant: databaseTenant!, user: firstUser.Object);
        using var second = Build(cache, locks, work, tenant: databaseTenant!, user: secondUser.Object);
        await first.GetRequiredService<IQueryService>().GetAsync(1);
        await second.GetRequiredService<IQueryService>().GetAsync(1);
        Assert.Equal(1, work.Calls);
        await second.GetRequiredService<IQueryService>().UpdateAsync(1);
        await first.GetRequiredService<IQueryService>().GetAsync(1);
        Assert.Equal(2, work.Calls);
    }

    [Fact]
    public async Task CultureChanges_ReuseCachedResult_AndEvictTheSameKey()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUICulture = CultureInfo.CurrentUICulture;
        var work = new QueryWork();
        using var provider = Build(new TestCache(), new TestLocks(), work);
        var service = provider.GetRequiredService<IQueryService>();
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("zh-CN");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("zh-CN");
            await service.GetAsync(1);
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            await service.GetAsync(1);
            Assert.Equal(1, work.Calls);
            await service.UpdateAsync(1);
            await service.GetAsync(1);
            Assert.Equal(2, work.Calls);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUICulture;
        }
    }

    [Fact]
    public async Task ImplementationAttribute_CachesResult()
    {
        var work = new QueryWork();
        using var provider = Build(new TestCache(), new TestLocks(), work);
        var service = provider.GetRequiredService<IQueryService>();
        await service.ImplementationAsync();
        await service.ImplementationAsync();
        Assert.Equal(1, work.Calls);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task PaginationResult_IsRejectedBeforeCacheAccessOrBusinessCall(int kind)
    {
        var cache = new Mock<ICache>(MockBehavior.Strict);
        var locks = new Mock<ILockable>(MockBehavior.Strict);
        var work = new QueryWork();
        using var provider = Build(cache.Object, locks.Object, work);
        var service = provider.GetRequiredService<IQueryService>();

        var error = await Assert.ThrowsAsync<NotSupportedException>(() => kind switch
        {
            0 => (Task)service.PageAsync(),
            1 => service.UntypedPageAsync(),
            _ => service.DerivedPageAsync()
        });

        Assert.Contains("Pagination<T>", error.Message);
        Assert.Equal(0, work.Calls);
        cache.VerifyNoOtherCalls();
        locks.VerifyNoOtherCalls();
    }
    [Fact]
    public async Task ExpressionEviction_UsesDtoIdBeforeSaveMutation()
    {
        var cache = new TestCache();
        var tx = new TestTransactions();
        using var provider = Build(cache, new TestLocks(), new QueryWork(), transactions: tx);
        var service = provider.GetRequiredService<IQueryService>();
        await service.GetAsync(1);
        tx.HasActiveTransaction = true;
        var input = new SaveInput { Id = 1 };
        await service.SaveAsync(input);
        Assert.Equal(101, input.Id);
        Assert.Single(cache.Data);
        Assert.Equal(1, tx.Registrations);
        await tx.CommitAsync();
        Assert.Empty(cache.Data);
    }

    [Fact]
    public async Task ConditionalAndEmptyBatchSave_DoNotRegisterCallbacks()
    {
        var tx = new TestTransactions { HasActiveTransaction = true };
        using var provider = Build(new TestCache(), new TestLocks(), new QueryWork(), transactions: tx);
        var service = provider.GetRequiredService<IQueryService>();
        var input = new SaveInput();
        await service.SaveAsync(input);
        Assert.Equal(100, input.Id);
        await service.SaveBatchAsync([new SaveInput(), new SaveInput()]);
        Assert.Equal(0, tx.Registrations);
    }

    [Fact]
    public async Task BatchSave_CapturesDistinctKeysAndRegistersOneCommitCallback()
    {
        var cache = new TestCache();
        var tx = new TestTransactions();
        using var provider = Build(cache, new TestLocks(), new QueryWork(), transactions: tx);
        var service = provider.GetRequiredService<IQueryService>();
        await service.GetAsync(1);
        await service.GetAsync(2);
        await service.GetAsync(3);
        tx.HasActiveTransaction = true;
        await service.SaveBatchAsync([new() { Id = 1 }, new() { Id = 1 }, new() { Id = 2 }, new()]);
        Assert.Equal(1, tx.Registrations);
        Assert.Equal(3, cache.Data.Count);
        await tx.CommitAsync();
        Assert.Single(cache.Data);
        Assert.Equal(2, cache.Removes);
    }

    [Fact]
    public async Task FailedSave_DoesNotRegisterEviction()
    {
        var tx = new TestTransactions();
        var cache = new TestCache();
        using var provider = Build(cache, new TestLocks(), new QueryWork(), transactions: tx);
        var service = provider.GetRequiredService<IQueryService>();
        await service.GetAsync(1);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SaveAsync(new() { Id = 1, Fail = true }));
        Assert.Single(cache.Data);
        Assert.Equal(0, tx.Registrations);
    }

    [Fact]
    public async Task InvalidExpression_FailsBeforeBusinessOrCacheAccess()
    {
        var cache = new Mock<ICache>(MockBehavior.Strict);
        var work = new QueryWork();
        using var provider = Build(cache.Object, new TestLocks(), work);
        await Assert.ThrowsAsync<Microsoft.CodeAnalysis.Scripting.CompilationErrorException>(() =>
            provider.GetRequiredService<IQueryService>().BadExpressionAsync(1));
        Assert.Equal(0, work.Calls);
        cache.VerifyNoOtherCalls();
    }

}

public interface IQueryService
{
    [CheckAccess]
    [Cacheable("detail", Key = "id")]
    Task<string?> GetAsync(int id, CancellationToken cancellationToken = default);
    [CacheEvict("detail", Key = "id")]
    Task UpdateAsync(int id);
    [CacheEvict("detail", Key = "input.Id", Condition = "input.Id > 0")]
    Task SaveAsync(SaveInput input);
    [CacheEvict("detail", Keys = "inputs.Where(x => x.Id > 0).Select(x => x.Id)")]
    Task SaveBatchAsync(List<SaveInput> inputs);
    [Cacheable("detail", Key = "id.Unknown")]
    Task<string?> BadExpressionAsync(int id);
    [Cacheable("search")]
    Task<string?> SearchAsync(Dictionary<string, int> input, CancellationToken cancellationToken = default);
    [Cacheable("search")]
    Task<string?> OtherSearchAsync(Dictionary<string, int> input);
    [Cacheable("invalid")]
    string Invalid();
    [Cacheable("zero")]
    Task<int> ZeroAsync();
    Task<string?> ImplementationAsync();
    [Cacheable("page")]
    Task<Pagination<string>> PageAsync();
    [Cacheable("page")]
    Task<Pagination> UntypedPageAsync();
    [Cacheable("page")]
    Task<DerivedPagination> DerivedPageAsync();
}

public sealed class SaveInput
{
    public int Id { get; set; }
    public bool Fail { get; set; }
}

public class DerivedPagination : Pagination<string> { }

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
    public Task SaveAsync(SaveInput input)
    {
        if (input.Fail) throw new InvalidOperationException("save failed");
        input.Id += 100;
        return Task.CompletedTask;
    }
    public Task SaveBatchAsync(List<SaveInput> inputs)
    {
        foreach (var input in inputs) input.Id += 100;
        inputs.Clear();
        return Task.CompletedTask;
    }
    public Task<string?> BadExpressionAsync(int id) => work.Run(id, default);
    public Task UpdateAsync(int id) => Task.CompletedTask;
    public Task<string?> SearchAsync(Dictionary<string, int> input, CancellationToken cancellationToken = default) => work.Run(1, cancellationToken);
    public Task<string?> OtherSearchAsync(Dictionary<string, int> input) => work.Run(1, default);
    public string Invalid() => "invalid";
    public Task<int> ZeroAsync() { Interlocked.Increment(ref work.Calls); return Task.FromResult(0); }
    [Cacheable("implementation")]
    public Task<string?> ImplementationAsync() => work.Run(1, default);
    public Task<Pagination<string>> PageAsync() { Interlocked.Increment(ref work.Calls); return Task.FromResult(new Pagination<string>()); }
    public Task<Pagination> UntypedPageAsync() { Interlocked.Increment(ref work.Calls); return Task.FromResult(new Pagination()); }
    public Task<DerivedPagination> DerivedPageAsync() { Interlocked.Increment(ref work.Calls); return Task.FromResult(new DerivedPagination()); }
}

public class CheckAccessAttribute : AopAttribute
{
    public override Task Invoke(AspectContext context, AspectDelegate next)
    {
        if (context.ServiceProvider.GetRequiredService<QueryWork>().Denied) throw new UnauthorizedAccessException();
        return next(context);
    }
}

internal sealed class TestTenantAccessor(string tenant) : IDbTenantAccessor
{
    public string GetTenantId() => tenant;
    public IReadOnlyList<string> GetAccessibleTenantIds() => tenant == null ? [] : [tenant];
}

internal sealed class TestTransactions : ITransactionCallbackRegistry
{
    public bool HasActiveTransaction { get; set; }
    public int Registrations;
    private readonly List<Func<Task>> _callbacks = [];
    public Task RegisterAfterCommitAsync(Func<Task> callback)
    {
        Registrations++;
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
    public int FailOnRead;
    private int _readCount;
    public bool FailWrite;
    public TimeSpan? LastExpiry;
    public Task<T> GetAsync<T>(string key)
    {
        if (Interlocked.Increment(ref _readCount) == FailOnRead || FailRead) throw new IOException("cache unavailable");
        return Task.FromResult(Data.TryGetValue(key, out var json) ? JsonConvert.DeserializeObject<T>(json)! : default!);
    }
    public Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        if (FailWrite) throw new IOException("cache unavailable");
        LastExpiry = expiry;
        Data[key] = JsonConvert.SerializeObject(value);
        return Task.FromResult(true);
    }
    public int Removes;
    public Task<bool> RemoveAsync(string key) { Removes++; return Task.FromResult(Data.TryRemove(key, out _)); }
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
