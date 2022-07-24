using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Kurisu.DataAccessor.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Kurisu.DataAccessor.Internal
{
    /// <summary>
    /// 数据库访问服务
    /// </summary>
    internal class AppDbService : IAppDbService
    {
        private readonly IAppSlaveDb _slaveDb;

        private readonly IAppMasterDb _masterDb;

        public AppDbService(IAppSlaveDb appSlaveDb, IAppMasterDb appMasterDb)
        {
            _slaveDb = appSlaveDb;
            _masterDb = appMasterDb;
        }

        public DbContext GetMasterDbContext() => _masterDb.GetMasterDbContext();

        public DbContext GetSlaveDbContext() => _slaveDb.GetSlaveDbContext();

        public IQueryable<T> Queryable<T>(bool useMasterDb) where T : class, new()
        {
            if (_slaveDb == null)
            {
                return _masterDb.Queryable<T>();
            }

            return useMasterDb
                ? _masterDb.Queryable<T>()
                : _slaveDb.Queryable<T>();
        }

        #region Write

        public IQueryable<T> Queryable<T>() where T : class, new()
        {
            return Queryable<T>(false);
        }

        public async Task<int> SaveChangesAsync() => await _masterDb.SaveChangesAsync();

        public async Task<int> RunSqlAsync(string sql, params object[] args)
        {
            return await _masterDb.RunSqlAsync(sql, args);
        }


        public async Task<int> RunSqlInterAsync(FormattableString strSql)
        {
            return await _masterDb.RunSqlInterAsync(strSql);
        }

        public async ValueTask SaveAsync(object entity)
        {
            await _masterDb.SaveAsync(entity);
        }


        public async ValueTask SaveAsync<T>(T entity) where T : class, new()
        {
            await _masterDb.SaveAsync(entity);
        }

        public async ValueTask SaveAsync(IEnumerable<object> entities)
        {
            await _masterDb.SaveAsync(entities);
        }


        public async ValueTask SaveAsync<T>(IEnumerable<T> entities) where T : class, new()
        {
            await _masterDb.SaveAsync(entities);
        }


        public async ValueTask InsertAsync(object entity)
        {
            await _masterDb.InsertAsync(entity);
        }

        public async ValueTask<object> InsertReturnIdentityAsync(object entity)
        {
            return await _masterDb.InsertReturnIdentityAsync(entity);
        }

        public async ValueTask<TKey> InsertReturnIdentityAsync<TKey>(object entity)
        {
            return await _masterDb.InsertReturnIdentityAsync<TKey>(entity);
        }

        public async ValueTask<TKey> InsertReturnIdentityAsync<TKey, TEntity>(TEntity entity) where TEntity : class, new()
        {
            return await _masterDb.InsertReturnIdentityAsync<TKey, TEntity>(entity);
        }

        public async ValueTask InsertAsync<T>(T entity) where T : class, new()
        {
            await _masterDb.InsertAsync(entity);
        }

        public async Task InsertRangeAsync<T>(IEnumerable<T> entities) where T : class, new()
        {
            await _masterDb.InsertRangeAsync(entities);
        }

        public async Task UpdateAsync(object entity, bool updateAll = false)
        {
            await _masterDb.UpdateAsync(entity, updateAll);
        }

        public async Task UpdateAsync<T>(T entity, bool updateAll = false) where T : class, new()
        {
            await _masterDb.UpdateAsync(entity, updateAll);
        }


        public async Task UpdateRangeAsync<T>(IEnumerable<T> entities, bool updateAll = false) where T : class, new()
        {
            await _masterDb.UpdateRangeAsync(entities, updateAll);
        }


        public async Task DeleteAsync(object entity)
        {
            await _masterDb.DeleteAsync(entity);
        }

        public async Task DeleteAsync<T>(T entity) where T : class, new()
        {
            await _masterDb.DeleteAsync(entity);
        }

        public async Task DeleteRangeAsync<T>(IEnumerable<T> entities) where T : class, new()
        {
            await _masterDb.DeleteRangeAsync(entities);
        }

        public async Task DeleteRangeAsync(IEnumerable<object> entities)
        {
            await _masterDb.DeleteRangeAsync(entities);
        }


        public async Task DeleteByIdAsync<T>(object keyValue) where T : class, new()
        {
            await _masterDb.DeleteByIdAsync<T>(keyValue);
        }

        #endregion

        #region Read

        public async Task<T> FirstOrDefaultAsync<T>() where T : class, new()
        {
            if (_slaveDb == null)
            {
                return await _masterDb.FirstOrDefaultAsync<T>();
            }

            return await _slaveDb.FirstOrDefaultAsync<T>();
        }

        public async ValueTask<T> FirstOrDefaultAsync<T>(params object[] keyValues) where T : class, new()
        {
            if (_slaveDb == null)
            {
                return await _masterDb.FirstOrDefaultAsync<T>(keyValues);
            }

            return await _slaveDb.FirstOrDefaultAsync<T>(keyValues);
        }

        public async ValueTask<object> FirstOrDefaultAsync(Type type, params object[] keys)
        {
            if (_slaveDb == null)
            {
                return await _masterDb.FirstOrDefaultAsync(type, keys);
            }

            return await _slaveDb.FirstOrDefaultAsync(type, keys);
        }

        public async Task<T> FirstOrDefaultAsync<T>(Expression<Func<T, bool>> predicate) where T : class, new()
        {
            if (_slaveDb == null)
            {
                return await _masterDb.FirstOrDefaultAsync(predicate);
            }

            return await _slaveDb.FirstOrDefaultAsync(predicate);
        }

        public async Task<List<T>> ToListAsync<T>(Expression<Func<T, bool>> predicate = null) where T : class, new()
        {
            if (_slaveDb == null)
            {
                return await _masterDb.ToListAsync(predicate);
            }

            return await _slaveDb.ToListAsync(predicate);
        }

        public async Task UseTransactionAsync(Action action)
        {
            await _masterDb.UseTransactionAsync(action);
        }

        #endregion
    }
}