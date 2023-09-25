using Kurisu.EFSharding.Sharding.ShardingExecutors.Abstractions;

namespace Kurisu.EFSharding.Sharding.Abstractions;

public interface IShardingTrackQueryExecutor
{
    /// <summary>
    /// execute query
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="queryCompilerContext"></param>
    /// <returns></returns>
    TResult Execute<TResult>(IQueryCompilerContext queryCompilerContext);


    /// <summary>
    /// execute query async
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="queryCompilerContext"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    TResult ExecuteAsync<TResult>(IQueryCompilerContext queryCompilerContext, CancellationToken cancellationToken = new CancellationToken());
}