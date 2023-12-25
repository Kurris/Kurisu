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
        return _db.Updateable<T>();
    }
}