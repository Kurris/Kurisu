using System.Linq.Expressions;

namespace Kurisu.EFSharding.Sharding.MergeEngines.ShardingMergeEngines.Averages;


public static class AverageMergeEngineExtension
{
    public static IQueryable<Tuple<long, T>> BuildExpression<T>(this IQueryable<IGrouping<int, T>> queryable)
    {
        var sourceParameter = Expression.Parameter(typeof(IQueryable<IGrouping<int, T>>));
        var selectCall = BuildSelect<T>(sourceParameter);
        var lambda = Expression.Lambda<Func<IQueryable<IGrouping<int, T>>, IQueryable<Tuple<long, T>>>>(selectCall, sourceParameter);
        var compile = lambda.Compile();
        return compile(queryable);
    }
    private static MethodCallExpression BuildSelect<T>(this ParameterExpression sourceParameter)
    {
        var groupingType = typeof(IGrouping<int, T>);
        var selectMethod = ShardingQueryableMethods.Select.MakeGenericMethod(groupingType, typeof(Tuple<long, T>));
        var resultParameter = Expression.Parameter(groupingType);

        var longCountCall = BuildLongCount<T>(resultParameter);
        var sumCall = BuildSum<T>(resultParameter);
        var resultSelector = Expression.New(typeof(Tuple<long, T>).GetConstructors().First(), longCountCall, sumCall);
        //queryable.Expression,
        return Expression.Call(selectMethod, sourceParameter,Expression.Lambda(resultSelector, resultParameter));
    }

    private static MethodCallExpression BuildLongCount<T>(ParameterExpression resultParameter)
    {
        var asQueryableMethod =ShardingQueryableMethods.AsQueryable.MakeGenericMethod(typeof(T));
        var longCountMethod = ShardingQueryableMethods.LongCountWithoutPredicate.MakeGenericMethod(typeof(T));

        return Expression.Call(longCountMethod, Expression.Call(asQueryableMethod, resultParameter));
    }
    private static MethodCallExpression BuildSum<T>(ParameterExpression resultParameter)
    {
        var asQueryableMethod =ShardingQueryableMethods.AsQueryable.MakeGenericMethod(typeof(T));
        var sumMethod = ShardingQueryableMethods.GetSumWithoutSelector(typeof(T));

        return Expression.Call(sumMethod, Expression.Call(asQueryableMethod, resultParameter));
    }
}