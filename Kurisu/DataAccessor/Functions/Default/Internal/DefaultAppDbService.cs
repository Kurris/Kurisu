using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Kurisu.DataAccessor.Dto;
using Kurisu.DataAccessor.Functions.Default.Abstractions;
using Kurisu.DataAccessor.Functions.ReadWriteSplit.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Kurisu.DataAccessor.Functions.Default.Internal
{
    /// <summary>
    /// 数据库访问服务
    /// </summary>
    public class DefaultAppDbService : IAppDbService
    {
        private readonly IAppMasterDb _masterDb;

        public DefaultAppDbService(IAppMasterDb appMasterDb)
        {
            _masterDb = appMasterDb;
        }

        public virtual DbContext GetMasterDbContext() => _masterDb.GetMasterDbContext();
        public virtual DbContext GetSlaveDbContext() => null;

        public virtual IQueryable<T> Queryable<T>(bool useMasterDb) where T : class, new()
        {
            return _masterDb.Queryable<T>();
        }

        #region Write

        public virtual IQueryable<T> Queryable<T>() where T : class, new()
        {
            //只有主库
            return Queryable<T>(true);
        }

        public virtual async Task<int> SaveChangesAsync() => await _masterDb.SaveChangesAsync();

        public virtual async Task<int> RunSqlAsync(string sql, params object[] args)
        {
            return await _masterDb.RunSqlAsync(sql, args);
        }


        public virtual async Task<int> RunSqlInterAsync(FormattableString strSql)
        {
            return await _masterDb.RunSqlInterAsync(strSql);
        }

        public virtual async ValueTask SaveAsync(object entity)
        {
            await _masterDb.SaveAsync(entity);
        }


        public virtual async ValueTask SaveAsync<T>(T entity) where T : class, new()
        {
            await _masterDb.SaveAsync(entity);
        }

        public virtual async ValueTask SaveAsync(IEnumerable<object> entities)
        {
            await _masterDb.SaveAsync(entities);
        }


        public virtual async ValueTask SaveAsync<T>(IEnumerable<T> entities) where T : class, new()
        {
            await _masterDb.SaveAsync(entities);
        }


        public virtual async ValueTask InsertAsync(object entity)
        {
            await _masterDb.InsertAsync(entity);
        }

        public virtual async ValueTask<object> InsertReturnIdentityAsync(object entity)
        {
            return await _masterDb.InsertReturnIdentityAsync(entity);
        }

        public virtual async ValueTask<TKey> InsertReturnIdentityAsync<TKey>(object entity)
        {
            return await _masterDb.InsertReturnIdentityAsync<TKey>(entity);
        }

        public virtual async ValueTask<TKey> InsertReturnIdentityAsync<TKey, TEntity>(TEntity entity) where TEntity : class, new()
        {
            return await _masterDb.InsertReturnIdentityAsync<TKey, TEntity>(entity);
        }

        public virtual async ValueTask InsertAsync<T>(T entity) where T : class, new()
        {
            await _masterDb.InsertAsync(entity);
        }

        public virtual async Task InsertRangeAsync<T>(IEnumerable<T> entities) where T : class, new()
        {
            await _masterDb.InsertRangeAsync(entities);
        }

        public virtual async Task UpdateAsync(object entity, bool updateAll = false)
        {
            await _masterDb.UpdateAsync(entity, updateAll);
        }

        public virtual async Task UpdateAsync<T>(T entity, bool updateAll = false) where T : class, new()
        {
            await _masterDb.UpdateAsync(entity, updateAll);
        }


        public virtual async Task UpdateRangeAsync<T>(IEnumerable<T> entities, bool updateAll = false) where T : class, new()
        {
            await _masterDb.UpdateRangeAsync(entities, updateAll);
        }


        public virtual async Task DeleteAsync(object entity)
        {
            await _masterDb.DeleteAsync(entity);
        }

        public virtual async Task DeleteAsync<T>(T entity) where T : class, new()
        {
            await _masterDb.DeleteAsync(entity);
        }

        public virtual async Task DeleteRangeAsync<T>(IEnumerable<T> entities) where T : class, new()
        {
            await _masterDb.DeleteRangeAsync(entities);
        }

        public virtual async Task DeleteRangeAsync(IEnumerable<object> entities)
        {
            await _masterDb.DeleteRangeAsync(entities);
        }


        public virtual async Task DeleteByIdAsync<T>(object keyValue) where T : class, new()
        {
            await _masterDb.DeleteByIdAsync<T>(keyValue);
        }

        #endregion

        #region Read

        public virtual async Task<T> FirstOrDefaultAsync<T>() where T : class, new()
        {
            return await _masterDb.FirstOrDefaultAsync<T>();
        }

        public virtual async ValueTask<T> FirstOrDefaultAsync<T>(params object[] keyValues) where T : class, new()
        {
            return await _masterDb.FirstOrDefaultAsync<T>(keyValues);
        }

        public virtual async ValueTask<object> FirstOrDefaultAsync(Type type, params object[] keys)
        {
            return await _masterDb.FirstOrDefaultAsync(type, keys);
        }

        public virtual async Task<T> FirstOrDefaultAsync<T>(Expression<Func<T, bool>> predicate) where T : class, new()
        {
            return await _masterDb.FirstOrDefaultAsync(predicate);
        }

        public virtual async Task<List<T>> ToListAsync<T>(Expression<Func<T, bool>> predicate = null) where T : class, new()
        {
            return await _masterDb.ToListAsync(predicate);
        }

        public virtual async Task<Pagination<T>> ToPageAsync<T>(PageInput input) where T : class, new()
        {
            return await _masterDb.ToPageAsync<T>(input);
        }

        public virtual async Task<List<T>> ToListAsync<T>(string sql, params object[] args) where T : class, new()
        {
            return await _masterDb.ToListAsync<T>(sql, args);
        }

        public virtual async Task<T> FirstOrDefaultAsync<T>(string sql, params object[] args) where T : class, new()
        {
            return await _masterDb.FirstOrDefaultAsync<T>(sql, args);
        }

        #endregion

        public virtual async Task<int> UseTransactionAsync(Func<Task> func)
        {
            return await _masterDb.UseTransactionAsync(func);
        }

        public virtual async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return await _masterDb.BeginTransactionAsync();
        }
    }
}