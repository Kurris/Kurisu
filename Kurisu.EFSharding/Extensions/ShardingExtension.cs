using System.Linq.Expressions;
using Kurisu.EFSharding.Core.VirtualRoutes.DataSourceRoutes.RouteRuleEngine;
using Kurisu.EFSharding.Core.VirtualRoutes.TableRoutes;
using Kurisu.EFSharding.Core.VirtualRoutes.TableRoutes.RouteTails.Abstractions;
using Kurisu.EFSharding.Exceptions;
using Kurisu.EFSharding.Sharding.Abstractions;
using Kurisu.EFSharding.Extensions.DbContextExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Kurisu.EFSharding.Extensions;

public static class ShardingExtension
{
    private static readonly string ShardingTableDbContextFormat = $"sharding_{Guid.NewGuid():n}_";


    public static string FormatRouteTail2ModelCacheKey(this string originalTail)
    {
        return $"{ShardingTableDbContextFormat}{originalTail}";
    }

    public static string ShardingPrint(this Expression expression)
    {
        return expression?.Print();
    }

    public static string ShardingPrint(this IQueryable queryable)
    {
        return queryable.Expression.ShardingPrint();
    }

    /// <summary>
    /// 根据对象集合解析
    /// </summary>
    /// <typeparam name="TShardingDbContext"></typeparam>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="shardingDbContext"></param>
    /// <param name="entities"></param>
    /// <returns></returns>
    public static Dictionary<string, Dictionary<DbContext, IEnumerable<TEntity>>> BulkShardingEnumerable<TShardingDbContext, TEntity>(this TShardingDbContext shardingDbContext,
        IEnumerable<TEntity> entities) where TShardingDbContext : DbContext, IShardingDbContext where TEntity : class
    {
        if (entities.IsEmpty())
            return new Dictionary<string, Dictionary<DbContext, IEnumerable<TEntity>>>();
        var shardingRuntimeContext = shardingDbContext.GetShardingRuntimeContext();
        var entityType = typeof(TEntity);
        var routeTailFactory = shardingRuntimeContext.GetRouteTailFactory();
        var virtualDatasource = shardingRuntimeContext.GetVirtualDatasource();
        var datasourceRouteManager = shardingRuntimeContext.GetDatasourceRouteManager();
        var tableRouteManager = shardingRuntimeContext.GetTableRouteManager();
        var entityMetadataManager = shardingRuntimeContext.GetMetadataManager();
        var datasourceNames = new Dictionary<string, Dictionary<string, BulkDicEntry<TEntity>>>();
        var entitiesArray = entities as TEntity[] ?? entities.ToArray();
        var isShardingDatasource = entityMetadataManager.IsShardingDatasource(entityType);
        var isShardingTable = entityMetadataManager.IsShardingTable(entityType);
        if (!isShardingDatasource && !isShardingTable)
            return new Dictionary<string, Dictionary<DbContext, IEnumerable<TEntity>>>
            {
                {
                    virtualDatasource.DefaultDatasourceName,
                    new Dictionary<DbContext, IEnumerable<TEntity>>()
                    {
                        {
                            shardingDbContext.GetShardingExecutor().CreateGenericDbContext(entitiesArray[0]),
                            entitiesArray
                        }
                    }
                }
            };
        if (!isShardingDatasource)
        {
            var bulkDicEntries = new Dictionary<string, BulkDicEntry<TEntity>>();
            datasourceNames.Add(virtualDatasource.DefaultDatasourceName, bulkDicEntries);

            var tableRoute = tableRouteManager.GetRoute(entityType);
            var allTails = tableRoute.GetTails().ToHashSet();
            foreach (var entity in entitiesArray)
            {
                BulkShardingTableEnumerable(shardingDbContext, virtualDatasource.DefaultDatasourceName, bulkDicEntries,
                    routeTailFactory, tableRoute, allTails, entity);
            }
        }
        else
        {
            var virtualdatasourceRoute = datasourceRouteManager.GetRoute(entityType);
            var alldatasourceNames = virtualdatasourceRoute.GetAllDatasourceNames().ToHashSet();

            var metadataList = entityMetadataManager.TryGet(entityType);
            IVirtualTableRoute tableRoute = null;
            ISet<string> allTails = null;
            if (isShardingTable)
            {
                tableRoute = tableRouteManager.GetRoute(entityType);
                allTails = tableRoute.GetTails().ToHashSet();
            }

            foreach (var entity in entitiesArray)
            {
                var shardingDatasourceValue = entity.GetPropertyValue(metadataList.FirstOrDefault(x => x.IsDatasourceMetadata)?.Property?.Name);
                if (shardingDatasourceValue == null)
                    throw new ShardingCoreInvalidOperationException($" etities has null value of sharding data source value");
                var shardingDatasourceName = virtualdatasourceRoute.ShardingKeyToDataSourceName(shardingDatasourceValue);
                if (!alldatasourceNames.Contains(shardingDatasourceName))
                    throw new ShardingCoreException(
                        $" data source name :[{shardingDatasourceName}] all data source names:[{string.Join(",", alldatasourceNames)}]");
                if (!datasourceNames.TryGetValue(shardingDatasourceName, out var bulkDicEntries))
                {
                    bulkDicEntries = new Dictionary<string, BulkDicEntry<TEntity>>();
                    datasourceNames.Add(shardingDatasourceName, bulkDicEntries);
                }

                if (isShardingTable)
                {
                    BulkShardingTableEnumerable(shardingDbContext, shardingDatasourceName, bulkDicEntries,
                        routeTailFactory, tableRoute, allTails, entity);
                }
                else
                    BulkNoShardingTableEnumerable(shardingDbContext, shardingDatasourceName, bulkDicEntries,
                        routeTailFactory, entity);
            }
        }

        return datasourceNames.ToDictionary(o => o.Key,
            o => o.Value.Select(o => o.Value).ToDictionary(v => v.InnerDbContext, v => v.InnerEntities.Select(t => t)));
    }

