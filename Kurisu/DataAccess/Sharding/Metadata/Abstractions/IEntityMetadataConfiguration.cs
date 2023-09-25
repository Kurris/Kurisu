namespace Kurisu.DataAccess.Sharding.Metadata.Abstractions;

public interface IEntityMetadataConfiguration<TEntity> where TEntity : class, new()
{
    /// <summary>
    /// 元数据创建时
    /// </summary>
    /// <param name="builder"></param>
    void OnMetadataBuilder(MetadataBuilder<TEntity> builder);
}