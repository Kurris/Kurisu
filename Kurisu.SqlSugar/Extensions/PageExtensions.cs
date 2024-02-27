using Kurisu.Core.DataAccess.Dto;
using SqlSugar;

namespace Kurisu.SqlSugar.Extensions;

/// <summary>
/// 分页扩展
/// </summary>
public static class PageExtensions
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
        var result = await queryable.ToPageListAsync(input.PageIndex, input.PageSize, total);

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
        var result = queryable.ToPageList(input.PageIndex, input.PageSize, ref total);

        return new Pagination<T>
        {
            PageIndex = input.PageIndex,
            PageSize = input.PageSize,
            Total = total,
            Data = result
        };
    }
}
