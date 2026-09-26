using AspectCore.Extensions.DependencyInjection;
using Kurisu.AspNetCore.Abstractions.Cache;
using Kurisu.AspNetCore.Abstractions.Cache.Aop;
using Kurisu.AspNetCore.Abstractions.DistributedLock;
using Kurisu.AspNetCore.Abstractions.DistributedLock.Aop;
using Kurisu.Extensions.Cache;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.CodeAnalysis.Scripting;
using Moq;
using Xunit;

namespace Kurisu.Test.Cache.MethodCaching;

public class ExpressionBindingTests
{
    private static ServiceProvider Build(ICache cache, ILockable locks, BindingWork work)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(cache);
        services.AddSingleton(locks);
        services.AddSingleton(work);
        services.AddMethodCaching();
        services.AddTransient<IBindingService, BindingService>();
        services.AddTransient<ClassBindingService>();
        return services.BuildDynamicProxyProvider();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ParameterNames_AlwaysFollowServiceMethod(bool implementation)
    {
        var work = new BindingWork();
        var cache = new TestCache();
        using var provider = Build(cache, new TestLocks(), work);
        var service = provider.GetRequiredService<IBindingService>();
        if (implementation)
        {
            Assert.Equal(7, await service.ImplementationAsync(7));
            Assert.Equal(7, await service.ImplementationAsync(7));
            await service.ImplementationEvictAsync(7);
        }
        else
        {
            Assert.Equal(7, await service.InterfaceAsync(7));
            Assert.Equal(7, await service.InterfaceAsync(7));
            await service.InterfaceEvictAsync(7);
        }
        Assert.Equal(1, work.Queries);
        Assert.Empty(cache.Data);
    }

    [Fact]
    public async Task ImplementationLock_UsesInterfaceParameterName()
    {
        var acquired = new List<string>();
        var locks = new TestLocks { OnAcquired = acquired.Add };
        using var provider = Build(new TestCache(), locks, new BindingWork());
        await provider.GetRequiredService<IBindingService>().ImplementationLockAsync(7);
        Assert.Equal(new[] { "Locker:binding:7" }, acquired);
        Assert.Equal(0, locks.Held);
    }

    [Fact]
    public async Task SwappedImplementationParameterNames_DoNotChangeKeyBinding()
    {
        var work = new BindingWork();
        using var provider = Build(new TestCache(), new TestLocks(), work);
        var service = provider.GetRequiredService<IBindingService>();
        Assert.Equal(7, await service.ReorderedAsync(7, 10));
        Assert.Equal(7, await service.ReorderedAsync(7, 20));
        Assert.Equal(1, work.Queries);
    }

