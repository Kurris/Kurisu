using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Kurisu.DataAccessor.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Kurisu.DataAccessor.Internal
{
    internal class ReadImplementation : IAppSlaveDb
    {
        internal ReadImplementation(DbContext dbContext)
        {
            DbContext = dbContext;
        }

        public virtual DbContext DbContext { get; }

        public IQueryable<T> Queryable<T>() where T : class, new()
        {
            return DbContext.Set<T>().AsQueryable();
        }


        public DbContext GetSlaveDbContext() => DbContext;

        public async Task<T> FirstOrDefaultAsync<T>() where T : class, new()
        {
            return await DbContext.Set<T>().FirstOrDefaultAsync();
        }

        public async ValueTask<T> FirstOrDefaultAsync<T>(params object[] keyValues) where T : class, new()
        {
            return await DbContext.Set<T>().FindAsync(keyValues);
        }

        public async ValueTask<object> FirstOrDefaultAsync(Type type, params object[] keys)
        {
            return await DbContext.FindAsync(type, keys);
        }

        public async Task<T> FirstOrDefaultAsync<T>(Expression<Func<T, bool>> predicate) where T : class, new()
        {
            return await DbContext.Set<T>().FirstOrDefaultAsync(predicate);
        }

        public async Task<List<T>> ToListAsync<T>(Expression<Func<T, bool>> predicate = null) where T : class, new()
        {
            return predicate == null
                ? await DbContext.Set<T>().ToListAsync()
                : await DbContext.Set<T>().Where(predicate).ToListAsync();
        }
    }
}