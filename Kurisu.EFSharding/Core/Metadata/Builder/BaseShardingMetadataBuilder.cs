using System.Linq.Expressions;
using Kurisu.EFSharding.Core.Metadata.Model;
using Kurisu.EFSharding.Extensions;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Kurisu.EFSharding.Core.Metadata.Builder;

internal abstract class BaseShardingMetadataBuilder<TEntity> : IShardingMetadataBuilder<TEntity>
    where TEntity : class, new()
{
    internal BaseShardingMetadataBuilder(BaseShardingMetadata metadata)
    {
        Metadata = metadata;
    }

    protected BaseShardingMetadata Metadata { get; }

    public virtual IShardingMetadataBuilder<TEntity> SetProperty<TProperty>(Expression<Func<TEntity, TProperty>> propertyExpression)
    {
        var propertyAccess = propertyExpression.GetPropertyAccess();
        Metadata.SetProperty(propertyAccess);
        return this;
    }

    public virtual IShardingMetadataBuilder<TEntity> SetProperty(string propertyName)
    {
        var propertyInfo = typeof(TEntity).GetUltimateShadowingProperty(propertyName);
        Metadata.SetProperty(propertyInfo);
        return this;
    }

    public virtual IShardingMetadataBuilder<TEntity> SetExtraProperty<TProperty>(Expression<Func<TEntity, TProperty>> propertyExpression)
    {
        var propertyAccess = propertyExpression.GetPropertyAccess();
        Metadata.SetExtraProperty(propertyAccess);
        return this;
    }

    public virtual IShardingMetadataBuilder<TEntity> SetExtraProperty(string propertyName)
    {
        var propertyInfo = typeof(TEntity).GetUltimateShadowingProperty(propertyName);
        Metadata.SetExtraProperty(propertyInfo);
        return this;
    }
}