    [Fact]
    public async Task ImplementationOnlyParameterName_ProducesCompilationError()
    {
        var work = new BindingWork();
        var cache = new Mock<ICache>(MockBehavior.Strict);
        var locks = new Mock<ILockable>(MockBehavior.Strict);
        using var provider = Build(cache.Object, locks.Object, work);
        await Assert.ThrowsAsync<CompilationErrorException>(() =>
            provider.GetRequiredService<IBindingService>().ImplementationNameAsync(7));
        Assert.Equal(0, work.Queries);
        cache.VerifyNoOtherCalls();
        locks.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ClassProxy_UsesClassServiceParameterNames()
    {
        var work = new BindingWork();
        var cache = new TestCache();
        using var provider = Build(cache, new TestLocks(), work);
        var service = provider.GetRequiredService<ClassBindingService>();
        Assert.Equal(7, await service.GetAsync(7));
        Assert.Equal(7, await service.GetAsync(7));
        Assert.Equal(1, work.Queries);
        await service.EvictAsync(7);
        Assert.Empty(cache.Data);
    }

    [Fact]
    public async Task CompositeKeys_DistinguishArgumentsAndMatchDtoEviction()
    {
        var work = new BindingWork();
        using var provider = Build(new TestCache(), new TestLocks(), work);
        var service = provider.GetRequiredService<IBindingService>();
        await service.CompositeAsync(7, "zh");
        await service.CompositeAsync(7, "en");
        await service.CompositeAsync(7, "zh");
        Assert.Equal(2, work.Queries);
        await service.CompositeEvictAsync(new BindingInput { Id = 7, Locale = "zh" });
        await service.CompositeAsync(7, "en");
        Assert.Equal(2, work.Queries);
        await service.CompositeAsync(7, "zh");
        Assert.Equal(3, work.Queries);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public async Task InvalidConfiguration_FailsBeforeBusinessOrStorage(int kind)
    {
        var cache = new Mock<ICache>(MockBehavior.Strict);
        var locks = new Mock<ILockable>(MockBehavior.Strict);
        var work = new BindingWork();
        using var provider = Build(cache.Object, locks.Object, work);
        var service = provider.GetRequiredService<IBindingService>();
        Func<Task> call = kind switch
        {
            0 => () => service.MissingKeyAsync(1),
            1 => () => service.BothKeysAsync(1),
            2 => () => service.ScalarKeysAsync(1),
            3 => () => service.NonBooleanConditionAsync(1),
            4 => () => service.MissingLockKeyAsync(1),
            5 => () => service.BothLockKeysAsync(1),
            _ => () => service.ScalarLockKeysAsync(1)
        };
        if (kind == 3) await Assert.ThrowsAsync<CompilationErrorException>(call);
        else await Assert.ThrowsAsync<ArgumentException>(call);
        Assert.Equal(0, work.Writes);
        cache.VerifyNoOtherCalls();
        locks.VerifyNoOtherCalls();
    }
}

public sealed class BindingInput
{
    public int Id { get; set; }
    public string Locale { get; set; } = "";
}

public sealed class BindingWork
{
    public int Queries;
    public int Writes;
}

public interface IBindingService
{
    [Cacheable("binding", Key = "id")]
    Task<int> InterfaceAsync(int id);
    [CacheEvict("binding", Key = "id")]
    Task InterfaceEvictAsync(int id);
    Task<int> ImplementationAsync(int id);
    Task ImplementationEvictAsync(int id);
    Task ImplementationLockAsync(int id);
    Task<int> ReorderedAsync(int left, int right);
    Task<int> ImplementationNameAsync(int id);
    [Cacheable("composite", Key = "$\"{id}:{locale}\"")]
    Task<int> CompositeAsync(int id, string locale);
    [CacheEvict("composite", Key = "$\"{input.Id}:{input.Locale}\"")]
    Task CompositeEvictAsync(BindingInput input);
    [CacheEvict("binding")]
    Task MissingKeyAsync(int id);
    [CacheEvict("binding", Key = "id", Keys = "new[] { id }")]
    Task BothKeysAsync(int id);
    [CacheEvict("binding", Keys = "id")]
    Task ScalarKeysAsync(int id);
    [CacheEvict("binding", Key = "id", Condition = "id")]
    Task NonBooleanConditionAsync(int id);
    [TryLock("binding", "locked")]
    Task MissingLockKeyAsync(int id);
    [TryLock("binding", "locked", Key = "id", Keys = "new[] { id }")]
    Task BothLockKeysAsync(int id);
    [TryLock("binding", "locked", Keys = "id")]
    Task ScalarLockKeysAsync(int id);
}

public class BindingService(BindingWork work) : IBindingService
{
    public Task<int> InterfaceAsync(int value) { work.Queries++; return Task.FromResult(value); }
    public Task InterfaceEvictAsync(int value) => Task.CompletedTask;
    [Cacheable("binding", Key = "id")]
    public Task<int> ImplementationAsync(int value) { work.Queries++; return Task.FromResult(value); }
    [CacheEvict("binding", Key = "id", Condition = "id > 0")]
    public Task ImplementationEvictAsync(int value) => Task.CompletedTask;
    [TryLock("binding", "locked", Key = "id")]
    public Task ImplementationLockAsync(int value) => Task.CompletedTask;
    [Cacheable("reordered", Key = "left")]
    public Task<int> ReorderedAsync(int right, int left) { work.Queries++; return Task.FromResult(right); }
    [Cacheable("invalid-name", Key = "value")]
    public Task<int> ImplementationNameAsync(int value) { work.Queries++; return Task.FromResult(value); }
    public Task<int> CompositeAsync(int id, string locale) { work.Queries++; return Task.FromResult(id); }
    public Task CompositeEvictAsync(BindingInput input) => Task.CompletedTask;
    private Task Write() { work.Writes++; return Task.CompletedTask; }
    public Task MissingKeyAsync(int id) => Write();
    public Task BothKeysAsync(int id) => Write();
    public Task ScalarKeysAsync(int id) => Write();
    public Task NonBooleanConditionAsync(int id) => Write();
    public Task MissingLockKeyAsync(int id) => Write();
    public Task BothLockKeysAsync(int id) => Write();
    public Task ScalarLockKeysAsync(int id) => Write();
}

public class ClassBindingService(BindingWork work)
{
    [Cacheable("class-binding", Key = "value")]
    public virtual Task<int> GetAsync(int value) { work.Queries++; return Task.FromResult(value); }
    [CacheEvict("class-binding", Key = "value")]
    public virtual Task EvictAsync(int value) => Task.CompletedTask;
}
