using System.Collections.Concurrent;
using Kurisu.EFSharding.Core.Metadata.Model;
using Kurisu.EFSharding.Exceptions;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Kurisu.EFSharding.Core.Metadata.Manager;

/// <summary>
/// 默认分片对象元数据管理者实现
/// </summary>
internal class MetadataManager : IMetadataManager
{
    private readonly ConcurrentDictionary<Type, List<BaseShardingMetadata>> _caches = new();


    public bool AddMetadata(BaseShardingMetadata metadata)
    {
        if (!_caches.TryGetValue(metadata.ClrType, out var metadataList))
        {
            metadataList = new List<BaseShardingMetadata> {metadata};
            _caches.TryAdd(metadata.ClrType, metadataList);
        }
        else
        {
            metadataList.Add(metadata);
        }


        return true;
    }

    /// <summary>
    /// 对象是否是分表对象
    /// </summary>
    /// <param name="entityType"></param>
    /// <returns></returns>
    public bool IsShardingTable(Type entityType)
    {
        return _caches.TryGetValue(entityType, out var metadataList)
               && metadataList.Any(x => x.IsTableMetadata);
    }


    /// <summary>
    /// 对象是否是分库对象
    /// </summary>
    /// <param name="entityType"></param>
    /// <returns></returns>
    public bool IsShardingDatasource(Type entityType)
    {
        return _caches.TryGetValue(entityType, out var metadataList)
               && metadataList.Any(x => x.IsDatasourceMetadata);
    }

    public bool IsShardingDatasourceOnly(Type entityType)
    {
        if (!_caches.TryGetValue(entityType, out var metadataList))
            return false;
        return metadataList.Count == 1 && metadataList.First().IsDatasourceMetadata;
    }

    /// <summary>
    /// 对象获取没有返回null
    /// </summary>
    /// <param name="entityType"></param>
    /// <returns></returns>
    public List<BaseShardingMetadata> TryGet(Type entityType)
    {
        return !_caches.TryGetValue(entityType, out var metadataList)
            ? null
            : metadataList;
    }

    /// <summary>
    /// 是否是分片对象(包括分表或者分库)
    /// </summary>
    /// <param name="entityType"></param>
    /// <returns></returns>
    public bool IsSharding(Type entityType)
    {
        return _caches.TryGetValue(entityType, out var metadataList)
               && metadataList.Any(x => x.IsTableMetadata || x.IsDatasourceMetadata);
    }

    public List<Type> GetAllShardingEntities()
    {
        return _caches.Where(o => o.Value.Any(x => x.IsDatasourceMetadata || x.IsTableMetadata))
            .Select(o => o.Key)
            .ToList();
    }

    public void InitModel(IEntityType efEntityType)
    {
        if (_caches.TryGetValue(efEntityType.ClrType, out var metadataList))
        {
            foreach (var metadata in metadataList)
            {
                foreach (var metadataProperty in metadata.Properties)
                {
                    var propertyName = metadataProperty.Key;
                    var property = efEntityType.GetProperty(propertyName);
                    if (property.ValueGenerated != ValueGenerated.Never)
                    {
                        throw new ShardingCoreConfigException($"sharding table key:{propertyName} is not {nameof(ValueGenerated)}.{nameof(ValueGenerated.Never)}");
                    }
                }

                metadata.SetEntityModel(efEntityType);

                if (string.IsNullOrWhiteSpace(metadata.TableName))
                {
                    throw new ShardingCoreInvalidOperationException(
                        $"init model error, cant get logic table name:[{metadata.TableName}] from  entity:[{efEntityType.ClrType}],is view:[{metadata.IsView}]");
                }
            }
        }
    }
}