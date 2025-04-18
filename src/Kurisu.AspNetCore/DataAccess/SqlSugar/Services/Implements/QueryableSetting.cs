﻿using System;
using System.Linq;

namespace Kurisu.AspNetCore.DataAccess.SqlSugar.Services.Implements;

/// <summary>
/// 查询服务
/// </summary>
internal class QueryableSetting : IQueryableSetting
{
    /// <summary>
    /// 跨tenant数据权限
    /// </summary>
    public bool EnableCrossTenant { get; set; }

    /// <summary>
    /// 跨基地忽略的类型
    /// </summary>
    public Type[] CrossTenantIgnoreTypes { get; set; }

    /// <summary>
    /// 是否启用数据权限
    /// </summary>
    public bool EnableDataPermission { get; set; }

    /// <summary>
    /// 数据权限忽略的类型
    /// </summary>
    public Type[] DataPermissionIgnoreTypes { get; set; }

    /// <inheritdoc />
    public bool GetEnableDataPermission<T>()
    {
        return EnableDataPermission && !DataPermissionIgnoreTypes.Contains(typeof(T));
    }

    /// <inheritdoc />
    public bool GetEnableCrossTenant<T>()
    {
        return EnableCrossTenant && !CrossTenantIgnoreTypes.Contains(typeof(T));
    }
}