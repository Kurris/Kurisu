using AspectCore.Extensions.DependencyInjection;
using Kurisu.AspNetCore.Abstractions.Cache;
using Kurisu.AspNetCore.Abstractions.Cache.Aop;
using Kurisu.AspNetCore.Abstractions.DistributedLock;
using Kurisu.Extensions.Cache;
using Kurisu.Extensions.Cache.MethodCaching;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Kurisu.Test.Cache.MethodCaching;

public class DefaultCacheKeyTests
{
    private static ServiceProvider Build(TestCache cache, BindingWork work)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ICache>(cache);
        services.AddSingleton<ILockable>(new TestLocks());
        services.AddSingleton(work);
        services.AddMethodCaching();
        services.AddTransient<IDefaultKeyService, DefaultKeyService>();
        return services.BuildDynamicProxyProvider();
    }

    [Fact]
    public async Task SingleArgument_MatchesExplicitKey_AndDefaultEviction()
    {
        var cache = new TestCache();
        var work = new BindingWork();
        await using var provider = Build(cache, work);
        var service = provider.GetRequiredService<IDefaultKeyService>();
        Assert.Equal(7, await service.GetAsync(7));
        Assert.Equal(7, await service.ExplicitAsync(7));
        Assert.Equal(1, work.Queries);
        Assert.Single(cache.Data);
        await service.EvictAsync(7);
        Assert.Empty(cache.Data);
        await service.GetAsync(7);
        Assert.Equal(2, work.Queries);
    }

    [Fact]
    public async Task SimpleValues_ShareStringKey_WhileRegionsRemainIsolated()
    {
        var cache = new TestCache();
        var work = new BindingWork();
        await using var provider = Build(cache, work);
        var service = provider.GetRequiredService<IDefaultKeyService>();
        await service.GetAsync(7);
        await service.OtherRegionAsync(7);
        await service.ObjectAsync(7);
        await service.ObjectAsync(7L);
        await service.ObjectAsync("7");
        Assert.Equal(2, work.Queries);
        Assert.Equal(2, cache.Data.Count);
        await service.EvictObjectAsync("7");
        Assert.Single(cache.Data);
        await service.OtherRegionAsync(7);
        await service.ObjectAsync(7L);
        Assert.Equal(3, work.Queries);
    }

    [Fact]
    public void UnsupportedKeys_AreRejectedInsteadOfSerialized()
    {
        var generator = new DefaultCacheKeyGenerator(Options.Create(new MethodCacheOptions()));
        var policy = new MethodCachePolicy();
        var scope = new MethodCacheScope();
        object?[] values = [null, new { Id = 7 }, new[] { 1, 2 }, true, 7d, 7m, Guid.Empty, DateTime.MinValue, DayOfWeek.Monday];
        foreach (var value in values)
        {
            var error = Assert.Throws<ArgumentException>(() => generator.Generate("products", value!, policy, scope));
            Assert.Contains("Key", error.Message);
        }
    }

    [Theory]
    [InlineData(7)]
    [InlineData(7L)]
    [InlineData("7")]
    public void SimpleKey_IsReadableAndIncludesRegionVersionAndScope(object value)
    {
        var generator = new DefaultCacheKeyGenerator(Options.Create(new MethodCacheOptions { KeyPrefix = "shop:production" }));
        var scope = new MethodCacheScope();
        scope.Dimensions["tenant"] = "a";
        Assert.Equal("shop:production:data:s:products:1:scope:1:tenant=a:key:7",
            generator.Generate("products", value, new MethodCachePolicy(), scope));
    }

    [Fact]
    public void DirectKey_EscapesSeparators_AndPreservesDimensionBoundaries()
    {
        var generator = new DefaultCacheKeyGenerator(Options.Create(new MethodCacheOptions()));
        var policy = new MethodCachePolicy { Version = "v:1" };
        var scope = new MethodCacheScope();
        scope.Dimensions["tenant:name"] = "a=b";
        Assert.Equal("kurisu:method-cache:data:s:product%3Aprices:v%3A1:scope:1:tenant%3Aname=a%3Db:key:a%3Ab%25",
            generator.Generate("product:prices", "a:b%", policy, scope));

        Assert.NotEqual(generator.Generate("a:b", "7", new MethodCachePolicy { Version = "c" }, scope),
            generator.Generate("a", "7", new MethodCachePolicy { Version = "b:c" }, scope));
        Assert.NotEqual(generator.Generate("products", "a:b", policy, scope),
            generator.Generate("products", "a%3Ab", policy, scope));

        var first = new MethodCacheScope();
        first.Dimensions["a"] = "b:c=d";
        var second = new MethodCacheScope();
        second.Dimensions["a"] = "b";
        second.Dimensions["c"] = "d";
        Assert.NotEqual(generator.Generate("products", "7", policy, first), generator.Generate("products", "7", policy, second));
        first.Dimensions["a"] = null!;
        second.Dimensions.Clear();
        second.Dimensions["a"] = "";
        Assert.NotEqual(generator.Generate("products", "7", policy, first), generator.Generate("products", "7", policy, second));
    }

    [Fact]
    public void ComplexScope_IsRejectedWithDimensionName()
    {
        var generator = new DefaultCacheKeyGenerator(Options.Create(new MethodCacheOptions()));
        var policy = new MethodCachePolicy();
        var scope = new MethodCacheScope();
        scope.Dimensions["permissions"] = new[] { "read", "write" };
        var error = Assert.Throws<ArgumentException>(() => generator.Generate("products", "7", policy, scope));
        Assert.Contains("permissions", error.Message);
    }

    [Fact]
    public async Task DefaultKey_UsesOnlyFirstParameter()
    {
        var cache = new TestCache();
        var work = new BindingWork();
        await using var provider = Build(cache, work);
        var service = provider.GetRequiredService<IDefaultKeyService>();
        await service.PairAsync(1, 2);
        Assert.EndsWith(":key:1", Assert.Single(cache.Data).Key);
        await service.PairAsync(1, 99);
        using var cts = new CancellationTokenSource();
        await service.PairAliasAsync(1, 2, cts.Token);
        await service.PairAsync(2, 1);
        Assert.Equal(2, work.Queries);
        await service.EvictPairAsync(1, 2, CancellationToken.None);
        Assert.Single(cache.Data);
        await service.PairAsync(2, 1);
        Assert.Equal(2, work.Queries);
        await service.PairAsync(1, 2);
        Assert.Equal(3, work.Queries);
    }

    [Fact]
    public async Task ExplicitCollectionKey_IsOneString_WhileKeysEvictsIndividualEntries()
    {
        var cache = new TestCache();
        var work = new BindingWork();
        await using var provider = Build(cache, work);
        var service = provider.GetRequiredService<IDefaultKeyService>();
        await service.GetAsync(1);
        await service.GetAsync(2);
        await service.CollectionAsync([1, 2]);
        Assert.Equal(3, cache.Data.Count);
        await service.EvictBatchAsync([1, 2, 1]);
        Assert.Single(cache.Data); // batch does not delete the collection query
        await service.CollectionAsync([1, 2]);
        Assert.Equal(3, work.Queries);
        await service.EvictCollectionAsync([1, 2]);
        Assert.Empty(cache.Data);
    }

    [Fact]
    public async Task NoArguments_CanUseExplicitConstantKey()
    {
        var cache = new TestCache();
        var work = new BindingWork();
        await using var provider = Build(cache, work);
        var service = provider.GetRequiredService<IDefaultKeyService>();
        await service.NoArgsAsync();
        Assert.EndsWith(":key:all", Assert.Single(cache.Data).Key);
        await service.TokenOnlyAsync(CancellationToken.None);
        Assert.Equal(1, work.Queries);
        await service.EvictNoArgsAsync();
        Assert.Empty(cache.Data);
    }

    [Fact]
    public async Task ExplicitDtoKey_IsCapturedBeforeBusinessMutation()
    {
        var cache = new TestCache();
        var work = new BindingWork();
        await using var provider = Build(cache, work);
        var service = provider.GetRequiredService<IDefaultKeyService>();
        await service.DtoAsync(new BindingInput { Id = 7 });
        var input = new BindingInput { Id = 7 };
        await service.EvictDtoAsync(input);
        Assert.Equal(8, input.Id);
        Assert.Empty(cache.Data);
    }

    [Fact]
    public async Task MultipleParameters_CanUseExplicitCompositeStringKey()
    {
        var cache = new TestCache();
        var work = new BindingWork();
        await using var provider = Build(cache, work);
        var service = provider.GetRequiredService<IDefaultKeyService>();
        await service.CompositeAsync(1, 2);
        Assert.EndsWith(":key:1%3A2", Assert.Single(cache.Data).Key);
        await service.CompositeAsync(1, 3);
        await service.CompositeAsync(1, 2);
        Assert.Equal(2, work.Queries);
        await service.EvictCompositeAsync(1, 2);
        Assert.Single(cache.Data);
        await service.CompositeAsync(1, 3);
        Assert.Equal(2, work.Queries);
        await service.CompositeAsync(1, 2);
        Assert.Equal(3, work.Queries);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public async Task InvalidKey_FailsBeforeStorageOrBusiness(int kind)
    {
        var cache = new TestCache { FailRead = true };
        var work = new BindingWork();
        await using var provider = Build(cache, work);
        var service = provider.GetRequiredService<IDefaultKeyService>();
        Func<Task> call = kind switch
        {
            0 => () => service.ObjectAsync(new BindingInput { Id = 7 }),
            1 => () => service.MissingArgsAsync(),
            2 => () => service.TokenFirstAsync(CancellationToken.None),
            3 => () => service.InvalidExplicitAsync(new BindingInput { Id = 7 }),
            _ => () => service.InvalidBatchAsync([new BindingInput { Id = 7 }])
        };
        await Assert.ThrowsAsync<ArgumentException>(call);
        Assert.Equal(0, work.Queries);
        Assert.Equal(0, work.Writes);
        Assert.Equal(0, cache.Removes);
        Assert.Empty(cache.Data);
    }
}

