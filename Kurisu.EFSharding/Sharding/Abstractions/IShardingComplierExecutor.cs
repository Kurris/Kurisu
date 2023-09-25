using System.Linq.Expressions;

namespace Kurisu.EFSharding.Sharding.Abstractions;

public interface IShardingCompilerExecutor
{
    /// <summary>
    /// execute query
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="shardingDbContext"></param>
    /// <param name="query"></param>
    /// <returns></returns>
    TResult Execute<TResult>(IShardingDbContext shardingDbContext, Expression query);

    /// <summary>
    /// execute query async
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="shardingDbContext"></param>
    /// <param name="query"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    TResult ExecuteAsync<TResult>(IShardingDbContext shardingDbContext, Expression query, CancellationToken cancellationToken = new());
}