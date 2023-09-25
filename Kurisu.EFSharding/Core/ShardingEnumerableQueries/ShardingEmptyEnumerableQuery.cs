using System.Linq.Expressions;

namespace Kurisu.EFSharding.Core.ShardingEnumerableQueries;

internal class ShardingEmptyEnumerableQuery<TSource> : IShardingEmptyEnumerableQuery
{
    private readonly Expression<Func<TSource, bool>> _whereExpression;

    public ShardingEmptyEnumerableQuery(Expression<Func<TSource, bool>> whereExpression)
    {
        _whereExpression = whereExpression;
    }

    public IQueryable EmptyQueryable()
    {
        return new List<TSource>(0).AsQueryable().Where(_whereExpression);
    }
}