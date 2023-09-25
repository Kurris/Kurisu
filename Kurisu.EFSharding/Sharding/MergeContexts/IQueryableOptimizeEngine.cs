using Kurisu.EFSharding.Sharding.ShardingExecutors.Abstractions;

namespace Kurisu.EFSharding.Sharding.MergeContexts;

internal interface IQueryableOptimizeEngine
{
    IOptimizeResult Optimize(IMergeQueryCompilerContext mergeQueryCompilerContext, IParseResult parseResult,
        IRewriteResult rewriteResult);
}