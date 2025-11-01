using System.Linq.Expressions;
using Kurisu.AspNetCore.Abstractions.Authentication;
using Kurisu.AspNetCore.Abstractions.DataAccess;
using Kurisu.AspNetCore.Abstractions.DataAccess.Contract.Field;
using Kurisu.AspNetCore.DataAccess.SqlSugar.Attributes;
using Kurisu.AspNetCore.Extensions;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;

namespace Kurisu.Extensions.SqlSugar;

internal class SqlSugarDbContext : ISqlSugarDbContext
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IDatasourceManager _datasourceManager;

    public SqlSugarDbContext(IServiceProvider serviceProvider, IDatasourceManager datasourceManager)
    {
        _serviceProvider = serviceProvider;
        _datasourceManager = datasourceManager;
    }

    public ISqlSugarClient Client => _datasourceManager.GetCurrentClient<ISqlSugarClient>();

    public ScopeQuerySetting GetScopeQuerySetting() => _serviceProvider.GetRequiredService<ScopeQuerySetting>();


    public ISugarQueryable<T> Queryable<T>()
    {
        var type = typeof(T);

        var query = Client.Queryable<T>();

        var setting = GetScopeQuerySetting();

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
                var currentUser = _serviceProvider.GetRequiredService<ICurrentUser>();
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


    #region insert

    public async Task<long> InsertReturnIdentityAsync<T>(T obj, CancellationToken cancellationToken = default) where T : class, new()
    {
        return await Client.Insertable(obj).ExecuteReturnBigIdentityAsync(cancellationToken);
    }

    public async Task<int> InsertAsync<T>(T obj, CancellationToken cancellationToken = default) where T : class, new()
    {
        return await Client.Insertable(obj).ExecuteCommandAsync(cancellationToken);
    }

    public async Task<int> InsertAsync<T>(List<T> objs, CancellationToken cancellationToken = default) where T : class, new()
    {
        return await Client.Insertable(objs.ToArray()).ExecuteCommandAsync(cancellationToken);
    }

    public long InsertReturnIdentity<T>(T obj) where T : class, new()
    {
        return Client.Insertable(obj).ExecuteReturnBigIdentity();
    }

    public int Insert<T>(T obj) where T : class, new()
    {
        return Client.Insertable(obj).ExecuteCommand();
    }

    public bool Insert<T>(List<T> objs) where T : class, new()
    {
        return Client.Insertable(objs.ToArray()).ExecuteCommandIdentityIntoEntity();
    }

    #endregion


    #region delete

    public async Task<int> DeleteAsync<T>(T obj, bool isReally = false, CancellationToken cancellationToken = default) where T : class, new()
    {
        if (isReally || !typeof(T).IsAssignableTo(typeof(ISoftDeleted)))
        {
            return await this.Deleteable(obj).ExecuteCommandAsync(cancellationToken);
        }

        obj.SetPropertyValue(nameof(ISoftDeleted.IsDeleted), true);
        return await this.UpdateAsync(obj, cancellationToken);
    }

    public async Task<int> DeleteAsync<T>(List<T> objs, bool isReally = false, CancellationToken cancellationToken = default) where T : class, new()
    {
        var list = objs.ToList();

        if (isReally || !typeof(T).IsAssignableTo(typeof(ISoftDeleted)))
        {
            var total = 0;
            foreach (var item in list)
            {
                total += await this.DeleteAsync(item, isReally, cancellationToken);
            }

            return total;
        }

        list.ForEach(x => x.SetPropertyValue(nameof(ISoftDeleted.IsDeleted), true));
        return await this.UpdateAsync(list, cancellationToken);
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

    public int Delete<T>(List<T> objs, bool isReally = false) where T : class, new()
    {
        if (isReally || !typeof(T).IsAssignableTo(typeof(ISoftDeleted)))
        {
            var total = 0;
            foreach (var item in objs)
            {
                total += Delete(item, isReally);
            }

            return total;
        }

        objs.ForEach(x => x.SetPropertyValue(nameof(ISoftDeleted.IsDeleted), true));
        return Update(objs);
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
        return Client.Deleteable<T>();
    }

    #endregion


    #region update

    public async Task<int> UpdateAsync<T>(T obj, CancellationToken cancellationToken = default) where T : class, new()
    {
        return await Client.Updateable(obj).ExecuteCommandAsync(cancellationToken);
    }

    public async Task<int> UpdateAsync<T>(List<T> objs, CancellationToken cancellationToken = default) where T : class, new()
    {
        return await Client.Updateable(objs).ExecuteCommandAsync(cancellationToken);
    }

    public IUpdateable<T> Updateable<T>() where T : class, new()
    {
        return Client.Updateable<T>();
    }


    public int Update<T>(T obj) where T : class, new()
    {
        return Client.Updateable(obj).ExecuteCommand();
    }

    public int Update<T>(List<T> objs) where T : class, new()
    {
        return Client.Updateable(objs).ExecuteCommand();
    }

    #endregion


    /// <inheritdoc />
    public async Task IgnoreTenantAsync(Func<Task> func)
    {
        try
        {
            Client.QueryFilter.ClearAndBackup<ITenantId>();
            GetScopeQuerySetting().IgnoreTenant = true;
            await func();
        }
        finally
        {
            Client.QueryFilter.Restore();
            GetScopeQuerySetting().IgnoreTenant = false;
        }
    }

    /// <inheritdoc />
    public void IgnoreTenant(Action action)
    {
        try
        {
            Client.QueryFilter.ClearAndBackup<ITenantId>();
            GetScopeQuerySetting().IgnoreTenant = true;
            action();
        }
        finally
        {
            Client.QueryFilter.Restore();
            GetScopeQuerySetting().IgnoreTenant = false;
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