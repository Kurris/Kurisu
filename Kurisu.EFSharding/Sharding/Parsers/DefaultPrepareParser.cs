using System.Linq.Expressions;
using Kurisu.EFSharding.Sharding.Abstractions;
using Kurisu.EFSharding.Sharding.Parsers.Abstractions;
using Kurisu.EFSharding.Sharding.Parsers.Visitors;

namespace Kurisu.EFSharding.Sharding.Parsers;


public class DefaultPrepareParser:IPrepareParser
{
    public IPrepareParseResult Parse(IShardingDbContext shardingDbContext, Expression query)
    {
        var shardingQueryPrepareVisitor = new ShardingQueryPrepareVisitor(shardingDbContext);
        var expression = shardingQueryPrepareVisitor.Visit(query);
        var shardingPrepareResult = shardingQueryPrepareVisitor.GetShardingPrepareResult();
        return new PrepareParseResult(shardingDbContext, expression, shardingPrepareResult);
    }
}