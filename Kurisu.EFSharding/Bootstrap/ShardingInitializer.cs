using Kurisu.EFSharding.Core.DependencyInjection;
using Kurisu.EFSharding.Core.ShardingConfigurations.Abstractions;
using Kurisu.EFSharding.Extensions;

namespace Kurisu.EFSharding.Bootstrap;

internal class ShardingInitializer : IShardingInitializer
{
    private readonly IShardingProvider _shardingProvider;
    private readonly IRouteOptions _routeOptions;

    public ShardingInitializer(IShardingProvider shardingProvider,
        IRouteOptions routeOptions)
    {
        _shardingProvider = shardingProvider;
        _routeOptions = routeOptions;
    }

    /// <summary>
    /// 初始化
    /// </summary>
    public void Initialize()
    {
        var shardingEntities = _routeOptions.RouteTypes;

        foreach (var entityType in shardingEntities)
        {
            var metadataInitializerType =
                typeof(MetadataInitializer<>).GetGenericType0(entityType);

            var entityMetadataInitializer = (IMetadataInitializer) _shardingProvider.CreateInstance(metadataInitializerType);
            entityMetadataInitializer.Initialize();
        }
    }
}