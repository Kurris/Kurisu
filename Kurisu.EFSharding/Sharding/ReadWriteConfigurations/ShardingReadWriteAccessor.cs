using Kurisu.EFSharding.Sharding.ReadWriteConfigurations.Abstractions;

namespace Kurisu.EFSharding.Sharding.ReadWriteConfigurations;

public class ShardingReadWriteAccessor : IShardingReadWriteAccessor
{
    private static AsyncLocal<ShardingReadWriteContext> _shardingReadWriteContext = new();


    /// <summary>
    ///
    /// </summary>
    public ShardingReadWriteContext ShardingReadWriteContext
    {
        get => _shardingReadWriteContext.Value;
        set => _shardingReadWriteContext.Value = value;
    }
}