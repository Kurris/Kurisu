using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Kurisu.AspNetCore.Authentication.Abstractions;
using Kurisu.AspNetCore.DataAccess.Entity;
using Kurisu.AspNetCore.DataAccess.SqlSugar.Attributes;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;

namespace Kurisu.AspNetCore.DataAccess.SqlSugar.Services.Implements;

internal class DbContext : IDbContext
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ICurrentUser _currentUser;
    private readonly ISqlSugarOptionsService _sqlSugarOptionsService;

    public DbContext(ISqlSugarClient db, IServiceProvider serviceProvider, ICurrentUser currentUser)
    {
        Client = db;
        _serviceProvider = serviceProvider;
        _currentUser = currentUser;
        _sqlSugarOptionsService = serviceProvider.GetService<ISqlSugarOptionsService>();
    }

    public ISqlSugarClient Client { get; }

    public ISugarQueryable<T> Queryable<T>()
    {
        var type = typeof(T);

        var query = Client.Queryable<T>();

        var setting = _serviceProvider.GetService<QueryableSettingService>();

        if (setting.EnableCrossTenant && !setting.CrossTenantIgnoreTypes.Contains(type))
        {
            var tenantIds = _serviceProvider.GetService<ICurrentUser>().GetUserClaim("tenants").Split(",").ToList();

            if (typeof(T).IsAssignableTo(typeof(ITenantId)))
            {
                query = query.Where(GetWhereExpression<T, string>(nameof(ITenantId.TenantId), tenantIds));
            }

            var tenantIdField = typeof(T).GetProperties().FirstOrDefault(x => x.IsDefined(typeof(TenantIdAttribute), false));
            if (tenantIdField != null)
            {
                query = query.Where(GetWhereExpression<T, string>(tenantIdField.Name, tenantIds));
            }
        }

        if (setting.EnableDataPermission && !setting.DataPermissionIgnoreTypes.Contains(type))
        {
            var permissionData = _serviceProvider.GetRequiredService<IGetDataPermissions>().GetData<T>();
            query = permissionData.Aggregate(query, (current, item) => current.Where(GetWhereExpression<T, Guid>(item.Key, item.Value)));
        }

        return query;
    }

    private static Expression<Func<T, bool>> GetWhereExpression<T, TType>(string property, List<TType> ls)
    {
        var parameterExpression = Expression.Parameter(typeof(T));
        var prop = Expression.Property(parameterExpression, property);
        var eq = typeof(List<TType>).GetMethod("Contains", new[] { typeof(TType) })!;
        var constant = Expression.Constant(ls, typeof(List<TType>));
        var containsExp = Expression.Call(constant, eq, prop);

        return Expression.Lambda<Func<T, bool>>(containsExp, parameterExpression);
    }

    public IDbContext ChangeDb(string dbId)
    {
        var client = ((SqlSugarClient)Client).GetConnection(dbId);
        return new DbContext(client, _serviceProvider, _currentUser);
    }

    #region insert

    public async Task<long> InsertReturnIdentityAsync<T>(T obj) where T : class, new()
    {
        return await Client.Insertable(obj).ExecuteReturnBigIdentityAsync();
    }

    public async Task<int> InsertAsync<T>(T obj) where T : class, new()
    {
        return await Client.Insertable(obj).ExecuteCommandAsync();
    }

    public async Task<int> InsertAsync<T>(T[] obj) where T : class, new()
    {
        return await Client.Insertable(obj).ExecuteCommandAsync();
    }

    public async Task<int> InsertAsync<T>(List<T> obj) where T : class, new()
    {
        return await Client.Insertable(obj).ExecuteCommandAsync();
    }

    public long InsertReturnIdentity<T>(T obj) where T : class, new()
    {
        return Client.Insertable(obj).ExecuteReturnBigIdentity();
    }

    public int Insert<T>(T obj) where T : class, new()
    {
        return Client.Insertable(obj).ExecuteCommand();
    }

    public int Insert<T>(T[] obj) where T : class, new()
    {
        return Client.Insertable(obj).ExecuteCommand();
    }

    public int Insert<T>(List<T> obj) where T : class, new()
    {
        return Client.Insertable(obj).ExecuteCommand();
    }

    public async Task<int> SaveAsync<T>(T obj) where T : SugarBaseEntity, new()
    {
        return obj.Id == 0
            ? await this.InsertAsync(obj)
            : await this.UpdateAsync(obj);
    }

    public int Save<T>(T obj) where T : SugarBaseEntity, new()
    {
        return obj.Id == 0
            ? this.Insert(obj)
            : this.Update(obj);
    }

    #endregion


    #region delete

    public async Task<int> DeleteAsync<T>(T obj) where T : class, ISoftDeleted, new()
    {
        obj.IsDeleted = true;
        return await this.UpdateAsync(obj);
    }

    public async Task<int> DeleteAsync<T>(T[] obj) where T : class, ISoftDeleted, new()
    {
        var list = obj.ToList();
        list.ForEach(x => x.IsDeleted = true);
        return await this.UpdateAsync(list);
    }

    public async Task<int> DeleteAsync<T>(List<T> obj) where T : class, ISoftDeleted, new()
    {
        obj.ForEach(x => x.IsDeleted = true);
        return await this.UpdateAsync(obj);
    }

    public async Task<int> DeleteReallyAsync<T>(T obj) where T : class, new()
    {
        return await Client.Deleteable(obj).EnableDiffLogEventIF(_sqlSugarOptionsService.Diff).ExecuteCommandAsync();
    }

    public async Task<int> DeleteReallyAsync<T>(Expression<Func<T, bool>> expression) where T : class, new()
    {
        return await Client.Deleteable(expression).EnableDiffLogEventIF(_sqlSugarOptionsService.Diff).ExecuteCommandAsync();
    }

    public async Task<int> DeleteReallyAsync<T>(List<T> obj) where T : class, new()
    {
        return await Client.Deleteable(obj).EnableDiffLogEventIF(_sqlSugarOptionsService.Diff).ExecuteCommandAsync();
    }


    public int Delete<T>(T obj) where T : class, ISoftDeleted, new()
    {
        obj.IsDeleted = true;
        return this.Update(obj);
    }

    public int Delete<T>(T[] obj) where T : class, ISoftDeleted, new()
    {
        var list = obj.ToList();
        list.ForEach(x => x.IsDeleted = true);
        return this.Update(list);
    }

    public int Delete<T>(List<T> obj) where T : class, ISoftDeleted, new()
    {
        obj.ForEach(x => x.IsDeleted = true);
        return this.Update(obj);
    }

    public IDeleteable<T> Deleteable<T>() where T : class, new()
    {
        return Client.Deleteable<T>();
    }

    public int DeleteReally<T>(T obj) where T : class, new()
    {
        return Client.Deleteable(obj).EnableDiffLogEventIF(_sqlSugarOptionsService.Diff).ExecuteCommand();
    }

    public int DeleteReally<T>(Expression<Func<T, bool>> expression) where T : class, new()
    {
        return Client.Deleteable(expression).EnableDiffLogEventIF(_sqlSugarOptionsService.Diff).ExecuteCommand();
    }

    public int DeleteReally<T>(List<T> obj) where T : class, new()
    {
        return Client.Deleteable(obj).EnableDiffLogEventIF(_sqlSugarOptionsService.Diff).ExecuteCommand();
    }

    #endregion


    #region update

    public async Task<int> UpdateAsync<T>(T obj) where T : class, new()
    {
        return await Client.Updateable(obj).EnableDiffLogEventIF(_sqlSugarOptionsService.Diff).ExecuteCommandAsync();
    }

    public async Task<int> UpdateAsync<T>(T[] obj) where T : class, new()
    {
        return await Client.Updateable(obj).EnableDiffLogEventIF(_sqlSugarOptionsService.Diff).ExecuteCommandAsync();
    }

    public async Task<int> UpdateAsync<T>(List<T> obj) where T : class, new()
    {
        return await Client.Updateable(obj).EnableDiffLogEventIF(_sqlSugarOptionsService.Diff).ExecuteCommandAsync();
    }

    public IUpdateable<T> Updateable<T>() where T : class, new()
    {
        return Client.Updateable<T>().EnableDiffLogEventIF(_sqlSugarOptionsService.Diff);
    }


    public int Update<T>(T obj) where T : class, new()
    {
        return Client.Updateable(obj).EnableDiffLogEventIF(_sqlSugarOptionsService.Diff).ExecuteCommand();
    }

    public int Update<T>(T[] obj) where T : class, new()
    {
        return Client.Updateable(obj).EnableDiffLogEventIF(_sqlSugarOptionsService.Diff).ExecuteCommand();
    }

    public int Update<T>(List<T> obj) where T : class, new()
    {
        return Client.Updateable(obj).EnableDiffLogEventIF(_sqlSugarOptionsService.Diff).ExecuteCommand();
    }

    #endregion

    public async Task UseTransactionAsync(Func<Task> func, IsolationLevel isolationLevel = IsolationLevel.RepeatableRead)
    {
        await Client.Ado.BeginTranAsync(isolationLevel);
        try
        {
            await func();
            await Client.Ado.CommitTranAsync();
        }
        catch (Exception)
        {
            await Client.Ado.RollbackTranAsync();
            throw;
        }
    }

    public void UseTransaction(Action action, IsolationLevel isolationLevel = IsolationLevel.RepeatableRead)
    {
        Client.Ado.BeginTran(isolationLevel);
        try
        {
            action();
            Client.Ado.CommitTran();
        }
        catch (Exception)
        {
            Client.Ado.RollbackTran();
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