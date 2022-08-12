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
        protected readonly IAppSlaveDb SlaveDb;

        public ReadWriteSplitAppDbService(IAppMasterDb appMasterDb, IAppSlaveDb appSlaveDb) : base(appMasterDb)
        {
            SlaveDb = appSlaveDb;
        }

        public override DbContext GetSlaveDbContext() => SlaveDb.GetSlaveDbContext();

        public override IQueryable<T> Queryable<T>(bool useMasterDb) where T : class
        {
            if (SlaveDb == null)
            {
                return MasterDb.Queryable<T>();
            }

            return useMasterDb
                ? MasterDb.Queryable<T>()
                : SlaveDb.Queryable<T>();
        }


        public override IQueryable<T> Queryable<T>() where T : class
        {
            //默认从库
            return Queryable<T>(false);
        }

        #region Read

        public override async Task<T> FirstOrDefaultAsync<T>() where T : class
        {
            if (SlaveDb == null)
            {
                return await MasterDb.FirstOrDefaultAsync<T>();
            }

            return await SlaveDb.FirstOrDefaultAsync<T>();
        }

        public override async ValueTask<T> FirstOrDefaultAsync<T>(params object[] keyValues) where T : class
        {
            if (SlaveDb == null)
            {
                return await MasterDb.FirstOrDefaultAsync<T>(keyValues);
            }

            return await SlaveDb.FirstOrDefaultAsync<T>(keyValues);
        }

        public override async ValueTask<object> FirstOrDefaultAsync(Type type, params object[] keys)
        {
            if (SlaveDb == null)
            {
                return await MasterDb.FirstOrDefaultAsync(type, keys);
            }

            return await SlaveDb.FirstOrDefaultAsync(type, keys);
        }

        public override async Task<T> FirstOrDefaultAsync<T>(Expression<Func<T, bool>> predicate) where T : class
        {
            if (SlaveDb == null)
            {
                return await MasterDb.FirstOrDefaultAsync(predicate);
            }

            return await SlaveDb.FirstOrDefaultAsync(predicate);
        }

        public override async Task<List<T>> ToListAsync<T>(Expression<Func<T, bool>> predicate = null) where T : class
        {
            if (SlaveDb == null)
            {
                return await MasterDb.ToListAsync(predicate);
            }

            return await SlaveDb.ToListAsync(predicate);
        }

        public override async Task<Pagination<T>> ToPageAsync<T>(PageInput input) where T : class
        {
            if (SlaveDb == null)
            {
                return await MasterDb.ToPageAsync<T>(input);
            }

            return await SlaveDb.ToPageAsync<T>(input);
        }

        public override async Task<List<T>> ToListAsync<T>(string sql, params object[] args) where T : class
        {
            if (SlaveDb == null)
            {
                return await MasterDb.ToListAsync<T>(sql, args);
            }

            return await SlaveDb.ToListAsync<T>(sql, args);
        }

        public override async Task<T> FirstOrDefaultAsync<T>(string sql, params object[] args) where T : class
        {
            if (SlaveDb == null)
            {
                return await MasterDb.FirstOrDefaultAsync<T>(sql, args);
            }

            return await SlaveDb.FirstOrDefaultAsync<T>(sql, args);
        }

        #endregion
    }
}