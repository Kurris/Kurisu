using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Kurisu.DataAccessor.Abstractions;
using Kurisu.DataAccessor.Extensions;
using Kurisu.DataAccessor.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Kurisu.DataAccessor.Internal
{
    /// <summary>
    /// 数据库操作抽象类
    /// </summary>
    public abstract class DbOperationImplementation : IMasterDbImplementation, ISlaveDbImplementation
    {
        public DbOperationImplementation(DbContext dbContext)
        {
            DbContext = dbContext;
        }


        /// <summary>
        /// 数据库上下文
        /// </summary>
        public DbContext DbContext { get; }


        public IEntityType GetEntityType<T>() where T : class, new()
        {
            return DbContext.Model.FindEntityType(typeof(T));
        }

        public IEntityType GetEntityType(object obj)
        {
            return DbContext.Model.FindEntityType(obj.GetType());
        }


        #region 主键,主键值,表名

        public virtual IDictionary<string, object> FindPrimaryKeyValue<T>(T t) where T : class, new()
        {
            var key = GetEntityType<T>().FindPrimaryKey();
            var dic = new Dictionary<string, object>(key.Properties.Count);
            foreach (var item in key.Properties)
            {
                var propInfo = item.PropertyInfo;
                dic.Add(propInfo.Name, propInfo.GetValue(t));
            }

            return dic;
        }

        public virtual (string key, object value) FindFirstPrimaryKeyValue<T>(T t) where T : class, new()
        {
            var key = GetEntityType<T>().FindPrimaryKey();
            var propInfo = key.Properties.First().PropertyInfo;
            return (propInfo.Name, propInfo.GetValue(t));
        }

        public virtual (string key, object value) FindFirstPrimaryKeyValue(object entity)
        {
            var key = GetEntityType(entity).FindPrimaryKey();
            var propInfo = key.Properties.First().PropertyInfo;
            return (propInfo.Name, propInfo.GetValue(entity));
        }

        public virtual (string table, IEnumerable<string> keys) FindPrimaryKeyWithTable<T>() where T : class, new()
        {
            var entityType = GetEntityType<T>();
            var tableName = entityType.GetTableName();
            var key = entityType.FindPrimaryKey();
            return (tableName, key.Properties.Select(x => x.PropertyInfo.Name));
        }

        public virtual IEnumerable<string> FindPrimaryKey<T>() where T : class, new()
        {
            var key = GetEntityType<T>().FindPrimaryKey();
            return key.Properties.Select(x => x.PropertyInfo.Name);
        }


        public T BuildEntity<T>(object keyValue, EntityState entityState) where T : class, new()
        {
            var keyProperty = GetEntityType<T>().FindPrimaryKey().Properties.FirstOrDefault()?.PropertyInfo;
            if (keyProperty == null) return default;

            var trackInfo = GetTrackState<T>(keyValue);
            if (trackInfo.isTrack)
            {
                trackInfo.trackEntity.State = entityState;
                return trackInfo.trackEntity.Entity as T;
            }

            var entity = new T();
            keyProperty.SetValue(keyValue, entity);

            ChangeEntityState(entity, entityState);
            return entity;
        }

        public virtual EntityEntry<T> ChangeEntityState<T>(T entity, EntityState entityState) where T : class, new()
        {
            var entry = DbContext.Entry(entity);
            entry.State = entityState;
            return entry;
        }


        public (bool isTrack, EntityEntry trackEntity) GetTrackState<T>(object keyValue) where T : class, new()
        {
            var entities = DbContext.ChangeTracker.Entries<T>();
            var keyName = FindPrimaryKey<T>().First();

            var trackEntity = entities.FirstOrDefault(x => x.CurrentValues[keyName] == keyValue);
            return (trackEntity != null, trackEntity);
        }

        #endregion

        #region 跟踪,可查询

        public virtual IQueryable<T> AsQueryable<T>(Expression<Func<T, bool>> predicate) where T : class, new() => DbContext.Set<T>().Where(predicate);
        public virtual IQueryable<T> AsQueryable<T>() where T : class, new() => DbContext.Set<T>().AsQueryable();

        public virtual IQueryable<T> AsNoTracking<T>() where T : class, new() => DbContext.Set<T>().AsNoTracking();

        public virtual IDbCommonOperation AsNoTracking()
        {
            DbContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
            return this;
        }

        public virtual IDbCommonOperation AsNoTrackingWithIdentityResolution()
        {
            DbContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTrackingWithIdentityResolution;
            return this;
        }

        #endregion

        public virtual async Task<int> RunSqlAsync(string strSql, IDictionary<string, object> keyValues = null)
        {
            if (keyValues != null)
                return await DbContext.Database.ExecuteSqlRawAsync(strSql, new DbParameterBuilder(this.DbContext).AddParams(keyValues).GetParams());
            else
                return await DbContext.Database.ExecuteSqlRawAsync(strSql);
        }

        public virtual async Task<int> RunSqlInterAsync(FormattableString strSql) => await DbContext.Database.ExecuteSqlInterpolatedAsync(strSql);

        public virtual Task ExecProcAsync(string procName, IDictionary<string, object> keyValues = null) => throw new NotImplementedException("请在派生类中实现");

        #region Write Implementation

        public virtual async ValueTask SaveAsync(object entity)
        {
            var (_, value) = this.FindFirstPrimaryKeyValue(entity);
            if (value == default)
                await this.DbContext.AddAsync(entity);
            else
                Db.RecursionAttach(this.DbContext, entity);
        }

        public virtual async ValueTask SaveAsync(IEnumerable<object> entities)
        {
            var adds = entities.Where(x => this.FindFirstPrimaryKeyValue(x).value == default);
            await this.DbContext.AddRangeAsync(adds);

            var updates = entities.Where(x => !adds.Contains(x));
            foreach (var update in updates)
                Db.RecursionAttach(this.DbContext, update);
        }

        public virtual async ValueTask AddAsync<T>(T entity) where T : class, new() => await DbContext.Set<T>().AddAsync(entity);

        public virtual async Task AddAsync<T>(IEnumerable<T> entities) where T : class, new() => await DbContext.Set<T>().AddRangeAsync(entities);


        public virtual async Task UpdateAsync<T>(T entity, bool updateAll = false) where T : class, new()
        {
            if (updateAll)
                this.DbContext.Update(entity);
            else
                Db.RecursionAttach(this.DbContext, entity);

            await Task.CompletedTask;
        }

        public virtual async Task UpdateAsync<T>(IEnumerable<T> entities, bool updateAll = false) where T : class, new()
        {
            foreach (var entity in entities)
            {
                await UpdateAsync(entity, updateAll);
            }
        }

        public virtual async Task DeleteAsync<T>(T entity) where T : class, new()
        {
            DbContext.Set<T>().Remove(entity);
            await Task.CompletedTask;
        }

        public virtual async Task DeleteAsync<T>(IEnumerable<T> entities) where T : class, new()
        {
            DbContext.Set<T>().RemoveRange(entities);
            await Task.CompletedTask;
        }

        public virtual async Task DeleteAsync<T>(object keyValue) where T : class, new()
        {
            var entity = BuildEntity<T>(keyValue, EntityState.Deleted);
            if (entity != null) return;

            entity = await FindAsync<T>(keyValue);
            if (entity != null) await this.DeleteAsync(entity);
        }

        public virtual async Task DeleteAsync<T>(IEnumerable<object> keyValues) where T : class, new()
        {
            var ts = new List<T>(keyValues.Count());
            foreach (var item in keyValues)
            {
                var t = new T();
                var (key, _) = FindFirstPrimaryKeyValue(t);
                t.GetType().GetProperty(key).SetValue(t, item);
                ts.Add(t);
            }

            await this.DeleteAsync(ts);
        }

        #endregion

        #region Read Implementation

        public virtual async Task<T> FindFirstAsync<T>() where T : class, new() => await DbContext.Set<T>().FirstOrDefaultAsync();
        public virtual async ValueTask<T> FindAsync<T>(params object[] keyValues) where T : class, new() => await DbContext.Set<T>().FindAsync(keyValues);
        public virtual async ValueTask<object> FindAsync(Type type, params object[] keys) => await DbContext.FindAsync(type, keys);
        public virtual async Task<T> FindAsync<T>(Expression<Func<T, bool>> predicate) where T : class, new() => await DbContext.Set<T>().FirstOrDefaultAsync(predicate);

        public virtual async Task<IEnumerable<T>> FindListAsync<T>(Expression<Func<T, bool>> predicate = null) where T : class, new()
        {
            IEnumerable<T> et = predicate == null
                ? await DbContext.Set<T>().ToListAsync()
                : await DbContext.Set<T>().Where(predicate).ToListAsync();

            return et;
        }

        public virtual async Task<Pagination<T>> FindListAsync<T>(int pageIndex, int pageSize) where T : class, new()
        {
            return await DbContext.Set<T>().AsQueryable().ToPagedListAsync(pageIndex, pageSize);
        }

        public virtual async Task<Pagination<T>> FindListAsync<T>(Expression<Func<T, bool>> predicate, int pageIndex, int pageSize) where T : class, new()
        {
            return await DbContext.Set<T>().Where(predicate).ToPagedListAsync(pageIndex, pageSize);
        }

        public virtual async Task<DataTable> GetTableAsync(string strSql, IDictionary<string, object> keyValues = null)
        {
            return await new DbHelper(this.DbContext).GetDataTable(strSql, keyValues);
        }

        public virtual async Task<IDataReader> GetReaderAsync(string strSql, IDictionary<string, object> keyValues = null)
        {
            return await new DbHelper(this.DbContext).GeDataReader(strSql, keyValues);
        }

        public virtual async Task<object> GetScalarAsync(string strSql, IDictionary<string, object> keyValues = null)
        {
            return await new DbHelper(this.DbContext).GetScalar(strSql, keyValues);
        }

        #endregion
    }
}