using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Kurisu.DataAccessor.Entity;
using Kurisu.DataAccessor.Functions.ReadWriteSplit.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.DataAccessor.Functions.Default.Internal;

/// <summary>
/// 数据操作(写)
/// </summary>
// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
[SkipScan]
public class WriteImplementation : ReadImplementation, IAppMasterDb
{
    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="dbContext"></param>
    public WriteImplementation(DbContext dbContext) : base(dbContext)
    {
        DbContext = dbContext;
    }

    protected override DbContext DbContext { get; }

    public DbContext GetMasterDbContext() => DbContext;

    public override IQueryable<T> Queryable<T>()
    {
        return DbContext.Set<T>().AsQueryable();
    }


    public virtual async Task<int> SaveChangesAsync()
    {
        return await DbContext.SaveChangesAsync();
    }


    public virtual async Task<int> RunSqlAsync(string sql, params object[] args)
    {
        return await DbContext.Database.ExecuteSqlRawAsync(sql, args);
    }

    public virtual async Task<int> RunSqlAsync(FormattableString strSql)
    {
        return await DbContext.Database.ExecuteSqlInterpolatedAsync(strSql);
    }

    public virtual async ValueTask SaveAsync(object entity)
    {
        var (_, value) = FindKeyAndValue<object>(entity);

        switch (value)
        {
            case Guid guid when guid == Guid.Empty:
                throw new ArgumentException("主键为Guid类型时，主键必须存在唯一值");
            case Guid:
            case null:
            case 0:
                await DbContext.AddAsync(entity);
                break;
            default:
                DbContext.Update(entity);
                break;
        }
    }

    public virtual async ValueTask SaveAsync<T>(T entity) where T : class, new()
    {
        var (_, value) = FindKeyAndValue<object>(entity);

        switch (value)
        {
            case Guid guid when guid == Guid.Empty:
                throw new ArgumentException("主键为Guid类型时，主键必须存在唯一值");
            case Guid:
            case null:
            case 0:
                await DbContext.AddAsync(entity);
                break;
            default:
                DbContext.Update(entity);
                break;
        }
    }

    public virtual async ValueTask SaveRangeAsync(IEnumerable<object> entities)
    {
        foreach (var entity in entities)
        {
            var (_, value) = FindKeyAndValue<object>(entity);

            switch (value)
            {
                case Guid guid when guid == Guid.Empty:
                    throw new ArgumentException("主键为Guid类型时，主键必须存在唯一值");
                case Guid:
                case null:
                case 0:
                    await DbContext.AddAsync(entity);
                    break;
                default:
                    DbContext.Update(entity);
                    break;
            }
        }
    }

    public virtual async ValueTask SaveRangeAsync<T>(IEnumerable<T> entities) where T : class, new()
    {
        foreach (var entity in entities)
        {
            var (_, value) = FindKeyAndValue<object>(entity);

            switch (value)
            {
                case Guid guid when guid == Guid.Empty:
                    throw new ArgumentException("主键为Guid类型时，主键必须存在唯一值");
                case Guid:
                case null:
                case 0:
                    await DbContext.AddAsync(entity);
                    break;
                default:
                    DbContext.Update(entity);
                    break;
            }
        }
    }

    public virtual async Task UpdateAsync(object entity, bool updateAll = false)
    {
        await UpdateAsync<object>(entity, updateAll);
    }

    public virtual async Task UpdateAsync<T>(T entity, bool updateAll = false) where T : class, new()
    {
        DbContext.Update(entity);
        if (!updateAll)
            IgnoreNullAndDefaultValues(entity);

        await Task.CompletedTask;
    }

    public virtual async Task UpdateRangeAsync<T>(IEnumerable<T> entities, bool updateAll = false) where T : class, new()
    {
        DbContext.UpdateRange(entities);
        if (!updateAll)
        {
            foreach (var entity in entities)
                IgnoreNullAndDefaultValues(entity);
        }

        await Task.CompletedTask;
    }


    public virtual async ValueTask<TKey> InsertReturnIdentityAsync<TKey>(object entity)
    {
        await DbContext.AddAsync(entity);
        await DbContext.SaveChangesAsync();
        return FindKeyValue<TKey>(entity);
    }

    public virtual async ValueTask<TKey> InsertReturnIdentityAsync<TKey, TEntity>(TEntity entity) where TEntity : class, new()
    {
        await DbContext.Set<TEntity>().AddAsync(entity);
        await DbContext.SaveChangesAsync();
        return FindKeyValue<TKey>(entity);
    }

