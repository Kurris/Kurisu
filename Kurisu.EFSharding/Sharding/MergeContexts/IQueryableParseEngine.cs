using Kurisu.EFSharding.Sharding.ShardingExecutors.Abstractions;

namespace Kurisu.EFSharding.Sharding.MergeContexts;

public interface IQueryableParseEngine
{
    IParseResult Parse(IMergeQueryCompilerContext mergeQueryCompilerContext);
}