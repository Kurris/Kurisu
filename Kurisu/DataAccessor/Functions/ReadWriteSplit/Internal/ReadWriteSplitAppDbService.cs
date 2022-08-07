using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Kurisu.DataAccessor.Dto;
using Kurisu.DataAccessor.Functions.Default.Internal;
using Kurisu.DataAccessor.Functions.ReadWriteSplit.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Kurisu.DataAccessor.Functions.ReadWriteSplit.Internal
{
    /// <summary>
    /// 数据库访问服务
    /// </summary>
    public class ReadWriteSplitAppDbService : DefaultAppDbService
    {
        private readonly IAppSlaveDb _slaveDb;
        private readonly IAppMasterDb _masterDb;

        public ReadWriteSplitAppDbService(IAppSlaveDb appSlaveDb, IAppMasterDb appMasterDb) : base(appMasterDb)
        {
            _slaveDb = appSlaveDb;
            _masterDb = appMasterDb;
        }

        public override DbContext GetMasterDbContext() => _masterDb.GetMasterDbContext();
        public override DbContext GetSlaveDbContext() => _slaveDb.GetSlaveDbContext();

        public override IQueryable<T> Queryable<T>(bool useMasterDb) where T : class
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

        public override IQueryable<T> Queryable<T>() where T : class
        {
            //默认从库
            return Queryable<T>(false);
        }

        public override async Task<int> SaveChangesAsync() => await _masterDb.SaveChangesAsync();

        public override async Task<int> RunSqlAsync(string sql, params object[] args)
        {
            return await _masterDb.RunSqlAsync(sql, args);
        }


        public override async Task<int> RunSqlInterAsync(FormattableString strSql)
        {
            return await _masterDb.RunSqlInterAsync(strSql);
        }

        public override async ValueTask SaveAsync(object entity)
        {
            await _masterDb.SaveAsync(entity);
        }


        public override async ValueTask SaveAsync<T>(T entity) where T : class
        {
            await _masterDb.SaveAsync(entity);
        }

        public override async ValueTask SaveAsync(IEnumerable<object> entities)
        {
            await _masterDb.SaveAsync(entities);
        }


        public override async ValueTask SaveAsync<T>(IEnumerable<T> entities) where T : class
        {
            await _masterDb.SaveAsync(entities);
        }


        public override async ValueTask InsertAsync(object entity)
        {
            await _masterDb.InsertAsync(entity);
        }

        public override async ValueTask<object> InsertReturnIdentityAsync(object entity)
        {
            return await _masterDb.InsertReturnIdentityAsync(entity);
        }

        public override async ValueTask<TKey> InsertReturnIdentityAsync<TKey>(object entity)
        {
            return await _masterDb.InsertReturnIdentityAsync<TKey>(entity);
        }

        public override async ValueTask<TKey> InsertReturnIdentityAsync<TKey, TEntity>(TEntity entity) where TEntity : class
        {
            return await _masterDb.InsertReturnIdentityAsync<TKey, TEntity>(entity);
        }

        public override async ValueTask InsertAsync<T>(T entity) where T : class
        {
            await _masterDb.InsertAsync(entity);
        }

        public override async Task InsertRangeAsync<T>(IEnumerable<T> entities) where T : class
        {
            await _masterDb.InsertRangeAsync(entities);
        }

        public override async Task UpdateAsync(object entity, bool updateAll = false)
        {
            await _masterDb.UpdateAsync(entity, updateAll);
        }

        public override async Task UpdateAsync<T>(T entity, bool updateAll = false) where T : class
        {
            await _masterDb.UpdateAsync(entity, updateAll);
        }


        public override async Task UpdateRangeAsync<T>(IEnumerable<T> entities, bool updateAll = false) where T : class
        {
            await _masterDb.UpdateRangeAsync(entities, updateAll);
        }


        public override async Task DeleteAsync(object entity)
        {
            await _masterDb.DeleteAsync(entity);
        }

        public override async Task DeleteAsync<T>(T entity) where T : class
        {
            await _masterDb.DeleteAsync(entity);
        }

        public override async Task DeleteRangeAsync<T>(IEnumerable<T> entities) where T : class
        {
            await _masterDb.DeleteRangeAsync(entities);
        }

        public override async Task DeleteRangeAsync(IEnumerable<object> entities)
        {
            await _masterDb.DeleteRangeAsync(entities);
        }


        public override async Task DeleteByIdAsync<T>(object keyValue) where T : class
        {
            await _masterDb.DeleteByIdAsync<T>(keyValue);
        }

        #endregion

        #region Read

        public override async Task<T> FirstOrDefaultAsync<T>() where T : class
        {
            if (_slaveDb == null)
            {
                return await _masterDb.FirstOrDefaultAsync<T>();
            }

            return await _slaveDb.FirstOrDefaultAsync<T>();
        }

        public override async ValueTask<T> FirstOrDefaultAsync<T>(params object[] keyValues) where T : class
        {
            if (_slaveDb == null)
            {
                return await _masterDb.FirstOrDefaultAsync<T>(keyValues);
            }

            return await _slaveDb.FirstOrDefaultAsync<T>(keyValues);
        }

        public override async ValueTask<object> FirstOrDefaultAsync(Type type, params object[] keys)
        {
            if (_slaveDb == null)
            {
                return await _masterDb.FirstOrDefaultAsync(type, keys);
            }

            return await _slaveDb.FirstOrDefaultAsync(type, keys);
        }

        public override async Task<T> FirstOrDefaultAsync<T>(Expression<Func<T, bool>> predicate) where T : class
        {
            if (_slaveDb == null)
            {
                return await _masterDb.FirstOrDefaultAsync(predicate);
            }

            return await _slaveDb.FirstOrDefaultAsync(predicate);
        }

        public override async Task<List<T>> ToListAsync<T>(Expression<Func<T, bool>> predicate = null) where T : class
        {
            if (_slaveDb == null)
            {
                return await _masterDb.ToListAsync(predicate);
            }

            return await _slaveDb.ToListAsync(predicate);
        }

        public override async Task<Pagination<T>> ToPageAsync<T>(PageInput input) where T : class
        {
            if (_slaveDb == null)
            {
                return await _masterDb.ToPageAsync<T>(input);
            }

            return await _slaveDb.ToPageAsync<T>(input);
        }

        public override async Task<List<T>> ToListAsync<T>(string sql, params object[] args) where T : class
        {
            if (_slaveDb == null)
            {
                return await _masterDb.ToListAsync<T>(sql, args);
            }

            return await _slaveDb.ToListAsync<T>(sql, args);
        }

        public override async Task<T> FirstOrDefaultAsync<T>(string sql, params object[] args) where T : class
        {
            if (_slaveDb == null)
            {
                return await _masterDb.FirstOrDefaultAsync<T>(sql, args);
            }

            return await _slaveDb.FirstOrDefaultAsync<T>(sql, args);
        }

        #endregion

        public override async Task<int> UseTransactionAsync(Func<Task> func)
        {
            return await _masterDb.UseTransactionAsync(func);
        }

        public override async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return await _masterDb.BeginTransactionAsync();
        }
    }
}
