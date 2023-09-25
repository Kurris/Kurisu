using Kurisu.EFSharding.Core.Metadata.Model;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Kurisu.EFSharding.Core.Metadata.Manager;

/// <summary>
/// 元数据管理者
/// </summary>
public interface IMetadataManager
{
    /// <summary>
    /// 添加元数据
    /// </summary>
    /// <param name="metadata"></param>
    /// <returns></returns>
    bool AddMetadata(BaseShardingMetadata metadata);

    /// <summary>
    /// 是否分表
    /// </summary>
    /// <param name="entityType"></param>
    /// <returns></returns>
    bool IsShardingTable(Type entityType);

    /// <summary>
    /// 是否分库
    /// </summary>
    /// <param name="entityType"></param>
    /// <returns></returns>
    bool IsShardingDatasource(Type entityType);

    bool IsShardingDatasourceOnly(Type entityType);

    /// <summary>
    /// 尝试获取
    /// </summary>
    /// <param name="entityType"></param>
    /// <returns></returns>
    List<BaseShardingMetadata> TryGet(Type entityType);


    /// <summary>
    /// 是否是分片对象(包括分表或者分库)
    /// </summary>
    /// <param name="entityType"></param>
    /// <returns></returns>
    bool IsSharding(Type entityType);

    List<Type> GetAllShardingEntities();

    void InitModel(IEntityType entityType);
}