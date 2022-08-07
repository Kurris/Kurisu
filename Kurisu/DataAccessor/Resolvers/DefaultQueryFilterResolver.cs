using System;
using System.Linq.Expressions;
using Kurisu.DataAccessor.Abstractions.Setting;
using Kurisu.DataAccessor.Entity;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kurisu.DataAccessor.Resolvers
{
    /// <summary>
    /// 默认查询过滤处理器
    /// </summary>
    public class DefaultQueryFilterResolver : IDefaultQueryFilterResolver
    {
        public void HandleQueryFilter(EntityTypeBuilder builder, Type entityType)
        {
            //软删除过滤
            if (entityType.IsAssignableTo(typeof(ISoftDeleted)))
            {
                var softDeletedProperty = typeof(ISoftDeleted).GetProperties()[0];

                var parameter = Expression.Parameter(entityType, "x");
                var property = Expression.Property(parameter, softDeletedProperty.Name);
                var constant = Expression.Constant(false, typeof(bool));

                var method = typeof(bool).GetMethod("Equals", new[] {softDeletedProperty.PropertyType})!;
                var call = Expression.Call(property, method, constant);
                var lambda = Expression.Lambda(call, parameter);

                builder.HasQueryFilter(lambda);
            }
        }
    }
}