using Kurisu.Authentication.Abstractions;
using Kurisu.DataAccess.Entity;
using Kurisu.DataAccess.Functions.Default.Resolvers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Kurisu.DataAccess.Functions.MultiTenant.Resolvers;

/// <summary>
/// 多租户数据库上下文保存处理器
/// </summary>
public class MultiTenantDefaultValuesOnSaveChangesResolver : DefaultValuesOnSaveChangesResolver
{
    private readonly ICurrentTenantInfoResolver _currentTenantInfoResolver;

    public MultiTenantDefaultValuesOnSaveChangesResolver(ICurrentUserInfoResolver currentUserInfoResolver
        , ICurrentTenantInfoResolver currentTenantInfoResolver) : base(currentUserInfoResolver)
    {
        _currentTenantInfoResolver = currentTenantInfoResolver;
    }

    protected const string TenantProperty = nameof(ITenantId<int>.TenantId);

    protected override void OnAdded(DbContext dbContext, EntityEntry entry)
    {
        if (entry.Entity.GetType().IsAssignableTo(typeof(ITenantId<int>)))
        {
            entry.CurrentValues[TenantProperty] = _currentTenantInfoResolver.GetTenantId<int>();
        }

        base.OnAdded(dbContext, entry);
    }
}