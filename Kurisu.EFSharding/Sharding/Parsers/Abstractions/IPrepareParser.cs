using System.Linq.Expressions;
using Kurisu.EFSharding.Sharding.Abstractions;

namespace Kurisu.EFSharding.Sharding.Parsers.Abstractions;


public interface IPrepareParser
{
    IPrepareParseResult Parse(IShardingDbContext shardingDbContext, Expression query);
}