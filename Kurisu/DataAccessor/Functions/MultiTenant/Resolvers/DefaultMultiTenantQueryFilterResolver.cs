using System;
using System.Linq;
using System.Linq.Expressions;
using Kurisu.DataAccessor.Entity;
using Kurisu.DataAccessor.Functions.Default.Resolvers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kurisu.DataAccessor.Functions.MultiTenant.Resolvers;

/// <summary>
/// 多租户查询过滤处理器
/// </summary>
// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
public class DefaultMultiTenantQueryFilterResolver : DefaultQueryFilterResolver
{
    public override void HandleQueryFilter(DbContext dbContext, Type entityType, EntityTypeBuilder builder)
    {
        base.HandleQueryFilter(dbContext, entityType, builder);

        if (entityType.IsAssignableTo(typeof(ITenantId)))
        {
            builder.HasQueryFilter(GetMultiTenantExpression(dbContext, entityType));
        }
    }


    /// <summary>
    /// 获取多租户查询表达式(兼容软删除)
    /// </summary>
    /// <param name="dbContext">上下文</param>
    /// <param name="entityType">实体类型</param>
    /// <param name="p">表达式类型参数</param>
    /// <returns></returns>
    protected virtual LambdaExpression GetMultiTenantExpression(DbContext dbContext, Type entityType, ParameterExpression p = null)
    {
        var tenantProperty = typeof(ITenantId).GetProperties()[0];

        var parameter = p ?? Expression.Parameter(entityType);
        var property = Expression.Property(parameter, tenantProperty.Name);

        //HasQueryFilter可使用DbContext实例
        var tenant = Expression.Property(Expression.Constant(dbContext), typeof(ITenantId).GetProperties().First());
        var binaryExpression = Expression.Equal(property, tenant);
        var lambda = Expression.Lambda(binaryExpression, parameter);

        if (entityType.IsAssignableTo(typeof(ISoftDeleted)))
        {
            var left = lambda.Body;
            var right = base.GetSoftDeletedExpression(entityType, parameter).Body;

            lambda = Expression.Lambda(Expression.AndAlso(left, right), parameter);
        }

        return lambda;
    }
}