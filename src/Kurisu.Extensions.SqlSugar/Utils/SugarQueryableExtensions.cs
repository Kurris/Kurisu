using Kurisu.AspNetCore.Abstractions.DataAccess.Contract.Page;
using SqlSugar;

namespace Kurisu.Extensions.SqlSugar.Utils;

/// <summary>
/// 查询扩展
/// </summary>
public static class SugarQueryableExtensions
{
    /// <summary>
    /// 获取数据分页
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="queryable"></param>
    /// <param name="query"></param>
    /// <returns></returns>
    public static async Task<Pagination<T>> ToPageListAsync<T>(this ISugarQueryable<T> queryable, PageQuery query)
    {
        RefAsync<int> total = 0;
        var result = query.IsExport
            ? await queryable.ToPageListAsync(query.PageIndex, query.PageSize)
            : await queryable.ToPageListAsync(query.PageIndex, query.PageSize, total);

        return new Pagination<T>
        {
            PageIndex = query.PageIndex,
            PageSize = query.PageSize,
            Total = total,
            Data = result
        };
    }

    /// <summary>
    /// 获取数据分页
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="queryable"></param>
    /// <param name="query"></param>
    /// <returns></returns>
    public static Pagination<T> ToPageList<T>(this ISugarQueryable<T> queryable, PageQuery query)
    {
        int total = 0;
        var result = query.IsExport
            ? queryable.ToPageList(query.PageIndex, query.PageSize)
            : queryable.ToPageList(query.PageIndex, query.PageSize, ref total);

        return new Pagination<T>
        {
            PageIndex = query.PageIndex,
            PageSize = query.PageSize,
            Total = total,
            Data = result
        };
    }
}