using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Kurisu.DataAccessor.Dto;
using Kurisu.DataAccessor.Extensions;
using Kurisu.DataAccessor.Functions.ReadWriteSplit.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Kurisu.DataAccessor.Functions.Default.Internal
{
    /// <summary>
    /// 数据操作(读)
    /// </summary>
    public class ReadImplementation : IAppSlaveDb
    {
        public ReadImplementation(DbContext dbContext)
        {
            dbContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTrackingWithIdentityResolution;
            DbContext = dbContext;
        }

        protected virtual DbContext DbContext { get; }

        public virtual IQueryable<T> Queryable<T>() where T : class, new()
        {
            return DbContext.Set<T>().AsQueryable();
        }


        public virtual DbContext GetSlaveDbContext() => DbContext;

        public virtual async Task<T> FirstOrDefaultAsync<T>() where T : class, new()
        {
            return await DbContext.Set<T>().FirstOrDefaultAsync();
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
            return await DbContext.Set<T>().FirstOrDefaultAsync(predicate);
        }

        public virtual async Task<List<T>> ToListAsync<T>(Expression<Func<T, bool>> predicate = null) where T : class, new()
        {
            return predicate == null
                ? await DbContext.Set<T>().ToListAsync()
                : await DbContext.Set<T>().Where(predicate).ToListAsync();
        }

        public virtual async Task<Pagination<T>> ToPageAsync<T>(PageInput input) where T : class, new()
        {
            return await Queryable<T>().ToPageAsync(input);
        }

        public virtual async Task<List<T>> ToListAsync<T>(string sql, params object[] args) where T : class, new()
        {
            return await DbContext.Set<T>().FromSqlRaw(sql, args).ToListAsync();
        }

        public virtual async Task<T> FirstOrDefaultAsync<T>(string sql, params object[] args) where T : class, new()
        {
            return await DbContext.Set<T>().FromSqlRaw(sql, args).FirstOrDefaultAsync();
        }
    }
}