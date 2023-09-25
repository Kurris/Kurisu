using Kurisu.EFSharding.Sharding.ShardingExecutors.Abstractions;

namespace Kurisu.EFSharding.Sharding.MergeContexts;

public interface IQueryableRewriteEngine
{
    IRewriteResult GetRewriteQueryable(IMergeQueryCompilerContext mergeQueryCompilerContext, IParseResult parseResult);
}