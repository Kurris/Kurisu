using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Kurisu.Authentication.Abstractions;
using Kurisu.DataAccessor.Entity;
using Kurisu.DataAccessor.Functions.Default.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Kurisu.DataAccessor.Functions.Default.Resolvers;

/// <summary>
/// DbContext保存时触发默认值生成处理器
/// </summary>
// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
public class DefaultValuesOnSaveChangesResolver : IDefaultValuesOnSaveChangesResolver
{
    private readonly int _sub;

    public DefaultValuesOnSaveChangesResolver(ICurrentUserInfoResolver currentUserInfoResolver)
    {
        _sub = currentUserInfoResolver.GetSubjectId<int>();
    }

    /// <summary>
    /// 软删除属性名称
    /// </summary>
    // ReSharper disable once MemberCanBePrivate.Global
    protected const string SoftDeletedPropertyName = nameof(ISoftDeleted.IsDeleted);


    /// <summary>
    /// 保存修改时
    /// </summary>
    /// <param name="dbContext"></param>
    [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
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
        entry.CurrentValues[BaseEntityConstants.CreatedBy] = _sub;
        entry.CurrentValues[BaseEntityConstants.CreateTime] = DateTime.Now;
    }

    /// <summary>
    /// 修改时
    /// </summary>
    /// <param name="dbContext"></param>
    /// <param name="entry"></param>
    protected virtual void OnModified(DbContext dbContext, EntityEntry entry)
    {
        entry.CurrentValues[BaseEntityConstants.ModifiedBy] = _sub;
        entry.CurrentValues[BaseEntityConstants.ModifiedTime] = DateTime.Now;
    }

    /// <summary>
    /// 删除时
    /// </summary>
    /// <param name="dbContext"></param>
    /// <param name="entry"></param>
    protected virtual void OnDeleted(DbContext dbContext, EntityEntry entry)
    {
        //当前实体继承ISoftDeleted
        if (entry.Entity.GetType().IsAssignableTo(typeof(ISoftDeleted)))
        {
            //只有在IsSoftDeleted = false进行删除时才会软删除
            //实体值主动设置为true,则物理删除
            if (!Convert.ToBoolean(entry.CurrentValues[SoftDeletedPropertyName]))
            {
                //重置实体状态
                entry.State = EntityState.Unchanged;

                //设置为软删除
                entry.Property(SoftDeletedPropertyName).IsModified = true;
                entry.CurrentValues[SoftDeletedPropertyName] = true;

                //修改人
                entry.Property(BaseEntityConstants.ModifiedBy).IsModified = true;
                entry.CurrentValues[BaseEntityConstants.ModifiedBy] = _sub;

                //修改时间
                entry.Property(BaseEntityConstants.ModifiedTime).IsModified = true;
                entry.CurrentValues[BaseEntityConstants.ModifiedTime] = DateTime.Now;
            }
        }
    }
}