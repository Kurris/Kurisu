using Kurisu.EFSharding.Sharding.ShardingDbContextExecutors;

namespace Kurisu.EFSharding.Sharding.Abstractions;

public interface IShardingDbContextDiscover
{
    /// <summary>
    /// 获取当前拥有的所有db context
    /// </summary>
    /// <returns></returns>
    IDictionary<string, IDatasourceDbContext> GetDataSourceDbContexts();
}