    private static void BulkShardingTableEnumerable<TShardingDbContext, TEntity>(TShardingDbContext shardingDbContext, string datasourceName, Dictionary<string, BulkDicEntry<TEntity>> datasourceBulkDicEntries,
        IRouteTailFactory routeTailFactory, IVirtualTableRoute tableRoute, ISet<string> allTails, TEntity entity)
        where TShardingDbContext : DbContext, IShardingDbContext
        where TEntity : class
    {
        var entityType = typeof(TEntity);

        var shardingKey = entity.GetPropertyValue(tableRoute.Metadata.Property.Name);
        var tail = tableRoute.ToTail(shardingKey);
        if (!allTails.Contains(tail))
            throw new ShardingCoreException(
                $"sharding key route not match entity:{entityType.FullName},sharding key:{shardingKey},sharding tail:{tail}");

        var routeTail = routeTailFactory.Create(tail);
        var routeTailIdentity = routeTail.GetRouteTailIdentity();
        if (!datasourceBulkDicEntries.TryGetValue(routeTailIdentity, out var bulkDicEntry))
        {
            var dbContext = shardingDbContext.GetShareDbContext(datasourceName, routeTail);
            bulkDicEntry = new BulkDicEntry<TEntity>(dbContext, new LinkedList<TEntity>());
            datasourceBulkDicEntries.Add(routeTailIdentity, bulkDicEntry);
        }

        bulkDicEntry.InnerEntities.AddLast(entity);
    }

    private static void BulkNoShardingTableEnumerable<TShardingDbContext, TEntity>(TShardingDbContext shardingDbContext, string datasourceName, Dictionary<string, BulkDicEntry<TEntity>> datasourceBulkDicEntries, IRouteTailFactory routeTailFactory, TEntity entity)
        where TShardingDbContext : DbContext, IShardingDbContext
        where TEntity : class
    {
        var routeTail = routeTailFactory.Create(string.Empty);
        var routeTailIdentity = routeTail.GetRouteTailIdentity();
        if (!datasourceBulkDicEntries.TryGetValue(routeTailIdentity, out var bulkDicEntry))
        {
            var dbContext = shardingDbContext.GetShareDbContext(datasourceName, routeTail);
            bulkDicEntry = new BulkDicEntry<TEntity>(dbContext, new LinkedList<TEntity>());
            datasourceBulkDicEntries.Add(routeTailIdentity, bulkDicEntry);
        }

        bulkDicEntry.InnerEntities.AddLast(entity);
    }

    internal class BulkDicEntry<TEntity>
    {
        public BulkDicEntry(DbContext innerDbContext, LinkedList<TEntity> innerEntities)
        {
            InnerDbContext = innerDbContext;
            InnerEntities = innerEntities;
        }

        public DbContext InnerDbContext { get; }
        public LinkedList<TEntity> InnerEntities { get; }
    }

