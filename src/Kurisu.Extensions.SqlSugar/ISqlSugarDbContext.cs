using Kurisu.AspNetCore.Abstractions.DataAccess;
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
    
    /// <summary>
    /// 更新 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    IUpdateable<T> Updateable<T>() where T : class, new();
    
    /// <summary>
    /// 删除
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    IDeleteable<T> Deleteable<T>() where T : class, new();

}