﻿using System.Data;
using System.Linq.Expressions;
using Kurisu.Core.DataAccess.Entity;
using Kurisu.Core.User.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;

namespace Kurisu.SqlSugar.Services.Implements;

internal class DbContext : IDbContext
{
    private readonly ISqlSugarClient _db;
    private readonly IServiceProvider _serviceProvider;
    private readonly ISqlSugarOptionsService _sqlSugarOptionsService;

    public DbContext(ISqlSugarClient db, IServiceProvider serviceProvider)
    {
        _db = db;
        _serviceProvider = serviceProvider;
        _sqlSugarOptionsService = serviceProvider.GetService<ISqlSugarOptionsService>();
    }

    public ISqlSugarClient Client => _db;

    public ISugarQueryable<T> Queryable<T>()
    {
        var dataPermission = _serviceProvider.GetService<DataPermissionService>();
        if (dataPermission.Enable && !dataPermission.UseSqlWhere && typeof(T).IsAssignableTo(typeof(ITenantId)))
        {
            var tenantIds = _serviceProvider.GetService<ICurrentUser>().GetUserClaim("tenants").Split(",").ToList();

            var parameterExpression = Expression.Parameter(typeof(T));
            var prop = Expression.Property(parameterExpression, nameof(ITenantId.TenantId));
            var eq = typeof(List<string>).GetMethod("Contains", new[] { typeof(string) });
            var constant = Expression.Constant(tenantIds, typeof(List<string>));
            var containsExp = Expression.Call(constant, eq, prop);

            var where = Expression.Lambda<Func<T, bool>>(containsExp, parameterExpression);

            return _db.Queryable<T>().Where(where);
        }
        else
        {
            return _db.Queryable<T>();
        }
    }

    public IDbContext ChangeDb(string dbId)
    {
        var client = ((SqlSugarClient)_db).GetConnection(dbId);
        return new DbContext(client, _serviceProvider);
    }

    #region insert

    public async Task<long> InsertReturnIdentityAsync<T>(T obj) where T : class, new()
    {
        return await _db.Insertable(obj).ExecuteReturnBigIdentityAsync();
    }

    public async Task<int> InsertAsync<T>(T obj) where T : class, new()
    {
        return await _db.Insertable(obj).ExecuteCommandAsync();
    }

    public async Task<int> InsertAsync<T>(T[] obj) where T : class, new()
    {
        return await _db.Insertable(obj).ExecuteCommandAsync();
    }

    public async Task<int> InsertAsync<T>(List<T> obj) where T : class, new()
    {
        return await _db.Insertable(obj).ExecuteCommandAsync();
    }

    public long InsertReturnIdentity<T>(T obj) where T : class, new()
    {
        return _db.Insertable(obj).ExecuteReturnBigIdentity();
    }

    public int Insert<T>(T obj) where T : class, new()
    {
        return _db.Insertable(obj).ExecuteCommand();
    }

    public int Insert<T>(T[] obj) where T : class, new()
    {
        return _db.Insertable(obj).ExecuteCommand();
    }

    public int Insert<T>(List<T> obj) where T : class, new()
    {
        return _db.Insertable(obj).ExecuteCommand();
    }

    #endregion


    #region delete

    public async Task<int> DeleteAsync<T>(T obj) where T : class, ISoftDeleted, new()
    {
        obj.IsDeleted = true;
        return await _db.Updateable(obj).ExecuteCommandAsync();
    }

    public async Task<int> DeleteAsync<T>(T[] obj) where T : class, ISoftDeleted, new()
    {
        var list = obj.ToList();
        list.ForEach(x => x.IsDeleted = true);
        return await _db.Updateable(list).ExecuteCommandAsync();
    }

    public async Task<int> DeleteAsync<T>(List<T> obj) where T : class, ISoftDeleted, new()
    {
        obj.ForEach(x => x.IsDeleted = true);
        return await _db.Updateable(obj).ExecuteCommandAsync();
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


    public int Delete<T>(T obj) where T : class, ISoftDeleted, new()
    {
        obj.IsDeleted = true;
        return Update(obj);
    }

    public int Delete<T>(T[] obj) where T : class, ISoftDeleted, new()
    {
        var list = obj.ToList();
        list.ForEach(x => x.IsDeleted = true);
        return Update(obj);
    }

    public int Delete<T>(List<T> obj) where T : class, ISoftDeleted, new()
    {
        obj.ForEach(x => x.IsDeleted = true);
        return Update(obj);
    }

    public IDeleteable<T> Deleteable<T>() where T : class, new()
    {
        return _db.Deleteable<T>();
    }

    public int DeleteReally<T>(T obj) where T : class, new()
    {
        return _db.Deleteable(obj).EnableDiffLogEventIF(_sqlSugarOptionsService.Diff).ExecuteCommand();
    }

    public int DeleteReally<T>(Expression<Func<T, bool>> expression) where T : class, new()
    {
        return _db.Deleteable(expression).EnableDiffLogEventIF(_sqlSugarOptionsService.Diff).ExecuteCommand();
    }

    public int DeleteReally<T>(List<T> obj) where T : class, new()
    {
        return _db.Deleteable(obj).EnableDiffLogEventIF(_sqlSugarOptionsService.Diff).ExecuteCommand();
    }


    #endregion


    #region update

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


    public int Update<T>(T obj) where T : class, new()
    {
        return _db.Updateable(obj).EnableDiffLogEventIF(_sqlSugarOptionsService.Diff).ExecuteCommand();
    }

    public int Update<T>(T[] obj) where T : class, new()
    {
        return _db.Updateable(obj).EnableDiffLogEventIF(_sqlSugarOptionsService.Diff).ExecuteCommand();
    }

    public int Update<T>(List<T> obj) where T : class, new()
    {
        return _db.Updateable(obj).EnableDiffLogEventIF(_sqlSugarOptionsService.Diff).ExecuteCommand();
    }

    #endregion

    public async Task UseTransactionAsync(Func<Task> func, IsolationLevel isolationLevel = IsolationLevel.RepeatableRead)
    {
        await _db.Ado.BeginTranAsync(isolationLevel);
        try
        {
            await func();
            await _db.Ado.CommitTranAsync();
        }
        catch (Exception)
        {
            await _db.Ado.RollbackTranAsync();
            throw;
        }
    }

    public void UseTransaction(Action action, IsolationLevel isolationLevel = IsolationLevel.RepeatableRead)
    {
        _db.Ado.BeginTran(isolationLevel);
        try
        {
            action();
            _db.Ado.CommitTran();
        }
        catch (Exception)
        {
            _db.Ado.RollbackTran();
            throw;
        }
    }


    public async Task IgnoreAsync<T>(Func<Task> func)
    {
        try
        {
            Client.QueryFilter.ClearAndBackup<T>();
            if (typeof(T) == typeof(ITenantId)) _sqlSugarOptionsService.IgnoreTenant = true;

            await func();
        }
        finally
        {
            Client.QueryFilter.Restore();
            if (typeof(T) == typeof(ITenantId)) _sqlSugarOptionsService.IgnoreTenant = false;
        }
    }

    public void Ignore<T>(Action action)
    {
        try
        {
            Client.QueryFilter.ClearAndBackup<T>();
            if (typeof(T) == typeof(ITenantId)) _sqlSugarOptionsService.IgnoreTenant = true;

            action();
        }
        finally
        {
            Client.QueryFilter.Restore();
            if (typeof(T) == typeof(ITenantId)) _sqlSugarOptionsService.IgnoreTenant = false;
        }
    }
}