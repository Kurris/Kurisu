using System;

namespace Kurisu.AspNetCore.DataAccess.SqlSugar.Services;

/// <summary>
/// 查询配置
/// </summary>
public interface IQueryableSetting
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

    /// <summary>
    /// 获取是否启用数据权限
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public bool GetEnableDataPermission<T>();

    /// <summary>
    /// 获取是否启用跨租户
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public bool GetEnableCrossTenant<T>();
}