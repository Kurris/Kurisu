using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Kurisu.DataAccess.Entity;
using Kurisu.DataAccess.Functions.Default.Abstractions;
using Kurisu.Utils.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.DataAccess.Functions.Default.Internal;

/// <summary>
/// 数据操作(写)
/// </summary>
// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
[SkipScan]
public class WriteImplementation : ReadImplementation, IDbWrite
{
    private readonly DbContext _dbContext;

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="dbContext"></param>
    public WriteImplementation(DbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public override DbContext GetDbContext() => _dbContext;


    public override IQueryable<T> AsQueryable<T>()
    {
        return _dbContext.Set<T>().AsQueryable();
    }

    public virtual async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess = true)
    {
        return await _dbContext.SaveChangesAsync(acceptAllChangesOnSuccess);
    }


    public virtual async Task<int> RunSqlAsync(string sql, params object[] args)
    {
        return await _dbContext.Database.ExecuteSqlRawAsync(sql, args);
    }

    public virtual async Task<int> RunSqlAsync(FormattableString strSql)
    {
        return await _dbContext.Database.ExecuteSqlInterpolatedAsync(strSql);
    }

    #region save

    public virtual async ValueTask SaveAsync(object entity)
    {
        var id = FindKeyValue<int>(entity);
        if (id == 0)
            await _dbContext.AddAsync(entity);
        else
            _dbContext.Update(entity);
    }

    public virtual async ValueTask SaveAsync<T>(T entity) where T : class, new()
    {
        var id = FindKeyValue<int>(entity);
        if (id == 0)
            await _dbContext.AddAsync(entity);
        else
            _dbContext.Update(entity);
    }

    public virtual async ValueTask SaveRangeAsync(IEnumerable<object> entities)
    {
        foreach (var entity in entities)
        {
            var id = FindKeyValue<int>(entity);
            if (id == 0)
                await _dbContext.AddAsync(entity);
            else
                _dbContext.Update(entity);
        }
    }

    public virtual async ValueTask SaveRangeAsync<T>(IEnumerable<T> entities) where T : class, new()
    {
        foreach (var entity in entities)
        {
            var id = FindKeyValue<int>(entity);
            if (id == 0)
                await _dbContext.AddAsync(entity);
            else
                _dbContext.Update(entity);
        }
    }

    #endregion

    #region add

    public virtual async ValueTask AddAsync(object entity)
    {
        await _dbContext.AddAsync(entity);
    }

    public virtual async ValueTask AddAsync<T>(T entity) where T : class, new()
    {
        await _dbContext.Set<T>().AddAsync(entity);
    }

    public virtual async Task AddRangeAsync(IEnumerable<object> entities)
    {
        await _dbContext.AddRangeAsync(entities);
    }

    public virtual async Task AddRangeAsync<T>(IEnumerable<T> entities) where T : class, new()
    {
        await _dbContext.Set<T>().AddRangeAsync(entities);
    }

    #endregion

    #region update

    public virtual async Task UpdateAsync(object entity, bool all = false)
    {
        _dbContext.Update(entity);
        if (!all)
            IgnoreNullAndDefaultValues(entity);

        await Task.CompletedTask;
    }

    public virtual async Task UpdateAsync<T>(T entity, bool all = false) where T : class, new()
    {
        _dbContext.Set<T>().Update(entity);
        if (!all)
            IgnoreNullAndDefaultValues(entity);

        await Task.CompletedTask;
    }

    public virtual async Task UpdateRangeAsync<T>(IEnumerable<T> entities, bool all = false) where T : class, new()
    {
        _dbContext.Set<T>().UpdateRange(entities);
        if (!all)
        {
            foreach (var entity in entities)
                IgnoreNullAndDefaultValues(entity);
        }

        await Task.CompletedTask;
    }

    public virtual async Task UpdateRangeAsync(IEnumerable<object> entities, bool all = false)
    {
        _dbContext.UpdateRange(entities);
        if (!all)
        {
            foreach (var entity in entities)
                IgnoreNullAndDefaultValues(entity);
        }

        await Task.CompletedTask;
    }

    #endregion

    #region delete

    public virtual async Task DeleteAsync(object entity)
    {
        _dbContext.Remove(entity);
        await Task.CompletedTask;
    }


