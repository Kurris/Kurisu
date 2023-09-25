using System.Diagnostics.CodeAnalysis;
using Kurisu.EFSharding.Core.RuntimeContexts;
using Kurisu.EFSharding.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Kurisu.EFSharding.Extensions.DbContextExtensions;

[ExcludeFromCodeCoverage]
internal static class DbContextExtensions
{
    public static bool RemoveShardingTables(this DbContext dbContext)
    {
        var contextModel = dbContext.GetService<IDesignTimeModel>().Model;

        var shardingRuntimeContext = dbContext.GetShardingRuntimeContext();
        var entityMetadataManager = shardingRuntimeContext.GetMetadataManager();

        var entityTypes = contextModel.GetEntityTypes();
        foreach (var entityType in entityTypes)
        {
            if (entityType.GetFieldValue("_data") is List<object> data)
            {
                data.Clear();
            }
        }

        var contextModelRelationalModel = contextModel.GetRelationalModel() as RelationalModel;
        var valueTuples = contextModelRelationalModel.Tables.Where(o => o.Value.EntityTypeMappings
                .Any(m => entityMetadataManager.IsShardingTable(m.EntityType.ClrType)))
            .Select(o => o.Key)
            .ToList();

        foreach (var t in valueTuples)
        {
            contextModelRelationalModel.Tables.Remove(t);
        }

        return contextModelRelationalModel.Tables.Count > 0;
    }

    /// <summary>
    /// 移除所有除了仅分库的
    /// </summary>
    /// <param name="dbContext"></param>
    public static bool RemoveWithoutShardingDatasourceOnly(this DbContext dbContext)
    {
        var contextModel = dbContext.GetService<IDesignTimeModel>().Model;

        var shardingRuntimeContext = dbContext.GetShardingRuntimeContext();
        var entityMetadataManager = shardingRuntimeContext.GetMetadataManager();

        var entityTypes = contextModel.GetEntityTypes();
        foreach (var entityType in entityTypes)
        {
            if (entityType.GetFieldValue("_data") is List<object> data)
            {
                data.Clear();
            }
        }

        var contextModelRelationalModel = contextModel.GetRelationalModel() as RelationalModel;
        var valueTuples =
            contextModelRelationalModel.Tables.Where(o => o.Value.EntityTypeMappings.Any(m => !entityMetadataManager.IsShardingDatasourceOnly(m.EntityType.ClrType))).Select(o => o.Key).ToList();

        foreach (var t in valueTuples)
        {
            contextModelRelationalModel.Tables.Remove(t);
        }

        return contextModelRelationalModel.Tables.Count > 0;
    }

    /// <summary>
    /// 移除所有的除了我指定的那个类型
    /// </summary>
    /// <param name="dbContext"></param>
    /// <param name="type"></param>
    public static void RemoveAllExceptTable(this DbContext dbContext, Type type)
    {
        var contextModel = dbContext.GetService<IDesignTimeModel>().Model;
        var entityTypes = contextModel.GetEntityTypes();
        foreach (var entityType in entityTypes)
        {
            if (entityType.GetFieldValue("_data") is List<object> data)
            {
                data.Clear();
            }
        }

        var contextModelRelationalModel = contextModel.GetRelationalModel() as RelationalModel;
        var valueTuples =
            contextModelRelationalModel.Tables
                .Where(o => o.Value.EntityTypeMappings.All(m => m.EntityType.ClrType != type))
                .Select(o => o.Key).ToList();
        for (int i = 0; i < valueTuples.Count; i++)
        {
            contextModelRelationalModel.Tables.Remove(valueTuples[i]);
        }
    }

    public static IEnumerable<object> GetPrimaryKeyValues<TEntity>(TEntity entity, IKey primaryKey) where TEntity : class
    {
        return primaryKey.Properties.Select(o => entity.GetPropertyValue(o.Name));
    }

    public static TEntity GetAttachedEntity<TEntity>(this DbContext context, TEntity entity) where TEntity : class
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        var entityPrimaryKey = context.Model.FindRuntimeEntityType(entity.GetType()).FindPrimaryKey();
        if (entityPrimaryKey == null)
        {
            return entity;
        }

        var primaryKeyValue = GetPrimaryKeyValues(entity, entityPrimaryKey).ToArray();
        if (primaryKeyValue.IsEmpty())
            return null;
        var dbContextDependencies = (IDbContextDependencies) typeof(DbContext).GetTypePropertyValue(context, "DbContextDependencies");
        var stateManager = dbContextDependencies.StateManager;

        var internalEntityEntry = stateManager.TryGetEntry(entityPrimaryKey, primaryKeyValue);

        if (internalEntityEntry == null)
            return null;
        return (TEntity) internalEntityEntry.Entity;
    }

    public static IShardingRuntimeContext GetShardingRuntimeContext(this DbContext dbContext)
    {
        var shardingRuntimeContext = dbContext.GetService<IShardingRuntimeContext>();
        if (shardingRuntimeContext == null)
        {
            throw new ShardingCoreInvalidOperationException($"Can not resolve type : {nameof(IShardingRuntimeContext)}");
        }

        return shardingRuntimeContext;
    }
}