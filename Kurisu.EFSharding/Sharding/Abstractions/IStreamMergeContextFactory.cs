using Kurisu.EFSharding.Sharding.ShardingExecutors.Abstractions;

namespace Kurisu.EFSharding.Sharding.Abstractions;

internal interface IStreamMergeContextFactory
{
    StreamMergeContext Create(IMergeQueryCompilerContext mergeQueryCompilerContext);
}