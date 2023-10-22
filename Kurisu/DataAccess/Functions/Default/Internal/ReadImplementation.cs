//using System;
//using System.Collections.Generic;
//using System.Data;
//using System.Linq;
//using System.Linq.Expressions;
//using System.Threading.Tasks;
//using Kurisu.DataAccess.Dto;
//using Kurisu.DataAccess.Functions.Default.Abstractions;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.DependencyInjection;

//namespace Kurisu.DataAccess.Functions.Default.Internal;

///// <summary>
///// 数据操作(读)
///// </summary>
//public class ReadImplementation : IDbRead
//{
//    private readonly DbContext _dbContext;

//    public ReadImplementation(DbContext dbContext)
//    {
//        _dbContext = dbContext;
//    }

//    public virtual DbContext GetDbContext() => _dbContext;

//    public virtual IQueryable<T> AsQueryable<T>() where T : class, new()
//    {
//        return _dbContext.Set<T>().AsQueryable();
//    }

//    public virtual IDbRead AsNoTracking()
//    {
//        _dbContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
//        return this;
//    }

//    public virtual IQueryable<T> AsNoTracking<T>() where T : class, new()
//    {
//        return _dbContext.Set<T>().AsNoTracking();
//    }

//    #region first

//    public virtual async Task<T> FirstOrDefaultAsync<T>() where T : class, new()
//    {
//        return await _dbContext.Set<T>().FirstOrDefaultAsync();
//    }

//    public virtual async ValueTask<T> FirstOrDefaultAsync<T>(params object[] keyValues) where T : class, new()
//    {
//        return await _dbContext.Set<T>().FindAsync(keyValues);
//    }

//    public virtual async ValueTask<object> FirstOrDefaultAsync(Type type, params object[] keys)
//    {
//        return await _dbContext.FindAsync(type, keys);
//    }

//    public virtual async Task<T> FirstOrDefaultAsync<T>(Expression<Func<T, bool>> predicate) where T : class, new()
//    {
//        return await _dbContext.Set<T>().FirstOrDefaultAsync(predicate);
//    }

//    #endregion

//    #region list

//    public virtual async Task<List<T>> ToListAsync<T>() where T : class, new()
//    {
//        return await _dbContext.Set<T>().ToListAsync();
//    }

//    public virtual async Task<List<T>> ToListAsync<T>(Expression<Func<T, bool>> predicate) where T : class, new()
//    {
//        return await _dbContext.Set<T>().Where(predicate).ToListAsync();
//    }

//    #endregion

//    #region page

//    public virtual async Task<Pagination<T>> ToPageAsync<T>(PageInput input) where T : class, new()
//    {
//        return await AsQueryable<T>().ToPageAsync(input);
//    }

//    public virtual async Task<Pagination<T>> ToPageAsync<T>(PageInput input, Expression<Func<T, bool>> predicate) where T : class, new()
//    {
//        return await AsQueryable<T>().Where(predicate).ToPageAsync(input);
//    }

//    #endregion

//    #region sql

//    public virtual async Task<List<T>> ToListAsync<T>(string sql, params object[] args) where T : class, new()
//    {
//        return await _dbContext.Set<T>().FromSqlRaw(sql, args).ToListAsync();
//    }

//    public virtual async Task<List<T>> ToListAsync<T>(FormattableString sql) where T : class, new()
//    {
//        return await _dbContext.Set<T>().FromSqlInterpolated(sql).ToListAsync();
//    }

//    public virtual async Task<T> FirstOrDefaultAsync<T>(string sql, params object[] args) where T : class, new()
//    {
//        return await _dbContext.Set<T>().FromSqlRaw(sql, args).FirstOrDefaultAsync();
//    }

//    public virtual async Task<T> FirstOrDefaultAsync<T>(FormattableString sql) where T : class, new()
//    {
//        return await _dbContext.Set<T>().FromSqlInterpolated(sql).FirstOrDefaultAsync();
//    }

//    public virtual async Task<Pagination<T>> ToPageAsync<T>(string sql, PageInput input) where T : class, new()
//    {
//        var pageIndex = input.PageIndex;
//        var pageSize = input.PageSize;

//        var conn = _dbContext.Database.GetDbConnection();

//        if (conn.State.Equals(ConnectionState.Closed))
//            await conn.OpenAsync();

//        await using var command = conn.CreateCommand();

//        command.CommandText = $@" select count(1) from ({sql}) t ";

//        var totalCount = (int?) await command.ExecuteScalarAsync();
//        var data = await ToListAsync<T>(sql + " limit @i,@s ", pageIndex, pageSize);


//        return new Pagination<T>
//        {
//            PageIndex = pageIndex,
//            PageSize = pageSize,
//            Total = totalCount ?? 0,
//            Data = data
//        };
//    }

//    #endregion
//}