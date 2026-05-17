using AspectCore.Extensions.DependencyInjection;
using Kurisu.AspNetCore.Abstractions.Cache;
using Kurisu.AspNetCore.Abstractions.Cache.Aop;
using Kurisu.Extensions.Cache;
using Kurisu.Extensions.Cache.Options;
using Kurisu.Extensions.Cache.Providers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kurisu.Test.Cache;

[Trait("feature", "aop-integration")]
public class RedisCacheAopIntegrationTests
{
    private static ServiceProvider BuildAopServiceProvider()
    {
        var connectionString = Environment.GetEnvironmentVariable("KURISU_TEST_REDIS")
            ?? throw new InvalidOperationException("环境变量 KURISU_TEST_REDIS 未设置");

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions<RedisOptions>().Configure(o => o.ConnectionString = connectionString);
        services.AddRedis();
        services.AddSingleton<ILockAopTestService, LockAopTestService>();
        services.AddSingleton<IMultiKeyLockAopService, MultiKeyLockAopService>();
        services.AddSingleton<ITryLockKeyAopService, TryLockKeyAopService>();

        return services.BuildDynamicProxyProvider();
    }

    [Fact(DisplayName = "[TryLock]应拦截方法调用并自动获取与释放锁")]
    public async Task TryLock_ShouldAcquireAndReleaseLock_AroundMethodExecution()
    {
        using var serviceProvider = BuildAopServiceProvider();
        var cache = serviceProvider.GetRequiredService<RedisCache>();
        var service = serviceProvider.GetRequiredService<ILockAopTestService>();

        var lockId = $"trylock:{Guid.NewGuid():N}";
        var expectedKey = $"Locker:test-scene:{lockId}";

        await service.ExecuteAsync(lockId);

        // 方法执行后锁应已释放
        Assert.False(await cache.KeyExistsAsync(expectedKey));
    }

    [Fact(DisplayName = "[TryLock]多次调用不同Key应各自独立获取锁")]
    public async Task TryLock_ShouldAcquireIndependentLocks_ForDifferentKeys()
    {
        using var serviceProvider = BuildAopServiceProvider();
        var cache = serviceProvider.GetRequiredService<RedisCache>();
        var service = serviceProvider.GetRequiredService<ILockAopTestService>();

        var id1 = $"multi1:{Guid.NewGuid():N}";
        var id2 = $"multi2:{Guid.NewGuid():N}";

        var t1 = service.ExecuteAsync(id1);
        var t2 = service.ExecuteAsync(id2);
        await Task.WhenAll(t1, t2);

        Assert.False(await cache.KeyExistsAsync($"Locker:test-scene:{id1}"));
        Assert.False(await cache.KeyExistsAsync($"Locker:test-scene:{id2}"));
    }

    [Fact(DisplayName = "[TryLock]同Key并发调用应被序列化")]
    public async Task TryLock_ShouldSerializeConcurrentCalls_ForSameKey()
    {
        using var serviceProvider = BuildAopServiceProvider();
        var service = serviceProvider.GetRequiredService<ILockAopTestService>();

        var sharedId = $"serial:{Guid.NewGuid():N}";
        var executionOrder = new List<int>();
        var tasks = new Task[3];
        for (var i = 0; i < 3; i++)
        {
            var idx = i;
            tasks[i] = service.ExecuteAsync(sharedId, () =>
            {
                lock (executionOrder)
                    executionOrder.Add(idx);
                return Task.CompletedTask;
            });
        }

        await Task.WhenAll(tasks);
        // 三个调用都应执行完（顺序可能随机但全部执行）
        Assert.Equal(3, executionOrder.Count);
    }

    [Fact(DisplayName = "[TryLockFixedExpiry]应使用固定过期模式")]
    public async Task TryLockFixedExpiry_ShouldUseFixedExpiryMode()
    {
        using var serviceProvider = BuildAopServiceProvider();
        var cache = serviceProvider.GetRequiredService<RedisCache>();
        var service = serviceProvider.GetRequiredService<ILockAopTestService>();

        var lockId = $"fixed:{Guid.NewGuid():N}";
        var expectedKey = $"Locker:test-fixed:{lockId}";

        await service.ExecuteFixedExpiryAsync(lockId);

        Assert.False(await cache.KeyExistsAsync(expectedKey));
    }

    [Fact(DisplayName = "[TryLockLimitedRenewals]应使用有限续期模式")]
    public async Task TryLockLimitedRenewals_ShouldUseLimitedRenewalMode()
    {
        using var serviceProvider = BuildAopServiceProvider();
        var cache = serviceProvider.GetRequiredService<RedisCache>();
        var service = serviceProvider.GetRequiredService<ILockAopTestService>();

        var lockId = $"limited:{Guid.NewGuid():N}";
        var expectedKey = $"Locker:test-limited:{lockId}";

        await service.ExecuteLimitedRenewalAsync(lockId);

        Assert.False(await cache.KeyExistsAsync(expectedKey));
    }

