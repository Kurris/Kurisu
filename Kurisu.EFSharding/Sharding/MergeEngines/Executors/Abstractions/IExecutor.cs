using Kurisu.EFSharding.Sharding.MergeEngines.Common;
using Kurisu.EFSharding.Sharding.MergeEngines.ShardingMergeEngines.Abstractions;

namespace Kurisu.EFSharding.Sharding.MergeEngines.Executors.Abstractions;


internal interface IExecutor<TResult>
{
    IShardingMerger<TResult> GetShardingMerger();
    Task<List<TResult>> ExecuteAsync(bool async, DataSourceSqlExecutorUnit dataSourceSqlExecutorUnit, CancellationToken cancellationToken = new CancellationToken());
}