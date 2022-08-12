using System;
using System.Linq;
using Kurisu.Authentication.Abstractions;
using Kurisu.DataAccessor.Entity;
using Kurisu.DataAccessor.Resolvers.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Kurisu.DataAccessor.Resolvers
{
    /// <summary>
    /// DbContext保存时触发默认值生成处理器
    /// </summary>
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    public class DefaultValuesOnSaveChangesResolver : IDefaultValuesOnSaveChangesResolver
    {
        private readonly ICurrentUserInfoResolver _currentUserInfoResolver;

        public DefaultValuesOnSaveChangesResolver(ICurrentUserInfoResolver currentUserInfoResolver)
        {
            _currentUserInfoResolver = currentUserInfoResolver;
        }

        protected static readonly string SoftDeletedPropertyName;

        static DefaultValuesOnSaveChangesResolver()
        {
            SoftDeletedPropertyName = typeof(ISoftDeleted).GetProperties().First().Name;
        }

        /// <summary>
        /// 保存修改时
        /// </summary>
        /// <param name="dbContext"></param>
        public virtual void OnSaveChanges(DbContext dbContext)
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