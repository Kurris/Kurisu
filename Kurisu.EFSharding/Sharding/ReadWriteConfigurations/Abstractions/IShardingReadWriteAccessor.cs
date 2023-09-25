namespace Kurisu.EFSharding.Sharding.ReadWriteConfigurations.Abstractions;

public interface IShardingReadWriteAccessor
{
    ShardingReadWriteContext ShardingReadWriteContext { get; set; }
}