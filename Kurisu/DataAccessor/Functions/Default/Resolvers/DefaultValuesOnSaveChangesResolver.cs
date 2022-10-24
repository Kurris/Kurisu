using System;
using System.Linq;
using System.Linq.Expressions;
using Kurisu.Authentication.Abstractions;
using Kurisu.DataAccessor.Entity;
using Kurisu.DataAccessor.Functions.Default.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

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
        private static Func<IDbContextSoftDeleted, bool> _dbContextSoftDeletedCheckHandler;

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
            entry.CurrentValues[nameof(BaseEntity<object>.CreatedBy)] = _sub;
            entry.CurrentValues[nameof(BaseEntity<object>.CreateTime)] = DateTime.Now;
        }

        /// <summary>
        /// 修改时
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="entry"></param>
        protected virtual void OnModified(DbContext dbContext, EntityEntry entry)
        {
            entry.CurrentValues[nameof(BaseEntity<object>.ModifiedBy)] = _sub;
            entry.CurrentValues[nameof(BaseEntity<object>.ModifiedTime)] = DateTime.Now;
        }

        /// <summary>
        /// 删除时
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="entry"></param>
        protected virtual void OnDeleted(DbContext dbContext, EntityEntry entry)
        {
            if (_dbContextSoftDeletedCheckHandler == null)
            {
                var parameter = Expression.Parameter(typeof(IDbContextSoftDeleted));
                var softDeletedMemberExpression = Expression.Property(Expression.Constant(dbContext), typeof(IDbContextSoftDeleted).GetProperties().First());
                //expression : p => p.IsEnableSoftDeleted == true
                var binaryExpression = Expression.Equal(softDeletedMemberExpression, Expression.Constant(true));

                var lambda = Expression.Lambda<Func<IDbContextSoftDeleted, bool>>(binaryExpression, parameter);
                _dbContextSoftDeletedCheckHandler = lambda.Compile();
            }

            //实体继承ISoftDeleted并且当前DbContext开启软删除
            if (entry.Entity.GetType().IsAssignableTo(typeof(ISoftDeleted)) && _dbContextSoftDeletedCheckHandler(dbContext as IDbContextSoftDeleted))
            {
                //只有在实体 IsSoftDeleted = false 时进行删除时才会软删除
                //实体值主动设置为true,则物理删除
                if (!Convert.ToBoolean(entry.CurrentValues[SoftDeletedPropertyName]))
                {
                    //重置实体状态
                    entry.State = EntityState.Unchanged;

                    entry.Property(SoftDeletedPropertyName).IsModified = true;
                    entry.CurrentValues[SoftDeletedPropertyName] = true;

                    entry.Property(nameof(BaseEntity<object>.ModifiedBy)).IsModified = true;
                    entry.CurrentValues[nameof(BaseEntity<object>.ModifiedBy)] = _sub;

                    entry.Property(nameof(BaseEntity<object>.ModifiedTime)).IsModified = true;
                    entry.CurrentValues[nameof(BaseEntity<object>.ModifiedTime)] = DateTime.Now;
                }
            }
        }
    }
}