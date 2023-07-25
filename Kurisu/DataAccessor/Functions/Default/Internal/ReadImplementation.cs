using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Kurisu.DataAccessor.Dto;
using Kurisu.DataAccessor.Functions.ReadWriteSplit.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.DataAccessor.Functions.Default.Internal;

/// <summary>
/// 数据操作(读)
/// </summary>
public class ReadImplementation : IAppSlaveDb
{
    public ReadImplementation(DbContext dbContext)
    {
        DbContext = dbContext;
    }

    protected virtual DbContext DbContext { get; }
    public virtual DbContext GetSlaveDbContext() => DbContext;

    public virtual IQueryable<T> Queryable<T>() where T : class, new()
    {
        return DbContext.Set<T>().AsQueryable();
    }

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

    public virtual async Task<Pagination<T>> ToPageAsync<T>(PageInput input, Expression<Func<T, bool>> predicate = null) where T : class, new()
    {
        return predicate == null
            ? await Queryable<T>().ToPageAsync()
            : await Queryable<T>().Where(predicate).ToPageAsync();
    }

    public virtual async Task<List<T>> ToListAsync<T>(string sql, params object[] args) where T : class, new()
    {
        return await DbContext.Set<T>().FromSqlRaw(sql, args).ToListAsync();
    }

    public async Task<List<T>> ToListAsync<T>(FormattableString sql) where T : class, new()
    {
        return await DbContext.Set<T>().FromSqlInterpolated(sql).ToListAsync();
    }

    public virtual async Task<T> FirstOrDefaultAsync<T>(string sql, params object[] args) where T : class, new()
    {
        return await DbContext.Set<T>().FromSqlRaw(sql, args).FirstOrDefaultAsync();
    }

    public async Task<T> FirstOrDefaultAsync<T>(FormattableString sql) where T : class, new()
    {
        return await DbContext.Set<T>().FromSqlInterpolated(sql).FirstOrDefaultAsync();
    }
}