    public virtual async ValueTask<object> InsertReturnIdentityAsync(object entity)
    {
        await DbContext.AddAsync(entity);
        await DbContext.SaveChangesAsync();
        return FindKeyValue<object>(entity);
    }

    public virtual async ValueTask InsertAsync(object entity)
    {
        await DbContext.AddAsync(entity);
    }

    public virtual async ValueTask InsertAsync<T>(T entity) where T : class, new()
    {
        await DbContext.Set<T>().AddAsync(entity);
    }

    public virtual async Task InsertRangeAsync(IEnumerable<object> entities)
    {
        await DbContext.AddRangeAsync(entities);
    }

    public virtual async Task InsertRangeAsync<T>(IEnumerable<T> entities) where T : class, new()
    {
        await DbContext.Set<T>().AddRangeAsync(entities);
    }


    public virtual async Task DeleteAsync(object entity)
    {
        DbContext.Remove(entity);
        await Task.CompletedTask;
    }


    public virtual async Task DeleteAsync<T>(T entity) where T : class, new()
    {
        DbContext.Set<T>().Remove(entity);
        await Task.CompletedTask;
    }

    public virtual async Task DeleteRangeAsync<T>(IEnumerable<T> entities) where T : class, new()
    {
        DbContext.Set<T>().RemoveRange(entities);
        await Task.CompletedTask;
    }


    public virtual async Task DeleteRangeAsync(IEnumerable<object> entities)
    {
        DbContext.RemoveRange(entities);
        await Task.CompletedTask;
    }


    public virtual async Task DeleteByIdsAsync<T>(params object[] keyValues) where T : class, new()
    {
        var entities = BuildEntities<T>(EntityState.Deleted, keyValues);
        if (entities?.Any() == true)
        {
            var deletes = await DbContext.Set<T>().Where(x => keyValues.Contains(DbContext.Entry(x).CurrentValues[BaseEntityConstants.Id])).ToListAsync();
            await DeleteRangeAsync(deletes);
        }
    }

    public virtual async Task<int> UseTransactionAsync(Func<Task> func)
    {
        await using var trans = await BeginTransactionAsync();
        try
        {
            await func.Invoke();
            var effectRow = await SaveChangesAsync();
            await trans.CommitAsync();

            return effectRow;
        }
        catch (Exception)
        {
            await trans.RollbackAsync();
            throw;
        }
    }

    public virtual async Task<IDbContextTransaction> BeginTransactionAsync()
    {
        return await DbContext.Database.BeginTransactionAsync();
    }


    #region find key and value

    /// <summary>
    /// 查找key的值
    /// </summary>
    /// <param name="entity"></param>
    /// <typeparam name="TKey"></typeparam>
    /// <returns></returns>
    private TKey FindKeyValue<TKey>(object entity)
    {
        return FindKeyAndValue<TKey>(entity).value;
    }

    /// <summary>
    /// 查找key 和 value的值
    /// </summary>
    /// <param name="entity"></param>
    /// <typeparam name="TKey"></typeparam>
    /// <returns></returns>
    private (string key, TKey value) FindKeyAndValue<TKey>(object entity)
    {
        var key = GetEntityType(entity).FindPrimaryKey();
        var propertyInfo = key.Properties.First(x => x.Name == BaseEntityConstants.Id).PropertyInfo;
        return (propertyInfo.Name, (TKey) propertyInfo.GetValue(entity));
    }

    /// <summary>
    /// 查找key value
    /// </summary>
    /// <param name="t"></param>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    /// <returns></returns>
    // ReSharper disable once UnusedMember.Local
    private (string key, TKey value) FindKeyValue<TEntity, TKey>(TEntity t) where TEntity : class, new()
    {
        var key = GetEntityType<TEntity>().FindPrimaryKey();
        var propInfo = key.Properties[0].PropertyInfo;
        return (propInfo.Name, (TKey) propInfo.GetValue(t));
    }

    /// <summary>
    /// 查找table和keys
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    // ReSharper disable once UnusedMember.Local
    private (string table, IEnumerable<string> keys) FindTableAndKeys<T>() where T : class, new()
    {
        var entityType = GetEntityType<T>();
        var tableName = entityType.GetTableName();
        var key = entityType.FindPrimaryKey();
        return (tableName, key.Properties.Select(x => x.PropertyInfo.Name));
    }

    /// <summary>
    /// 查找keys
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    // ReSharper disable once UnusedMember.Local
    private IEnumerable<string> FindKeys<T>() where T : class, new()
    {
        var key = GetEntityType<T>().FindPrimaryKey();
        return key.Properties.Select(x => x.PropertyInfo.Name);
    }

