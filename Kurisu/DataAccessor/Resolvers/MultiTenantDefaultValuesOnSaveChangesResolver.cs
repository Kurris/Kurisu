using System;
using System.Linq;
using Kurisu.Authentication.Abstractions;
using Kurisu.DataAccessor.Entity;
using Microsoft.EntityFrameworkCore;

namespace Kurisu.DataAccessor.Resolvers
{
    /// <summary>
    /// 多租户数据库上下文保存处理器
    /// </summary>
    public class MultiTenantDefaultValuesOnSaveChangesResolver : DefaultValuesOnSaveChangesResolver
    {
        private readonly ICurrentUserInfoResolver _currentUserInfoResolver;
        private readonly ICurrentTenantInfoResolver _currentTenantInfoResolver;

        public MultiTenantDefaultValuesOnSaveChangesResolver(ICurrentUserInfoResolver currentUserInfoResolver
            , ICurrentTenantInfoResolver currentTenantInfoResolver) : base(currentUserInfoResolver)
        {
            _currentUserInfoResolver = currentUserInfoResolver;
            _currentTenantInfoResolver = currentTenantInfoResolver;
        }

        protected static readonly string TenantProperty;

        static MultiTenantDefaultValuesOnSaveChangesResolver()
        {
            //SoftDeletedPropertyName = typeof(ISoftDeleted).GetProperties().First().Name;
            TenantProperty = typeof(ITenantId).GetProperties().First().Name;
        }

        public override void OnSaveChanges(DbContext dbContext)
        {
            var sub = _currentUserInfoResolver.GetSubjectId();

            var entries = dbContext.ChangeTracker.Entries();
            foreach (var entry in entries)
            {
                // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
                switch (entry.State)
                {
                    case EntityState.Modified:
                    {
                        entry.CurrentValues[nameof(BaseEntity<object>.Updater)] = sub;
                        entry.CurrentValues[nameof(BaseEntity<object>.UpdateTime)] = DateTime.Now;
                    }
                        break;
                    case EntityState.Added:
                    {
                        if (entry.Entity.GetType().IsAssignableTo(typeof(ITenantId)))
                        {
                            entry.CurrentValues[TenantProperty] = _currentTenantInfoResolver.GetTenantId();
                        }

                        entry.CurrentValues[nameof(BaseEntity<object>.Creator)] = sub;
                        entry.CurrentValues[nameof(BaseEntity<object>.CreateTime)] = DateTime.Now;
                    }
                        break;
                    case EntityState.Deleted:
                    {
                        if (entry.Entity.GetType().IsAssignableTo(typeof(ISoftDeleted)))
                        {
                            entry.CurrentValues[SoftDeletedPropertyName] = true;
                            entry.CurrentValues[nameof(BaseEntity<object>.Updater)] = sub;
                            entry.CurrentValues[nameof(BaseEntity<object>.UpdateTime)] = DateTime.Now;

                            entry.State = EntityState.Modified;
                        }
                    }
                        break;
                }
            }
        }
    }
}