using System.Linq.Expressions;
using Kurisu.Core.DataAccess.Entity;
using SqlSugar;

namespace Kurisu.SqlSugar.Services.Implements;

internal class DbContext : IDbContext
{
    private readonly ISqlSugarClient _db;
    private readonly ISqlSugarOptionsService _sqlSugarOptionsService;

    public DbContext(ISqlSugarClient db, ISqlSugarOptionsService sqlSugarOptionsService)
    {
        _db = db;
        _sqlSugarOptionsService = sqlSugarOptionsService;
    }

    public ISqlSugarClient Client => _db;

    public ISugarQueryable<T> Queryable<T>()
    {
        return _db.Queryable<T>();
    }

    public IDbContext ChangeDb(string dbId)
    {
        var client = ((SqlSugarClient)_db).GetConnection(dbId);
        return new DbContext(client, _sqlSugarOptionsService);
    }

    public async Task<long> InsertReturnIdentityAsync<T>(T obj) where T : class, new()
    {
        return await _db.Insertable(obj).EnableDiffLogEventIF(_sqlSugarOptionsService.Diff).ExecuteReturnBigIdentityAsync();
    }

    public async Task<int> InsertAsync<T>(T obj) where T : class, new()
    {
        return await _db.Insertable(obj).EnableDiffLogEventIF(_sqlSugarOptionsService.Diff).ExecuteCommandAsync();
    }

    public async Task<int> InsertAsync<T>(T[] obj) where T : class, new()
    {
        return await _db.Insertable(obj).EnableDiffLogEventIF(_sqlSugarOptionsService.Diff).ExecuteCommandAsync();
    }

    public async Task<int> InsertAsync<T>(List<T> obj) where T : class, new()
    {
        return await _db.Insertable(obj).EnableDiffLogEventIF(_sqlSugarOptionsService.Diff).ExecuteCommandAsync();
    }


    public async Task<int> DeleteAsync<T>(T obj) where T : class, ISoftDeleted, new()
    {
        obj.IsDeleted = true;
        return await _db.Updateable(obj).EnableDiffLogEventIF(_sqlSugarOptionsService.Diff).ExecuteCommandAsync();
    }

    public async Task<int> DeleteAsync<T>(T[] obj) where T : class, ISoftDeleted, new()
    {
        var list = obj.ToList();
        list.ForEach(x => x.IsDeleted = true);
        return await _db.Updateable(list).EnableDiffLogEventIF(_sqlSugarOptionsService.Diff).ExecuteCommandAsync();
    }

    public async Task<int> DeleteAsync<T>(List<T> obj) where T : class, ISoftDeleted, new()
    {
        obj.ForEach(x => x.IsDeleted = true);
        return await _db.Updateable(obj).EnableDiffLogEventIF(_sqlSugarOptionsService.Diff).ExecuteCommandAsync();
    }

    public async Task<int> DeleteReallyAsync<T>(T obj) where T : class, new()
    {
        return await _db.Deleteable(obj).EnableDiffLogEventIF(_sqlSugarOptionsService.Diff).ExecuteCommandAsync();
    }

    public async Task<int> DeleteReallyAsync<T>(Expression<Func<T, bool>> expression) where T : class, new()
    {
        return await _db.Deleteable(expression).EnableDiffLogEventIF(_sqlSugarOptionsService.Diff).ExecuteCommandAsync();
    }

    public async Task<int> DeleteReallyAsync<T>(List<T> obj) where T : class, new()
    {
        return await _db.Deleteable(obj).EnableDiffLogEventIF(_sqlSugarOptionsService.Diff).ExecuteCommandAsync();
    }


    public async Task<int> UpdateAsync<T>(T obj) where T : class, new()
    {
        return await _db.Updateable(obj).EnableDiffLogEventIF(_sqlSugarOptionsService.Diff).ExecuteCommandAsync();
    }

    public async Task<int> UpdateAsync<T>(T[] obj) where T : class, new()
    {
        return await _db.Updateable(obj).EnableDiffLogEventIF(_sqlSugarOptionsService.Diff).ExecuteCommandAsync();
    }

    public async Task<int> UpdateAsync<T>(List<T> obj) where T : class, new()
    {
        return await _db.Updateable(obj).EnableDiffLogEventIF(_sqlSugarOptionsService.Diff).ExecuteCommandAsync();
    }

    public IUpdateable<T> Updateable<T>() where T : class, new()
    {
        return _db.Updateable<T>().EnableDiffLogEventIF(_sqlSugarOptionsService.Diff);
    }

    public Task<DbResult<T>> UseTransactionAsync<T>(Func<Task<T>> func, Action<Exception> callback = null)
    {
        return _db.Ado.UseTranAsync(func, callback);
    }

    public DbResult<T> UseTransaction<T>(Func<T> func, Action<Exception> callback = null)
    {
        return _db.Ado.UseTran(func, callback);
    }
}