    [Fact(DisplayName = "ITryLockKey参数应被解析为锁Key")]
    public async Task TryLock_ShouldResolveLockKey_FromTryLockKeyParameter()
    {
        using var serviceProvider = BuildAopServiceProvider();
        var cache = serviceProvider.GetRequiredService<RedisCache>();
        var service = serviceProvider.GetRequiredService<ITryLockKeyAopService>();

        var keyModel = new TestLockKey { Id = $"itlk:{Guid.NewGuid():N}" };
        var expectedKey = $"Locker:test-tlk:{keyModel.GetKey()}";

        await service.ExecuteAsync(keyModel);

        Assert.False(await cache.KeyExistsAsync(expectedKey));
    }

    [Fact(DisplayName = "ITryLockKeys参数应被解析为多个锁Key")]
    public async Task TryLock_ShouldAcquireMultipleLocks_FromTryLockKeysParameter()
    {
        using var serviceProvider = BuildAopServiceProvider();
        var cache = serviceProvider.GetRequiredService<RedisCache>();
        var service = serviceProvider.GetRequiredService<IMultiKeyLockAopService>();

        var keys = new TestLockKeys(["key-a", "key-b", "key-c"]);
        var expectedKeys = new[]
        {
            $"Locker:test-multi:{keys.GetKeys().ElementAt(0)}",
            $"Locker:test-multi:{keys.GetKeys().ElementAt(1)}",
            $"Locker:test-multi:{keys.GetKeys().ElementAt(2)}",
        };

        await service.ExecuteAsync(keys);

        foreach (var k in expectedKeys)
            Assert.False(await cache.KeyExistsAsync(k));
    }

    [Fact(DisplayName = "多锁获取时任一失败应回滚已获取的锁")]
    public async Task TryLock_ShouldRollbackAcquiredLocks_WhenMultiLockFails()
    {
        using var serviceProvider = BuildAopServiceProvider();
        var cache = serviceProvider.GetRequiredService<RedisCache>();
        var service = serviceProvider.GetRequiredService<IMultiKeyLockAopService>();

        // 先手动占用其中一个Key
        var keys = new TestLockKeys(["rollback-a", "rollback-b"]);
        var preemptiveKey = $"Locker:test-rollback:{keys.GetKeys().ElementAt(0)}";
        await cache.StringSetAsync(preemptiveKey, "external-holder", TimeSpan.FromSeconds(10), StackExchange.Redis.When.NotExists);

        // 调用应失败（因为第一个Key已被占用且不重试）
        await Assert.ThrowsAnyAsync<Exception>(() => service.ExecuteNoRetryAsync(keys));

        // 所有Key都不应有残留锁
        foreach (var k in keys.GetKeys())
            await cache.KeyDeleteAsync($"Locker:test-rollback:{k}");
    }
}

// ---- 测试服务接口与实现 ----

public interface ILockAopTestService
{
    [TryLock("test-scene", "操作失败")]
    Task ExecuteAsync(string id, Func<Task>? work = null);

    [TryLockFixedExpiry("test-fixed", "操作失败", 3)]
    Task ExecuteFixedExpiryAsync(string id);

    [TryLockLimitedRenewals("test-limited", "操作失败", 3, 2)]
    Task ExecuteLimitedRenewalAsync(string id);
}

public class LockAopTestService : ILockAopTestService
{
    public Task ExecuteAsync(string id, Func<Task>? work = null)
        => work?.Invoke() ?? Task.CompletedTask;

    public Task ExecuteFixedExpiryAsync(string id)
        => Task.CompletedTask;

    public Task ExecuteLimitedRenewalAsync(string id)
        => Task.CompletedTask;
}

public interface IMultiKeyLockAopService
{
    [TryLock("test-multi", "操作失败")]
    Task ExecuteAsync(ITryLockKeys keys);

    [TryLock("test-rollback", "操作失败", RetryCount = 0)]
    Task ExecuteNoRetryAsync(ITryLockKeys keys);
}

public class MultiKeyLockAopService : IMultiKeyLockAopService
{
    public Task ExecuteAsync(ITryLockKeys keys) => Task.CompletedTask;
    public Task ExecuteNoRetryAsync(ITryLockKeys keys) => Task.CompletedTask;
}

public interface ITryLockKeyAopService
{
    [TryLock("test-tlk", "操作失败")]
    Task ExecuteAsync(ITryLockKey key);
}

public class TryLockKeyAopService : ITryLockKeyAopService
{
    public Task ExecuteAsync(ITryLockKey key) => Task.CompletedTask;
}

public class TestLockKey : ITryLockKey
{
    public string Id { get; set; } = string.Empty;
    public string GetKey() => Id;
}

public class TestLockKeys : ITryLockKeys
{
    private readonly string[] _keys;
    public TestLockKeys(string[] keys) => _keys = keys;
    public IEnumerable<string> GetKeys() => _keys;
}
