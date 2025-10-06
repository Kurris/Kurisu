using System.Data;
using System.Linq.Expressions;
using Kurisu.AspNetCore.Abstractions.Authentication;
using Kurisu.AspNetCore.Abstractions.DataAccess.Contract;
using Kurisu.AspNetCore.DataAccess.SqlSugar;
using Kurisu.AspNetCore.DataAccess.SqlSugar.Attributes;
using Kurisu.AspNetCore.DataAccess.SqlSugar.Services;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;

namespace Kurisu.Extensions.SqlSugar.Services.Implements;

internal class DbContext(ISqlSugarClient db, IServiceProvider serviceProvider) : IDbContext
{
    private readonly ISqlSugarOptionsService _sqlSugarOptionsService = serviceProvider.GetRequiredService<ISqlSugarOptionsService>();

    public ISqlSugarClient Client { get; } = db;

    public IQueryableSetting GetQueryableSetting() => serviceProvider.GetRequiredService<IQueryableSetting>();


    public ISugarQueryable<T> Queryable<T>()
    {
        var type = typeof(T);

        var query = Client.Queryable<T>();

        var setting = GetQueryableSetting();

        if (setting.GetEnableCrossTenant<T>())
        {
            var tenantIdName =
                type.IsAssignableTo(typeof(ITenantId))
                    ? nameof(ITenantId.TenantId)
                    : type.GetProperties().Any(x => x.IsDefined(typeof(TenantIdAttribute), false))
                        ? type.GetProperties().First(x => x.IsDefined(typeof(TenantIdAttribute), false)).Name
                        : string.Empty;

            if (!string.IsNullOrEmpty(tenantIdName))
            {
                var currentUser = serviceProvider.GetRequiredService<ICurrentUser>();
                var tenantsStr = currentUser.GetUserClaim("tenants");
                if (!string.IsNullOrEmpty(tenantsStr))
                {
                    var tenantIds = tenantsStr.Split(",").ToList();
                    query = query.Where(GetWhereExpression<T, string>(tenantIdName, tenantIds));
                }
            }
        }

        // ReSharper disable once InvertIf
        if (setting.GetEnableDataPermission<T>())
        {
            var permissionData = serviceProvider.GetRequiredService<IGetDataPermissions>().GetData<T>();
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
        return new DbContext(client, serviceProvider);
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
        return Client.Deleteable<T>();
    }

    public IDeleteable<T> Deleteable<T>(T obj) where T : class, new()
    {
        return Client.Deleteable(obj);
    }

    public IDeleteable<T> Deleteable<T>(List<T> list) where T : class, new()
    {
        return Client.Deleteable(list);
    }

    #endregion


    #region update

    public async Task<int> UpdateAsync<T>(T obj) where T : class, new()
    {
        return await Client.Updateable(obj).ExecuteCommandAsync();
    }

    public async Task<int> UpdateAsync<T>(T[] obj) where T : class, new()
    {
        return await Client.Updateable(obj).ExecuteCommandAsync();
    }

    public async Task<int> UpdateAsync<T>(List<T> obj) where T : class, new()
    {
        return await Client.Updateable(obj).ExecuteCommandAsync();
    }

    public IUpdateable<T> Updateable<T>() where T : class, new()
    {
        return Client.Updateable<T>();
    }


    public int Update<T>(T obj) where T : class, new()
    {
        return Client.Updateable(obj).ExecuteCommand();
    }

    public int Update<T>(T[] obj) where T : class, new()
    {
        return Client.Updateable(obj).ExecuteCommand();
    }

    public int Update<T>(List<T> obj) where T : class, new()
    {
        return Client.Updateable(obj).ExecuteCommand();
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

    /// <inheritdoc />
    public async Task IgnoreTenantAsync(Func<Task> func)
    {
        try
        {
            Client.QueryFilter.ClearAndBackup<ITenantId>();
            _sqlSugarOptionsService.IgnoreTenant = true;
            await func();
        }
        finally
        {
            Client.QueryFilter.Restore();
            _sqlSugarOptionsService.IgnoreTenant = false;
        }
    }

    /// <inheritdoc />
    public void IgnoreTenant(Action action)
    {
        try
        {
            Client.QueryFilter.ClearAndBackup<ITenantId>();
            _sqlSugarOptionsService.IgnoreTenant = true;
            action();
        }
        finally
        {
            Client.QueryFilter.Restore();
            _sqlSugarOptionsService.IgnoreTenant = false;
        }
    }

    public async Task IgnoreSoftDeletedAsync(Func<Task> func)
    {
        try
        {
            Client.QueryFilter.ClearAndBackup<ISoftDeleted>();
            await func();
        }
        finally
        {
            Client.QueryFilter.Restore();
        }
    }

    public void IgnoreSoftDeleted(Action action)
    {
        try
        {
            Client.QueryFilter.ClearAndBackup<ISoftDeleted>();
            action();
        }
        finally
        {
            Client.QueryFilter.Restore();
        }
    }
}

public static class DbContextExtensions
{
    public static void SetPropertyValue<T>(this T obj, string propertyName, object value)
    {
        var prop = typeof(T).GetProperty(propertyName);
        if (prop != null && prop.CanWrite)
        {
            prop.SetValue(obj, value);
        }
    }
}