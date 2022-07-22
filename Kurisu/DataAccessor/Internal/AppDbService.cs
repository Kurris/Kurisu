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
    /// app 数据库访问服务
    /// </summary>
    internal class AppDbService : IAppDbService
    {
        public AppDbService(Func<Type, IDbService> dbResolver)
        {
            SlaveDb = dbResolver.Invoke(typeof(IAppSlaveDb)) as IAppSlaveDb;
            MasterDb = dbResolver.Invoke(typeof(IAppMasterDb)) as IAppMasterDb;
        }

        public IAppSlaveDb SlaveDb { get; }

        public IAppMasterDb MasterDb { get; }

        public DbContext GetMasterDbContext() => MasterDb.GetMasterDbContext();

        public DbContext GetSlaveDbContext() => SlaveDb.GetSlaveDbContext();

        public IQueryable<T> Queryable<T>(bool useMasterDb) where T : class, new()
        {
            if (SlaveDb == null)
            {
                return MasterDb.Queryable<T>();
            }

            return useMasterDb
                ? MasterDb.Queryable<T>()
                : SlaveDb.Queryable<T>();
        }

        #region Write

        public IQueryable<T> Queryable<T>() where T : class, new()
        {
            return Queryable<T>(false);
        }

        public async Task<int> SaveChangesAsync() => await CommitToDbAsync();

        public async Task<int> RunSqlAsync(string sql, params object[] args) => await MasterDb.RunSqlAsync(sql, args);


        public async Task<int> RunSqlInterAsync(FormattableString strSql) => await MasterDb.RunSqlInterAsync(strSql);

        public async ValueTask SaveAsync(object entity) => await MasterDb.SaveAsync(entity);


        public async ValueTask SaveAsync<T>(T entity) where T : class, new() => await MasterDb.SaveAsync(entity);

        public async ValueTask SaveAsync(IEnumerable<object> entities) => await MasterDb.SaveAsync(entities);


        public async ValueTask SaveAsync<T>(IEnumerable<T> entities) where T : class, new() => await MasterDb.SaveAsync(entities);


        public async ValueTask InsertAsync(object entity)
        {
            await MasterDb.InsertAsync(entity);
            await CommitToDbAsync();
        }

        public async ValueTask<object> InsertReturnIdentityAsync(object entity) => await MasterDb.InsertReturnIdentityAsync(entity);

        public async ValueTask InsertAsync<T>(T entity) where T : class, new()
        {
            await MasterDb.InsertAsync(entity);
            await CommitToDbAsync();
        }

        public async Task InsertRangeAsync<T>(IEnumerable<T> entities) where T : class, new() => await MasterDb.InsertRangeAsync(entities);

        public async Task UpdateAsync(object entity, bool updateAll = false) => await MasterDb.UpdateAsync(entity, updateAll);

        public async Task UpdateAsync<T>(T entity, bool updateAll = false) where T : class, new() => await MasterDb.UpdateAsync(entity, updateAll);


        public async Task UpdateRangeAsync<T>(IEnumerable<T> entities, bool updateAll = false) where T : class, new() => await MasterDb.UpdateRangeAsync(entities, updateAll);


        public async Task DeleteAsync(object entity) => await MasterDb.DeleteAsync(entity);

        public async Task DeleteAsync<T>(T entity) where T : class, new() => await MasterDb.DeleteAsync(entity);

        public async Task DeleteRangeAsync<T>(IEnumerable<T> entities) where T : class, new() => await MasterDb.DeleteRangeAsync(entities);


        public async Task DeleteByIdAsync<T>(object keyValue) where T : class, new() => await MasterDb.DeleteByIdAsync<T>(keyValue);

        #endregion

        /// <summary>
        /// 数据变更提交到Db
        /// </summary>
        /// <returns></returns>
        private async Task<int> CommitToDbAsync()
        {
            if ((GetMasterDbContext() as IAppDbContext).IsAutomaticSaveChanges)
            {
                return 0;
            }

            return await MasterDb.SaveChangesAsync();
        }


        #region Read

        public async Task<T> FirstOrDefaultAsync<T>() where T : class, new()
        {
            if (SlaveDb == null)
            {
                return await MasterDb.FirstOrDefaultAsync<T>();
            }

            return await SlaveDb.FirstOrDefaultAsync<T>();
        }

        public async ValueTask<T> FirstOrDefaultAsync<T>(params object[] keyValues) where T : class, new()
        {
            if (SlaveDb == null)
            {
                return await MasterDb.FirstOrDefaultAsync<T>(keyValues);
            }

            return await SlaveDb.FirstOrDefaultAsync<T>(keyValues);
        }

        public async ValueTask<object> FirstOrDefaultAsync(Type type, params object[] keys)
        {
            if (SlaveDb == null)
            {
                return await MasterDb.FirstOrDefaultAsync(type, keys);
            }

            return await SlaveDb.FirstOrDefaultAsync(type, keys);
        }

        public async Task<T> FirstOrDefaultAsync<T>(Expression<Func<T, bool>> predicate) where T : class, new()
        {
            if (SlaveDb == null)
            {
                return await MasterDb.FirstOrDefaultAsync(predicate);
            }

            return await SlaveDb.FirstOrDefaultAsync(predicate);
        }

        public async Task<List<T>> ToListAsync<T>(Expression<Func<T, bool>> predicate = null) where T : class, new()
        {
            if (SlaveDb == null)
            {
                return await MasterDb.ToListAsync(predicate);
            }

            return await SlaveDb.ToListAsync(predicate);
        }

        #endregion
    }
}