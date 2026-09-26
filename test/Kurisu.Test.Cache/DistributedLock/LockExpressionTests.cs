using AspectCore.Extensions.DependencyInjection;
using Kurisu.AspNetCore.Abstractions.DistributedLock;
using Kurisu.AspNetCore.Abstractions.DistributedLock.Aop;
using Kurisu.Expressions;
using Kurisu.Test.Cache.MethodCaching;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kurisu.Test.Cache.DistributedLock;

public class LockExpressionTests
{
    private static ServiceProvider Build(TestLocks locks)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILockable>(locks);
        services.AddSingleton<ExpressionCompiler>();
        services.AddSingleton<MethodExpressionEvaluator>();
        services.AddTransient<IExpressionLockService, ExpressionLockService>();
        return services.BuildDynamicProxyProvider();
    }

    [Fact]
    public async Task DtoKeyAndCollection_AreEvaluatedWithoutInterfaces()
    {
        var acquired = new List<string>();
        var locks = new TestLocks { OnAcquired = acquired.Add };
        using var provider = Build(locks);
        var service = provider.GetRequiredService<IExpressionLockService>();
        await service.SingleAsync(new() { Id = 7 }, () => { Assert.Equal(1, locks.Held); return Task.CompletedTask; });
        Assert.Equal(new[] { "Locker:expression:7" }, acquired);
        Assert.Equal(0, locks.Held);
        acquired.Clear();
        await service.BatchAsync([new() { Id = 2 }, new() { Id = 1 }, new() { Id = 2 }]);
        Assert.Equal(new[] { "Locker:expression:1", "Locker:expression:2" }, acquired);
        Assert.Equal(0, locks.Held);
    }

    [Fact]
    public async Task BusinessFailure_ReleasesExpressionLock()
    {
        var locks = new TestLocks();
        using var provider = Build(locks);
        var service = provider.GetRequiredService<IExpressionLockService>();
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SingleAsync(new() { Id = 7 },
            () => throw new InvalidOperationException("failed")));
        Assert.Equal(0, locks.Held);
        await service.SingleAsync(new() { Id = 7 }, () => Task.CompletedTask);
        Assert.Equal(0, locks.Held);
    }

    [Fact]
    public async Task PartialAcquisitionFailure_ReleasesEarlierLocks()
    {
        var locks = new TestLocks();
        using var provider = Build(locks);
        await using var occupied = await locks.LockAsync("Locker:expression:2", new DistributedLockAcquisitionOptions());
        await Assert.ThrowsAnyAsync<Exception>(() => provider.GetRequiredService<IExpressionLockService>()
            .BatchAsync([new() { Id = 1 }, new() { Id = 2 }]));
        Assert.Equal(1, locks.Held);
    }

    [Fact]
    public async Task EmptyKeys_FailWithoutTakingLocks()
    {
        var locks = new TestLocks();
        using var provider = Build(locks);
        await Assert.ThrowsAsync<ArgumentException>(() => provider.GetRequiredService<IExpressionLockService>().BatchAsync([]));
        Assert.Equal(0, locks.Held);
    }
}

public interface IExpressionLockService
{
    [TryLock("expression", "locked", Key = "input.Id")]
    Task SingleAsync(SaveInput input, Func<Task> work);
    [TryLock("expression", "locked", Keys = "inputs.Select(x => x.Id)")]
    Task BatchAsync(List<SaveInput> inputs);
}

public class ExpressionLockService : IExpressionLockService
{
    public Task SingleAsync(SaveInput input, Func<Task> work) => work();
    public Task BatchAsync(List<SaveInput> inputs) => Task.CompletedTask;
}
