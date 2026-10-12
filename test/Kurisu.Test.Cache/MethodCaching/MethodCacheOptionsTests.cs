using AspectCore.Extensions.DependencyInjection;
using Kurisu.AspNetCore.Abstractions.Cache;
using Kurisu.AspNetCore.Abstractions.Cache.Aop;
using Kurisu.AspNetCore.Abstractions.DistributedLock;
using Kurisu.Extensions.Cache;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Kurisu.Test.Cache.MethodCaching;

public class MethodCacheOptionsTests
{
    [Fact]
    public void DefaultOnly_DoesNotRequireNamedPolicies()
    {
        var services = new ServiceCollection();
        services.AddMethodCaching();
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<MethodCacheOptions>>().Value;
        Assert.NotNull(options.Default);
        Assert.Empty(options.Policies);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void InvalidDefaultOrNamedPolicy_IsRejected(int kind)
    {
        var services = new ServiceCollection();
        services.AddMethodCaching(o =>
        {
            switch (kind)
            {
                case 0: o.Default = null!; break;
                case 1: o.Default.Expiry = TimeSpan.Zero; break;
                case 2: o.Policies["Named"] = new MethodCachePolicy { Expiry = TimeSpan.Zero }; break;
                case 3: o.Policies[" "] = new MethodCachePolicy(); break;
            }
        });
        using var provider = services.BuildServiceProvider();
        Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<MethodCacheOptions>>().Value);
    }

    [Fact]
    public async Task DefaultAndNamedPolicies_AreSelectedIndependently_AndMissingPolicyFallsBack()
    {
        var cache = new TestCache();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ICache>(cache);
        services.AddSingleton<ILockable>(new TestLocks());
        services.AddMethodCaching(o =>
        {
            o.Default = new MethodCachePolicy { Expiry = TimeSpan.FromMinutes(1) };
            o.Policies["Named"] = new MethodCachePolicy { Expiry = TimeSpan.FromMinutes(5) };
        });
        services.AddTransient<IPolicyQueryService, PolicyQueryService>();
        await using var provider = services.BuildDynamicProxyProvider();
        var service = provider.GetRequiredService<IPolicyQueryService>();
        Assert.Equal("default", await service.DefaultAsync());
        Assert.InRange(cache.LastExpiry!.Value, TimeSpan.FromSeconds(54), TimeSpan.FromSeconds(60));
        Assert.Equal("named", await service.NamedAsync());
        Assert.InRange(cache.LastExpiry!.Value, TimeSpan.FromSeconds(270), TimeSpan.FromSeconds(300));
        await service.EvictNamedAsync();
        Assert.Single(cache.Data);
        Assert.Equal("fallback", await service.MissingAsync());
        Assert.InRange(cache.LastExpiry!.Value, TimeSpan.FromSeconds(54), TimeSpan.FromSeconds(60));
    }

    [Fact]
    public async Task NamedDefault_IsAnOrdinaryPolicy_AndOmittedPolicyUsesFallback()
    {
        var cache = new TestCache();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ICache>(cache);
        services.AddSingleton<ILockable>(new TestLocks());
        services.AddMethodCaching(o =>
        {
            o.Default.Expiry = TimeSpan.FromMinutes(1);
            o.Policies["Default"] = new MethodCachePolicy { Expiry = TimeSpan.FromMinutes(2) };
        });
        services.AddTransient<IPolicyQueryService, PolicyQueryService>();
        await using var provider = services.BuildDynamicProxyProvider();
        var service = provider.GetRequiredService<IPolicyQueryService>();

        Assert.Equal("default", await service.DefaultAsync());
        Assert.InRange(cache.LastExpiry!.Value, TimeSpan.FromSeconds(54), TimeSpan.FromSeconds(60));
        var fallbackKey = Assert.Single(cache.Data).Key;
        Assert.Equal("named-default", await service.NamedDefaultAsync());
        Assert.InRange(cache.LastExpiry!.Value, TimeSpan.FromSeconds(108), TimeSpan.FromSeconds(120));
        await service.EvictDefaultAsync();
        Assert.Single(cache.Data);
        Assert.DoesNotContain(fallbackKey, cache.Data.Keys);
    }
}

public interface IPolicyQueryService
{
    [Cacheable("policy", Key = "\"default\"")]
    Task<string> DefaultAsync();

    [Cacheable("policy", Policy = "Named", Key = "\"named\"")]
    Task<string> NamedAsync();

    [CacheEvict("policy", Policy = "Named", Key = "\"named\"")]
    Task EvictNamedAsync();

    [Cacheable("policy", Policy = "Missing", Key = "\"missing\"")]
    Task<string> MissingAsync();

    [Cacheable("policy", Policy = "Default", Key = "\"named-default\"")]
    Task<string> NamedDefaultAsync();

    [CacheEvict("policy", Key = "\"default\"")]
    Task EvictDefaultAsync();
}

public class PolicyQueryService : IPolicyQueryService
{
    public Task<string> DefaultAsync() => Task.FromResult("default");
    public Task<string> NamedAsync() => Task.FromResult("named");
    public Task EvictNamedAsync() => Task.CompletedTask;
    public Task<string> MissingAsync() => Task.FromResult("fallback");
    public Task<string> NamedDefaultAsync() => Task.FromResult("named-default");
    public Task EvictDefaultAsync() => Task.CompletedTask;
}