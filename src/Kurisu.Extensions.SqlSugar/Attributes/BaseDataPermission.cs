using System;
using System.Collections.Generic;

namespace Kurisu.AspNetCore.DataAccess.SqlSugar.Attributes;

/// <summary>
/// 数据权限
/// </summary>
public abstract class BaseDataPermissionAttribute : Attribute
{
}

/// <summary>
/// 标识租户/基地
/// </summary>
public class TenantIdAttribute : BaseDataPermissionAttribute
{
}

/// <summary>
/// 定义获取数据权限
/// </summary>
public interface IGetDataPermissions
{
    /// <summary>
    /// 获取数据
    /// </summary>
    /// <returns></returns>
    Dictionary<string, List<Guid>> GetData<T>();
}