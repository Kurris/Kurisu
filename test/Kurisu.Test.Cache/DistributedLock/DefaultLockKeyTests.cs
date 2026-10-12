using System.Globalization;
using AspectCore.Extensions.DependencyInjection;
using Kurisu.AspNetCore.Abstractions.DistributedLock;
using Kurisu.AspNetCore.Abstractions.DistributedLock.Aop;
using Kurisu.Expressions;
using Kurisu.Extensions.Cache;
using Kurisu.Test.Cache.MethodCaching;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kurisu.Test.Cache.DistributedLock;

public class DefaultLockKeyTests
{
    private static ServiceProvider Build(TestLocks locks, BindingWork work, bool withExpressions = false)
    {
        var services = new ServiceCollection();
        services.AddDistributedLockOptions();
        services.AddSingleton<ILockable>(locks);
        services.AddSingleton(work);
        if (withExpressions)
        {
            services.AddSingleton<ExpressionCompiler>();
            services.AddSingleton<MethodExpressionEvaluator>();
        }
        services.AddTransient<IDefaultLockKeyService, DefaultLockKeyService>();
        return services.BuildDynamicProxyProvider();
    }

    [Fact]
    public async Task PrimitiveFirstParameter_UsesExistingKeyFormat_AndIgnoresOtherArguments()
    {
        var acquired = new List<string>();
        var locks = new TestLocks { OnAcquired = acquired.Add };
        var work = new BindingWork();
        await using var provider = Build(locks, work); // default path needs no expression compiler
        var service = provider.GetRequiredService<IDefaultLockKeyService>();
        await service.StringAsync("7", "description");
        await service.IntAsync(7, 99);
        await service.LongAsync(7L, CancellationToken.None);
        Assert.Equal(Enumerable.Repeat("Locker:default-lock:7", 3), acquired);
        Assert.Equal(3, work.Writes);
        Assert.Equal(0, locks.Held);
    }

    [Fact]
    public async Task NumericKey_UsesInvariantCulture()
    {
        var acquired = new List<string>();
        var locks = new TestLocks { OnAcquired = acquired.Add };
        await using var provider = Build(locks, new BindingWork());
        var original = CultureInfo.CurrentCulture;
        try
        {
            var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            culture.NumberFormat.NegativeSign = "~";
            CultureInfo.CurrentCulture = culture;
            await provider.GetRequiredService<IDefaultLockKeyService>().IntAsync(-7, 99);
            Assert.Equal(new[] { "Locker:default-lock:-7" }, acquired);
        }
        finally { CultureInfo.CurrentCulture = original; }
    }

    [Fact]
    public async Task LegacyInterfaces_ResolveSingleAndSortedDistinctMultipleKeys()
    {
        var acquired = new List<string>();
        var locks = new TestLocks { OnAcquired = acquired.Add };
        var work = new BindingWork();
        await using var provider = Build(locks, work);
        var service = provider.GetRequiredService<IDefaultLockKeyService>();
        await service.DefaultAsync(new LegacySingleKey("7"));
        await service.DefaultAsync(new LegacyMultipleKeys(["2", "1", "2"]));
        Assert.Equal(new[] { "Locker:default-lock:7", "Locker:default-lock:1", "Locker:default-lock:2" }, acquired);
        Assert.Equal(2, work.Writes);
        Assert.Equal(0, locks.Held);
    }

    [Fact]
    public async Task ExplicitExpressions_TakePrecedenceOverLegacyInterface()
    {
        var acquired = new List<string>();
        var locks = new TestLocks { OnAcquired = acquired.Add };
        await using var provider = Build(locks, new BindingWork(), withExpressions: true);
        var service = provider.GetRequiredService<IDefaultLockKeyService>();
        var input = new ThrowingLegacyKey { Id = 7 };
        await service.ExplicitAsync(input);
        await service.ExplicitBatchAsync(input);
        Assert.Equal(new[] { "Locker:default-lock:7", "Locker:default-lock:7", "Locker:default-lock:8" }, acquired);
        Assert.Equal(0, locks.Held);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    public async Task InvalidFirstParameter_FailsBeforeLockOrBusiness(int kind)
    {
        var acquired = new List<string>();
        var locks = new TestLocks { OnAcquired = acquired.Add };
        var work = new BindingWork();
        await using var provider = Build(locks, work);
        var service = provider.GetRequiredService<IDefaultLockKeyService>();
        object? input = kind switch
        {
            0 => null,
            1 => " ",
            2 => new BindingInput { Id = 7 },
            3 => new[] { 1, 2 },
            4 => 7m,
            5 => new LegacySingleKey(null!),
            6 => new LegacyMultipleKeys([]),
            7 => new LegacyMultipleKeys(null!),
            _ => null
        };
        Func<Task> call = kind switch
        {
            8 => service.NoArgsAsync,
            9 => () => service.TokenFirstAsync(CancellationToken.None, 7),
            _ => () => service.DefaultAsync(input!)
        };
        await Assert.ThrowsAsync<ArgumentException>(call);
        Assert.Empty(acquired);
        Assert.Equal(0, work.Writes);
    }

    [Fact]
    public async Task DefaultInterface_BusinessFailureReleasesLock()
    {
        var locks = new TestLocks();
        await using var provider = Build(locks, new BindingWork());
        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.GetRequiredService<IDefaultLockKeyService>()
            .FailAsync(new LegacySingleKey("7")));
        Assert.Equal(0, locks.Held);
    }
}

public sealed class LegacySingleKey(string key) : ITryLockKey
{
    public string GetKey() => key;
}

public sealed class LegacyMultipleKeys(IEnumerable<string> keys) : ITryLockKeys
{
    public IEnumerable<string> GetKeys() => keys;
}

public sealed class ThrowingLegacyKey : ITryLockKey
{
    public int Id { get; set; }
    public string GetKey() => throw new InvalidOperationException("Explicit expressions must take precedence.");
}

public interface IDefaultLockKeyService
{
    [TryLock("default-lock", "locked")] Task StringAsync(string id, string description);
    [TryLock("default-lock", "locked")] Task IntAsync(int id, int other);
    [TryLock("default-lock", "locked")] Task LongAsync(long id, CancellationToken token);
    [TryLock("default-lock", "locked")] Task DefaultAsync(object input);
    [TryLock("default-lock", "locked", Key = "input.Id")] Task ExplicitAsync(ThrowingLegacyKey input);
    [TryLock("default-lock", "locked", Keys = "new[] { input.Id + 1, input.Id }")] Task ExplicitBatchAsync(ThrowingLegacyKey input);
    [TryLock("default-lock", "locked")] Task NoArgsAsync();
    // ReSharper disable once MethodSupportsCancellation
    [TryLock("default-lock", "locked")] Task TokenFirstAsync(CancellationToken token, int id);
    [TryLock("default-lock", "locked")] Task FailAsync(ITryLockKey input);
}

public class DefaultLockKeyService(BindingWork work) : IDefaultLockKeyService
{
    private Task Write() { work.Writes++; return Task.CompletedTask; }
    public Task StringAsync(string id, string description) => Write();
    public Task IntAsync(int id, int other) => Write();
    public Task LongAsync(long id, CancellationToken token) => Write();
    public Task DefaultAsync(object input) => Write();
    public Task ExplicitAsync(ThrowingLegacyKey input) => Write();
    public Task ExplicitBatchAsync(ThrowingLegacyKey input) => Write();
    public Task NoArgsAsync() => Write();
    public Task TokenFirstAsync(CancellationToken token, int id) => Write();
    public Task FailAsync(ITryLockKey input) => throw new InvalidOperationException("business");
}
