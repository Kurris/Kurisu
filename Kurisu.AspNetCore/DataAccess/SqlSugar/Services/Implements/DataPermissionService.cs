using System.Collections.Generic;

namespace Kurisu.AspNetCore.DataAccess.SqlSugar.Services.Implements;

/// <summary>
/// 数据权限
/// </summary>
public class DataPermissionService
{
    /// <summary>
    /// 启用
    /// </summary>
    public bool Enable { get; set; }

    /// <summary>
    /// where条件
    /// </summary>
    public List<string> Wheres { get; } = new();

    /// <summary>
    /// 使用sql where 拼接
    /// </summary>
    public bool UseSqlWhere { get; set; }
}