public interface IDefaultKeyService
{
    [Cacheable("default-key")]
    Task<int> GetAsync(int id);

    [Cacheable("default-key", Key = "id")]
    Task<int> ExplicitAsync(int id);

    [CacheEvict("default-key")]
    Task EvictAsync(int id);

    [CacheEvict("default-key", Key = "id")]
    Task EvictObjectAsync(object id);

    [Cacheable("other-region")]
    Task<int> OtherRegionAsync(int id);

    [Cacheable("default-key")]
    Task<int> ObjectAsync(object id);

    [Cacheable("default-key")]
    Task<int> PairAsync(int first, int second);

    [Cacheable("default-key")]
    Task<int> PairAliasAsync(int left, int right, CancellationToken token);

    [CacheEvict("default-key")]
    Task EvictPairAsync(int left, int right, CancellationToken token);

    [Cacheable("default-key", Key = "string.Join(\",\", ids)")]
    Task<int> CollectionAsync(int[] ids);

    [CacheEvict("default-key", Key = "string.Join(\",\", ids)")]
    Task EvictCollectionAsync(int[] ids);

    [CacheEvict("default-key", Keys = "ids")]
    Task EvictBatchAsync(int[] ids);

    [Cacheable("no-args", Key = "\"all\"")]
    Task<int> NoArgsAsync();

