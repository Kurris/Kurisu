namespace Kurisu.EFSharding.Sharding.ReadWriteConfigurations.Abstractions;

public interface IShardingReadWriteManager
{

    ShardingReadWriteContext GetCurrent();

    ShardingReadWriteScope CreateScope();
}