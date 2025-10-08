using Kurisu.AspNetCore.Abstractions.DataAccess;
using Kurisu.Extensions.SqlSugar.Services;
using SqlSugar;

namespace Kurisu.Extensions.SqlSugar;

public interface ISqlSugarDbContext : IDbContext
{
    /// <summary>
    /// 使用原生sugar client
    /// </summary>
    /// <remarks>
    /// 将会脱离IDbContext的控制
    /// </remarks>
    public ISqlSugarClient Client { get; }

    /// <summary>
    /// 查询
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    ISugarQueryable<T> Queryable<T>();
    
    IQueryableSetting GetQueryableSetting();
    
    
    IUpdateable<T> Updateable<T>() where T : class, new();
    IDeleteable<T> Deleteable<T>() where T : class, new();

}