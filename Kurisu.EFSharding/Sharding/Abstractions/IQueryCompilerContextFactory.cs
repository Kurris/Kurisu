using Kurisu.EFSharding.Sharding.Parsers.Abstractions;
using Kurisu.EFSharding.Sharding.ShardingExecutors.Abstractions;

namespace Kurisu.EFSharding.Sharding.Abstractions;

public interface IQueryCompilerContextFactory
{
    IQueryCompilerContext Create(IPrepareParseResult prepareParseResult);
}