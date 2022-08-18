using System.Linq;
using Kurisu.Authentication.Abstractions;
using Kurisu.DataAccessor.Entity;
using Kurisu.DataAccessor.Functions.Default.Resolvers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Kurisu.DataAccessor.Functions.MultiTenant.Resolvers
{
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

        protected static readonly string TenantProperty;

        static MultiTenantDefaultValuesOnSaveChangesResolver()
        {
            TenantProperty = typeof(ITenantId).GetProperties().First().Name;
        }


        protected override void OnAdded(DbContext dbContext, EntityEntry entry)
        {
            if (entry.Entity.GetType().IsAssignableTo(typeof(ITenantId)))
            {
                entry.CurrentValues[TenantProperty] = _currentTenantInfoResolver.GetTenantId();
            }

            base.OnAdded(dbContext, entry);
        }
    }
}