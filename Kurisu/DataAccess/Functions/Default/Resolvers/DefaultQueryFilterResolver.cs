//using System;
//using System.Linq.Expressions;
//using Kurisu.DataAccess.Functions.Default.Abstractions;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.EntityFrameworkCore.Metadata.Builders;
//using ISoftDeleted = Kurisu.DataAccess.Entity.ISoftDeleted;

//namespace Kurisu.DataAccess.Functions.Default.Resolvers;

///// <summary>
///// 默认查询过滤处理器
///// </summary>
//public class DefaultQueryFilterResolver : IQueryFilterResolver
//{
//    /// <summary>
//    /// 处理查询过滤器
//    /// </summary>
//    /// <param name="dbContext"></param>
//    /// <param name="entityType">实体类型</param>
//    /// <param name="builder">实体builder</param>
//    public virtual void HandleQueryFilter(DbContext dbContext, Type entityType, EntityTypeBuilder builder)
//    {
//        //软删除过滤
//        if (entityType.IsAssignableTo(typeof(ISoftDeleted)))
//        {
//            //dbContext如果不开启软删除不影响此处的条件
//            builder.HasQueryFilter(GetSoftDeletedExpression(entityType));
//            builder.Property(nameof(ISoftDeleted.IsDeleted))
//                .HasColumnType("bit(1)")
//                .HasComment("是否逻辑删除");
//        }
//    }

//    /// <summary>
//    /// 获取软删除表达式
//    /// </summary>
//    /// <param name="entityType"></param>
//    /// <param name="p"></param>
//    /// <returns></returns>
//    protected virtual LambdaExpression GetSoftDeletedExpression(Type entityType, ParameterExpression p = null)
//    {
//        var parameter = p ?? Expression.Parameter(entityType);

//        var property = Expression.Property(parameter, nameof(ISoftDeleted.IsDeleted));
//        var constant = Expression.Constant(false, typeof(bool));

//        var binary = Expression.Equal(property, constant);
//        var lambda = Expression.Lambda(binary, parameter);

//        return lambda;
//    }
//}