    public virtual async Task DeleteAsync<T>(T entity) where T : class, new()
    {
        _dbContext.Set<T>().Remove(entity);
        await Task.CompletedTask;
    }

    public virtual async Task DeleteRangeAsync<T>(IEnumerable<T> entities) where T : class, new()
    {
        _dbContext.Set<T>().RemoveRange(entities);
        await Task.CompletedTask;
    }


    public virtual async Task DeleteRangeAsync(IEnumerable<object> entities)
    {
        _dbContext.RemoveRange(entities);
        await Task.CompletedTask;
    }

    public Task DeleteAsync<T>(Expression<Func<T, bool>> predicate) where T : class, new()
    {
        throw new NotImplementedException();
    }


    public virtual async Task DeleteByIdsAsync<T>(params object[] keyValues) where T : class, new()
    {
        var entities = BuildEntities<T>(EntityState.Deleted, keyValues);
        if (entities?.Any() == true)
        {
            var deletes = await _dbContext.Set<T>().Where(x => keyValues.Contains(_dbContext.Entry(x).CurrentValues[BaseEntityConstants.Id])).ToListAsync();
            await DeleteRangeAsync(deletes);
        }
    }

    #endregion

    public virtual async Task<int> UseTransactionAsync(Func<Task> func, bool acceptAllChangesOnSuccess)
    {
        await using var trans = await BeginTransactionAsync();
        try
        {
            await func.Invoke();
            var effectRow = await SaveChangesAsync(acceptAllChangesOnSuccess);
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
        return await _dbContext.Database.BeginTransactionAsync();
    }

    #region find key and value

    /// <summary>
    /// 查找主键值
    /// </summary>
    /// <param name="entity"></param>
    /// <typeparam name="TKey"></typeparam>
    /// <returns></returns>
    private TKey FindKeyValue<TKey>(object entity)
    {
        //return entity.GetExpressionPropertyValue<TKey>(BaseEntityConstants.Id);
        //var key = GetEntityType(entity).FindPrimaryKey();
        //var propertyInfo = key.Properties.First(x => x.Name == BaseEntityConstants.Id).PropertyInfo;
        //return (propertyInfo.Name, (TKey)propertyInfo.GetValue(entity));

        //todo
        return default;
    }

    /// <summary>
    /// 查找主键值
    /// </summary>
    /// <param name="t"></param>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    /// <returns></returns>
    // ReSharper disable once UnusedMember.Local
    private TKey FindKeyValue<TEntity, TKey>(TEntity t) where TEntity : class, new()
    {
        //todo
        return default(TKey);
        //return (TKey) t.GetExpressionPropertyValue(BaseEntityConstants.Id);
        //var key = GetEntityType<TEntity>().FindPrimaryKey();
        //var propInfo = key.Properties[0].PropertyInfo;
        //return (propInfo.Name, (TKey)propInfo.GetValue(t));
    }

    #endregion

    #region others

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
        var entry = _dbContext.Entry(entity);
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
        var entries = _dbContext.ChangeTracker.Entries<T>().ToArray();

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

        var entry = _dbContext.Entry(entity);

        foreach (var property in properties)
        {
            var propertyEntry = entry.Property(property.Name);
            if (propertyEntry != null)
            {
                var propertyValue = propertyEntry.CurrentValue;
                var propertyType = property.ClrType; //propertyEntry.Metadata.PropertyInfo.PropertyType;

                // 判断是否是无效的值，比如为 null，默认时间，以及空 Guid 值
                var isInvalid = propertyValue == null
                                || propertyType == typeof(DateTime) && propertyValue.ToString() == new DateTime().ToString(CultureInfo.InvariantCulture)
                                || propertyType == typeof(DateTimeOffset) && propertyValue.ToString() == new DateTimeOffset().ToString()
                                || propertyType == typeof(Guid) && propertyValue.ToString() == Guid.Empty.ToString();

                if (isInvalid)
                    propertyEntry.IsModified = false;
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
        return _dbContext.Model.FindEntityType(typeof(T));
    }

    /// <summary>
    /// 获取实体类型
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    private IEntityType GetEntityType(object obj)
    {
        return _dbContext.Model.FindEntityType(obj.GetType());
    }


    private class EntityTrackState<T> where T : class
    {
        public bool IsTrack { get; init; }
        public object KeyValue { get; init; }
        public EntityEntry<T> Entry { get; init; }
    }

    #endregion
}