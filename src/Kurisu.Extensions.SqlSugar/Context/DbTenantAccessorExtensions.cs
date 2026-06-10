using Kurisu.Extensions.ContextAccessor.Abstractions;

namespace Kurisu.Extensions.SqlSugar.Context;

internal static class DbTenantAccessorExtensions
{
    public static bool HasEffectiveTenant(this IDbTenantAccessor tenantAccessor, IContextSnapshotManager<DbOperationState> snapshotManager)
    {
        return !string.IsNullOrWhiteSpace(snapshotManager.ContextAccessor.Current.UseTenantId) || tenantAccessor.HasTenant;
    }

    public static string GetEffectiveTenantId(this IDbTenantAccessor tenantAccessor, IContextSnapshotManager<DbOperationState> snapshotManager)
    {
        var useTenantId = snapshotManager.ContextAccessor.Current.UseTenantId;
        return !string.IsNullOrWhiteSpace(useTenantId) ? useTenantId : tenantAccessor.GetTenantId();
    }
}
