﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Kurisu.AspNetCore.Authentication.Abstractions;
using Kurisu.AspNetCore.DataAccess.Entity;
using Kurisu.AspNetCore.DataAccess.SqlSugar.Attributes;
using Kurisu.AspNetCore.Utils.Extensions;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;

namespace Kurisu.AspNetCore.DataAccess.SqlSugar.Services.Implements;

internal class DbContext : IDbContext
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ISqlSugarOptionsService _sqlSugarOptionsService;

    public DbContext(ISqlSugarClient db, IServiceProvider serviceProvider)
    {
        Client = db;
        _serviceProvider = serviceProvider;
        _sqlSugarOptionsService = serviceProvider.GetService<ISqlSugarOptionsService>();
    }

    public ISqlSugarClient Client { get; }

    /// <inheritdoc />
    public IQueryableSetting GetQueryableSetting()
    {
        return _serviceProvider.GetService<IQueryableSetting>();
    }


    public ISugarQueryable<T> Queryable<T>()
    {
        var type = typeof(T);

        var query = Client.Queryable<T>();

        var setting = GetQueryableSetting();

        if (setting.GetEnableCrossTenant<T>())
        {
            var tenantIds = _serviceProvider.GetRequiredService<ICurrentUser>().GetUserClaim("tenants").Split(",").ToList();

            if (type.IsAssignableTo(typeof(ITenantId)))
            {
                query = query.Where(GetWhereExpression<T, string>(nameof(ITenantId.TenantId), tenantIds));
            }

            var tenantIdField = type.GetProperties().FirstOrDefault(x => x.IsDefined(typeof(TenantIdAttribute), false));
            if (tenantIdField != null)
            {
                query = query.Where(GetWhereExpression<T, string>(tenantIdField.Name, tenantIds));
            }
        }

        if (setting.GetEnableDataPermission<T>())
        {
            var permissionData = _serviceProvider.GetRequiredService<IGetDataPermissions>().GetData<T>();
            query = permissionData.Aggregate(query, (current, item) => current.Where(GetWhereExpression<T, Guid>(item.Key, item.Value)));
        }

        return query;
    }

    private static Expression<Func<T, bool>> GetWhereExpression<T, TType>(string property, List<TType> ls)
    {
        var parameterExpression = Expression.Parameter(typeof(T)); //x =>
        var prop = Expression.Property(parameterExpression, property); // x=> x.TenantId
        var eq = typeof(List<TType>).GetMethod("Contains", new[] { typeof(TType) })!; //Contains
        var constant = Expression.Constant(ls, typeof(List<TType>)); // tenantIds
        var containsExp = Expression.Call(constant, eq, prop); //tenantIds.Contains(x.TenantId)

        //// x => tenantIds.Contains(x.TenantId)
        return Expression.Lambda<Func<T, bool>>(containsExp, parameterExpression);
    }

    public IDbContext ChangeDb(string dbId)
    {
        var client = ((SqlSugarClient)Client).GetConnection(dbId);
        return new DbContext(client, _serviceProvider);
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

    public async Task<int> DeleteAsync<T>(T obj, bool isReally = false) where T : class, new()
    {
        if (isReally || !typeof(T).IsAssignableTo(typeof(ISoftDeleted)))
        {
            return await this.Deleteable(obj).ExecuteCommandAsync();
        }

        obj.SetPropertyValue(nameof(ISoftDeleted.IsDeleted), true);
        return await this.UpdateAsync(obj);
    }

    public async Task<int> DeleteAsync<T>(T[] obj, bool isReally = false) where T : class, new()
    {
        var list = obj.ToList();

        if (isReally || !typeof(T).IsAssignableTo(typeof(ISoftDeleted)))
        {
            return await this.Deleteable(list).ExecuteCommandAsync();
        }

        list.ForEach(x => x.SetPropertyValue(nameof(ISoftDeleted.IsDeleted), true));
        return await this.UpdateAsync(list);
    }

    public async Task<int> DeleteAsync<T>(List<T> list, bool isReally = false) where T : class, new()
    {
        if (isReally || !typeof(T).IsAssignableTo(typeof(ISoftDeleted)))
        {
            return await this.Deleteable(list).ExecuteCommandAsync();
        }

        list.ForEach(x => x.SetPropertyValue(nameof(ISoftDeleted.IsDeleted), true));
        return await this.UpdateAsync(list);
    }

    public int Delete<T>(T obj, bool isReally = false) where T : class, new()
    {
        if (isReally || !typeof(T).IsAssignableTo(typeof(ISoftDeleted)))
        {
            return this.Deleteable(obj).ExecuteCommand();
        }

        obj.SetPropertyValue(nameof(ISoftDeleted.IsDeleted), true);
        return this.Update(obj);
    }

    public int Delete<T>(T[] obj, bool isReally = false) where T : class, new()
    {
        var list = obj.ToList();

        if (isReally || !typeof(T).IsAssignableTo(typeof(ISoftDeleted)))
        {
            return this.Deleteable(list).ExecuteCommand();
        }

        list.ForEach(x => x.SetPropertyValue(nameof(ISoftDeleted.IsDeleted), true));
        return this.Update(list);
    }

    public int Delete<T>(List<T> list, bool isReally = false) where T : class, new()
    {
        if (isReally || !typeof(T).IsAssignableTo(typeof(ISoftDeleted)))
        {
            return this.Deleteable(list).ExecuteCommand();
        }

        list.ForEach(x => x.SetPropertyValue(nameof(ISoftDeleted.IsDeleted), true));
        return this.Update(list);
    }

    public IDeleteable<T> Deleteable<T>() where T : class, new()
    {
        return Client.Deleteable<T>().EnableDiffLogEventIF(_sqlSugarOptionsService.Diff);
    }

    public IDeleteable<T> Deleteable<T>(T obj) where T : class, new()
    {
        return Client.Deleteable(obj).EnableDiffLogEventIF(_sqlSugarOptionsService.Diff);
    }

    public IDeleteable<T> Deleteable<T>(List<T> list) where T : class, new()
    {
        return Client.Deleteable(list).EnableDiffLogEventIF(_sqlSugarOptionsService.Diff);
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