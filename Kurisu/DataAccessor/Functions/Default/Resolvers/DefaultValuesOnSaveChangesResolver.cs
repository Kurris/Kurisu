using System;
using System.Linq;
using System.Linq.Expressions;
using Kurisu.Authentication.Abstractions;
using Kurisu.DataAccessor.Entity;
using Kurisu.DataAccessor.Functions.Default.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using ISoftDeleted = Kurisu.DataAccessor.Entity.ISoftDeleted;

namespace Kurisu.DataAccessor.Functions.Default.Resolvers
{
    /// <summary>
    /// DbContext保存时触发默认值生成处理器
    /// </summary>
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    public class DefaultValuesOnSaveChangesResolver : IDefaultValuesOnSaveChangesResolver
    {
        private readonly int _sub;

        public DefaultValuesOnSaveChangesResolver(ICurrentUserInfoResolver currentUserInfoResolver)
        {
            _sub = currentUserInfoResolver.GetSubjectId();
        }

        protected static readonly string SoftDeletedPropertyName;
        private static Func<IDbContextSoftDeleted, bool> _softDeletedCheckFunc;

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
            var entries = dbContext.ChangeTracker.Entries();

            foreach (var entry in entries.Where(x => x.State == EntityState.Added))
                OnAdded(dbContext, entry);

            foreach (var entry in entries.Where(x => x.State == EntityState.Modified))
                OnModified(dbContext, entry);

            foreach (var entry in entries.Where(x => x.State == EntityState.Deleted))
                OnDeleted(dbContext, entry);
        }


        /// <summary>
        /// 新增时
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="entry"></param>
        protected virtual void OnAdded(DbContext dbContext, EntityEntry entry)
        {
            entry.CurrentValues[nameof(BaseEntity<object>.Creator)] = _sub;
            entry.CurrentValues[nameof(BaseEntity<object>.CreateTime)] = DateTime.Now;
        }

        /// <summary>
        /// 修改时
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="entry"></param>
        protected virtual void OnModified(DbContext dbContext, EntityEntry entry)
        {
            entry.CurrentValues[nameof(BaseEntity<object>.Updater)] = _sub;
            entry.CurrentValues[nameof(BaseEntity<object>.UpdateTime)] = DateTime.Now;
        }

        /// <summary>
        /// 删除时
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="entry"></param>
        protected virtual void OnDeleted(DbContext dbContext, EntityEntry entry)
        {
            if (_softDeletedCheckFunc == null)
            {
                var parameter = Expression.Parameter(typeof(IDbContextSoftDeleted));
                var softDeletedMemberExpression = Expression.Property(Expression.Constant(dbContext), typeof(IDbContextSoftDeleted).GetProperties().First());
                var binaryExpression = Expression.Equal(softDeletedMemberExpression, Expression.Constant(true));

                var lambda = Expression.Lambda<Func<IDbContextSoftDeleted, bool>>(binaryExpression, parameter);
                _softDeletedCheckFunc = lambda.Compile();
            }

            if (entry.Entity.GetType().IsAssignableTo(typeof(ISoftDeleted)) && _softDeletedCheckFunc(dbContext as IDbContextSoftDeleted))
            {
                //实体值主动设置为true,则物理删除
                if (!Convert.ToBoolean(entry.CurrentValues[SoftDeletedPropertyName]))
                {
                    //重置实体状态
                    entry.State = EntityState.Unchanged;

                    entry.Property(SoftDeletedPropertyName).IsModified = true;
                    entry.CurrentValues[SoftDeletedPropertyName] = true;

                    entry.Property(nameof(BaseEntity<object>.Updater)).IsModified = true;
                    entry.CurrentValues[nameof(BaseEntity<object>.Updater)] = _sub;

                    entry.Property(nameof(BaseEntity<object>.UpdateTime)).IsModified = true;
                    entry.CurrentValues[nameof(BaseEntity<object>.UpdateTime)] = DateTime.Now;
                }
            }
        }
    }
}