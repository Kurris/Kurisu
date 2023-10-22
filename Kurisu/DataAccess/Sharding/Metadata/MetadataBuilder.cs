//using System;
//using System.Linq.Expressions;
//using Microsoft.EntityFrameworkCore.Infrastructure;

//namespace Kurisu.DataAccess.Sharding.Metadata;

///// <summary>
///// 元数据建造者
///// </summary>
///// <typeparam name="TEntity"></typeparam>
//public sealed class MetadataBuilder<TEntity>
//    where TEntity : class, new()
//{
//    private readonly EntityMetadata _entityMetadata;

//    private MetadataBuilder(EntityMetadata entityMetadata)
//    {
//        _entityMetadata = entityMetadata;
//    }


//    public static MetadataBuilder<TEntity> Create(EntityMetadata metadata)
//    {
//        return new MetadataBuilder<TEntity>(metadata);
//    }

//    /// <summary>
//    /// 设置分表字段
//    /// </summary>
//    /// <typeparam name="TProperty"></typeparam>
//    /// <param name="propertyExpression"></param>
//    /// <returns></returns>
//    public MetadataBuilder<TEntity> Property<TProperty>(Expression<Func<TEntity, TProperty>> propertyExpression)
//    {
//        var propertyAccess = propertyExpression.GetPropertyAccess();
//        _entityMetadata.SetProperty(propertyAccess);
//        return this;
//    }
//}