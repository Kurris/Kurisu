using Kurisu.AspNetCore.Abstractions.DataAccess.Contract;

// ReSharper disable once CheckNamespace
namespace Kurisu.AspNetCore.Extensions;

/// <summary>
/// 分页扩展
/// </summary>
public static class PageDtoExtensions
{
    /// <summary>
    /// 获取数据分页
    /// </summary>
    /// <param name="data"></param>
    /// <param name="input"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static Pagination<T> ToPage<T>(this IEnumerable<T> data, PageDto input)
    {
        return data.ToPage(input.PageIndex, input.PageSize);
    }

    /// <summary>
    /// 获取数据分页
    /// </summary>
    /// <param name="data"></param>
    /// <param name="pageIndex"></param>
    /// <param name="pageSize"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static Pagination<T> ToPage<T>(this IEnumerable<T> data, int pageIndex, int pageSize)
    {
        if (pageIndex <= 0) throw new ArgumentOutOfRangeException(nameof(pageIndex));
        if (pageSize <= 0) throw new ArgumentOutOfRangeException(nameof(pageSize));

        var list = data.ToList();

        return new Pagination<T>
        {
            PageIndex = pageIndex,
            PageSize = pageSize,
            Total = list.Count,
            Data = list.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList()
        };
    }
}