using System;
using System.Diagnostics;
using Kurisu.Authentication.Abstractions;
using Kurisu.DataAccessor.Abstractions.Setting;
using Kurisu.DataAccessor.Entity;
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


        /// <summary>
        /// 保存修改时
        /// </summary>
        /// <param name="dbContext"></param>
        public virtual void OnSaveChanges(DbContext dbContext)
        {
            var sub = _currentUserInfoResolver.GetSubjectId();

            var entities = dbContext.ChangeTracker.Entries();
            foreach (var entity in entities)
            {
                // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
                switch (entity.State)
                {
                    case EntityState.Modified:
                        entity.Property(nameof(BaseEntity<int>.Updater)).CurrentValue = sub;
                        entity.Property(nameof(BaseEntity<object>.UpdateTime)).CurrentValue = DateTime.Now;
                        break;
                    case EntityState.Added:
                        entity.Property(nameof(BaseEntity<object>.Creator)).CurrentValue = sub;
                        entity.Property(nameof(BaseEntity<object>.CreateTime)).CurrentValue = DateTime.Now;
                        break;
                }
            }
        }
    }
}