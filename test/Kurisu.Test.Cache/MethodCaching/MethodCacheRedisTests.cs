using System.Collections.Concurrent;
using AspectCore.Extensions.DependencyInjection;
using Kurisu.AspNetCore.Abstractions.Cache;
using Kurisu.AspNetCore.Abstractions.DataAccess.Core;
using Kurisu.Extensions.Cache;
using Kurisu.Extensions.Cache.Options;
using Kurisu.Extensions.Cache.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Kurisu.Test.Cache.MethodCaching;

[Trait("feature", "method-cache-redis")]
public class MethodCacheRedisTests
{
    private static ServiceProvider Build(QueryWork work, string prefix, ConcurrentDictionary<string, byte> keys,
        TestTransactions? transactions = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.Configure<RedisOptions>(o => o.ConnectionString = RedisCacheTestSupport.GetConnectionString());
        services.AddRedis();
        services.Replace(ServiceDescriptor.Singleton<ICache>(sp => new TrackedCache(sp.GetRequiredService<RedisCache>(), keys)));
        services.AddSingleton(work);
        if (transactions != null) services.AddSingleton<ITransactionCallbackRegistry>(transactions);
        services.AddMethodCaching(o =>
        {
            o.KeyPrefix = prefix;
            o.Policies["Default"].LockExpiry = TimeSpan.FromMilliseconds(300);
            o.Policies["Default"].LockPollInterval = TimeSpan.FromMilliseconds(25);
            o.Policies["Default"].Expiry = TimeSpan.FromSeconds(30);
        });
        services.AddTransient<IQueryService, QueryService>();
        return services.BuildDynamicProxyProvider();
    }

    [Fact]
    public async Task IndependentRedisProviders_CoalesceLoadBeyondInitialLease()
    {
        var keys = new ConcurrentDictionary<string, byte>();
        var prefix = $"kurisu:test:method-cache:{Guid.NewGuid():N}";
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var work = new QueryWork { Handler = async (_, token) => { entered.TrySetResult(); await Task.Delay(1200, token); return "loaded"; } };
        using var first = Build(work, prefix, keys);
        using var second = Build(work, prefix, keys);
        var firstService = first.GetRequiredService<IQueryService>();
        var secondService = second.GetRequiredService<IQueryService>();
        var redis = first.GetRequiredService<RedisCache>();
        try
        {
            var leader = firstService.GetAsync(1);
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            var followers = Enumerable.Range(0, 10).Select(_ => secondService.GetAsync(1)).ToArray();
            Assert.All(await Task.WhenAll(followers.Append(leader)), value => Assert.Equal("loaded", value));
            Assert.Equal(1, work.Calls);
            foreach (var key in keys.Keys) Assert.False(await redis.ExistsAsync($"{key}:load-lock"));
        }
        finally
        {
            foreach (var key in keys.Keys) await redis.RemoveAsync(key);
        }
    }

    [Fact]
    public async Task AbandonedLease_ExpiresAndAllowsLoad_CancelledWaitDoesNotReleaseOtherOwner()
    {
        var keys = new ConcurrentDictionary<string, byte>();
        var work = new QueryWork();
        using var provider = Build(work, $"kurisu:test:method-cache:{Guid.NewGuid():N}", keys);
        var service = provider.GetRequiredService<IQueryService>();
        var redis = provider.GetRequiredService<RedisCache>();
        await service.GetAsync(1);
        var key = Assert.Single(keys.Keys);
        var lockKey = $"{key}:load-lock";
        try
        {
            await redis.RemoveAsync(key);
            await redis.SetAsync(lockKey, "external-owner", TimeSpan.FromSeconds(10));
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.GetAsync(1, cts.Token));
            Assert.True(await redis.ExistsAsync(lockKey));
            Assert.Equal(1, work.Calls);
            await redis.SetAsync(lockKey, "abandoned-owner", TimeSpan.FromMilliseconds(250));
            Assert.Equal("1", await service.GetAsync(1).WaitAsync(TimeSpan.FromSeconds(5)));
            Assert.Equal(2, work.Calls);
            Assert.False(await redis.ExistsAsync(lockKey));
        }
        finally
        {
            await redis.RemoveAsync(key);
            await redis.RemoveAsync(lockKey);
        }
    }

    [Fact]
    public async Task ConditionalBatchEviction_RollbackKeepsRedisEntries_CommitRemovesCapturedKeys()
    {
        var keys = new ConcurrentDictionary<string, byte>();
        var work = new QueryWork();
        var transactions = new TestTransactions();
        using var provider = Build(work, $"kurisu:test:expressions:{Guid.NewGuid():N}", keys, transactions);
        var service = provider.GetRequiredService<IQueryService>();
        var redis = provider.GetRequiredService<RedisCache>();
        try
        {
            foreach (var id in new[] { 1, 2, 3 }) await service.GetAsync(id);
            transactions.HasActiveTransaction = true;
            await service.SaveAsync(new SaveInput());
            Assert.Equal(0, transactions.Registrations);
            await service.SaveBatchAsync([new() { Id = 1 }, new() { Id = 2 }, new() { Id = 1 }]);
            Assert.Equal(1, transactions.Registrations);
            foreach (var key in keys.Keys) Assert.True(await redis.ExistsAsync(key));
            transactions.Rollback();
            foreach (var id in new[] { 1, 2, 3 }) await service.GetAsync(id);
            Assert.Equal(3, work.Calls);

            transactions.HasActiveTransaction = true;
            await service.SaveBatchAsync([new() { Id = 1 }, new() { Id = 2 }, new()]);
            foreach (var key in keys.Keys) Assert.True(await redis.ExistsAsync(key));
            await transactions.CommitAsync();
            Assert.Equal(1, (await Task.WhenAll(keys.Keys.Select(redis.ExistsAsync))).Count(exists => exists));
            foreach (var id in new[] { 1, 2, 3 }) await service.GetAsync(id);
            Assert.Equal(5, work.Calls);
        }
        finally
        {
            foreach (var key in keys.Keys) await redis.RemoveAsync(key);
        }
    }

    private sealed class TrackedCache(ICache inner, ConcurrentDictionary<string, byte> keys) : ICache
    {
        public Task<T> GetAsync<T>(string key) { keys.TryAdd(key, 0); return inner.GetAsync<T>(key); }
        public Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry = null) => inner.SetAsync(key, value, expiry);
        public Task<bool> RemoveAsync(string key) => inner.RemoveAsync(key);
        public Task<bool> ExistsAsync(string key) => inner.ExistsAsync(key);
        public Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null) => inner.GetOrSetAsync(key, factory, expiry);
    }
}
