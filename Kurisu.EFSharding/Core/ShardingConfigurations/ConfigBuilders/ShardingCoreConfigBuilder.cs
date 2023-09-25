using Kurisu.EFSharding.Core.DependencyInjection;
using Kurisu.EFSharding.Core.RuntimeContexts;
using Kurisu.EFSharding.Core.ShardingConfigurations.Abstractions;
using Kurisu.EFSharding.Sharding.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.EFSharding.Core.ShardingConfigurations.ConfigBuilders;

public class ShardingCoreConfigBuilder<TShardingDbContext> where TShardingDbContext : DbContext, IShardingDbContext
{
    private readonly IServiceCollection _services;


    private readonly ShardingRuntimeBuilder<TShardingDbContext> _shardingRuntimeBuilder;

    public ShardingCoreConfigBuilder(IServiceCollection services)
    {
        _services = services;
        _shardingRuntimeBuilder = new ShardingRuntimeBuilder<TShardingDbContext>();
    }

    public ShardingCoreConfigBuilder<TShardingDbContext> UseRouteConfig(Action<IRouteOptions> routeConfigure)
    {
        _shardingRuntimeBuilder.UseRouteConfig(routeConfigure);
        return this;
    }

    public ShardingCoreConfigBuilder<TShardingDbContext> UseRouteConfig(Action<IShardingProvider, IRouteOptions> routeConfigure)
    {
        _shardingRuntimeBuilder.UseRouteConfig(routeConfigure);
        return this;
    }

    public ShardingCoreConfigBuilder<TShardingDbContext> UseConfig(Action<ShardingOptions> shardingConfigure)
    {
        _shardingRuntimeBuilder.UseConfig(shardingConfigure);
        return this;
    }

    public ShardingCoreConfigBuilder<TShardingDbContext> UseConfig(Action<IShardingProvider, ShardingOptions> shardingConfigure)
    {
        _shardingRuntimeBuilder.UseConfig(shardingConfigure);
        return this;
    }

    public ShardingCoreConfigBuilder<TShardingDbContext> ReplaceService<TService, TImplement>()
    {
        return ReplaceService<TService, TImplement>(ServiceLifetime.Singleton);
    }

    public ShardingCoreConfigBuilder<TShardingDbContext> ReplaceService<TService, TImplement>(ServiceLifetime lifetime)
    {
        _shardingRuntimeBuilder.ReplaceService<TService, TImplement>(lifetime);
        return this;
    }

    public void AddShardingCore()
    {
        _services.AddSingleton<IShardingRuntimeContext<TShardingDbContext>>(sp => _shardingRuntimeBuilder.Build(sp));
        _services.AddSingleton<IShardingRuntimeContext>(sp => sp.GetRequiredService<IShardingRuntimeContext<TShardingDbContext>>());
    }
}