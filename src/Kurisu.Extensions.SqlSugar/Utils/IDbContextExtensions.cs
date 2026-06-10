using Kurisu.AspNetCore.Abstractions.DataAccess.Contract.Field;
using Kurisu.AspNetCore.Abstractions.DataAccess.Core.Context;
using Kurisu.Extensions.ContextAccessor.Abstractions;
using Kurisu.Extensions.SqlSugar.Core.Context;
using Kurisu.Extensions.SqlSugar.Context;
using Kurisu.Extensions.SqlSugar.Sharding;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;

namespace Kurisu.Extensions.SqlSugar.Utils;

public static class IDbContextExtensions
{
    /// <summary>
    /// 转换为 SqlSugar 专用 DbContext
    /// </summary>
    public static ISqlSugarDbContext AsSqlSugarDbContext(this IDbContext dbContext)
    {
        return (ISqlSugarDbContext)dbContext;
    }

    /// <summary>
    /// 获取查询构造器，若实体标记了 <see cref="EnableShardingAttribute"/> 则自动应用分表
    /// </summary>
    public static ISugarQueryable<T> Queryable<T>(this IDbContext dbContext)
    {
        if (!ShardingEntityHelper.IsEnabled<T>())
            return dbContext.DefaultQueryable<T>();

        if (dbContext.ServiceProvider.GetRequiredService<IContextAccessor<DbOperationState>>().Current.IgnoreSharding)
            return dbContext.DefaultQueryable<T>();

        if (!typeof(T).IsAssignableTo(typeof(ITenantId)))
            return dbContext.DefaultQueryable<T>();

        return dbContext.UseShardingQueryable<T>();
    }

    private static ISugarQueryable<T> DefaultQueryable<T>(this IDbContext dbContext)
    {
        return dbContext.AsSqlSugarDbContext().Queryable<T>();
    }

    private static ISugarQueryable<T> UseShardingQueryable<T>(this IDbContext dbContext)
    {
        var tenantAccessor = dbContext.ServiceProvider.GetRequiredService<IDbTenantAccessor>();
        var tenantId = tenantAccessor.GetTenantId();
        if (string.IsNullOrEmpty(tenantId))
        {
            throw new InvalidOperationException("未能解析分表租户ID，请确认已配置数据库租户访问器");
        }

        var resolver = dbContext.ServiceProvider.GetRequiredService<IShardingRouteResolver>();
        var suffix = resolver.GetSuffix(tenantId);

        return dbContext.Queryable<T>(suffix);
    }

    private static ISugarQueryable<T> Queryable<T>(this IDbContext dbContext, string suffix)
    {
        var sqlsugarDbContext = dbContext.AsSqlSugarDbContext();
        var originalTable = sqlsugarDbContext.GetClient().EntityMaintenance.GetTableName<T>();
        return sqlsugarDbContext.Queryable<T>().AS($"{originalTable}_{suffix}");
    }
}
