using Kurisu.AspNetCore.Abstractions.Cache;
using Kurisu.AspNetCore.Abstractions.DataAccess;
using Kurisu.AspNetCore.Abstractions.DataAccess.Core;
using Kurisu.Extensions.ContextAccessor.Abstractions;

namespace Kurisu.Extensions.SqlSugar.Context.Impl;

internal sealed class SqlSugarMethodCacheScopeContributor(
    IDbTenantAccessor tenantAccessor,
    IDbConnectionStringManager connectionManager,
    IContextSnapshotManager<DbOperationState> snapshotManager) : IMethodCacheScopeContributor
{
    public void Contribute(MethodCacheScope scope)
    {
        var state = snapshotManager.ContextAccessor.Current
            ?? throw new InvalidOperationException("SqlSugar 方法缓存需要初始化上下文生命周期；后台任务请使用 InitLifecycle()。");
        scope.Dimensions["tenant"] = tenantAccessor.GetTenantId();
        scope.Dimensions["datasource"] = connectionManager.Current;
        // 第一版绕过特殊过滤查询，避免精确失效遗漏这些缓存变体。
        scope.BypassCache |= state.IgnoreTenant || state.EnableCrossTenant || state.IgnoreSoftDeleted ||
                             state.EnableDataPermission || !state.IgnoreSharding;
    }
}
