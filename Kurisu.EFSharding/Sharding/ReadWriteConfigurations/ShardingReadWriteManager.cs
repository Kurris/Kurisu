using Kurisu.EFSharding.Sharding.ReadWriteConfigurations.Abstractions;

namespace Kurisu.EFSharding.Sharding.ReadWriteConfigurations;

public class ShardingReadWriteManager:IShardingReadWriteManager
{
    private readonly IShardingReadWriteAccessor _shardingReadWriteAccessor;


    public ShardingReadWriteManager(IShardingReadWriteAccessor shardingReadWriteAccessor)
    {
        _shardingReadWriteAccessor = shardingReadWriteAccessor;
    }

    public ShardingReadWriteContext GetCurrent()
    {
        return _shardingReadWriteAccessor.ShardingReadWriteContext;
    }

    public ShardingReadWriteScope CreateScope()
    {
        var shardingPageScope = new ShardingReadWriteScope(_shardingReadWriteAccessor);
        shardingPageScope.ShardingReadWriteAccessor.ShardingReadWriteContext = ShardingReadWriteContext.Create();
        return shardingPageScope;
    }
}