    #endregion


    /// <summary>
    /// 生成指定的状态的实体
    /// </summary>
    /// <param name="entityState"></param>
    /// <param name="keyValues"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    private List<T> BuildEntities<T>(EntityState entityState, params object[] keyValues) where T : class, new()
    {
        var keyProperty = GetEntityType<T>().FindPrimaryKey().Properties.FirstOrDefault(x => x.Name == BaseEntityConstants.Id)?.PropertyInfo;
        if (keyProperty == null) return null;

        var entityTrackStates = GetTrackStates<T>(keyValues);
        var result = new List<T>(entityTrackStates.Count);

        //已经跟踪
        var tracks = entityTrackStates.Where(x => x.IsTrack);
        foreach (var entityTrackState in tracks)
        {
            //如果已经跟踪的实体状态为dded，那么删除实体时，只需要设置为unchanged
            entityTrackState.Entry.State = entityTrackState.Entry.State == EntityState.Added ? EntityState.Unchanged : entityState;
            result.Add(entityTrackState.Entry.Entity);
        }


        //返回创建的新实体和定义的跟踪状态
        var unTracks = entityTrackStates.Where(x => !x.IsTrack);
        foreach (var entityTrackState in unTracks)
        {
            var entity = new T();
            keyProperty.SetValue(entity, entityTrackState.KeyValue);
            result.Add(ChangeEntityState(entity, entityState).Entity);
        }

        return result;
    }

    /// <summary>
    /// 修改实体跟踪状态
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="entityState"></param>
    /// <typeparam name="TEntity"></typeparam>
    /// <returns></returns>
    private EntityEntry<TEntity> ChangeEntityState<TEntity>(TEntity entity, EntityState entityState)
        where TEntity : class, new()
    {
        //触发跟踪实体
        var entry = DbContext.Entry(entity);
        entry.State = entityState;
        return entry;
    }


    /// <summary>
    /// 获取实体和跟踪状态
    /// </summary>
    /// <param name="keyValues"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    private List<EntityTrackState<T>> GetTrackStates<T>(params object[] keyValues) where T : class, new()
    {
        var entries = DbContext.ChangeTracker.Entries<T>().ToArray();

        var result = new List<EntityTrackState<T>>(entries.Length);
        foreach (var keyValue in keyValues)
        {
            var entry = entries.FirstOrDefault(x => x.CurrentValues[BaseEntityConstants.Id].ToString() == keyValue.ToString());
            result.Add(new EntityTrackState<T>
            {
                IsTrack = entry?.Entity != null,
                KeyValue = keyValue,
                Entry = entry
            });
        }

        return result;
    }


    /// <summary>
    /// 忽略null值
    /// </summary>
    /// <param name="entity"></param>
    /// <typeparam name="T"></typeparam>
    private void IgnoreNullAndDefaultValues<T>(T entity) where T : class, new()
    {
        // 获取所有的属性
        var properties = GetEntityType<T>()?.GetProperties().ToArray();
        if (properties == null || !properties.Any()) return;

        foreach (var property in properties)
        {
            var entityProperty = DbContext.Entry(entity).Property(property.Name);
            if (entityProperty != null)
            {
                var propertyValue = entityProperty.CurrentValue;
                var propertyType = entityProperty.Metadata.PropertyInfo?.PropertyType;

                // 判断是否是无效的值，比如为 null，默认时间，以及空 Guid 值
                var isInvalid = propertyValue == null
                                || propertyType == typeof(DateTime) && propertyValue.ToString() == new DateTime().ToString(CultureInfo.InvariantCulture)
                                || propertyType == typeof(DateTimeOffset) && propertyValue.ToString() == new DateTimeOffset().ToString()
                                || propertyType == typeof(Guid) && propertyValue.ToString() == Guid.Empty.ToString();

                if (isInvalid)
                    entityProperty.IsModified = false;
            }
        }
    }


    /// <summary>
    /// 获取实体类型
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    private IEntityType GetEntityType<T>() where T : class, new()
    {
        return DbContext.Model.FindEntityType(typeof(T));
    }

    /// <summary>
    /// 获取实体类型
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    private IEntityType GetEntityType(object obj)
    {
        return DbContext.Model.FindEntityType(obj.GetType());
    }


    private class EntityTrackState<T> where T : class
    {
        public bool IsTrack { get; init; }
        public object KeyValue { get; init; }
        public EntityEntry<T> Entry { get; init; }
    }
}