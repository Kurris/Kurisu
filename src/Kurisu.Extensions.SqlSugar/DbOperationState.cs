using Kurisu.Extensions.ContextAccessor.Abstractions;

namespace Kurisu.Extensions.SqlSugar;

internal class DbOperationState : IContextable<DbOperationState>
{
    public DbOperationState()
    {
        IgnoreTenant = false;
        IgnoreSoftDeleted = false;
        EnableCrossTenant = false;
        EnableDataPermission = false;
        IgnoreSharding = true;
    }

    /// <summary>
    /// 忽略租户
    /// </summary>
    public bool IgnoreTenant { get; set; }

    /// <summary>
    /// 忽略逻辑删除
    /// </summary>
    public bool IgnoreSoftDeleted { get; set; }

    /// <summary>
    /// 跨tenant数据权限
    /// </summary>
    public bool EnableCrossTenant { get; set; }

    /// <summary>
    /// 是否启用数据权限
    /// </summary>
    public bool EnableDataPermission { get; set; }

    /// <summary>
    /// 是否运行分表
    /// </summary>
    public bool IgnoreSharding { get; set; }
}