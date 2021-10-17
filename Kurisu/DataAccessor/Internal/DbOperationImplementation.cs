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

        #region 主键,主键值,表名

        public virtual IDictionary<string, object> FindPrimaryKeyValue<T>(T t) where T : class, new()
        {
            var entityType = DbContext.Model.FindEntityType(typeof(T));
            var key = entityType.FindPrimaryKey();
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
            var entityType = DbContext.Model.FindEntityType(typeof(T));
            var key = entityType.FindPrimaryKey();
            var propInfo = key.Properties.FirstOrDefault()?.PropertyInfo;
            return (propInfo.Name, propInfo.GetValue(t));
        }

        public virtual (string key, object value) FindFirstPrimaryKeyValue(object entity)
        {
            var entityType = DbContext.Model.FindEntityType(entity.GetType());
            var key = entityType.FindPrimaryKey();
            var propInfo = key.Properties.FirstOrDefault()?.PropertyInfo;
            return (propInfo.Name, propInfo.GetValue(entity));
        }

        public virtual (string table, IEnumerable<string> keys) FindPrimaryKeyWithTable<T>() where T : class, new()
        {
            var entityType = this.DbContext.Model.FindEntityType(typeof(T));
            var tableName = entityType.GetTableName();
            var key = entityType.FindPrimaryKey();
            return (tableName, key.Properties.Select(x => x.PropertyInfo.Name));
        }

        public virtual IEnumerable<string> FindPrimaryKey<T>() where T : class, new()
        {
            var entityType = this.DbContext.Model.FindEntityType(typeof(T));
            var key = entityType.FindPrimaryKey();
            return key.Properties.Select(x => x.PropertyInfo.Name);
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

        public virtual async Task RunSqlAsync(string strSql, IDictionary<string, object> keyValues = null)
        {
            if (keyValues != null)
                await DbContext.Database.ExecuteSqlRawAsync(strSql, new DbParameterBuilder(this.DbContext).AddParams(keyValues).GetParams());
            else
                await DbContext.Database.ExecuteSqlRawAsync(strSql);
        }

        public virtual async Task RunSqlInterAsync(FormattableString strSql) => await DbContext.Database.ExecuteSqlInterpolatedAsync(strSql);

        public virtual async Task ExecProcAsync(string procName, IDictionary<string, object> keyValues = null) => throw new NotImplementedException("请在派生类中实现");

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
        }

        public virtual async Task UpdateAsync<T>(IEnumerable<T> entities, bool updateAll = false) where T : class, new()
        {
            foreach (var entity in entities)
            {
                await UpdateAsync(entity, updateAll);
            }
        }


        public virtual async Task UpdateAsync<T>(IEnumerable<Expression<Func<T, bool>>> setPredicates, Expression<Func<T, bool>> wherePredicate, IDictionary<string, object> keyValues = default)
            where T : class, new()
        {
            var (table, keys) = FindPrimaryKeyWithTable<T>();
            var tableName = table;

            var whereCondition = new ConditionBuilderVisitor("MySql");
            whereCondition.Visit(wherePredicate);
            var whereString = whereCondition.CombineWithWhere();

            var setStrings = string.Join(",", setPredicates.Select(x =>
            {
                var setCondition = new ConditionBuilderVisitor("MySql");
                setCondition.Visit(x);
                return setCondition.Combine();
            })).TrimStart('(', ' ').TrimEnd(')', ' ');

            var sql = $@"UPDATE {tableName} SET {setStrings} {whereString}";

            await this.RunSqlAsync(sql, keyValues);
        }

        public virtual async Task DeleteAsync<T>(T entity) where T : class, new() => DbContext.Set<T>().Remove(entity);
        public virtual async Task DeleteAsync<T>(IEnumerable<T> entities) where T : class, new() => DbContext.Set<T>().RemoveRange(entities);

        public virtual async Task DeleteAsync<T>(object keyValue) where T : class, new()
        {
            var t = new T();
            var (key, _) = FindFirstPrimaryKeyValue(t);
            t.GetType().GetProperty(key).SetValue(t, keyValue);
            await this.DeleteAsync(t);
        }

        public virtual async Task DeleteAsync<T>(IEnumerable<int> keyValues) where T : class, new()
        {
            foreach (var item in keyValues)
            {
                var t = new T();
                var (key, _) = FindFirstPrimaryKeyValue(t);
                t.GetType().GetProperty(key).SetValue(t, item);

                await this.DeleteAsync(t);
            }
        }

        public virtual async Task DeleteAsync<T>(Expression<Func<T, bool>> predicate) where T : class, new()
        {
            var ts = await this.FindListAsync(predicate);
            if (ts != null && ts.Any())
            {
                await DeleteAsync(ts);
            }
        }

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
    }
}