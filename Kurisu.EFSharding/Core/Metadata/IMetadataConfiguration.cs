using Kurisu.EFSharding.Core.Metadata.Builder;

namespace Kurisu.EFSharding.Core.Metadata;

/// <summary>
/// 对象元数据分库配置 用来配置分库对象的一些信息
/// </summary>
/// <typeparam name="TEntity"></typeparam>
public interface IMetadataConfiguration<TEntity>
    where TEntity : class, new()
{
    /// <summary>
    /// 配置分库对象
    /// </summary>
    /// <param name="builder"></param>
    void Configure(IShardingMetadataBuilder<TEntity> builder);
}