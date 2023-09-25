using System.Linq.Expressions;
using System.Reflection;
using Kurisu.EFSharding.Core;
using Kurisu.EFSharding.Core.QueryRouteManagers;
using Microsoft.EntityFrameworkCore.Query.Internal;


namespace Kurisu.EFSharding.Extensions.ShardingQueryableExtensions;

/// <summary>
/// 分片查询额外扩展
/// </summary>
internal static class EntityFrameworkShardingQueryableExtension
{
    internal static readonly MethodInfo NotSupportMethodInfo
        = typeof(EntityFrameworkShardingQueryableExtension).GetTypeInfo().GetDeclaredMethods(nameof(UseUnionAllMerge)).Single();

    internal static readonly MethodInfo AsRouteMethodInfo
        = typeof(EntityFrameworkShardingQueryableExtension)
            .GetTypeInfo()
            .GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic)
            .Where(m => m.Name == nameof(AsRoute))
            .Single(m => m.GetParameters().Any(p => p.ParameterType == typeof(ShardingQueryableAsRouteOptions)));

    internal static readonly MethodInfo UseConnectionModeMethodInfo
        = typeof(EntityFrameworkShardingQueryableExtension)
            .GetTypeInfo()
            .GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic)
            .Where(m => m.Name == nameof(UseConnectionMode))
            .Single(m => m.GetParameters().Any(p => p.ParameterType == typeof(ShardingQueryableUseConnectionModeOptions)));


    internal static readonly MethodInfo AsSequenceModeMethodInfo
        = typeof(EntityFrameworkShardingQueryableExtension)
            .GetTypeInfo()
            .GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic)
            .Where(m => m.Name == nameof(AsSequence))
            .Single(m => m.GetParameters().Any(p => p.ParameterType == typeof(ShardingQueryableAsSequenceOptions)));


    /// <summary>
    /// 标记当前操作是不支持分片的可以自行才用union all
    /// </summary>
    /// <param name="source"></param>
    /// <typeparam name="TEntity"></typeparam>
    /// <returns></returns>
    public static IQueryable<TEntity> UseUnionAllMerge<TEntity>(this IQueryable<TEntity> source)
    {
        return
            source.Provider is EntityQueryProvider
                ? source.Provider.CreateQuery<TEntity>(
                    Expression.Call(
                        (Expression) null,
                        NotSupportMethodInfo.MakeGenericMethod(typeof(TEntity)),
                        source.Expression))
                : source;
    }

    /// <summary>
    /// 需要Route启用HintRoute:开启提示路由的前提下手动指定表、手动指定数据源
    /// </summary>
    /// <param name="source"></param>
    /// <param name="routeConfigure"></param>
    /// <typeparam name="TEntity"></typeparam>
    /// <returns></returns>
    public static IQueryable<TEntity> AsRoute<TEntity>(this IQueryable<TEntity> source, Action<ShardingRouteContext> routeConfigure)
    {
        var shardingQueryableAsRouteOptions = new ShardingQueryableAsRouteOptions(routeConfigure);

        return source.AsRoute(shardingQueryableAsRouteOptions);
    }

    internal static IQueryable<TEntity> AsRoute<TEntity>(this IQueryable<TEntity> source, ShardingQueryableAsRouteOptions shardingQueryableAsRouteOptions)
    {
        return source.Provider is EntityQueryProvider
            ? source.Provider.CreateQuery<TEntity>(
                Expression.Call(
                    (Expression) null,
                    AsRouteMethodInfo.MakeGenericMethod(typeof(TEntity)),
                    source.Expression,
                    Expression.Constant(shardingQueryableAsRouteOptions)))
            : source;
    }

    /// <summary>
    /// 设置连接而模式
    /// connectionMode = ConnectionModeEnum.SYSTEM_AUTO
    /// maxQueryConnectionsLimit就可以简单理解为同一个数据库的并发数
    /// </summary>
    /// <param name="source"></param>
    /// <param name="maxQueryConnectionsLimit">单次查询最大连接数</param>
    /// <param name="connectionMode">连接模式</param>
    /// <typeparam name="TEntity"></typeparam>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static IQueryable<TEntity> UseConnectionMode<TEntity>(this IQueryable<TEntity> source, int maxQueryConnectionsLimit, ConnectionModeEnum connectionMode = ConnectionModeEnum.Auto)
    {
        if (maxQueryConnectionsLimit <= 0)
            throw new ArgumentException($"{nameof(UseConnectionMode)} {nameof(maxQueryConnectionsLimit)} should >=1");
        var shardingQueryableUseConnectionModeOptions = new ShardingQueryableUseConnectionModeOptions(maxQueryConnectionsLimit);

        return UseConnectionMode(source, shardingQueryableUseConnectionModeOptions);
    }

    internal static IQueryable<TEntity> UseConnectionMode<TEntity>(this IQueryable<TEntity> source, ShardingQueryableUseConnectionModeOptions shardingQueryableUseConnectionModeOptions)
    {
        return
            source.Provider is EntityQueryProvider
                ? source.Provider.CreateQuery<TEntity>(
                    Expression.Call(
                        (Expression) null,
                        UseConnectionModeMethodInfo.MakeGenericMethod(typeof(TEntity)),
                        source.Expression,
                        Expression.Constant(shardingQueryableUseConnectionModeOptions)))
                : source;
    }

    /// <summary>
    /// 需要Route启用EntityQuery:不使用顺序查询,仅支持单表
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="source"></param>
    /// <returns></returns>
    public static IQueryable<TEntity> AsNoSequence<TEntity>(this IQueryable<TEntity> source)
    {
        return source.AsSequence(new ShardingQueryableAsSequenceOptions(true, false));
    }

    /// <summary>
    /// 需要Route启用EntityQuery:使用顺序查询,仅支持单表
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="source"></param>
    /// <param name="comparerWithShardingComparer">顺序查询的排序方式是否和表后缀一样</param>
    /// <returns></returns>
    public static IQueryable<TEntity> AsSequence<TEntity>(this IQueryable<TEntity> source,
        bool comparerWithShardingComparer)
    {
        return source.AsSequence(new ShardingQueryableAsSequenceOptions(comparerWithShardingComparer, true));
    }

    internal static IQueryable<TEntity> AsSequence<TEntity>(this IQueryable<TEntity> source, ShardingQueryableAsSequenceOptions shardingQueryableAsSequenceOptions)
    {
        return
            source.Provider is EntityQueryProvider
                ? source.Provider.CreateQuery<TEntity>(
                    Expression.Call(
                        (Expression) null,
                        AsSequenceModeMethodInfo.MakeGenericMethod(typeof(TEntity)),
                        source.Expression,
                        Expression.Constant(shardingQueryableAsSequenceOptions)))
                : source;
    }
}