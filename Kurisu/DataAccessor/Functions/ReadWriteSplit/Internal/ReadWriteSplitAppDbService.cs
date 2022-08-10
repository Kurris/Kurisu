using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Kurisu.DataAccessor.Dto;
using Kurisu.DataAccessor.Functions.Default.Internal;
using Kurisu.DataAccessor.Functions.ReadWriteSplit.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Kurisu.DataAccessor.Functions.ReadWriteSplit.Internal
{
    /// <summary>
    /// 数据库访问服务
    /// </summary>
    public class ReadWriteSplitAppDbService : DefaultAppDbService
    {
        private readonly IAppMasterDb _masterDb;
        private readonly IAppSlaveDb _slaveDb;

        public ReadWriteSplitAppDbService(IAppMasterDb appMasterDb, IAppSlaveDb appSlaveDb) : base(appMasterDb)
        {
            _masterDb = appMasterDb;
            _slaveDb = appSlaveDb;
        }

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


        public override IQueryable<T> Queryable<T>() where T : class
        {
            //默认从库
            return Queryable<T>(false);
        }

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
    }
}