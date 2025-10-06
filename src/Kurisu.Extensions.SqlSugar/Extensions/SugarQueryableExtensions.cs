using System.Threading.Tasks;
using Kurisu.AspNetCore.Abstractions.DataAccess.Contract;
using SqlSugar;

namespace Kurisu.AspNetCore.DataAccess.SqlSugar.Extensions;

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
    /// <param name="input"></param>
    /// <returns></returns>
    public static async Task<Pagination<T>> ToPageListAsync<T>(this ISugarQueryable<T> queryable, PageDto input)
    {
        RefAsync<int> total = 0;
        var result = input.IsExport
            ? await queryable.ToPageListAsync(input.PageIndex, input.PageSize)
            : await queryable.ToPageListAsync(input.PageIndex, input.PageSize, total);

        return new Pagination<T>
        {
            PageIndex = input.PageIndex,
            PageSize = input.PageSize,
            Total = total,
            Data = result
        };
    }

    /// <summary>
    /// 获取数据分页
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="queryable"></param>
    /// <param name="input"></param>
    /// <returns></returns>
    public static Pagination<T> ToPageList<T>(this ISugarQueryable<T> queryable, PageDto input)
    {
        int total = 0;
        var result = input.IsExport
            ? queryable.ToPageList(input.PageIndex, input.PageSize)
            : queryable.ToPageList(input.PageIndex, input.PageSize, ref total);

        return new Pagination<T>
        {
            PageIndex = input.PageIndex,
            PageSize = input.PageSize,
            Total = total,
            Data = result
        };
    }
}