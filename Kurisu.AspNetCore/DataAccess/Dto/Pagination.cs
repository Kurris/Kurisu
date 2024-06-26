﻿using System.Collections.Generic;

namespace Kurisu.AspNetCore.DataAccess.Dto;

/// <summary>
/// 分页
/// </summary>
/// <typeparam name="T">数据类型</typeparam>
public class Pagination<T>
{
    /// <summary>
    /// 页码
    /// </summary>
    public int PageIndex { get; set; }

    /// <summary>
    /// 页容量
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// 总条数
    /// </summary>
    public int Total { get; set; }

    /// <summary>
    /// 总页数
    /// </summary>
    public int TotalPages
    {
        get
        {
            if (Total > 0 && PageSize > 0)
            {
                return Total % PageSize == 0
                    ? Total / PageSize
                    : Total / PageSize + 1;
            }

            return 0;
        }
    }

    /// <summary>
    /// 是否有上一页
    /// </summary>
    public bool HasPrevious => PageIndex - 1 > 0;

    /// <summary>
    /// 是否有下一页
    /// </summary>
    public bool HasNext => PageIndex < TotalPages;

    /// <summary>
    /// 内容
    /// </summary>
    public List<T> Data { get; set; }
}

/// <summary>
/// 分页
/// </summary>
public class Pagination : Pagination<object>
{
}