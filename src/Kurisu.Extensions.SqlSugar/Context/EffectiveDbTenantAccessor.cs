using Kurisu.Extensions.ContextAccessor.Abstractions;

namespace Kurisu.Extensions.SqlSugar.Context;

/// <summary>
/// IDbTenantAccessor 装饰器，统一处理 <see cref="DbOperationState.UseTenantId"/> 优先逻辑。
/// 所有 IDbTenantAccessor 实现均被此类包装，确保 GetTenantId() 自动优先返回 UseTenant 作用域覆盖值。
/// </summary>
internal class EffectiveDbTenantAccessor(IDbTenantAccessor inner, IContextSnapshotManager<DbOperationState> snapshotManager) : IDbTenantAccessor
{
    /// <inheritdoc />
    public string GetTenantId()
    {
        var useTenantId = snapshotManager.ContextAccessor.Current.UseTenantId;
        return !string.IsNullOrWhiteSpace(useTenantId) ? useTenantId : inner.GetTenantId();
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetAccessibleTenantIds()
    {
        return inner.GetAccessibleTenantIds();
    }
}
