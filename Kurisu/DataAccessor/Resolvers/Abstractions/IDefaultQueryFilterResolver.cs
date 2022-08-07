using System;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kurisu.DataAccessor.Abstractions.Setting
{
    /// <summary>
    /// 默认查询过滤处理器
    /// </summary>
    public interface IQueryFilterResolver
    {
        /// <summary>
        /// 处理软删除实体
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="entityType"></param>
        void HandleQueryFilter(EntityTypeBuilder builder, Type entityType);
    }
}