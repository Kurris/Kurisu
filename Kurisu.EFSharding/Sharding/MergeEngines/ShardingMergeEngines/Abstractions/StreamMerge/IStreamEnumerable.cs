namespace Kurisu.EFSharding.Sharding.MergeEngines.ShardingMergeEngines.Abstractions.StreamMerge;

internal interface IStreamEnumerable<TEntity> : IAsyncEnumerable<TEntity>, IEnumerable<TEntity>, IDisposable
{
}