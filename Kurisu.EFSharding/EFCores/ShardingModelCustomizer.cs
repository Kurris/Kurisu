using Kurisu.EFSharding.Core.Metadata.Manager;
using Kurisu.EFSharding.Core.VirtualRoutes.TableRoutes.RouteTails.Abstractions;
using Kurisu.EFSharding.Exceptions;
using Kurisu.EFSharding.Sharding.Abstractions;
using Kurisu.EFSharding.Extensions.DbContextExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Kurisu.EFSharding.EFCores;

/// <summary>
/// 分片DbContext Model自定义
/// </summary>
/// <remarks>
/// Customize --> DbContext.OnModelCreating
/// </remarks>
internal class ShardingModelCustomizer : ModelCustomizer
{
    public ShardingModelCustomizer(ModelCustomizerDependencies dependencies) : base(dependencies)
    {
    }

    public override void Customize(ModelBuilder modelBuilder, DbContext context)
    {
        base.Customize(modelBuilder, context);

        if (context is IShardingDbContext shardingDbContext && shardingDbContext.RouteTail.IsShardingTableQuery())
        {
            var shardingRuntimeContext = context.GetShardingRuntimeContext();
            var entityMetadataManager = shardingRuntimeContext.GetMetadataManager();
            var isMultiEntityQuery = shardingDbContext.RouteTail.IsMultiEntityQuery();
            if (!isMultiEntityQuery)
            {
                var singleQueryRouteTail = (ISingleQueryRouteTail) shardingDbContext.RouteTail;
                var tail = singleQueryRouteTail.GetTail();

                //设置分表
                var mutableEntityTypes = modelBuilder.Model.GetEntityTypes()
                    .Where(o => entityMetadataManager.IsShardingTable(o.ClrType)).ToArray();
                foreach (var entityType in mutableEntityTypes)
                {
                    MappingToTable(entityMetadataManager, entityType, modelBuilder, tail);
                }
            }
            else
            {
                var multiQueryRouteTail = (IMultiQueryRouteTail) shardingDbContext.RouteTail;
                var entityTypes = multiQueryRouteTail.GetEntityTypes();
                var mutableEntityTypes = modelBuilder.Model.GetEntityTypes().Where(o =>
                    entityMetadataManager.IsShardingTable(o.ClrType) && entityTypes.Contains(o.ClrType)).ToArray();
                foreach (var entityType in mutableEntityTypes)
                {
                    var queryTail = multiQueryRouteTail.GetEntityTail(entityType.ClrType);
                    if (queryTail != null)
                    {
                        MappingToTable(entityMetadataManager, entityType, modelBuilder, queryTail);
                    }
                }
            }
        }
    }

    private static void MappingToTable(IMetadataManager entityMetadataManager, IReadOnlyTypeBase mutableEntityType,
        ModelBuilder modelBuilder, string tail)
    {
        var clrType = mutableEntityType.ClrType;
        var metadataList = entityMetadataManager.TryGet(clrType);
        if (metadataList == null)
            throw new ShardingCoreInvalidOperationException($"not found entity type:[{clrType}]'s entity metadata");

        var metadata = metadataList.FirstOrDefault(x => x.IsTableMetadata);
        if (metadata == null)
            throw new ShardingCoreInvalidOperationException($"not found entity type:[{clrType}]'s entity metadata");

        if (metadata.IsView)
        {
            throw new ShardingCoreInvalidOperationException(
                $"entity type:[{clrType}]'s entity metadata is view cant remapping table name");
        }

        var shardingEntity = metadata.ClrType;
        //todo TableSeparator
        const string tableSeparator = "_"; //metadata.TableSeparator;
        var entity = modelBuilder.Entity(shardingEntity);
        var tableName = metadata.TableName;
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentNullException($"{shardingEntity}: not found logic table name。");

        entity.ToTable($"{tableName}{tableSeparator}{tail}", metadata.Schema);
    }
}