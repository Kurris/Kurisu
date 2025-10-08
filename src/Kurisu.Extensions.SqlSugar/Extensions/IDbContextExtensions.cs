using Kurisu.AspNetCore.Abstractions.DataAccess;
using SqlSugar;

namespace Kurisu.Extensions.SqlSugar.Extensions;

public static class IDbContextExtensions
{
    public static ISqlSugarDbContext AsSqlSugarDbContext(this IDbContext dbContext)
    {
        return (ISqlSugarDbContext)dbContext;
    }

    /// <summary>
    /// 查询
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static ISugarQueryable<T> Queryable<T>(this IDbContext dbContext)
    {
        return dbContext.AsSqlSugarDbContext().Queryable<T>();
    }
}