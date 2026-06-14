using Kurisu.Extensions.ContextAccessor.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Extensions.SqlSugar.Context;

/// <summary>
/// 默认数据库租户上下文访问器，从 UseTenant 作用域（如 [UseTenant]）获取租户ID。
/// 适用于匿名接口等无用户身份的场景。
/// </summary>
public class DefaultDbTenantAccessor : IDbTenantAccessor
{
    private readonly IContextSnapshotManager<DbOperationState> _snapshotManager;

    /// <summary>
    /// ctor
    /// </summary>
    public DefaultDbTenantAccessor(IServiceProvider serviceProvider)
    {
        _snapshotManager = serviceProvider.GetRequiredService<IContextSnapshotManager<DbOperationState>>();
    }

    /// <inheritdoc />
    public string GetTenantId()
    {
        return _snapshotManager.ContextAccessor.Current.UseTenantId;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetAccessibleTenantIds()
    {
        return [];
    }
}