    public static Dictionary<DbContext, IEnumerable<TEntity>> BulkShardingTableEnumerable<TShardingDbContext, TEntity>(this TShardingDbContext shardingDbContext,
        IEnumerable<TEntity> entities) where TShardingDbContext : DbContext, IShardingDbContext
        where TEntity : class
    {
        var shardingRuntimeContext = shardingDbContext.GetShardingRuntimeContext();
        var entityMetadataManager = shardingRuntimeContext.GetMetadataManager();
        if (entityMetadataManager.IsShardingDatasource(typeof(TEntity)))
            throw new ShardingCoreInvalidOperationException(typeof(TEntity).FullName);
        //if (!entityMetadataManager.IsShardingTable(typeof(TEntity)))
        //    throw new ShardingCoreInvalidOperationException(typeof(TEntity).FullName);
        if (entities.IsEmpty())
            return new Dictionary<DbContext, IEnumerable<TEntity>>();
        return shardingDbContext.BulkShardingEnumerable(entities).First().Value;
    }

    /// <summary>
    /// 根据条件表达式解析
    /// </summary>
    /// <typeparam name="TShardingDbContext"></typeparam>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="shardingDbContext"></param>
    /// <param name="where"></param>
    /// <returns></returns>
    public static IDictionary<string, IEnumerable<DbContext>> BulkShardingExpression<TShardingDbContext, TEntity>(this TShardingDbContext shardingDbContext, Expression<Func<TEntity, bool>> where) where TEntity : class
        where TShardingDbContext : DbContext, IShardingDbContext
    {
        var shardingRuntimeContext = shardingDbContext.GetShardingRuntimeContext();
        var routeTailFactory = shardingRuntimeContext.GetRouteTailFactory();
        var dataSourceRouteManager = shardingRuntimeContext.GetDatasourceRouteManager();
        var tableRouteManager = shardingRuntimeContext.GetTableRouteManager();
        var entityMetadataManager = shardingRuntimeContext.GetMetadataManager(); // (IEntityMetadataManager)ShardingContainer.GetService(typeof(IEntityMetadataManager<>).GetGenericType0(shardingDbContext.GetType()));

        var dataSourceNames = dataSourceRouteManager.GetDatasourceNames(where);
        var result = new Dictionary<string, LinkedList<DbContext>>();
        var entityType = typeof(TEntity);

        foreach (var dataSourceName in dataSourceNames)
        {
            if (!result.TryGetValue(dataSourceName, out var dbContexts))
            {
                dbContexts = new LinkedList<DbContext>();
                result.Add(dataSourceName, dbContexts);
            }

            if (entityMetadataManager.IsShardingTable(entityType))
            {
                var physicTables = tableRouteManager.RouteTo(entityType, new DatasourceRouteResult(dataSourceName), new ShardingTableRouteConfig(predicate: @where));
                if (physicTables.IsEmpty())
                    throw new ShardingCoreException($"{where.ShardingPrint()} cant found any physic table");

                var dbs = physicTables.Select(o => shardingDbContext.GetShareDbContext(dataSourceName, routeTailFactory.Create(o.Tail))).ToList();
                foreach (var dbContext in dbs)
                {
                    dbContexts.AddLast(dbContext);
                }
            }
            else
            {
                var dbContext = shardingDbContext.GetShareDbContext(dataSourceName, routeTailFactory.Create(string.Empty));
                dbContexts.AddLast(dbContext);
            }
        }

        return result.ToDictionary(o => o.Key, o => (IEnumerable<DbContext>) o.Value);
    }

    public static IEnumerable<DbContext> BulkShardingTableExpression<TShardingDbContext, TEntity>(this TShardingDbContext shardingDbContext, Expression<Func<TEntity, bool>> where) where TEntity : class
        where TShardingDbContext : DbContext, IShardingDbContext
    {
        var shardingRuntimeContext = shardingDbContext.GetShardingRuntimeContext();
        var entityMetadataManager = shardingRuntimeContext.GetMetadataManager(); // (IEntityMetadataManager)ShardingContainer.GetService(typeof(IEntityMetadataManager<>).GetGenericType0(shardingDbContext.GetType()));
        if (entityMetadataManager.IsShardingDatasource(typeof(TEntity)))
            throw new ShardingCoreInvalidOperationException(typeof(TEntity).FullName);
        return shardingDbContext.BulkShardingExpression<TShardingDbContext, TEntity>(where).First().Value;
    }
}