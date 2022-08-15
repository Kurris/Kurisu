using System;
using System.Linq.Expressions;
using Kurisu.DataAccessor.Entity;
using Kurisu.DataAccessor.Functions.Default.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ISoftDeleted = Kurisu.DataAccessor.Entity.ISoftDeleted;

namespace Kurisu.DataAccessor.Functions.Default.Resolvers
{
    /// <summary>
    /// 默认查询过滤处理器
    /// </summary>
    public class DefaultQueryFilterResolver : IQueryFilterResolver
    {
        /// <summary>
        /// 处理查询过滤器
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="entityType">实体类型</param>
        /// <param name="builder">实体builder</param>
        public virtual void HandleQueryFilter(DbContext dbContext, Type entityType, EntityTypeBuilder builder)
        {
            //软删除过滤
            if (entityType.IsAssignableTo(typeof(ISoftDeleted)))
            {
                builder.HasQueryFilter(GetSoftDeletedExpression(entityType));
            }
        }

        /// <summary>
        /// 获取软删除表达式
        /// </summary>
        /// <param name="entityType"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        protected virtual LambdaExpression GetSoftDeletedExpression(Type entityType, ParameterExpression p = null)
        {
            var softDeletedProperty = typeof(ISoftDeleted).GetProperties()[0];

            var parameter = p ?? Expression.Parameter(entityType);

            var property = Expression.Property(parameter, softDeletedProperty.Name);
            var constant = Expression.Constant(false, typeof(bool));

            var binary = Expression.Equal(property, constant);
            var lambda = Expression.Lambda(binary, parameter);

            return lambda;
        }
    }
}