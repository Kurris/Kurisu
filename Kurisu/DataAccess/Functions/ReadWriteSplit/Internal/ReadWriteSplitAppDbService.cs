using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Kurisu.DataAccess.Dto;
using Kurisu.DataAccess.Functions.Default.Abstractions;
using Kurisu.DataAccess.Functions.Default.Internal;

namespace Kurisu.DataAccess.Functions.ReadWriteSplit.Internal;

/// <summary>
/// 数据库访问服务
/// </summary>
public class ReadWriteSplitAppDbService : DefaultAppDbService
{
    protected readonly IDbRead _dbRead;

    public ReadWriteSplitAppDbService(IDbWrite dbWrite, IDbRead dbRead) : base(dbWrite)
    {
        _dbRead = dbRead;
    }

    public override IQueryable<T> AsQueryable<T>(bool useWriteDb)
    {
        if (useWriteDb)
            return base.AsQueryable<T>();

        return _dbRead.AsQueryable<T>();
    }

    public override IQueryable<T> AsQueryable<T>()
    {
        return _dbRead.AsQueryable<T>();
    }

    public override Task<T> FirstOrDefaultAsync<T>()
    {
        return _dbRead.FirstOrDefaultAsync<T>();
    }

    public override ValueTask<T> FirstOrDefaultAsync<T>(params object[] keyValues)
    {
        return _dbRead.FirstOrDefaultAsync<T>(keyValues);
    }

    public override ValueTask<object> FirstOrDefaultAsync(Type type, params object[] keys)
    {
        return _dbRead.FirstOrDefaultAsync(type, keys);
    }

    public override Task<T> FirstOrDefaultAsync<T>(Expression<Func<T, bool>> predicate)
    {
        return _dbRead.FirstOrDefaultAsync(predicate);
    }

    public override Task<T> FirstOrDefaultAsync<T>(string sql, params object[] args)
    {
        return _dbRead.FirstOrDefaultAsync<T>(sql, args);
    }

    public override Task<T> FirstOrDefaultAsync<T>(FormattableString sql)
    {
        return _dbRead.FirstOrDefaultAsync<T>(sql);
    }

    public override Task<List<T>> ToListAsync<T>()
    {
        return _dbRead.ToListAsync<T>();
    }

    public override Task<List<T>> ToListAsync<T>(Expression<Func<T, bool>> predicate)
    {
        return _dbRead.ToListAsync(predicate);
    }

    public override Task<List<T>> ToListAsync<T>(string sql, params object[] args)
    {
        return _dbRead.ToListAsync<T>(sql, args);
    }

    public override Task<List<T>> ToListAsync<T>(FormattableString sql)
    {
        return _dbRead.ToListAsync<T>(sql);
    }

    public override Task<Pagination<T>> ToPageAsync<T>(PageInput input)
    {
        return _dbRead.ToPageAsync<T>(input);
    }

    public override Task<Pagination<T>> ToPageAsync<T>(PageInput input, Expression<Func<T, bool>> predicate)
    {
        return _dbRead.ToPageAsync(input, predicate);
    }

    public override Task<Pagination<T>> ToPageAsync<T>(string sql, PageInput input)
    {
        return _dbRead.ToPageAsync<T>(sql, input);
    }
}