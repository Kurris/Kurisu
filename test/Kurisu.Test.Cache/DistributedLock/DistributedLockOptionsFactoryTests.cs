using Kurisu.AspNetCore.Abstractions.DistributedLock.Impl;
using AspectCore.Extensions.DependencyInjection;
using Kurisu.AspNetCore.Abstractions.DistributedLock;
using Kurisu.AspNetCore.Abstractions.DistributedLock.Aop;
using Kurisu.Expressions;
using Kurisu.Extensions.Cache;
using Kurisu.Test.Cache.MethodCaching;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kurisu.Test.Cache.DistributedLock;

public class DistributedLockOptionsFactoryTests
{
    [Fact]
    public async Task DefaultFactory_CreatesIndependentConfigurationsWithExistingDefaults()
    {
        var factory = new DefaultDistributedLockOptionsFactory();
        var first = factory.CreateDefault();
        var second = factory.CreateDefault();
        Assert.NotSame(first, second);
        Assert.NotSame(first.TimeModeHandler, second.TimeModeHandler);
        Assert.NotSame(first.RetryStrategy, second.RetryStrategy);
        first.TimeModeHandler = LockTimeModeHandler.FixedExpiry(TimeSpan.FromSeconds(1));
        var settings = second.TimeModeHandler.Resolve();
        Assert.Equal(TimeSpan.FromSeconds(6), settings.Expiry);
        Assert.True(settings.EnableAutoRenewal);
        Assert.Null(settings.MaxRenewalCount);
        Assert.True(await second.RetryStrategy.ShouldRetryAsync(1));
        Assert.True(await second.RetryStrategy.ShouldRetryAsync(2));
        Assert.False(await second.RetryStrategy.ShouldRetryAsync(3));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Registration_PreservesApplicationOverridesRegardlessOfOrder(bool registerCustomLast)
    {
        var services = new ServiceCollection();
        var factory = new ApplicationLockOptionsFactory();
        var locks = new TestLocks();
        if (!registerCustomLast)
        {
            services.AddSingleton<IDistributedLockOptionsFactory>(factory);
            services.AddSingleton<ILockable>(locks);
        }

        services.AddRedis();
        services.AddMethodCaching();
        services.AddDistributedLockOptions();
        if (registerCustomLast)
        {
            services.AddSingleton<IDistributedLockOptionsFactory>(factory);
            services.AddSingleton<ILockable>(locks);
        }

        using var provider = services.BuildServiceProvider();
        Assert.Same(factory, provider.GetRequiredService<IDistributedLockOptionsFactory>());
        Assert.Same(locks, provider.GetRequiredService<ILockable>());
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
    public async Task TryLock_UsesApplicationDefaultsAndPreservesExplicitOverrides(int mode)
    {
        var factory = new ApplicationLockOptionsFactory { RenewalLimit = mode == 9 ? 2 : null };
        var locks = new RecordingLocks();
        var services = new ServiceCollection();
        services.AddSingleton<IDistributedLockOptionsFactory>(factory);
        services.AddDistributedLockOptions();
        services.AddSingleton<ILockable>(locks);
        services.AddSingleton<ExpressionCompiler>();
        services.AddSingleton<MethodExpressionEvaluator>();
        services.AddTransient<IFactoryLockService, FactoryLockService>();
        await using var provider = services.BuildDynamicProxyProvider();
        var service = provider.GetRequiredService<IFactoryLockService>();
        if (mode is 7 or 8)
        {
            await Assert.ThrowsAsync<ArgumentException>(() => mode switch
            {
                7 => service.InvalidExpiryAsync(1),
                _ => service.InvalidRenewalCountAsync(1)
            });
            Assert.Null(locks.Options);
            return;
        }

        await (mode switch
        {
            0 => service.DefaultAsync(1),
            1 => service.ExplicitRetryAsync(1),
            2 => service.FixedAsync(1),
            3 => service.LimitedAsync(1),
            4 => service.ExpiryOnlyAsync(1),
            5 => service.DisableRenewalOnlyAsync(1),
            6 => service.RenewalCountOnlyAsync(1),
            _ => service.InfiniteRenewalAsync(1)
        });
        Assert.Equal(1, factory.Calls);
        var options = locks.Options;
        Assert.NotNull(options);
        var settings = options.TimeModeHandler.Resolve();
        var expectedExpiry = mode switch { 2 or 3 => 10, 4 => 20, _ => 42 };
        Assert.Equal(TimeSpan.FromSeconds(expectedExpiry), settings.Expiry);
        Assert.Equal(mode is not (2 or 5), settings.EnableAutoRenewal);
        int? expectedRenewalCount = mode is 3 or 6 ? 2 : null;
        Assert.Equal(expectedRenewalCount, settings.MaxRenewalCount);
        if (mode == 1)
            Assert.False(await options.RetryStrategy.ShouldRetryAsync(1));
        else
            Assert.Same(factory.RetryStrategy, options.RetryStrategy);
    }

    private sealed class ApplicationLockOptionsFactory : IDistributedLockOptionsFactory
    {
        public int? RenewalLimit { get; init; }
        public int Calls { get; private set; }
        public IDistributedLockRetryStrategy RetryStrategy { get; } = new DefaultLockRetryStrategy(5);

        public DistributedLockAcquisitionOptions CreateDefault()
        {
            Calls++;
            return new DistributedLockAcquisitionOptions
            {
                TimeModeHandler = new LockTimeModeHandler(TimeSpan.FromSeconds(42), maxRenewalCount: RenewalLimit),
                RetryStrategy = RetryStrategy
            };
        }
    }

    private sealed class RecordingLocks : ILockable
    {
        private readonly TestLocks _locks = new();
        public DistributedLockAcquisitionOptions? Options { get; private set; }

        public Task<ILockHandler> LockAsync(string lockKey, DistributedLockAcquisitionOptions options,
            CancellationToken cancellationToken = default)
        {
            Options = options;
            return _locks.LockAsync(lockKey, options, cancellationToken);
        }
    }
}

public interface IFactoryLockService
{
    [TryLock("factory", "locked", Key = "id")]
    Task DefaultAsync(int id);

    [TryLock("factory", "locked", Key = "id", RetryCount = 0)]
    Task ExplicitRetryAsync(int id);

    [TryLock("factory", "locked", ExpirySeconds = 10, MaxRenewalCount = LockRenewal.Disabled, Key = "id")]
    Task FixedAsync(int id);

    [TryLock("factory", "locked", ExpirySeconds = 10, MaxRenewalCount = 2, Key = "id")]
    Task LimitedAsync(int id);

    [TryLock("factory", "locked", ExpirySeconds = 20, Key = "id")]
    Task ExpiryOnlyAsync(int id);

    [TryLock("factory", "locked", MaxRenewalCount = LockRenewal.Disabled, Key = "id")]
    Task DisableRenewalOnlyAsync(int id);

    [TryLock("factory", "locked", MaxRenewalCount = 2, Key = "id")]
    Task RenewalCountOnlyAsync(int id);

    [TryLock("factory", "locked", ExpirySeconds = 0, Key = "id")]
    Task InvalidExpiryAsync(int id);

    [TryLock("factory", "locked", MaxRenewalCount = -2, Key = "id")]
    Task InvalidRenewalCountAsync(int id);

    [TryLock("factory", "locked", MaxRenewalCount = LockRenewal.Unlimited, Key = "id")]
    Task InfiniteRenewalAsync(int id);
}

public class FactoryLockService : IFactoryLockService
{
    public Task DefaultAsync(int id) => Task.CompletedTask;
    public Task ExplicitRetryAsync(int id) => Task.CompletedTask;
    public Task FixedAsync(int id) => Task.CompletedTask;
    public Task LimitedAsync(int id) => Task.CompletedTask;
    public Task ExpiryOnlyAsync(int id) => Task.CompletedTask;
    public Task DisableRenewalOnlyAsync(int id) => Task.CompletedTask;
    public Task RenewalCountOnlyAsync(int id) => Task.CompletedTask;
    public Task InvalidExpiryAsync(int id) => Task.CompletedTask;
    public Task InvalidRenewalCountAsync(int id) => Task.CompletedTask;
    public Task InfiniteRenewalAsync(int id) => Task.CompletedTask;
}
