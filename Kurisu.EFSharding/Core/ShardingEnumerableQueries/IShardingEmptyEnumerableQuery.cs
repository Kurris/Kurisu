namespace Kurisu.EFSharding.Core.ShardingEnumerableQueries;

public interface IShardingEmptyEnumerableQuery
{
    IQueryable EmptyQueryable();
}