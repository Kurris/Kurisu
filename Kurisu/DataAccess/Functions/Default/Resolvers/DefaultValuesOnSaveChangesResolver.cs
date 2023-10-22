//using System;
//using System.Diagnostics.CodeAnalysis;
//using System.Linq;
//using Kurisu.Authentication.Abstractions;
//using Kurisu.DataAccess.Entity;
//using Kurisu.DataAccess.Functions.Default.Abstractions;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.EntityFrameworkCore.ChangeTracking;

//namespace Kurisu.DataAccess.Functions.Default.Resolvers;

///// <summary>
///// DbContext保存时触发默认值生成处理器
///// </summary>
//// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
//public class DefaultValuesOnSaveChangesResolver : IDefaultValuesOnSaveChangesResolver
//{
//    private readonly int _sub;

//    public DefaultValuesOnSaveChangesResolver(ICurrentUserInfoResolver currentUserInfoResolver)
//    {
//        _sub = currentUserInfoResolver.GetSubjectId<int>();
//    }

//    /// <summary>
//    /// 保存修改时
//    /// </summary>
//    /// <param name="dbContext"></param>
//    /// <exception cref="ArgumentOutOfRangeException"></exception>
//    [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
//    public void OnSaveChanges(DbContext dbContext)
//    {
//        var entries = dbContext.ChangeTracker.Entries()
//            .Where(x => x.Metadata.ClrType.IsAssignableTo(typeof(IBaseEntity)));

//        if (!entries.Any()) return;

//        foreach (var entry in entries)
//        {
//            switch (entry.State)
//            {
//                case EntityState.Deleted:
//                {
//                    OnDeleted(dbContext, entry);
//                    break;
//                }
//                case EntityState.Modified:
//                {
//                    OnModified(dbContext, entry);
//                    break;
//                }
//                case EntityState.Added:
//                {
//                    OnAdded(dbContext, entry);
//                    break;
//                }
//                case EntityState.Detached:
//                case EntityState.Unchanged:
//                default:
//                    break;
//            }
//        }
//    }


//    /// <summary>
//    /// 新增时
//    /// </summary>
//    /// <param name="dbContext"></param>
//    /// <param name="entry"></param>
//    protected virtual void OnAdded(DbContext dbContext, EntityEntry entry)
//    {
//        entry.CurrentValues[BaseEntityConstants.CreatedBy] = _sub;
//        //CreateTime datetime(3) default current_timestamp(3)
//        entry.CurrentValues[BaseEntityConstants.CreateTime] = DateTime.Now;
//    }

//    /// <summary>
//    /// 修改时
//    /// </summary>
//    /// <param name="dbContext"></param>
//    /// <param name="entry"></param>
//    protected virtual void OnModified(DbContext dbContext, EntityEntry entry)
//    {
//        entry.CurrentValues[BaseEntityConstants.ModifiedBy] = _sub;
//        //在Update后,如果查询使用了更改跟踪,那么实体将获取不到ModifiedTime时间
//        //HasDefaultValueSql(ModifiedTime datetime(3) default null on update current_timestamp(3))
//        entry.CurrentValues[BaseEntityConstants.ModifiedTime] = DateTime.Now;
//    }

//    /// <summary>
//    /// 删除时
//    /// </summary>
//    /// <param name="dbContext"></param>
//    /// <param name="entry"></param>
//    protected virtual void OnDeleted(DbContext dbContext, EntityEntry entry)
//    {
//        //当前实体继承ISoftDeleted
//        var type = entry.Entity.GetType();
//        if (type.IsAssignableTo(typeof(ISoftDeleted)))
//        {
//            //只有在IsSoftDeleted = false进行删除时才会软删除
//            //实体值主动设置为true,则物理删除
//            if (!Convert.ToBoolean(entry.CurrentValues[BaseEntityConstants.IsDeleted]))
//            {
//                //重置实体状态
//                entry.State = EntityState.Unchanged;

//                //设置为软删除
//                entry.Property(BaseEntityConstants.IsDeleted).IsModified = true;
//                entry.CurrentValues[BaseEntityConstants.IsDeleted] = true;

//                if (type.IsSubclassOf(typeof(BaseEntity<int>)))
//                {
//                    //修改人
//                    entry.Property(BaseEntityConstants.ModifiedBy).IsModified = true;
//                    entry.CurrentValues[BaseEntityConstants.ModifiedBy] = _sub;

//                    //修改时间
//                    entry.Property(BaseEntityConstants.ModifiedTime).IsModified = true;
//                    entry.CurrentValues[BaseEntityConstants.ModifiedTime] = DateTime.Now;
//                }
//            }
//        }
//    }
//}