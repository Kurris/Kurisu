using Kurisu.EFSharding.Sharding.ShardingExecutors.Abstractions;
#if EFCORE2
using Microsoft.EntityFrameworkCore.Internal;
#endif

namespace Kurisu.EFSharding.Sharding.Abstractions
{

    public interface IShardingQueryExecutor
    {
        /// <summary>
        /// execute query
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="mergeQueryCompilerContext"></param>
        /// <returns></returns>
        TResult Execute<TResult>(IMergeQueryCompilerContext mergeQueryCompilerContext);
        /// <summary>
        /// execute query async
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="mergeQueryCompilerContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        TResult ExecuteAsync<TResult>(IMergeQueryCompilerContext mergeQueryCompilerContext, CancellationToken cancellationToken = new CancellationToken());
    }
}