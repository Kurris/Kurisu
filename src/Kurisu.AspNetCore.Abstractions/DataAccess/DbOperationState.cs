using Kurisu.AspNetCore.Abstractions.State;

namespace Kurisu.AspNetCore.Abstractions.DataAccess;

public class DbOperationState : ICopyable<DbOperationState>
{
    public DbOperationState()
    {
        CrossTenantIgnoreTypes = [];
        DataPermissionIgnoreTypes = [];
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


    public bool GetEnableDataPermission<T>()
    {
        return EnableDataPermission && !DataPermissionIgnoreTypes.Contains(typeof(T));
    }


    public bool GetEnableCrossTenant<T>()
    {
        return EnableCrossTenant && !CrossTenantIgnoreTypes.Contains(typeof(T));
    }
}