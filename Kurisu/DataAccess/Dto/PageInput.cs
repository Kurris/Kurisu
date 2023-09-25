using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable PossibleMultipleEnumeration

namespace Kurisu.DataAccess.Dto;

/// <summary>
/// 分页入参数
/// <remarks>
/// 默认:pageIndex:1 pageSize:20
/// </remarks>
/// </summary>
[SkipScan]
public class PageInput
{
    public PageInput()
    {
        PageIndex = 1;
        PageSize = 20;
    }

    public PageInput(int pageIndex, int pageSize)
    {
        PageIndex = pageIndex;
        PageSize = pageSize;
    }


    /// <summary>
    /// 页码
    /// </summary>
    public int PageIndex { get; set; }


    /// <summary>
    /// 页容量
    /// </summary>
    public int PageSize { get; set; }
}

/// <summary>
/// 分页入参数扩展
/// </summary>
public static class PageInputExtensions
{
    /// <summary>
    /// 获取数据分页
    /// </summary>
    /// <param name="data"></param>
    /// <param name="input"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static Pagination<T> ToPage<T>(this IEnumerable<T> data, PageInput input)
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

        return new Pagination<T>
        {
            PageIndex = pageIndex,
            PageSize = pageSize,
            Total = data.Count(),
            Data = data.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList()
        };
    }
}