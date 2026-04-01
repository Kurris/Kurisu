using Kurisu.AspNetCore.Abstractions.Authentication;
using Kurisu.AspNetCore.Abstractions.DataAccess.Contract.Field;
using Kurisu.AspNetCore.Abstractions.DataAccess.Core.Context;
using Kurisu.Extensions.ContextAccessor.Abstractions;
using Kurisu.Extensions.SqlSugar.Core.Context;
using Kurisu.Extensions.SqlSugar.Sharding;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;

namespace Kurisu.Extensions.SqlSugar.Utils;

public static class IDbContextExtensions
{
    /// <summary>
    /// 转SqlSugar特定操作
    /// </summary>
    /// <param name="dbContext"></param>
    /// <returns></returns>
    public static ISqlSugarDbContext AsSqlSugarDbContext(this IDbContext dbContext)
    {
        return (ISqlSugarDbContext)dbContext;
    }

    public static ISugarQueryable<T> Queryable<T>(this IDbContext dbContext, bool ignoreSharding = false)
    {
        if (!ignoreSharding)
        {
            return dbContext.UseShardingQueryable<T>();
        }

        return dbContext.DefaultQueryable<T>();
    }

    private static ISugarQueryable<T> DefaultQueryable<T>(this IDbContext dbContext)
    {
        return dbContext.AsSqlSugarDbContext().Queryable<T>();
    }


    public static ISugarQueryable<T> UseShardingQueryable<T>(this IDbContext dbContext)
    {
        if (dbContext.ServiceProvider.GetService<IContextAccessor<DbOperationState>>().Current.IgnoreSharding)
        {
            return dbContext.DefaultQueryable<T>();
        }

        var type = typeof(T);
        if (!type.IsAssignableTo(typeof(IShardingRoute)) || !type.IsAssignableTo(typeof(ITenantId)))
        {
            return dbContext.DefaultQueryable<T>();
        }

        // 通过当前用户获取租户ID,确定分表后缀
        var currentUser = dbContext.ServiceProvider.GetRequiredService<ICurrentUser>();
        var tenantId = currentUser.GetTenantId();

        // 内存获取分表后缀
        var memoryCache = dbContext.ServiceProvider.GetService<IMemoryCache>();

        var cacheKey = $"sharding:tenant:{tenantId}";
        string suffix = null;

        if (memoryCache.TryGetValue<string>(cacheKey, out var mval))
        {
            suffix = mval;
        }

        if (string.IsNullOrEmpty(suffix))
        {
            throw new InvalidOperationException($"无法为租户 {tenantId} 解析分表后缀");
        }

        return dbContext.Queryable<T>(suffix);
    }


    public static ISugarQueryable<T> Queryable<T>(this IDbContext dbContext, string suffix)
    {
        if (string.IsNullOrEmpty(suffix))
        {
            throw new ArgumentNullException(nameof(suffix));
        }
        var sqlsugarDbContext = dbContext.AsSqlSugarDbContext();
        var originalTable = sqlsugarDbContext.GetClient().EntityMaintenance.GetTableName<T>();
        var table = $"{originalTable}_{suffix}";

        return sqlsugarDbContext.Queryable<T>().AS(table);
    }
}