    [Cacheable("no-args", Key = "\"all\"")]
    Task<int> TokenOnlyAsync(CancellationToken token);

    [CacheEvict("no-args", Key = "\"all\"")]
    Task EvictNoArgsAsync();

    [Cacheable("dto", Key = "input.Id")]
    Task<int> DtoAsync(BindingInput input);

    [CacheEvict("dto", Key = "input.Id")]
    Task EvictDtoAsync(BindingInput input);

    [Cacheable("composite", Key = "$\"{first}:{second}\"")]
    Task<int> CompositeAsync(int first, int second);

    [CacheEvict("composite", Key = "$\"{first}:{second}\"")]
    Task EvictCompositeAsync(int first, int second);

    [Cacheable("invalid")]
    Task<int> MissingArgsAsync();

    [Cacheable("invalid")]
    Task<int> TokenFirstAsync(CancellationToken token);

    [Cacheable("invalid", Key = "input")]
    Task<int> InvalidExplicitAsync(BindingInput input);

    [CacheEvict("invalid", Keys = "inputs")]
    Task InvalidBatchAsync(BindingInput[] inputs);
}

public class DefaultKeyService(BindingWork work) : IDefaultKeyService
{
    private Task<int> Query(int value)
    {
        work.Queries++;
        return Task.FromResult(value);
    }

    public Task<int> GetAsync(int id) => Query(id);
    public Task<int> ExplicitAsync(int id) => Query(id);
    public Task EvictAsync(int id) => Task.CompletedTask;
    public Task EvictObjectAsync(object id) => Task.CompletedTask;
    public Task<int> OtherRegionAsync(int id) => Query(id);
    public Task<int> ObjectAsync(object id) => Query(Convert.ToInt32(id));
    public Task<int> PairAsync(int first, int second) => Query(first);
    public Task<int> PairAliasAsync(int left, int right, CancellationToken token) => Query(left);
    public Task EvictPairAsync(int left, int right, CancellationToken token) => Task.CompletedTask;
    public Task<int> CollectionAsync(int[] ids) => Query(ids.Length);
    public Task EvictCollectionAsync(int[] ids) => Task.CompletedTask;
    public Task EvictBatchAsync(int[] ids) => Task.CompletedTask;
    public Task<int> NoArgsAsync() => Query(1);
    public Task<int> TokenOnlyAsync(CancellationToken token) => Query(1);
    public Task EvictNoArgsAsync() => Task.CompletedTask;
    public Task<int> DtoAsync(BindingInput input) => Query(input.Id);
    public Task<int> CompositeAsync(int first, int second) => Query(first);
    public Task EvictCompositeAsync(int first, int second) => Task.CompletedTask;
    public Task<int> MissingArgsAsync() => Query(1);
    public Task<int> TokenFirstAsync(CancellationToken token) => Query(1);
    public Task<int> InvalidExplicitAsync(BindingInput input) => Query(input.Id);

    public Task InvalidBatchAsync(BindingInput[] inputs)
    {
        work.Writes++;
        return Task.CompletedTask;
    }

    public Task EvictDtoAsync(BindingInput input)
    {
        input.Id++;
        return Task.CompletedTask;
    }
}