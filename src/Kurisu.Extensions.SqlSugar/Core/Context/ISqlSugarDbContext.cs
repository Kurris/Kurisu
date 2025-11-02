using Kurisu.AspNetCore.Abstractions.DataAccess.Contract;
using SqlSugar;

namespace Kurisu.Extensions.SqlSugar.Core.Context;

public interface ISqlSugarDbContext
{
    /// <summary>
    /// code-first生成表结构
    /// </summary>
    /// <param name="tables"></param>
    void CodeFirstInitTables(params Type[] tables);
    
    
    ISqlSugarClient Client { get; }

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
    IUpdateable<T> Updateable<T>() where T : class, IEntity, new();

    /// <summary>
    /// 删除
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    IDeleteable<T> Deleteable<T>() where T : class, IEntity, new();
}