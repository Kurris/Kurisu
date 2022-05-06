using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Kurisu.Common.Dto;
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
    internal abstract class DbOperationImplementation : IMasterDbService
    {
        internal DbOperationImplementation(DbContext dbContext)
        {
            this.DbContext = dbContext;
        }

        public DbContext DbContext { get; }

        #region 跟踪,可查询

        public virtual IQueryable<T> AsQueryable<T>(Expression<Func<T, bool>> predicate) where T : class, new()
        {
            return DbContext.Set<T>().Where(predicate);
        }

        public virtual IQueryable<T> AsQueryable<T>() where T : class, new()
        {
            return DbContext.Set<T>().AsQueryable();
        }

        public virtual IQueryable<T> AsNoTracking<T>() where T : class, new()
        {
            return DbContext.Set<T>().AsNoTracking();
        }

        public virtual IQueryable<T> AsNoTrackingWithIdentityResolution<T>() where T : class, new()
        {
            return DbContext.Set<T>().AsNoTrackingWithIdentityResolution();
        }

        public virtual IMasterDbService AsNoTracking()
        {
            DbContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
            return this;
        }

        public virtual IMasterDbService AsNoTrackingWithIdentityResolution()
        {
            DbContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTrackingWithIdentityResolution;
            return this;
        }

        #endregion

        public virtual async Task<int> RunSqlAsync(string sql, IDictionary<string, object> keyValues = null)
        {
            if (keyValues != null)
                return await DbContext.Database.ExecuteSqlRawAsync(sql, new DbParameterBuilder(this.DbContext).AddParams(keyValues).GetParams());
            else
                return await DbContext.Database.ExecuteSqlRawAsync(sql);
        }

        public virtual async Task<int> RunSqlInterAsync(FormattableString strSql) => await DbContext.Database.ExecuteSqlInterpolatedAsync(strSql);

        public virtual Task ExecProcAsync(string procName, IDictionary<string, object> keyValues = null) => throw new NotImplementedException("请在派生类中实现");

        #region Write Implementation

        public virtual async ValueTask<T> SaveAsync<T>(object entity) where T : class, new()
        {
            var (_, value) = this.FindKeyValue<object>(entity);

            switch (value)
            {
                case Guid guid when guid == Guid.Empty:
                    throw new ArgumentException("主键为Guid类型时，主键必须存在唯一值");
                case Guid _:
                case null:
                    await this.DbContext.AddAsync(entity);
                    break;
                default:
                    await UpdateAsync(entity);
                    break;
            }

            return (T) value;
        }

        public async ValueTask SaveAsync<T>(T entity) where T : class, new()
        {
            var (_, value) = this.FindKeyValue<object>(entity);

            switch (value)
            {
                case Guid guid when guid == Guid.Empty:
                    throw new ArgumentException("主键为Guid类型时，主键必须存在唯一值");
                case Guid _:
                case null:
                    await this.DbContext.AddAsync(entity);
                    break;
                default:
                    await UpdateAsync(entity);
                    break;
            }
        }

        public virtual async ValueTask<IEnumerable<T>> SaveAsync<T>(IEnumerable<object> entities) where T : class, new()
        {
            var result = new List<T>(entities.Count());

            foreach (var entity in entities)
            {
                var (_, value) = this.FindKeyValue<object>(entity);

                switch (value)
                {
                    case Guid guid when guid == Guid.Empty:
                        throw new ArgumentException("主键为Guid类型时，主键必须存在唯一值");
                    case Guid _:
                    case null:
                        await this.DbContext.AddAsync(entity);
                        break;
                    default:
                        await UpdateAsync(entity);
                        break;
                }

                result.Add((T) value);
            }

            return result;
        }

        public async ValueTask SaveAsync<T>(IEnumerable<T> entities) where T : class, new()
        {
            foreach (var entity in entities)
            {
                var (_, value) = this.FindKeyValue<object>(entity);

                switch (value)
                {
                    case Guid guid when guid == Guid.Empty:
                        throw new ArgumentException("主键为Guid类型时，主键必须存在唯一值");
                    case Guid _:
                    case null:
                        await this.DbContext.AddAsync(entity);
                        break;
                    default:
                        await UpdateAsync(entity);
                        break;
                }
            }
        }

        public virtual async ValueTask AddAsync<T>(T entity) where T : class, new()
        {
            await DbContext.Set<T>().AddAsync(entity);
        }

        public virtual async Task AddAsync<T>(IEnumerable<T> entities) where T : class, new()
        {
            await DbContext.Set<T>().AddRangeAsync(entities);
        }


        public virtual async Task UpdateAsync<T>(T entity, bool updateAll = false) where T : class, new()
        {
            this.DbContext.Update(entity);
            if (!updateAll)
                IgnoreNullValues(entity);

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

            entity = await FirstOrDefaultAsync<T>(keyValue);
            if (entity != null) await this.DeleteAsync(entity);
        }

        public virtual async Task DeleteAsync<T>(IEnumerable<object> keyValues) where T : class, new()
        {
            var ts = new List<T>(keyValues.Count());
            foreach (var item in keyValues)
            {
                var t = new T();
                var keyName = FindKey<T>();
                t.GetType().GetProperty(keyName).SetValue(t, item);
                ts.Add(t);
            }

            await this.DeleteAsync(ts);
        }

        #endregion

        #region Read Implementation

        public virtual async Task<T> FirstOrDefaultAsync<T>() where T : class, new()
        {
            return await AsNoTracking<T>().FirstOrDefaultAsync();
        }

        public virtual async ValueTask<T> FirstOrDefaultAsync<T>(params object[] keyValues) where T : class, new()
        {
            return await DbContext.Set<T>().FindAsync(keyValues);
        }

        public virtual async ValueTask<object> FirstOrDefaultAsync(Type type, params object[] keys)
        {
            return await DbContext.FindAsync(type, keys);
        }

        public virtual async Task<T> FirstOrDefaultAsync<T>(Expression<Func<T, bool>> predicate) where T : class, new()
        {
            return await AsNoTracking<T>().FirstOrDefaultAsync(predicate);
        }

        public virtual async Task<IEnumerable<T>> ToListAsync<T>(Expression<Func<T, bool>> predicate = null) where T : class, new()
        {
            IEnumerable<T> data = predicate == null
                ? await AsNoTracking<T>().ToListAsync()
                : await AsNoTracking<T>().Where(predicate).ToListAsync();

            return data;
        }

        public virtual async Task<Pagination<T>> ToPageAsync<T>(int pageIndex, int pageSize) where T : class, new()
        {
            return await AsNoTracking<T>().ToPagedListAsync(pageIndex, pageSize);
        }

        public virtual async Task<Pagination<T>> ToPageAsync<T>(Expression<Func<T, bool>> predicate, int pageIndex, int pageSize) where T : class, new()
        {
            return await AsNoTracking<T>().Where(predicate).ToPagedListAsync(pageIndex, pageSize);
        }

        public virtual async Task<DataTable> GetTableAsync(string sql, IDictionary<string, object> keyValues = null)
        {
            return await new DbHelper(this.DbContext).GetDataTable(sql, keyValues);
        }

        public virtual async Task<IDataReader> GetReaderAsync(string sql, IDictionary<string, object> keyValues = null)
        {
            return await new DbHelper(this.DbContext).GeDataReader(sql, keyValues);
        }

        public virtual async Task<object> GetScalarAsync(string sql, IDictionary<string, object> keyValues = null)
        {
            return await new DbHelper(this.DbContext).GetScalar(sql, keyValues);
        }

        #endregion

        private void IgnoreNullValues<T>(T entity) where T : class, new()
        {
            // 获取所有的属性
            var properties = GetEntityType<T>()?.GetProperties();
            if (properties == null) return;

            foreach (var property in properties)
            {
                var entityProperty = DbContext.Entry(entity).Property(property.Name);
                var propertyValue = entityProperty?.CurrentValue;
                var propertyType = entityProperty?.Metadata?.PropertyInfo?.PropertyType;

                // 判断是否是无效的值，比如为 null，默认时间，以及空 Guid 值
                var isInvalid = propertyValue == null
                                || (propertyType == typeof(DateTime) && propertyValue.ToString() == new DateTime().ToString())
                                || (propertyType == typeof(DateTimeOffset) && propertyValue.ToString() == new DateTimeOffset().ToString())
                                || (propertyType == typeof(Guid) && propertyValue.ToString() == Guid.Empty.ToString());

                if (isInvalid && entityProperty != null)
                    entityProperty.IsModified = false;
            }
        }


        #region help method

        private IEntityType GetEntityType<T>() where T : class, new()
        {
            return DbContext.Model.FindEntityType(typeof(T));
        }

        private IEntityType GetEntityType(object obj)
        {
            return DbContext.Model.FindEntityType(obj.GetType());
        }

        /// <summary>
        /// 查找key value
        /// </summary>
        /// <param name="t"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private IDictionary<string, object> FindKeyValues<T>(T t) where T : class, new()
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


        private string FindKey<T>() where T : class, new()
        {
            return GetEntityType<T>().FindPrimaryKey().Properties[0].Name;
        }

        /// <summary>
        /// 查找key value
        /// </summary>
        /// <param name="t"></param>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <returns></returns>
        private (string key, TKey value) FindKeyValue<TEntity, TKey>(TEntity t) where TEntity : class, new()
        {
            var key = GetEntityType<TEntity>().FindPrimaryKey();
            var propInfo = key.Properties[0].PropertyInfo;
            return (propInfo.Name, (TKey) propInfo.GetValue(t));
        }

        /// <summary>
        /// 查找key value
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        private (string key, TKey value) FindKeyValue<TKey>(object entity)
        {
            var key = GetEntityType(entity).FindPrimaryKey();
            var propInfo = key.Properties[0].PropertyInfo;
            return (propInfo.Name, (TKey) propInfo.GetValue(entity));
        }

        /// <summary>
        /// 查找table和keys
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
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
        private IEnumerable<string> FindKeys<T>() where T : class, new()
        {
            var key = GetEntityType<T>().FindPrimaryKey();
            return key.Properties.Select(x => x.PropertyInfo.Name);
        }


        /// <summary>
        /// 生成指定的状态的实体
        /// </summary>
        /// <param name="keyValue"></param>
        /// <param name="entityState"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private T BuildEntity<T>(object keyValue, EntityState entityState) where T : class, new()
        {
            var keyProperty = GetEntityType<T>().FindPrimaryKey().Properties.FirstOrDefault()?.PropertyInfo;
            if (keyProperty == null) return default;

            var (isTrack, trackEntity) = GetTrackState<T>(keyValue);
            //已经跟踪
            if (isTrack)
            {
                //如果已经跟踪的实体状态为dded，那么删除实体时，只需要设置为unchanged
                trackEntity.State = trackEntity.State == EntityState.Added ? EntityState.Unchanged : entityState;
                return (T) trackEntity.Entity;
            }

            //返回创建的新实体和定义的跟踪状态
            var entity = new T();
            keyProperty.SetValue(keyValue, entity);

            return ChangeEntityState(entity, entityState).Entity;
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
            var entry = DbContext.Entry(entity);
            entry.State = entityState;
            return entry;
        }


        /// <summary>
        /// 获取实体和跟踪状态
        /// </summary>
        /// <param name="keyValue"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private (bool isTrack, EntityEntry trackEntity) GetTrackState<T>(object keyValue) where T : class, new()
        {
            var entities = DbContext.ChangeTracker.Entries<T>();
            var keyName = FindKey<T>();

            var trackEntity = entities.FirstOrDefault(x => x.CurrentValues[keyName].ToString() == keyValue.ToString());
            return (trackEntity != null, trackEntity);
        }

        #endregion
    }
}