using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kurisu.DataAccessor.Resolvers.Abstractions
{
    /// <summary>
    /// 默认查询过滤处理器
    /// </summary>
    public interface IQueryFilterResolver
    {
        /// <summary>
        /// 处理软删除实体
        /// </summary>
        /// <param name="dbContext">上下文</param>
        /// <param name="entityType">实体类型</param>
        /// <param name="builder">实体构建器</param>
        void HandleQueryFilter(DbContext dbContext, Type entityType, EntityTypeBuilder builder);
    }
}