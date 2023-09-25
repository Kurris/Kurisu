using System.Linq.Expressions;

namespace Kurisu.EFSharding.Core.Metadata.Builder;

/// <summary>
/// 分片元数据builder
/// </summary>
/// <typeparam name="TEntity"></typeparam>
public interface IShardingMetadataBuilder<TEntity>
    where TEntity : class, new()
{
    IShardingMetadataBuilder<TEntity> SetProperty<TProperty>(Expression<Func<TEntity, TProperty>> propertyExpression);

    IShardingMetadataBuilder<TEntity> SetProperty(string propertyName);

    IShardingMetadataBuilder<TEntity> SetExtraProperty<TProperty>(Expression<Func<TEntity, TProperty>> propertyExpression);

    IShardingMetadataBuilder<TEntity> SetExtraProperty(string propertyName);
}