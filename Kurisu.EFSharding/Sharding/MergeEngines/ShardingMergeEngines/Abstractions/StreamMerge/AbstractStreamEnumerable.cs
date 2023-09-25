using System.Collections;
using Kurisu.EFSharding.Sharding.Enumerators.StreamMergeAsync;
using Kurisu.EFSharding.Sharding.MergeEngines.Executors.Abstractions;
using Kurisu.EFSharding.Sharding.MergeEngines.ShardingExecutors;


namespace Kurisu.EFSharding.Sharding.MergeEngines.ShardingMergeEngines.Abstractions.StreamMerge;

internal abstract class AbstractStreamEnumerable<TEntity> : AbstractBaseMergeEngine, IStreamEnumerable<TEntity>
{
    protected abstract IExecutor<IStreamMergeAsyncEnumerator<TEntity>> CreateExecutor(bool async);

    protected AbstractStreamEnumerable(StreamMergeContext streamMergeContext) : base(streamMergeContext)
    {
    }


    public IAsyncEnumerator<TEntity> GetAsyncEnumerator(
        CancellationToken cancellationToken = new CancellationToken())
    {
        return GetStreamMergeAsyncEnumerator(true, cancellationToken);
    }

    public IEnumerator<TEntity> GetEnumerator()
    {
        return GetStreamMergeAsyncEnumerator(false);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Dispose()
    {
        GetStreamMergeContext().Dispose();
    }


    /// <summary>
    /// 获取查询的迭代器
    /// </summary>
    /// <param name="async"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public virtual IStreamMergeAsyncEnumerator<TEntity> GetStreamMergeAsyncEnumerator(bool async, CancellationToken cancellationToken = new CancellationToken())
    {
        cancellationToken.ThrowIfCancellationRequested();

        var defaultSqlRouteUnits = GetDefaultSqlRouteUnits();
        var executor = CreateExecutor(async);
        return ShardingExecutor.Execute<IStreamMergeAsyncEnumerator<TEntity>>(GetStreamMergeContext(), executor,
            async, defaultSqlRouteUnits, cancellationToken);
    }
}