using Kurisu.EFSharding.Sharding.ShardingExecutors.QueryableCombines;

namespace Kurisu.EFSharding.Sharding.ShardingExecutors.Abstractions;

public interface IQueryableCombine
{
    QueryCombineResult Combine(IQueryCompilerContext queryCompilerContext);
}