using Kurisu.AspNetCore.Abstractions.DataAccess.Contract;
using SqlSugar;

namespace Kurisu.Extensions.SqlSugar.Core.Context;

public interface ISqlSugarDbContext
{
    /// <summary>
    /// 获取操作客户端
    /// </summary>
    /// <returns></returns>
    ISqlSugarClient GetClient();

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