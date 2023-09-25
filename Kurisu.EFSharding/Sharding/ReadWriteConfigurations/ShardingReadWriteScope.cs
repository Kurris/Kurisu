using Kurisu.EFSharding.Sharding.ReadWriteConfigurations.Abstractions;

namespace Kurisu.EFSharding.Sharding.ReadWriteConfigurations;

public class ShardingReadWriteScope:IDisposable
{
    public IShardingReadWriteAccessor ShardingReadWriteAccessor { get; }


    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="shardingReadWriteAccessor"></param>
    public ShardingReadWriteScope(IShardingReadWriteAccessor shardingReadWriteAccessor)
    {
        ShardingReadWriteAccessor = shardingReadWriteAccessor;
    }

    /// <summary>
    /// 回收
    /// </summary>
    public void Dispose()
    {
        ShardingReadWriteAccessor.ShardingReadWriteContext = null;
    }
}