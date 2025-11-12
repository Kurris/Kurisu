using Kurisu.AspNetCore.Abstractions.DataAccess.Contract;
using Kurisu.AspNetCore.Abstractions.DataAccess.Core.Context;
using Kurisu.Extensions.SqlSugar.Core.Context;
using SqlSugar;

namespace Kurisu.Extensions.SqlSugar.Extensions;

public static class IDbContextExtensions
{
    public static ISqlSugarDbContext AsSqlSugarDbContext(this IDbContext dbContext)
    {
        return (ISqlSugarDbContext)dbContext;
    }

    public static ISugarQueryable<T> Queryable<T>(this IDbContext dbContext)
    {
        return dbContext.AsSqlSugarDbContext().Queryable<T>();
    }

    public static IUpdateable<T> Updateable<T>(this IDbContext dbContext) where T : class, IEntity, new()
    {
        return dbContext.AsSqlSugarDbContext().Updateable<T>();
    }

    public static IDeleteable<T> Deleteable<T>(this IDbContext dbContext) where T : class, IEntity, new()
    {
        return dbContext.AsSqlSugarDbContext().Deleteable<T>();
    }
}